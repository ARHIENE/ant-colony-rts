using System.Linq;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    // 첫 제작·연구 시설은 선택 재료가 아니라 확정된 원재료로 짓는다.
    public static class EarlyConstruction
    {
        public static bool HasFixedCost(BuildingKind kind) => kind is BuildingKind.Carpentry or BuildingKind.ScienceLab
            or BuildingKind.ConscriptionPost or BuildingKind.ScoutPost;
        public static (ResourceType type, int amount)[] Cost(BuildingKind kind) => kind switch
        {
            BuildingKind.Carpentry => new[] { (ResourceType.Wood, 10) },
            BuildingKind.ConscriptionPost or BuildingKind.ScoutPost => new[] { (ResourceType.Wood, 20) },
            _ => new[] { (ResourceType.Wood, 20), (ResourceType.Stone, 10) }
        };
        public static string Label(BuildingKind kind) => string.Join(" · ", Cost(kind).Select(c => c.type.DisplayName() + " " + c.amount));
        internal static GameObject Prepare(GameObject template, BuildingKind kind)
        {
            if (template == null || !HasFixedCost(kind)) return template;
            var building = template.GetComponent<BuildingBase>();
            var name = kind switch { BuildingKind.Carpentry => "기초 목공대", BuildingKind.ConscriptionPost => "징집소",
                BuildingKind.ScoutPost => "정찰 초소", _ => "정착지 연구대" };
            var cost = Cost(kind).Sum(c => c.amount);
            if (building.Data.displayName == name && building.Data.constructionAnts == 0 && building.Data.foodCost == 0
                && building.Data.soilCost == cost && building.Data.specialCost == 0) return template;
            var data = Object.Instantiate(building.Data);
            data.displayName = name; data.foodCost = 0; data.soilCost = cost;
            data.specialCost = 0; data.constructionAnts = 0;
            building.ConfigureRuntime(data);
            return template;
        }
    }
}
