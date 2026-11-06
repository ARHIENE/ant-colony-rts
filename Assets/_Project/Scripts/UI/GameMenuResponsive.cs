using System;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private RectTransform PrototypeWindow(string title, string subtitle, Action rebuild)
        {
            var root = Frame(title);
            root.GetComponent<MenuViewport>().enabled = false;
            MenuTheme.Stretch(root);
            root.localScale = Vector3.one;
            var available = ((RectTransform)root.parent).rect.size;
            var width = Mathf.Min(1120, available.x - 24);
            var height = Mathf.Min(760, available.y - 24);
            var window = L.Plate(root, "PrototypeWindow", 0, 0, width, height);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f);
            window.anchoredPosition = Vector2.zero;
            L.Label(window, title, 20, 20, 10, width - 80, 28, bold: true);
            L.Label(window, subtitle, 12, 20, 40, width - 40, 24, MenuTheme.Muted);
            L.Button(window, "Close", "×", width - 52, 12, 32, 32, Resume);
            L.Line(window, 20, 70, width - 40);
            root.gameObject.AddComponent<PrototypeWindowLayout>().Rebuild = rebuild;
            return window;
        }
    }
}
