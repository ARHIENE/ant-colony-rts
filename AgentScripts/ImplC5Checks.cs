using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplC5Checks
{
    static int count;
    static readonly List<GameObject> made = new List<GameObject>();
    const BindingFlags StaticHidden = BindingFlags.Static | BindingFlags.NonPublic;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("C5 FAIL " + count + ": " + message); count++; }
    static T Put<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", StaticHidden)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template); made.Add(go); go.name = "C5 " + kind;
        go.transform.position = position; go.SetActive(true); return go.GetComponent<T>();
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
        foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
    }
    static void Act(Component target, string method = "Act")
    {
        WorkTargetPanel.Select(target);
        var panel = Object.FindAnyObjectByType<WorkTargetPanel>(); Check(panel != null, "work panel exists");
        typeof(WorkTargetPanel).GetMethod(method, Hidden).Invoke(panel, null);
    }
    static float FloorAmount(AntColony.Data.ResourceType type) => ResourceNode.Available
        .Where(n => n != null && n.IsLooseCargo && n.ResourceType == type).Sum(n => n.AmountRemaining);
    public static Task<string> Main() => Run();
    public static async Task<string> Run()
    {
        count = 0; made.Clear(); Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var scale = Time.timeScale;
        var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplC5-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var recipes = Processor.RecipesFor(BuildingKind.Smelter);
            Check(recipes.Take(3).Select(r => r.output).SequenceEqual(new[] { AntColony.Data.ResourceType.Iron,
                AntColony.Data.ResourceType.Copper, AntColony.Data.ResourceType.Steel }), "old smelter order indices");
            Check(recipes.Skip(3).Select(r => r.output).SequenceEqual(SupplyRecipes.For(BuildingKind.Smelter).Select(r => r.output)), "supply recipes appended");
            var tabs = (IEnumerable)typeof(BuildScreen).GetField("Tabs", StaticHidden).GetValue(null);
            var kinds = new List<BuildingKind>();
            foreach (IEnumerable tab in tabs)
            foreach (var entry in tab) kinds.Add((BuildingKind)entry.GetType().GetField("kind").GetValue(entry));
            foreach (var kind in new[] { BuildingKind.PlanterBed, BuildingKind.StarterMushroomBed, BuildingKind.ManualWell,
                BuildingKind.WaterPump, BuildingKind.FluidPipe, BuildingKind.FluidTank, BuildingKind.SteamGenerator,
                BuildingKind.OilDrill, BuildingKind.OilRefinery, BuildingKind.FuelCharger, BuildingKind.CivicPublicService,
                BuildingKind.School, BuildingKind.CitizenClinic }) Check(kinds.Count(k => k == kind) == 1, "one build entry " + kind);
            Check(!kinds.Contains(BuildingKind.WaterSource) && !kinds.Contains(BuildingKind.OilDeposit), "natural sources not buildable in menu");
            foreach (var biome in BiomeRules.All)
            {
                Check(StartingCandidate.Clothes(biome, MapClimate.Cold) == "평범한 보온 옷", "cold overrides biome " + biome);
                Check(StartingCandidate.Clothes(biome, MapClimate.TropicalWet) == "평범한 통풍 옷", "wet overrides biome " + biome);
            }
            foreach (var commanders in new[] { 1, 2 })
            {
                var biome = commanders == 1 ? MapBiome.Forest : MapBiome.Desert;
                var team = Enumerable.Range(0, commanders).Select(StartingCandidate.Roll).ToArray();
                foreach (var c in team) c.traits.values.Clear();
                CrashLandingStart.Begin(new NewGameOptions { seed = 261024, mapSize = MapSize.Small, biome = biome,
                    climate = MapClimate.Cold, startingCount = commanders, startingTeam = team });
                await Ready();
                Check(CommanderRoster.Instance.Count == commanders && Object.FindAnyObjectByType<Stockpile>() == null, "crash start without stockpile");
                Check(CommanderRoster.Instance.Commanders.All(c => c.PersonalState.ordinaryClothes == "평범한 보온 옷"), "actual starter clothes");
                var wild = ResourceNode.Available.Where(n => n.name.StartsWith("시작 야생 작물 ")).ToArray();
                Check(wild.Length == 3, "three guaranteed wild crops");
                var seeds = FarmSeeds.Capture();
                try
                {
                    foreach (var node in wild)
                    {
                        Check(!node.IsLooseCargo && node.AmountRemaining > 0 && NavRoute.Reachable(WorldMapManager.Instance.HomePosition, node.transform.position), "wild crop reachable");
                        var source = node.GetComponent<FarmSeedSource>();
                        Check(source != null && source.Crop == CrashLandingStart.StartingCrop(biome), "adapted seed source");
                        var before = FarmSeeds.Count(source.Crop);
                        Check(FarmSeeds.Collect(source) && !FarmSeeds.Collect(source) && FarmSeeds.Count(source.Crop) == before + 1, "one seed per source");
                    }
                }
                finally { FarmSeeds.Restore(seeds); }
            }
            var home = WorldMapManager.Instance.HomePosition + Vector3.right * 1000;
            Put<Storage>(BuildingKind.Storage, home);
            var rm = ResourceManager.Instance;
            Check(rm.Add(AntColony.Data.ResourceType.PlantingMaterial, 2) == 2, "test storage accepts planting material");
            var bed = Put<StarterCropBed>(BuildingKind.PlanterBed, home + Vector3.forward * 4);
            var planting = rm.GetAmount(AntColony.Data.ResourceType.PlantingMaterial);
            Act(bed); Act(bed);
            Check(bed.Crop.planted && rm.GetAmount(AntColony.Data.ResourceType.PlantingMaterial) == planting - 1, "panel plant spends once");
            var tank = Put<SupplyFacility>(BuildingKind.FluidTank, home + Vector3.right * 4);
            Check(tank.Fill(AntColony.Data.ResourceType.Water, 7), "fill water for UI check");
            var water = FloorAmount(AntColony.Data.ResourceType.Water); Act(tank, "EatCorpse");
            Check(tank.Supply.amount == 0 && FloorAmount(AntColony.Data.ResourceType.Water) == water + 7, "panel unloads water to floor");
            Check(rm.Add(AntColony.Data.ResourceType.Drum, 1) == 1, "test empty drum");
            var charger = Put<SupplyFacility>(BuildingKind.FuelCharger, home + Vector3.forward * 8);
            Check(charger.Fill(AntColony.Data.ResourceType.Petroleum, 10), "test petroleum");
            var drums = FloorAmount(AntColony.Data.ResourceType.FilledFuelDrum); Act(charger); Act(charger);
            Check(FloorAmount(AntColony.Data.ResourceType.FilledFuelDrum) == drums + 1 && charger.Supply.amount == 0, "panel fills drum once");
            GameMenuController.Instance.Diplomacy();
            // 월드맵이 이미 해금된 세션이면 외교 화면의 캐러밴 버튼으로 들어간다(해금 전에는 바로 캐러밴 화면).
            Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "SupplyCaravan")?.onClick.Invoke();
            Check(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b => b.name == "Buy 1"), "early diplomacy shows supply purchase buttons");
            Check(rm.Add(AntColony.Data.ResourceType.Food, 20) == 20, "test caravan currency");
            var events = ColonyEvents.Instance; var eventState = events.Capture();
            eventState.caravanRemaining = 90; events.Restore(eventState);
            var food = rm.GetAmount(AntColony.Data.ResourceType.Food);
            var wood = FloorAmount(AntColony.Data.ResourceType.Wood);
            for (var i = 0; i < 2; i++)
            {
                GameMenuController.Instance.Diplomacy();
                Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "SupplyCaravan")?.onClick.Invoke();
                var buy = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .Single(b => b.name == "Buy 1" && b.transform.parent.name == "Supply Wood");
                Check(buy.interactable, "caravan buy enabled"); buy.onClick.Invoke();
            }
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == food - 2 * ColonyEvents.SupplyPrice(AntColony.Data.ResourceType.Wood)
                && FloorAmount(AntColony.Data.ResourceType.Wood) == wood + 2, "early caravan repeated purchase through UI");
            GameMenuController.Instance.Resume();
            return "PASS " + count + " (manual well execution requires C1; fixed seed key/save requires handoff)";
        }
        finally
        {
            WorkTargetPanel.Clear();
            foreach (var go in made) if (go != null) { go.SetActive(false); Object.Destroy(go); }
            made.Clear(); SaveStorage.RootOverride = root; UserSettings.Apply(settings, false);
            UnityEngine.Random.state = random; Time.timeScale = scale;
        }
    }
}
