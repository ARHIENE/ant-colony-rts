using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Core
{
    public enum DefenseLine { Firepower, Range, Durability, Traps }

    // 과학 기술 30개의 효과 수치를 한 곳에서 조회한다. 각 시스템은 여기 값을 곱하거나 더하기만 한다.
    // 한파·홍수·가뭄·산불·곰팡이 관련 값은 4단계 랜덤 이벤트가 이 값을 읽는다.
    public static class ScienceEffects
    {
        public static bool Has(ScienceTechnology t) => CampaignResearch.Instance != null && CampaignResearch.Instance.Has(t);

        // 농사
        public static float FarmYieldMultiplier => Has(ScienceTechnology.FungalFarming) ? 1.2f : 1f;
        public static bool WideFarms => Has(ScienceTechnology.FungalFarming);
        public static bool CropUnlocked(FarmCrop crop) => crop == FarmCrop.Fungus
            || (crop == FarmCrop.Honeydew && Has(ScienceTechnology.HoneydewRanch))
            || (crop == FarmCrop.AdvancedFungus && Has(ScienceTechnology.AdvancedCrops));

        // 의료
        public static float MinorHealRate => Has(ScienceTechnology.Herbs) ? 180f / 120f : 1f;
        public static float SeriousTreatRate => Has(ScienceTechnology.Herbs) ? 240f / 180f : 1f;
        public static float MoldSpreadMultiplier => Has(ScienceTechnology.Sanitation) ? .5f : 1f;
        public static float MoldTreatSeconds => Has(ScienceTechnology.Sanitation) ? 30f : 60f;

        // 재해(4단계 이벤트가 사용)
        public static bool FloodImmune => Has(ScienceTechnology.Drainage);
        public static float DroughtGrowthPenalty => Has(ScienceTechnology.Drainage) ? .2f : .5f;
        public static float WildfireDamageMultiplier => Has(ScienceTechnology.Firebreaks) ? .5f : 1f;
        public static bool FirebreaksBlockSpread => Has(ScienceTechnology.Firebreaks);
        public static float ColdEffectMultiplier => Has(ScienceTechnology.Insulation) ? .5f : 1f;

        // 수송
        public static float RouteIntervalSeconds => Has(ScienceTechnology.EfficientTransport) ? 30f : 60f;
        public static float CargoMultiplier => Has(ScienceTechnology.EfficientTransport) ? 1.5f : 1f;
        public static float TroopCapacityMultiplier => Has(ScienceTechnology.HeavyTransport) ? 1.5f : 1f;
        public static int CommanderSlotBonus => Has(ScienceTechnology.HeavyTransport) ? 2 : 0;

        // 제작(3단계 공방이 사용)
        public static float QualityUpgradeChance => Has(ScienceTechnology.AdvancedWeapons) ? .1f : 0f;
        public static bool CanCraft(WeaponKind kind) => kind switch
        {
            WeaponKind.Mandible or WeaponKind.AcidSprayer => Has(ScienceTechnology.Blades),
            _ => Has(ScienceTechnology.AdvancedWeapons)
        };
        // 비행 날개는 활공 날개 연구 + 날개 훈련장(비행 병영) T1 이상이 필요하다.
        public static bool CanCraft(ArmorKind kind) => kind == ArmorKind.Coating
            ? Has(ScienceTechnology.ArmorPlates)
            : Has(ScienceTechnology.Gliding) && System.Array.Exists(UnityEngine.Object.FindObjectsByType<Barracks>(UnityEngine.FindObjectsSortMode.None),
                b => b.isActiveAndEnabled && b.Role == UnitRole.Flying && b.CurrentTier >= 1);
        public static bool CanCraftTrinkets => Has(ScienceTechnology.Trinkets);

        // 방어
        public static float AcidTowerDamageMultiplier => (Has(ScienceTechnology.AcidRefining) ? 1.25f : 1f) * DefenseUpgrades.FirepowerMultiplier;

        public static bool BuildingUnlocked(BuildingKind kind) => kind switch
        {
            BuildingKind.SoilWall => Has(ScienceTechnology.Resin),
            BuildingKind.TrapPit => Has(ScienceTechnology.Traps),
            BuildingKind.AreaAcidTower or BuildingKind.DefenseLab => Has(ScienceTechnology.AcidRefining),
            BuildingKind.Watchtower => Has(ScienceTechnology.Watchtowers),
            BuildingKind.MineField => Has(ScienceTechnology.Mines),
            BuildingKind.RestRoom => Has(ScienceTechnology.Recreation),
            BuildingKind.Workshop => Has(ScienceTechnology.Blades),
            BuildingKind.Infirmary => Has(ScienceTechnology.Infirmary),
            BuildingKind.AirshipYard => Has(ScienceTechnology.MigrationTheory),
            _ => true
        };
    }

    // 방어시설 연구소 4라인 × 3단계. 단계 비용 = 단계 × F30·S40, 3단계는 Special 5 추가.
    public static class DefenseUpgrades
    {
        public const int MaxLevel = 3;
        public static int Level(DefenseLine line) => CampaignResearch.Instance != null ? CampaignResearch.Instance.DefenseLevel(line) : 0;
        public static float FirepowerMultiplier => 1f + .15f * Level(DefenseLine.Firepower);
        public static float RangeBonus => 1.5f * Level(DefenseLine.Range);
        public static float DurabilityMultiplier => 1f + .2f * Level(DefenseLine.Durability);
        public static bool TrapAutoRepair => Level(DefenseLine.Traps) >= MaxLevel;

        public static void Cost(int nextLevel, out int food, out int soil, out int special)
        { food = 30 * nextLevel; soil = 40 * nextLevel; special = nextLevel == MaxLevel ? 5 : 0; }

        public static bool TryUpgrade(DefenseLine line)
        {
            var research = CampaignResearch.Instance;
            var next = Level(line) + 1;
            if (research == null || next > MaxLevel || !DefenseLab.AnyActive || ResourceManager.Instance == null) return false;
            Cost(next, out var food, out var soil, out var special);
            if (!ResourceManager.Instance.TrySpend(food, soil, special, reason: ResourceReason.Research)) return false;
            research.SetDefenseLevel(line, next);
            return true;
        }
    }
}
