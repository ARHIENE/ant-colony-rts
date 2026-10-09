using System;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private static Workshop.OrderMode orderMode = Workshop.OrderMode.Count;
        private static int orderAmount = 1;
        private static string ModeName(Workshop.OrderMode m) => m == Workshop.OrderMode.Count ? "지정 수량" : m == Workshop.OrderMode.KeepStock ? "재고 유지" : "계속 생산";
        public void ShowWorkshop(Workshop workshop)
        {
            if (workshop == null) { Science(); return; }
            Screen("공방 / 장비 보관함");
            var inventory = EquipmentInventory.Instance;
            MenuTheme.Text(content, $"보관함 {inventory?.Items.Count ?? 0}/{EquipmentInventory.Capacity} · 대기열 {workshop.Jobs.Count}/{GameBalance.CraftQueueCapacity}", 19, 45);
            MenuTheme.Text(content, workshop.Ruined ? "파괴됨 — 제작 중단, 대기열 취소 가능" : $"제작 장수: {workshop.Crafter?.CommanderName ?? "미배정"}", 18, 45);
            // 생산 목록: 방식·수량을 고른 뒤 품목을 누르면 목록에 추가. 제작이 허용된 장수가 자율로 와서 만든다.
            MenuTheme.Button(content, $"방식: {ModeName(orderMode)} (눌러서 변경)", () => { orderMode = (Workshop.OrderMode)(((int)orderMode + 1) % 3); ShowWorkshop(workshop); });
            if (orderMode != Workshop.OrderMode.Forever)
            {
                MenuTheme.Button(content, $"{(orderMode == Workshop.OrderMode.Count ? "만들 개수" : "목표 재고")} {orderAmount} (−)", () => { orderAmount = Mathf.Max(1, orderAmount - 1); ShowWorkshop(workshop); });
                MenuTheme.Button(content, $"{(orderMode == Workshop.OrderMode.Count ? "만들 개수" : "목표 재고")} {orderAmount} (+)", () => { orderAmount = Mathf.Min(Workshop.MaxOrderAmount, orderAmount + 1); ShowWorkshop(workshop); });
            }
            foreach (EquipmentRecipe recipe in Enum.GetValues(typeof(EquipmentRecipe)))
            {
                if (!EquipmentRecipes.Unlocked(recipe)) continue;
                var cost = EquipmentRecipes.Cost(recipe);
                MenuTheme.Button(content, $"목록 추가: {EquipmentRecipes.Name(recipe)} — Food {cost.x} / 재료 {cost.y} / Special {cost.z} · 재고 {Workshop.Stock(recipe)}", () => {
                    if (!workshop.AddOrder(recipe, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowWorkshop(workshop);
                }).interactable = !workshop.Ruined && workshop.isActiveAndEnabled && workshop.Orders.Count < Workshop.MaxOrders;
            }
            for (int i = 0; i < workshop.Orders.Count; i++)
            {
                var order = workshop.Orders[i];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}개" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {Workshop.Stock(order.recipe)}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"생산 {i + 1}. {EquipmentRecipes.Name(order.recipe)} · {ModeName(order.mode)} · {state} — 삭제", () => { workshop.Orders.Remove(order); ShowWorkshop(workshop); });
            }
            for (int i = 0; i < workshop.Jobs.Count; i++)
            {
                int index = i; var job = workshop.Jobs[i];
                MenuTheme.Button(content, $"{i + 1}. {EquipmentRecipes.Name(job.recipe)} {job.work / GameBalance.CraftWork:P0} · 취소 ({(job.work > 0 ? 50 : 100)}% 환급)",
                    () => { workshop.Cancel(index); ShowWorkshop(workshop); });
            }
            if (workshop.Crafter != null) MenuTheme.Button(content, "장수 배정 해제 (진행도 유지)", () => { workshop.Release(); ShowWorkshop(workshop); });
            else foreach (var c in SortedCommanders())
                if (workshop.CanAssign(c)) MenuTheme.Button(content, "제작 배정: " + c.CommanderName, () => { workshop.TryAssign(c); ShowWorkshop(workshop); });
            MenuTheme.Text(content, "생산 목록은 대기열이 빌 때마다 위에서부터 1개씩 대기열에 넣고 그때 비용을 냅니다. 작업표에서 제작이 켜진 장수가 자율로 와서 만듭니다. 장비 장착·해제는 장수 상세에서 가능합니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowWorkshop(workshop));
            MenuTheme.Button(content, "게임 재개", Resume);
        }
    }
}
