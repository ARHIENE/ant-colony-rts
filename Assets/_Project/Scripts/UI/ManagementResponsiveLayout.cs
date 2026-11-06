using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // Existing operational actions keep their data bindings. Only their containers reflow.
    [DefaultExecutionOrder(750)]
    public sealed class ManagementResponsiveLayout : MonoBehaviour
    {
        private RectTransform window, root;
        private RectTransform[] sections, tabs;
        private Vector2[] positions, dimensions;
        private float originalHeight;
        private int selected;
        private Vector2 previous;
        private System.Collections.Generic.Dictionary<RectTransform, Vector2> originalPositions;

        private void Start()
        {
            window = (RectTransform)transform; root = (RectTransform)window.parent;
            sections = window.Cast<RectTransform>().Where(r => r.Find("ListArea") != null).ToArray();
            if (sections.Length < 2) { enabled = false; return; }
            originalHeight = window.sizeDelta.y;
            positions = sections.Select(s => s.anchoredPosition).ToArray();
            dimensions = sections.Select(s => s.sizeDelta).ToArray();
            originalPositions = window.Cast<RectTransform>().ToDictionary(r => r, r => r.anchoredPosition);
            root.GetComponent<MenuViewport>().enabled = false;
            MenuTheme.Stretch(root); root.localScale = Vector3.one;
            tabs = sections.Select((s, i) =>
            {
                int index = i;
                return (RectTransform)L.Button(window, "Section " + i, s.name, 0, 0, 100, 30,
                    () => { selected = index; previous = Vector2.zero; }).transform;
            }).ToArray();
        }

        private void LateUpdate()
        {
            if (sections == null || sections.Length < 2) return;
            var available = ((RectTransform)root.parent).rect.size;
            var size = new Vector2(Mathf.Min(1100, available.x - 24), Mathf.Min(originalHeight, available.y - 24));
            if (size == previous) return;
            previous = size;
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f);
            window.anchoredPosition = Vector2.zero; window.sizeDelta = size;
            bool compact = size.x < 1040;
            foreach (RectTransform child in window)
            {
                if (sections.Contains(child) || tabs.Contains(child)) continue;
                var p = originalPositions[child];
                if (child.name == "Close") L.Place(child, size.x - 52, 12, 32, 32);
                else if (child.name == "Resume") L.Place(child, size.x - 80, size.y - 43, 60, 30);
                else if (-p.y > originalHeight - 65) child.anchoredPosition = new Vector2(p.x, -(size.y - (originalHeight + p.y)));
                if (child.name == "Line" || child.name == "Rim") child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x - Mathf.Max(0, p.x) * 2);
                if (child.GetComponent<Text>() != null && -p.y < 55)
                {
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0, size.x - p.x - 70));
                    child.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Truncate;
                }
            }
            float tabWidth = (size.x - 40) / tabs.Length;
            for (int i = 0; i < sections.Length; i++)
            {
                tabs[i].gameObject.SetActive(compact);
                L.Place(tabs[i], 20 + tabWidth * i, 104, tabWidth - 6, 32);
                tabs[i].GetComponent<Outline>().effectColor = selected == i ? MenuTheme.Accent : MenuTheme.Line;
                var section = sections[i]; section.gameObject.SetActive(!compact || selected == i);
                float x, y, width, height;
                if (compact) { x = 12; y = 148; width = size.x - 24; height = size.y - y - 66; }
                else
                {
                    x = positions[i].x / 1100 * size.x; y = 104 + (-positions[i].y - 104) / 544 * (size.y - 176);
                    width = dimensions[i].x / 1100 * size.x; height = dimensions[i].y / 544 * (size.y - 176);
                }
                L.Place(section, x, y, width, Mathf.Max(80, height));
                var area = (RectTransform)section.Find("ListArea");
                L.Place(area, 12, 42, width - 24, Mathf.Max(38, height - 50));
                var heading = section.GetComponentInChildren<Text>();
                heading.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 24);
            }
        }
    }
}
