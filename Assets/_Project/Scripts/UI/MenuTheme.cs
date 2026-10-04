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
            TextColor = Hex(0xefe7da), Dim = Hex(0xaa9b86), AccentInk = Hex(0x1a1206),
            Hp = Hex(0x6cc46a), HpMid = Hex(0xe0b43a), Danger = Hex(0xe8574a), DangerInk = Hex(0xffb3aa), Loyal = Hex(0x86a9e6);
        public static readonly Color Background = new Color(Plate.r, Plate.g, Plate.b, .98f);
        public static readonly Color Accent = Hex(0xf2a93b);
        public static readonly Color Muted = Hex(0xc2b7a6);
        public static readonly Color Selection = Hex(0x8bc8ba), Warning = Hex(0xef955f);
        // HUD v4.1 하단 프레임(2026-10-04 시안): 청동 띠·판 재질·움푹한 화면.
        public static readonly Color FrameHi = Hex(0xd8aa62), Frame = Hex(0x8f6a3a), FrameLo = Hex(0x3b2a17), FrameInk = Hex(0x0a0705),
            FrameMat = Hex(0x271e15), ScreenColor = Hex(0x0e0b08), ScreenRim = Hex(0x6b4f2c), FrameButton = Hex(0x30251a);

        // 청동 띠 7px(밝은 1 · 본색 4 · 어두운 1 · 먹선 1). vertical이면 inkFirst 쪽(왼쪽)부터 같은 순서를 뒤집어 쌓는다.
        public static RectTransform Bronze(Transform parent, string name, bool vertical = false, bool inkFirst = false)
        {
            var root = Rect(name, parent);
            Color[] colors = { FrameHi, Frame, FrameLo, FrameInk }; float[] sizes = { 1, 4, 1, 1 };
            float at = 0;
            for (var i = 0; i < 4; i++)
            {
                var k = inkFirst ? 3 - i : i;
                var band = Rect("Band", root);
                band.anchorMin = vertical ? new Vector2(0, 0) : new Vector2(0, 1); band.anchorMax = vertical ? new Vector2(0, 1) : new Vector2(1, 1);
                band.pivot = new Vector2(0, 1);
                band.sizeDelta = vertical ? new Vector2(sizes[k], 0) : new Vector2(0, sizes[k]);
                band.anchoredPosition = vertical ? new Vector2(at, 0) : new Vector2(0, -at);
                var image = band.gameObject.AddComponent<Image>(); image.color = colors[k]; image.raycastTarget = false;
                at += sizes[k];
            }
            return root;
        }

        // 리벳 하나(8×8, 청동 점 + 어두운 테). anchor 기준 position.
        public static void Rivet(Transform parent, Vector2 anchor, Vector2 position)
        {
            var rim = Rect("Rivet", parent); rim.anchorMin = rim.anchorMax = rim.pivot = anchor;
            rim.sizeDelta = new Vector2(5, 5); rim.anchoredPosition = position;
            rim.gameObject.AddComponent<Image>().color = FrameLo;
            var dot = Rect("Head", rim); dot.anchorMin = dot.anchorMax = dot.pivot = new Vector2(.5f, .5f); dot.sizeDelta = new Vector2(3, 3);
            dot.gameObject.AddComponent<Image>().color = FrameHi;
            foreach (var image in rim.GetComponentsInChildren<Image>()) image.raycastTarget = false;
        }

        // 프레임 위 버튼: 판보다 밝은 갈색 + 청동 테. primary = 건설 같은 주 명령(호박색).
        public static void StyleFrameButton(UnityEngine.UI.Button button, bool primary = false)
        {
            StyleButton(button);
            var colors = button.colors;
            colors.normalColor = primary ? Hex(0x87591f) : FrameButton;
            colors.highlightedColor = primary ? Hex(0xa06c26) : Hex(0x3b2e20);
            button.colors = colors;
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = primary ? Hex(0xd79a45) : ScreenRim;
        }

        // 움푹한 화면:어두운 바탕 + 먹선 2px + 청동 1px 테.
        public static Image InsetScreen(GameObject go)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = ScreenColor;
            var ink = go.AddComponent<Outline>(); ink.effectColor = FrameInk; ink.effectDistance = new Vector2(2, -2);
            var rim = go.AddComponent<Outline>(); rim.effectColor = ScreenRim; rim.effectDistance = new Vector2(1, -1);
            return image;
        }
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
