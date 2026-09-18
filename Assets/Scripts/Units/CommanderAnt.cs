using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // 장수(지휘관). 플레이어가 직접 조작하는 유일한 유닛이다.
    // 일반개미는 GameObject가 아니라 AntPool의 숫자이므로, 배정된 병력 수가 곧 이 부대의 체력이다
    // (기획: 개미 1마리 = HP 1). 채집량·공격력도 병력 수에 비례한다.
    public class CommanderAnt : WorkerAnt
    {
        // 기획 고정값: 일반개미 1마리가 HP 1이다. UnitData.maxHealth는 장수 부대 체력에 쓰지 않는다.
        public const float HealthPerTroop = 1f;
        // ponytail: 지원 수치는 1차 프로토타입 상수다. 밸런스가 확정되면 데이터 에셋으로 옮긴다.
        public const float SupportAuraRadius = 6f;
        public const float SupportAttackBonus = 1f;
        public const float SupportArmorBonus = 1f;

        [SerializeField] private string commanderName = "Commander";
        [SerializeField] private CommanderRank rank = CommanderRank.Sergeant;
        [SerializeField] private List<UnitRole> allowedRoles = new List<UnitRole> { UnitRole.Worker };
        [SerializeField] private UnitRole role = UnitRole.Worker;
        [SerializeField, Min(0)] private int troopCount;
        [SerializeField] private UnitData[] roleProfiles;
        [SerializeField] private CommanderProgression progression = new CommanderProgression();
        [SerializeField] private CommanderTraits traits = new CommanderTraits();
        [SerializeField] private CommanderWorkProficiency workProficiency = new CommanderWorkProficiency();
        // 연구소 개별 강화 단계. 보직이 바뀌어도 장수에게 남는다.
        [SerializeField, Min(0)] private int labAttackLevel;
        [SerializeField, Min(0)] private int labArmorLevel;
        public const float LabAttackBonusPerLevel = 2f;
        public const float LabArmorBonusPerLevel = 1f;

        // 1마리분(HP 1)에 못 미친 누적 피해. 배정/회수/보직 변경 어디서도 초기화하지 않는다(회복·복제 금지).
        private float pendingDamage;
        private UnitData runtimeData;
        private bool troopsReleased;
        private readonly CommanderSkills skills = new CommanderSkills();
        public ExpeditionTransport Transport { get; internal set; }
        public bool IsEmbarked { get; private set; }
        private readonly List<Renderer> embarkRenderers = new List<Renderer>();
        private readonly List<Collider> embarkColliders = new List<Collider>();

        internal void SetEmbarked(bool embarked, Vector3 position)
        {
            CommandStop();
            if (embarked && !IsEmbarked)
            {
                foreach (var r in GetComponentsInChildren<Renderer>())
                    if (r.enabled) { embarkRenderers.Add(r); r.enabled = false; }
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.enabled) { embarkColliders.Add(c); c.enabled = false; }
            }
            IsEmbarked = embarked;
            GetComponent<SelectableObject>().enabled = !embarked;
            Agent.enabled = false;
            transform.position = position;
            if (!embarked)
            {
                foreach (var r in embarkRenderers) if (r != null) r.enabled = true;
                foreach (var c in embarkColliders) if (c != null) c.enabled = true;
                embarkRenderers.Clear();
                embarkColliders.Clear();
                ApplyMovementMode();
            }
        }

        protected override void Update()
        {
            if (!IsEmbarked) base.Update();
        }

        public override void CommandMove(Vector3 destination)
        {
            if (!IsEmbarked) base.CommandMove(destination);
        }

        public string CommanderName => commanderName;
        public CommanderRank Rank => rank;
        public UnitRole Role => role;
        public IReadOnlyList<UnitRole> AllowedRoles => allowedRoles;

        public int TroopCount => troopCount;
        public int CommandLimit => CapacityFor(role);
        public int FreeRanks => Mathf.Max(0, CommandLimit - troopCount);
        public bool HasTroops => troopCount > 0;

        public float MaxHealth => troopCount * HealthPerTroop;
        public override float CurrentHealth => Mathf.Max(0f, MaxHealth - pendingDamage);

        // 임시 정책: 이번 1차 전환에서 장수는 죽지 않는다. 병력 0이면 무력화될 뿐 선택·보충 대상으로 남는다.
        // 병력 0 이후의 사망/포로 규칙은 미정이며, 이 값을 확정 기획으로 취급하지 말 것.
        public override bool IsDead => false;

        public CommanderProgression Progression => progression;
        public CommanderTraits Traits => traits;
        public CommanderWorkProficiency WorkProficiency => workProficiency;
        public int LabAttackLevel => labAttackLevel;
        public int LabArmorLevel => labArmorLevel;
        // 한 장수는 동시에 한 연구소에서만 강화된다. 연구소가 시작·해제한다.
        public ResearchLab LabUpgradeLab { get; set; }
        public bool LabUpgradeBusy => LabUpgradeLab != null;

        public CommanderSkills Skills => skills;

        // 수동 시전. 비활성·병력 0이면 거부하고, 레벨·중복·재사용 대기는 CommanderSkills가 판정한다.
        public bool CanPowerStrike => isActiveAndEnabled && !IsEmbarked && HasTroops && skills.CanArmPowerStrike(progression.Level);
        public bool CanDefensiveStance => isActiveAndEnabled && !IsEmbarked && HasTroops && skills.CanStartDefensiveStance(progression.Level);
        public bool TryPowerStrike() => CanPowerStrike && skills.TryArmPowerStrike(progression.Level);
        public bool TryDefensiveStance() => CanDefensiveStance && skills.TryStartDefensiveStance(progression.Level);

        public void CompleteLabUpgrade(bool attack, int maxLevel)
        {
            if (attack) labAttackLevel = Mathf.Min(maxLevel, labAttackLevel + 1);
            else labArmorLevel = Mathf.Min(maxLevel, labArmorLevel + 1);
        }

        // 병력 수만큼 부대 전체의 전투력/채집량이 늘어난다.
        // 레벨 공격 보너스는 기존 공격력과 같이 1마리분에 더해진 뒤 병력 수만큼 곱해진다.
        // 신중형 성격은 1마리분 공격력을 깎으므로 0 밑으로 내려가지 않게 막는다(병력 수를 곱하면 부호가 증폭된다).
        public override float AttackDamage => Mathf.Max(0f, base.AttackDamage + progression.AttackBonus
            + traits.AttackBonus + labAttackLevel * LabAttackBonusPerLevel
            + (HasSupportAura ? SupportAttackBonus : 0f)) * troopCount;
        public override float Armor => base.Armor + progression.ArmorBonus + traits.ArmorBonus
            + labArmorLevel * LabArmorBonusPerLevel + (HasSupportAura ? SupportArmorBonus : 0f)
            + (skills.DefensiveStanceActive ? CommanderSkills.DefensiveStanceArmor : 0f);
        protected override float GatherRate => base.GatherRate * Mathf.Max(1, troopCount) * workProficiency.GatherMultiplier;
        protected override void OnGathered(float amount) => workProficiency.AddGathered(amount);
        protected override float CarryCapacity => base.CarryCapacity * Mathf.Max(1, troopCount);

        // 중첩 없이 가장 가까운 지원 장수 한 명만 확인한다. 현재 장수 12명 규모에서는 선형 검색이 가장 단순하다.
        public bool HasSupportAura
        {
            get
            {
                var radiusSquared = SupportAuraRadius * SupportAuraRadius;
                foreach (var unit in Active)
                    if (unit is CommanderAnt support && support != this && support.HasTroops
                        && support.Role == UnitRole.Support
                        && (support.Position - Position).sqrMagnitude <= radiusSquared)
                        return true;
                return false;
            }
        }

        // 운반 중이거나 건설 중에는 배정/회수/보직 변경을 막는다. 중간에 인원이 바뀌면 자원이 증발한다.
        public bool CanChangeAllocation => !IsEmbarked && !IsCarrying && !IsConstructing;

        public override bool CanStartConstruction => Transport == null && HasTroops && base.CanStartConstruction;

        public bool CanTakeRole(UnitRole candidate) => allowedRoles != null && allowedRoles.Contains(candidate);

        // 보직별 지휘 한도. 지금은 관직만으로 정해지며, 보직 연구 보너스가 생기면 여기 한 곳만 고치면 된다.
        public int CapacityFor(UnitRole candidate) => CommanderRanks.CommandLimit(rank);

        public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)
        {
            // 보직에 따라 role을 바꿔야 하므로 공유 에셋을 그대로 쓰지 않고 장수 전용 사본을 만든다.
            if (data != null && data != runtimeData)
            {
                if (runtimeData != null) Destroy(runtimeData);
                runtimeData = Instantiate(data);
                runtimeData.name = data.name + " (" + commanderName + ")";
            }

            base.Initialize(runtimeData != null ? runtimeData : data, sourcePool, prefab);
            ApplyRoleProfile();
            ApplyMovementMode();
        }

        // 여왕방/설정 스크립트가 장수 개체를 구분해 세팅할 때 쓴다.
        public void ConfigureCommander(string displayName, CommanderRank commanderRank, IEnumerable<UnitRole> roles, UnitRole startRole)
        {
            commanderName = displayName;
            rank = commanderRank;
            allowedRoles = new List<UnitRole>(roles);
            if (allowedRoles.Count == 0) allowedRoles.Add(startRole);
            role = CanTakeRole(startRole) ? startRole : allowedRoles[0];
            if (Data != null)
            {
                ApplyRoleProfile();
                ApplyMovementMode();
            }
        }

        // 번식·영입·포로 회유로 합류한 장수의 성격/충성심을 세팅한다. null이면 기존 값을 유지한다.
        public void ApplyTraits(CommanderTraits value)
        {
            if (value != null) traits = value;
        }

        // 대기 중인 일반개미를 이 장수에게 배정한다. 지휘 한도와 대기 인원을 모두 넘지 못한다.
        public bool TryAssign(int count)
        {
            if (count <= 0 || Transport != null || !isActiveAndEnabled || !CanChangeAllocation || AntPool.Instance == null) return false;
            if (count > CommandLimit - troopCount) return false;
            if (!AntPool.Instance.TryAssign(count)) return false;
            troopsReleased = false;
            troopCount += count;
            return true;
        }

        // 배정된 병력을 대기 풀로 돌려보낸다. 실제로 돌려보낸 수를 반환한다.
        // pendingDamage는 유지되므로 회수 후 재배정으로 피해를 씻어낼 수 없다.
        public int ReturnTroops(int count)
        {
            if (count <= 0 || Transport != null || !CanChangeAllocation || AntPool.Instance == null) return 0;
            // 부상 중인 마지막 1마리를 건강한 대기 인력으로 넘겨 회복시키지 않는다.
            count = Mathf.Min(count, troopCount - (pendingDamage > 0f ? 1 : 0));
            if (count <= 0) return 0;
            AntPool.Instance.ReturnAssigned(count);
            troopCount -= count;
            if (troopCount == 0) CommandStop();
            return count;
        }

        // 보직 변경. 손실(pendingDamage/troopCount)은 그대로 유지되고 회복도 복제도 일어나지 않는다.
        public bool TrySetRole(UnitRole newRole)
        {
            if (!CanChangeAllocation || !CanTakeRole(newRole)) return false;
            if (CapacityFor(newRole) < troopCount) return false;
            if (newRole == role) return true;
            if (IsFlying && newRole != UnitRole.Flying && !UnityEngine.AI.NavMesh.SamplePosition(
                transform.position, out _, LandingSampleRadius, UnityEngine.AI.NavMesh.AllAreas)) return false;

            role = newRole;
            ApplyRoleProfile();
            CommandStop();
            ApplyMovementMode();
            return true;
        }

        // 관직 변경은 무료다. 한도를 낮춰 현재 병력을 초과하게 되는 경우만 거부한다.
        public bool TrySetRank(CommanderRank newRank)
        {
            if (!System.Enum.IsDefined(typeof(CommanderRank), newRank)) return false;
            if (CommanderRanks.CommandLimit(newRank) < troopCount) return false;
            rank = newRank;
            return true;
        }

        // 피해는 부대 전체가 나눠 받고, HP 1이 쌓일 때마다 병력이 실제로 줄어든다(환급 없음).
        public override void TakeDamage(float amount)
        {
            if (IsEmbarked || troopCount <= 0 || float.IsNaN(amount) || amount <= 0f) return;

            var applied = Mathf.Max(1f, amount - Armor);

            // 즉사급/무한대 피해는 루프를 돌리지 않고 남은 병력만큼만 한 번에 처리한다.
            if (float.IsInfinity(applied) || applied >= CurrentHealth)
            {
                AntPool.Instance?.LoseAssigned(troopCount);
                troopCount = 0;
                pendingDamage = 0f;
                CommandStop();
                return;
            }

            pendingDamage += applied;
            var casualties = Mathf.FloorToInt(pendingDamage / HealthPerTroop);
            if (casualties <= 0) return;

            casualties = Mathf.Min(casualties, troopCount);
            pendingDamage -= casualties * HealthPerTroop;
            troopCount -= casualties;
            AntPool.Instance?.LoseAssigned(casualties);
        }

        // 병력이 없으면 공격 대상이 될 수 없고 자동 교전도 하지 않는다.
        public override bool CanAttackTarget(IDamageable target) => !IsEmbarked && HasTroops && base.CanAttackTarget(target);

        public override void CommandAttack(IDamageable target)
        {
            if (!HasTroops) return;
            base.CommandAttack(target);
        }

        public override void CommandAttackMove(Vector3 destination)
        {
            if (IsEmbarked || !HasTroops) return;
            base.CommandAttackMove(destination);
        }

        public override void CommandGather(ResourceNode node)
        {
            if (IsEmbarked || !HasTroops) return;
            base.CommandGather(node);
        }

        public override void CommandBuild(BuildingConstructionSite site)
        {
            if (!HasTroops) return;
            base.CommandBuild(site);
        }

        // 마지막 일격을 넣은 장수만 경험치를 받는다. 공격 전에 살아 있던 대상이 이 타격으로 죽은 경우만 인정하므로
        // 이미 죽은 대상, 비살상 타격, 다른 주체가 죽인 대상에는 경험치가 붙지 않는다.
        protected override void DealDamage(IDamageable target)
        {
            var reward = CommanderProgression.KillXp(target);
            var wasAlive = !target.IsDead;
            // 강타는 살아 있는 대상에 대한 다음 실제 타격 한 번에만 소모된다.
            var multiplier = wasAlive && skills.ConsumePowerStrike() ? CommanderSkills.PowerStrikeMultiplier : 1f;
            target.TakeDamage(AttackDamage * multiplier);
            if (wasAlive && reward > 0 && IsKilled(target)) progression.AddXp(reward);
        }

        // 침공 개체처럼 처치와 동시에 Destroy되는 대상은 IsDead 조회가 불안전하므로 파괴 여부를 먼저 본다.
        private static bool IsKilled(IDamageable target)
        {
            return target is Object unityObject && unityObject == null || target.IsDead;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseTroopsOnce();
            skills.CancelEffects();
            // 비활성화·파괴(OnDisable 선행) 시 진행 중인 연구소 강화를 즉시 취소한다.
            if (LabUpgradeLab != null) LabUpgradeLab.CancelResearch();
        }

        private void OnDestroy()
        {
            ReleaseTroopsOnce();
            if (runtimeData != null) Destroy(runtimeData);
        }

        // 배속된 개미를 대기 풀로 정확히 한 번만 돌려준다.
        // 씬 teardown에서는 AntPool이 먼저 파괴될 수 있으므로 null이면 조용히 넘어간다.
        private void ReleaseTroopsOnce()
        {
            if (troopsReleased || troopCount <= 0) return;
            troopsReleased = true;
            if (AntPool.Instance != null) AntPool.Instance.ReturnAssigned(troopCount);
            troopCount = 0;
        }

        public void SetRoleProfiles(UnitData[] profiles)
        {
            roleProfiles = profiles;
            ApplyRoleProfile();
        }

        private void ApplyRoleProfile()
        {
            if (Data == null) return;
            if (roleProfiles != null)
                foreach (var profile in roleProfiles)
                    if (profile != null && profile.role == role)
                    {
                        Data.attackDamage = profile.attackDamage;
                        Data.attackRange = profile.attackRange;
                        Data.attackInterval = profile.attackInterval;
                        Data.moveSpeed = profile.moveSpeed;
                        Data.armor = profile.armor;
                        Data.gatherRate = profile.gatherRate;
                        Data.carryCapacity = profile.carryCapacity;
                        break;
                    }
            Data.role = role;
            if (Agent != null) Agent.speed = Data.moveSpeed;
        }
    }
}
