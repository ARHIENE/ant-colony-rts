using System;
using AntColony.Units;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void GroundCrafting()
        {
            var window = V5Window("맨바닥 제작", "작업대와 연구 없이 제작합니다. 재료는 착수할 때 소비하며 진행도는 저장됩니다.");
            var left = V5Section(window, "제작법", 20, 104, 380, 544);
            var right = V5Section(window, "장수별 제작", 414, 104, 666, 544);
            foreach (GroundRecipe recipe in Enum.GetValues(typeof(GroundRecipe)))
            {
                MenuTheme.Text(left, GroundCraftRecipes.Name(recipe), 15, 28);
                MenuTheme.Text(left, GroundCraftRecipes.CostLabel(recipe), 13, 30);
                MenuTheme.Text(left, recipe == GroundRecipe.StoneTools ? "광맥 채굴에 필요 · 제작한 장수가 사용" : "사거리 12m · 공격 간격 2초 · 탄약 없음", 12, 48);
            }
            foreach (var c in SortedCommanders())
            {
                MenuTheme.Text(right, c.CommanderName + " · " + (c.PersonalState.toolTier >= 2 ? "돌 도구" : c.PersonalState.toolTier == 1 ? "나무 도구" : "맨손"), 15, 30);
                if (c.IsGroundCrafting)
                {
                    var state = c.PersonalState.groundCraft;
                    MenuTheme.Text(right, GroundCraftRecipes.Name((GroundRecipe)state.recipe) + $" · {state.work / GroundCraftRecipes.Work:P0}", 13, 28);
                    MenuTheme.Button(right, "제작 취소 · 미진행 100% / 진행 후 70% 반환", () => { c.CancelGroundCraft(); GroundCrafting(); });
                }
                else foreach (GroundRecipe recipe in Enum.GetValues(typeof(GroundRecipe)))
                {
                    var captured = recipe; var block = GroundCraftRecipes.Block(c, recipe);
                    var button = MenuTheme.Button(right, GroundCraftRecipes.Name(recipe) + (block == "" ? " 제작" : " · " + block), () => { c.StartGroundCraft(captured); GroundCrafting(); });
                    button.interactable = block == "";
                }
            }
            MenuTheme.Button(left, "생산 관리", () => Management("생산·자원", "생산"));
            V5Fit(window);
        }
    }
}
