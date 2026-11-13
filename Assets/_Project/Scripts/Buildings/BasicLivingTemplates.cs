using AntColony.Data;
using UnityEngine;

namespace AntColony.Buildings
{
    public static class BasicLivingTemplates
    {
        // B1 소유 런타임 템플릿은 그대로 두고, 기존 생활 가구 정의만 보정한다.
        public static GameObject Prepare(GameObject template, BuildingKind kind)
        {
            // Use the same full-sized footprint for placement, construction and navigation.
            // The old 1m-wide bed shrank the imported mattress below the resident's body length.
            if (template != null)
            {
                var size = kind switch
                {
                    BuildingKind.SingleBed => new Vector3(1.65f, .85f, 2.8f),
                    BuildingKind.BigTable => new Vector3(2.4f, 1.2f, 3.2f),
                    BuildingKind.Bookshelf => new Vector3(2.2f, 2.6f, 1.05f),
                    BuildingKind.Carpentry => new Vector3(2.5f, 1.5f, 1.95f),
                    _ => Vector3.zero
                };
                if (size != Vector3.zero) template.transform.localScale = size;
            }
            if (template == null || kind != BuildingKind.SleepingMat && kind != BuildingKind.Toilet
                && kind != BuildingKind.Washbasin && kind != BuildingKind.SingleBed) return template;
            var building = template.GetComponent<BuildingBase>();
            if (building.Data.constructionAnts == 0 && (kind != BuildingKind.Toilet || building.Data.displayName == "구덩이 변소")
                && (kind != BuildingKind.SleepingMat || building.Data.foodCost == 0 && building.Data.soilCost == 0
                    && building.Data.specialCost == 0 && building.Data.buildTimeSeconds == 0)) return template;
            var data = Object.Instantiate(building.Data);
            data.constructionAnts = 0;
            if (kind == BuildingKind.SleepingMat)
            {
                data.foodCost = data.soilCost = data.specialCost = 0;
                data.buildTimeSeconds = 0;
            }
            if (kind == BuildingKind.Toilet) data.displayName = "구덩이 변소";
            building.ConfigureRuntime(data);
            return template;
        }
    }
}
