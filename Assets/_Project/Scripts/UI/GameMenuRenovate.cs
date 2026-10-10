using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.UI
{
    // 재료 개보수(2026-10-10): 건물 선택 → 재료 변경 → 새 재료·비용 확인 → 장수 현장 작업 → 완료 시 외형·성능 변경.
    public sealed partial class GameMenuController
    {
        public void ShowRenovate(BuildingBase b)
        {
            if (b == null || b.Data == null) { Resume(); return; }
            Screen(b.Data.displayName + " / 재료 개보수");
            MenuTheme.Text(content, "현재 " + BuildScreen.MaterialLine(b.MainMaterial, b.Data.soilCost), 18, 40);
            MenuTheme.Text(content, $"지시할 때 새 재료 {b.Data.soilCost}을(를) 차감하고, 장수가 작업하는 동안 건물을 쓸 수 없습니다. 취소하면 새 재료를 모두 돌려받고, 완료하면 기존 재료의 {GameBalance.DemolishRefundShare:P0}를 돌려받습니다(창고가 차면 건물 주변 바닥).", 16, 60);
            foreach (var m in MaterialInfo.Structural)
            {
                if (m == b.MainMaterial) continue;
                var captured = m; var enough = ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(m) >= b.Data.soilCost;
                MenuTheme.Button(content, "변경: " + BuildScreen.MaterialLine(m, b.Data.soilCost), () => {
                    if (Demolition.OrderRenovate(b, captured) != null) { ToastManager.Show($"{b.Data.displayName} 개보수 예정지를 지정했습니다."); Resume(); }
                    else ToastManager.Show("재료가 부족하거나 지금은 개보수할 수 없습니다.");
                }).interactable = enough && Demolition.CanRenovate(b);
            }
            MenuTheme.Button(content, "게임 재개", Resume);
        }
    }
}
