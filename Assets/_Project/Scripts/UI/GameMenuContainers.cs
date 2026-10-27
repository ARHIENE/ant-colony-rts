using AntColony.Buildings;
using AntColony.Data;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void ShowContainers(SupplyFacility station)
        {
            if (station == null || station.Kind != BuildingKind.FuelCharger) { Resume(); return; }
            var window = V5Window("용기 충전·배출", "용기를 실제 배송한 뒤 제작 담당 장수가 작업합니다. 빈 용기는 재사용합니다.");
            var left = V5Section(window, "작업 선택", 20, 104, 520, 544);
            var names = new[] { "정제유 충전", "원유 충전", "가스 압축", "정제유 배출", "원유 배출", "가스 배출" };
            for (var i = 0; i < names.Length; i++)
            {
                var mode = i;
                MenuTheme.Button(left, (station.Supply.containerMode == i ? "선택 · " : "") + names[i], () => {
                    if (!station.SetContainerMode(mode)) ToastManager.Show("남아 있는 기체/액체를 먼저 배출하거나 사용하세요.");
                    ShowContainers(station);
                });
            }
            MenuTheme.Text(left, "작업 종류를 바꾸면 기존 지시를 취소하고 도착한 용기를 돌려줍니다.", 14, 60);
            var right = V5Section(window, "수량·진행", 560, 104, 520, 544);
            MenuTheme.Text(right, names[station.Supply.containerMode] + " · 남은 " + station.Supply.containerOrders + "개", 19, 44);
            MenuTheme.Text(right, station.ContainerStatus + $" · 작업 {station.Supply.work:0.#}/{SupplyFacility.ProcessSeconds:0}초", 15, 52);
            MenuTheme.Text(right, station.UnpackingContainer ? $"{station.FilledContainer.DisplayName()} 1 → 배관망 {station.ContainerFluid.DisplayName()} {SupplyFacility.ContainerAmount} + {station.EmptyContainer.DisplayName()} 1"
                : $"{station.EmptyContainer.DisplayName()} 1 + 배관망 {station.ContainerFluid.DisplayName()} {SupplyFacility.ContainerAmount} → {station.FilledContainer.DisplayName()} 1", 15, 80);
            foreach (var amount in new[] { 1, 5, 10 })
            { var count = amount; MenuTheme.Button(right, count + "개 지시 추가", () => { station.AddContainerOrder(count); ShowContainers(station); }); }
            MenuTheme.Button(right, station.Supply.containerPaused ? "재개" : "일시정지", () => { station.PauseContainers(!station.Supply.containerPaused); ShowContainers(station); });
            MenuTheme.Button(right, "지시 취소 · 용기 반환", () => { station.CancelContainerWork(); ShowContainers(station); });
            MenuTheme.Button(right, "새로고침", () => ShowContainers(station));
        }
    }
}
