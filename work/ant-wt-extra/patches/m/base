using System;
using System.Collections.Generic;
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
        Cleaning = 4096, Administration = 8192, Husbandry = 16384, Firefighting = 32768,
        Legacy = 63, Added = Nursing | Repair | Hauling | Hunting | Cooking | Art, All = 65535
    }
    public enum CommanderDuty { Civilian, Deployed, Returning }

    [Serializable] public class CommanderWorkState
    {
        public CommanderJobs jobs = CommanderJobs.All;
        public CommanderDuty duty;
        public float health = GameBalance.CommanderHealth, quietSeconds, recoverySeconds;
        public float unconsciousSeconds, deploymentMealSeconds;
        public bool holdDeployment;
        public int bossPickup = -1; // 보스 직접 회수 지시. 화물 자체는 CampaignResearch의 carrier로 저장한다.
        public float sampleWork; // 표본 1개를 회수하기 전의 소수 작업량. 기존 개인 JSON에 포함한다.
        public FoodLots rations = new FoodLots();
        public bool resting; // 휴식 지시: 피로가 풀릴 때까지 자율 작업을 쉰다.
        // 작업표 비트 순서. 옛 15칸은 보존하고 소화만 기본 9로 추가한다.
        public int[] priorities = new int[16];
        public const int JobCount = 16, MaxPriority = 9, DefaultPriority = 5;
        public void MigrateLegacyJobs()
        {
            if (priorities != null && priorities.Length == JobCount)
            {
                return;
            }
            if (priorities != null && priorities.Length != 0 && priorities.Length != 15) return;
            var previous = priorities;
            priorities = new int[JobCount];
            if (previous != null && previous.Length == 15) Array.Copy(previous, priorities, 15);
            else for (var i = 0; i < 15; i++) priorities[i] = ((int)jobs & 1 << i) != 0 ? DefaultPriority : 0;
            jobs |= CommanderJobs.Firefighting;
            priorities[15] = MaxPriority;
        }
        public static int Index(CommanderJobs job) { for (int i = 0; i < JobCount; i++) if ((int)job == 1 << i) return i; return -1; }
        // jobs 비트가 켬/금지의 기준이다. SetJobEnabled로 켜진 칸은 보통, 꺼진 칸은 금지로 맞춘다.
        public int Priority(CommanderJobs job)
        {
            int i = Index(job); if (i < 0) return 0;
            if (priorities != null && priorities.Length == 15) MigrateLegacyJobs();
            if (priorities == null || priorities.Length != JobCount) priorities = new int[JobCount];
            bool on = (jobs & job) == job;
            if (!on) priorities[i] = 0; else if (priorities[i] <= 0 || priorities[i] > MaxPriority) priorities[i] = job == CommanderJobs.Firefighting ? MaxPriority : DefaultPriority;
            return priorities[i];
        }
        public Vector3 returnPosition;
        public bool Valid { get { MigrateLegacyJobs(); return (jobs & ~CommanderJobs.All) == 0 && (priorities == null || priorities.Length == 0 || (priorities.Length == 15 || priorities.Length == JobCount) && Array.TrueForAll(priorities, p => p >= 0 && p <= MaxPriority)) && Enum.IsDefined(typeof(CommanderDuty), duty)
            && Finite(health) && health >= 0 && health <= GameBalance.CommanderHealth
            && bossPickup >= -1 && bossPickup <= 3 && Finite(quietSeconds) && quietSeconds >= 0 && quietSeconds <= GameBalance.AutoReturnSeconds
            && Finite(sampleWork) && sampleWork >= 0 && sampleWork < 1
            && Finite(recoverySeconds) && recoverySeconds >= 0 && recoverySeconds <= GameCalendar.SecondsPerDay
            && Finite(unconsciousSeconds) && unconsciousSeconds >= 0 && unconsciousSeconds <= GameCalendar.SecondsPerDay
            && Finite(deploymentMealSeconds) && deploymentMealSeconds >= 0 && deploymentMealSeconds <= 3 && (rations == null || rations.Valid)
            && Finite(returnPosition.x) && Finite(returnPosition.y) && Finite(returnPosition.z); } }
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
        public int JobPriority(CommanderJobs job) => AllowsJob(job) ? WorkState.Priority(job) : 0;
        // 1~9 단계 지정(0이면 금지). 불가 특성 칸은 켤 수 없다.
        public bool SetJobPriority(CommanderJobs job, int level)
        {
            if (CommanderWorkState.Index(job) < 0 || level < 0 || level > CommanderWorkState.MaxPriority || !SetJobEnabled(job, level > 0)) return false;
            WorkState.Priority(job); if (level > 0) WorkState.priorities[CommanderWorkState.Index(job)] = level;
            return true;
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
            WorkState.rations ??= new FoodLots(); AllocateDeploymentFood();
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
            personalState.carriedRations ??= new FoodLots(); personalState.carriedRations.Add(WorkState.rations); WorkState.rations = new FoodLots();
            WorkState.holdDeployment = false; AntColony.UI.ToastManager.SetPinned("dutyfood:" + personalState.id, null);
            CommandStop();
        }
        public void TickDuty(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || IsDead || IsHostile || AntColony.Save.SaveSystem.Busy) return;
            TickCarriedSupplies(seconds);
            if (TickRecovery(seconds)) return;
            WorkState.rations ??= new FoodLots();
            if (IsDeployed) TickDeploymentMeal(seconds);
            if (!IsDeployed && !IsAwayFromHome && !IsEmbarked && !IsCaptive && !WorkPriorities.Red
                && (Satiety <= GameBalance.EatBelowSatiety || IsSleepTime || Hygiene < GameBalanceNeeds.TemporaryCap))
            {
                if (IsCarrying) { DropCargo(); CommandStop(); }
                if (IsPlaying) LeavePlay();
            }
            TickCarriedMeal(seconds);
            if (TickMeal(seconds)) { LeaveWash(); return; }
            if (TickSleep(seconds)) { LeaveWash(); return; }
            if (TickHygiene(seconds) || TickCorpseWork(seconds) || TickJoy(seconds)) return;
            if (TickMed(seconds)) return;
            if (TickBossRewardWork()) return;
            if (CanSelfTend && MedicalRules.NeedsDoctor(Health, MedicineAllowed(Health)) && !Taken(this, MedJob.Tend)
                && StartMed(MedJob.Tend, this)) return;
            if (IsAwayFromHome) { WorkState.quietSeconds = 0; return; }
            if (!CanReceiveOrders) return;
            if (ServiceJob == CommanderJobs.Firefighting && TickService(seconds)) return;
            if (IsReturning)
            {
                if (GetDistanceTo(WorkState.returnPosition) <= 2) FinishReturn();
                else if ((IsFlying || !Agent.pathPending && !Agent.hasPath) && CanReach(WorkState.returnPosition)) SetMoveDestination(WorkState.returnPosition);
                return;
            }
            if (IsDeployed)
            {
                if (DeploymentEating || WorkState.holdDeployment || !HomeDefenseReturn.Allows(this) || BossCargoCount > 0) { WorkState.quietSeconds = 0; return; }
                WorkState.quietSeconds = IsInCombat || CombatTargeting.FindNearestEnemy(Position, GameBalance.ReturnEnemyRadius, Role) != null
                    ? 0 : Mathf.Min(GameBalance.AutoReturnSeconds, WorkState.quietSeconds + seconds);
                if (WorkState.quietSeconds >= GameBalance.AutoReturnSeconds) ReturnToPost();
                return;
            }
            if (TickBurial(seconds) || TickService(seconds) || TickHunt(seconds) || TickAnimalTask(seconds)) return;
            if (TickGroundCraft(seconds)) return;
            if (TickBedRest()) return;
            if (!IsWorking && ScienceAssignment == null && CraftingWorkshop == null) SetWorkTarget(null);
            if (ScienceAssignment != null)
            {
                if (CampaignResearch.Instance?.Active == null || !AllowsJob(CommanderJobs.Research)) ScienceAssignment.ReleaseResearcher();
                else return;
            }
            if (WorkState.resting && (IsWorking || !IsFatigued)) WorkState.resting = false;
            if (LabUpgradeBusy || IsWorking || IsInCombat) return;
            // 들고 있는 짐은 먼저 창고로(받을 곳이 없으면 그 자리에 내려놓는다). 운반을 마친 뒤에야 다음 작업을 고른다(2026-10-12).
            if (CarriedAmount >= 1) { ReturnCargoToStorage(); return; }
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
            if (WorkState.resting && !WorkPriorities.Red) return;
            if (AntColony.World.ExpeditionTransport.HasPendingCrewOrder(this)) return; // G42 직접 지시가 자율 작업보다 우선
            workScan -= seconds; if (workScan > 0) return; workScan = GameBalance.WorkScanSeconds;
            // ponytail: one scan per second for the small commander roster; index jobs if profiling shows contention.
            // 노란 경보 대상 → 작업 종류 우선순위 → 대상 우선순위 1~9 → 실제 이동 거리 → 거리까지 같으면 오래 기다린 요청(2026-10-12).
            // 같은 작업 우선순위 단계의 후보는 작업 종류와 무관하게 함께 비교한다. 연구는 플레이어 지시 전용이라 자율 목록에 없다.
            var tiers = AutoJobs.Where(AllowsJob).GroupBy(j => WorkState.Priority(j)).OrderByDescending(g => g.Key).ToArray();
            foreach (var yellow in new[] { true, false })
                foreach (var tier in tiers)
                    foreach (var level in tier.SelectMany(AutoCandidates).Where(c => c.target != null && (!yellow || WorkPriorities.Yellow(c.target)))
                        .GroupBy(c => (WorkPriorities.Level(c.target), c.target is Corpse k && k.Priority)).OrderByDescending(g => g.Key.Item1).ThenByDescending(g => g.Key.Item2))
                        foreach (var (_, start) in ByTravel(level))
                            if (start()) return;
        }
        // 같은 대상 우선순위 안에서 실제 경로 길이 → 오래 기다린 요청 순. 경로 계산은 직선거리로 가까운 몇 개만 한다.
        // ponytail: 직선 상위 8개만 경로를 잰다. 후보가 많아 먼 곳이 실제로 더 가까운 경우가 잦으면 늘린다.
        private IEnumerable<(Component target, Func<bool> start)> ByTravel(IEnumerable<(Component target, Func<bool> start)> group)
        {
            var sorted = group.OrderBy(c => (c.target.transform.position - Position).sqrMagnitude).ToList();
            var head = sorted.Take(8).Select(c => (c, d: Mathf.Round(TravelDistance(c.target.transform.position) * 2) / 2))
                .OrderBy(x => x.d).ThenBy(x => WorkPriorities.RequestedAt(x.c.target)).Select(x => x.c);
            return head.Concat(sorted.Skip(8));
        }
        private static readonly CommanderJobs[] AutoJobs = { CommanderJobs.Firefighting, CommanderJobs.Nursing, CommanderJobs.Repair, CommanderJobs.Cleaning, CommanderJobs.Building, CommanderJobs.Art,
            CommanderJobs.Crafting, CommanderJobs.Administration, CommanderJobs.Cooking, CommanderJobs.Hunting, CommanderJobs.Hauling, CommanderJobs.Husbandry, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering };
        // 작업 하나의 후보 대상과 착수 동작. 정렬은 TickDuty가 단계별로 모아서 한다.
        private IEnumerable<(Component target, Func<bool> start)> AutoCandidates(CommanderJobs job)
        {
            switch (job)
            {
                case CommanderJobs.Firefighting:
                    foreach (var fire in FindObjectsByType<WallFire>(FindObjectsSortMode.None).Where(f => f.Burning && f.Managed))
                        yield return (fire, () => CommandExtinguish(fire));
                    yield break;
                case CommanderJobs.Nursing:
                    if (WorkPriorities.Red) yield break; // 빨간 경보 중에는 간호하지 않는다.
                    // 치료가 생산보다 먼저: 할 수 있는 치료가 없을 때(약품 부족 포함)만 약제대 생산 목록을 맡는다(2026-10-11).
                    // 장수 처치·급식·수술(2026-10-15)이 위독하면 그것만, 아니면 동물 투약·수술(2026-10-11)과 함께 후보로 둔다.
                    var commanderCare = CommanderNursingTasks().ToList();
                    var treatments = commanderCare.Concat(HasLifeThreateningPatient && commanderCare.Count > 0 ? Enumerable.Empty<(Component, Func<bool>)>() : AnimalNursingTasks()).ToList();
                    foreach (var t in treatments) yield return t;
                    if (treatments.Count == 0)
                        foreach (var bench in FindObjectsByType<Processor>(FindObjectsSortMode.None).Where(p => p.IsMedical && p.NeedsWork))
                            yield return (bench, () => StartService(bench, CommanderJobs.Nursing));
                    yield break;
                case CommanderJobs.Repair:
                    foreach (var building in FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(BuildingRepair.Needed))
                        yield return (building, () => !Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == building) && StartService(building, CommanderJobs.Repair));
                    yield break;
                case CommanderJobs.Cleaning:
                    foreach (var corpse in CorpseWorkCandidates(false)) yield return (corpse, () => StartCorpseWork(corpse, false));
                    yield break;
                case CommanderJobs.Building: case CommanderJobs.Art:
                    // 중심 대신 주변 접근 지점으로 판정한다(철거·이동 예정지는 가구가 길을 막아 중심에 닿지 못한다).
                    // 재료가 모두 도착한 예정지는 시공, 아직이면 예약된 재료를 직접 가져온다(건설 장수도 배송 가능, 2026-10-12).
                    foreach (var site in FindObjectsByType<BuildingConstructionSite>().Where(s => s.Job == job && !s.Paused))
                        yield return (site, () =>
                        {
                            if (!site.CanWork) return site.PendingPickup != null && CommandFetchFor(site);
                            if (!TryWorkApproach(site.Position, out _)) return false; CommandBuild(site); return ConstructionTarget == site;
                        });
                    yield break;
                case CommanderJobs.Crafting:
                    foreach (var shop in FindObjectsByType<Workshop>(FindObjectsSortMode.None))
                        yield return (shop, () => !shop.Ruined && !shop.IsDead && shop.Crafter == null && shop.CanContinue(this) && GoToFacility(shop));
                    foreach (var processor in FindObjectsByType<Processor>(FindObjectsSortMode.None).Where(p => !p.IsMedical && p.NeedsWork)) // 약제대는 간호(의료)
                        yield return (processor, () => StartService(processor, CommanderJobs.Crafting));
                    yield break;
                case CommanderJobs.Cooking:
                    foreach (var kitchen in FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Where(k => k.NeedsCook))
                        yield return (kitchen, () => !Active.OfType<CommanderAnt>().Any(c => c != this && c.ServiceTarget == kitchen) && StartService(kitchen, CommanderJobs.Cooking));
                    // 요리사가 재료를 창고에서 가져다 놓는다(2026-10-15, 원격 차감 제거).
                    foreach (var kitchen in FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Where(k => k.NeedsIngredients))
                        yield return (kitchen, () => CommandFetchFor(kitchen));
                    foreach (var t in ButcherTasks()) yield return t; // 도축대 해체(2026-10-11)
                    yield break;
                case CommanderJobs.Administration:
                    foreach (var desk in FindObjectsByType<AdminDesk>(FindObjectsSortMode.None).Where(d => d.NeedsWork))
                        yield return (desk, () => StartService(desk, CommanderJobs.Administration));
                    yield break;
                case CommanderJobs.Husbandry: // 포획·돌봄·직접 채취·도축(2026-10-11)
                    foreach (var t in AnimalHusbandryTasks()) yield return t;
                    yield break;
                case CommanderJobs.Hunting:
                    foreach (var animal in WildMonster.All.Where(m => m.Huntable && m.HuntDesignated))
                        yield return (animal, () => !Active.OfType<CommanderAnt>().Any(c => c != this && c.HuntTarget == animal) && StartHunt(animal));
                    yield break;
            }
            if (job == CommanderJobs.Hauling)
            {
                foreach (var wheel in PowerNode.All.Where(p => p.NeedsRunner))
                    yield return (wheel, () => StartService(wheel, CommanderJobs.Hauling));
                foreach (var well in SupplyFacility.All.Where(ManualWellNeedsWork))
                    yield return (well, () => StartService(well, CommanderJobs.Hauling));
                foreach (var t in RescueTasks()) yield return t; // 쓰러진 장수 구조 → 의료 침상(2026-10-15)
                foreach (var t in BurialTasks()) yield return t; // 안치 지시된 시신 → 묘비(2026-10-15)
                foreach (var t in PrisonerFeedTasks()) yield return t; // 포로 급식(2026-10-15)
                foreach (var t in AnimalHaulingTasks()) yield return t; // 생물 운반·먹이통 보충·사체 → 도축대(2026-10-11)
                // 건설 재료 배송(2026-10-12): 예약된 재료를 창고에서 예정지로.
                foreach (var site in FindObjectsByType<BuildingConstructionSite>().Where(s => s.PendingPickup != null))
                    yield return (site, () => CommandFetchFor(site));
                // 바닥 장비(외교 초과분·사망 장수 장비 등)를 장비 보관함으로 옮긴다. 기존 우클릭 회수 동작을 재사용.
                if (EquipmentInventory.Instance != null && !EquipmentInventory.Instance.Full)
                    foreach (var loot in FindObjectsByType<AntColony.World.EquipmentLoot>(FindObjectsSortMode.None).Where(l => l.Collector == null))
                        yield return (loot, () => CanReach(loot.transform.position) && loot.TryCollect(this));
            }
            foreach (var node in ResourceNode.Available.Where(n => n != null && JobFor(n) == job))
                yield return (node, () =>
                {
                    if (!node.CanGatherWith(this) || CarriedAmount != 0 && CarriedType != node.ResourceType
                        || ResourceManager.Instance == null || ResourceManager.Instance.Room(node.ResourceType, this) <= 0
                        || node.IsLooseCargo && Active.OfType<WorkerAnt>().Any(w => w != this && w.CurrentResourceNode == node) // 바닥 더미는 한 장수만 맡는다
                        || node.GetComponentInParent<ExpeditionSite>() != null || node.IsRaidLoot || !TryWorkApproach(node.transform.position, out _)) return false;
                    CommandGather(node); return true;
                });
        }
        public bool IsFatigued => Fatigue >= GameBalance.TiredFatigue;
        private bool CanTakeCivilianOrder => CanReceiveOrders && !IsDeployed && !IsAwayFromHome && !LabUpgradeBusy;
        public bool CanRest => !WorkPriorities.Red && CanTakeCivilianOrder && IsFatigued && !WorkState.resting;
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
        public bool CanSendToTreatment => !WorkPriorities.Red && CanTakeCivilianOrder && PersonalState.NeedsTreatment && TreatmentFacility == null && TreatmentTarget != null;
        // 치료 보내기: 빈 침상이 있는 가장 가까운 의무실로 가서 입원한다(기존 자동 시설 이동을 재사용).
        public bool SendToTreatment()
        {
            if (!CanSendToTreatment) return false;
            var infirmary = TreatmentTarget; CommandStop(); ScienceAssignment?.ReleaseResearcher(); WorkState.resting = false;
            if ((infirmary.Position - Position).sqrMagnitude <= 49) return infirmary.TryAdmit(this);
            if (!UnityEngine.AI.NavMesh.SamplePosition(infirmary.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) || !CanReach(hit.position)) return false;
            automaticFacility = infirmary; base.CommandMove(hit.position); return true;
        }
        // 연구(2026-10-08): 자율로 맡지 않는다. 플레이어가 연구 항목을 고른 뒤 장수를 지정하면 연구소로 가서 맡는다. 완료되면 배정이 풀린다(자동 반복 없음).
        public bool SendToResearch(ScienceLab lab)
        {
            if (lab == null || lab.IsDead || lab.Busy || lab.Target != null || CampaignResearch.Instance?.Active == null || lab.Tier < CampaignResearch.Instance.Active.Tier
                || !AllowsJob(CommanderJobs.Research) || !CanTakeCivilianOrder || !CivilianWorkReady || !CanChangeAllocation) return false;
            CommandStop(); WorkState.resting = false;
            return GoToFacility(lab);
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
