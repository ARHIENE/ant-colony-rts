using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // HUD 초기화·씬 전환 이후에도 메뉴 순서와 너비를 메뉴 소유 쪽에서 유지한다.
    [DefaultExecutionOrder(800)]
    public sealed class GameMenuDock : MonoBehaviour
    {
        private static readonly string[] Keys = { "Menu [Esc]", "Work Schedule [T]", "Work Designations", "Science [K]", "WorldMapToggle", "V6 Logistics", "V6 Colony", "V6 Production", "V6 Roster", "V6 Build" };
        private static readonly string[] Labels = { "≡", "작업표", "작업 지정", "과학", "월드맵", "물류·식품", "의료·관리", "생산·자원", "인재·편성", "＋ 건설" };
        private void LateUpdate()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(16, 9);
            var layout = GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4; layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childControlWidth = layout.childControlHeight = true;
            float width = 0;
            for (int i = 0; i < Keys.Length; i++)
            {
                var child = transform.Find(Keys[i]); if (child == null) continue;
                child.SetSiblingIndex(i);
                var text = child.GetComponentInChildren<Text>(); text.text = Labels[i]; text.fontSize = 13;
                var element = child.GetComponent<LayoutElement>();
                element.preferredHeight = 34; element.preferredWidth = text.preferredWidth + 24; element.minWidth = element.preferredWidth;
                width += element.preferredWidth + 4;
                var button = child.GetComponent<Button>(); var colors = button.colors;
                colors.normalColor = i == Keys.Length - 1 ? MenuTheme.Plate2 : Color.clear;
                MenuTheme.SetColorsInstant(button, colors);
            }
            rect.sizeDelta = new Vector2(width, 34);
        }
    }
}
