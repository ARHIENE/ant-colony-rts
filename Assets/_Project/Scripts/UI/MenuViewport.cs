using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 기존 관리 화면의 좌표는 유지하고, 빈 바깥 여백을 제외한 창을 화면에 맞춘다.
    [DefaultExecutionOrder(700)]
    public sealed class MenuViewport : MonoBehaviour
    {
        public bool FitSelf;
        private RectTransform root;
        private Vector2 viewport;
        private readonly Dictionary<Text, int> sizes = new Dictionary<Text, int>();

        private void LateUpdate()
        {
            if (root == null) root = (RectTransform)transform;
            var parent = (RectTransform)root.parent;
            if (viewport == parent.rect.size && sizes.Count > 0) return;
            viewport = parent.rect.size;
            root.localScale = Vector3.one;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.anchoredPosition = Vector2.zero;
            if (!FitSelf) root.sizeDelta = new Vector2(1440, 900);
            var bounds = root.rect;
            if (!FitSelf)
            {
                bool first = true;
                foreach (RectTransform child in root)
                {
                    if (!child.gameObject.activeSelf) continue;
                    var corners = new Vector3[4]; child.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector2 p = root.InverseTransformPoint(corner);
                        if (first) { bounds = new Rect(p, Vector2.zero); first = false; }
                        bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, p.x), Mathf.Min(bounds.yMin, p.y), Mathf.Max(bounds.xMax, p.x), Mathf.Max(bounds.yMax, p.y));
                    }
                }
            }
            var scale = Mathf.Min(1, (viewport.x - 32) / Mathf.Max(1, bounds.width), (viewport.y - 32) / Mathf.Max(1, bounds.height));
            root.localScale = Vector3.one * scale;
            root.anchoredPosition = -bounds.center * scale;
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (!sizes.TryGetValue(text, out var original)) sizes[text] = original = text.fontSize;
                // 화면이 작아져도 본문을 12px 아래로 축소하지 않는다.
                text.fontSize = Mathf.Max(original, Mathf.CeilToInt(12 / scale));
                text.resizeTextForBestFit = false;
            }
        }
    }
}
