using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // Pixel-sized chrome shared with the approved HTML HUD; independent of gameplay.
    public static class HudChrome
    {
        private static readonly Dictionary<int, Sprite> shapes = new Dictionary<int, Sprite>();
        private static Sprite Shape(int radius, bool outline)
        {
            int key = radius * 2 + (outline ? 1 : 0);
            if (shapes.TryGetValue(key, out var cached) && cached != null) return cached;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "HUD radius " + radius, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var q = new Vector2(Mathf.Abs(x + .5f - size / 2f), Mathf.Abs(y + .5f - size / 2f)) - Vector2.one * (size / 2f - radius);
                float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                float alpha = Mathf.Clamp01(.5f - distance);
                if (outline) alpha *= Mathf.Clamp01(distance + 1.5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * (radius + 1));
            shapes[key] = sprite; return sprite;
        }

        public static void Round(Image image, int radius, Color edge)
        {
            image.sprite = Shape(radius, false); image.type = Image.Type.Sliced;
            var existing = image.transform.Find("Chrome Border");
            var border = existing != null ? existing.GetComponent<Image>() : MenuTheme.Rect("Chrome Border", image.transform).gameObject.AddComponent<Image>();
            MenuTheme.Stretch(border.rectTransform); border.raycastTarget = false;
            border.sprite = Shape(radius, true); border.type = Image.Type.Sliced; border.color = edge;
            border.transform.SetAsLastSibling();
        }

        public static void Clip(Image image, int radius)
        {
            image.sprite = Shape(radius, false); image.type = Image.Type.Sliced;
            var mask = image.GetComponent<Mask>() ?? image.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
        }

        public static RawImage Icon(Transform parent, string name, string asset, Color color)
        {
            var child = parent.Find(name);
            var icon = child != null ? child.GetComponent<RawImage>() : null;
            if (icon == null) icon = MenuTheme.Rect(name, parent).gameObject.AddComponent<RawImage>();
            if (icon.texture == null || icon.texture.name != asset) icon.texture = Resources.Load<Texture2D>("UI/HudIcons/" + asset);
            icon.color = color; icon.raycastTarget = false; return icon;
        }

        public static void CenterIcon(RawImage icon, float x, float y, float size)
        {
            var r = icon.rectTransform; r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
            r.anchoredPosition = new Vector2(x, y); r.sizeDelta = Vector2.one * size;
        }
    }
}
