using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 장수 획득 경로 3종(번식·영입·포로)의 현황판. HUD 캔버스에 붙어 SelectedUnitPanel과 같은 방식으로 동작한다.
    // 건물을 여러 채 지을 수 있으므로 수용소·파견소는 버튼으로 골라 쓴다(자동으로 갈아타지 않는다).
    public class CommanderAcquisitionPanel : MonoBehaviour
    {
        // 건물 목록 훑기는 매 프레임 돌 만큼 싸지 않으므로 이 간격으로만 다시 찾는다.
        // 다만 "고른 건물이 사라졌는가"는 매 프레임 확인한다(파괴 즉시 조작이 막혀야 한다).
        private const float LookupInterval = .5f;
        private const float PanelWidth = 400f;
        // 칸 높이는 13pt 기준 줄 수에 맞춰 잡았다(줄바꿈까지 감안해 각 칸이 넘치지 않는 크기).
        private const float PanelHeight = 360f;
        // 버튼 두 줄이 차지하는 높이. 위쪽 글자 칸은 여기까지만 내려올 수 있다.
        private const float ButtonRowsHeight = 70f;
        private const float TextWidth = 376f;

        private GameObject panel;
        private Text nurseryText;
        private Text scoutText;
        private Text prisonerHeaderText;
        private Text prisonerDetailText;
        private Text feedbackText;
        private Text dispatchLabel;
        private Text persuadeLabel;
        private Button cycleCampButton;
        private Button cycleScoutButton;
        private Button dispatchButton;
        private Button nextPrisonerButton;
        private Button persuadeButton;
        private Button executeButton;

        private NurseryChamber nursery;
        private ScoutPost scout;
        private PrisonerCamp camp;
        private int campCount;
        private int scoutCount;
        private int campIndex;
        private int scoutIndex;
        private float lookupTimer;

        // 인덱스가 아니라 포로 자체를 들고 있는다. 앞쪽 포로가 탈출해도 선택이 다른 포로로 옮겨가지 않는다.
        private Prisoner selected;
        private string feedback = "";

        // 살아 있고 켜져 있는 건물만 조작 대상이다. 파괴·비활성 즉시 null이 되어 버튼과 핸들러가 함께 막힌다.
        public PrisonerCamp Camp => camp != null && camp.isActiveAndEnabled ? camp : null;
        public ScoutPost Scout => scout != null && scout.isActiveAndEnabled ? scout : null;
        public Prisoner SelectedPrisoner => selected;
        public string Feedback => feedback;
        public RectTransform PanelRect => panel != null ? (RectTransform)panel.transform : null;

        private void Start()
        {
            var background = CreateImage("CommanderAcquisitionPanel", transform, MenuTheme.Background);
            panel = background.gameObject;
            // 패널 위 클릭이 뒤쪽 지형으로 새어나가 엉뚱한 이동 명령이 되지 않게 막는다.
            background.raycastTarget = true;
            var rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-10f, 138f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            CreateText("Title", rect, -8f, 20f, 15).text = "장수 영입";
            nurseryText = CreateText("Nursery", rect, -32f, 36f, 13);
            scoutText = CreateText("Scout", rect, -74f, 36f, 13);
            prisonerHeaderText = CreateText("PrisonerHeader", rect, -116f, 20f, 13);
            prisonerDetailText = CreateText("PrisonerDetail", rect, -140f, 56f, 13);
            feedbackText = CreateText("Feedback", rect, -202f, 34f, 13);

            cycleCampButton = CreateButton(rect, 12f, 44f, "Next Prison", CycleCamp, out _);
            cycleScoutButton = CreateButton(rect, 140f, 44f, "Next Post", CycleScout, out _);
            dispatchButton = CreateButton(rect, 268f, 44f, "Dispatch", Dispatch, out dispatchLabel);
            nextPrisonerButton = CreateButton(rect, 12f, 10f, "Next POW", SelectNextPrisoner, out _);
            persuadeButton = CreateButton(rect, 140f, 10f, "Persuade", Persuade, out persuadeLabel);
            executeButton = CreateButton(rect, 268f, 10f, "Execute", ExecuteSelected, out _);
            panel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (panel == null) return;

            lookupTimer -= Time.deltaTime;
            if (lookupTimer <= 0f)
            {
                lookupTimer = LookupInterval;
                // 완공된(활성) 건물만 대상이다. 배치용 템플릿은 비활성이라 잡히지 않는다.
                nursery = NurseryChamber.Primary;
                var camps = FindActive<PrisonerCamp>();
                var posts = FindActive<ScoutPost>();
                campCount = camps.Count;
                scoutCount = posts.Count;
                // 고른 건물이 사라졌을 때만 갈아탄다. 살아 있으면 플레이어의 선택을 유지한다.
                if (Camp == null) camp = camps.Count > 0 ? camps[0] : null;
                if (Scout == null) scout = posts.Count > 0 ? posts[0] : null;
                campIndex = camps.IndexOf(camp);
                scoutIndex = posts.IndexOf(scout);
            }

            var activeCamp = Camp;
            var activeScout = Scout;
            panel.SetActive(nursery != null || activeScout != null || activeCamp != null);
            if (!panel.activeSelf) return;

            nurseryText.text = nursery != null ? nursery.GetStatusLabel() : "양육실: 건설 필요";
            scoutText.text = activeScout != null
                ? $"{activeScout.GetStatusLabel()}   (초소 {scoutIndex + 1}/{scoutCount})"
                : "정찰 초소: 건설 필요";
            dispatchLabel.text = activeScout != null && activeScout.IsDispatched ? "정찰 중" : "정찰 파견";
            persuadeLabel.text = activeCamp != null ? $"회유 · 식량 {activeCamp.PersuadeFoodCost}" : "회유";
            RefreshPrisoners(activeCamp);
            feedbackText.text = feedback;

            cycleCampButton.interactable = campCount > 1;
            cycleScoutButton.interactable = scoutCount > 1;
            dispatchButton.interactable = activeScout != null && !activeScout.IsDispatched;
            nextPrisonerButton.interactable = activeCamp != null && activeCamp.Count > 1;
            // 식량 부족은 버튼을 막지 않는다. 눌렀을 때 이유를 알려주는 편이 낫다.
            persuadeButton.interactable = activeCamp != null && selected != null;
            executeButton.interactable = activeCamp != null && selected != null;
        }

        // 포로는 한 번에 한 명만 자세히 보여준다. 전원을 늘어놓으면 패널을 넘쳐 버튼을 가린다.
        private void RefreshPrisoners(PrisonerCamp activeCamp)
        {
            if (activeCamp == null)
            {
                selected = null;
                prisonerHeaderText.text = "포로 수용소: 건설 필요";
                prisonerDetailText.text = "";
                return;
            }

            var prisoners = activeCamp.Prisoners;
            var index = IndexOf(prisoners, selected);
            if (index < 0)
            {
                selected = prisoners.Count > 0 ? prisoners[0] : null;
                index = prisoners.Count > 0 ? 0 : -1;
            }

            prisonerHeaderText.text = $"수용 {activeCamp.Count}/{activeCamp.Capacity}명"
                + (campCount > 1 ? $" (수용소 {campIndex + 1}/{campCount})" : "")
                + (selected != null ? $"   포로 {index + 1}/{prisoners.Count}" : "   포로 없음")
                + $"   합류 {activeCamp.RecruitedCount} · 처형 {activeCamp.ExecutedCount} · 탈출 {activeCamp.EscapedCount}";

            prisonerDetailText.text = selected == null
                ? ""
                : $"{selected.Name}\n"
                + $"{DisplayNames.Personality(selected.Traits.Personality)} · 기분 {PrisonerCamp.MoodOf(selected):0} · 피로 {selected.PersonalState?.sleep.fatigue ?? 0:0} · 시도 {selected.PersuadeAttempts}회\n"
                // 실제 판정은 시도 횟수를 올린 뒤에 굴리므로 다음 시도 기준 확률을 보여준다.
                + $"다음 회유 {activeCamp.NextPersuadeChance(selected):P0} · 식량 {activeCamp.PersuadeFoodCost}";
        }

        public void CycleCamp()
        {
            var camps = FindActive<PrisonerCamp>();
            if (camps.Count == 0)
            {
                camp = null;
                feedback = "포로 수용소를 먼저 건설하세요.";
                return;
            }
            camp = camps[(camps.IndexOf(Camp) + 1) % camps.Count];
            campCount = camps.Count;
            campIndex = camps.IndexOf(camp);
            selected = null;
        }

        public void CycleScout()
        {
            var posts = FindActive<ScoutPost>();
            if (posts.Count == 0)
            {
                scout = null;
                feedback = "정찰 초소를 먼저 건설하세요.";
                return;
            }
            scout = posts[(posts.IndexOf(Scout) + 1) % posts.Count];
            scoutCount = posts.Count;
            scoutIndex = posts.IndexOf(scout);
        }

        public void Dispatch()
        {
            var activeScout = Scout;
            if (activeScout == null)
            {
                feedback = "정찰 초소를 먼저 건설하세요.";
                return;
            }
            if (activeScout.IsDispatched)
            {
                feedback = "이미 정찰 중입니다. 귀환 후 다시 파견하세요.";
                return;
            }
            // TryDispatch는 이유를 구분해주지 않으므로, 먼저 확인해 어떤 조건이 막았는지 알려준다.
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(activeScout.DispatchFoodCost, 0))
            {
                feedback = $"식량 부족: 파견에 식량 {activeScout.DispatchFoodCost}이 필요합니다.";
                return;
            }
            GameMenuController.Instance?.ShowScout(activeScout); // 동행 장수는 정찰 파견 창에서 고른다.
        }

        public void SelectNextPrisoner()
        {
            var activeCamp = Camp;
            if (activeCamp == null || activeCamp.Count == 0) return;
            selected = activeCamp.Prisoners[(IndexOf(activeCamp.Prisoners, selected) + 1) % activeCamp.Count];
        }

        public void Persuade()
        {
            var activeCamp = Camp;
            if (activeCamp == null || selected == null)
            {
                feedback = "회유·처형할 포로를 먼저 선택하세요.";
                return;
            }
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(activeCamp.PersuadeFoodCost, 0))
            {
                feedback = $"식량 부족: 회유에 식량 {activeCamp.PersuadeFoodCost}이 필요합니다.";
                return;
            }
            var name = selected.Name;
            feedback = activeCamp.TryPersuade(selected) ? $"{name} 정착지 합류!" : $"{name} 회유 거절.";
        }

        public void ExecuteSelected()
        {
            var activeCamp = Camp;
            if (activeCamp == null || selected == null)
            {
                feedback = "회유·처형할 포로를 먼저 선택하세요.";
                return;
            }
            var name = selected.Name;
            feedback = activeCamp.Execute(selected) ? $"{name} 처형." : "회유·처형할 포로를 먼저 선택하세요.";
        }

        // 켜져 있는 건물만, 씬에 놓인 순서대로. 배치용 템플릿과 부서진 건물은 빠진다.
        private static List<T> FindActive<T>() where T : Behaviour
        {
            var found = new List<T>();
            foreach (var component in AntColony.Core.SceneObjects.All<T>())
                if (component.isActiveAndEnabled) found.Add(component);
            return found;
        }

        private static int IndexOf(IReadOnlyList<Prisoner> prisoners, Prisoner prisoner)
        {
            if (prisoner == null) return -1;
            for (var i = 0; i < prisoners.Count; i++)
                if (prisoners[i] == prisoner) return i;
            return -1;
        }

        private static Button CreateButton(Transform parent, float x, float y, string label,
            UnityEngine.Events.UnityAction action, out Text labelText)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(120f, 26f);
            go.GetComponent<Image>().color = MenuTheme.Plate2;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(action);

            labelText = CreateText("Label", rect, 0f, 26f, 13);
            labelText.text = DisplayNames.AcquisitionButton(label);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            labelText.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // 폭을 고정하고 줄바꿈·잘라내기를 켜 둔다. 긴 장수 이름이 패널 밖으로 흘러나가지 않는다.
        private static Text CreateText(string name, Transform parent, float top, float height, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = MenuTheme.Font;
            text.fontSize = fontSize;
            text.color = MenuTheme.TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, top);
            rect.sizeDelta = new Vector2(TextWidth, height);
            return text;
        }
    }
}
