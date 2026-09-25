using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

// Official unity command run_script, fresh Play mode. Saves are isolated from player slots.
public static class StorageResearchChecks
{
    static int checks;
    static readonly ColonyResourceType[] types = { ColonyResourceType.Food, ColonyResourceType.Soil, ColonyResourceType.Special };
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static int[] Capacity() => types.Select(t => ResourceManager.Instance.GetCapacity(t)).ToArray();
    static int[] Bonus(Storage s) => new[] { s.Data.foodCapacityBonus, s.Data.soilCapacityBonus, s.Data.specialCapacityBonus };
    static void Capacities(int[] expected, string label) => Check(Capacity().SequenceEqual(expected), label);
    static async Task Reload()
    {
        Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "StorageResearch-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone(); var isolated = settings.Clone(); isolated.autoSaveEnabled = false;
        UserSettings.Apply(isolated, false);
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250925 }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (var type in types) { rm.AddCapacity(type, 10000); rm.Add(type, 5000); }
            var c = CommanderRoster.Instance.Commanders[0]; c.CommandStop();
            var labTemplate = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
            var lab = Object.Instantiate(labTemplate, c.Position + Vector3.forward * 4, Quaternion.identity);
            lab.name = "ScienceLab"; lab.gameObject.SetActive(true);
            Check(lab.TryAssign(c) && lab.TryUpgrade(), "tier two researcher");
            var research = CampaignResearch.Instance;
            Check(research.TryStart(ScienceTechnology.FungalFarming), "start prerequisite"); research.Tick(10000);
            var before = Capacity();
            var existing = Object.FindObjectsByType<Storage>().Where(s => s.isActiveAndEnabled).ToArray();
            var increases = Enumerable.Range(0, 3).Select(i => existing.Sum(s => Bonus(s)[i] / 2)).ToArray();
            Check(existing.Length > 0 && increases.All(n => n > 0), "existing warehouse bonuses");
            Check(research.TryStart(ScienceTechnology.Fermentation), "start storage research");
            Capacities(before, "no bonus before completion"); research.Tick(10000);
            var expected = before.Select((n, i) => n + increases[i]).ToArray();
            Capacities(expected, "completion increases each warehouse by fifty percent, not colony base");
            Check(!research.TryStart(ScienceTechnology.Fermentation), "cannot repeat research");
            research.RestoreState(research.CaptureState()); Capacities(expected, "refresh does not duplicate bonus");
            var template = Object.FindObjectsByType<Storage>(FindObjectsInactive.Include).Single(s => s.name == "StorageTemplate");
            var built = Object.Instantiate(template, c.Position + Vector3.right * 6, Quaternion.identity); built.name = "Storage";
            Capacities(expected, "inactive template grants nothing");
            built.gameObject.SetActive(true);
            var contribution = Bonus(built).Select(n => n + n / 2).ToArray();
            expected = expected.Select((n, i) => n + contribution[i]).ToArray();
            Capacities(expected, "new warehouse includes research");
            built.enabled = false; Capacities(expected.Select((n, i) => n - contribution[i]).ToArray(), "disable removes entire contribution");
            built.enabled = true; Capacities(expected, "reenable adds once");
            foreach (var commander in CommanderRoster.Instance.Commanders) commander.CommandStop();
            var amounts = types.Select(rm.GetAmount).ToArray();
            await Reload(); Capacities(expected, "reload preserves effective capacity");
            Check(types.Select(t => ResourceManager.Instance.GetAmount(t)).SequenceEqual(amounts), "reload preserves resources");
            await Reload(); Capacities(expected, "second reload cannot accumulate bonus");
            var legacy = SaveSnapshot.Capture();
            var all = Object.FindObjectsByType<Storage>();
            increases = Enumerable.Range(0, 3).Select(i => all.Sum(s => Bonus(s)[i] / 2)).ToArray();
            legacy.colony.foodCapacity -= increases[0]; legacy.colony.soilCapacity -= increases[1]; legacy.colony.specialCapacity -= increases[2];
            var json = JsonUtility.ToJson(legacy).Replace("\"storageResearchApplied\":true,", "");
            Check(SaveValidator.TryParse(json, out var parsed, out var error) && !parsed.colony.storageResearchApplied, "legacy marker absent: " + error);
            var path = Path.Combine(SaveStorage.Root, "legacy.json"); SaveStorage.WriteAtomic(path, json);
            Check(SaveSystem.TryLoad(path, out error), "legacy load: " + error); await Ready();
            Capacities(expected, "legacy completed research gains missing bonus once");
            await Reload(); Capacities(expected, "migrated capacity stable on resave");
            built = Object.FindObjectsByType<Storage>().First(); contribution = Bonus(built).Select(n => n + n / 2).ToArray();
            Object.Destroy(built.gameObject); await Task.Delay(50);
            Capacities(expected.Select((n, i) => n - contribution[i]).ToArray(), "destroy after reload removes correct bonus");
            return "PASS " + checks + " storage research checks; completion, construction, disable/destroy, repeated reload, legacy migration";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; UserSettings.Apply(settings, false); }
    }
}
