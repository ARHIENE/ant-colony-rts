using System;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void ShowWorkshop(Workshop workshop)
        {
            if (workshop == null) { Science(); return; }
            Screen("공방 / 장비 보관함");
            var inventory = EquipmentInventory.Instance;
            MenuTheme.Text(content, $"보관함 {inventory?.Items.Count ?? 0}/{EquipmentInventory.Capacity} · 대기열 {workshop.Jobs.Count}/{GameBalance.CraftQueueCapacity}", 19, 45);
            MenuTheme.Text(content, workshop.Ruined ? "파괴됨 — 제작 중단, 대기열 취소 가능" : $"제작 장수: {workshop.Crafter?.CommanderName ?? "미배정"}", 18, 45);
            foreach (EquipmentRecipe recipe in Enum.GetValues(typeof(EquipmentRecipe)))
            {
                if (!EquipmentRecipes.Unlocked(recipe)) continue;
                var cost = EquipmentRecipes.Cost(recipe);
                MenuTheme.Button(content, $"{EquipmentRecipes.Name(recipe)} — Food {cost.x} / Soil {cost.y} / Special {cost.z}", () => {
                    if (!workshop.TryEnqueue(recipe)) ToastManager.Show("자원·대기열·보관함 빈칸을 확인하세요."); ShowWorkshop(workshop);
                }).interactable = !workshop.Ruined && workshop.isActiveAndEnabled && workshop.Jobs.Count < GameBalance.CraftQueueCapacity && inventory != null && !inventory.Full;
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
            MenuTheme.Text(content, "공방 8m 안 유휴 장수를 배정하세요. 게임을 재개하면 제작이 진행됩니다. 장비 장착·해제는 장수 상세에서 가능합니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowWorkshop(workshop));
            MenuTheme.Button(content, "게임 재개", Resume);
        }
    }
}
