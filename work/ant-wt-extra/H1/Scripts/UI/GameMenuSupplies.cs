using AntColony.Core;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void CarriedSupplies()
        {
            var w = V5Window("장수 소지품", "배고프면 소지 식량을 먼저 먹습니다. 개인 식량은 창고 재고에 포함되지 않습니다.");
            L.Label(w, "장수", 13, 24, 112, 180, 26, bold: true);
            L.Label(w, "무기 · 도구 · 복장", 13, 204, 112, 430, 26, bold: true);
            L.Label(w, "보존식 · 무게", 13, 644, 112, 420, 26, bold: true);
            var list = L.List(w, 24, 148, 1052, 476);
            foreach (var c in SortedCommanders())
            {
                var row = L.Cell(list, c.CommanderName + " Supplies", 96);
                L.Label(row, c.CommanderName, 15, 0, 0, 170, 36, bold: true);
                L.Label(row, (c.Weapon?.Label ?? "맨손") + " · " + (c.PersonalState.toolTier >= 2 ? "돌 도구" : c.PersonalState.toolTier == 1 ? "나무 도구" : "도구 없음")
                    + "\n" + c.PersonalState.ordinaryClothes, 13, 180, 0, 430, 68);
                L.Label(row, $"{c.CarriedRationMeals}끼 · 소지 {c.StartingLoadWeight:0.0}/{c.TotalLoadCapacity:0.0}\n남은 보존 {(c.PersonalState.carriedRations?.Stalest ?? 0) / 150f:0.0}일", 13, 620, 0, 230, 68);
                var drop = L.Button(row, "Drop Rations " + c.CommanderName, "1끼 내려놓기", 854, 20, 166, 34, () => { c.DropCarriedRations(1); CarriedSupplies(); }, "발밑에 내려놓습니다. 원래 신선도를 유지합니다.");
                drop.interactable = c.CarriedRationMeals > 0 && c.CanReceiveOrders && !c.IsAwayFromHome;
                L.Line(row, 0, 88, 1020);
            }
        }
    }
}
