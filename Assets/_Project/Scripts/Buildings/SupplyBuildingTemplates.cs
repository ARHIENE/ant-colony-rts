using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public static class SupplyBuildingTemplates
    {
        public static bool IsKind(BuildingKind kind) => kind >= BuildingKind.PlanterBed && kind <= BuildingKind.FuelCharger
            || kind is BuildingKind.Mill or BuildingKind.OilPress or BuildingKind.BrewingBarrel or BuildingKind.Still
            || kind is BuildingKind.DeepDrill or BuildingKind.GasExtractor or BuildingKind.GasPipe or BuildingKind.GasTank or BuildingKind.AdvancedDeepDrill or BuildingKind.GasSmelter or BuildingKind.GasGenerator or BuildingKind.ChemistryBench or BuildingKind.CharcoalKiln or BuildingKind.CokeOven;
        public static (AntColony.Data.ResourceType type, int amount)[] Cost(BuildingKind kind) => kind switch
        {
            BuildingKind.CharcoalKiln => new[] { (AntColony.Data.ResourceType.Stone, 10), (AntColony.Data.ResourceType.Clay, 5) },
            BuildingKind.CokeOven => new[] { (AntColony.Data.ResourceType.Brick, 10), (AntColony.Data.ResourceType.Iron, 5) },
            BuildingKind.Mill or BuildingKind.OilPress => new[] { (AntColony.Data.ResourceType.Wood, 10), (AntColony.Data.ResourceType.Stone, 5) },
            BuildingKind.BrewingBarrel => new[] { (AntColony.Data.ResourceType.Plank, 10) },
            BuildingKind.Still => new[] { (AntColony.Data.ResourceType.Copper, 10), (AntColony.Data.ResourceType.Brick, 5) },
            BuildingKind.AdvancedDeepDrill => new[] { (AntColony.Data.ResourceType.TitaniumAlloy, 10), (AntColony.Data.ResourceType.Copper, 5) },
            BuildingKind.DeepDrill or BuildingKind.GasExtractor or BuildingKind.GasSmelter or BuildingKind.GasGenerator or BuildingKind.ChemistryBench => new[] { (AntColony.Data.ResourceType.Steel, 10), (AntColony.Data.ResourceType.Copper, 5) },
            BuildingKind.WaterSource or BuildingKind.OilDeposit => System.Array.Empty<(AntColony.Data.ResourceType, int)>(),
            BuildingKind.FluidPipe or BuildingKind.GasPipe => new[] { (AntColony.Data.ResourceType.Copper, 1) },
            BuildingKind.FluidTank or BuildingKind.GasTank or BuildingKind.FuelCharger => new[] { (AntColony.Data.ResourceType.Steel, 5) },
            BuildingKind.ManualWell or BuildingKind.PlanterBed or BuildingKind.StarterMushroomBed => new[] { (AntColony.Data.ResourceType.Wood, 10) },
            _ => new[] { (AntColony.Data.ResourceType.Iron, 10), (AntColony.Data.ResourceType.Copper, 5) }
        };

        public static GameObject Create(BuildingKind kind)
        {
            if (!IsKind(kind)) return null;
            // 기존 템플릿 탐색과 동일하게 비활성 템플릿을 재사용한다.
            foreach (var b in Object.FindObjectsByType<BuildingBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.Data != null && b.Data.kind == kind && b.name.EndsWith("Template")) return b.gameObject;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false);
            go.name = kind + "Template"; go.transform.localScale = kind == BuildingKind.ChemistryBench ? new Vector3(2, 1.4f, 1.2f) : Vector3.one;
            if (kind is BuildingKind.CharcoalKiln or BuildingKind.CokeOven) go.transform.localScale = new Vector3(2.5f, 2, 2.5f);
            go.GetComponent<Collider>().isTrigger = true;
            var data = ScriptableObject.CreateInstance<BuildingData>(); data.name = kind + "Data";
            data.kind = kind; data.displayName = Name(kind); data.foodCost = data.soilCost = data.specialCost = data.constructionAnts = 0;
            foreach (var cost in Cost(kind)) data.soilCost += cost.amount;
            data.buildTimeSeconds = 5;
            BuildingBase building;
            if (kind == BuildingKind.PlanterBed || kind == BuildingKind.StarterMushroomBed) building = go.AddComponent<StarterCropBed>();
            else if (kind == BuildingKind.SteamGenerator || kind == BuildingKind.GasGenerator) building = go.AddComponent<PowerNode>();
            else if (Processor.IsKind(kind)) building = go.AddComponent<Processor>();
            else building = go.AddComponent<SupplyFacility>();
            building.ConfigureRuntime(data);
            return go;
        }

        private static string Name(BuildingKind kind) => kind switch
        {
            BuildingKind.ChemistryBench => "화학 작업대",
            BuildingKind.CharcoalKiln => "숯가마", BuildingKind.CokeOven => "코크스로",
            BuildingKind.Mill => "제분기", BuildingKind.OilPress => "착유기", BuildingKind.BrewingBarrel => "양조통", BuildingKind.Still => "증류기",
            BuildingKind.AdvancedDeepDrill => "합금 심층 채굴기", BuildingKind.GasSmelter => "가스 제련로", BuildingKind.GasGenerator => "가스 발전기",
            BuildingKind.DeepDrill => "심층 채굴기", BuildingKind.GasExtractor => "가스 추출기",
            BuildingKind.GasPipe => "기체 배관", BuildingKind.GasTank => "기체 탱크",
            BuildingKind.PlanterBed => "화분형 밭", BuildingKind.StarterMushroomBed => "기본 버섯밭",
            BuildingKind.WaterSource => "수원", BuildingKind.OilDeposit => "석유 매장 지점", BuildingKind.ManualWell => "수동 우물",
            BuildingKind.WaterPump => "취수 펌프", BuildingKind.FluidPipe => "기초 배관", BuildingKind.FluidTank => "유체 탱크",
            BuildingKind.SteamGenerator => "증기 고체연료 발전기", BuildingKind.OilDrill => "석유 시추기",
            BuildingKind.OilRefinery => "정유기", BuildingKind.FuelCharger => "연료 충전기", _ => kind.ToString()
        };
    }
}
