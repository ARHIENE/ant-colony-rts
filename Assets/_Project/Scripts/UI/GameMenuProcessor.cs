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
            Screen(p.Data.displayName + " / 가공");
            var rm = ResourceManager.Instance;
            MenuTheme.Text(content, p.Current != null ? $"진행 중: {p.Current.name} {p.Progress:P0}" : "진행 중인 가공 없음", 19, 45);
            MenuTheme.Button(content, $"방식: {ModeName(orderMode)} (눌러서 변경)", () => { orderMode = (Workshop.OrderMode)(((int)orderMode + 1) % 3); ShowProcessor(p); });
            if (orderMode != Workshop.OrderMode.Forever)
            {
                MenuTheme.Button(content, $"{(orderMode == Workshop.OrderMode.Count ? "만들 묶음" : "목표 재고")} {orderAmount} (−)", () => { orderAmount = Mathf.Max(1, orderAmount - 1); ShowProcessor(p); });
                MenuTheme.Button(content, $"{(orderMode == Workshop.OrderMode.Count ? "만들 묶음" : "목표 재고")} {orderAmount} (+)", () => { orderAmount = Mathf.Min(Workshop.MaxOrderAmount, orderAmount + 1); ShowProcessor(p); });
            }
            for (int i = 0; i < p.Recipes.Count; i++)
            {
                var index = i; var r = p.Recipes[i];
                MenuTheme.Button(content, $"목록 추가: {r.name} {r.amount} ← {r.Cost} · 재고 {rm?.GetAmount(r.output) ?? 0}", () => {
                    if (!p.AddOrder(index, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowProcessor(p);
                }).interactable = p.isActiveAndEnabled && p.Orders.Count < Workshop.MaxOrders;
            }
            foreach (var order in p.Orders.ToArray())
            {
                var r = p.Recipes[order.recipe];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}묶음" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {rm?.GetAmount(r.output) ?? 0}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"{r.name} · {ModeName(order.mode)} · {state} — 삭제", () => { p.Orders.Remove(order); ShowProcessor(p); });
            }
            MenuTheme.Text(content, "작업표에서 제작이 켜진 장수가 자율로 와서 가공합니다. 재료는 한 묶음을 시작할 때 차감하고, 창고가 차면 결과물은 창고 주변 바닥에 둡니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowProcessor(p));
            MenuTheme.Button(content, "게임 재개", Resume);
        }
    }
}
