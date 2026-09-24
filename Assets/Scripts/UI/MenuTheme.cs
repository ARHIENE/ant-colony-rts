using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AntColony.UI
{
    public static class MenuTheme
    {
        public static readonly Color Background = new Color(.06f, .08f, .1f, .98f);
        public static readonly Color Accent = new Color(.28f, .68f, .57f);
        public static readonly Color Muted = new Color(.65f, .73f, .73f);
        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            var image = edge.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = Accent; image.raycastTarget = false;
            return rect;
        }
        public static void StyleButton(UnityEngine.UI.Button button)
        {
            button.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var colors = button.colors;
            colors.normalColor = new Color(.12f, .19f, .21f);
            colors.highlightedColor = new Color(.23f, .39f, .36f);
            colors.pressedColor = Accent; colors.selectedColor = new Color(.18f, .31f, .29f);
            colors.disabledColor = new Color(.08f, .1f, .12f); colors.fadeDuration = .1f;
            button.colors = colors;
        }
        public static Canvas Canvas(string name, Transform parent, int order)
        {
            var rect = Rect(name, parent); var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scale = rect.gameObject.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1280, 720);
            rect.gameObject.AddComponent<GraphicRaycaster>(); return canvas;
        }
        public static Text Text(Transform parent, string value, int size = 20, float height = 40)
        {
            var rect = Rect("Text", parent); var text = rect.gameObject.AddComponent<Text>(); text.font = Font; text.text = value;
            text.fontSize = size; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height; return text;
        }
        public static Button Button(Transform parent, string label, Action action, string tip = null)
        {
            var rect = Rect(label, parent); rect.gameObject.AddComponent<Image>().color = new Color(.15f, .23f, .27f);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            StyleButton(button);
            var text = Text(rect, label, 19); Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
            if (!string.IsNullOrEmpty(tip)) rect.gameObject.AddComponent<MenuTooltip>().Message = tip;
            return button;
        }
        public static InputField Input(Transform parent, string value)
        {
            var rect = Rect("Seed", parent); rect.gameObject.AddComponent<Image>().color = new Color(.12f, .16f, .2f);
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
