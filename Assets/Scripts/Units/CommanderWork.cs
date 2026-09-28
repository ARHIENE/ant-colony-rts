using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public BuildingBase ServiceTarget { get; private set; }
        public CommanderJobs ServiceJob { get; private set; }
        public WildMonster HuntTarget { get; private set; }
        private float huntCooldown;
        private Workforce workTarget;
        public bool CivilianWorkReady => isActiveAndEnabled && !IsDead && PersonalHealth > 0 && !IsDeployed && !IsAwayFromHome
            && !IsAsleep && !IsSleepTime && !IsDeparting && !PersonalState.treating && PersonalState.mentalBreak == MentalBreak.None && PersonalState.rageRemaining <= 0;
        public Workforce WorkTarget => CivilianWorkReady && workTarget != null && workTarget.isActiveAndEnabled ? workTarget : null;
        public int WorkforceLimit => GameBalance.WorkforceBase + GameBalance.WorkforcePerSkill * talents.Level(CurrentActivity);
        public int WorkingAnts { get { WorkTarget?.Refresh(); return WorkTarget != null ? WorkTarget.Allocated : 0; } }
        public float WorkforceMultiplier => 1 + WorkingAnts * GameBalance.WorkforcePerAnt;
        public float WorkRate(CommanderActivity activity) => talents.Multiplier(activity) * WorkFactor * WorkforceMultiplier;
        public float LoadCapacity => (GameBalance.CarryBase + talents.Level(CommanderActivity.Strength) * GameBalance.CarryPerStrength)
            * traits.CarryMultiplier + WorkingAnts * GameBalance.CarryPerAnt;
        internal void SetWorkTarget(Component target)
        {
            var old = workTarget;
            workTarget = Workforce.For(target);
            if (old != null && old != workTarget) old.Refresh();
            workTarget?.Refresh();
        }
        public override void CommandStop()
        {
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
            CommanderJobs.Art => CommanderActivity.Art, CommanderJobs.Hunting => CommanderActivity.Melee,
            _ => CommanderActivity.Gathering
        };
        public bool CanDoJob(CommanderJobs job) => !traits.Blocks(job);
        public bool StartService(BuildingBase target, CommanderJobs job)
        {
            if (!CivilianWorkReady || !CanReceiveOrders || IsWorking || LabUpgradeBusy || !CanDoJob(job)
                || Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == target)
                || target == null || target.IsDead || !target.isActiveAndEnabled || !target.CountsTowardPlayerDefeat
                || job != CommanderJobs.Nursing && job != CommanderJobs.Repair && job != CommanderJobs.Cooking
                || !TryWorkApproach(target.Position, out var approach)) return false;
            CommandStop(); ServiceTarget = target; ServiceJob = job;
            SetMoveDestination(approach); SetWorkTarget(target); return true;
        }
        private bool TickService(float seconds)
        {
            if (ServiceTarget == null) { if (ServiceJob != CommanderJobs.None) CommandStop(); return false; }
            if (!CivilianWorkReady || !ServiceTarget.isActiveAndEnabled || ServiceTarget.IsDead) { CommandStop(); return false; }
            if ((Position - ServiceTarget.Position).sqrMagnitude > 49) return true;
            StopMoving();
            if (ServiceJob == CommanderJobs.Nursing)
            {
                var infirmary = ServiceTarget as Infirmary;
                if (infirmary == null || infirmary.Patients.Count == 0) { CommandStop(); return false; }
                GainExperience(CommanderActivity.Medicine, seconds);
            }
            else if (ServiceJob == CommanderJobs.Cooking)
            {
                if (!(ServiceTarget is Kitchen kitchen) || !kitchen.Work(this, seconds)) { CommandStop(); return false; }
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
