using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 바이옴 맵 스타일(2026-10-03): 바이옴마다 바닥·장식·물이 바뀌고, 본거지는 잠기지 않으며, 낚시터는 걸어갈 수 있는 물가에 있다.
public static class MapStyleChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var report = new StringBuilder();
        var spawned = typeof(MapGenerator).GetField("spawnedObjects", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var biome in BiomeRules.All)
        {
            var style = BiomeMapStyle.For(biome);
            Check(style != null && style.layers.Count == 5 && style.spawns.Count > 5, biome + " style asset");
            while (SaveSystem.Busy) await Task.Delay(50);
            var watch = Stopwatch.StartNew();
            SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = biome });
            while (SaveSystem.Busy) await Task.Delay(20);
            watch.Stop(); Time.timeScale = 0;
            var gen = Object.FindAnyObjectByType<MapGenerator>();
            var objects = ((System.Collections.Generic.List<GameObject>)spawned.GetValue(gen)).Where(o => o != null).ToList();
            var home = Object.FindAnyObjectByType<AntColony.Buildings.Stockpile>().Position;
            Check(objects.Count > 150, biome + " decorations spawned: " + objects.Count);
            Check(objects.All(o => style.spawns.Any(s => o.name.StartsWith(s.prefab.name))), biome + " decorations come from biome set");
            Check(float.IsNaN(MapGenerator.WaterLevel) != style.water, biome + " water matches style");
            Check(!MapGenerator.InWater(home), biome + " home above water");
            Check(!style.spawns.Any(s => s.foliage) || gen.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "AntColony/SeasonFoliage"), biome + " foliage uses season shader");
            Check(objects.Where(o => !style.spawns.First(s => o.name.StartsWith(s.prefab.name)).solid).All(o => o.GetComponentInChildren<Collider>() == null), biome + " small decor has no collider");
            var fishing = Object.FindObjectsByType<ResourceNode>().First(n => n.RequiresFishing && n.GetComponentInParent<ExpeditionSite>() == null);
            if (style.water)
            {
                Check(Mathf.Abs(fishing.transform.position.y - MapGenerator.WaterLevel) < .7f, biome + " fishing spot on shore");
                var path = new NavMeshPath();
                Check(NavMesh.SamplePosition(fishing.transform.position, out var f, 3, NavMesh.AllAreas) && NavMesh.SamplePosition(home, out var h, 5, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(h.position, f.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, biome + " fishing spot reachable");
            }
            report.Append($"{biome}: {objects.Count} decor, water {(style.water ? MapGenerator.WaterLevel.ToString("0.0") : "-")}, {watch.ElapsedMilliseconds}ms; ");
        }
        return "PASS " + checks + " map style checks — " + report;
    }
}
