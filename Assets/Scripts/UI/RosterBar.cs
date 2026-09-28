using System.Linq;
using AntColony.Camera;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD v2 상단 가운데 장수 바(림월드 정착민 바): 초상화 한 줄, 12칸씩 좌우 스크롤.
    // 클릭 = 선택, 더블클릭 = 카메라 이동. 칸 아래 기분 막대, 우상단 하는 일/경고 아이콘, 원정·출전은 흐리게.
    public sealed class RosterBar : MonoBehaviour
    {
        public const int Visible = 12;
        private const float Cell = 32f, Gap = 2f, DoubleClick = .35f;

        private sealed class Slot { public Button button; public Text letter; public RectTransform mood; public Image moodFill; public RawImage icon; public Text alert; public CanvasGroup group; public MenuTooltip tip; }
        private readonly Slot[] slots = new Slot[Visible];
        private RectTransform root;
        private Text count;
        private int offset;
        private CommanderAnt lastClicked;
        private float lastClickTime;

        public static CommanderAnt[] Commanders => GameMenuController.SortedCommanders().Where(c => c != null && c.IsColonyMember && !c.IsDead).ToArray();
        public int Offset => offset;

        public static RosterBar Create(Transform canvas)
        {
            var width = 20 + 4 + Visible * (Cell + Gap) + 4 + 20 + 44;
            var root = MenuTheme.Rect("RosterBar", canvas);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 1);
            root.sizeDelta = new Vector2(width, 36); root.anchoredPosition = new Vector2(-10, -2);
            var bar = root.gameObject.AddComponent<RosterBar>(); bar.root = root;
            root.gameObject.AddComponent<CanvasGroup>();
            Arrow(root, "Roster Prev", "<", 0, () => bar.Scroll(-Visible));
            for (var i = 0; i < Visible; i++) bar.slots[i] = bar.Portrait(i, 24 + i * (Cell + Gap));
            Arrow(root, "Roster Next", ">", 24 + Visible * (Cell + Gap) + 1, () => bar.Scroll(Visible));
            bar.count = MenuTheme.Text(root, "", 11);
            var r = bar.count.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, .5f);
            r.anchoredPosition = new Vector2(24 + Visible * (Cell + Gap) + 26, 0); r.sizeDelta = new Vector2(44, 20);
            bar.count.color = MenuTheme.Dim; bar.count.font = MenuTheme.NumberFont;
            return bar;
        }

        public void Scroll(int delta)
        {
            var total = Commanders.Length;
            offset = Mathf.Clamp(offset + delta, 0, Mathf.Max(0, total - Visible));
        }

        // 검사·클릭 공용: 해당 칸의 장수를 누른 것과 같다.
        public void Click(int index)
        {
            var list = Commanders;
            if (index + offset >= list.Length) return;
            var c = list[index + offset];
            var selection = FindFirstObjectByType<SelectionManager>();
            var selectable = c.GetComponent<SelectableObject>();
            if (selection != null && selectable != null) selection.SelectOnly(selectable);
            if (c == lastClicked && Time.unscaledTime - lastClickTime <= DoubleClick)
                FindFirstObjectByType<IsometricCameraController>()?.FocusOn(c.Position);
            lastClicked = c; lastClickTime = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            var visible = GameSession.Exists && GameSession.Instance.GameStarted && !GameMenuController.BlocksInput;
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = visible ? 1 : 0; group.blocksRaycasts = group.interactable = visible;
            if (!visible) return;
            var list = Commanders;
            offset = Mathf.Clamp(offset, 0, Mathf.Max(0, list.Length - Visible));
            var selected = SelectedUnitPanel.FindSingleSelectedCommander(FindFirstObjectByType<SelectionManager>());
            for (var i = 0; i < Visible; i++)
            {
                var s = slots[i];
                var has = i + offset < list.Length;
                s.button.gameObject.SetActive(has);
                if (!has) continue;
                var c = list[i + offset];
                s.letter.text = Initial(c.CommanderName);
                s.letter.color = c == selected ? MenuTheme.TextColor : MenuTheme.Muted;
                s.button.GetComponent<Outline>().effectColor = c == selected ? MenuTheme.Accent : MenuTheme.Line;
                var mood = Mathf.Clamp01(c.Mood / 100f);
                s.mood.anchorMax = new Vector2(mood, 1);
                s.moodFill.color = c.Mood <= 20 ? MenuTheme.Danger : c.Mood <= CommanderOverhead.MoodWarning ? MenuTheme.Accent : MenuTheme.Hp;
                var activity = CommanderOverhead.Activity(c);
                var alert = CommanderOverhead.MoodAlert(c);
                s.alert.gameObject.SetActive(alert);
                s.icon.texture = ActivityIcons.Get(activity);
                s.icon.gameObject.SetActive(!alert && s.icon.texture != null);
                s.group.alpha = c.IsDeployed || c.IsAwayFromHome ? .55f : 1f;
                s.tip.Message = $"{c.CommanderName} · {(c.IsAwayFromHome ? "원정 중" : activity.Length > 0 ? activity : "대기")}{(c.IsNocturnal ? " (야행성)" : "")}"
                    + $" · 기분 {c.Mood:0}{(alert ? " 경고" : "")}{(c.SleepsRough ? " · 노숙" : "")}";
            }
            count.text = list.Length > Visible ? $"{offset + 1}-{Mathf.Min(offset + Visible, list.Length)}/{list.Length}" : list.Length.ToString();
        }

        // 초상 에셋 전까지 이름 첫 글자. 자동 이름(「Commander 7」)은 번호로 구분한다.
        public static string Initial(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var last = name.Substring(name.LastIndexOf(' ') + 1);
            return last.Length > 0 && last.All(char.IsDigit) ? last : name.Substring(0, 1);
        }

        private Slot Portrait(int index, float x)
        {
            var rect = MenuTheme.Rect("Roster " + index, root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(x, 0); rect.sizeDelta = new Vector2(Cell, Cell);
            rect.gameObject.AddComponent<Image>();
            rect.gameObject.AddComponent<Outline>().effectColor = MenuTheme.Line;
            var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
            button.onClick.AddListener(() => Click(index));
            var s = new Slot { button = button, group = rect.gameObject.AddComponent<CanvasGroup>(), tip = rect.gameObject.AddComponent<MenuTooltip>() };
            s.letter = MenuTheme.Text(rect, "", 12); MenuTheme.Stretch(s.letter.rectTransform);
            s.letter.alignment = TextAnchor.MiddleCenter; s.letter.fontStyle = FontStyle.Bold;
            var track = MenuTheme.Rect("Mood", rect);
            track.anchorMin = new Vector2(0, 0); track.anchorMax = new Vector2(1, 0); track.pivot = new Vector2(.5f, 0);
            track.offsetMin = new Vector2(2, 2); track.offsetMax = new Vector2(-2, 5);
            track.gameObject.AddComponent<Image>().color = MenuTheme.Well;
            s.mood = MenuTheme.Rect("Fill", track); MenuTheme.Stretch(s.mood);
            s.moodFill = s.mood.gameObject.AddComponent<Image>(); s.moodFill.raycastTarget = false;
            var icon = MenuTheme.Rect("Activity", rect);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(1, 1);
            icon.sizeDelta = new Vector2(12, 12); icon.anchoredPosition = new Vector2(-1, -1);
            s.icon = icon.gameObject.AddComponent<RawImage>(); s.icon.raycastTarget = false;
            s.alert = MenuTheme.Text(rect, "!", 12);
            var a = s.alert.rectTransform; a.anchorMin = a.anchorMax = a.pivot = new Vector2(1, 1);
            a.sizeDelta = new Vector2(12, 14); a.anchoredPosition = new Vector2(-1, 0);
            s.alert.color = MenuTheme.Danger; s.alert.fontStyle = FontStyle.Bold; s.alert.alignment = TextAnchor.MiddleCenter;
            return s;
        }

        private static void Arrow(RectTransform root, string name, string label, float x, System.Action action)
        {
            var rect = MenuTheme.Rect(name, root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = new Vector2(x, 0); rect.sizeDelta = new Vector2(20, 30);
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
            button.onClick.AddListener(() => action());
            var text = MenuTheme.Text(rect, label, 13); MenuTheme.Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleCenter; text.color = MenuTheme.Muted;
        }
    }
}
