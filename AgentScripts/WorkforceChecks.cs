using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class WorkforceChecks
{
    static int checks;
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .02f, message + $" ({actual}/{expected})");
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static void Warp(CommanderAnt c, Vector3 position)
    {
        c.CommandStop(); Check(NavMesh.SamplePosition(position, out var hit, 12, NavMesh.AllAreas), "walkable");
        Check(c.Agent.Warp(hit.position), "warp");
    }
    static void At(float seconds) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, seconds);
    static ResourceNode Loot(Vector3 position, float amount)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false); go.name = "Workforce test cargo";
        go.transform.position = position; var n = go.AddComponent<ResourceNode>(); n.ConfigureLoot(AntColony.Data.ResourceType.Food, amount); go.SetActive(true); return n;
    }
    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Workforce-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 260929, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0; At(20);
            var all = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember).ToArray();
            foreach (var c0 in all) { c0.CommandStop(); c0.WorkState.jobs = CommanderJobs.None; c0.Traits.values.Clear(); c0.Traits.passions.Clear(); }
            var c = all[0]; var other = all[1]; var pool = AntPool.Instance; pool.Breed(150);
            var resources = ResourceManager.Instance;
            Call(resources, "RestoreState", 1000, 1000, 1000, 2000, 2000, 2000);
            var home = c.Position;
            Check(CommanderTalents.Count == 13 && (int)CommanderJobs.All == 8191, "13 skills / 13 job bits");
            Array.Clear(c.Talents.levels, 0, CommanderTalents.Count);
            var node = Loot(home + Vector3.right * 2, 200);
            var workforce = Workforce.For(node); workforce.Request(30);
            int total = pool.Total, free = pool.Free;
            Check(workforce.Allocated == 0, "unattended target reserves no ants");
            c.CommandGather(node); workforce.Refresh();
            Check(c.CurrentResourceNode == node && workforce.Allocated == 5 && pool.Working == 5 && pool.Free == free - 5 && pool.Total == total, "skill zero allocation and conservation");
            Near(c.LoadCapacity, 20, "carry strength zero + 5 workers");
            c.Talents.levels[(int)CommanderActivity.Gathering] = 10; workforce.Refresh();
            Check(workforce.Allocated == 25 && workforce.Limit == 25, "skill ten cap 25");
            c.Talents.levels[(int)CommanderActivity.Strength] = 20; workforce.Request(10);
            Near(c.LoadCapacity, 60, "strength 20, workers 10 carry 60");
            c.Traits.values.Add(CommanderTrait.Muscular); Near(c.LoadCapacity, 80, "carry trait only personal portion"); c.Traits.values.Clear();
            workforce.Request(200); Check(workforce.Requested == 45 && workforce.Allocated == 25, "request clamped and skill capped");
            c.CommandStop(); Check(pool.Working == 0 && pool.Free == free && workforce.Allocated == 0, "stop returns workers once");
            c.CommandStop(); Check(pool.Free == free, "repeat stop no duplication");
            c.CommandGather(node); node.gameObject.SetActive(false); Check(pool.Working == 0, "target disabled returns workers"); c.CommandStop(); node.gameObject.SetActive(true);

            c.WorkState.jobs = CommanderJobs.Hauling; c.TickDuty(2);
            Check(c.CurrentResourceNode == node && CommanderOverhead.Activity(c) == "운반", "hauling selects loose cargo"); c.CommandStop(); c.WorkState.jobs = CommanderJobs.None;
            node.GatheringForbidden = true; c.WorkState.jobs = CommanderJobs.Hauling; c.TickDuty(2);
            Check(c.CurrentResourceNode != node, "forbidden cargo not hauled"); c.CommandStop(); c.WorkState.jobs = CommanderJobs.None; node.GatheringForbidden = false;
            var xp = c.Talents.Xp(CommanderActivity.Strength);
            Call(c, "OnDelivered", 10f); Near(c.Talents.Xp(CommanderActivity.Strength), xp, "max strength stays capped");
            c.Talents.levels[(int)CommanderActivity.Strength] = 0; Call(c, "OnDelivered", 10f); Near(c.Talents.Xp(CommanderActivity.Strength), 5, "strength XP per delivered resource .5");

            var storage = Build<Storage>(BuildingKind.Storage, home + Vector3.forward * 6);
            Warp(c, storage.Position + Vector3.right * 3);
            Call(storage, "RestoreHealth", storage.MaxHealth * .5f);
            int food = resources.GetAmount(AntColony.Data.ResourceType.Food), soil = resources.GetAmount(AntColony.Data.ResourceType.Soil);
            Check(c.StartService(storage, CommanderJobs.Repair), "repair starts");
            c.TickDuty(1); var repair = BuildingRepair.For(storage);
            Check(storage.CurrentHealth > storage.MaxHealth * .5f && repair.Credit > 0, "repair advances");
            Check(resources.GetAmount(AntColony.Data.ResourceType.Food) == food - Mathf.CeilToInt(storage.Data.foodCost * .25f)
                && resources.GetAmount(AntColony.Data.ResourceType.Soil) == soil - Mathf.CeilToInt(storage.Data.soilCost * .25f), "half damage costs quarter build price");
            int paidSoil = resources.GetAmount(AntColony.Data.ResourceType.Soil);
            c.CommandStop(); Check(c.StartService(storage, CommanderJobs.Repair), "resume repair"); c.TickDuty(1);
            Check(resources.GetAmount(AntColony.Data.ResourceType.Soil) == paidSoil, "resume does not double pay");
            for (int i = 0; i < 100; i++) c.TickDuty(1);
            Near(storage.CurrentHealth, storage.MaxHealth, "repair finishes");
            c.CommandStop();
            c.Traits.values.Add(CommanderTrait.CannotBuild);
            Check(!c.SetJobEnabled(CommanderJobs.Repair, true) && !c.CanDoJob(CommanderJobs.Building), "trait blocks jobs"); c.Traits.values.Clear();

            CampaignResearch.Instance.RestoreState(new CampaignResearch.State { completed = new System.Collections.Generic.List<int> { (int)ScienceTechnology.Herbs, (int)ScienceTechnology.Infirmary } });
            var infirmary = Build<Infirmary>(BuildingKind.Infirmary, home + Vector3.left * 7);
            Warp(c, infirmary.Position + Vector3.forward * 3); Warp(other, infirmary.Position + Vector3.right * 3);
            other.PersonalState.injuries.Clear(); other.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Serious, remaining = 240 });
            Check(infirmary.TryAdmit(other), "patient admitted");
            Near(infirmary.TreatmentRate, .5f, "no nurse half rate");
            other.TickPersonal(10); Near(other.PersonalState.injuries[0].remaining, 233.3333f, "unnursed treatment half speed with Herbs bonus");
            c.Talents.levels[(int)CommanderActivity.Medicine] = 20; Check(c.StartService(infirmary, CommanderJobs.Nursing), "nurse starts");
            c.TickDuty(1); Near(infirmary.TreatmentRate, 1.4f, "medical skill speed");
            Near(Infirmary.PermanentRisk(0), .1f, "medical 0 risk"); Near(Infirmary.PermanentRisk(10), .05f, "medical 10 risk"); Near(Infirmary.PermanentRisk(20), 0, "medical 20 risk");
            other.PersonalState.injuries[0].prognosisPending = true;
            other.TickPersonal(200); Check(other.PersonalState.injuries.Count == 0, "expert nursing heals without permanent damage");
            c.TickDuty(1); Check(c.ServiceTarget == null, "nurse leaves empty infirmary");

            var kitchen = Build<Kitchen>(BuildingKind.Kitchen, home + Vector3.back * 6);
            Warp(c, kitchen.Position + Vector3.right * 3);
            c.Traits.values.Add(CommanderTrait.Chef);
            Check(c.StartService(kitchen, CommanderJobs.Cooking), "cook starts");
            for (int i = 0; i < 20; i++) c.TickDuty(1);
            Check(kitchen.Meals.meals.Count > 0 && kitchen.Meals.meals.All(m => m.quality == 3), "chef cooks high quality meals");
            Check(c.Talents.Xp(CommanderActivity.Cooking) > 0, "cooking XP");
            var meals = kitchen.Meals.meals.Count; float progress = kitchen.Meals.progress;
            At(620); c.TickDuty(1); Check(c.IsAsleep && c.ServiceTarget == null && pool.Working == 0, "sleep stops service and returns workers");
            Check(kitchen.Meals.meals.Count == meals && Mathf.Approximately(kitchen.Meals.progress, progress), "sleep preserves cooking progress");
            At(920); c.TickDuty(1); c.PersonalState.sleep.poorly = false; c.Traits.values.Clear();

            var decoration = Build<Decoration>(BuildingKind.MarbleMosaic, home + Vector3.right * 6); decoration.gameObject.SetActive(false);
            var siteGo = new GameObject("Art test blueprint"); siteGo.transform.position = home;
            var site = siteGo.AddComponent<BuildingConstructionSite>(); site.Initialize(decoration.gameObject, 6);
            Warp(c, home); c.Talents.levels[(int)CommanderActivity.Art] = 20;
            c.CommandBuild(site); Check(c.ConstructionTarget == site && site.IsArt && c.CurrentActivity == CommanderActivity.Art, "art builds decoration");
            At(1520); c.TickDuty(1); Check(site != null && !site.HasBuilder && !decoration.gameObject.activeSelf, "sleep preserves paid blueprint");
            At(1820); c.TickDuty(1); c.WorkState.jobs = CommanderJobs.Art; c.TickDuty(2);
            Check(c.ConstructionTarget == site, "art resumes blueprint");
            site.Complete(c); c.CommandStop(); await Task.Delay(100);
            Check(decoration.gameObject.activeSelf && decoration.Quality >= 1 && decoration.Quality <= 3, "art completion quality");
            Check(Decoration.MoodAt(c) > 0 && Decoration.MoodAt(c) <= 10, "decoration environment mood"); c.WorkState.jobs = CommanderJobs.None;

            var animal = Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None).First(m => m.Huntable);
            Check(!c.StartHunt(animal), "undesignated wildlife rejected");
            animal.HuntDesignated = true; animal.Temperament = WildlifeTemperament.Defensive;
            Warp(c, animal.Position + Vector3.right * .3f); Check(c.StartHunt(animal), "designated hunt starts without troops");
            c.Talents.levels[(int)CommanderActivity.Melee] = 20;
            for (int i = 0; i < 20 && !animal.IsDead; i++) c.TickDuty(2);
            Check(animal.IsDead && ResourceNode.Available.Any(n => n.name == "사냥 사체" && n.IsLooseCargo), "hunted animal leaves food cargo");
            c.CommandStop(); Warp(c, home);

            GameMenuController.Instance.WorkSchedule();
            Check(Object.FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Job ")) == all.Length * 13, "13 toggles per commander");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            WorkTargetPanel.Select(node); await Task.Delay(80);
            Check(GameObject.Find("WorkforceSlider") != null, "target workforce slider visible");
            var selection = Object.FindFirstObjectByType<AntColony.Units.SelectionManager>(); selection.SelectOnly(c.GetComponent<AntColony.Units.SelectableObject>()); await Task.Delay(80);
            Check(WorkTargetPanel.Target == null && GameObject.Find("Skill 예술") != null, "13 skill card and target clear");
            Check(new[] { "간호", "수리", "운반", "사냥", "요리", "예술" }.All(n => ActivityIcons.Get(n) != null), "new job icons");

            workforce.Request(10); c.CommandGather(node); workforce.Refresh();
            foreach (var x in all.Where(x => x != c)) x.CommandStop();
            var file = SaveSnapshot.Capture();
            Check(file.colony.antsFree == pool.Free + pool.Working && file.colony.antsAssigned == pool.Assigned, "save folds workforce into free pool");
            Check(SaveValidator.TryParse(JsonUtility.ToJson(file), out var parsed, out var error), "save validates " + error);
            Check(parsed.nodes.Any(n => n.workforce == 10 && n.looseCargo), "save keeps target workforce and cargo kind");
            Check(parsed.buildings.Any(b => b.kind == "Kitchen" && b.kitchen.meals.Count > 0), "save preserves cooked meals");
            var bad = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(file)); bad.nodes[0].workforce = 1000;
            Check(!SaveValidator.Validate(bad, out _), "invalid manpower rejected");
            bad = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(file)); bad.commanders[0].talents.levels = new int[9];
            Check(!SaveValidator.Validate(bad, out _), "truncated current skills rejected");
            var legacy = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(file)); legacy.version = 9;
            foreach (var x in legacy.commanders) { Array.Resize(ref x.talents.levels, 9); Array.Resize(ref x.talents.experience, 9); x.personalState.work.jobs = CommanderJobs.Legacy; }
            Check(SaveValidator.Validate(legacy, out error) && legacy.commanders.All(x => x.talents.levels.Length == 13 && x.personalState.work.jobs == CommanderJobs.All), "v9 migration " + error);
            int beforeSave = pool.Total;
            Check(SaveSystem.TrySave(false, 0, out error), "save roundtrip " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load roundtrip " + error); await Ready(); await Task.Delay(200);
            Check(AntPool.Instance.Total == beforeSave, "load conserves all ants");
            Check(Object.FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Any(k => k.Meals.meals.Count > 0), "load cooked meals");
            Check(Object.FindObjectsByType<Workforce>(FindObjectsSortMode.None).Any(w => w.Requested == 10), "load workforce requests");
            return checks + " passed";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
