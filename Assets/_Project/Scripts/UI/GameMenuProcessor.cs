using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.UI
{
    // 가공대 생산 목록(2026-10-10). 공방 화면과 같은 방식·수량 선택을 쓴다.
    public sealed partial class GameMenuController
    {
        public void ShowProcessor(Processor p)
        {
            if (p == null) { Resume(); return; }
            var window = V5Window(p.Data.displayName + " / 가공", "생산 방식과 수량을 선택한 뒤 품목을 추가하세요.");
            content = V5Section(window, "생산 품목 / 주문 설정", 20, 104, 520, 544);
            var rm = ResourceManager.Instance;
            if (p.Data.kind == AntColony.Data.BuildingKind.Carpentry)
                MenuTheme.Button(content, "돌 도구 · 단궁 제작", GroundCrafting);
            MenuTheme.Text(content, p.Current != null ? $"진행 중: {p.Current.name} {p.Progress:P0}" : "진행 중인 가공 없음", 19, 45);
            ProductionControls(() => ShowProcessor(p));
            for (int i = 0; i < p.Recipes.Count; i++)
            {
                var index = i; var r = p.Recipes[i];
                MenuTheme.Button(content, $"목록 추가: {r.name} {r.amount} ← {r.Cost} · 재고 {rm?.GetAmount(r.output) ?? 0}", () => {
                    if (!p.AddOrder(index, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowProcessor(p);
                }).interactable = p.isActiveAndEnabled && p.Orders.Count < Workshop.MaxOrders;
            }
            content = V5Section(window, "생산 목록 / 진행 상태", 554, 104, 526, 544);
            foreach (var order in p.Orders.ToArray())
            {
                var r = p.Recipes[order.recipe];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}묶음" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {rm?.GetAmount(r.output) ?? 0}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"{r.name} · {ModeName(order.mode)} · {state} — 삭제", () => { p.Orders.Remove(order); ShowProcessor(p); });
            }
            MenuTheme.Text(content, (p.IsMedical ? "작업표에서 의료가 켜진 장수가 할 수 있는 치료가 없을 때 와서 만듭니다(의료 능력은 속도만)." : "작업표에서 제작이 켜진 장수가 자율로 와서 가공합니다.")
                + " 재료는 한 묶음을 시작할 때 차감하고, 창고가 차면 결과물은 창고 주변 바닥에 둡니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowProcessor(p));
            V5Fit(window);
        }
    }
}
