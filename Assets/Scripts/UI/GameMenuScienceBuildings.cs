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
            BuildingKind.SoilWall, BuildingKind.TrapPit, BuildingKind.AreaAcidTower, BuildingKind.Watchtower,
            BuildingKind.MineField, BuildingKind.DefenseLab, BuildingKind.RestRoom, BuildingKind.Workshop
        };

        private void ScienceBuildings()
        {
            foreach (var workshop in FindObjectsByType<Workshop>())
                MenuTheme.Button(content, "공방 열기: " + workshop.name, () => ShowWorkshop(workshop));
            var placement = FindFirstObjectByType<BuildingPlacementController>();
            foreach (var kind in ScienceBuildingKinds)
            {
                var data = BuildingPlacementController.GetTemplate(kind, UnitRole.Worker)?.GetComponent<BuildingBase>()?.Data;
                if (data == null) continue;
                var special = data.specialCost > 0 ? $"/{data.specialCost} Special" : "";
                var captured = kind;
                MenuTheme.Button(content, $"Build {data.displayName} ({data.foodCost}F/{data.soilCost}S{special}/{data.constructionAnts} ants)",
                    () => { Resume(); placement?.BeginSciencePlacement(captured); },
                    ScienceEffects.BuildingUnlocked(kind) ? "Select an idle commander at home, then place." : "Research the matching science first.")
                    .interactable = ScienceEffects.BuildingUnlocked(kind);
            }
            MenuTheme.Button(content, $"New farm crop: {BuildingPlacementController.SelectedCrop} (change)",
                () => { BuildingPlacementController.CycleCrop(); Science(); }, "Honeydew and advanced fungus unlock through research.");
            if (!DefenseLab.AnyActive) return;
            foreach (DefenseLine line in System.Enum.GetValues(typeof(DefenseLine)))
            {
                var level = DefenseUpgrades.Level(line);
                if (level >= DefenseUpgrades.MaxLevel) { MenuTheme.Text(content, $"Defense {line}: level {level} (max)", 17, 32); continue; }
                DefenseUpgrades.Cost(level + 1, out var food, out var soil, out var special);
                var captured = line;
                MenuTheme.Button(content, $"Defense {line}: level {level} -> {level + 1} ({food}F/{soil}S{(special > 0 ? $"/{special} Special" : "")})",
                    () => { if (!DefenseUpgrades.TryUpgrade(captured)) ToastManager.Show("Insufficient resources."); Science(); });
            }
        }

        private void RegenerationButtons(CommanderAnt c)
        {
            if (!ScienceEffects.Has(ScienceTechnology.Regeneration) || !c.PersonalState.injuries.Exists(i => i.severity == InjurySeverity.Permanent)) return;
            foreach (var infirmary in FindObjectsByType<Infirmary>(UnityEngine.FindObjectsSortMode.None))
                MenuTheme.Button(content, $"Regenerate limb at {infirmary.name} ({GameBalance.RegenerationSpecial} Special, 8 min)",
                    () => { if (!infirmary.TryRegenerate(c)) ToastManager.Show("Requires no serious injury, a free bed and Special."); Details(c); })
                    .interactable = infirmary.CanRegenerate(c);
        }
    }
}
