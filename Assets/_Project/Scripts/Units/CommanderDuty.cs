using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // 새 작업은 뒤 비트에 붙여 기존 저장 번호를 유지한다.
    [Flags] public enum CommanderJobs
    {
        None = 0, Building = 1, Crafting = 2, Research = 4, Farming = 8, Fishing = 16, Gathering = 32,
        Nursing = 64, Repair = 128, Hauling = 256, Hunting = 512, Cooking = 1024, Art = 2048,
        Cleaning = 4096,
        Legacy = 63, Added = Nursing | Repair | Hauling | Hunting | Cooking | Art, All = 8191
    }
    public enum CommanderDuty { Civilian, Deployed, Returning }

    [Serializable] public class CommanderWorkState
    {
        public CommanderJobs jobs = CommanderJobs.All;
        public CommanderDuty duty;
        public float health = GameBalance.CommanderHealth, quietSeconds, recoverySeconds;
        public bool resting; // 휴식 지시: 피로가 풀릴 때까지 자율 작업을 쉰다.
        public Vector3 returnPosition;
        public bool Valid => (jobs & ~CommanderJobs.All) == 0 && Enum.IsDefined(typeof(CommanderDuty), duty)
            && Finite(health) && health >= 0 && health <= GameBalance.CommanderHealth
            && Finite(quietSeconds) && quietSeconds >= 0 && quietSeconds <= GameBalance.AutoReturnSeconds
            && Finite(recoverySeconds) && recoverySeconds >= 0 && recoverySeconds <= GameBalance.CommanderRecoverySeconds
            && Finite(returnPosition.x) && Finite(returnPosition.y) && Finite(returnPosition.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public partial class CommanderAnt
    {
        private float workScan;
        private BuildingBase automaticFacility;
        public CommanderWorkState WorkState => PersonalState.work;
        public bool IsDeployed => WorkState.duty != CommanderDuty.Civilian;
        public bool IsReturning => WorkState.duty == CommanderDuty.Returning;
        internal bool CanResumeDutyAfterLoad => !IsAwayFromHome && (IsReturning || ServiceTarget != null || CorpseTarget != null || automaticFacility != null || CurrentResourceNode != null || IsCarrying);
        public float PersonalHealth => WorkState.health;
        public float TroopHealth => Mathf.Max(0, troopCount - pendingDamage);
        internal float PendingTroopDamage => pendingDamage;
        public bool AllowsJob(CommanderJobs job) => (WorkState.jobs & job) == job && CanDoJob(job);
        public bool SetJobEnabled(CommanderJobs job, bool enabled)
        {
            if (job == CommanderJobs.None || (job & ~CommanderJobs.All) != 0 || enabled && !CanDoJob(job)) return false;
            WorkState.jobs = enabled ? WorkState.jobs | job : WorkState.jobs & ~job;
            return true; // Already started work finishes, including delivery of its cargo.
        }

        internal bool CanMobilize => isActiveAndEnabled && !IsDead && PersonalHealth > 0 && !IsDeparting
            && PersonalState.mentalBreak == MentalBreak.None && !PersonalState.treating && PersonalState.rageRemaining <= 0
            && !traits.Has(CommanderTrait.Pacifist) && !IsChild && !IsAwayFromHome && !IsDeployed && troopCount == 0 && !Social.diving && LabUpgradeLab == null;
        internal void Mobilize(int troops, Vector3 home)
        {
            ScienceAssignment?.ReleaseResearcher();
            CraftingWorkshop?.Release();
            SuspendWork(); automaticFacility = null;
            WorkState.resting = false; WakeForDuty();
            troopCount = troops; troopsReleased = false;
            WorkState.duty = CommanderDuty.Deployed; WorkState.returnPosition = home; WorkState.quietSeconds = 0;
        }
        public bool ReturnToPost()
        {
            if (!IsDeployed || IsAwayFromHome || !CanReceiveOrders || !CanReach(WorkState.returnPosition)) return false;
            CommandStop(); WorkState.duty = CommanderDuty.Returning;
            SetMoveDestination(WorkState.returnPosition); return true;
        }
        private void FinishReturn()
        {
            // Partial troop damage remains attached to the commander, preventing return/relaunch healing.
            AntPool.Instance?.ReturnAssigned(troopCount); troopCount = 0;
            WorkState.duty = CommanderDuty.Civilian; WorkState.quietSeconds = 0;
            CommandStop();
        }
        public void TickDuty(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead || IsHostile || AntColony.Save.SaveSystem.Busy) return;
            if (WorkState.recoverySeconds > 0)
            {
                WorkState.recoverySeconds = Mathf.Max(0, WorkState.recoverySeconds - seconds);
                if (WorkState.recoverySeconds == 0) WorkState.health = GameBalance.CommanderHealth;
                return;
            }
            if (TickSleep(seconds) || TickMeal(seconds) || TickCorpseWork(seconds) || TickHygiene(seconds) || TickJoy(seconds)) return;
            if (IsAwayFromHome || !CanReceiveOrders) return;
            if (IsReturning)
            {
                if (GetDistanceTo(WorkState.returnPosition) <= 2) FinishReturn();
                else if ((IsFlying || !Agent.pathPending && !Agent.hasPath) && CanReach(WorkState.returnPosition)) SetMoveDestination(WorkState.returnPosition);
                return;
            }
            if (IsDeployed)
            {
                WorkState.quietSeconds = IsInCombat || CombatTargeting.FindNearestEnemy(Position, GameBalance.ReturnEnemyRadius, Role) != null
                    ? 0 : Mathf.Min(GameBalance.AutoReturnSeconds, WorkState.quietSeconds + seconds);
                if (WorkState.quietSeconds >= GameBalance.AutoReturnSeconds) ReturnToPost();
                return;
            }
            if (TickService(seconds) || TickHunt(seconds)) return;
            if (!IsWorking && ScienceAssignment == null && CraftingWorkshop == null) SetWorkTarget(null);
            if (ScienceAssignment != null)
            {
                if (CampaignResearch.Instance?.Active == null || !AllowsJob(CommanderJobs.Research)) ScienceAssignment.ReleaseResearcher();
                else return;
            }
            if (WorkState.resting && (IsWorking || !IsFatigued)) WorkState.resting = false;
            if (LabUpgradeBusy || IsWorking || IsInCombat) return;
            if (CarriedAmount >= 1)
            {
                if (ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(CarriedType) < ResourceManager.Instance.GetCapacity(CarriedType)) ReturnCargoToStorage();
                return;
            }
            if (automaticFacility != null)
            {
                if (!automaticFacility.isActiveAndEnabled || automaticFacility.IsDead) automaticFacility = null;
                else if ((automaticFacility.Position - Position).sqrMagnitude <= 49)
                {
                    base.CommandStop();
                    if (automaticFacility is Workshop workshop && AllowsJob(CommanderJobs.Crafting)) workshop.TryAssign(this);
                    if (automaticFacility is ScienceLab lab && AllowsJob(CommanderJobs.Research)) lab.TryAssign(this);
                    if (automaticFacility is Infirmary infirmary) infirmary.TryAdmit(this);
                    automaticFacility = null; return;
                }
            }
            if (IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath) return;
            if (WorkState.resting) return;
            workScan -= seconds; if (workScan > 0) return; workScan = GameBalance.WorkScanSeconds;
            // ponytail: one scan per second for the small commander roster; index jobs if profiling shows contention.
            if (AllowsJob(CommanderJobs.Nursing))
                foreach (var hospital in FindObjectsByType<Infirmary>(FindObjectsSortMode.None).Where(h => h.Patients.Count > 0 && h.Nurse == null))
                    if (StartService(hospital, CommanderJobs.Nursing)) return;
            if (AllowsJob(CommanderJobs.Repair))
                foreach (var building in FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(BuildingRepair.Needed).OrderBy(b => (b.Position - Position).sqrMagnitude))
                    if (!Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == building) && StartService(building, CommanderJobs.Repair)) return;
            if (AllowsJob(CommanderJobs.Cleaning) && FindCorpseWork(false)) return;
            if (AllowsJob(CommanderJobs.Building) || AllowsJob(CommanderJobs.Art))
                foreach (var site in FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None).OrderBy(s => (s.Position - Position).sqrMagnitude))
                    if (!site.HasBuilder && AllowsJob(site.IsArt ? CommanderJobs.Art : CommanderJobs.Building) && CanReach(site.Position)) { CommandBuild(site); return; }
            if (AllowsJob(CommanderJobs.Crafting))
                foreach (var shop in FindObjectsByType<Workshop>(FindObjectsSortMode.None).OrderBy(s => (s.Position - Position).sqrMagnitude))
                    if (!shop.Ruined && !shop.IsDead && shop.Crafter == null && shop.Jobs.Count > 0 && GoToFacility(shop)) return;
            if (AllowsJob(CommanderJobs.Research) && CampaignResearch.Instance?.Active != null)
                foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None).OrderBy(s => (s.Position - Position).sqrMagnitude))
                    if (!lab.IsDead && !lab.Busy && lab.Target == null && lab.Tier >= CampaignResearch.Instance.Active.Tier && GoToFacility(lab)) return;
            if (AllowsJob(CommanderJobs.Cooking))
                foreach (var kitchen in FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Where(k => k.NeedsCook))
                    if (!Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == kitchen) && StartService(kitchen, CommanderJobs.Cooking)) return;
            if (AllowsJob(CommanderJobs.Hunting))
                foreach (var animal in WildMonster.All.Where(m => m.Huntable && m.HuntDesignated).OrderBy(m => (m.Position - Position).sqrMagnitude))
                    if (!Active.OfType<CommanderAnt>().Any(c => c != this && c.HuntTarget == animal) && StartHunt(animal)) return;
            if (AllowsJob(CommanderJobs.Hauling))
                foreach (var wheel in PowerNode.All.Where(p => p.NeedsRunner).OrderBy(p => (p.Position - Position).sqrMagnitude))
                    if (StartService(wheel, CommanderJobs.Hauling)) return;
            foreach (var job in new[] { CommanderJobs.Hauling, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering })
                if (AllowsJob(job))
                    foreach (var node in ResourceNode.Available.OrderBy(n => (n.transform.position - Position).sqrMagnitude))
                        if (node.CanGather && (CarriedAmount == 0 || CarriedType == node.ResourceType)
                            && ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(node.ResourceType) < ResourceManager.Instance.GetCapacity(node.ResourceType)
                            && node.GetComponentInParent<ExpeditionSite>() == null && !node.IsRaidLoot && JobFor(node) == job
                            && TryWorkApproach(node.transform.position, out _)) { CommandGather(node); return; }
        }
        public bool IsFatigued => Fatigue >= GameBalance.TiredFatigue;
        private bool CanTakeCivilianOrder => CanReceiveOrders && !IsDeployed && !IsAwayFromHome && !LabUpgradeBusy;
        public bool CanRest => CanTakeCivilianOrder && IsFatigued && !WorkState.resting;
        // 휴식 보내기: 배정된 숙소로 가서(없거나 갈 수 없으면 제자리) 피로가 풀릴 때까지 자율 작업을 멈춘다.
        public bool SendToRest()
        {
            if (!CanRest) return false;
            CommandStop(); ScienceAssignment?.ReleaseResearcher();
            var room = Dormitory.Assign(this);
            if (room != null && UnityEngine.AI.NavMesh.SamplePosition(room.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position)) base.CommandMove(hit.position);
            WorkState.resting = true; return true;
        }
        public Infirmary TreatmentTarget => FindObjectsByType<Infirmary>(FindObjectsSortMode.None)
            .Where(i => Infirmary.Unlocked && i.isActiveAndEnabled && !i.IsDead && i.Patients.Count < Infirmary.Capacity)
            .OrderBy(i => (i.Position - Position).sqrMagnitude).FirstOrDefault();
        public bool CanSendToTreatment => CanTakeCivilianOrder && PersonalState.NeedsTreatment && TreatmentFacility == null && TreatmentTarget != null;
        // 치료 보내기: 빈 침상이 있는 가장 가까운 의무실로 가서 입원한다(기존 자동 시설 이동을 재사용).
        public bool SendToTreatment()
        {
            if (!CanSendToTreatment) return false;
            var infirmary = TreatmentTarget; CommandStop(); ScienceAssignment?.ReleaseResearcher(); WorkState.resting = false;
            if ((infirmary.Position - Position).sqrMagnitude <= 49) return infirmary.TryAdmit(this);
            if (!UnityEngine.AI.NavMesh.SamplePosition(infirmary.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) || !CanReach(hit.position)) return false;
            automaticFacility = infirmary; base.CommandMove(hit.position); return true;
        }
        private bool GoToFacility(BuildingBase facility)
        {
            if (Active.OfType<CommanderAnt>().Any(c => c != this && c.automaticFacility == facility)) return false;
            if ((Position - facility.Position).sqrMagnitude <= 49)
                return facility is Workshop shop ? shop.TryAssign(this) : ((ScienceLab)facility).TryAssign(this);
            if (!UnityEngine.AI.NavMesh.SamplePosition(facility.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) || !CanReach(hit.position)) return false;
            automaticFacility = facility; base.CommandMove(hit.position); return true;
        }
        public static CommanderJobs JobFor(ResourceNode node) => node.IsLooseCargo ? CommanderJobs.Hauling : node.RequiresFishing ? CommanderJobs.Fishing
            : node.GetComponent<BuildingBase>() != null ? CommanderJobs.Farming : CommanderJobs.Gathering;
    }
}
