using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        internal Save.CommanderOrderDto CaptureOrder() => new Save.CommanderOrderDto {
            soldier = CaptureSoldierOrder(), worker = CaptureWorkerOrder(), service = Save.SaveReferences.Key(ServiceTarget),
            serviceJob = (int)ServiceJob, hunt = Save.SaveReferences.Key(HuntTarget), huntCooldown = huntCooldown,
            automaticFacility = Save.SaveReferences.Key(automaticFacility), workScan = workScan };
        internal void RestoreOrder(Save.CommanderOrderDto d)
        {
            if (d == null) return;
            RestoreWorkerOrder(d.worker);
            ServiceTarget = Save.SaveReferences.Get<BuildingBase>(d.service); ServiceJob = (CommanderJobs)d.serviceJob;
            HuntTarget = Save.SaveReferences.Get<WildMonster>(d.hunt); huntCooldown = d.huntCooldown;
            automaticFacility = Save.SaveReferences.Get<BuildingBase>(d.automaticFacility); workScan = d.workScan;
            if (ServiceTarget != null) SetWorkTarget(ServiceTarget);
            RestoreSoldierOrder(d.soldier);
        }

        public BuildingBase ServiceTarget { get; private set; }
        public CommanderJobs ServiceJob { get; private set; }
        public WildMonster HuntTarget { get; private set; }
        private float huntCooldown;
        private Workforce workTarget;
        public bool CivilianWorkReady => isActiveAndEnabled && !IsDead && !IsCaptive && !IsEmbarked && !Health.Downed && PersonalHealth > 0 && !IsDeployed && !IsAwayFromHome
            && !IsAsleep && (!IsSleepTime || WorkPriorities.Red) && !IsDeparting && !PersonalState.treating && PersonalState.mentalBreak == MentalBreak.None && PersonalState.rageRemaining <= 0;
        public Workforce WorkTarget => CivilianWorkReady && workTarget != null && workTarget.isActiveAndEnabled ? workTarget : null;
        public float WorkRate(CommanderActivity activity) => talents.Multiplier(activity) * WorkFactor * AgeWorkMultiplier(activity) * AntColony.Buildings.RoomSystem.WorkBonus(WorkTarget) * BiomeRules.WorkAt(AntColony.Buildings.RoomSystem.IsIndoors(Position)) * AntColony.Map.WeatherSystem.WorkAt(AntColony.Buildings.RoomSystem.IsIndoors(Position));
        public float LoadCapacity => Mathf.Max(0, TotalLoadCapacity - StartingLoadWeight - BossCargoCount);
        internal void SetWorkTarget(Component target)
        {
            workTarget = Workforce.For(target);
        }
        public override void CommandStop()
        {
            WorkState.deploymentMealSeconds = 0;
            WorkState.bossPickup = -1;
            ReleaseCorpse();
            ReleaseAnimalTask();
            EndMed(); EndBurial();
            ServiceTarget = null; ServiceJob = CommanderJobs.None; HuntTarget = null;
            SetWorkTarget(null);
            ScienceAssignment?.ReleaseResearcher(); CraftingWorkshop?.Release();
            automaticFacility = null;
            base.CommandStop();
        }
        public static CommanderActivity SkillFor(CommanderJobs job) => job switch
        {
            CommanderJobs.Building or CommanderJobs.Repair => CommanderActivity.Building,
            CommanderJobs.Crafting => CommanderActivity.Crafting, CommanderJobs.Research => CommanderActivity.Research,
            CommanderJobs.Farming => CommanderActivity.Farming, CommanderJobs.Fishing => CommanderActivity.Fishing,
            CommanderJobs.Nursing => CommanderActivity.Medicine, CommanderJobs.Cooking => CommanderActivity.Cooking,
            CommanderJobs.Firefighting => CommanderActivity.Firefighting,
            CommanderJobs.Art => CommanderActivity.Art, CommanderJobs.Hunting => CommanderActivity.Melee, CommanderJobs.Administration => CommanderActivity.Politics, CommanderJobs.Husbandry => CommanderActivity.Husbandry,
            _ => CommanderActivity.Gathering
        };
        public bool CanDoJob(CommanderJobs job) => !traits.Blocks(job) && !IsChild; // 어린 장수는 일하지 않는다(Phase 4)
        public bool StartService(BuildingBase target, CommanderJobs job)
        {
            var firefighting = job == CommanderJobs.Firefighting;
            if ((!firefighting && !CivilianWorkReady || firefighting && (IsDead || IsAwayFromHome || IsAsleep || IsCaptive || PersonalState.health.Downed)) || !CanReceiveOrders || IsWorking || LabUpgradeBusy || !CanDoJob(job)
                || Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == target)
                || target == null || target.IsDead || !target.isActiveAndEnabled || !target.CountsTowardPlayerDefeat || target is Processor processor && !processor.CanContinue(this)
                || job != CommanderJobs.Nursing && job != CommanderJobs.Repair && job != CommanderJobs.Cooking && !(firefighting && target.GetComponent<WallFire>()?.Burning == true) && !(job == CommanderJobs.Hauling && (target is PowerNode || target is SupplyFacility well && ManualWellNeedsWork(well))) && !(job == CommanderJobs.Administration && target is AdminDesk) && !(job == CommanderJobs.Crafting && target is Processor)
                || !TryWorkApproach(target.Position, out var approach)) return false;
            CommandStop(); ServiceTarget = target; ServiceJob = job;
            SetMoveDestination(approach); SetWorkTarget(target); return true;
        }
        public bool CommandExtinguish(WallFire fire)
        {
            if (fire == null || !fire.Burning || !CanReceiveOrders || IsAwayFromHome || !CanDoJob(CommanderJobs.Firefighting)
                || !TryWorkApproach(fire.transform.position, out _)) return false;
            CommandStop();
            return StartService(fire.GetComponent<BuildingBase>(), CommanderJobs.Firefighting);
        }
        private bool TickService(float seconds)
        {
            if (ServiceTarget == null) { if (ServiceJob != CommanderJobs.None) CommandStop(); return false; }
            bool firefighting = ServiceJob == CommanderJobs.Firefighting;
            if ((!firefighting && (!CivilianWorkReady || !AllowsJob(ServiceJob)) || firefighting && (!CanReceiveOrders || IsAwayFromHome || IsAsleep || PersonalState.health.Downed)) || !ServiceTarget.isActiveAndEnabled || ServiceTarget.IsDead) { CommandStop(); return false; }
            if (firefighting && ServiceTarget.GetComponent<WallFire>()?.Burning != true) { CommandStop(); return false; }
            if (ServiceTarget is SupplyFacility manual && !ManualWellNeedsWork(manual)) { CommandStop(); return false; }
            if (ServiceTarget is SupplyFacility && !CanReach(ServiceTarget.Position)) { CommandStop(); return false; }
            if ((Position - ServiceTarget.Position).sqrMagnitude > (firefighting ? 20.25f : 49)
                || ServiceTarget is SupplyFacility && !HasReachedDestination())
            {
                if (!TryWorkApproach(ServiceTarget.Position, out var approach)) { CommandStop(); return false; }
                if (IsFlying || Agent != null && !Agent.pathPending && !Agent.hasPath) SetMoveDestination(approach);
                return true;
            }
            StopMoving();
            if (firefighting)
            {
                var fire = ServiceTarget.GetComponent<WallFire>();
                GainExperience(CommanderActivity.Firefighting, seconds);
                if (!fire.Extinguish(seconds)) CommandStop();
            }
            else if (ServiceTarget is Processor bench) // 가공대(제작)·약제대(간호=의료, 2026-10-11)
            {
                if (ServiceJob != bench.Job || !bench.Work(this, seconds)) { CommandStop(); return false; }
            }
            else if (ServiceJob == CommanderJobs.Nursing)
            {
                var infirmary = ServiceTarget as Infirmary;
                if (infirmary == null || infirmary.Patients.Count == 0 || WorkPriorities.Red) { CommandStop(); return false; }
                GainExperience(CommanderActivity.Medicine, seconds);
            }
            else if (ServiceJob == CommanderJobs.Cooking)
            {
                if (!(ServiceTarget is Kitchen kitchen) || !kitchen.Work(this, seconds)) { CommandStop(); return false; }
            }
            else if (ServiceJob == CommanderJobs.Hauling) // 쳇바퀴 뛰기(운반 작업, 근력 경험치·피로↑)
            {
                if (ServiceTarget is SupplyFacility well)
                {
                    // 큰 틱도 한 배치까지만: 가득 찬 뒤 WorkManual의 바닥 반출 fallback을 실행하지 않는다.
                    var dt = Mathf.Min(seconds, StartSupplyRules.ManualWaterSeconds - well.Supply.work);
                    if (!well.WorkManual(dt)) { CommandStop(); return false; }
                    GainExperience(CommanderActivity.Gathering, dt);
                    if (!ManualWellNeedsWork(well)) CommandStop();
                    return true;
                }
                if (!(ServiceTarget is PowerNode wheel) || !wheel.Run(this, seconds)) { CommandStop(); return false; }
            }
            else if (ServiceJob == CommanderJobs.Administration) // 행정 책상에서 정치 업무(2026-10-08)
            {
                if (!(ServiceTarget is AdminDesk desk) || !desk.Work(this, seconds)) { CommandStop(); return false; }
            }
            else if (!BuildingRepair.For(ServiceTarget).Work(this, seconds)) { CommandStop(); return false; }
            return true;
        }
        public bool StartHunt(WildMonster target)
        {
            if (!CivilianWorkReady || !CanReceiveOrders || LabUpgradeBusy || IsWorking || !CanDoJob(CommanderJobs.Hunting)
                || target == null || !target.Huntable || !target.HuntDesignated || !TryWorkApproach(target.Position, out _)) return false;
            CommandStop(); HuntTarget = target; return true;
        }
        private bool TickHunt(float seconds)
        {
            if (HuntTarget == null) return false;
            var target = HuntTarget;
            if (!CivilianWorkReady || !target.Huntable || !target.HuntDesignated) { CommandStop(); return false; }
            if (GetDistanceTo(target.Position) > Data.attackRange)
            { if (TryWorkApproach(target.Position, out var approach)) SetMoveDestination(approach); else CommandStop(); return true; }
            StopMoving(); huntCooldown -= seconds;
            if (huntCooldown > 0) return true;
            huntCooldown = Data.attackInterval;
            // 사냥은 장수 개인 공격. 출전 병력/전투 스킬의 배율은 끌어오지 않는다.
            target.TakeDamage(Mathf.Max(1, Data.attackDamage + talents.Level(CombatActivity) + traits.AttackBonus + EquipmentBonus(EquipmentSlot.Weapon)));
            if (target.IsDead) { GainExperience(CombatActivity, 25); CommandStop(); }
            return true;
        }
    }
}
