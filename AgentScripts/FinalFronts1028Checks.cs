using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class FinalFronts1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FinalFronts-" + Guid.NewGuid().ToString("N"));
        var objects = new List<GameObject>(); var oldTemplates = new Dictionary<ColonyInvasion, object>();
        var templateField = typeof(ColonyInvasion).GetField("raiderTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            Check(FinalBattleEconomy.DefenseSeconds == 2250, "15 days unchanged");
            Check(FinalAssaultPlan.Phase(749) == 0 && FinalAssaultPlan.Phase(750) == 1 && FinalAssaultPlan.Phase(1500) == 2 && FinalAssaultPlan.Phase(2250) == 2, "exact phase boundaries");
            Check(FinalAssaultPlan.Fronts(0) == 1 && FinalAssaultPlan.Fronts(750) == 2 && FinalAssaultPlan.Fronts(1500) == 3, "front count");
            foreach (var source in UnityEngine.Object.FindObjectsByType<ColonyInvasion>(FindObjectsInactive.Include))
            { oldTemplates[source] = templateField.GetValue(source); templateField.SetValue(source, null); }
            var sourceGo = new GameObject("Final front source"); objects.Add(sourceGo); sourceGo.SetActive(false); var sourceTest = sourceGo.AddComponent<ColonyInvasion>();
            var templateGo = new GameObject("Final front template"); objects.Add(templateGo); templateGo.SetActive(false); var template = templateGo.AddComponent<WildMonster>();
            templateField.SetValue(sourceTest, template);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(ColonyInvasion).GetField("spawnPoint", flags).SetValue(sourceTest, CommanderRoster.Instance.Commanders[0].transform);
            typeof(ColonyInvasion).GetField("firstWaveCount", flags).SetValue(sourceTest, 6);
            var local = SceneObjects.Any<LocalIncursions>(); Check(local != null, "saved visitor registry");
            var spawn = typeof(ColonyInvasion).GetMethod("SpawnFinalWave", BindingFlags.Static | BindingFlags.NonPublic);
            var home = RocketCampaign.Instance.Home;
            foreach (var phaseTime in new[] { 0f, 750f, 1500f })
            {
                GameSession.Instance.MarkStarted(0, phaseTime);
                var before = local.Visitors.Where(v => v != null).ToHashSet();
                var snapshot = new RocketSnapshot { taken = true, at = 0 }; var args = new object[] { snapshot, 0, 0f };
                Check((bool)spawn.Invoke(null, args), "real wave spawned phase " + phaseTime);
                var spawned = local.Visitors.Where(v => v != null && !before.Contains(v)).ToArray();
                Check(spawned.Length == 6, "same total six phase " + phaseTime);
                if (phaseTime > 0)
                {
                    var distant = spawned.Where(v => (v.Position - home).sqrMagnitude > 625).ToArray();
                    Check(distant.Length == 6, "validated outer approaches used " + phaseTime);
                    var directions = distant.Select(v => (v.Position - home).normalized).ToArray();
                    Check(directions.Any(a => directions.Any(b => Vector3.Dot(a, b) < -.3f)), "actual separated fronts " + phaseTime);
                }
                foreach (var v in spawned) UnityEngine.Object.DestroyImmediate(v.gameObject);
                var copy = JsonUtility.FromJson<RocketSnapshot>(JsonUtility.ToJson(snapshot));
                Check(FinalAssaultPlan.Phase(GameCalendar.GameSeconds - copy.at) == FinalAssaultPlan.Phase(phaseTime), "restore stable phase " + phaseTime);
            }
            var remote = new GameObject("Force invalid outer approach"); objects.Add(remote); remote.transform.position = home + Vector3.right * 10000;
            remote.AddComponent<AntColony.Buildings.BuildingBase>();
            var previous = local.Visitors.Where(v => v != null).ToHashSet();
            var fallbackArgs = new object[] { new RocketSnapshot { taken = true }, 0, 0f };
            Check((bool)spawn.Invoke(null, fallbackArgs), "invalid outer routes fall back without losing wave");
            var fallback = local.Visitors.Where(v => v != null && !previous.Contains(v)).ToArray();
            Check(fallback.Length == 6 && fallback.All(v => (v.Position - CommanderRoster.Instance.Commanders[0].Position).sqrMagnitude < 100), "fallback uses existing entry and same total");
            typeof(ColonyInvasion).GetField("maxActiveRaiders", flags).SetValue(sourceTest, 6);
            Check(!(bool)spawn.Invoke(null, fallbackArgs) && local.Visitors.Count(v => v != null && !v.IsDead) == 6, "active cap prevents extra wave");
            foreach (var v in fallback) UnityEngine.Object.DestroyImmediate(v.gameObject);
            return "PASS " + count + " final front checks";
        }
        finally
        {
            foreach (var pair in oldTemplates) if (pair.Key != null) templateField.SetValue(pair.Key, pair.Value);
            foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false);
        }
    }
}
