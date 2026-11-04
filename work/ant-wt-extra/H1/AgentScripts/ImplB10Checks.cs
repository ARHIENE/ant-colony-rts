using System;
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
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplB10Checks
{
    static int count;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("B10 FAIL " + count + ": " + label); count++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
    static float FloorAmount(AntColony.Data.ResourceType type) => ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == type).Sum(n => n.AmountRemaining);
    static GameObject Template(BuildingKind kind) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
    static T Put<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var template = Template(kind); Check(template != null, "template " + kind);
        var go = Object.Instantiate(template); made.Add(go); go.name = "B10 " + kind;
        go.transform.position = position; go.transform.localScale = Vector3.one; go.SetActive(true);
        return go.GetComponent<T>();
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
        foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
    }
    static async Task Start(int commanders, MapBiome biome)
    {
        var team = Enumerable.Range(0, commanders).Select(StartingCandidate.Roll).ToArray();
        foreach (var candidate in team) candidate.traits.values.Clear();
        CrashLandingStart.Begin(new NewGameOptions { seed = 261024, mapSize = MapSize.Small, biome = biome, climate = MapClimate.Cold,
            startingCount = commanders, startingTeam = team });
        await Ready();
        Check(CommanderRoster.Instance.Count == commanders, "one/two commander crash start");
        Check(Object.FindAnyObjectByType<Stockpile>() == null, "no stockpile assumed");
    }

    public static async Task<string> Main()
    {
        count = 0; made.Clear(); Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var scale = Time.timeScale;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB10-" + Guid.NewGuid().ToString("N"));
        var random = UnityEngine.Random.state;
        try
        {
            await Ready(); await Start(1, MapBiome.Forest);
            var home = WorldMapManager.Instance.HomePosition;
            foreach (var biome in BiomeRules.All)
            foreach (var commanders in new[] { 1, 2 })
            {
                var before = new HashSet<ResourceNode>(ResourceNode.Available);
                var sources = new HashSet<SupplyFacility>(SupplyFacility.All);
                var seeds = FloorAmount(AntColony.Data.ResourceType.PlantingMaterial);
                StartingEnvironment.Create(home, biome, commanders);
                var nodes = ResourceNode.Available.Where(n => !before.Contains(n)).ToArray();
                foreach (var type in new[] { AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.Wood,
                    AntColony.Data.ResourceType.Stone, AntColony.Data.ResourceType.CopperOre, AntColony.Data.ResourceType.TinOre,
                    AntColony.Data.ResourceType.Coal, AntColony.Data.ResourceType.IronOre })
                    Check(nodes.Any(n => n.ResourceType == type && n.AmountRemaining > 0 && Vector3.Distance(home, n.transform.position) <= StartSupplyRules.Radius), "G35 guaranteed " + biome + "/" + type);
                Check(Near(nodes.Where(n => n.ResourceType == AntColony.Data.ResourceType.Food).Sum(n => n.AmountRemaining), StartSupplyRules.StartFood(commanders)), "G58 seven days food " + biome);
                Check(nodes.All(n => NavRoute.Reachable(home, n.transform.position)), "G35 start resources reachable " + biome);
                Check(Near(FloorAmount(AntColony.Data.ResourceType.PlantingMaterial) - seeds, commanders * 3), "G35/G58 planting material " + biome);
                var water = SupplyFacility.All.Single(n => !sources.Contains(n) && n.Kind == BuildingKind.WaterSource);
                Check(water.name == StartSupplyRules.WaterName(biome) && Vector3.Distance(home, water.Position) <= 40, "G59 reachable water " + biome);
                Check(NavRoute.Reachable(home, water.Position), "G59 water has a walking route " + biome);
            }
            Check(BiomeRules.Name(MapBiome.City) == "온대 숲 폐허" && BiomeRules.Name(MapBiome.Garden) == "초원", "G35 biome mapping");
            for (var i = 0; i < 100; i++)
            {
                var cold = WeatherSystem.Roll(Season.Summer, MapBiome.Forest, i / 100f, MapClimate.Cold);
                var wet = WeatherSystem.Roll(Season.Winter, MapBiome.Forest, i / 100f, MapClimate.TropicalWet);
                Check(cold != WeatherKind.Rain && cold != WeatherKind.Storm && wet != WeatherKind.Snow && wet != WeatherKind.Blizzard, "G35 climate variants");
            }

            await Start(2, MapBiome.City); home = WorldMapManager.Instance.HomePosition;
            Check(BiomeRules.FarmAllowed && ScienceEffects.BuildingUnlocked(BuildingKind.Farm), "G58 city not globally forbidden");
            foreach (var kind in new[] { BuildingKind.PlanterBed, BuildingKind.StarterMushroomBed, BuildingKind.ManualWell,
                BuildingKind.WaterPump, BuildingKind.FluidPipe, BuildingKind.FluidTank, BuildingKind.Treadmill })
                Check(BuildingPlacementController.LockReason(kind) == null && Template(kind).GetComponent<BuildingBase>().Data.constructionAnts == 0, "G58/G59 no research or citizen gate " + kind);
            var origin = new Vector3(Mathf.Floor(home.x) + 1000.5f, home.y, Mathf.Floor(home.z) + 1000.5f);
            var floor = Put<FloorTile>(BuildingKind.Floor, origin + new Vector3(20, 0, 20));
            Check(!StartGroundRules.OpenFieldAllowed(floor.Position) && StartGroundRules.OpenFieldAllowed(floor.Position + Vector3.right * 3), "G58 pavement only");
            var wide = Put<StarterCropBed>(BuildingKind.PlanterBed, floor.Position + Vector3.right * 3);
            wide.transform.localScale = Vector3.one * 7;
            Check(StartGroundRules.OpenFieldAllowed(wide.Position) && !StartGroundRules.OpenFieldAllowed(wide), "G58 pavement under edge of wide field is blocked");

            var waterSource = Put<SupplyFacility>(BuildingKind.WaterSource, origin + new Vector3(0, 0, 5));
            Check(!Demolition.CanDemolish(waterSource), "G59 natural water source cannot be demolished");
            var well = Put<SupplyFacility>(BuildingKind.ManualWell, origin + new Vector3(1, 0, 5));
            var tank = Put<SupplyFacility>(BuildingKind.FluidTank, origin + new Vector3(2, 0, 5));
            Check(well.WorkManual(2) && tank.Supply.amount == 0, "G59 manual work incomplete");
            var manualJson = JsonUtility.ToJson(well.Supply); well.RestoreSupply(JsonUtility.FromJson<SupplyFacility.State>(manualJson));
            Check(well.WorkManual(3) && tank.Supply.amount == 10, "G59 five seconds ten water across JSON restore");
            Check(!tank.Fill(AntColony.Data.ResourceType.CrudeOil, 1), "G59 tank prevents mixed fluid");
            Check(!well.WorkManual(float.NaN) && !well.WorkManual(float.PositiveInfinity), "G59 invalid time no work");
            waterSource.gameObject.SetActive(false); Check(!well.WorkManual(5), "G59 missing source blocks manual water"); waterSource.gameObject.SetActive(true);

            var rm = ResourceManager.Instance;
            rm.Add(AntColony.Data.ResourceType.PlantingMaterial, 3);
            var beds = new[] { Put<StarterCropBed>(BuildingKind.StarterMushroomBed, origin + new Vector3(3, 0, 5)),
                Put<StarterCropBed>(BuildingKind.StarterMushroomBed, origin + new Vector3(2, 0, 6)),
                Put<StarterCropBed>(BuildingKind.PlanterBed, origin + new Vector3(2, 0, 4)) };
            var food = FloorAmount(AntColony.Data.ResourceType.Food);
            foreach (var bed in beds) { Check(bed.Plant() && !bed.Plant(), "G58 one seed once"); bed.Tick(StartSupplyRules.CropSeconds); }
            Check(tank.Supply.amount == 1, "G59 one water per cell per day");
            Check(Near(FloorAmount(AntColony.Data.ResourceType.Food) - food, StartSupplyRules.DailyFood * 3), "G58 three beds sustain one commander");
            tank.gameObject.SetActive(false);
            var dry = beds[0].Crop.growth; beds[0].Tick(100);
            Check(Near(beds[0].Crop.growth, dry), "G59 dry bed pauses without free growth"); tank.gameObject.SetActive(true);
            Check(tank.Fill(AntColony.Data.ResourceType.Water, 1), "G59 replenish crop water");
            beds[0].Tick(GameCalendar.SecondsPerDay / 2);
            var paidWater = beds[0].Crop.wateredSeconds;
            beds[0].RestoreCrop(JsonUtility.FromJson<StarterCropBed.State>(JsonUtility.ToJson(beds[0].Crop)));
            beds[0].Tick(GameCalendar.SecondsPerDay / 2);
            Check(tank.Supply.amount == 1 && Near(paidWater, GameCalendar.SecondsPerDay / 2)
                && Near(beds[0].Crop.wateredSeconds, 0), "G59 restored prepaid day does not consume water twice");
            Check(!SupplyFacility.Valid(new SupplyFacility.State { work = float.NaN }) && !StarterCropBed.Valid(new StarterCropBed.State { growth = -1 }), "save rejects invalid supply/crop state");

            var steam = Put<PowerNode>(BuildingKind.SteamGenerator, origin);
            var wire1 = Put<PowerNode>(BuildingKind.PowerWire, origin + Vector3.right);
            var wire2 = Put<PowerNode>(BuildingKind.PowerWire, origin + Vector3.right * 2);
            var wire3 = Put<PowerNode>(BuildingKind.PowerWire, origin + Vector3.right * 3);
            var oilSource = Put<SupplyFacility>(BuildingKind.OilDeposit, origin + new Vector3(1, 0, 2));
            var drill = Put<SupplyFacility>(BuildingKind.OilDrill, origin + new Vector3(1, 0, 1));
            var pipe = Put<SupplyFacility>(BuildingKind.FluidPipe, origin + new Vector3(2, 0, 1));
            var refinery = Put<SupplyFacility>(BuildingKind.OilRefinery, origin + new Vector3(3, 0, 1));
            var oilTank = Put<SupplyFacility>(BuildingKind.FluidTank, origin + new Vector3(4, 0, 1));
            Check(GameBalance.TreadmillWatts == 100 && drill.Demand == 100 && refinery.Demand == 200, "G46 fixed watts");
            Check(PowerGrid.WantsPower(steam), "G46 external consumers request generation without lamp or battery");
            PowerGrid.Tick(.5f); drill.Tick(5); Check(drill.Supply.amount == 0, "G46 unpowered drill stops");
            steam.RestoreFuel(7); PowerGrid.Tick(.5f);
            Check(drill.Powered && refinery.Powered, "G46 solid fuel powers drill and refinery");
            drill.Tick(5); Check(oilTank.Supply.amount == 10, "G46 drill sends crude through pipe without drum");
            refinery.Tick(5); Check(oilTank.Supply.amount == 9 && refinery.Supply.amount == 1, "G46 direct pipeline refining without container");
            pipe.gameObject.SetActive(false); var crude = oilTank.Supply.amount; drill.Tick(5);
            Check(oilTank.Supply.amount == crude && drill.Supply.amount == 10, "G46 broken pipe isolates drill output");
            var depositPosition = oilSource.Position; oilSource.transform.position = pipe.Position;
            drill.Tick(5);
            Check(oilTank.Supply.amount == crude && drill.Supply.amount == 20, "G46 natural deposit cannot bridge broken pipe");
            oilSource.transform.position = depositPosition;
            refinery.Tick(5); Check(oilTank.Supply.amount == crude - 1, "G46 refinery can finish buffered crude after disconnection");
            pipe.gameObject.SetActive(true);
            crude = oilTank.Supply.amount;
            oilSource.gameObject.SetActive(false); drill.Tick(5); Check(oilTank.Supply.amount == crude && drill.Supply.amount == 20, "G46 no deposit no oil"); oilSource.gameObject.SetActive(true);
            steam.RestoreFuel(0); PowerGrid.Tick(.5f); Check(!drill.Powered && !refinery.Powered, "G46 power loss stops facilities");
            rm.Add(AntColony.Data.ResourceType.Coal, 2); var coal = rm.GetAmount(AntColony.Data.ResourceType.Coal);
            typeof(PowerNode).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(steam, null);
            Check(steam.FuelSeconds == 0 && rm.GetAmount(AntColony.Data.ResourceType.Coal) == coal, "G46 paused generator cannot consume fuel");
            Check(SupplyRecipes.For(BuildingKind.Smelter).Single(r => r.output == AntColony.Data.ResourceType.Drum).inputs.Single().amount == 5
                && SupplyRecipes.For(BuildingKind.Smelter).Single(r => r.output == AntColony.Data.ResourceType.GasCylinder).inputs.Single().amount == 10, "G46 steel container recipes");
            var charger = Put<SupplyFacility>(BuildingKind.FuelCharger, origin + new Vector3(5, 0, 1));
            oilTank.RestoreSupply(new SupplyFacility.State { fluid = (int)AntColony.Data.ResourceType.Petroleum, amount = 10 });
            var drums = FloorAmount(AntColony.Data.ResourceType.FilledFuelDrum);
            Check(!charger.FillFuelDrum() && oilTank.Supply.amount == 10, "G46 missing drum preserves petroleum");
            rm.Add(AntColony.Data.ResourceType.Drum, 1);
            Check(charger.FillFuelDrum() && oilTank.Supply.amount == 0
                && FloorAmount(AntColony.Data.ResourceType.FilledFuelDrum) == drums + 1, "G46 charger consumes one drum and ten petroleum");
            Check(!charger.FillFuelDrum(), "G46 no duplicate charged drum");

            var wood = FloorAmount(AntColony.Data.ResourceType.Wood);
            var demolition = Demolition.OrderDemolish(well); Check(demolition != null, "G14 demolition order");
            var mark = demolition.GetComponent<DemolitionSite>();
            typeof(DemolitionSite).GetMethod("OnWorkStarted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mark, null);
            typeof(DemolitionSite).GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mark, null);
            Check(Near(FloorAmount(AntColony.Data.ResourceType.Wood) - wood, 7), "G14 seventy percent fixed material refund");
            typeof(DemolitionSite).GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mark, null);
            Check(Near(FloorAmount(AntColony.Data.ResourceType.Wood) - wood, 7), "G14 refund once"); Object.DestroyImmediate(demolition.gameObject);

            var events = ColonyEvents.Instance;
            events.Restore(new ColonyEvents.State());
            Check(events.EnsureSeasonCaravan(0) && !events.EnsureSeasonCaravan(0), "G14 one guaranteed visit per season");
            var eventState = JsonUtility.FromJson<ColonyEvents.State>(JsonUtility.ToJson(events.Capture())); events.Restore(eventState);
            Check(!events.EnsureSeasonCaravan(0) && events.CaravanPresent, "G14 visit survives JSON restore without duplicate");
            rm.Add(AntColony.Data.ResourceType.Food, 100);
            var bought = FloorAmount(AntColony.Data.ResourceType.Steel); var balance = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Check(events.TryBuySupply(AntColony.Data.ResourceType.Steel, 1) && Near(FloorAmount(AntColony.Data.ResourceType.Steel), bought + 1)
                && rm.GetAmount(AntColony.Data.ResourceType.Food) == balance - ColonyEvents.SupplyPrice(AntColony.Data.ResourceType.Steel), "G14 advanced material trade before diplomacy unlock");
            Check(!events.TryBuySupply(AntColony.Data.ResourceType.Steel, -1) && !events.TryBuySupply(AntColony.Data.ResourceType.Food, 1), "G14 invalid trade rejected");
            foreach (var type in new[] { AntColony.Data.ResourceType.Special, AntColony.Data.ResourceType.Component, AntColony.Data.ResourceType.Sulfur })
            {
                bought = FloorAmount(type);
                Check(events.TryBuySupply(type, 1) && FloorAmount(type) == bought + 1, "G14 recurring essential supply " + type);
            }
            eventState = events.Capture(); eventState.caravanRemaining = 0; events.Restore(eventState);
            Check(!events.TryBuySupply(AntColony.Data.ResourceType.Steel, 1), "G14 expired caravan cannot sell");
            Check(events.EnsureSeasonCaravan(1), "G14 next season repeat supply");

            rm.Add(AntColony.Data.ResourceType.Water, 11); steam.RestoreFuel(6);
            drill.RestoreSupply(new SupplyFacility.State { work = 2, fluid = (int)AntColony.Data.ResourceType.CrudeOil, amount = 4 });
            beds[0].RestoreCrop(new StarterCropBed.State { planted = true, growth = 123, wateredSeconds = 45 });
            var savedWater = rm.GetAmount(AntColony.Data.ResourceType.Water);
            await Task.Delay(100); // 철거의 지연 Destroy가 끝난 뒤 저장.
            Check(SaveSystem.TrySave(false, 0, out var saveError), "save: " + saveError);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out var loadError), "load: " + loadError);
            await Ready();
            var loadedDrill = SupplyFacility.All.Single(n => n.Kind == BuildingKind.OilDrill);
            var loadedSteam = Object.FindObjectsByType<PowerNode>(FindObjectsSortMode.None).Single(n => n.Kind == BuildingKind.SteamGenerator);
            Check(loadedDrill.Supply.amount == 4 && Near(loadedDrill.Supply.work, 2) && Near(loadedSteam.FuelSeconds, 6), "save/load oil and fuel progress");
            Check(ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Water) == savedWater && GameSession.Instance.Options.climate == MapClimate.Cold, "save/load water totals and climate");
            Check(Object.FindObjectsByType<StarterCropBed>(FindObjectsSortMode.None).Any(b => Near(b.Crop.growth, 123) && Near(b.Crop.wateredSeconds, 45)), "save/load paid crop water and growth");
            Check(!ColonyEvents.Instance.EnsureSeasonCaravan(1), "save/load caravan guarantee once");
            Check(ResourceNode.Available.Any(n => n.ResourceType == AntColony.Data.ResourceType.Food && !n.IsLooseCargo && n.AmountRemaining > 0), "save/load natural start food active");
            return "PASS " + count;
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.DestroyImmediate(go);
            made.Clear(); UnityEngine.Random.state = random; Time.timeScale = scale;
            SaveStorage.RootOverride = root; UserSettings.Apply(settings, false);
        }
    }
}
