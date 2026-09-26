using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    public static class MenuTheme
    {
        // 디자인 토큰: Claude Design 캔버스 「개미 RTS UI」(2026-09-26)의 :root 변수와 같은 값.
        public static readonly Color World = Hex(0x4b4436), Plate = Hex(0x1b1712), Plate2 = Hex(0x231e17), PlateHover = Hex(0x2e271e),
            Well = Hex(0x120f0b), EdgeLo = Hex(0x080605), Line = Hex(0x3a3127), Line2 = Hex(0x4a3f32), Rim = Hex(0x5a4c3b),
            TextColor = Hex(0xefe7da), Dim = Hex(0x968976), AccentInk = Hex(0x1a1206),
            Hp = Hex(0x6cc46a), HpMid = Hex(0xe0b43a), Danger = Hex(0xe8574a), DangerInk = Hex(0xffb3aa), Loyal = Hex(0x86a9e6);
        public static readonly Color Background = new Color(Plate.r, Plate.g, Plate.b, .98f);
        public static readonly Color Accent = Hex(0xf2a93b);
        public static readonly Color Muted = Hex(0xb8ac9a);
        private static Font font, numberFont;
        public static Font Font => font != null ? font : font = Resources.Load<Font>("Fonts/NotoSansKR") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font NumberFont => numberFont != null ? numberFont : numberFont = Resources.Load<Font>("Fonts/BarlowSemiCondensed-SemiBold") ?? Font;
        public static Color Hex(int rgb) => new Color((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);
        public static RectTransform Rect(string name, Transform parent)
        { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        public static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var rect = Rect(name, parent); rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = position;
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = Background;
            var edge = Rect("Accent", rect); edge.anchorMin = new Vector2(0, 1); edge.anchorMax = Vector2.one;
            edge.pivot = new Vector2(.5f, 1); edge.sizeDelta = new Vector2(0, 2); edge.anchoredPosition = Vector2.zero;
            var image = edge.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = Rim; image.raycastTarget = false;
            return rect;
        }
        public static void StyleButton(UnityEngine.UI.Button button)
        {
            button.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var colors = button.colors;
            colors.normalColor = Plate2;
            colors.highlightedColor = PlateHover;
            colors.pressedColor = Accent; colors.selectedColor = Hex(0x2a2219);
            colors.disabledColor = Well; colors.fadeDuration = .12f;
            button.colors = colors;
        }
        public static Canvas Canvas(string name, Transform parent, int order)
        {
            var rect = Rect(name, parent); var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scale = rect.gameObject.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1440, 900);
            rect.gameObject.AddComponent<GraphicRaycaster>(); return canvas;
        }
        public static Text Text(Transform parent, string value, int size = 20, float height = 40)
        {
            var rect = Rect("Text", parent); var text = rect.gameObject.AddComponent<Text>(); text.font = Font; text.text = value;
            text.fontSize = size; text.color = TextColor; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return text;
        }
        public static Button Button(Transform parent, string label, Action action, string tip = null)
        {
            var rect = Rect(label, parent); rect.gameObject.AddComponent<Image>().color = Plate2;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            StyleButton(button);
            var text = Text(rect, label, 19); Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
            if (!string.IsNullOrEmpty(tip)) rect.gameObject.AddComponent<MenuTooltip>().Message = tip;
            return button;
        }
        public static InputField Input(Transform parent, string value)
        {
            var rect = Rect("Seed", parent); rect.gameObject.AddComponent<Image>().color = Well;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var text = Text(rect, value); Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(12, 0);
            var input = rect.gameObject.AddComponent<InputField>(); input.textComponent = text; input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 11; input.text = value; return input;
        }
        public static RectTransform Scroll(Transform parent)
        {
            var root = Rect("Scroll", parent); Stretch(root);
            var scroll = root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 30;
            var view = Rect("Viewport", root); Stretch(view); view.gameObject.AddComponent<Image>().color = Color.white;
            view.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect("Content", view); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.padding = new RectOffset(8, 16, 8, 12);
            layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.childControlWidth = true; layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view; scroll.content = content; return content;
        }
    }
    public sealed class MenuTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Message;
        public void OnPointerEnter(PointerEventData data) => GameMenuController.Instance?.Tooltip(Message);
        public void OnPointerExit(PointerEventData data) => GameMenuController.Instance?.Tooltip("");
        private void OnDisable() => GameMenuController.Instance?.Tooltip("");
    }
}
