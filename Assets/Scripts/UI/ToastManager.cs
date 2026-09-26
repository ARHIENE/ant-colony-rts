using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed class ToastManager : MonoBehaviour
    {
        private static ToastManager instance;
        private readonly Queue<(string message, float expires)> messages = new Queue<(string, float)>();
        private Text label;
        private RectTransform panel;
        private readonly Dictionary<string, string> crises = new Dictionary<string, string>();
        public static void SetCrisis(string key, string message)
        {
            if (instance == null) return;
            if (message == null) instance.crises.Remove(key); else instance.crises[key] = message;
            instance.Refresh();
        }
        public static int Count => instance != null ? instance.messages.Count : 0;
        private void Awake()
        {
            instance = this;
            var canvas = MenuTheme.Canvas("Notifications", transform, 200);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            // 디자인: 상단 바 아래 오른쪽 318px 판넬. 내용 높이에 맞춰 늘고, 비면 숨긴다.
            panel = MenuTheme.Panel(canvas.transform, "Toasts", new Vector2(1, 1), new Vector2(318, 0), new Vector2(-8, -48));
            panel.GetComponent<Image>().raycastTarget = false;
            var stack = panel.gameObject.AddComponent<VerticalLayoutGroup>(); stack.padding = new RectOffset(10, 10, 8, 8);
            stack.childControlHeight = stack.childControlWidth = true; stack.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            label = MenuTheme.Text(panel, "", 14, 20);
            label.alignment = TextAnchor.UpperLeft;
            Destroy(label.GetComponent<LayoutElement>());
            panel.gameObject.SetActive(false);
        }
        public static void Show(string message)
        {
            if (instance == null || string.IsNullOrWhiteSpace(message)) return;
            while (instance.messages.Count >= 3) instance.messages.Dequeue();
            instance.messages.Enqueue((message, Time.unscaledTime + UserSettings.Current.toastSeconds));
            instance.Refresh();
        }
        private void Update()
        { while (messages.Count > 0 && messages.Peek().expires <= Time.unscaledTime) messages.Dequeue(); Refresh(); }
        private void Refresh()
        {
            if (label == null) return;
            label.text = string.Join("\n", System.Linq.Enumerable.Concat(crises.Values, System.Linq.Enumerable.Select(messages, x => x.message)));
            panel.gameObject.SetActive(label.text.Length > 0);
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
