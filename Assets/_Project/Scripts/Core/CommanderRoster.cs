using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    // 플레이어 장수의 생성 창구이자 명부. 시작 장수뿐 아니라 번식·영입·포로 회유로 합류하는 장수도
    // 전부 이 컴포넌트를 거치므로, 장수를 만드는 방법은 여기 한 곳에만 있다.
    public class CommanderRoster : MonoBehaviour
    {
        public static CommanderRoster Instance { get; private set; }

        [SerializeField] private UnitData[] roleProfiles;
        [SerializeField] private Transform spawnOrigin;
        // ponytail: 초기 장수/병력 수는 임시 시작 설정이다.
        [SerializeField, Min(1)] private int startingCommanders = 12;
        [SerializeField, Min(1f)] private float spawnSampleRadius = 10f;

        private readonly List<CommanderAnt> commanders = new List<CommanderAnt>();
        private int nextCommanderNumber = 1;

        public IReadOnlyList<CommanderAnt> Commanders
        {
            get
            {
                commanders.RemoveAll(commander => commander == null);
                return commanders;
            }
        }

        // 세력 규모. 스카우트 영입 성공률이 이 값을 읽는다.
        public int Count => Commanders.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (roleProfiles == null || roleProfiles.Length == 0 || roleProfiles[0] == null || AntPool.Instance == null) return;
            var origin = spawnOrigin != null ? spawnOrigin.position : transform.position;
            if (GameSession.Exists && GameSession.Instance.GameStarted)
            {
                if (GameSession.Instance.PendingLoad != null) return;
                var team = GameSession.Instance.Options.startingTeam;
                if (team == null) return;
                for (var i = 0; i < team.Length; i++)
                {
                    var c = Create(team[i].name, CommanderRank.Sergeant, new[] { UnitRole.Worker, UnitRole.Melee },
                        UnitRole.Worker, JsonUtility.FromJson<CommanderTraits>(JsonUtility.ToJson(team[i].traits)), origin + new Vector3(i * 2f, 0, 4));
                    if (c == null) throw new System.InvalidOperationException("시작 장수를 배치할 수 없습니다.");
                    c.RestoreTalents(team[i].talents.Copy());
                    var p = c.PersonalState;
                    p.ageMonths = team[i].ageYears * 12; p.lifespanMonths = 80 * 12;
                    p.appearanceSeed = team[i].appearanceSeed; p.ordinaryClothes = StartingCandidate.Clothes(GameSession.Instance.Options.biome);
                    p.toolTier = 1; p.relations.Clear();
                    p.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.WoodenSword, quality = 1 });
                    p.carriedRations = new FoodLots(); p.carriedRations.Add(c.RationPortion * 14, 24 * 150f);
                    c.RefreshEquipment();
                }
                return;
            }
            for (var i = 0; i < startingCommanders; i++)
            {
                var position = origin + new Vector3((i % 4 - 1.5f) * 2f, 0f, 4f + i / 4 * 2f);
                var combatRole = (UnitRole)(1 + i % 5);
                var commander = Create(null, CommanderRank.Sergeant, new[] { UnitRole.Worker, combatRole },
                    UnitRole.Worker, CommanderTraits.Random(), position);

            }
        }

        // 장수 한 명을 만들어 명부에 넣는다. displayName이 비면 일련번호로 자동 명명한다.
        // NavMesh 위에 자리를 잡지 못하면 아무것도 만들지 않고 null을 돌려준다(반쯤 생성된 장수를 남기지 않는다).
        public CommanderAnt Create(string displayName, CommanderRank rank, IEnumerable<UnitRole> roles,
            UnitRole startRole, CommanderTraits traits, Vector3 position)
        {
            if (roleProfiles == null || roleProfiles.Length == 0 || roleProfiles[0] == null) return null;
            if (!NavMesh.SamplePosition(position, out var hit, spawnSampleRadius, NavMesh.AllAreas)) return null;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = string.IsNullOrEmpty(displayName) ? $"Commander {nextCommanderNumber}" : displayName;
            go.transform.SetParent(transform);
            go.transform.position = hit.position;
            go.transform.localScale = Vector3.one * .6f;
            nextCommanderNumber++;

            var commander = go.AddComponent<CommanderAnt>();
            commander.ConfigureCommander(go.name, rank, roles, startRole);
            commander.Initialize(roleProfiles[0], null, null);
            commander.SetRoleProfiles(roleProfiles);
            if (traits != null) commander.ApplyTraits(traits);
            commander.Talents.Generate(commander.Traits);
            commanders.Add(commander);
            return commander;
        }

        public void SetRoleProfiles(UnitData[] profiles) => roleProfiles = profiles;
        internal void Forget(CommanderAnt commander) => commanders.Remove(commander);
        private void Update()
        {
            foreach (var c in Commanders) if (c.IsCaptive) c.TickCaptivity(Time.deltaTime);
        }

        // 저장 복원 전용. 기존 장수를 모두 없앤다. 배속 개미는 각 장수의 OnDestroy가 대기 풀로 돌려주고,
        // 풀 숫자는 그 뒤 AntPool.RestoreCounts가 저장값으로 다시 덮어쓴다.
        internal void ClearAll()
        {
            foreach (var commander in new List<CommanderAnt>(commanders))
                if (commander != null) Destroy(commander.gameObject);
            commanders.Clear();
            nextCommanderNumber = 1;
        }

        // 복원된 장수는 항상 명시한 이름을 그대로 쓴다. 자동 일련번호가 저장된 이름을 덮지 않게 한다.
        internal CommanderAnt CreateForLoad(string displayName, CommanderRank rank, IEnumerable<UnitRole> roles,
            UnitRole startRole, CommanderTraits traits, Vector3 position)
        {
            return Create(string.IsNullOrEmpty(displayName) ? "Commander" : displayName, rank, roles, startRole, traits, position);
        }
    }
}
