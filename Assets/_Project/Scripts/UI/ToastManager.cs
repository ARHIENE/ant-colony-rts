using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    public enum ToastKind { Info, Warning, Hint }

    // 우상단 알림 스택. 위기 = 해결/닫기 전까지 고정, 경고 10초, 완료·행운(Info) = 설정값(기본 6초), 힌트(민트) 10초.
    // 실제 시간 기준, 마우스를 올리면 멈춘다. 동시 최대 5개, 같은 알림은 ×N으로 합치고 넘치면 +N건. 클릭 = 닫기.
    public sealed class ToastManager : MonoBehaviour
    {
        public const int MaxVisible = 5;
        public const float WarningSeconds = 10f, HintSeconds = 10f;
        public static readonly Color Mint = MenuTheme.Hex(0x6fd6b8);
        private sealed class Toast { public string key, message, actionLabel; public System.Action action; public ToastKind kind; public int count = 1; public float remaining; public bool crisis; }

        private static ToastManager instance;
        private readonly List<Toast> toasts = new List<Toast>();
        private readonly Dictionary<string, (System.Action action, string label)> locations = new Dictionary<string, (System.Action, string)>();
        private readonly Dictionary<string, string> crises = new Dictionary<string, string>();
        private readonly Dictionary<string, string> dismissed = new Dictionary<string, string>();
        private RectTransform panel;
        private bool hovered, dirty = true;

        public static int Count => instance != null ? instance.toasts.Count : 0;
        public static bool Paused => instance != null && instance.hovered;
        // 검사·다른 UI용: 현재 화면에 보이는 줄(위기 포함, 최대 5 + 넘침 줄).
        public static IReadOnlyList<string> VisibleLines => instance != null ? instance.Lines() : new List<string>();

        private void Awake()
        {
            instance = this;
            var canvas = MenuTheme.Canvas("Notifications", transform, 200);
            // HUD v2: 달력·속도 판(HudClock) 아래 오른쪽 318px 판넬, 위에서 아래로 쌓인다. 비면 숨긴다.
            panel = MenuTheme.Panel(canvas.transform, "Toasts", new Vector2(1, 1), new Vector2(360, 0), new Vector2(-8, -100));
            var stack = panel.gameObject.AddComponent<VerticalLayoutGroup>(); stack.padding = new RectOffset(8, 8, 8, 8); stack.spacing = 4;
            stack.childControlHeight = stack.childControlWidth = true; stack.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            panel.gameObject.AddComponent<PointerRelay>().owner = this;
            panel.gameObject.SetActive(false);
        }
        private void OnDestroy() { if (instance == this) instance = null; }

        public static void Show(string message) => Show(message, ToastKind.Info);
        public static void Show(string message, ToastKind kind)
        {
            if (instance == null || string.IsNullOrWhiteSpace(message)) return;
            var seconds = kind == ToastKind.Warning ? WarningSeconds : kind == ToastKind.Hint ? HintSeconds : UserSettings.Current.toastSeconds;
            var same = instance.toasts.Find(t => t.kind == kind && t.message == message);
            if (same != null) { same.count++; same.remaining = seconds; }
            else instance.toasts.Add(new Toast { message = message, kind = kind, remaining = seconds });
            instance.dirty = true;
        }
        public static void SetCrisis(string key, string message, System.Action action = null, string actionLabel = "위치로 이동")
        {
            if (instance == null) return;
            if (message == null)
            {
                if (!instance.crises.Remove(key)) return;
                instance.dismissed.Remove(key); instance.locations.Remove(key);
            }
            else
            {
                if (action != null) instance.locations[key] = (action, actionLabel);
                else instance.locations.Remove(key);
                if (instance.crises.TryGetValue(key, out var current) && current == message) return;
                if (instance.dismissed.TryGetValue(key, out var old) && old != message) instance.dismissed.Remove(key);
                instance.crises[key] = message;
            }
            instance.dirty = true;
        }
        // 클릭 닫기와 같은 동작(검사용으로도 쓴다). index는 VisibleLines 순서.
        public static void Dismiss(int index)
        {
            if (instance == null) return;
            var shown = instance.Shown();
            if (index < 0 || index >= shown.Count) return;
            var t = shown[index];
            if (t.crisis) instance.dismissed[t.key] = t.message; else instance.toasts.Remove(t);
            instance.dirty = true;
        }

        private List<Toast> All() => crises.Where(c => !dismissed.ContainsKey(c.Key))
            .Select(c => new Toast { key = c.Key, message = c.Value, crisis = true,
                action = locations.TryGetValue(c.Key, out var location) ? location.action : null,
                actionLabel = locations.TryGetValue(c.Key, out var target) ? target.label : null }).Concat(toasts).ToList();
        private List<Toast> Shown() => All().Take(MaxVisible).ToList();
        private List<string> Lines()
        {
            var all = All();
            var lines = all.Take(MaxVisible).Select(Label).ToList();
            if (all.Count > MaxVisible) lines.Add($"+{all.Count - MaxVisible}건");
            return lines;
        }
        private static string Label(Toast t) => t.count > 1 ? $"{t.message} ×{t.count}" : t.message;

        private void Update()
        {
            if (!hovered)
                for (var i = toasts.Count - 1; i >= 0; i--)
                    if ((toasts[i].remaining -= Time.unscaledDeltaTime) <= 0) { toasts.RemoveAt(i); dirty = true; }
            if (dirty) Rebuild();
        }

        private void Rebuild()
        {
            dirty = false;
            for (var i = panel.childCount - 1; i >= 0; i--)
                if (panel.GetChild(i).name.StartsWith("Toast")) Destroy(panel.GetChild(i).gameObject);
            var all = All(); var shown = all.Take(MaxVisible).ToList();
            for (var i = 0; i < shown.Count; i++)
            {
                var index = i; var t = shown[i];
                Row("Toast " + i, Label(t), t.crisis ? MenuTheme.DangerInk : t.kind == ToastKind.Warning ? MenuTheme.Warning : t.kind == ToastKind.Hint ? Mint : MenuTheme.TextColor,
                    () => Dismiss(index), t.action, t.actionLabel);
            }
            if (all.Count > MaxVisible) Row("Toast More", $"+{all.Count - MaxVisible}건", MenuTheme.Dim, null);
            panel.gameObject.SetActive(all.Count > 0);
            if (all.Count == 0) hovered = false;
        }

        private void Row(string name, string text, Color color, System.Action onClick, System.Action action = null, string actionLabel = null)
        {
            var rect = MenuTheme.Rect(name, panel);
            rect.gameObject.AddComponent<Image>().color = MenuTheme.Plate2;
            var rows = rect.gameObject.AddComponent<VerticalLayoutGroup>(); rows.padding = new RectOffset(6, 6, 3, 3);
            rows.childControlHeight = rows.childControlWidth = true; rows.childForceExpandHeight = false;
            var heading = MenuTheme.Rect("Heading", rect);
            var horizontal = heading.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.childControlHeight = horizontal.childControlWidth = true; horizontal.childForceExpandWidth = false;
            var label = MenuTheme.Text(heading, text, 13, 20); label.color = color; label.alignment = TextAnchor.UpperLeft;
            DestroyImmediate(label.GetComponent<LayoutElement>());
            if (onClick != null)
            {
                var close = MenuTheme.Button(heading, "×", onClick);
                var size = close.GetComponent<LayoutElement>(); size.minWidth = size.preferredWidth = 26; size.preferredHeight = 26;
            }
            if (action != null)
            {
                var jump = MenuTheme.Button(rect, actionLabel, action);
                jump.name = "Location"; jump.GetComponent<LayoutElement>().preferredHeight = 30;
                jump.GetComponentInChildren<Text>().fontSize = 12;
            }
        }

        // 판넬 위 마우스 진입/이탈을 매니저로 넘긴다.
        private sealed class PointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public ToastManager owner;
            public void OnPointerEnter(PointerEventData e) => owner.hovered = true;
            public void OnPointerExit(PointerEventData e) => owner.hovered = false;
        }
    }
}
