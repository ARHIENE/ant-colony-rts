using System;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    public static class StartingEnvironment
    {
        public static void Create(Vector3 origin, MapBiome biome, int commanders)
        {
            // 확정 식용 자원은 쓰레기·무작위 지형 자원과 별도로 둔다.
            Place(AntColony.Data.ResourceType.Food, StartSupplyRules.StartFood(commanders), 0, "시작 식용 자원");
            Place(AntColony.Data.ResourceType.Wood, 80, 1, "시작 목재");
            Place(AntColony.Data.ResourceType.Stone, 40, 2, "시작 석재");
            Place(AntColony.Data.ResourceType.CopperOre, 24, 3, "시작 구리광맥");
            Place(AntColony.Data.ResourceType.TinOre, 12, 4, "시작 주석광맥");
            Place(AntColony.Data.ResourceType.Coal, 30, 5, "시작 석탄광맥");
            Place(AntColony.Data.ResourceType.IronOre, 30, 6, "시작 철광맥");
            DiplomacyManager.DropFloor(AntColony.Data.ResourceType.PlantingMaterial, commanders * 3, Ground(origin, 7));
            Source(BuildingKind.WaterSource, 8, StartSupplyRules.WaterName(biome));
            Source(BuildingKind.OilDeposit, 9, "석유 매장 지점");

            void Place(AntColony.Data.ResourceType type, int amount, int slot, string name)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.SetActive(false);
                go.name = name; go.transform.position = Ground(origin, slot) + Vector3.up * .3f;
                go.transform.localScale = Vector3.one * .6f; go.GetComponent<Collider>().isTrigger = true;
                go.AddComponent<ResourceNode>().ConfigureNatural(type, amount, 0); go.SetActive(true);
            }
            void Source(BuildingKind kind, int slot, string name)
            {
                var go = UnityEngine.Object.Instantiate(SupplyBuildingTemplates.Create(kind));
                go.name = name; go.transform.position = Ground(origin, slot); go.SetActive(true);
            }
        }

        private static Vector3 Ground(Vector3 origin, int slot)
        {
            var angle = slot * Mathf.PI * 2 / 10;
            var desired = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 14;
            if (NavMesh.SamplePosition(desired, out var hit, 8, NavMesh.AllAreas)
                && Vector3.Distance(origin, hit.position) <= StartSupplyRules.Radius
                && AntColony.Map.NavRoute.Reachable(origin, hit.position)) return hit.position;
            if (NavMesh.SamplePosition(origin, out hit, 8, NavMesh.AllAreas)) return hit.position;
            throw new InvalidOperationException("시작 자원·수원을 배치할 수 있는 땅이 없습니다.");
        }
    }
}
