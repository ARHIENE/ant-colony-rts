using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public enum AnimalTask { None, Capture, Carry, Care, Harvest, Slaughter, Dose, Surgery, FillFeeder, HaulCarcass, Butcher }

    // 목장 작업(2026-10-11 개편). 목장별 담당자 없이 작업표로 맡는다.
    // 사육: 포획(현장 결박)·돌봄·직접 채취·요청받은 현장 도축 / 운반: 생물 운반·먹이통 보충·사체를 도축대로 / 간호(=의료): 경상 투약·중상 수술 / 요리: 도축대 해체.
    // 야생 개체를 다루는 일(포획·돌봄·채취·도축·치료)은 취급 실패 판정을 한다. 길들인·쓰러진 개체는 저항하지 않는다.
    public partial class CommanderAnt
    {
        public AnimalTask AnimalJob { get; private set; }
        public Critter AnimalTarget { get; private set; }
        public RanchFacility AnimalFacility { get; private set; }
        public CritterCarcass CarriedCarcass { get; private set; }
        public Critter CarriedCritter => AnimalTarget != null && AnimalTarget.Carrier == this ? AnimalTarget : null;
        private ResourceType feedType; private int feedAmount; private bool feedLoaded;
        private float animalProgress; private int animalStage;
        // 생물을 안고 있으면 무게·근력에 따라 느려진다.
        internal float AnimalCarryMultiplier => CarriedCritter is Critter a ? Mathf.Clamp(1f - a.Info.weight + talents.Level(CommanderActivity.Strength) * .02f, .4f, 1f) : 1f;

        private static CommanderJobs JobOf(AnimalTask t) => t switch
        {
            AnimalTask.Carry or AnimalTask.FillFeeder or AnimalTask.HaulCarcass => CommanderJobs.Hauling,
            AnimalTask.Dose or AnimalTask.Surgery => CommanderJobs.Nursing,
            AnimalTask.Butcher => CommanderJobs.Cooking,
            _ => CommanderJobs.Husbandry
        };
        private static CommanderActivity SkillOf(AnimalTask t) => t switch
        {
            AnimalTask.Dose or AnimalTask.Surgery => CommanderActivity.Medicine,
            AnimalTask.Butcher => CommanderActivity.Cooking,
            AnimalTask.Carry or AnimalTask.FillFeeder or AnimalTask.HaulCarcass => CommanderActivity.Strength,
            _ => CommanderActivity.Husbandry
        };

        public bool StartAnimalTask(AnimalTask task, Critter critter = null, RanchFacility facility = null, CritterCarcass carcass = null)
        {
            if (!CivilianWorkReady || !CanReceiveOrders || IsWorking || LabUpgradeBusy || IsCarrying || !CanDoJob(JobOf(task))) return false;
            if (critter != null && (critter.Handler != null && critter.Handler != this || critter.Monster == null || critter.Monster.IsDead)) return false;
            if (facility != null && (facility.Worker != null && facility.Worker != this || !facility.Usable)) return false;
            if (carcass != null && (carcass.Carrier != null || carcass.Corpse == null || carcass.Corpse.Handler != null)) return false;
            if (task == AnimalTask.Slaughter && (critter == null || !critter.SlaughterOrdered || critter.Clinic != null)) return false;
            if (task == AnimalTask.Surgery && facility != null && facility.Patient != null && facility.Patient != critter) return false;
            var at = critter != null ? critter.transform.position : facility != null ? facility.Position : carcass != null ? carcass.transform.position : Position;
            if (task == AnimalTask.FillFeeder) at = StorageNear(Position);
            if (!TryWorkApproach(at, out _)) return false;
            if (task == AnimalTask.FillFeeder && facility?.FillNeed() == null) return false;
            // 투약·수술 시작 전에 재고 1개를 예약한다(중복 소비 방지). 회복 재입원·이어받는 수술은 키트가 필요 없다.
            var stock = task == AnimalTask.Dose ? ResourceType.AnimalMedicine : ResourceType.SurgeryKit;
            var needsStock = task == AnimalTask.Dose || task == AnimalTask.Surgery && critter != null && !critter.SurgeryStarted && !(critter.Recovering && !critter.HeavyInjury);
            if (needsStock && AnimalMedicalStock.Available(stock) < 1 && !AnimalMedicalStock.Holds(this, stock)) return false;
            CommandStop(); WorkState.resting = false;
            if (needsStock && !AnimalMedicalStock.Reserve(this, stock)) return false;
            AnimalJob = task; AnimalTarget = critter; AnimalFacility = facility; CarriedCarcass = null; animalProgress = 0; animalStage = 0;
            if (critter != null) critter.Handler = this;
            if (facility != null && (task == AnimalTask.Care || task == AnimalTask.Butcher || task == AnimalTask.Surgery)) facility.Worker = this; // 수술은 빈 치료대를 먼저 확보
            if (carcass != null) { carcass.Corpse.Claim(this); CarriedCarcass = carcass; }
            if (task == AnimalTask.FillFeeder) { var (t, n) = facility.FillNeed().Value; feedType = t; feedAmount = Mathf.Min(n, Mathf.FloorToInt(LoadCapacity)); feedLoaded = false; facility.Reserve(t, feedAmount); }
            return true;
        }

        // 작업 중단: 안고 있던 생물·사체는 그 자리에 내려놓는다(결박 시간은 남은 만큼 다시 흐른다). 예약은 해제.
        private void ReleaseAnimalTask()
        {
            AnimalMedicalStock.Release(this); // 시작 전 취소: 예약만 해제
            if (AnimalJob == AnimalTask.None) return;
            var critter = AnimalTarget;
            if (critter != null)
            {
                if (AnimalJob == AnimalTask.Slaughter) critter.InterruptSlaughter(); // 실제 도축 중 강제 중단: 부분 피해, 생존
                if (critter.Carrier == this) critter.PutDown(Position);
                if (critter.Handler == this) critter.Handler = null;
                if (critter.CalledTo == AnimalFacility) critter.CalledTo = null;
            }
            if (AnimalFacility != null && AnimalFacility.Worker == this) AnimalFacility.Worker = null;
            if (CarriedCarcass != null) { CarriedCarcass.Carrier = null; CarriedCarcass.Corpse?.Release(this); CarriedCarcass.transform.position = Position; }
            if (AnimalJob == AnimalTask.FillFeeder)
            {
                AnimalFacility?.Reserve(feedType, -feedAmount);
                if (feedLoaded && feedAmount > 0) DiplomacyManager.StoreResource(feedType, feedAmount, ResourceReason.Refund, Position);
            }
            AnimalJob = AnimalTask.None; AnimalTarget = null; AnimalFacility = null; CarriedCarcass = null; feedAmount = 0; feedLoaded = false;
        }
        internal void DropCarriedCritter() { if (CarriedCritter != null) CommandStop(); }

        private bool TickAnimalTask(float seconds)
        {
            if (AnimalJob == AnimalTask.None) return false;
            if (!CivilianWorkReady || !CanDoJob(JobOf(AnimalJob))) { CommandStop(); return false; }
            var a = AnimalTarget;
            if (a != null && (a.Monster == null || a.Monster.IsDead) || AnimalFacility != null && !AnimalFacility.Usable && AnimalJob != AnimalTask.Surgery && AnimalJob != AnimalTask.HaulCarcass) { CommandStop(); return false; }
            switch (AnimalJob)
            {
                case AnimalTask.Capture: return TickCapture(a, seconds);
                case AnimalTask.Carry: return TickCarry(a);
                case AnimalTask.Care: return TickCare(a, seconds);
                case AnimalTask.Slaughter: return TickSlaughter(a, seconds);
                case AnimalTask.Harvest: case AnimalTask.Dose: return TickHandle(a, seconds);
                case AnimalTask.Surgery: return TickSurgery(a, seconds);
                case AnimalTask.FillFeeder: return TickFeeder();
                case AnimalTask.HaulCarcass: return TickCarcass();
                case AnimalTask.Butcher:
                    if (!Near(AnimalFacility.Position, 2.5f)) return true;
                    StopMoving(); GetComponent<AntVisual>()?.Action("Attack");
                    if (!AnimalFacility.Butcher(this, seconds)) CommandStop();
                    return true;
            }
            return false;
        }
        // 도착 판정: 대상 가까이이거나, 건물이라 중심까지 못 가면 접근 지점에 닿았을 때.
        private bool Near(Vector3 p, float range)
        {
            if (GetDistanceTo(p) <= range) return true;
            if (!TryWorkApproach(p, out var approach)) { CommandStop(); return false; }
            if (GetDistanceTo(approach) <= 1.2f) return true;
            SetMoveDestination(approach); TickFlightMovement();
            return false;
        }
        private bool Work(float seconds, float needed, AnimalTask kind)
        {
            StopMoving(); GetComponent<AntVisual>()?.Action("Eat");
            animalProgress += WorkRate(SkillOf(kind)) * seconds;
            GainExperience(SkillOf(kind), seconds);
            return animalProgress >= needed;
        }
        // 취급 실패 판정: 실패하면 성향대로 도주·주변 공격·다룬 장수 공격.
        private bool Handled(Critter a, CommanderActivity skill)
        {
            var approved = a.ApprovedId == PersonalState.id; if (approved) a.ApprovedId = "";
            if (Random.value >= SpeciesInfo.FailChance(a.Species, a.Tame || a.Downed, this, skill)) return true;
            a.Handler = null; a.Agitate(this);
            a.RaiseEscape($"{a.Info.name} 취급 실패 — " + (a.Info.temper == CritterTemper.Flee ? "도망치는 중입니다." : "주변을 공격합니다. 진정할 때까지 거리를 두세요."));
            return false;
        }

        // 포획: 덫·제압 없이 다가가 붙잡아 현장에 결박한다. 실패하면 지정 취소·실패 알림·소량 피해(자동 재시도 없음).
        private bool TickCapture(Critter a, float seconds)
        {
            if (!a.CaptureDesignated) { CommandStop(); return false; }
            if (!Near(a.transform.position, 2f)) return true;
            // 길들인 개체는 저항하지 않으므로 다가가면 바로 결박한다(실패·피해 판정 없음).
            if (!a.Tame && !Work(seconds, GameBalance.CritterCaptureSeconds, AnimalTask.Capture)) return true;
            if (!a.Tame && !Handled(a, CommanderActivity.Husbandry))
            {
                a.CaptureDesignated = false; ToastManager.Show($"{CommanderName}: {a.Info.name} 포획 실패");
                WorkState.health = Mathf.Max(1, WorkState.health - GameBalance.CritterCaptureFailDamage); // 포획 실패: 소량 피해(쓰러지지는 않음)
                CommandStop(); return false;
            }
            a.Bind(); a.Handler = null;
            ToastManager.Show($"{CommanderName}: {a.Info.name}을(를) 결박했습니다 — 개체를 선택해 운반할 우리를 지정하세요." + (a.TransportTo != null ? $" ({a.TransportTo.Data.displayName}로 운반 요청됨)" : ""));
            AnimalTarget = null; CommandStop(); return false;
        }

        // 운반: 장수 한 명당 한 마리. 들고 가는 동안 결박 시간은 멈추고 다른 일은 하지 않는다. 도착하면 결박을 푼다.
        private bool TickCarry(Critter a)
        {
            var dest = a.TransportTo;
            if (dest == null || !dest.Operating || !a.Carriable && a.Carrier != this) { CommandStop(); return false; }
            if (a.Carrier != this)
            {
                if (!Near(a.transform.position, 2f)) return true;
                a.PickUp(this); return true;
            }
            var room = dest.Room; var cell = room.Cells[(a.GetHashCode() & 0x7fffffff) % room.Cells.Count];
            var drop = new Vector3(cell.x + .5f, Position.y, cell.y + .5f);
            if (RoomSystem.RoomAt(Position) != room || GetDistanceTo(drop) > 2.5f)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(drop, out var hit, 3, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position)) { SetMoveDestination(hit.position); TickFlightMovement(); }
                else CommandStop();
                return true;
            }
            a.Handler = null; a.Deliver(Position);
            ToastManager.Show($"{CommanderName}: {a.Info.name}을(를) {dest.Data.displayName}에 옮겼습니다.");
            AnimalTarget = null; CommandStop(); return false;
        }

        // 돌봄: 장수가 돌봄대에 가서 같은 우리 안의 한 마리를 부른다. 둘 다 모이면 돌보고, 야생은 취급 판정.
        private bool TickCare(Critter a, float seconds)
        {
            var station = AnimalFacility;
            if (a.Pen == null || !a.InOperatingPen || a.CareBlocked) { CommandStop(); return false; }
            if (a.Eating) { CommandStop(); return false; } // 배고프면 식사가 먼저(대기 순서는 유지)
            if (!Near(station.Position, 2.5f)) return true;
            a.CalledTo = station;
            if ((a.transform.position - station.Position).sqrMagnitude > 2.5f * 2.5f)
            {
                if ((animalProgress -= seconds) < -GameBalance.CritterCareWorkSeconds * 4) CommandStop(); // 길이 막혀 못 오면 건너뛴다
                return true;
            }
            if (animalProgress < 0) animalProgress = 0;
            a.Halt();
            if (!Work(seconds, GameBalance.CritterCareWorkSeconds, AnimalTask.Care)) return true;
            a.CalledTo = null;
            if (Handled(a, CommanderActivity.Husbandry)) a.FinishCare(this);
            else { a.CareBlocked = true; ToastManager.Show($"{CommanderName}: {a.Info.name} 돌봄 실패 — 이 개체의 자동 돌봄을 멈춥니다(개체 화면에서 재허용)."); }
            CommandStop(); return false;
        }

        // 직접 채취·투약: 개체에게 가서 작업하고 야생은 취급 판정. 투약은 예약한 의약품 1개를 쓴다.
        private bool TickHandle(Critter a, float seconds)
        {
            var pen = a.Pen;
            if (pen == null && !(AnimalJob == AnimalTask.Dose && a.Clinic != null) || AnimalJob == AnimalTask.Harvest && a.DirectStock < 1 // 시설 안 투약은 우리 소속과 무관
                || AnimalJob == AnimalTask.Dose && !AnimalMedicalStock.Holds(this, ResourceType.AnimalMedicine)) { CommandStop(); return false; }
            if (!Near(a.transform.position, 2f)) return true;
            var (need, skill) = AnimalJob == AnimalTask.Harvest ? (GameBalance.CritterHarvestSeconds, CommanderActivity.Husbandry) : (GameBalance.CritterDoseSeconds, CommanderActivity.Medicine);
            if (!Work(seconds, need, AnimalJob)) return true;
            if (!Handled(a, skill)) { CommandStop(); return false; }
            a.Handler = null;
            if (AnimalJob == AnimalTask.Harvest) DiplomacyManager.DropFloor(a.Info.direct, a.TakeDirect(), a.transform.position); // 채취 물건은 운반 작업이 옮긴다
            else if (AnimalMedicalStock.Consume(this, ResourceType.AnimalMedicine)) { a.Medicate(); a.TreatOrder = false; }
            AnimalTarget = null; CommandStop(); return false;
        }

        // 수동 도축: 유저가 요청한 개체만. 야생은 먼저 포획해 결박(도축용 포획, 취급 판정 — 실패하면 요청 취소·자동 재시도 없음).
        // 실제 도축을 시작하면 취소할 수 없고, 동물은 멈추고 결박 시간도 멈춘다. 끝나면 사체가 남는다.
        private bool TickSlaughter(Critter a, float seconds)
        {
            if (!a.SlaughterOrdered || a.Clinic != null) { CommandStop(); return false; }
            if (!a.SlaughterStarted)
            {
                if (!Near(a.transform.position, 2f)) return true;
                if (!a.Tame && !a.Downed && !a.Bound)
                {
                    if (!Work(seconds, GameBalance.CritterCaptureSeconds, AnimalTask.Slaughter)) return true;
                    if (!Handled(a, CommanderActivity.Husbandry))
                    {
                        a.CancelSlaughter(); ToastManager.Show($"{CommanderName}: {a.DisplayName} 도축용 포획 실패 — 요청을 취소했습니다.");
                        CommandStop(); return false;
                    }
                    a.Bind(); a.Handler = this;
                }
                animalProgress = 0; a.BeginSlaughter();
            }
            if (!Work(seconds, GameBalance.CritterSlaughterSeconds, AnimalTask.Slaughter)) return true;
            a.Handler = null; AnimalTarget = null; a.FinishSlaughter();
            CommandStop(); return false;
        }

        // 수술: 빈 치료대와 키트를 확보(예약)한 뒤 개체를 붙잡아(야생은 취급 판정) 치료대로 옮기고, 시작 때 키트를 소비한다.
        // 집도의가 자리를 비우면 진행은 멈춘 채 남고 다른 의료 장수가 새 키트 없이 이어받는다(사용자 확정). 결과는 Critter.CompleteSurgery(기여도 가중 성공률).
        // 회복 중 시설이 부서져 밖에 나온 개체는 같은 작업으로 빈 치료대에 다시 입원시킨다(키트 불필요, 잔여 회복부터).
        private bool TickSurgery(Critter a, float seconds)
        {
            var clinic = AnimalFacility;
            if (clinic == null || !clinic.isActiveAndEnabled || clinic.IsDead) { CommandStop(); return false; }
            var readmit = a.Recovering && !a.HeavyInjury;
            if (a.Clinic != clinic)
            {
                if (clinic.Patient != null && clinic.Patient != a) { CommandStop(); return false; }
                if (a.Carrier != this)
                {
                    if (!Near(a.transform.position, 2f)) return true;
                    if (animalStage == 0 && !a.Downed && !a.Tame)
                    {
                        if (!Work(seconds, GameBalance.CritterCaptureSeconds, AnimalTask.Surgery)) return true;
                        if (!Handled(a, CommanderActivity.Medicine)) { CommandStop(); return false; }
                        a.Handler = this;
                    }
                    animalStage = 1; animalProgress = 0; a.PickUp(this); return true;
                }
                if (!Near(clinic.Position, 2.5f)) return true;
                a.EnterClinic(clinic); clinic.Worker = this;
                if (readmit) { a.Handler = null; AnimalTarget = null; CommandStop(); return false; }
            }
            if (readmit || a.AwaitingDecision) { CommandStop(); return false; }
            if (!Near(clinic.Position, 2.5f)) return true;
            clinic.Worker = this;
            if (!a.SurgeryStarted)
            {
                if (!a.MayOperate(this)) { CommandStop(); return false; }
                if (!AnimalMedicalStock.Consume(this, ResourceType.SurgeryKit)) // 예약 키트가 사라졌고 대체도 없음: 원래 우리로
                { a.Handler = null; a.AbortAdmission(clinic.Position + Vector3.right * 1.5f); AnimalTarget = null; CommandStop(); return false; }
                a.StartSurgery();
            }
            StopMoving(); GetComponent<AntVisual>()?.Action("Eat");
            a.AddSurgery(this, WorkRate(CommanderActivity.Medicine) * seconds); GainExperience(CommanderActivity.Medicine, seconds, CommanderActivity.Research);
            if (a.SurgeryProgress < GameBalance.CritterSurgerySeconds) return true;
            a.Handler = null; AnimalTarget = null; CommandStop(); a.CompleteSurgery(this); return false;
        }

        // 먹이통 보충: 창고에서 꺼내 먹이통에 넣는다. 운반 중인 양은 예정 재고로 잡아 과잉 반입을 막는다.
        private bool TickFeeder()
        {
            var feeder = AnimalFacility;
            if (!feedLoaded)
            {
                if (!Near(StorageNear(Position), 3f)) return true;
                var rm = ResourceManager.Instance; var take = Mathf.Min(feedAmount, rm != null ? rm.GetAmount(feedType) : 0);
                if (take <= 0 || !rm.TrySpend(feedType, take, ResourceReason.Upkeep)) { CommandStop(); return false; }
                feeder.Reserve(feedType, take - feedAmount); feedAmount = take; feedLoaded = true; return true;
            }
            if (!Near(feeder.Position, 2.5f)) return true;
            feeder.Reserve(feedType, -feedAmount); feeder.Deposit(feedType, feedAmount); feedAmount = 0; feedLoaded = false;
            GainExperience(CommanderActivity.Strength, 2f);
            CommandStop(); return false;
        }
        private static Vector3 StorageNear(Vector3 from)
        {
            var best = FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(b => (b is Storage || b is Stockpile) && b.isActiveAndEnabled && !b.IsDead)
                .OrderBy(b => (b.Position - from).sqrMagnitude).FirstOrDefault();
            return best != null ? best.Position : from;
        }

        // 사체 운반: 도축대로 옮긴다. 장수가 집으면 포식자의 섭식은 중단된다.
        private bool TickCarcass()
        {
            var k = CarriedCarcass; var table = AnimalFacility;
            // 목적 도축대가 철거·파괴되면 다른 사용 가능한 도축대로, 없으면 그 자리에 내려놓는다(일시정지된 도축대도 이미 운반 중이면 받는다).
            if (k != null && (table == null || !table.isActiveAndEnabled || table.IsDead)) AnimalFacility = table = RanchFacility.ButcherFor(k.Species, Position);
            if (k == null || k.Meat <= 0 || table == null) { CommandStop(); return false; }
            if (k.Carrier != this)
            {
                if (!Near(k.transform.position, 2f)) return true;
                k.Carrier = this; return true;
            }
            if (!Near(table.Position, 2.5f)) return true;
            table.AddCarcass(k); k.Carrier = null; CarriedCarcass = null;
            k.Corpse?.Release(this); Destroy(k.gameObject);
            GainExperience(CommanderActivity.Strength, 2f);
            CommandStop(); return false;
        }

        // 운반 작업 후보(작업표 '운반'): 생물 운반 요청·먹이통 보충(우선순위 높은 통부터)·사체 → 도축대.
        private System.Collections.Generic.IEnumerable<(Component target, System.Func<bool> start)> AnimalHaulingTasks()
        {
            foreach (var a in Critter.All.Where(x => x != null && x.TransportTo != null && x.Carriable && x.Carrier == null && x.Handler == null && !x.Agitated).ToList())
                yield return (a, () => StartAnimalTask(AnimalTask.Carry, a));
            foreach (var f in RanchFacility.All.Where(f => f.IsFeeder && f.FillNeed() != null).OrderByDescending(f => f.Priority).ToList())
                yield return (f, () => StartAnimalTask(AnimalTask.FillFeeder, null, f));
            // 사체 → 그 종을 허용하고 일시정지가 아닌 가장 가까운 도축대.
            foreach (var k in CritterCarcass.All.Where(k => k.Meat > 0 && k.Carrier == null && k.Corpse != null && k.Corpse.Handler == null).ToList())
                if (RanchFacility.ButcherFor(k.Species, k.transform.position) is RanchFacility table)
                    yield return (k, () => StartAnimalTask(AnimalTask.HaulCarcass, null, table, k));
        }
        private System.Collections.Generic.IEnumerable<(Component target, System.Func<bool> start)> AnimalHusbandryTasks()
        {
            foreach (var a in Critter.All.Where(x => x != null && x.CaptureDesignated && x.Handler == null && x.Carrier == null && x.Clinic == null && !x.Bound && !x.Agitated
                && Ranch.MayHandle(x, this)).ToList())
                yield return (a, () => StartAnimalTask(AnimalTask.Capture, a));
            // 유저가 요청한 도축(요청 순서는 따로 두지 않고 작업표·대상 우선순위·거리 규칙을 따른다).
            foreach (var a in Critter.All.Where(x => x != null && x.SlaughterOrdered && !x.SlaughterStarted && x.Handler == null && x.Carrier == null && x.Clinic == null && !x.Agitated
                && Ranch.MayHandle(x, this)).ToList())
                yield return (a, () => StartAnimalTask(AnimalTask.Slaughter, a));
            foreach (var pen in FindObjectsByType<Ranch>(FindObjectsSortMode.None))
                foreach (var t in pen.HusbandryTasks(this)) yield return t;
        }
        private System.Collections.Generic.IEnumerable<(Component target, System.Func<bool> start)> AnimalNursingTasks()
        {
            // 치료대에 있는 중상 개체의 수술(집도의가 비운 수술 이어받기 포함, 실패 후 선택 대기 제외).
            foreach (var a in Critter.All.Where(x => x != null && x.Clinic != null && x.HeavyInjury && x.Handler == null && x.Clinic.Worker == null && x.MayOperate(this)).ToList())
                if (a.SurgeryStarted || AnimalMedicalStock.Available(ResourceType.SurgeryKit) >= 1)
                    yield return (a.Clinic, () => StartAnimalTask(AnimalTask.Surgery, a, a.Clinic));
            // 회복 중 치료대 안에서 새로 생긴 경상: 투약(원래 우리의 자동 치료 또는 치료 지시). 예약되지 않은 의약품이 있어야 한다.
            foreach (var a in Critter.All.Where(x => x != null && x.Clinic != null && x.Recovering && x.Injured && !x.HeavyInjury && x.Medicated <= 0 && x.Handler == null
                && (x.TreatOrder || x.ReturnPen != null && x.ReturnPen.AutoTreat) && Ranch.MayHandle(x, this, CommanderActivity.Medicine)).ToList())
                if (AnimalMedicalStock.Available(ResourceType.AnimalMedicine) >= 1) yield return (a, () => StartAnimalTask(AnimalTask.Dose, a));
            // 회복 중 시설이 부서져 밖에 있는 개체: 빈 치료대에 재입원(키트 불필요).
            foreach (var a in Critter.All.Where(x => x != null && x.Recovering && !x.HeavyInjury && x.Clinic == null && x.Handler == null && x.Carrier == null && Ranch.MayHandle(x, this, CommanderActivity.Medicine)).ToList())
                if (RanchFacility.FreeClinic() is RanchFacility free) yield return (a, () => StartAnimalTask(AnimalTask.Surgery, a, free));
            foreach (var pen in FindObjectsByType<Ranch>(FindObjectsSortMode.None))
                foreach (var t in pen.NursingTasks(this)) yield return t;
        }
        private System.Collections.Generic.IEnumerable<(Component target, System.Func<bool> start)> ButcherTasks()
            => RanchFacility.All.Where(f => f.NeedsButcher && f.Worker == null).ToList().Select(f => ((Component)f, (System.Func<bool>)(() => StartAnimalTask(AnimalTask.Butcher, null, f))));
    }
}
