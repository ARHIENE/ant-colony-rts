using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class Impl1025CodexChecks
{
    static int checks;
    static readonly List<Object> made = new List<Object>();
    static void Check(bool ok, string label) { if (!ok) throw new Exception("1025 Codex FAIL: " + label); checks++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Put<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var data = ScriptableObject.CreateInstance<BuildingData>(); made.Add(data);
        data.kind = kind; data.maxHealth = 100; data.foodCapacityBonus = data.soilCapacityBonus = 100;
        var go = new GameObject("Impl1025 " + kind); go.SetActive(false); made.Add(go); go.transform.position = position;
        var building = go.AddComponent<T>();
        typeof(BuildingBase).GetMethod("ConfigureRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(building, new object[] { data });
        go.SetActive(true); return building;
    }
    static void Stock(StoreBin bin, AntColony.Data.ResourceType type, int amount, float shelf)
    {
        var lots = new FoodLots(); lots.Add(amount, shelf);
        Check(ResourceManager.Instance.AddTo(bin, type, amount, lots: lots) == amount, "stock " + type);
    }
    static void ResetMeal(CommanderAnt c)
    {
        var m = c.PersonalState.meal;
        m.satiety = 10; m.eatSeconds = m.poisonSeconds = m.retrySeconds = 0; m.pickedFood = 0; m.served = m.rotten = false; m.diet = CommanderAnt.AllDiet;
        c.CommandStop();
    }
    static void Warp(CommanderAnt c, Vector3 p) => Check(c.Agent.Warp(p), "warp to test island");

    static void Traits()
    {
        var counts = new int[3]; var grades = new int[4]; var starting = new int[4];
        for (int i = 0; i < 20000; i++) counts[TraitRules.Count((i + .5f) / 20000) - 1]++;
        Check(counts.SequenceEqual(new[] { 19000, 900, 100 }), "trait count 95/4.5/0.5");
        for (int i = 0; i < 10000; i++) grades[(int)TraitRules.RollGrade((i + .5f) / 10000, false)]++;
        Check(grades.SequenceEqual(new[] { 2500, 6000, 1400, 100 }), "slot grades 25/60/14/1");
        for (int i = 0; i < 9900; i++) starting[(int)TraitRules.RollGrade((i + .5f) / 9900, true)]++;
        Check(starting.SequenceEqual(new[] { 2500, 6000, 1400, 0 }), "starting grades renormalize over 99");
        Check(TraitRules.Count(.95f) == 2 && TraitRules.Count(.995f) == 3, "count exact boundaries");
        var activities = new[] { CommanderActivity.Gathering, CommanderActivity.Building, CommanderActivity.Farming, CommanderActivity.Fishing,
            CommanderActivity.Crafting, CommanderActivity.Research, CommanderActivity.Medicine, CommanderActivity.Cooking,
            CommanderActivity.Strength, CommanderActivity.Art, CommanderActivity.Politics, CommanderActivity.Husbandry };
        for (int i = 0; i < 13; i++)
        {
            var gold = (CommanderTrait)((int)CommanderTrait.GoldGathering + i);
            var diamond = (CommanderTrait)((int)CommanderTrait.DiamondGathering + i);
            var t = new CommanderTraits(); Check(t.TryAdd(gold) && !t.TryAdd(diamond), "same specialty excludes gold/diamond " + i);
            Check(t.Grade(gold) == TraitGrade.Gold && t.Grade(diamond) == TraitGrade.Diamond, "grade lookup " + i);
            if (i < 12) Check(Near(t.MetricBonus(activities[i]), .2f), "gold metric +20 " + i);
            else Check(Near(TraitRules.InvestigationMultiplier(t), .8f), "gold investigation -20");
            t.values.Clear(); t.TryAdd(diamond);
            if (i < 12) Check(Near(t.MetricBonus(activities[i]), .3f), "diamond metric +30 " + i);
            else Check(Near(TraitRules.InvestigationMultiplier(t), .7f), "Huginn errors/detection/capture -30");
            Check(Near(t.MetricBonus(CommanderActivity.Melee), 0) && Near(t.MetricBonus(CommanderActivity.Ranged), 0), "combat untouched " + i);
            var copy = JsonUtility.FromJson<CommanderTraits>(JsonUtility.ToJson(t));
            Check(copy.Has(diamond) && copy.Grade(diamond) == TraitGrade.Diamond, "enum grade JSON roundtrip " + i);
        }
        var art = new CommanderTraits(); art.TryAdd(CommanderTrait.DiamondCrafting);
        Check(Near(TraitRules.WorkBonus(art, CommanderActivity.Crafting, false), 0)
            && Near(TraitRules.WorkBonus(art, CommanderActivity.Crafting, true), .3f), "Muramasa equipment crafting only");
        art.values.Clear(); art.TryAdd(CommanderTrait.DiamondHusbandry);
        Check(Near(TraitRules.WorkBonus(art, CommanderActivity.Husbandry, false), 0), "Hamelin duration only");
        for (int i = 0; i < 500; i++)
        {
            var start = StartingCandidate.Roll(i); Check(start.Valid && start.traits.values.Count >= 1 && start.traits.values.Count <= 3, "starting count/validation");
            Check(start.traits.values.All(t => start.traits.Grade(t) != TraitGrade.Diamond), "no starting diamonds");
            var t = CommanderTraits.Inherit(CommanderTraits.Random(), CommanderTraits.Random());
            var rebuilt = new CommanderTraits(); Check(t.values.All(rebuilt.TryAdd), "generated traits obey exclusions");
        }
    }

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var scale = Time.timeScale; var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Impl1025-" + Guid.NewGuid().ToString("N"));
        NavMeshDataInstance nav = default;
        try
        {
            UnityEngine.Random.InitState(1025); Traits();
            Check(Near(CommanderAnt.MealPoisonChance(0), .06f) && Near(CommanderAnt.MealPoisonChance(10), .01f)
                && Near(CommanderAnt.MealPoisonChance(20), 0), "poison chance depends on cooking, no floor/cleanliness input");
            foreach (var p in new[] { (-1, .33f), (0, .6f), (1, .78f), (2, .96f) })
                Check(Near(MedicalRules.TendQuality(10, p.Item1, false, 1, false), p.Item2), "existing treatment quality " + p.Item1);
            Check(Near(MedicalRules.TendQuality(20, -1, true, 1, true), .35f), "no medicine cap then self factor");
            var recipes = Processor.RecipesFor(BuildingKind.MedicineBench);
            var basic = Array.FindIndex(recipes, r => r.output == AntColony.Data.ResourceType.MedicineBasic);
            var normal = Array.FindIndex(recipes, r => r.output == AntColony.Data.ResourceType.MedicineNormal);
            var advanced = Array.FindIndex(recipes, r => r.output == AntColony.Data.ResourceType.MedicineAdvanced);
            Check(basic == 2 && normal == 3 && advanced == 4, "old animal recipe indices preserved");
            Check(recipes[basic].work == 8 && recipes[normal].work == 12 && recipes[advanced].work == 20 && recipes.All(r => r.amount == 1), "D01 A times/output");
            Check(recipes[basic].inputs.SequenceEqual(new[] { (AntColony.Data.ResourceType.Herb, 2) })
                && recipes[normal].inputs.SequenceEqual(new[] { (AntColony.Data.ResourceType.Herb, 2), (AntColony.Data.ResourceType.Cloth, 1) })
                && recipes[advanced].inputs.SequenceEqual(new[] { (AntColony.Data.ResourceType.MedicineNormal, 1), (AntColony.Data.ResourceType.Herb, 2), (AntColony.Data.ResourceType.Component, 1) }), "D01 A exact ingredients");
            Check(recipes.All(r => r.support == null), "medical production has no support skill");
            var definitions = MedicineResearch.Definitions((ScienceTechnology)100, (ScienceTechnology)101);
            Check(definitions[0].Tier == 2 && definitions[0].Work == 400 && definitions[0].Food == 100 && definitions[0].Soil == 100 && definitions[0].Special == 10, "steam pharmacy cost");
            Check(definitions[1].Tier == 4 && definitions[1].Work == 1500 && definitions[1].Food == 200 && definitions[1].Soil == 200 && definitions[1].Special == 30
                && definitions[1].Prerequisites.Single() == (ScienceTechnology)100, "electric precision pharmacy/prerequisite");
            Check(EarlyConstruction.Cost(BuildingKind.MedicineBench).SequenceEqual(new[] { (AntColony.Data.ResourceType.Wood, 20), (AntColony.Data.ResourceType.Stone, 10) }), "bench fixed cost");

            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261025, mapSize = MapSize.Small }); await Ready();
            GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, 10);
            var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember && !x.IsDead);
            c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); c.Traits.values.Clear();
            c.PersonalState.carriedRations = new FoodLots();
            var work = c.WorkRate(CommanderActivity.Research); var carry = c.TotalLoadCapacity;
            c.Traits.TryAdd(CommanderTrait.DiamondResearch);
            Check(Near(c.WorkRate(CommanderActivity.Research), work * 1.3f), "actual research work rate +30");
            c.Traits.values.Clear(); c.Traits.TryAdd(CommanderTrait.DiamondStrength);
            Check(Near(c.TotalLoadCapacity, carry * 1.3f), "actual carry capacity +30 after strength"); c.Traits.values.Clear();
            var origin = new Vector3(10000, 0, 10000);
            var sources = new List<NavMeshBuildSource>();
            foreach (var p in new[] { origin, origin + Vector3.right * 60 }) sources.Add(new NavMeshBuildSource {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(20, 1, 20), transform = Matrix4x4.TRS(p - Vector3.up * .5f, Quaternion.identity, Vector3.one), area = 0 });
            var mesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(c.Agent.agentTypeID), sources, new Bounds(origin + Vector3.right * 30, new Vector3(100, 10, 40)), Vector3.zero, Quaternion.identity);
            Check(mesh != null, "isolated navmesh baked"); made.Add(mesh); nav = NavMesh.AddNavMeshData(mesh);
            Warp(c, origin);
            var a = Put<Storage>(BuildingKind.Storage, origin - Vector3.right * 5);
            var b = Put<Storage>(BuildingKind.Storage, origin + Vector3.right * 5);
            var blocked = Put<Storage>(BuildingKind.Storage, origin + Vector3.right * 60);
            Check(c.TryWorkApproach(a.Position, out _) && !c.TryWorkApproach(blocked.Position, out _), "disconnected storage inaccessible");
            Stock(blocked.Bin, AntColony.Data.ResourceType.FineMeal, 1, 1);
            Stock(a.Bin, AntColony.Data.ResourceType.SimpleMeal, 1, 100);
            ResetMeal(c);
            Check(!(bool)Call(c, "FetchMeal", true) && a.Bin.Amount(AntColony.Data.ResourceType.SimpleMeal) == 1, "no consumption before arrival");
            Warp(c, a.Position); Check((bool)Call(c, "FetchMeal", true), "collect reachable simple meal");
            Check(c.PersonalState.meal.quality == 1 && blocked.Bin.Amount(AntColony.Data.ResourceType.FineMeal) == 1, "blocked fine meal never remotely spent");
            Stock(a.Bin, AntColony.Data.ResourceType.FineMeal, 1, 100); Stock(b.Bin, AntColony.Data.ResourceType.FineMeal, 1, 10);
            a.Bin.priority = 1; b.Bin.priority = 9; ResetMeal(c); Warp(c, b.Position);
            Check((bool)Call(c, "FetchMeal", true) && b.Bin.Amount(AntColony.Data.ResourceType.FineMeal) == 0 && a.Bin.Amount(AntColony.Data.ResourceType.FineMeal) == 1, "globally stalest meal beats storage priority");
            ResourceManager.Instance.TakeFrom(a.Bin, AntColony.Data.ResourceType.FineMeal, 1);
            Stock(a.Bin, AntColony.Data.ResourceType.Food, 1, 10); Stock(a.Bin, AntColony.Data.ResourceType.Food, 1, 100); Stock(b.Bin, AntColony.Data.ResourceType.Food, 1, 20);
            ResetMeal(c); Warp(c, a.Position);
            Check(!(bool)Call(c, "FetchMeal", true) && c.PersonalState.meal.pickedFood == 1, "raw portion can span stores");
            Warp(c, b.Position); Check((bool)Call(c, "FetchMeal", true), "finish raw portion from next oldest lot");
            Check(a.Bin.Amount(AntColony.Data.ResourceType.Food) == 1 && Near(a.Bin.Lots(AntColony.Data.ResourceType.Food).Stalest, 100), "newer lot in first bin left intact");
            var state = JsonUtility.FromJson<CommanderMealState>(JsonUtility.ToJson(c.PersonalState.meal));
            Check(state.Valid && state.served, "picked meal survives personal JSON state");
            state.pickedFood = 1; state.pickedType = (int)AntColony.Data.ResourceType.Soil;
            Check(!state.Valid, "non-food pending portion rejected");

            var bench = Put<Processor>(BuildingKind.MedicineBench, origin);
            Check(bench.Job == CommanderJobs.Nursing && bench.Skill == CommanderActivity.Medicine, "medicine only via medical work");
            Check(bench.AddOrder(basic, Workshop.OrderMode.Count, 1), "basic needs no research");
            Check(!bench.AddOrder(normal, Workshop.OrderMode.Count, 1) && !bench.AddOrder(advanced, Workshop.OrderMode.Count, 1), "normal/advanced locked before research");
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Herb, 2);
            Check(!bench.Work(c, 20), "forbidden nursing cannot manufacture");
            c.SetJobEnabled(CommanderJobs.Nursing, true); c.PersonalState.sleep.asleep = false; c.PersonalState.meal.eatSeconds = 0;
            var before = c.Talents.Xp(CommanderActivity.Research);
            var medicineBefore = ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.MedicineBasic);
            Check(bench.Work(c, 100) && bench.Current == null, "basic medicine production completes");
            Check(Near(c.Talents.Xp(CommanderActivity.Research), before), "manufacturing gives no research support XP");
            Check(ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.MedicineBasic) == medicineBefore + 1, "one basic medicine produced");
            // Claude의 연구 등록 인계까지 확인한다. 미등록이면 이 마지막 검사가 명시적으로 실패한다.
            Check(CampaignResearch.Technologies.Any(d => d.Name == "약제") && CampaignResearch.Technologies.Any(d => d.Name == "정밀 약제"), "Claude pharmacy research registration");
            var normalResearch = CampaignResearch.Technologies.Single(d => d.Name == "약제");
            var advancedResearch = CampaignResearch.Technologies.Single(d => d.Name == "정밀 약제");
            var research = CampaignResearch.Instance.CaptureState(); research.completed.Add((int)normalResearch.Technology); CampaignResearch.Instance.RestoreState(research);
            Check(bench.AddOrder(normal, Workshop.OrderMode.Count, 1) && !bench.AddOrder(advanced, Workshop.OrderMode.Count, 1), "pharmacy opens only normal medicine");
            research.completed.Add((int)advancedResearch.Technology); CampaignResearch.Instance.RestoreState(research);
            Check(bench.AddOrder(advanced, Workshop.OrderMode.Count, 1), "precision pharmacy opens advanced medicine");
            return "PASS Impl1025CodexChecks " + checks;
        }
        finally
        {
            if (nav.valid) nav.Remove();
            foreach (var item in made.AsEnumerable().Reverse()) if (item != null) Object.DestroyImmediate(item);
            made.Clear(); SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = scale; UnityEngine.Random.state = random;
        }
    }
}
