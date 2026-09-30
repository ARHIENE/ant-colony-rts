using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
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
        private Slider slider;
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
            title = L.Label(root, "", 21, 16, 8, 780, 30);
            status = L.Label(root, "", 15, 16, 42, 780, 44);
            var track = L.Box(root, "WorkforceSlider", 16, 98, 500, 20, MenuTheme.Well);
            var handle = L.Box(track, "Handle", 0, 0, 16, 20, MenuTheme.Accent);
            slider = track.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.wholeNumbers = true; slider.minValue = 0; slider.maxValue = Workforce.Maximum;
            slider.onValueChanged.AddListener(v => { if (Target != null) Workforce.For(Target).Request((int)v); });
            instruction = L.Label(root, "", 12, 16, 126, 780, 42, MenuTheme.Muted);
            action = L.Button(root, "TargetAction", "", 544, 94, 246, 32, Act);
            eatCorpse = L.Button(root, "EatCorpse", "동족 포식 지시", 544, 136, 246, 32, EatCorpse);
            root.gameObject.SetActive(false);
        }
        private void OnDestroy() => Clear();
        private void LateUpdate()
        {
            bool visible = Target != null && Target.gameObject.activeInHierarchy && !BuildScreen.Picking;
            root.gameObject.SetActive(visible); if (!visible) return;
            eatCorpse.gameObject.SetActive(Target is Corpse edible && (edible.Edible || edible.GetComponent<ResourceNode>() != null));
            if (Target is Corpse corpse)
            {
                slider.gameObject.SetActive(false); action.gameObject.SetActive(true);
                title.text = corpse.Data.name + " 시체";
                var meat = corpse.GetComponent<ResourceNode>();
                status.text = $"{corpse.Data.count}구 · 소멸까지 {corpse.Data.remaining:0}초 · {(corpse.Handler != null ? corpse.Handler.CommanderName + $" 작업 {corpse.Handler.CorpseProgress:0.#}/5초" : "미처리")}"
                    + (meat != null ? $"\n운반 가능한 식량 {meat.AmountRemaining:0.#}" : "");
                instruction.rectTransform.sizeDelta = new Vector2(510, 42);
                instruction.text = "선택 장수 + 우클릭: 치우기 · Alt+우클릭: 동족 포식\n치우기 기본 5초. 사냥 사체는 우선 지정 시에만 자동 청소합니다.";
                action.GetComponentInChildren<Text>().text = corpse.Priority ? "치우기 우선 취소" : "치우기 우선";
                eatCorpse.GetComponentInChildren<Text>().text = meat != null ? "사냥 식량 운반" : "동족 포식 지시";
                return;
            }
            instruction.rectTransform.sizeDelta = new Vector2(780, 42);
            var workforce = Workforce.For(Target); workforce.Refresh();
            title.text = Target is BuildingBase b && b.Data != null ? b.Data.displayName : Target.name;
            slider.gameObject.SetActive(!(Target is WildMonster)); slider.SetValueWithoutNotify(workforce.Requested);
            status.text = $"인력 {workforce.Allocated}/{workforce.Limit} · 요청 {workforce.Requested} · 속도 ×{workforce.Multiplier:0.0}\n대기 개미 {AntPool.Instance?.Free ?? 0} · 장수 기술에 따라 상한이 달라집니다.";
            instruction.text = "작업 중인 장수가 없으면 인력은 대기 풀로 돌아갑니다. 설정은 이 대상에 저장됩니다.";
            var label = action.GetComponentInChildren<Text>();
            action.gameObject.SetActive(Target is ResourceNode || Target is Workshop || Target is WildMonster);
            if (Target is ResourceNode node) { label.text = node.GatheringForbidden ? "채집 허용" : "채집 금지"; instruction.text = $"남은 자원 {node.AmountRemaining:0.#} · {(node.IsLooseCargo ? "운반" : "채집")} 작업"; }
            if (Target is Workshop shop) { label.text = "제작 대기열"; instruction.text = $"제작 대기 {shop.Jobs.Count}건"; }
            if (Target is Kitchen kitchen) instruction.text = $"비축 식사 {kitchen.Meals.meals.Count}/{Kitchen.Capacity} · 조리 {kitchen.Meals.progress:0.#}/{Kitchen.CookSeconds}초";
            if (Target is Decoration decor) instruction.text = $"품질 {new[] { "조잡", "보통", "정교", "걸작" }[decor.Quality]} · 반경 8m 환경 기분 +{decor.MoodBonus:0.#}";
            if (Target is WildMonster animal)
            {
                status.text = $"{animal.Temperament} · 체력 {animal.CurrentHealth:0} · {(animal.HuntDesignated ? "사냥 지정됨" : "사냥 미지정")}";
                instruction.text = "사냥을 켠 장수만 지정된 야생 개체를 사냥합니다."; label.text = animal.HuntDesignated ? "사냥 취소" : "사냥 지정";
            }
        }
        private void Act()
        {
            if (Target is Corpse corpse) corpse.Priority = !corpse.Priority;
            if (Target is ResourceNode node) node.GatheringForbidden = !node.GatheringForbidden;
            if (Target is Workshop shop) GameMenuController.Instance?.ShowWorkshop(shop);
            if (Target is WildMonster animal && animal.Huntable) animal.HuntDesignated = !animal.HuntDesignated;
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
