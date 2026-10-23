using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 2026-10-19 계단식 지형: 높이는 단계 평지뿐(경사로만 소수), 본거지 평지, 물웅덩이는 NavMesh에서 빠지고 건설 불가,
// 경사로로 모든 언덕 평지에 걸어서 닿는다. 물 많은 물가·보통 숲·물 없는 사막 3종.
public static class TerraceTerrainChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Terrace-" + Guid.NewGuid().ToString("N"));
        try
        {
            var summary = "";
            foreach (var (seed, biome) in new[] { (261019, MapBiome.Waterside), (77, MapBiome.Forest), (4242, MapBiome.Desert),
                (11, MapBiome.Waterside), (1234, MapBiome.Forest), (9001, MapBiome.Desert) })
                summary += await One(seed, biome) + " ";
            return $"PASS {checks} terrace terrain checks: {summary}";
        }
        finally { SaveStorage.RootOverride = original; }
    }

    // 메모리가 제한된 에디터에서는 맵별로 실행하고 Play 세션을 종료한다.
    public static async Task<string> Single(int seed, int biome, int size = 0)
    {
        checks = 0;
        Check(Application.isPlaying, "play mode");
        var original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Terrace-" + Guid.NewGuid().ToString("N"));
        try { return $"PASS: {await One(seed, (MapBiome)biome, (MapSize)size)} ({checks} checks)"; }
        finally { SaveStorage.RootOverride = original; }
    }


    static async Task<string> One(int seed, MapBiome biome, MapSize size = MapSize.Small)
    {
        Check(Application.isPlaying, "play mode before new map");
        var water = biome != MapBiome.Desert;
        await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = seed, mapSize = size, biome = biome }); await Ready();
        await Task.Delay(200);
        var maps = Object.FindObjectsByType<MapGenerator>(FindObjectsSortMode.None);
        Check(maps.Length == 1, biome + " exactly one active terrain without test cleanup");
        var built = maps.Single();
        Check(MapGenerator.Active == built, biome + " home terrain stays active over the scene copy");

        var map = MapGenerator.Active;
        var terrace = (TerraceHeights.Result)typeof(MapGenerator).GetField("terrace", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
        var baseY = (float)typeof(MapGenerator).GetField("baseY", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
        var w = map.BaseXSize + 1;
        var verts = map.GetComponent<MeshFilter>().sharedMesh.vertices;
        var home = Object.FindFirstObjectByType<Stockpile>().Position;

        int ramp = 0, pit = 0; var levels = new System.Collections.Generic.HashSet<float>();
        foreach (var h in terrace.heights) { if (h != Mathf.Round(h)) ramp++; else levels.Add(h); if (h < 0) pit++; }
        Check(levels.Count >= (water ? 3 : 2), biome + " height steps (" + string.Join(",", levels) + ")");
        Check(ramp > 0 && ramp < terrace.heights.Length * .05f, biome + " ramps are the only sloped ground (" + ramp + ")");
        Check(water == (pit > 0 && map.WaterMesh != null && !float.IsNaN(MapGenerator.WaterLevel)), biome + " water pits " + pit);
        Check(MapGenerator.FlatAt(home, new Vector3(12, 1, 12)), biome + " home zone is flat");

        int deep = -1, rampAt = -1, rows = verts.Length / w;
        for (int i = 0; i < terrace.heights.Length; i++)
        {
            int x = i % w, z = i / w;
            if (rampAt < 0 && terrace.heights[i] != Mathf.Round(terrace.heights[i])) rampAt = i;
            if (deep >= 0 || x < 2 || z < 2 || x > w - 3 || z > rows - 3) continue;
            bool all = true;
            for (int dz = -2; dz <= 2 && all; dz++) for (int dx = -2; dx <= 2 && all; dx++) all = terrace.heights[i + dz * w + dx] < 0;
            if (all) deep = i;
        }
        Check(rampAt >= 0 && !MapGenerator.FlatAt(map.transform.TransformPoint(verts[rampAt]), Vector3.one), biome + " cannot build on a ramp");
        if (water)
        {
            // 웅덩이 한가운데(2칸 안이 전부 웅덩이)는 NavMesh가 없고 건설도 안 된다.
            Check(deep >= 0, biome + " found a deep pit");
            var pitPoint = map.transform.TransformPoint(verts[deep]);
            Check(!NavMesh.SamplePosition(pitPoint, out _, 1f, NavMesh.AllAreas), biome + " pit floor is not walkable");
            Check(!NavMesh.SamplePosition(pitPoint + Vector3.up * (baseY - TerraceHeights.WaterDepth - verts[deep].y), out _, 1f, NavMesh.AllAreas), biome + " water surface is not walkable");
            Check(!MapGenerator.FlatAt(pitPoint, Vector3.one), biome + " cannot build in a pit");
        }

        // 평지 표본(언덕 포함)이 본거지에서 걸어서 닿는가.
        var random = new System.Random(7); int sampled = 0, reached = 0, highSampled = 0, missing = 0; var unreached = "";
        for (int tries = 0; tries < 20000 && sampled < 300; tries++)
        {
            var i = random.Next(terrace.heights.Length);
            if (!terrace.open[i] || terrace.heights[i] < 0) continue;
            var p = map.transform.TransformPoint(verts[i]);
            // 큰 나무 바로 옆 점은 길찾기 조각에 붙어 지형 연결과 무관하게 실패할 수 있어 뺀다.
            if (Physics.OverlapSphere(p + Vector3.up, 2.5f).Any(c => c.gameObject != map.gameObject)) continue;
            if (!NavMesh.SamplePosition(p, out var hit, .6f, NavMesh.AllAreas)) { missing++; sampled++; continue; }
            sampled++; if (terrace.heights[i] >= 1) highSampled++;
            if (NavRoute.Reachable(home, hit.position)) reached++; else unreached += $" ({i % w},{i / w} h{terrace.heights[i]})";
        }
        Check(sampled >= 200 && highSampled > 20, $"{biome} sampled ground ({sampled}, high {highSampled})");
        Check(missing == 0, $"{biome} unobstructed flat ground has NavMesh ({missing} missing)");
        Check(reached == sampled, $"{biome} reachable from home {reached}/{sampled}{unreached}");
        if (seed == 9001 && biome == MapBiome.Desert)
        {
            foreach (var local in new[] { new Vector2Int(225, 116), new Vector2Int(219, 112),
                new Vector2Int(203, 118), new Vector2Int(207, 114), new Vector2Int(235, 130) })
            {
                var target = map.transform.TransformPoint(verts[local.y * w + local.x]);
                var path = new NavMeshPath();
                Check(NavMesh.CalculatePath(home, target, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete, $"desert 9001 formerly blocked hill {local}");
            }
        }
        return $"{biome} {size}(steps {levels.Count}, ramp {ramp}, pit {pit}, reach {reached}/{sampled})";
    }

    static TerraceHeights.Result Terrace() => (TerraceHeights.Result)typeof(MapGenerator)
        .GetField("terrace", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(MapGenerator.Active);

    // 저장 후 불러와도 같은 지형(단계 높이·수면)이 다시 만들어지는가.
    public static async Task<string> SaveLoad(int seed, int biome)
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Terrace-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = seed, mapSize = MapSize.Small, biome = (MapBiome)biome }); await Ready();
            await Task.Delay(200);
            var before = (float[])Terrace().heights.Clone(); var water = MapGenerator.WaterLevel;
            var waterVerts = MapGenerator.Active.WaterMesh != null ? MapGenerator.Active.WaterMesh.vertexCount : 0;
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error);
            await Task.Delay(200); await Ready(); await Task.Delay(200);
            Check(Object.FindObjectsByType<MapGenerator>(FindObjectsSortMode.None).Length == 1, "one terrain after load");
            var after = Terrace().heights;
            Check(after.Length == before.Length && after.SequenceEqual(before), "terrace heights identical after load");
            Check(water.Equals(MapGenerator.WaterLevel), $"water level {water} -> {MapGenerator.WaterLevel}");
            Check(waterVerts == (MapGenerator.Active.WaterMesh != null ? MapGenerator.Active.WaterMesh.vertexCount : 0), "water mesh after load");
            return $"PASS save/load {(MapBiome)biome} ({checks} checks)";
        }
        finally { SaveStorage.RootOverride = original; }
    }

    // 현재 지형에서 실제 NavMeshAgent가 본거지에서 경사로를 타고 가장 높은 단계 평지까지 걸어 올라가는가.
    public static async Task<string> WalkUp()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var map = MapGenerator.Active; var terrace = Terrace();
        var verts = map.GetComponent<MeshFilter>().sharedMesh.vertices;
        var home = Object.FindFirstObjectByType<Stockpile>().Position;
        var top = terrace.heights.Max(); var target = Vector3.zero; var best = float.MaxValue;
        for (int i = 0; i < verts.Length; i++)
        {
            if (terrace.heights[i] != top || terrace.clear[i]) continue;
            var p = map.transform.TransformPoint(verts[i]); var d = (p - home).sqrMagnitude;
            if (d < best && NavMesh.SamplePosition(p, out _, .6f, NavMesh.AllAreas)) { best = d; target = p; }
        }
        Check(best < float.MaxValue, "found top-level target");
        NavMesh.SamplePosition(home, out var start, 5f, NavMesh.AllAreas);
        var go = new GameObject("TerraceWalker"); go.transform.position = start.position;
        var agent = go.AddComponent<NavMeshAgent>(); agent.speed = 20; agent.acceleration = 100; agent.radius = .3f;
        var scale = Time.timeScale; Time.timeScale = 4;
        try
        {
            await Task.Delay(100);
            Check(agent.isOnNavMesh && agent.SetDestination(target), "walker on navmesh");
            for (int t = 0; t < 600 && (agent.pathPending || agent.remainingDistance > .8f); t++) await Task.Delay(50);
            var left = (go.transform.position - target).magnitude;
            Check(left < 1.5f, $"walker reached top ({left:F1}m left, path {agent.pathStatus})");
            return $"PASS walk {Vector3.Distance(start.position, target):F0}m, climbed {go.transform.position.y - start.position.y:F1}m to level {top} ({checks} checks)";
        }
        finally { Time.timeScale = scale; Object.Destroy(go); }
    }

    // 먼 길(한 번에 계산하면 PathPartial)을 NavRoute.SetDestination으로 보내 실제로 도착하는가(2026-10-22).
    public static async Task<string> WalkFar(int x, int z)
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var map = MapGenerator.Active; var verts = map.GetComponent<MeshFilter>().sharedMesh.vertices; var w = map.BaseXSize + 1;
        Check(NavMesh.SamplePosition(map.transform.TransformPoint(verts[z * w + x]), out var target, 1f, NavMesh.AllAreas), "target on navmesh");
        NavMesh.SamplePosition(Object.FindFirstObjectByType<Stockpile>().Position, out var start, 5f, NavMesh.AllAreas);
        var direct = new NavMeshPath(); NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, direct);
        var go = new GameObject("TerraceFarWalker"); go.transform.position = start.position;
        var agent = go.AddComponent<NavMeshAgent>(); agent.speed = 40; agent.acceleration = 200; agent.angularSpeed = 1000; agent.radius = .3f;
        var scale = Time.timeScale; Time.timeScale = 8;
        try
        {
            await Task.Delay(100);
            NavRoute.SetDestination(agent, target.position);
            Check(go.GetComponent<NavRouteFollower>() != null || direct.status == NavMeshPathStatus.PathComplete, "far route uses waypoints");
            var t = 0;
            for (; t < 1200 && (go.transform.position - target.position).magnitude > 1.5f; t++)
            {
                NavRoute.SetDestination(agent, target.position); // 유닛처럼 매 프레임 같은 목적지를 다시 줘도 진행을 잃지 않아야 한다
                await Task.Delay(50);
            }
            var left = (go.transform.position - target.position).magnitude;
            Check(left < 1.5f, $"far walker arrived ({left:F1}m left)");
            return $"PASS far walk direct {direct.status}, arrived after {t * .05f * 8:F0} game s ({checks} checks)";
        }
        finally { Time.timeScale = scale; Object.Destroy(go); }
    }
}
