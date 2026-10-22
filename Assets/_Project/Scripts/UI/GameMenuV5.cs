using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        // V5와 같은 고정 헤더/닫기와 독립 스크롤 영역. 실제 명령은 기존 화면이 연결한다.
        private RectTransform V5Window(string title, string subtitle)
        {
            var root = Frame(title);
            var window = L.Plate(root, "V5Window", 170, 90, 1100, 720);
            L.Label(window, title, 20, 20, 12, 1000, 36, MenuTheme.TextColor);
            L.Label(window, subtitle, 14, 20, 49, 1020, 30, MenuTheme.Muted);
            L.Button(window, "Close", "×", 1044, 16, 36, 32, Resume);
            L.Line(window, 20, 88, 1060);
            L.Line(window, 20, 664, 1060);
            L.Button(window, "Resume", "닫기 · 게임 재개", 910, 677, 170, 30, Resume);
            return window;
        }

        private RectTransform V5Section(RectTransform window, string title, float x, float y, float width, float height)
        {
            var box = L.Place(MenuTheme.Rect(title, window), x, y, width, height);
            L.Line(box, 0, 0, width);
            L.Label(box, title, 15, 12, 6, width - 24, 30, MenuTheme.Accent);
            return L.List(box, 12, 42, width - 24, height - 50);
        }

        private static void V5Fit(RectTransform window)
        {
            foreach (var text in window.GetComponentsInChildren<Text>(true))
            {
                if (text.transform.parent.GetComponent<Button>() != null)
                {
                    text.fontSize = 13;
                    text.resizeTextForBestFit = false;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                }
                else if (text.transform.parent.GetComponent<VerticalLayoutGroup>() != null)
                {
                    var layout = text.GetComponent<LayoutElement>();
                    if (layout != null) { layout.minHeight = layout.preferredHeight; layout.preferredHeight = -1; }
                    text.fontSize = 13;
                }
            }
        }

        private RectTransform V5FilterRow(RectTransform parent)
        {
            var row = L.Cell(parent, "Filters", 104);
            var grid = row.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210, 46); grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            return row;
        }

        private void ProductionControls(System.Action refresh)
        {
            var row = L.Cell(content, "ProductionModes", 44);
            foreach (AntColony.Buildings.Workshop.OrderMode mode in System.Enum.GetValues(typeof(AntColony.Buildings.Workshop.OrderMode)))
            {
                var choice = mode;
                L.Button(row, "Mode " + mode, ModeName(mode), (int)mode * 160, 0, 156, 40,
                    () => { orderMode = choice; refresh(); }, primary: mode == orderMode);
            }
            if (orderMode == AntColony.Buildings.Workshop.OrderMode.Forever)
            {
                MenuTheme.Text(content, "재료가 있는 동안 계속 생산합니다.", 15, 32);
                return;
            }
            MenuTheme.Text(content, orderMode == AntColony.Buildings.Workshop.OrderMode.Count ? "만들 수량" : "목표 재고", 15, 28);
            var amountRow = L.Cell(content, "ProductionAmount", 44);
            var input = MenuTheme.Input(amountRow, orderAmount.ToString());
            input.name = "OrderAmount";
            L.Place((RectTransform)input.transform, 52, 0, 150, 40);
            input.onEndEdit.AddListener(value => {
                if (int.TryParse(value, out var n)) orderAmount = Mathf.Clamp(n, 1, AntColony.Buildings.Workshop.MaxOrderAmount);
                input.SetTextWithoutNotify(orderAmount.ToString());
            });
            L.Button(amountRow, "Amount Minus", "−", 0, 0, 44, 40, () => { orderAmount = Mathf.Max(1, orderAmount - 1); input.SetTextWithoutNotify(orderAmount.ToString()); });
            L.Button(amountRow, "Amount Plus", "+", 210, 0, 44, 40, () => { orderAmount = Mathf.Min(AntColony.Buildings.Workshop.MaxOrderAmount, orderAmount + 1); input.SetTextWithoutNotify(orderAmount.ToString()); });
        }
    }
}
