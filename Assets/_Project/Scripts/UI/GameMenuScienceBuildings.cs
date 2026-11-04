using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.UI
{
    // 과학 화면의 2단계 시설 건설, 방어시설 연구소 업그레이드, 새 밭 작물 선택.
    public sealed partial class GameMenuController
    {
        private static readonly BuildingKind[] ScienceBuildingKinds =
        {
            BuildingKind.SoilWall, BuildingKind.TrapPit, BuildingKind.SpikeTrap, BuildingKind.AreaAcidTower, BuildingKind.Watchtower,
            BuildingKind.MineField, BuildingKind.DefenseLab, BuildingKind.RestRoom, BuildingKind.Workshop, BuildingKind.UndergroundScanner, BuildingKind.DeepDrill, BuildingKind.GasExtractor, BuildingKind.GasPipe, BuildingKind.GasTank, BuildingKind.AdvancedDeepDrill, BuildingKind.GasSmelter, BuildingKind.GasGenerator
        };

        private void ScienceBuildings()
        {
            foreach (var workshop in AntColony.Core.SceneObjects.All<Workshop>())
                MenuTheme.Button(content, "공방 열기: " + workshop.Data.displayName, () => ShowWorkshop(workshop));
            var placement = AntColony.Core.SceneObjects.Any<BuildingPlacementController>();
            foreach (var kind in ScienceBuildingKinds)
            {
                var data = BuildingPlacementController.GetTemplate(kind, UnitRole.Worker)?.GetComponent<BuildingBase>()?.Data;
                if (data == null) continue;
                var special = data.specialCost > 0 ? $"/특수 자원 {data.specialCost}" : "";
                var captured = kind;
                MenuTheme.Button(content, $"{data.displayName} 건설 (식량 {data.foodCost}/재료 {data.soilCost}{special})",
                    () => { Resume(); placement?.BeginSciencePlacement(captured); },
                    ScienceEffects.BuildingUnlocked(kind) ? "본거지에서 건설 담당 장수를 고른 뒤 배치하세요." : "해당 시설의 선행 연구를 먼저 완료하세요.")
                    .interactable = ScienceEffects.BuildingUnlocked(kind);
            }
            MenuTheme.Button(content, $"새 밭 작물: {CropName(BuildingPlacementController.SelectedCrop)} (변경)",
                () => { BuildingPlacementController.CycleCrop(); Science(); }, "새로 배치할 밭의 작물을 바꿉니다. 감로·고급 균류는 연구가 필요합니다.");
            if (!DefenseLab.AnyActive) return;
            foreach (DefenseLine line in System.Enum.GetValues(typeof(DefenseLine)))
            {
                var level = DefenseUpgrades.Level(line);
                if (level >= DefenseUpgrades.MaxLevel) { MenuTheme.Text(content, $"방어 {DisplayNames.Defense(line)}: {level}단계 (최고)", 17, 32); continue; }
                DefenseUpgrades.Cost(level + 1, out var food, out var soil, out var special);
                var captured = line;
                MenuTheme.Button(content, $"방어 {DisplayNames.Defense(line)}: {level}→{level + 1}단계 (식량 {food}/재료 {soil}{(special > 0 ? $"/특수 자원 {special}" : "")})",
                    () => { if (!DefenseUpgrades.TryUpgrade(captured)) ToastManager.Show("방어 강화에 필요한 자원이 부족합니다. 표시된 비용을 확인하세요."); Science(); });
            }
        }

        // 부위 재생은 건강 화면의 수술 목록에서 요청한다(2026-10-15 의료 개편).
        private void RegenerationButtons(CommanderAnt c) { }
    }
}
