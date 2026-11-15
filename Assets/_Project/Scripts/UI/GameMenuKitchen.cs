using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 요리 생산 목록(2026-10-12). 가공대 화면과 같은 생산 방식·수량 선택. 일시정지·진행 중 조리 취소(재료 손실은 확인 후).
    public sealed partial class GameMenuController
    {
        private bool confirmCookCancel;
        public void ShowKitchen(Kitchen k)
        {
            if (k == null || k.IsTable) { Resume(); return; }
            var window = V5Window(k.Data.displayName + " / 요리", "식량 원료는 육류·어류·열매·버섯 통합 · 훈연식도 범용 가공식이며 원산지 제한 없음");
            content = V5Section(window, "요리 / 주문 설정", 20, 104, 520, 544);
            MenuTheme.Text(content, k.Current != null ? $"조리 중: {k.Current.name} {k.Progress:P0}" : "조리 중인 요리 없음" + (k.WaitReason != null ? $" · {k.WaitReason}" : ""), 19, 45);
            MenuTheme.Text(content, WorkDiagnostics.Describe(k), 15, 68);
            ProductionControls(() => ShowKitchen(k));
            for (int i = 0; i < Kitchen.Recipes.Length; i++)
            {
                var index = i; var r = Kitchen.Recipes[i];
                MenuTheme.Button(content, $"목록 추가: {r.name} ← {r.Cost} · 기본 작업량 {r.seconds:0} · 재고 {Kitchen.Stock(r.output)}", () => {
                    if (!k.AddOrder(index, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowKitchen(k);
                }).interactable = k.isActiveAndEnabled && k.Orders.Count < Workshop.MaxOrders;
            }
            MenuTheme.Text(content, "조리대 재료: " + (k.Staged > 0 ? k.StagedDescription : "없음") + (k.NeedsIngredients ? " · 배송 대기" : ""), 16, 45);
            MenuTheme.Button(content, k.Paused ? "재개" : "일시정지", () => { k.Paused = !k.Paused; ShowKitchen(k); });
            MenuTheme.Button(content, "식단 관리(공통·장수별)", () => ShowDiet());
            if (k.Current != null)
                MenuTheme.Button(content, confirmCookCancel ? "한 번 더 누르면 취소(소비한 재료는 돌려받지 못함)" : "진행 중인 조리 취소", () => {
                    if (confirmCookCancel) k.CancelCurrent(); confirmCookCancel = !confirmCookCancel; ShowKitchen(k);
                });
            content = V5Section(window, "생산 목록 / 진행 상태", 554, 104, 526, 544);
            foreach (var order in k.Orders.ToArray())
            {
                var r = Kitchen.Recipes[order.recipe];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}회" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {Kitchen.Stock(r.output)}/{order.amount}" : "반복";
                var row = L.Cell(content, "CookingOrder", 90);
                L.Label(row, $"{r.name} · {ModeName(order.mode)} · {state}\n{k.OrderStatus(order)}", 15, 0, 0, 490, 44, MenuTheme.Muted);
                L.Button(row, "Earlier", "앞으로", 0, 47, 92, 36, () => { k.MoveOrder(order, -1); ShowKitchen(k); });
                L.Button(row, "Later", "뒤로", 98, 47, 92, 36, () => { k.MoveOrder(order, 1); ShowKitchen(k); });
                L.Button(row, "Remove", "삭제", 196, 47, 80, 36, () => { k.RemoveOrder(order); ShowKitchen(k); });
                if (order.mode == Workshop.OrderMode.KeepStock)
                {
                    L.Button(row, "Less", "−", 302, 47, 44, 36, () => { order.amount = Mathf.Max(1, order.amount - 1); ShowKitchen(k); });
                    L.Button(row, "More", "+", 352, 47, 44, 36, () => { order.amount = Mathf.Min(Workshop.MaxOrderAmount, order.amount + 1); ShowKitchen(k); });
                }
            }
            MenuTheme.Text(content, "위쪽 주문을 우선하며, 재료가 없는 주문은 건너뛰고 만들 수 있는 다음 요리를 준비합니다. 이미 운반·조리 중인 재료는 그 주문에 유지됩니다. 재고 유지 수량은 식사 인원과 부패 속도에 맞춰 조절하세요.", 15, 76);
            MenuTheme.Text(content, "작업표에서 요리가 켜진 장수가 와서 조리합니다. 요리사가 재료를 창고에서 조리대로 가져오고, 조리를 시작할 때 소비하며, 완성 요리는 주변 바닥에 놓여 운반 장수가 창고로 옮깁니다. 요리사가 떠나도 진행도는 남습니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowKitchen(k));
            V5Fit(window);
        }
    }
}
