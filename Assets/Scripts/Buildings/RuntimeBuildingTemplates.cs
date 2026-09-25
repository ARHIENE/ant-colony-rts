using AntColony.Data;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Buildings
{
    // 씬 템플릿이 없는 과학 시설의 비활성 템플릿을 만든다(이름이 Template으로 끝나 이후 FindTemplate이 재사용한다).
    // 비용·인력·시간·체력은 작업 지시서 2단계 표 값이다.
    internal static class RuntimeBuildingTemplates
    {
        internal static GameObject Create(BuildingKind kind)
        {
            // 함정·매설지는 밟혀야 하므로 길을 막지 않는 납작한 발판이다.
            var (name, scale, color, food, soil, special, ants, seconds, hp, walkable) = kind switch
            {
                BuildingKind.SoilWall => ("Soil Wall", new Vector3(2, 1.5f, .6f), new Color(.45f, .33f, .2f), 0, 25, 0, 3, 4f, 400f, false),
                BuildingKind.TrapPit => ("Trap Pit", new Vector3(1.6f, .1f, 1.6f), new Color(.3f, .22f, .12f), 10, 30, 0, 3, 5f, 100f, true),
                BuildingKind.AreaAcidTower => ("Area Acid Tower", new Vector3(1.6f, 2.4f, 1.6f), new Color(.55f, .8f, .3f), 40, 70, 0, 6, 10f, 220f, false),
                BuildingKind.Watchtower => ("Watchtower", new Vector3(1.2f, 3.5f, 1.2f), new Color(.6f, .55f, .4f), 20, 40, 0, 3, 6f, 150f, false),
                BuildingKind.MineField => ("Mine Field", new Vector3(1.4f, .1f, 1.4f), new Color(.6f, .25f, .2f), 20, 20, 2, 2, 3f, 50f, true),
                BuildingKind.DefenseLab => ("Defense Lab", new Vector3(3, 2, 3), new Color(.4f, .5f, .6f), 60, 80, 0, 6, 12f, 300f, false),
                BuildingKind.RestRoom => ("Rest Room", new Vector3(3, 1.5f, 3), new Color(.8f, .7f, .55f), 40, 60, 0, 4, 8f, 300f, false),
                BuildingKind.Workshop => ("공방", new Vector3(3, 2, 3), new Color(.7f, .5f, .3f), GameBalance.WorkshopFood, GameBalance.WorkshopSoil, GameBalance.WorkshopSpecial, GameBalance.WorkshopAnts, GameBalance.WorkshopBuildSeconds, 300f, false),
                _ => (null, Vector3.one, Color.white, 0, 0, 0, 0, 0f, 0f, false)
            };
            if (name == null) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.SetActive(false);
            go.name = kind + "Template";
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.kind = kind; data.displayName = name;
            data.foodCost = food; data.soilCost = soil; data.specialCost = special;
            data.constructionAnts = ants; data.buildTimeSeconds = seconds; data.maxHealth = hp;
            BuildingBase building = kind switch
            {
                BuildingKind.SoilWall => go.AddComponent<SoilWall>(),
                BuildingKind.TrapPit => go.AddComponent<TrapPit>(),
                BuildingKind.AreaAcidTower => go.AddComponent<AreaAcidTower>(),
                BuildingKind.Watchtower => go.AddComponent<Watchtower>(),
                BuildingKind.MineField => go.AddComponent<MineField>(),
                BuildingKind.DefenseLab => go.AddComponent<DefenseLab>(),
                BuildingKind.Workshop => go.AddComponent<Workshop>(),
                _ => go.AddComponent<RestRoom>()
            };
            building.ConfigureRuntime(data);
            if (walkable) go.GetComponent<Collider>().isTrigger = true;
            else go.AddComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
            return go;
        }
    }
}
