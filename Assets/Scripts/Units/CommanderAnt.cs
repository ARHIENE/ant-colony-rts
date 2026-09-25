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
    public partial class CommanderAnt : WorkerAnt
    {
        // 기획 고정값: 일반개미 1마리가 HP 1이다. UnitData.maxHealth는 장수 부대 체력에 쓰지 않는다.
        public const float HealthPerTroop = 1f;
        // ponytail: 지원 수치는 1차 프로토타입 상수다. 밸런스가 확정되면 데이터 에셋으로 옮긴다.
        public const float SupportAuraRadius = 6f;
        public const float SupportAttackBonus = 1f;
        public const float SupportArmorBonus = 1f;

        [SerializeField] private string commanderName = "Commander";
        [SerializeField, Min(0)] private int troopCount;
        [SerializeField] private UnitData[] roleProfiles;
        [SerializeField] private CommanderTalents talents = new CommanderTalents();
        [SerializeField] private CommanderTraits traits = new CommanderTraits();
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
        public AnnexedSettlement Garrison { get; internal set; }
        public ExpeditionSite Captor { get; internal set; }
        public bool IsCaptive => Captor != null;
        public bool IsAwayFromHome => Transport != null || Garrison != null || IsCaptive;
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
            GetComponent<SelectableObject>().enabled = !embarked && !IsDeparting;
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
            TickAdvancedSkills(Time.deltaTime);
            if (IsHostile) { TickDeparture(Time.deltaTime); return; }
            TickPersonal(Time.deltaTime);
            if (IsDeparting || Social.diving) return;
            if (!IsEmbarked && !IsDead && PersonalState.rageRemaining > 0) { TickRevenge(Time.deltaTime); return; }
            if (!IsEmbarked && !IsDead && !PersonalState.treating && PersonalState.mentalBreak == MentalBreak.None && !LabUpgradeBusy) base.Update();
        }

        public override void CommandMove(Vector3 destination)
        {
            if (CanReceiveOrders && !LabUpgradeBusy) base.CommandMove(destination);
        }

        public string CommanderName => commanderName;
        // 기존 프로필·훈련장 에셋의 키만 재사용한다. 장수 보직은 장비로 대체한다.
        public UnitRole Role => Weapon == null ? UnitRole.Melee : Weapon.weapon == WeaponKind.AcidSprayer ? UnitRole.Ranged
            : Weapon.weapon == WeaponKind.Shield ? UnitRole.Defense : Weapon.weapon == WeaponKind.Pheromone ? UnitRole.Support : UnitRole.Melee;
        public override bool IsFlying => EquippedArmor?.armor == ArmorKind.Wings && Social.groundedRemaining <= 0;

        public int TroopCount => troopCount;
        public int CommandLimit => Mathf.Max(1, Mathf.FloorToInt((10 + 2 * talents.Level(CommanderActivity.Command))
            * (1f - .2f * PersonalState.Severity(InjuryPart.Thorax))) + (int)TrinketBonus(TrinketEffect.Command));
        public int FreeRanks => Mathf.Max(0, CommandLimit - troopCount);
        public bool HasTroops => troopCount > 0;

        public float MaxHealth => troopCount * HealthPerTroop;
        public override float CurrentHealth => Mathf.Max(0f, MaxHealth - pendingDamage);

        // 병력 0의 부상·사망 판정은 OnDowned에서 새 게임 사망 옵션을 따른다.
        public override bool IsDead => PersonalState.dead;

        public CommanderTalents Talents => talents;
        internal void RestoreTalents(CommanderTalents value) => talents = value.Copy();
        public CommanderTraits Traits => traits;
        public int LabAttackLevel => labAttackLevel;
        public int LabArmorLevel => labArmorLevel;
        // 한 장수는 동시에 한 연구소에서만 강화된다. 연구소가 시작·해제한다.
        public ResearchLab LabUpgradeLab { get; set; }
        public ScienceLab ScienceAssignment { get; internal set; }
        public bool LabUpgradeBusy => LabUpgradeLab != null || ScienceAssignment != null;

        public CommanderSkills Skills => skills;

        // 수동 시전. 비활성·병력 0이면 거부하고, 레벨·중복·재사용 대기는 CommanderSkills가 판정한다.
        public bool CanPowerStrike => CanReceiveOrders && HasTroops && Role == UnitRole.Melee && skills.CanArmPowerStrike(talents.Level(CommanderActivity.Melee));
        public bool CanDefensiveStance => CanReceiveOrders && HasTroops && Role == UnitRole.Defense && skills.CanStartDefensiveStance(talents.Level(CommanderActivity.Melee));
        public bool TryPowerStrike() => CanPowerStrike && skills.TryArmPowerStrike(talents.Level(CommanderActivity.Melee), 1f + .5f * PersonalState.Severity(InjuryPart.Head));
        public bool TryDefensiveStance() => CanDefensiveStance && skills.TryStartDefensiveStance(talents.Level(CommanderActivity.Melee), 1f + .5f * PersonalState.Severity(InjuryPart.Head));

        public void CompleteLabUpgrade(bool attack, int maxLevel)
        {
            if (attack) labAttackLevel = Mathf.Min(maxLevel, labAttackLevel + 1);
            else labArmorLevel = Mathf.Min(maxLevel, labArmorLevel + 1);
        }

        // 병력 수만큼 부대 전체의 전투력/채집량이 늘어난다.
        // 레벨 공격 보너스는 기존 공격력과 같이 1마리분에 더해진 뒤 병력 수만큼 곱해진다.
        // 신중형 성격은 1마리분 공격력을 깎으므로 0 밑으로 내려가지 않게 막는다(병력 수를 곱하면 부호가 증폭된다).
        public override float AttackDamage => Mathf.Max(1f, base.AttackDamage + (Role == UnitRole.Support ? 0 : talents.Level(CombatActivity))
            + traits.AttackBonus + labAttackLevel * LabAttackBonusPerLevel
            + EquipmentBonus(EquipmentSlot.Weapon) + (HasNearbyFriend ? 1 : 0)
            + (HasSupportAura ? SupportAttackBonus : 0f)) * troopCount * (1f - .25f * PersonalState.Severity(InjuryPart.Mandible)) * (PersonalState.rageRemaining > 0 ? 1.3f : 1f);
        public override float Armor => base.Armor + (Role == UnitRole.Melee ? .5f * talents.Level(CommanderActivity.Melee) : Role == UnitRole.Defense ? talents.Level(CommanderActivity.Melee) : 0) + traits.ArmorBonus
            + labArmorLevel * LabArmorBonusPerLevel + (HasSupportAura ? SupportArmorBonus : 0f)
            + EquipmentBonus(EquipmentSlot.Armor) - 2f * PersonalState.Severity(InjuryPart.Thorax)
            + (skills.DefensiveStanceActive ? CommanderSkills.DefensiveStanceArmor : 0f);
        protected override float GatherRate => base.GatherRate * Mathf.Max(1, troopCount) * talents.Multiplier(CurrentActivity) * traits.WorkMultiplier * (1f - .3f * PersonalState.Severity(InjuryPart.Antenna)) * (1f + TrinketBonus(TrinketEffect.Gather)) * ColonyEvents.GatherMultiplier(this);
        protected override void OnGathered(float amount)
        {
            if (CurrentActivity == CommanderActivity.Gathering) GainExperience(CommanderActivity.Gathering, amount);
        }
        protected override void OnWorked(float seconds)
        {
            if (CurrentActivity != CommanderActivity.Gathering) GainExperience(CurrentActivity, seconds);
        }
        public void GainExperience(CommanderActivity activity, float amount) => talents.Add(activity,
            amount * traits.GrowthMultiplier(activity) * (1f - .5f * PersonalState.Severity(InjuryPart.Head)));
        protected override float CarryCapacity => base.CarryCapacity * Mathf.Max(1, troopCount);

        // 중첩 없이 가장 가까운 지원 장수 한 명만 확인한다. 현재 장수 12명 규모에서는 선형 검색이 가장 단순하다.
        public bool HasSupportAura
        {
            get
            {
                foreach (var unit in Active)
                    if (unit is CommanderAnt support && support != this && support.HasTroops
                        && support.Role == UnitRole.Support && support.IsHostile == IsHostile
                        && !support.IsEmbarked && !support.IsDead && (support.Position - Position).sqrMagnitude <= support.AuraRadius * support.AuraRadius)
                        return true;
                return false;
            }
        }

        // 운반 중이거나 건설 중에는 배정/회수/보직 변경을 막는다. 중간에 인원이 바뀌면 자원이 증발한다.
        public bool CanChangeAllocation => CanReceiveOrders && !IsCarrying && !IsConstructing && !LabUpgradeBusy;

        public override bool CanStartConstruction => CanChangeAllocation && !IsAwayFromHome && HasTroops && base.CanStartConstruction;

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
            // 구 생성 도구의 보직은 최초 장비로만 이관. 관직·허용 보직은 무시한다.
            personalState.equipment.Clear();
            if (startRole == UnitRole.Flying) personalState.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Armor, armor = ArmorKind.Wings, quality = 1 });
            else if (startRole != UnitRole.Worker) personalState.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Weapon, quality = 1,
                weapon = startRole == UnitRole.Ranged ? WeaponKind.AcidSprayer : startRole == UnitRole.Defense ? WeaponKind.Shield
                    : startRole == UnitRole.Support ? WeaponKind.Pheromone : WeaponKind.Mandible });
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
            if (count <= 0 || IsAwayFromHome || !isActiveAndEnabled || !CanChangeAllocation || AntPool.Instance == null) return false;
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
            if (count <= 0 || IsAwayFromHome || !CanChangeAllocation || AntPool.Instance == null) return 0;
            // 부상 중인 마지막 1마리를 건강한 대기 인력으로 넘겨 회복시키지 않는다.
            count = Mathf.Min(count, troopCount - (pendingDamage > 0f ? 1 : 0));
            if (count <= 0) return 0;
            AntPool.Instance.ReturnAssigned(count);
            OnTroopsRecalled(count, troopCount);
            troopCount -= count;
            if (troopCount == 0) CommandStop();
            return count;
        }

        // 저장 복원 전용. 병력 총량은 AntPool.RestoreCounts가 따로 맞추므로 여기서 풀을 건드리지 않는다.
        internal void RestoreTroops(int count, float savedPendingDamage)
        {
            troopCount = Mathf.Max(0, count);
            pendingDamage = Mathf.Max(0f, savedPendingDamage);
            troopsReleased = false;
        }

        internal void RestoreLabLevels(int attack, int armor)
        {
            labAttackLevel = Mathf.Max(0, attack);
            labArmorLevel = Mathf.Max(0, armor);
        }

        // 피해는 부대 전체가 나눠 받고, HP 1이 쌓일 때마다 병력이 실제로 줄어든다(환급 없음).
        public override void TakeDamage(float amount)
        {
            if (IsDead || IsEmbarked || troopCount <= 0 || float.IsNaN(amount) || amount <= 0f) return;
            Social.combatSeen = true;
            PersonalState.lastCombatSeconds = 0;
            GetComponent<AntVisual>()?.Action("Hit");

            var applied = Mathf.Max(1f, amount - Armor);

            // 즉사급/무한대 피해는 루프를 돌리지 않고 남은 병력만큼만 한 번에 처리한다.
            if (float.IsInfinity(applied) || applied >= CurrentHealth)
            {
                if (!IsDeparting) AntPool.Instance?.LoseAssigned(troopCount);
                troopCount = 0;
                pendingDamage = 0f;
                CommandStop();
                if (IsHostile) CaptureDeparting(); else OnDowned();
                return;
            }

            pendingDamage += applied;
            var casualties = Mathf.FloorToInt(pendingDamage / HealthPerTroop);
            if (casualties <= 0) return;

            casualties = Mathf.Min(casualties, troopCount);
            pendingDamage -= casualties * HealthPerTroop;
            troopCount -= casualties;
            if (!IsDeparting) AntPool.Instance?.LoseAssigned(casualties);
        }

        // 병력이 없으면 공격 대상이 될 수 없고 자동 교전도 하지 않는다.
        public override bool CanAttackTarget(IDamageable target) => CanReceiveOrders && !LabUpgradeBusy && HasTroops && base.CanAttackTarget(target);

        public override void CommandAttack(IDamageable target)
        {
            if (!CanReceiveOrders || LabUpgradeBusy || !HasTroops) return;
            base.CommandAttack(target);
        }

        public override void CommandAttackMove(Vector3 destination)
        {
            if (!CanReceiveOrders || LabUpgradeBusy || !HasTroops) return;
            base.CommandAttackMove(destination);
        }

        public override void CommandGather(ResourceNode node)
        {
            if (!CanReceiveOrders || LabUpgradeBusy || !HasTroops) return;
            if (Garrison != null && Garrison.DockedTransport == null) return;
            base.CommandGather(node);
        }

        public override void CommandBuild(BuildingConstructionSite site)
        {
            if (!CanReceiveOrders || LabUpgradeBusy || !HasTroops) return;
            base.CommandBuild(site);
        }

        // 마지막 일격을 넣은 장수만 경험치를 받는다. 공격 전에 살아 있던 대상이 이 타격으로 죽은 경우만 인정하므로
        // 이미 죽은 대상, 비살상 타격, 다른 주체가 죽인 대상에는 경험치가 붙지 않는다.
        protected override void DealDamage(IDamageable target)
        {
            Social.combatSeen = true;
            PersonalState.lastCombatSeconds = 0;
            var reward = CommanderProgression.KillXp(target);
            var wasAlive = !target.IsDead;
            // 강타는 살아 있는 대상에 대한 다음 실제 타격 한 번에만 소모된다.
            var multiplier = wasAlive && skills.ConsumePowerStrike() ? CommanderSkills.PowerStrikeMultiplier : 1f;
            target.TakeDamage(AttackDamage * multiplier);
            if (wasAlive && reward > 0 && IsKilled(target))
            {
                GainExperience(CombatActivity, reward);
                if (target is AntColony.Boss.BossHealth && Random.value < .2f) ShiftEventTrait(CommanderTrait.Coward, true);
            }
        }

        // 침공 개체처럼 처치와 동시에 Destroy되는 대상은 IsDead 조회가 불안전하므로 파괴 여부를 먼저 본다.
        private static bool IsKilled(IDamageable target)
        {
            return target is Object unityObject && unityObject == null || target.IsDead;
        }

        protected override void OnDisable()
        {
            if (acidVisual != null) Destroy(acidVisual);
            CraftingWorkshop?.Release();
            TreatmentFacility?.Release(this);
            if (Garrison != null) Garrison.Remove(this);
            base.OnDisable();
            ReleaseTroopsOnce();
            skills.CancelEffects();
            // 비활성화·파괴(OnDisable 선행) 시 진행 중인 연구소 강화를 즉시 취소한다.
            if (LabUpgradeLab != null) LabUpgradeLab.CancelResearch();
            ScienceAssignment?.ReleaseResearcher();
        }

        private void OnDestroy()
        {
            if (acidVisual != null) Destroy(acidVisual);
            ReleaseTroopsOnce();
            if (runtimeData != null) Destroy(runtimeData);
        }

        // 배속된 개미를 대기 풀로 정확히 한 번만 돌려준다.
        // 씬 teardown에서는 AntPool이 먼저 파괴될 수 있으므로 null이면 조용히 넘어간다.
        private void ReleaseTroopsOnce()
        {
            if (troopsReleased || troopCount <= 0 || IsDeparting) return;
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
                    if (profile != null && profile.role == Role)
                    {
                        Data.attackDamage = profile.attackDamage;
                        Data.attackRange = profile.attackRange;
                        Data.attackInterval = profile.attackInterval;
                        Data.moveSpeed = profile.moveSpeed;
                        Data.armor = profile.armor;
                        break;
                    }
            if (roleProfiles != null)
                foreach (var profile in roleProfiles)
                    if (profile != null && profile.role == UnitRole.Worker)
                    { Data.gatherRate = profile.gatherRate; Data.carryCapacity = profile.carryCapacity; break; }
            Data.role = Role;
            if (Role == UnitRole.Ranged) Data.attackRange = 6f;
            if (Agent != null) Agent.speed = Data.moveSpeed;
        }
    }
}
