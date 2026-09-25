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
            label = MenuTheme.Text(canvas.transform, "", 19, 110);
            label.alignment = TextAnchor.UpperCenter;
            label.rectTransform.anchorMin = new Vector2(.2f, .72f); label.rectTransform.anchorMax = new Vector2(.8f, .9f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
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
        private void Refresh() { if (label != null) label.text = string.Join("\n", System.Linq.Enumerable.Concat(crises.Values, System.Linq.Enumerable.Select(messages, x => x.message))); }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
