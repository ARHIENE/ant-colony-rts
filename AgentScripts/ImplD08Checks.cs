using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class ImplD08Checks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("D08: " + message); checks++; File.WriteAllText("Temp/ImplD08Checks-progress.txt", checks + " " + message); }
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(obj, args);
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .01f, message);
    static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static void Rules()
    {
        var a = AntColony.World.UndergroundSurvey.Generate(261025, 0, Vector3.zero, 30, AntColony.Core.MapBiome.Cave);
        var b = AntColony.World.UndergroundSurvey.Generate(261025, 0, Vector3.zero, 30, AntColony.Core.MapBiome.Cave);
        Check(JsonUtility.ToJson(new AntColony.World.UndergroundSurvey.State { layers = a }) == JsonUtility.ToJson(new AntColony.World.UndergroundSurvey.State { layers = b }), "seed fixed");
        var s = new AntColony.World.UndergroundSurvey.State { present = true, layers = a };
        Check(AntColony.World.UndergroundSurvey.Valid(s, 1), "generated state valid");
        Check(a.Count == 12 && a.Count(l => l.type < 0) == 4, "12 candidates: empty 4 resource 8");
        Check(a.All(l => l.remaining == l.total && !l.discovered && !l.discoveryPending), "no initial discoveries");
        Check(AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.IronOre) == 400
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.CopperOre) == 400
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.Coal) == 400
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.CrudeOil) == 600
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.NaturalGas) == 600
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.TitaniumOre) == 160
            && AntColony.World.UndergroundSurvey.Amount(AntColony.Data.ResourceType.StellarOre) == 80, "layer totals");
        var c = AntColony.Buildings.UndergroundScanner.Cost;
        Check(c.Length == 3 && c[0].type == AntColony.Data.ResourceType.Steel && c[0].amount == 60
            && c[1].type == AntColony.Data.ResourceType.Copper && c[1].amount == 20
            && c[2].type == AntColony.Data.ResourceType.Component && c[2].amount == 6, "fixed scanner cost");
        var definition = AntColony.Core.CampaignResearch.Technologies[(int)AntColony.Core.ScienceTechnology.UndergroundSurvey];
        Check(definition.Technology == AntColony.Core.ScienceTechnology.UndergroundSurvey && definition.Tier == 4
            && definition.Work == 1500 && definition.Food == 200 && definition.Soil == 200 && definition.Special == 30, "electric research");
        Check(AntColony.Buildings.UndergroundScanner.Radius == 35 && AntColony.Buildings.UndergroundScanner.CandidateSeconds == 300
            && AntColony.Buildings.UndergroundScanner.BuildSeconds == 45 && AntColony.Buildings.UndergroundScanner.Watts == 200, "scanner rules");
        var talents = new AntColony.Units.CommanderTalents();
        foreach (var level in new[] { 0, 10, 20 }) { talents.levels[(int)AntColony.Units.CommanderActivity.Research] = level;
            Near(talents.Multiplier(AntColony.Units.CommanderActivity.Research), .6f + .04f * level, "research speed"); }
        Check(AntColony.World.UndergroundSurvey.Valid(JsonUtility.FromJson<AntColony.World.UndergroundSurvey.State>("{}"), 1), "missing present accepts old empty object");
        var bad = Copy(s); bad.layers[0].work = float.NaN; Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "reject NaN work");
        bad = Copy(s); bad.layers[0].remaining = 1; Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "reject empty layer stock");
        bad = Copy(s); bad.layers[0].id = bad.layers[1].id; Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "reject duplicate id");
        bad = Copy(s); bad.layers[0].discoveryPending = true; Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "reject unpublished discovery");
        bad = Copy(s); bad.layers.Clear(); Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "present empty is not legacy");
        bad = Copy(s); bad.layers[4].remaining += 1; Check(!AntColony.World.UndergroundSurvey.Valid(bad, 1), "reject excess stock");
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(20);
        Check(!AntColony.Save.SaveSystem.Busy && AntColony.Core.GameSession.Instance.GameStarted, "session ready");
        Time.timeScale = 0;
    }
    static AntColony.Buildings.UndergroundScanner Scanner(Vector3 position)
    {
        var method = typeof(AntColony.Buildings.BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic);
        var template = (GameObject)method.Invoke(null, new object[] { AntColony.Data.BuildingKind.UndergroundScanner, AntColony.Data.UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, position, Quaternion.identity); go.name = "D08 Scanner"; go.SetActive(true);
        return go.GetComponent<AntColony.Buildings.UndergroundScanner>();
    }
    static async Task Roundtrip(string name)
    {
        Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var error), name + " save: " + error);
        var path = AntColony.Save.SaveSlots.PathFor(false, 0);
        Check(AntColony.Save.SaveSystem.TryLoad(path, out error), "load: " + error); await Ready();
    }
    public static async Task<string> Main()
    {
        checks = 0; Rules(); Check(Application.isPlaying, "Play mode required");
        var root = AntColony.Save.SaveStorage.RootOverride; var settings = AntColony.Core.UserSettings.Current.Clone();
        var scale = Time.timeScale; var random = UnityEngine.Random.state;
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; AntColony.Core.UserSettings.Apply(isolated, false);
            AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplD08-" + Guid.NewGuid().ToString("N"));
            var candidate = AntColony.Core.StartingCandidate.Roll(0); candidate.traits.values.Clear();
            candidate.talents.levels[(int)AntColony.Units.CommanderActivity.Research] = 10;
            candidate.talents.experience[(int)AntColony.Units.CommanderActivity.Research] = 0;
            AntColony.Core.CrashLandingStart.Begin(new AntColony.Core.NewGameOptions { startingCount = 1, seed = 261025,
                mapSize = AntColony.Core.MapSize.Small, biome = AntColony.Core.MapBiome.Cave, startingTeam = new[] { candidate } });
            await Ready();
            var survey = AntColony.World.UndergroundSurvey.Instance;
            Check(survey.Layers.Count == (AntColony.World.WorldMapManager.Instance.Sites.Count + 1) * 12, "every local map candidates");
            var initial = survey.Capture(); var commander = AntColony.Core.CommanderRoster.Instance.Commanders.Single();
            commander.CommandStop();
            var powerScanner = Scanner(commander.Position + new Vector3(12000, 0, 12000));
            var batteryObject = GameObject.CreatePrimitive(PrimitiveType.Cube); batteryObject.SetActive(false);
            batteryObject.transform.position = powerScanner.Position;
            var battery = batteryObject.AddComponent<AntColony.Buildings.PowerNode>();
            var batteryData = ScriptableObject.CreateInstance<AntColony.Data.BuildingData>(); batteryData.kind = AntColony.Data.BuildingKind.Battery;
            typeof(AntColony.Buildings.BuildingBase).GetMethod("ConfigureRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battery, new object[] { batteryData });
            typeof(AntColony.Buildings.PowerNode).GetProperty("Charge").SetValue(battery, 200f);
            batteryObject.SetActive(true); AntColony.Buildings.PowerGrid.Tick(.5f);
            Check(powerScanner.Powered, "real grid powers 200W scanner"); Near(battery.Charge, 100, "200W consumes 100J per half second");
            AntColony.Buildings.PowerGrid.Tick(1); Check(!powerScanner.Powered, "insufficient stored energy stops scanner");
            Near(battery.Charge, 100, "outage preserves battery under existing grid contract");
            powerScanner.gameObject.SetActive(false); batteryObject.SetActive(false);
            UnityEngine.Object.Destroy(powerScanner.gameObject); UnityEngine.Object.Destroy(batteryObject); UnityEngine.Object.Destroy(batteryData);
            var scanner = Scanner(commander.Position + Vector3.right * 2);
            var tie = Copy(initial);
            foreach (var l in tie.layers.Where(l => l.map == 0)) l.position = scanner.Position + Vector3.forward * 100;
            tie.layers.Single(l => l.id == 0).position = scanner.Position + Vector3.right * 5;
            tie.layers.Single(l => l.id == 1).position = scanner.Position - Vector3.right * 5;
            survey.Restore(tie); Check((int)Call(survey, "Reserve", scanner) == 0, "equal distance uses stable candidate id");
            survey.Restore(initial);
            Check(!scanner.TryAssign(commander), "research locks assignment");
            var research = AntColony.Core.CampaignResearch.Instance.CaptureState();
            research.completed.Add((int)AntColony.Core.ScienceTechnology.UndergroundSurvey);
            AntColony.Core.CampaignResearch.Instance.RestoreState(research);
            Check(scanner.TryAssign(commander), "assign one researcher");
            var second = Scanner(scanner.Position);
            Check(!second.TryAssign(commander), "same researcher cannot occupy two scanners");
            Time.timeScale = 1;
            scanner.Powered = false; scanner.Tick(50);
            Near(scanner.Progress, 0, "outage no progress");
            Check(scanner.CandidateId >= 0, "candidate reserved");
            int id = scanner.CandidateId;
            Check((int)Call(survey, "Reserve", second) != id, "duplicate reservation excluded");
            scanner.Powered = true; scanner.Tick(10); Near(scanner.Progress, 10, "research10 speed1");
            var originalPosition = scanner.Position; scanner.transform.position += Vector3.right * 100;
            scanner.Tick(1); Check(scanner.CandidateId < 0, "move releases old location reservation");
            Near(survey.Find(id).work, 10, "move preserves old location work");
            scanner.transform.position = originalPosition; scanner.Tick(1); Check(scanner.CandidateId == id, "return resumes same candidate");
            Time.timeScale = 0; scanner.Tick(10); Near(scanner.Progress, 10, "pause no progress");
            Time.timeScale = 1; scanner.Powered = false; scanner.Tick(20); Near(scanner.Progress, 10, "outage preserves work");
            Time.timeScale = 0;
            UnityEngine.Object.Destroy(second.gameObject); await Task.Delay(20);
            await Roundtrip("d08-progress");
            survey = AntColony.World.UndergroundSurvey.Instance;
            scanner = UnityEngine.Object.FindObjectsByType<AntColony.Buildings.UndergroundScanner>(FindObjectsSortMode.None).Single();
            Check(scanner.Target != null && scanner.CandidateId == id, "researcher and reservation restored"); Near(scanner.Progress, 10, "work roundtrip");
            scanner.Release(); Near(survey.Find(id).work, 10, "replacement preserves candidate work");
            var layer = survey.Layers.First(l => l.map == 0 && l.type >= 0 && l.id != id);
            var rm = AntColony.Core.ResourceManager.Instance;
            var amount = rm.GetAmount((AntColony.Data.ResourceType)layer.type);
            Call(survey, "Work", layer.id, 299f); Check(!layer.discovered, "only complete at 300");
            Call(survey, "Work", layer.id, 1f); Check(layer.discovered && layer.discoveryPending, "single candidate discovery");
            Check(rm.GetAmount((AntColony.Data.ResourceType)layer.type) == amount, "discovery gives no resource");
            var node = UnityEngine.Object.FindObjectsByType<AntColony.World.UndergroundLayer>(FindObjectsSortMode.None).Single(l => l.Id == layer.id).GetComponent<AntColony.World.ResourceNode>();
            Check(!node.IsLooseCargo && !node.CanGather && !node.CanGatherWith(commander), "natural layer cannot be hand gathered");
            Check((float)Call(survey, "Extract", layer.id, layer.position, (AntColony.Data.ResourceType)layer.type, 10f) == 0, "sample research gate");
            research = AntColony.Core.CampaignResearch.Instance.CaptureState(); if (!research.samples.Contains(layer.type)) research.samples.Add(layer.type);
            if (!research.resourceResearch.Contains(layer.type)) research.resourceResearch.Add(layer.type);
            AntColony.Core.CampaignResearch.Instance.RestoreState(research);
            Near((float)Call(survey, "Extract", layer.id, layer.position + Vector3.right * 2, (AntColony.Data.ResourceType)layer.type, 10f), 0, "3x3 boundary");
            Near((float)Call(survey, "Extract", layer.id, layer.position, (AntColony.Data.ResourceType)layer.type, 10f), 10, "first machine shared stock");
            Near((float)Call(survey, "Extract", layer.id, layer.position + Vector3.right, (AntColony.Data.ResourceType)layer.type, 10f), 10, "second machine same stock");
            Near(layer.remaining, layer.total - 20, "shared whole-layer amount");
            Time.timeScale = 1; int notices = AntColony.UI.ToastManager.Count;
            Call(survey, "Work", survey.Layers.First(l => l.map == 0 && l.type < 0).id, 300f);
            survey.TickNotices(.5f); Check(AntColony.UI.ToastManager.Count == notices, "wait one second batch");
            var pendingLayerId = layer.id; Time.timeScale = 0; await Roundtrip("d08-pending-notice");
            survey = AntColony.World.UndergroundSurvey.Instance; layer = survey.Find(pendingLayerId);
            Check(layer.discoveryPending, "pending discovery restored");
            Time.timeScale = 1; notices = AntColony.UI.ToastManager.Count;
            survey.TickNotices(.5f); Check(AntColony.UI.ToastManager.Count == notices + 1, "one grouped discovery toast");
            Check(!survey.Layers.Any(l => l.discoveryPending), "pending flushed once");
            Time.timeScale = 0; int layerId = layer.id; int total = layer.total;
            await Roundtrip("d08-layer"); survey = AntColony.World.UndergroundSurvey.Instance; layer = survey.Find(layerId);
            Near(layer.remaining, total - 20, "remaining roundtrip"); Check(layer.discovered, "discovery permanent");
            Call(survey, "Extract", layerId, layer.position, (AntColony.Data.ResourceType)layer.type, 10000f);
            Check(layer.remaining == 0 && layer.exhaustionNotified, "one exhaustion flag");
            notices = AntColony.UI.ToastManager.Count;
            Near((float)Call(survey, "Extract", layerId, layer.position, (AntColony.Data.ResourceType)layer.type, 1f), 0, "exhausted no refill");
            Check(notices == AntColony.UI.ToastManager.Count, "exhaustion not repeated");
            await Roundtrip("d08-exhausted"); survey = AntColony.World.UndergroundSurvey.Instance;
            Check(survey.Find(layerId).remaining == 0 && survey.Find(layerId).exhaustionNotified, "exhaustion roundtrip");
            var file = AntColony.Save.SaveSnapshot.Capture(); Check(file.version == 19, "save version retained");
            var bad = Copy(file); bad.underground.layers[0].work = float.NaN;
            Check(!AntColony.Save.SaveValidator.Validate(bad, out _), "loader rejects invalid survey");
            var legacy = Copy(file); legacy.underground = JsonUtility.FromJson<AntColony.World.UndergroundSurvey.State>("{}");
            legacy.buildings.RemoveAll(b => b.kind == "UndergroundScanner");
            var path = Path.Combine(AntColony.Save.SaveStorage.Root, "d08-legacy.json");
            Check(AntColony.Save.SaveValidator.Validate(legacy, out var error), "legacy missing present: " + error);
            var missing = Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(legacy)); missing.Remove("underground");
            Check(AntColony.Save.SaveValidator.TryParse(missing.ToString(), out legacy, out error), "legacy field absent: " + error);
            AntColony.Save.SaveStorage.WriteAtomic(path, JsonUtility.ToJson(legacy));
            Check(AntColony.Save.SaveSystem.TryLoad(path, out error), "legacy load: " + error); await Ready();
            Check(JsonUtility.ToJson(initial) == JsonUtility.ToJson(AntColony.World.UndergroundSurvey.Instance.Capture()), "old save deterministic initialization");
            var result = "IMPL-D08 " + checks + " checks passed"; File.WriteAllText("Temp/ImplD08Checks-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/ImplD08Checks-result.txt", e.ToString()); throw; }
        finally
        {
            Time.timeScale = scale; UnityEngine.Random.state = random;
            AntColony.Save.SaveStorage.RootOverride = root; AntColony.Core.UserSettings.Apply(settings, false);
        }
    }
}
