using AntColony.Buildings;
using AntColony.Core;

namespace AntColony.UI
{
    // 요리 생산 목록(2026-10-12). 가공대 화면과 같은 생산 방식·수량 선택. 일시정지·진행 중 조리 취소(재료 손실은 확인 후).
    public sealed partial class GameMenuController
    {
        private bool confirmCookCancel;
        public void ShowKitchen(Kitchen k)
        {
            if (k == null || k.IsTable) { Resume(); return; }
            var window = V5Window(k.Data.displayName + " / 요리", "생산 방식과 수량을 선택한 뒤 요리를 추가하세요.");
            content = V5Section(window, "요리 / 주문 설정", 20, 104, 520, 544);
            MenuTheme.Text(content, k.Current != null ? $"조리 중: {k.Current.name} {k.Progress:P0}" : "조리 중인 요리 없음" + (k.WaitReason != null ? $" · {k.WaitReason}" : ""), 19, 45);
            ProductionControls(() => ShowKitchen(k));
            for (int i = 0; i < Kitchen.Recipes.Length; i++)
            {
                var index = i; var r = Kitchen.Recipes[i];
                MenuTheme.Button(content, $"목록 추가: {r.name} ← 식량 {r.food} · {r.seconds:0}초 · 재고 {Kitchen.Stock(r.output)}", () => {
                    if (!k.AddOrder(index, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowKitchen(k);
                }).interactable = k.isActiveAndEnabled && k.Orders.Count < Workshop.MaxOrders;
            }
            MenuTheme.Button(content, k.Paused ? "재개" : "일시정지", () => { k.Paused = !k.Paused; ShowKitchen(k); });
            if (k.Current != null)
                MenuTheme.Button(content, confirmCookCancel ? "한 번 더 누르면 취소(소비한 재료는 돌려받지 못함)" : "진행 중인 조리 취소", () => {
                    if (confirmCookCancel) k.CancelCurrent(); confirmCookCancel = !confirmCookCancel; ShowKitchen(k);
                });
            content = V5Section(window, "생산 목록 / 진행 상태", 554, 104, 526, 544);
            foreach (var order in k.Orders.ToArray())
            {
                var r = Kitchen.Recipes[order.recipe];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}회" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {Kitchen.Stock(r.output)}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"{r.name} · {ModeName(order.mode)} · {state} — 삭제", () => { k.Orders.Remove(order); ShowKitchen(k); });
            }
            MenuTheme.Text(content, "작업표에서 요리가 켜진 장수가 와서 조리합니다. 재료는 조리를 시작할 때 소비하고, 완성 요리는 주변 바닥에 놓여 운반 장수가 창고로 옮깁니다. 요리사가 떠나도 진행도는 남습니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowKitchen(k));
            V5Fit(window);
        }
    }
}
