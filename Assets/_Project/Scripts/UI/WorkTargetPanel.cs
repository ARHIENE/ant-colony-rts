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
            FindFirstObjectByType<AntColony.Units.SelectionManager>()?.ClearSelection();
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
            eatCorpse.gameObject.SetActive(Target is Corpse edible && (edible.Edible || edible.GetComponent<ResourceNode>() != null));
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
            action.gameObject.SetActive(Target is ResourceNode || Target is Workshop || Target is Processor || Target is Ranch || Target is RanchFacility f0 && f0.IsFeeder || Target is BuildingConstructionSite || Target is WildMonster || Target is Gate || Target is Barracks || Target is DigSite);
            if (Target is BuildingBase placed && !(Target is Gate)) { var room = RoomSystem.RoomAt(placed.Position); instruction.text = (room == null ? "방 밖(효과 적음)" : $"방: {room.Title} · 등급 {room.GradeName} ({room.Cells.Count}칸)") + " · " + instruction.text; }
            if (Target is Dormitory dorm)
            {
                status.text = $"침대 {dorm.Beds}개 · 배정 {dorm.Residents.Count(c => c != null && !c.IsDead && c.IsColonyMember)}명"
                    + (HudOverview.BedShortage > 0 ? $"\n<color=#ef955f>군체 전체 침대 {HudOverview.BedShortage}개 부족</color>" : "\n군체 전체 침대 충분");
                instruction.text = RoomDescription(dorm) + "\n배정: " + ResidentNames(dorm);
            }
            if (Target is ResourceNode node) { label.text = node.GatheringForbidden ? "채집 허용" : "채집 금지"; instruction.text = $"남은 자원 {node.AmountRemaining:0.#} · {(node.IsLooseCargo ? "운반" : "채집")} 작업"; }
            if (Target is Workshop shop) { label.text = "제작 대기열"; instruction.text = $"제작 대기 {shop.Jobs.Count}건"; }
            if (Target is Processor processor) { label.text = "가공 목록"; instruction.text = processor.Current != null ? $"가공 중 {processor.Current.name} {processor.Progress:P0} · 목록 {processor.Orders.Count}건" : $"대기 · 목록 {processor.Orders.Count}건"; }
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
                else if (facility.IsClinic) instruction.text = facility.Patient is Critter p ? $"{p.Info.name} " + (p.SurgeryStarted ? $"수술 {p.SurgeryProgress:0.#}/{GameBalance.CritterSurgerySeconds}초" : p.TransportTo == null ? "— 복귀할 우리 필요(개체를 선택해 지정)" : "복귀 대기") : "비어 있음 · 중상 동물 수술용(동물 수술 키트 필요)";
                else if (facility.IsButcher) instruction.text = $"해체 대기 사체 {facility.Carcasses}구 · 요리 작업 장수가 해체합니다.";
            }
            if (Target is Barracks barracks) label.text = "병영 강화";
            if (Target is DigSite) label.text = "굴착 확장";
            if (Target is BuildingConstructionSite site) { label.text = "건설 취소"; instruction.text = $"남은 작업 {site.RemainingWork:0.#}/{site.BuildTimeSeconds:0.#}초 · 취소하면 건설비 전액 반환(재개발 보상비는 반환 안 됨)"; }
            if (Target is Kitchen kitchen) instruction.text = kitchen.IsTable ? "먹기 전용: 장수가 식사를 가져와 여기서 먹습니다."
                : $"비축 식사 {kitchen.Meals.meals.Count}/{Kitchen.Capacity} · 조리 {kitchen.Meals.progress:0.#}/{Kitchen.CookSeconds}초";
            if (Target is Gate gate) { label.text = gate.Open ? "성문 닫기" : "성문 열기"; instruction.text = gate.Open ? "열림: 모두 통과" : "닫힘: 성벽처럼 막음"; }
            if (Target is Decoration decor) instruction.text = $"품질 {new[] { "조잡", "보통", "정교", "걸작" }[decor.Quality]} · 반경 8m 환경 기분 +{decor.MoodBonus:0.#}";
            if (Scout is ScoutPost scout)
            {
                action.gameObject.SetActive(true);
                status.text = scout.GetStatusLabel();
                label.text = scout.IsDispatched ? "정찰 중" : "정찰 파견"; action.interactable = !scout.IsDispatched;
                instruction.text = "동행 장수 1명을 골라 보냅니다. 돌아올 때까지 장수는 소굴을 비웁니다.";
            }
            else action.interactable = true;
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
            title.text += $"  <size=14><color=#968976>우선 {WorkPriorities.Level(Target)}</color>{(WorkPriorities.Yellow(Target) ? "  <color=#f2c94c>노란 경보</color>" : "")}</size>";
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
                var panel = FindFirstObjectByType<WorkTargetPanel>();
                if (panel != null) ToastManager.Show(panel.status.text + "\n" + panel.instruction.text);
            }
        }
        public static void ShowRoomInfo() { if (Target is BuildingBase building) ToastManager.Show(RoomDescription(building)); }

        public static ScoutPost Scout => Target != null ? Target.GetComponent<ScoutPost>() : null;
        private void Act()
        {
            if (Scout is ScoutPost post) { GameMenuController.Instance?.ShowScout(post); return; }
            if (Target is Corpse corpse) corpse.Priority = !corpse.Priority;
            if (Target is ResourceNode node) node.GatheringForbidden = !node.GatheringForbidden;
            if (Target is Workshop shop) GameMenuController.Instance?.ShowWorkshop(shop);
            if (Target is Processor processor) GameMenuController.Instance?.ShowProcessor(processor);
            if (Target is WildMonster critterMonster && critterMonster.GetComponent<Critter>() is Critter critter) GameMenuController.Instance?.ShowCritter(critter);
            else if (Target is WildMonster animal && animal.Huntable) animal.HuntDesignated = !animal.HuntDesignated;
            if (Target is Ranch ranch) GameMenuController.Instance?.ShowRanch(ranch);
            if (Target is RanchFacility feeder && feeder.IsFeeder) GameMenuController.Instance?.ShowFeeder(feeder);
            if (Target is Gate gate) gate.SetOpen(!gate.Open);
            if (Target is Barracks barracks) barracks.TryUpgrade();
            if (Target is DigSite dig) dig.TryExpand();
            if (Target is BuildingConstructionSite site) { site.Cancel(); Clear(); }
        }
        private void EatCorpse()
        {
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
