using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Save;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AntColony.Core
{
    public static class CrashLandingStart
    {
        public static void Prepare(NewGameOptions options)
        {
            if (options == null || options.startingCount < 1 || options.startingCount > 3) throw new ArgumentException("시작 장수는 1~3명입니다.");
            if (options.startingTeam == null || options.startingTeam.Length != options.startingCount)
                options.startingTeam = Enumerable.Range(0, options.startingCount).Select(StartingCandidate.Roll).ToArray();
            if (options.startingTeam.Any(c => c == null || !c.Valid)) throw new ArgumentException("시작 후보 정보가 올바르지 않습니다.");
            var missing = StartingCandidate.Missing(options.startingTeam);
            if (missing.Length > 0) throw new InvalidOperationException("수행자 부족: " + string.Join(" · ", missing));
        }
        public static void Begin(NewGameOptions options) { Prepare(options); SaveSystem.NewGame(options); }

        public static void Apply()
        {
            var origin = ResourceManager.HomePosition;
            // 씬 기본 시설은 저장 카탈로그에 빈 자리로 남겨 옛 저장의 키를 유지한다.
            foreach (var b in Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None)
                .Where(b => b.CountsTowardPlayerDefeat && b.GetComponentInParent<ExpeditionSite>() == null).ToArray())
            { b.gameObject.SetActive(false); Object.Destroy(b.gameObject); }
            AntPool.Instance?.RestoreCounts(0, 0, 0);
            if (ColonyPopulation.Instance != null) ColonyPopulation.Instance.Restore(new ColonyPopulation.State());
            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                rm.RestoreState(0, 0, 0, rm.GetCapacity(ResourceType.Food), rm.GetCapacity(ResourceType.Soil), rm.GetCapacity(ResourceType.Special));
                rm.RestoreMaterials(null);
            }
            DiplomacyManager.DropFloor(ResourceType.Stone, 20, FindFree(origin, new Vector3(4, 0, 2)));
            StartingEnvironment.Create(origin, BiomeRules.Current, GameSession.Instance.Options.startingCount);
            CreateWildCrops(origin, BiomeRules.Current);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "불시착 잔해"; go.SetActive(false);
            go.transform.position = FindFree(origin, new Vector3(-5, 0, 3)) + Vector3.up * .5f;
            go.transform.localScale = new Vector3(3, 1, 2);
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.name = "CrashWreck"; data.displayName = "불시착 잔해"; data.kind = BuildingKind.CrashWreck;
            data.foodCost = data.soilCost = data.specialCost = data.constructionAnts = 0; data.buildTimeSeconds = 20;
            go.AddComponent<CrashWreck>().ConfigureRuntime(data); go.SetActive(true);
        }

        public static FarmCrop StartingCrop(MapBiome biome) => biome == MapBiome.Desert ? FarmCrop.Honeydew : FarmCrop.Fungus;

        public static ResourceNode[] CreateWildCrops(Vector3 origin, MapBiome biome)
        {
            var positions = new System.Collections.Generic.List<Vector3>();
            // 수원·시작 자원의 14m 원과 겹치지 않는 안쪽에서 완전 경로를 확인한다.
            for (var i = 0; i < 48 && positions.Count < 3; i++)
            {
                var angle = i * Mathf.PI * 2 / 16;
                var desired = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (6 + i / 16 * 3);
                if (!NavMesh.SamplePosition(desired, out var hit, 2, NavMesh.AllAreas)
                    || Vector3.Distance(origin, hit.position) > StartSupplyRules.Radius
                    || positions.Any(p => (p - hit.position).sqrMagnitude < 4)
                    || !AntColony.Map.NavRoute.Reachable(origin, hit.position)) continue;
                positions.Add(hit.position);
            }
            if (positions.Count != 3) throw new InvalidOperationException("시작 야생 작물 3개에 접근할 수 있는 땅이 없습니다.");
            return positions.Select((position, i) =>
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.SetActive(false);
                go.name = "시작 야생 작물 " + i; go.transform.position = position;
                go.transform.localScale = Vector3.one * .6f; go.GetComponent<Collider>().isTrigger = true;
                var node = go.AddComponent<ResourceNode>(); node.ConfigureNatural(AntColony.Data.ResourceType.Food, 1, 0);
                go.AddComponent<FarmSeedSource>().Configure(StartingCrop(biome), "start:" + i);
                go.SetActive(true); return node;
            }).ToArray();
        }

        private static Vector3 FindFree(Vector3 origin, Vector3 offset)
        {
            if (NavMesh.SamplePosition(origin + offset, out var hit, 8, NavMesh.AllAreas)) return hit.position;
            if (NavMesh.SamplePosition(origin, out hit, 15, NavMesh.AllAreas)) return hit.position;
            throw new InvalidOperationException("시작 물자를 배치할 수 있는 땅이 없습니다.");
        }
    }
}
