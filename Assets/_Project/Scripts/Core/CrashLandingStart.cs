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
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "불시착 잔해"; go.SetActive(false);
            go.transform.position = FindFree(origin, new Vector3(-5, 0, 3)) + Vector3.up * .5f;
            go.transform.localScale = new Vector3(3, 1, 2);
            var data = ScriptableObject.CreateInstance<BuildingData>();
            data.name = "CrashWreck"; data.displayName = "불시착 잔해"; data.kind = BuildingKind.CrashWreck;
            data.foodCost = data.soilCost = data.specialCost = data.constructionAnts = 0; data.buildTimeSeconds = 20;
            go.AddComponent<CrashWreck>().ConfigureRuntime(data); go.SetActive(true);
        }

        private static Vector3 FindFree(Vector3 origin, Vector3 offset)
        {
            if (NavMesh.SamplePosition(origin + offset, out var hit, 8, NavMesh.AllAreas)) return hit.position;
            if (NavMesh.SamplePosition(origin, out hit, 15, NavMesh.AllAreas)) return hit.position;
            throw new InvalidOperationException("시작 물자를 배치할 수 있는 땅이 없습니다.");
        }
    }
}
