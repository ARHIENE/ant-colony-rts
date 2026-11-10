using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD v2 우상단(자원 바로 아래) 318px 판: 「3년 가을 · 낮 / 밤까지 m:ss」 + 낮밤 진행 바 + 속도(⏸ 1 2 3 5).
    public sealed class HudClock : MonoBehaviour
    {
        private static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };
        public static string CalendarLabel => $"{GameCalendar.Year}년 {SeasonNames[(int)GameCalendar.CurrentSeason]} {GameCalendar.DayOfSeason}일";
        public static string DayLabel => $"{GameCalendar.Year}년 {SeasonNames[(int)GameCalendar.CurrentSeason]} · {(GameCalendar.IsNight ? "밤" : "낮")} · {AntColony.Map.WeatherSystem.Name(AntColony.Map.WeatherSystem.Current)}{(BiomeRules.Current != MapBiome.None ? " · " + BiomeRules.Name(BiomeRules.Current) : "")}";

        private RectTransform root, marker;
        private Text day, left;
        private readonly Button[] speeds = new Button[5];

        public static HudClock Create(Transform canvas)
        {
            var root = MenuTheme.Panel(canvas, "HudClock", Vector2.zero, new Vector2(240, 36), new Vector2(12, 0));
            var clock = root.gameObject.AddComponent<HudClock>(); clock.root = root; root.GetComponent<Image>().color = Color.clear; root.Find("Accent").gameObject.SetActive(false);
            root.gameObject.AddComponent<CanvasGroup>();
            clock.day = Label(root, "", 12, new Vector2(8, -5), new Vector2(220, 20), MenuTheme.TextColor);
            clock.day.fontStyle = FontStyle.Normal; clock.day.fontSize = 11;
            clock.day.raycastTarget = true;
            clock.day.gameObject.AddComponent<MenuTooltip>();
            clock.left = Label(root, "", 12, new Vector2(8, -26), new Vector2(120, 20), MenuTheme.Dim);
            clock.left.alignment = TextAnchor.MiddleLeft; clock.left.gameObject.SetActive(false);
            clock.day.rectTransform.sizeDelta = new Vector2(116, 26);
            // 낮 10 : 밤 5 비율의 막대와 현재 시각 표시.
            var bar = Box(root, "DayNightBar", new Vector2(8, -46), new Vector2(160, 4), MenuTheme.Well);
            bar.gameObject.SetActive(false);
            var d = Box(bar, "Day", Vector2.zero, new Vector2(160f * 2 / 3, 4), MenuTheme.Hex(0x6b5a3a));
            var n = Box(bar, "Night", new Vector2(160f * 2 / 3, 0), new Vector2(160f / 3, 4), MenuTheme.Hex(0x2a3346));
            d.GetComponent<Image>().raycastTarget = n.GetComponent<Image>().raycastTarget = false;
            clock.marker = Box(bar, "Now", new Vector2(0, 2), new Vector2(2, 8), MenuTheme.TextColor);

            var group = Box(root, "Speed", new Vector2(116, -2), new Vector2(136, 32), Color.clear);
            string[] labels = { "Ⅱ", "1×", "2×", "3×", "5×" };
            string[] names = { "Pause / Play", "1x", "2x", "3x", "5x" };
            string[] tips = { "일시정지 (Space)", "1배속 (F5)", "2배속 (F6)", "3배속 (F7)", "5배속 (F8)" };
            for (var i = 0; i < 5; i++)
            {
                var index = i;
                var rect = Box(group, names[i], new Vector2(i * 27, -2), new Vector2(26, 30), Color.white);
                var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button);
                button.onClick.AddListener(() =>
                {
                    var menu = GameMenuController.Instance; if (menu == null) return;
                    if (index == 0) menu.ToggleSimulation(); else menu.SetSpeed(GameMenuController.Speeds[index - 1]);
                });
                var text = Label(rect, labels[i], 12, Vector2.zero, new Vector2(26, 30), MenuTheme.Muted);
                text.font = MenuTheme.Font; text.fontSize = 11; text.alignment = TextAnchor.MiddleCenter;
                rect.gameObject.AddComponent<MenuTooltip>().Message = tips[i];
                clock.speeds[i] = button;
                if (i == 4) rect.gameObject.SetActive(false); // 5x remains available through its existing shortcut.
            }
            return clock;
        }

        private void LateUpdate()
        {
            var visible = GameSession.Exists && GameSession.Instance.GameStarted && !GameMenuController.BlocksInput;
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = visible ? 1 : 0; group.blocksRaycasts = group.interactable = visible;
            if (!visible) return;
            bool small = ((RectTransform)root.parent).rect.width <= 760;
            root.anchoredPosition = new Vector2(small ? 6 : 12, 0); root.sizeDelta = new Vector2(small ? 181 : 219, 36);
            day.rectTransform.anchoredPosition = new Vector2(small ? 0 : 25, -7); day.rectTransform.sizeDelta = new Vector2(small ? 80 : 69, 22);
            var sun = HudChrome.Icon(root, "Calendar Sun", "Sun", MenuTheme.Hex(0xbfb19a)); sun.gameObject.SetActive(!small); MenuLayout.Place(sun.rectTransform, 0, 10, 16, 16);
            var speedGroup = (RectTransform)root.Find("Speed"); speedGroup.anchoredPosition = new Vector2(small ? 83 : 101, -4);
            for (int i = 0; i < 4; i++) { var r = (RectTransform)speeds[i].transform; r.anchoredPosition = new Vector2(i * (small ? 25 : 30), 0); r.sizeDelta = new Vector2(small ? 23 : 28, 29); MenuTheme.Stretch(speeds[i].GetComponentInChildren<Text>().rectTransform); }
            for (int i = 0; i < 4; i++)
            {
                var icon = HudChrome.Icon(speeds[i].transform, "Speed Icon", i == 0 ? "Pause" : i == 1 ? "Play" : "ChevronsRight", MenuTheme.Hex(0xcbc3b4));
                HudChrome.CenterIcon(icon, i == 0 ? 0 : -4, 0, 13);
                var label = speeds[i].GetComponentInChildren<Text>(); label.text = i == 0 ? "" : i.ToString(); label.rectTransform.offsetMin = new Vector2(14, 0);
            }
            day.text = CalendarLabel;
            day.GetComponent<MenuTooltip>().Message = DayLabel;
            var seconds = Mathf.CeilToInt(GameCalendar.SecondsUntilPhaseChange);
            left.text = $"{(GameCalendar.IsNight ? "낮" : "밤")}까지 {seconds / 60}:{seconds % 60:00}";
            marker.anchoredPosition = new Vector2(160f * GameCalendar.TimeOfDay / GameCalendar.SecondsPerDay - 1, 2);
            for (var i = 0; i < speeds.Length; i++)
            {
                var on = i == 0 ? Time.timeScale == 0 : Time.timeScale == GameMenuController.Speeds[i - 1];
                var colors = speeds[i].colors; var want = on ? MenuTheme.Hex(0x625c4f) : Color.clear;
                if (colors.normalColor != want) { colors.normalColor = want; MenuTheme.SetColorsInstant(speeds[i], colors); }
                speeds[i].GetComponentInChildren<Text>().color = on ? MenuTheme.Hex(0xfff4df) : MenuTheme.Muted;
            }
        }

        private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = MenuTheme.Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static Text Label(Transform parent, string value, int size, Vector2 position, Vector2 box, Color color)
        {
            var text = MenuTheme.Text(parent, value, size);
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = box; text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
    }
}
