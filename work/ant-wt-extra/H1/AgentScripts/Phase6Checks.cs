using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using RT = AntColony.Data.ResourceType;

// Phase 6: 바이옴 6종(사막·동굴 추가), 시작 바이옴 선택, 월드맵 거점 바이옴.
public static class Phase6Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static async Task Start(MapBiome biome)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small, biome = biome });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        checks = 0;
        if (!Application.isPlaying) throw new Exception("Play mode required");
        Check(BiomeRules.All.Length == 6 && BiomeRules.All.Distinct().Count() == 6 && !BiomeRules.All.Contains(MapBiome.None), "six biomes");
        Check(BiomeRules.Name(MapBiome.Desert) == "사막" && BiomeRules.Name(MapBiome.Cave) == "동굴", "new biome names");
        Check(BiomeRules.Difficulty(MapBiome.Garden) == "쉬움" && BiomeRules.Difficulty(MapBiome.Cave) == "매우 어려움", "biome = difficulty");
        for (var i = 0; i < 200; i++) { var r = BiomeRules.Random(); Check(r >= MapBiome.Forest && r <= MapBiome.Cave, "random in six"); }
        Check(BiomeRules.ForSite("개미 언덕") == BiomeRules.ForSite("개미 언덕") && BiomeRules.ForSite("x") != MapBiome.None, "site biome deterministic");
        Check(Math.Abs(BiomeRules.NodeMultiplier(MapBiome.Desert, RT.Food) - .4f) < .001f && Math.Abs(BiomeRules.NodeMultiplier(MapBiome.Desert, RT.Soil) - 1.6f) < .001f, "desert nodes");
        Check(Math.Abs(BiomeRules.NodeMultiplier(MapBiome.Cave, RT.Special) - 1.6f) < .001f, "cave special");

        await Start(MapBiome.Desert);
        Check(BiomeRules.Current == MapBiome.Desert && BiomeRules.FatigueAt(false) == 1.5f && BiomeRules.FatigueAt(true) == 1f && BiomeRules.MoveAt(false) == .9f, "desert heat and antlion sand");
        Check(Math.Abs(BiomeRules.FarmGrowth - .4f) < .001f && BiomeRules.WorkAt(false) == 1f, "desert farm growth");
        var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error) && file.options.biome == (int)MapBiome.Desert, "desert save valid: " + error);

        await Start(MapBiome.Cave);
        Check(BiomeRules.WorkAt(false) == .85f && BiomeRules.WorkAt(true) == 1f, "cave darkness outside rooms");
        var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember);
        var dark = c.WorkRate(CommanderActivity.Research);
        Check(dark > 0, "cave work rate applies");

        // 월드맵 거점마다 바이옴, 중립 자원지 자원량에 반영.
        var world = WorldMapManager.Instance;
        Check(world.Sites.Select(s => s.Biome).Distinct().Count() >= 3, "world map has varied biomes");
        var site = world.Sites.FirstOrDefault(s => s.Kind == ExpeditionSiteKind.ResourceSite);
        if (site != null)
        {
            var food = site.GetComponentsInChildren<ResourceNode>(true).First(n => n.ResourceType == RT.Food);
            Check(Mathf.Approximately(food.AmountRemaining, Mathf.Round(100 * site.Difficulty * BiomeRules.NodeMultiplier(site.Biome, RT.Food))), "site food scaled by site biome");
        }
        return "PASS " + checks + " Phase 6 checks";
    }
}
