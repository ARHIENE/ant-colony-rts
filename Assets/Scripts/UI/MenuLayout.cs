using System;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 디자인 화면(1440×900 기준)을 절대 좌표로 옮기는 도우미. x·y는 부모의 왼쪽 위 기준, y는 아래로 증가.
    public static class MenuLayout
    {
        public static RectTransform Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool edge = false)
        {
            var rect = Place(MenuTheme.Rect(name, parent), x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            if (edge) rect.gameObject.AddComponent<Outline>().effectColor = MenuTheme.EdgeLo;
            return rect;
        }

        // 판(plate): 배경 + 위쪽 테두리(Rim).
        public static RectTransform Plate(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Box(parent, name, x, y, w, h, MenuTheme.Background, true);
            Box(rect, "Rim", 0, 0, w, 1, MenuTheme.Rim).GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        public static RectTransform Well(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Box(parent, name, x, y, w, h, MenuTheme.Well, true);
            rect.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        public static Text Label(Transform parent, string value, int size, float x, float y, float w, float h,
            Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var text = MenuTheme.Text(parent, value, size);
            Place(text.rectTransform, x, y, w, h);
            text.color = color ?? MenuTheme.TextColor; text.alignment = align; text.supportRichText = true;
            if (bold) text.fontStyle = FontStyle.Bold;
            return text;
        }

        public static void Line(Transform parent, float x, float y, float w) =>
            Box(parent, "Line", x, y, w, 1, MenuTheme.Line).GetComponent<Image>().raycastTarget = false;

        // 오브젝트 이름은 검사가 찾는 키, 표시 문구는 label.
        public static Button Button(Transform parent, string name, string label, float x, float y, float w, float h,
            Action action, string tip = null, bool primary = false, int size = 14, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rect = Place(MenuTheme.Rect(name, parent), x, y, w, h);
            rect.gameObject.AddComponent<Image>();
            rect.gameObject.AddComponent<Outline>().effectColor = primary ? MenuTheme.Hex(0xffc56b) : MenuTheme.Line;
            var button = rect.gameObject.AddComponent<Button>();
            MenuTheme.StyleButton(button);
            if (primary)
            {
                var colors = button.colors; colors.normalColor = MenuTheme.Accent; colors.highlightedColor = MenuTheme.Hex(0xffb84d); button.colors = colors;
            }
            button.onClick.AddListener(() => action());
            var text = Label(rect, label, size, align == TextAnchor.MiddleCenter ? 0 : 12, 0, align == TextAnchor.MiddleCenter ? w : w - 24, h,
                primary ? MenuTheme.AccentInk : MenuTheme.TextColor, align, primary);
            text.raycastTarget = false;
            if (!string.IsNullOrEmpty(tip)) rect.gameObject.AddComponent<MenuTooltip>().Message = tip;
            return button;
        }

        public static RectTransform Meter(Transform parent, float x, float y, float w, float h, float value, Color color)
        {
            var track = Well(parent, "Meter", x, y, w, h);
            var fill = Box(track, "Fill", 0, 0, w * Mathf.Clamp01(value), h, color);
            fill.GetComponent<Image>().raycastTarget = false;
            return track;
        }

        // 세로 목록(스크롤). 반환값은 VerticalLayoutGroup이 붙은 content.
        public static RectTransform List(Transform parent, float x, float y, float w, float h, int spacing = 6)
        {
            var area = Place(MenuTheme.Rect("ListArea", parent), x, y, w, h);
            var content = MenuTheme.Scroll(area);
            var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = spacing; layout.padding = new RectOffset(0, 8, 0, 8);
            return content;
        }

        // 세로 목록 안에 들어가는 고정 높이 칸. 안쪽은 Place로 절대 배치한다.
        public static RectTransform Cell(Transform list, string name, float h, Color? color = null)
        {
            var rect = MenuTheme.Rect(name, list);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = h;
            if (color != null) rect.gameObject.AddComponent<Image>().color = color.Value;
            return rect;
        }
    }
}
