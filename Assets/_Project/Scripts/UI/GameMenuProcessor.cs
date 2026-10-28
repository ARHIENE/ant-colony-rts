using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.UI
{
    // 가공대 생산 목록(2026-10-10). 공방 화면과 같은 방식·수량 선택을 쓴다.
    public sealed partial class GameMenuController
    {
        private Processor fermentationCancelConfirm;
        public void ShowProcessor(Processor p)
        {
            if (p == null) { Resume(); return; }
            var window = V5Window(p.Data.displayName + " / 가공", p.Industrial
                ? p.Chemical ? "제작 담당 · 전력 200W · 플라스틱은 배관 원유, 고무는 배송받은 수액 사용"
                    : "제작 담당 · 전력 200W · 묶음 시작 시 가스 1 투입 · 복원 시 중복 소비 없음"
                : "제작 담당의 재료 배송·현장 작업 필요 · 완성품은 바닥에서 운반 · 생산 방식과 수량을 선택하세요.");
            content = V5Section(window, "생산 품목 / 주문 설정", 20, 104, 520, 544);
            var rm = ResourceManager.Instance;
            if (p.Data.kind == AntColony.Data.BuildingKind.Carpentry)
                MenuTheme.Button(content, "돌 도구 · 단궁 제작", GroundCrafting);
            MenuTheme.Text(content, p.Current != null ? $"진행 중: {p.Current.name}" + (p.Brewing ? "" : $" {p.Progress:P0}") : "진행 중인 가공 없음", 19, 45);
            MenuTheme.Text(content, p.WaitReason, 17, 35);
            if (p.Brewing) MenuTheme.Text(content, "원료 투입 후 게임 1일 무인 발효 · 완료 후 장수가 회수합니다.\n시작 후 취소하면 투입 원료는 돌아오지 않습니다.", 14, 58);
            if (p.Chemical) MenuTheme.Text(content, $"내부 원유 {p.BufferedOil} · 플라스틱 취소: 원유 70% 내부 보관 / 고무 취소: 수액 70% 바닥 반환", 14, 58);
            if (p.Current != null) MenuTheme.Button(content, p.Brewing ? fermentationCancelConfirm == p ? "정말 취소 · 투입 원료 소실" : "발효 취소 (원료 소실·확인 필요)" : "미완성품 취소 · 일반 70% / 핵심 100% 반환", () => {
                if (p.Brewing && fermentationCancelConfirm != p) fermentationCancelConfirm = p;
                else { p.CancelCurrent(); fermentationCancelConfirm = null; } ShowProcessor(p);
            });
            ProductionControls(() => ShowProcessor(p));
            for (int i = 0; i < p.Recipes.Count; i++)
            {
                var index = i; var r = p.Recipes[i];
                MenuTheme.Button(content, $"목록 추가: {r.name} {r.amount} ← {(p.Chemical && r.output == AntColony.Data.ResourceType.Plastic ? "배관 원유 " + IndustrialRecipes.PlasticOil : r.Cost)} · 재고 {rm?.GetAmount(r.output) ?? 0}", () => {
                    if (!p.AddOrder(index, orderMode, orderAmount)) ToastManager.Show($"생산 목록은 최대 {Workshop.MaxOrders}개입니다."); ShowProcessor(p);
                }).interactable = p.isActiveAndEnabled && p.Orders.Count < Workshop.MaxOrders;
            }
            content = V5Section(window, "생산 목록 / 진행 상태", 554, 104, 526, 544);
            foreach (var order in p.Orders.ToArray())
            {
                var r = p.Recipes[order.recipe];
                var state = order.mode == Workshop.OrderMode.Count ? $"남은 {order.amount}묶음" : order.mode == Workshop.OrderMode.KeepStock ? $"재고 {Kitchen.Stock(r.output)}/{order.amount}" : "반복";
                MenuTheme.Button(content, $"{r.name} · {ModeName(order.mode)} · {state} — 삭제", () => { p.RemoveOrder(order); ShowProcessor(p); });
            }
            MenuTheme.Text(content, (p.IsMedical ? "작업표에서 의료가 켜진 장수가 할 수 있는 치료가 없을 때 와서 만듭니다(의료 능력은 속도만)." : "작업표에서 제작이 켜진 장수가 자율로 와서 가공합니다.")
                + (p.Chemical ? " 플라스틱 원유는 배관, 고무 수액은 운반으로 공급합니다." : " 재료를 실제 배송받은 뒤 한 묶음 시작 때 소비합니다.")
                + " 결과물은 제작대 주변 바닥에 나오며 운반 담당이 회수합니다.", 17, 70);
            MenuTheme.Button(content, "새로고침", () => ShowProcessor(p));
            V5Fit(window);
        }
    }
}
