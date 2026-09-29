using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Res = AntColony.Data.ResourceType;

// 7단계 바이옴 (2026-09-28): 자원 노드 ±60%, 숲 곰팡이·산불 2배, 정원 밭 +60%, 도시 밭 불가, 물가 낚시 +60%, 저장.
public static class BiomeChecks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy && GameSession.Instance.GameStarted, "scene ready"); Time.timeScale = 0;
    }
    static async Task NewGame(MapBiome biome)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261002, mapSize = MapSize.Small, biome = biome }); await Ready(); await Task.Delay(300);
        GameMenuController.Instance.Pause();
    }
    // HomeMapBuilder.ApplyBiomeNodes와 같은 대상: 본거지의 자연 자원 노드.
    static float Sum(Res type) => Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)
        .Where(n => n.ResourceType == type && n.RegrowSeconds == 0 && !n.RequiresFishing && !n.IsRaidLoot && !n.IsLooseCargo
            && n.GetComponentInParent<ExpeditionSite>() == null
            && HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(n.transform.position.x, 0, n.transform.position.z)))
        .Sum(n => n.AmountRemaining);
    static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) <= Mathf.Max(.5f, Mathf.Abs(b) * .01f), $"{label}: {a} / {b}");

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Biome-" + Guid.NewGuid().ToString("N"));
        try
        {
            // 기준(None): 보정 없음. 옛 저장·검사와 같다.
            await NewGame(MapBiome.None);
            Check(BiomeRules.Current == MapBiome.None && BiomeRules.NodeMultiplier(Res.Food) == 1 && BiomeRules.FarmAllowed, "none is neutral");
            float food = Sum(Res.Food), soil = Sum(Res.Soil), special = Sum(Res.Special);
            Check(food > 0 && soil > 0, $"baseline nodes F{food} S{soil}");

            // 숲 바닥: Food +60%, Special -60%, 곰팡이·산불 2배.
            await NewGame(MapBiome.Forest);
            Check(GameSession.Instance.Options.biome == MapBiome.Forest, "forest selected");
            Near(Sum(Res.Food), food * 1.6f, "forest food +60%");
            Near(Sum(Res.Soil), soil, "forest soil unchanged");
            Near(Sum(Res.Special), special * .4f, "forest special -60%");
            Check(BiomeRules.MoldSpread == 2 && BiomeRules.WildfireDamage == 2 && BiomeRules.FarmGrowth == 1, "forest mold/wildfire x2");
            Check(HudClock.DayLabel.Contains("숲 바닥"), "hud shows biome");

            // 저장 왕복: 바이옴 유지, 노드 양이 두 번 곱해지지 않는다.
            var forestFood = Sum(Res.Food);
            Check(SaveSystem.TrySave(false, 1, out var saveError), "save: " + saveError);
            var path = SaveSlots.PathFor(false, 1);
            Check(SaveSystem.TryLoad(path, out var loadError), "load: " + loadError); await Ready(); GameMenuController.Instance.Pause();
            Check(GameSession.Instance.Options.biome == MapBiome.Forest, "biome survives load");
            Near(Sum(Res.Food), forestFood, "load keeps node amounts");
            var file = SaveSnapshot.Capture(); file.options.biome = 99;
            var bad = Path.Combine(SaveStorage.SavesFolder, "bad-biome.json"); SaveStorage.WriteAtomic(bad, JsonUtility.ToJson(file));
            Check(!SaveSystem.TryLoad(bad, out _), "unknown biome rejected");

            // 규칙만 바꿔 보는 나머지 3종(맵 재생성 없이).
            var options = GameSession.Instance.Options;
            options.biome = MapBiome.Garden;
            Check(BiomeRules.FarmGrowth == 1.6f && BiomeRules.FarmAllowed, "garden farm growth +60%");
            options.biome = MapBiome.City;
            Check(!BiomeRules.FarmAllowed && !ScienceEffects.BuildingUnlocked(AntColony.Data.BuildingKind.Farm) && BiomeRules.NodeMultiplier(Res.Special) == 1.6f, "city: no farms, special +60%");
            options.biome = MapBiome.Waterside;
            Check(Mathf.Approximately(BiomeRules.FishingYield, 1.6f) && Mathf.Approximately(BiomeRules.NodeMultiplier(Res.Soil), .4f), "waterside fishing +60%, soil -60%");
            var pond = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).FirstOrDefault(n => n.RequiresFishing);
            if (pond != null)
            {
                typeof(ResourceNode).GetProperty("FishMonth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(pond, -1);
                pond.RefreshFishingMonth();
                Near(pond.AmountRemaining, GameBalance.FishingMonthlyFood * 1.6f, "waterside monthly fish +60%");
            }
            options.biome = MapBiome.Forest;
            Check(BiomeRules.Random() != MapBiome.None, "random never picks none");
            return $"BiomeChecks passed: {checks}";
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 1; }
    }
}
