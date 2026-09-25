using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 포로가 된 적 장수 한 명의 기록. 살아 있는 유닛이 아니라 수용소가 들고 있는 데이터다.
    [System.Serializable]
    public class Prisoner
    {
        public string Name;
        public CommanderRank Rank;
        public UnitRole[] Roles;
        public CommanderTraits Traits;
        public CommanderTalents Talents;
        public int PersuadeAttempts;
        public CommanderPersonalState PersonalState = new CommanderPersonalState();
        public int LabAttack, LabArmor;
        public float StrikeCooldown, StanceCooldown;

        public Prisoner(string name, CommanderRank rank, UnitRole[] roles, CommanderTraits traits)
        {
            Name = name;
            Rank = rank;
            Roles = roles != null && roles.Length > 0 ? roles : new[] { UnitRole.Worker };
            Traits = traits ?? CommanderTraits.Random();
            Talents = new CommanderTalents(); Talents.Generate(Traits);
        }
    }

    // 포로 회유. 기획: 회유는 확률 판정이고 포로의 성격·충성심에 따라 성공률이 달라진다.
    // 실패해도 포로는 유지되어 반복 시도할 수 있고, 언제든 처형할 수 있으며, 가끔 탈출을 시도한다.
    // 이 컴포넌트가 곧 수용소이므로, 없거나 정원이 차면 적 장수는 포로가 되지 않고 그대로 죽는다.
    public class PrisonerCamp : MonoBehaviour
    {
        // 완공된 수용소는 여러 채 지을 수 있다. 활성 상태인 것만 등록되므로 배치용 템플릿(비활성)은 들어오지 않는다.
        private static readonly List<PrisonerCamp> Active = new List<PrisonerCamp>();

        // 기존 호출부(적 장수 사망 처리·검사)가 쓰는 창구. 자리가 남은 수용소를 우선 고르고,
        // 전부 찼으면 먼저 지은 곳을 돌려줘 "정원 초과" 판정이 그대로 동작하게 한다.
        public static PrisonerCamp Instance
        {
            get
            {
                PrisonerCamp full = null;
                foreach (var camp in Active)
                {
                    if (camp == null) continue;
                    if (camp.HasSpace) return camp;
                    full ??= camp;
                }
                return full;
            }
        }

        // ponytail: 회유·탈출 수치는 1차 프로토타입 잠정값이다. 기획 확정 시 이 필드만 고치면 된다.
        [SerializeField, Min(1)] private int capacity = 5;
        [SerializeField, Range(0f, 1f)] private float basePersuadeChance = .6f;
        // 충성심이 높을수록 회유가 어렵다. 충성심 100이면 이 값만큼 확률이 깎인다.
        [SerializeField, Range(0f, 1f)] private float loyaltyPenalty = .5f;
        [SerializeField, Range(0f, 1f)] private float attemptBonus = .05f;
        [SerializeField, Min(0.1f)] private float escapeCheckSeconds = 30f;
        [SerializeField, Range(0f, 1f)] private float escapeChance = .1f;
        [SerializeField, Min(0)] private int persuadeFoodCost = 10;

        private readonly List<Prisoner> prisoners = new List<Prisoner>();
        private float escapeTimer;

        public IReadOnlyList<Prisoner> Prisoners => prisoners;
        public int Count => prisoners.Count;
        public bool HasSpace => prisoners.Count < capacity;
        public int EscapedCount { get; private set; }
        public int RecruitedCount { get; private set; }
        public int ExecutedCount { get; private set; }

        public int Capacity => capacity;
        public int PersuadeFoodCost => persuadeFoodCost;

        private void OnEnable()
        {
            Active.Add(this);
            escapeTimer = escapeCheckSeconds;
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        internal float EscapeTimer => escapeTimer;

        // 저장 복원 전용. 포로 명단을 통째로 갈아끼운다.
        internal void RestoreState(List<Prisoner> saved, float timer, int recruited, int executed, int escaped)
        {
            prisoners.Clear();
            if (saved != null) prisoners.AddRange(saved);
            escapeTimer = timer > 0f ? timer : escapeCheckSeconds;
            RecruitedCount = Mathf.Max(0, recruited);
            ExecutedCount = Mathf.Max(0, executed);
            EscapedCount = Mathf.Max(0, escaped);
        }

        private void Update() => Tick(Time.deltaTime);

        // 검사 스크립트가 시간을 직접 밀어 넣을 수 있도록 분리해 둔다.
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || prisoners.Count == 0) return;
            escapeTimer -= deltaTime;
            if (escapeTimer > 0f) return;
            escapeTimer = escapeCheckSeconds;

            // 뒤에서부터 돌아 탈출로 인한 인덱스 이동이 남은 포로를 건너뛰지 않게 한다.
            for (var i = prisoners.Count - 1; i >= 0; i--)
            {
                // 충성심이 높은 포로일수록 자기 진영으로 돌아가려 한다.
                var chance = escapeChance * (1f + prisoners[i].Traits.Loyalty / (float)CommanderTraits.MaxLoyalty);
                if (Random.value >= chance) continue;
                CampaignHistory.Record("탈주", prisoners[i].Name, "포로 수용소 탈출");
                prisoners.RemoveAt(i);
                EscapedCount++;
                AntColony.UI.ToastManager.Show("A prisoner escaped.");
            }
        }

        // 적 장수를 포로로 받는다. 정원이 차 있으면 거부하며, 이때 적 장수는 평소대로 죽는다.
        public bool TryCapture(string name, CommanderRank rank, UnitRole[] roles, CommanderTraits traits, CommanderTalents talents = null)
        {
            if (!isActiveAndEnabled || !HasSpace) return false;
            var prisoner = new Prisoner(name, rank, roles, traits);
            if (talents != null) prisoner.Talents = talents.Copy();
            prisoners.Add(prisoner);
            CampaignHistory.Record("포로", name, "적 장수 포획");
            AntColony.UI.ToastManager.Show(name + " captured.");
            return true;
        }

        internal bool TryCapture(CommanderAnt c)
        {
            if (!TryCapture(c.CommanderName, CommanderRank.Sergeant, new[] { UnitRole.Worker }, c.Traits, c.Talents)) return false;
            var prisoner = prisoners[prisoners.Count - 1];
            prisoner.PersonalState = c.CapturePersonalState();
            prisoner.PersonalState.social.departure = DepartureState.Imprisoned;
            prisoner.LabAttack = c.LabAttackLevel; prisoner.LabArmor = c.LabArmorLevel;
            prisoner.StrikeCooldown = c.Skills.PowerStrikeCooldownLeft; prisoner.StanceCooldown = c.Skills.DefensiveStanceCooldownLeft;
            return true;
        }

        // 회유 성공률. 충성심이 높을수록 낮아지고, 반복 시도할수록 조금씩 오른다.
        public float PersuadeChance(Prisoner prisoner) => PersuadeChance(prisoner, 0);

        // TryPersuade는 시도 횟수를 올린 뒤에 주사위를 굴린다. UI가 PersuadeChance를 그대로 쓰면
        // 실제 판정보다 낮은 값을 보여주므로, 다음 시도에 적용될 확률은 이쪽을 쓴다(판정 확률은 그대로다).
        public float NextPersuadeChance(Prisoner prisoner) => PersuadeChance(prisoner, 1);

        private float PersuadeChance(Prisoner prisoner, int extraAttempts)
        {
            if (prisoner == null) return 0f;
            var loyaltyRatio = prisoner.Traits.Loyalty / (float)CommanderTraits.MaxLoyalty;
            // 헌신형은 좀처럼 넘어오지 않고 용감형은 강한 쪽을 따른다.
            var personalityShift = prisoner.Traits.Personality switch
            {
                CommanderPersonality.Devoted => -.2f,
                CommanderPersonality.Brave => .1f,
                _ => 0f
            };
            var chance = basePersuadeChance - loyaltyRatio * loyaltyPenalty + personalityShift
                + (prisoner.PersuadeAttempts + extraAttempts) * attemptBonus;
            return Mathf.Clamp01(chance);
        }

        // 회유 시도. 성공하면 포로가 내 장수로 합류하고, 실패하면 포로는 그대로 남아 다시 시도할 수 있다.
        public bool TryPersuade(int index)
        {
            // 부서지거나 꺼진 수용소는 아무것도 못 한다(식량도 쓰지 않는다).
            if (!isActiveAndEnabled || index < 0 || index >= prisoners.Count) return false;
            var prisoner = prisoners[index];
            if (persuadeFoodCost > 0 && ResourceManager.Instance != null
                && !ResourceManager.Instance.TrySpend(persuadeFoodCost, 0, reason: ResourceReason.Expedition)) return false;

            prisoner.PersuadeAttempts++;
            if (Random.value > PersuadeChance(prisoner)) return false;

            var roster = CommanderRoster.Instance;
            // 합류할 장수를 실제로 만들지 못하면 포로를 잃지 않는다(다음 시도로 넘긴다).
            var recruit = roster?.Create(prisoner.Name, prisoner.Rank, prisoner.Roles,
                prisoner.Roles[0], prisoner.Traits, transform.position);
            if (recruit == null) return false;
            recruit.RestoreTalents(prisoner.Talents);
            recruit.RestorePersonalState(prisoner.PersonalState);
            recruit.Social.departure = DepartureState.None; recruit.Social.pendingDeparture = false;
            recruit.PersonalState.departure = "";
            recruit.Traits.SetLoyalty(30);
            recruit.RestoreLabLevels(prisoner.LabAttack, prisoner.LabArmor);
            recruit.Skills.Restore(false, prisoner.StrikeCooldown, prisoner.StanceCooldown, 0);
            recruit.GetComponent<SelectableObject>().enabled = true;

            prisoners.RemoveAt(index);
            RecruitedCount++;
            CampaignHistory.Record("합류", recruit.CommanderName, "포로 회유");
            AntColony.UI.ToastManager.Show(prisoner.Name + " joined your colony.");
            return true;
        }

        // UI는 인덱스가 아니라 포로 자체를 들고 있는다. 앞쪽 포로가 탈출해 인덱스가 밀려도
        // 엉뚱한 포로를 회유·처형하지 않고, 이미 사라진 포로면 그냥 실패한다.
        public bool TryPersuade(Prisoner prisoner) => TryPersuade(prisoners.IndexOf(prisoner));

        public bool Execute(Prisoner prisoner) => Execute(prisoners.IndexOf(prisoner));

        // 기획: 플레이어 선택으로 언제든 처형할 수 있다.
        public bool Execute(int index)
        {
            if (!isActiveAndEnabled || index < 0 || index >= prisoners.Count) return false;
            var prisoner = prisoners[index];
            CommanderAnt.OnPrisonerExecuted(prisoner.PersonalState.id, prisoner.PersonalState.originFaction, prisoner.Name);
            CampaignHistory.Record("처형", prisoner.Name, "포로 처형", true);
            prisoners.RemoveAt(index);
            ExecutedCount++;
            AntColony.UI.ToastManager.Show("Prisoner executed.");
            return true;
        }
    }
}
