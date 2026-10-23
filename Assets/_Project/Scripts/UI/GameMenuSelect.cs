using System;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    internal static class GameMenuSelect
    {
        internal static Dropdown Create(Transform parent, string[] options, int selected, float x, float y, float width, Action<int> changed)
        {
            var root = MenuLayout.Box(parent, "Subject Dropdown", x, y, width, 32, MenuTheme.Well, true);
            var dropdown = root.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = root.GetComponent<Image>();
            var caption = MenuLayout.Label(root, "", 13, 10, 0, width - 36, 32);
            MenuLayout.Label(root, "▾", 13, width - 24, 0, 20, 32);
            dropdown.captionText = caption;
            var template = MenuLayout.Box(root, "Template", 0, 32, width, 160, MenuTheme.Well, true);
            template.pivot = new Vector2(0, 1);
            var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = MenuTheme.Rect("Viewport", template); MenuTheme.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var list = MenuTheme.Rect("Content", viewport);
            list.anchorMin = new Vector2(0, 1); list.anchorMax = Vector2.one; list.pivot = new Vector2(.5f, 1); list.sizeDelta = new Vector2(0, 32);
            var item = MenuLayout.Box(list, "Item", 0, 0, width, 32, MenuTheme.Plate2);
            item.anchorMax = Vector2.one; item.anchorMin = new Vector2(0, 1); item.sizeDelta = new Vector2(0, 32);
            var toggle = item.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = item.GetComponent<Image>();
            var check = MenuLayout.Label(item, "✓", 13, 4, 0, 20, 32, MenuTheme.Accent);
            toggle.graphic = check;
            var label = MenuLayout.Label(item, "", 13, 26, 0, width - 36, 32);
            scroll.content = list; scroll.viewport = viewport;
            dropdown.template = template; dropdown.itemText = label;
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
            dropdown.SetValueWithoutNotify(Mathf.Max(0, selected)); dropdown.RefreshShownValue();
            template.gameObject.SetActive(false);
            dropdown.onValueChanged.AddListener(index => changed(index));
            return dropdown;
        }
    }
}
