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
            var window = V5Window("공방 / 장비 보관함", "생산 방식과 수량을 선택한 뒤 품목을 추가하세요.");
            content = V5Section(window, "생산 품목 / 주문 설정", 20, 104, 520, 544);
            var inventory = EquipmentInventory.Instance;
            MenuTheme.Text(content, $"보관함 {inventory?.Items.Count ?? 0}/{EquipmentInventory.Capacity} · 대기열 {workshop.Jobs.Count}/{GameBalance.CraftQueueCapacity}", 19, 45);
            MenuTheme.Text(content, workshop.WaitReason, 18, 45);
            // 생산 목록: 방식·수량을 고른 뒤 품목을 누르면 목록에 추가. 제작이 허용된 장수가 자율로 와서 만든다.
            ProductionControls(() => ShowWorkshop(workshop));
            foreach (EquipmentRecipe recipe in Enum.GetValues(typeof(EquipmentRecipe)))
            {
                if (!EquipmentRecipes.Unlocked(recipe)) continue;
                var cost = EquipmentRecipes.Cost(recipe);
                MenuTheme.Button(content, $"목록 추가: {EquipmentRecipes.Name(recipe)} — Food {cost.x} / 재료 {cost.y} / Special {cost.z} · 재고 {Workshop.Stock(recipe)}", () => {
                    if (!workshop.AddOrder(recipe, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowWorkshop(workshop);
                }).interactable = !workshop.Ruined && workshop.isActiveAndEnabled && workshop.Orders.Count < Workshop.MaxOrders;
            }
            content = V5Section(window, "생산 목록 / 진행 상태", 554, 104, 526, 544);
            for (int i = 0; i < workshop.Orders.Count; i++)
            {
                var order = workshop.Orders[i];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}개" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {Workshop.Stock(order.recipe)}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"생산 {i + 1}. {EquipmentRecipes.Name(order.recipe)} · {ModeName(order.mode)} · {state} — 삭제", () => { workshop.Orders.Remove(order); ShowWorkshop(workshop); });
            }
            for (int i = 0; i < workshop.Jobs.Count; i++)
            {
                int index = i; var job = workshop.Jobs[i];
                MenuTheme.Button(content, $"{i + 1}. {job.Label} {job.work / job.WorkRequired:P0} · {(!string.IsNullOrEmpty(job.makerId) ? "원제작자 전용 · 보관 중" : "미착수")} · 취소 (일반 {(job.work > 0 ? 70 : 100)}% · 핵심 100%)",
                    () => { workshop.Cancel(index); ShowWorkshop(workshop); });
            }
            if (workshop.Crafter != null) MenuTheme.Button(content, "장수 배정 해제 (진행도 유지)", () => { workshop.Release(); ShowWorkshop(workshop); });
            else foreach (var c in SortedCommanders())
                if (workshop.CanAssign(c)) MenuTheme.Button(content, "제작 배정: " + c.CommanderName, () => { workshop.TryAssign(c); ShowWorkshop(workshop); });
            MenuTheme.Text(content, "재료는 제작 착수 시 소비합니다. 보스 미완성품도 원제작자만 이어 만듭니다. 사망·영구 이탈 시 자동 인계·취소 없이 보관하며 직접 취소하세요. 취소 시 일반재료 미착수100%/착수70%, 핵심물100%를 환급받아 다른 장수가 새 주문을 만들 수 있습니다.", 17, 110);
            MenuTheme.Button(content, "새로고침", () => ShowWorkshop(workshop));
            V5Fit(window);
        }
    }
}
