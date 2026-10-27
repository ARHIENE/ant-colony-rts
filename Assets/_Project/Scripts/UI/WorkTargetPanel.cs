using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed class WorkTargetPanel : MonoBehaviour
    {
        public static Component Target { get; private set; }
        private RectTransform root;
        private Text title, status, instruction;
        private Button action, eatCorpse;
        public static void Clear() => Target = null;
        public static void Select(Component target)
        {
            AntColony.Core.SceneObjects.Any<AntColony.Units.SelectionManager>()?.ClearSelection();
            Target = target;
        }
        private void Start()
        {
            root = L.Plate(HudConsole.Center, "WorkTargetPanel", 12, 10, 808, 188);
            MenuTheme.Stretch(root); root.offsetMin = new Vector2(12, 8); root.offsetMax = new Vector2(-12, -14);
            root.Find("Rim").gameObject.SetActive(false); MenuTheme.InsetScreen(root.gameObject);
            title = L.Label(root, "", 21, 16, 8, 780, 30);
            status = L.Label(root, "", 15, 16, 42, 780, 44);
            instruction = L.Label(root, "", 12, 16, 126, 780, 42, MenuTheme.Muted);
            action = L.Button(root, "TargetAction", "", 544, 94, 246, 32, Act);
            eatCorpse = L.Button(root, "EatCorpse", "동족 포식 지시", 544, 136, 246, 32, EatCorpse);
            root.gameObject.SetActive(false);
        }
        private void OnDestroy() => Clear();
        private void LateUpdate()
        {
            var width = root.rect.width - 32;
            title.rectTransform.sizeDelta = new Vector2(width, 30);
            status.rectTransform.sizeDelta = new Vector2(width, 44);
            L.Place((RectTransform)action.transform, 16 + width * .6f, 94, width * .4f, 32);
            L.Place((RectTransform)eatCorpse.transform, 16 + width * .6f, 136, width * .4f, 32);
            foreach (var button in new[] { action, eatCorpse }) MenuTheme.Stretch(button.GetComponentInChildren<Text>().rectTransform);
            bool visible = Target != null && Target.gameObject.activeInHierarchy && !BuildScreen.Picking;
            root.gameObject.SetActive(visible); if (!visible) return;
            eatCorpse.gameObject.SetActive(Target is Corpse edible && (edible.Edible || edible.GetComponent<ResourceNode>() != null)
                || Target is SupplyFacility water && !water.IsNaturalSource && water.Supply.fluid == (int)AntColony.Data.ResourceType.Water && water.Supply.amount > 0);
            if (Target is Corpse corpse)
            {
                action.gameObject.SetActive(true);
                title.text = corpse.Data.name + " 시체";
                var meat = corpse.GetComponent<ResourceNode>();
                status.text = $"{corpse.Data.count}구 · 소멸까지 {corpse.Data.remaining:0}초 · {(corpse.Handler != null ? corpse.Handler.CommanderName + $" 작업 {corpse.Handler.CorpseProgress:0.#}/5초" : "미처리")}"
                    + (meat != null ? $"\n운반 가능한 식량 {meat.AmountRemaining:0.#}" : "");
                instruction.rectTransform.sizeDelta = new Vector2(width * .55f, 64);
                instruction.text = "선택 장수 + 우클릭: 치우기 · Alt+우클릭: 동족 포식\n치우기 기본 5초. 사냥 사체는 우선 지정 시에만 자동 청소합니다.";
                action.GetComponentInChildren<Text>().text = corpse.Priority ? "치우기 우선 취소" : "치우기 우선";
                eatCorpse.GetComponentInChildren<Text>().text = meat != null ? "사냥 식량 운반" : "동족 포식 지시";
                return;
            }
            instruction.rectTransform.sizeDelta = new Vector2(width, 64);
            var workforce = Workforce.For(Target);
            title.text = Target is BuildingBase b && b.Data != null ? b.Data.displayName : Target.name;
            var workers = AntColony.Units.AntUnitBase.Active.OfType<AntColony.Units.CommanderAnt>().Where(c => c.WorkTarget == workforce).ToArray();
            status.text = workers.Length == 0 ? "작업 중인 장수 없음"
                : $"작업 장수 {workers.Length}명: {string.Join(" · ", workers.Select(c => c.CommanderName))}";
            instruction.text = "평시 작업은 장수만 합니다. 작업표 우선순위에 따라 장수가 알아서 맡습니다.";
            var label = action.GetComponentInChildren<Text>();
            action.gameObject.SetActive(Target is ResourceNode || Target is Workshop || Target is Processor || Target is Kitchen cookAct && !cookAct.IsTable || Target is Ranch || Target is RanchFacility f0 && f0.IsFeeder || Target is Storage || Target is Stockpile || Target is BuildingConstructionSite || Target is WildMonster || Target is Gate || Target is Barracks || Target is DigSite || Target.GetComponent<FarmPlot>() != null || Target is CompostBin || Target is Tombstone || Target is Infirmary || Target.GetComponent<PrisonerCamp>() != null);
            if (Target is BuildingBase placed && !(Target is Gate)) { var room = RoomSystem.RoomAt(placed.Position); instruction.text = (room == null ? "방 밖(효과 적음)" : $"방: {room.Title} · 등급 {room.GradeName} ({room.Cells.Count}칸)") + " · " + instruction.text; }
            if (Target is Dormitory dorm)
            {
                status.text = $"침대 {dorm.Beds}개 · 배정 {dorm.Residents.Count(c => c != null && !c.IsDead && c.IsColonyMember)}명"
                    + (HudOverview.BedShortage > 0 ? $"\n<color=#ef955f>군체 전체 침대 {HudOverview.BedShortage}개 부족</color>" : "\n군체 전체 침대 충분");
                instruction.text = RoomDescription(dorm) + "\n배정: " + ResidentNames(dorm);
            }
            if (Target is ResourceNode node)
            {
                label.text = node.NeedsDesignation ? node.WorkRequested ? "지정 작업 취소" : node.WorkName + " 지정" : node.GatheringForbidden ? "채집 허용" : "채집 금지";
                instruction.text = $"남은 자원 {node.AmountRemaining:0.#} · " + (node.NeedsDesignation ? node.WorkWaitReason : node.IsLooseCargo ? "운반 작업" : "시설 자동 작업");
                if (node.NeedsDesignation) { eatCorpse.gameObject.SetActive(true); eatCorpse.GetComponentInChildren<Text>().text = node.GatheringForbidden ? "채집 금지 해제" : "채집 금지"; }
            }
            if (Target.GetComponent<PrisonerCamp>() is PrisonerCamp campInfo) { label.text = "포로 관리"; instruction.text = $"포로 {campInfo.Count}/{campInfo.Capacity} · 경비 {(campInfo.Guarded ? "있음" : "없음")}"; }
            if (Target is CompostBin compostInfo) { label.text = "퇴비통 관리"; instruction.text = compostInfo.Status; }
            if (Target is Tombstone tombInfo) { label.text = "묘비"; instruction.text = tombInfo.Empty ? "빈 묘비 · 사망 기록에서 고인을 등록하세요" : $"{tombInfo.Data2.name} · " + (tombInfo.Data2.body ? "안치됨" : "시신 미안치"); }
            if (Target is Infirmary bedInfo) { label.text = "환자 보기"; instruction.text = $"의료 침상 {bedInfo.Patients.Count}/{Infirmary.Capacity}" + (bedInfo.Patients.Count > 0 ? ": " + string.Join(", ", bedInfo.Patients.Select(p => $"{p.CommanderName}({p.Health.Summary})")) : ""); }
            if (Target.GetComponent<FarmPlot>() is FarmPlot farmInfo) { label.text = "밭 관리"; instruction.text = $"{farmInfo.Crop.ToString()} · {farmInfo.Status}"; }
            if (Target is Workshop shop) { label.text = "제작 대기열"; instruction.text = $"제작 대기 {shop.Jobs.Count}건 · {shop.WaitReason}"; }
            if (Target is Processor processor) { label.text = "가공 목록"; instruction.text = $"{processor.WaitReason} · {processor.Progress:P0} · 목록 {processor.Orders.Count}건"; }
            if (Target is Kitchen cookLabel && !cookLabel.IsTable) label.text = "요리 목록";
            if (Target is Ranch ranch)
            {
                label.text = "목장 관리"; var n = ranch.Animals.Count();
                instruction.text = ranch.Operating ? $"우리 {ranch.Cells}칸 · {n}마리" + (ranch.Crowding > 1 ? $" · 과밀 {ranch.Crowding:P0}" : "") + " · 표지를 둔 방(벽·울타리·문)이 우리입니다."
                    : $"뚫린 우리 · {n}마리 — 벽·울타리·문으로 둘러싸야 운영합니다(지붕 불필요).";
            }
            if (Target is RanchFacility facility)
            {
                if (facility.IsFeeder) { label.text = "먹이통 설정"; instruction.text = string.Join(" · ", facility.FeedTypes.Where(t => facility.Stock(t) >= 1 || facility.Allowed(t)).Select(t => $"{t.DisplayName()} {facility.Stock(t):0}/{facility.Target(t)}")); }
                else if (facility.IsCareStation) instruction.text = "같은 우리의 생물을 사육 장수가 한 마리씩 불러 돌봅니다." + (facility.Worker != null ? $" 돌보는 중: {facility.Worker.CommanderName}" : "");
                else if (facility.IsClinic)
                {
                    if (facility.Patient is Critter p) label.text = "환자 보기";
                    instruction.text = facility.Patient is Critter q ? GameMenuController.ClinicStatus(q) : facility.Worker != null ? $"입원 예약: {facility.Worker.CommanderName}" : "비어 있음 · 중상 동물 수술용(동물 수술 키트 필요)";
                }
                else if (facility.IsButcher) { label.text = "도축대 관리"; instruction.text = (facility.Paused ? "일시정지 · " : "") + $"대기 사체 {facility.Carcasses}구 · 등록 순서대로 요리 작업 장수가 해체합니다."; }
            }
            if ((Target is Storage || Target is Stockpile) && AntColony.Core.ResourceManager.Instance?.BinOf((BuildingBase)Target) is AntColony.Core.StoreBin bin)
            {
                label.text = "창고 설정";
                instruction.text = $"우선순위 {bin.priority} · " + (bin.Stocked.Any() ? string.Join(" · ", bin.Stocked.Select(t => $"{t.DisplayName()} {bin.Amount(t)}")) : "비어 있음");
            }
            if (Target is BuildingBase packed && Demolition.IsPacked(packed)) status.text = "포장된 가구(기능 정지) · 명령 '이동'으로 새 위치에 다시 설치합니다.";
            if (Target is Barracks barracks) label.text = "병영 강화";
            if (Target is UndergroundScanner scanner)
            {
                action.gameObject.SetActive(true); action.interactable = true;
                label.text = "지하 탐사 관리"; status.text = scanner.Status;
                instruction.text = $"200W · 반경 35m · 후보당 300초 · 담당: {scanner.Target?.CommanderName ?? "없음"}";
            }
            if (Target is DigSite) label.text = "굴착 확장";
            if (Target is BuildingConstructionSite site)
            {
                label.text = "건설 취소";
                // 필요/예약/도착 수량·대기 사유·배송/시공 장수(2026-10-12).
                var needs = string.Join(" · ", site.Needs.Select(n => $"{n.type.DisplayName()} {n.delivered}/{n.need}(예약 {n.reserved}·운반 {site.InTransit(n.type)})"));
                var crew = site.Builders.Concat(site.Couriers).Select(c => c.CommanderName).Distinct().ToList();
                status.text = (site.Started ? "시공 중" : site.Ready ? "재료 도착 · 시공 대기" : "재료 대기") + $" · {BuildingConstructionSite.Describe(site.WaitReason)}"
                    + (crew.Count > 0 ? " · 담당: " + string.Join(" · ", crew) : "");
                instruction.text = (needs.Length > 0 ? needs + "\n" : "") + $"남은 작업 {site.RemainingWork:0.#}/{site.BuildTimeSeconds:0.#}초 · "
                    + (site.Started ? "취소하면 재료 100%를 바닥에 반환" : "취소하면 예약 해제·도착한 재료는 바닥에") + "(재개발 보상비는 반환 안 됨)";
            }
            if (Target is Kitchen kitchen) instruction.text = kitchen.IsTable ? "먹기 전용: 장수가 식사를 가져와 여기서 먹습니다."
                : (kitchen.Current != null ? $"조리 중: {kitchen.Current.name} {kitchen.Progress:P0}" : "조리 대기" + (kitchen.WaitReason != null ? $" · {kitchen.WaitReason}" : "")) + $" · 목록 {kitchen.Orders.Count}개 — 열어서 요리 목록 설정";
            if (Target is Gate gate) { label.text = gate.Open ? "성문 닫기" : "성문 열기"; instruction.text = gate.Open ? "열림: 모두 통과" : "닫힘: 성벽처럼 막음"; }
            if (Target is Decoration decor) instruction.text = $"품질 {new[] { "조잡", "보통", "정교", "걸작" }[decor.Quality]} · 반경 8m 환경 기분 +{decor.MoodBonus:0.#}";
            if (Target is RecreationSpot leisure) instruction.text = $"최소 {leisure.MinimumUsers}명 · 정원 {leisure.Seats}명 · 현재 {leisure.Users.Count}명"
                + (leisure.HasMinimumUsers ? " · 이용 가능" : " · 인원 대기");
            if (Target.GetComponent<WallFire>() is WallFire fire && fire.Burning)
                instruction.text = $"화재 · 소화 {fire.CaptureState().extinguishWork:0.0}/5초 · 장수 선택 후 우클릭으로 소화";
            if (Scout is ScoutPost scout)
            {
                action.gameObject.SetActive(true);
                status.text = scout.GetStatusLabel();
                label.text = scout.IsDispatched ? "정찰 중" : "정찰 파견"; action.interactable = !scout.IsDispatched;
                instruction.text = "동행 장수 1명을 골라 보냅니다. 돌아올 때까지 장수는 정착지를 비웁니다.";
            }
            else action.interactable = true;
            if (Target is StarterCropBed crop)
            {
                action.gameObject.SetActive(true);
                label.text = crop.Crop.planted ? "파종 완료" : "파종재 1로 심기";
                action.interactable = !crop.Crop.planted;
                instruction.text = $"성장 {crop.Crop.growth:0}/{StartSupplyRules.CropSeconds:0}초 · 급수 필요 · 파종재 1 소비";
            }
            if (Target is SupplyFacility supply)
            {
                action.gameObject.SetActive(supply.Kind == BuildingKind.ManualWell || UndergroundProduction.IsSolidDrill(supply.Kind) || supply.Kind == BuildingKind.FuelCharger);
                label.text = UndergroundProduction.IsSolidDrill(supply.Kind) ? "채굴 작업 지시" : supply.Kind == BuildingKind.ManualWell ? "취수 작업 지시" : "연료 드럼통 충전";
                status.text = supply.Supply.amount > 0 ? $"{((AntColony.Data.ResourceType)supply.Supply.fluid).DisplayName()} {supply.Supply.amount}/{SupplyFacility.Capacity}" : "비어 있음";
                instruction.text = supply.Kind == BuildingKind.ManualWell ? "수원 근처에서 장수 운반 작업 · 5초당 물 10 · 물 반출 후 운반 가능"
                    : supply.Kind == BuildingKind.FuelCharger ? "빈 드럼통 1 + 정제유 10 → 충전 드럼통 1" : "배관으로 연결 · 보관된 물은 바닥으로 반출 가능";
                if (UndergroundProduction.IsExtractor(supply.Kind))
                { status.text = UndergroundProduction.Status(supply); instruction.text = UndergroundProduction.Preview(supply.Kind, supply.Position); }
                else if (supply.GasNetwork) instruction.text = "기체 전용 배관/탱크 · 액체와 연결되지 않음";
                eatCorpse.GetComponentInChildren<Text>().text = "물 전량 반출";
            }
            if (Target is Processor industrial && industrial.Industrial)
            {
                status.text = industrial.WaitReason;
                instruction.text = "티타늄 광석 → 티타늄 → 합금 · 재료 배송 + 제작 담당\n가스1/묶음 · 200W · 완성품은 바닥에서 운반";
            }
            if (Target is PowerNode gasPower && gasPower.Kind == BuildingKind.GasGenerator)
            {
                status.text = gasPower.GasStatus;
                instruction.text = "기체 배관·전력망을 각각 연결하세요. 공급 단절 시 투입된 연료까지 발전합니다.";
            }
            if (Target is WildMonster animal)
            {
                status.text = $"{animal.Temperament} · 체력 {animal.CurrentHealth:0} · {(animal.HuntDesignated ? "사냥 지정됨" : "사냥 미지정")}";
                instruction.text = "사냥을 켠 장수만 지정된 야생 개체를 사냥합니다."; label.text = animal.HuntDesignated ? "사냥 취소" : "사냥 지정";
                // 목장 생물(2026-10-11): 개체 관리(포획·운반·돌봄·치료·도축 금지·위험 작업 장수·방생).
                if (animal.GetComponent<Critter>() is Critter critter)
                {
                    title.text = (critter.Tame ? "길들인 " : "야생 ") + critter.Info.name + (critter.Pen != null ? $" ({critter.Pen.Data.displayName})" : "");
                    status.text = $"{critter.StageName} · 배고픔 {critter.Hunger:0} · 체력 {critter.HealthFraction:P0}{(critter.Bound ? " · 결박됨" : "")}{(critter.CaptureDesignated ? " · 포획 지정됨" : "")}";
                    instruction.text = critter.TransportTo != null ? $"운반 요청: {critter.TransportTo.Data.displayName}" : "포획하면 현장에 결박됩니다. 개체 관리에서 운반할 우리를 고르세요.";
                    label.text = "개체 관리"; action.interactable = true;
                }
            }
            title.text += $"  <size=14><color=#b1b3a7>우선 {WorkPriorities.Level(Target)}</color>{(WorkPriorities.Yellow(Target) ? "  <color=#f2c94c>노란 경보</color>" : "")}</size>";
        }
        private static string ResidentNames(Dormitory dorm)
        {
            var names = dorm.Residents.Where(c => c != null && !c.IsDead && c.IsColonyMember).Select(c => c.CommanderName).ToArray();
            return names.Length > 0 ? string.Join(" · ", names) : "아직 없음 (잠자리에 들 때 자동 배정)";
        }
        private static string RoomDescription(BuildingBase building)
        {
            var room = RoomSystem.RoomAt(building.Position);
            return (room == null ? "방 밖 — 벽과 문으로 공간을 둘러싸면 방이 됩니다."
                : $"방: {room.Title} · 등급 {room.GradeName} · {room.Cells.Count}칸") + "\n" + SpaceQuality.Describe(building.Position);
        }
        public static void ShowAssignments()
        {
            if (Target is Dormitory dorm) ToastManager.Show("숙소 배정: " + ResidentNames(dorm));
            else if (Target != null)
            {
                var panel = AntColony.Core.SceneObjects.Any<WorkTargetPanel>();
                if (panel != null) ToastManager.Show(panel.status.text + "\n" + panel.instruction.text);
            }
        }
        public static void ShowRoomInfo() { if (Target is BuildingBase building) ToastManager.Show(RoomDescription(building)); }

        public static ScoutPost Scout => Target != null ? Target.GetComponent<ScoutPost>() : null;
        private void Act()
        {
            if (Target is UndergroundScanner scanner) { GameMenuController.Instance?.ShowSurvey(scanner); return; }
            if (Target is StarterCropBed crop) { if (!crop.Plant()) ToastManager.Show("파종재 보유량과 파종 상태를 확인하세요."); return; }
            if (Target is SupplyFacility supply)
            {
                if (supply.Kind == BuildingKind.FuelCharger) { if (!supply.FillFuelDrum()) ToastManager.Show("빈 드럼통 1개와 배관망의 정제유 10이 필요합니다."); return; }
                if (supply.Kind == BuildingKind.ManualWell || UndergroundProduction.IsSolidDrill(supply.Kind))
                {
                    foreach (var commander in AntUnitBase.Active.OfType<CommanderAnt>().OrderBy(c => (c.Position - supply.Position).sqrMagnitude))
                        if (commander.StartService(supply, UndergroundProduction.IsSolidDrill(supply.Kind) ? CommanderJobs.Gathering : CommanderJobs.Hauling)) return;
                    ToastManager.Show("취수 작업이 가능한 평시 장수가 없습니다.");
                }
                return;
            }
            if (Scout is ScoutPost post) { GameMenuController.Instance?.ShowScout(post); return; }
            if (Target.GetComponent<FarmPlot>() is FarmPlot farmPlot) { GameMenuController.Instance?.ShowFarm(farmPlot); return; }
            if (Target is CompostBin compost) { GameMenuController.Instance?.ShowCompost(compost); return; }
            if (Target.GetComponent<PrisonerCamp>() is PrisonerCamp campAct) { GameMenuController.Instance?.ShowPrisoners(campAct); return; }
            if (Target is Tombstone tomb) { GameMenuController.Instance?.ShowMemorial(tomb); return; }
            if (Target is Infirmary bed) { if (bed.Patients.Count > 0) GameMenuController.Instance?.ShowHealth(bed.Patients[0]); return; }
            if (Target is Corpse corpse) corpse.Priority = !corpse.Priority;
            if (Target is ResourceNode node)
            {
                if (node.NeedsDesignation) { if (node.WorkRequested) node.CancelWork(); else if (!node.RequestWork()) ToastManager.Show("작업 지정 불가: 채집 금지·대상 상태를 확인하세요."); }
                else node.GatheringForbidden = !node.GatheringForbidden;
            }
            if (Target is Workshop shop) GameMenuController.Instance?.ShowWorkshop(shop);
            if (Target is Processor processor) GameMenuController.Instance?.ShowProcessor(processor);
            if (Target is Kitchen cook && !cook.IsTable) GameMenuController.Instance?.ShowKitchen(cook);
            if (Target is WildMonster critterMonster && critterMonster.GetComponent<Critter>() is Critter critter) GameMenuController.Instance?.ShowCritter(critter);
            else if (Target is WildMonster animal && animal.Huntable) animal.HuntDesignated = !animal.HuntDesignated;
            if (Target is Ranch ranch) GameMenuController.Instance?.ShowRanch(ranch);
            if (Target is RanchFacility feeder && feeder.IsFeeder) GameMenuController.Instance?.ShowFeeder(feeder);
            if (Target is RanchFacility table && table.IsButcher) GameMenuController.Instance?.ShowButcher(table);
            if (Target is RanchFacility clinic && clinic.IsClinic && clinic.Patient is Critter patient) GameMenuController.Instance?.ShowCritter(patient);
            if ((Target is Storage || Target is Stockpile) && AntColony.Core.ResourceManager.Instance?.BinOf((BuildingBase)Target) is AntColony.Core.StoreBin store) GameMenuController.Instance?.ShowStorage(store);
            if (Target is Gate gate) gate.SetOpen(!gate.Open);
            if (Target is Barracks barracks) barracks.TryUpgrade();
            if (Target is DigSite dig) dig.TryExpand();
            if (Target is BuildingConstructionSite site) { site.Cancel(); Clear(); }
        }
        private void EatCorpse()
        {
            if (Target is ResourceNode natural && natural.NeedsDesignation) { natural.GatheringForbidden = !natural.GatheringForbidden; return; }
            if (Target is SupplyFacility supply)
            { if (!supply.Unload(AntColony.Data.ResourceType.Water, supply.Supply.amount)) ToastManager.Show("반출할 물이 없습니다."); return; }
            if (!(Target is Corpse corpse)) return;
            foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>().OrderBy(c => (c.Position - corpse.Position).sqrMagnitude))
            {
                if (corpse.GetComponent<ResourceNode>() is ResourceNode meat)
                {
                    if (!c.CivilianWorkReady || !c.CanReceiveOrders || c.IsWorking || c.IsCarrying || c.CorpseTarget != null) continue;
                    c.CommandGather(meat); if (c.CurrentResourceNode == meat) return;
                }
                else if (c.StartCorpseWork(corpse, true)) return;
            }
            if (!corpse.Edible) { ToastManager.Show("사냥 식량을 운반할 수 있는 평시 장수가 없습니다."); return; }
            ToastManager.Show("시체에 접근 가능한 평시 동족 포식 장수가 없습니다.");
        }
    }
}
