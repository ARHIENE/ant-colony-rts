using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

// Run through RunChecks in a disposable Play session. No pre-existing stockpile is required.
public static class ImplC6Checks
{
    static int count;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label) { if (!value) throw new Exception("C6 FAIL " + count + ": " + label); count++; }
    static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static BuildingDto Find(SaveFileV1 f, string kind) => f.buildings.Single(b => b.runtimeBuilt && b.kind == kind);
    static void Invalid(SaveFileV1 source, Action<SaveFileV1> mutate, string field)
    {
        var f = Copy(source); mutate(f);
        Check(!SaveValidator.Validate(f, out var error) && error.Contains(field), field + ": " + error);
    }
    static T Building<T>(BuildingKind kind, Vector3 at) where T : BuildingBase
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.SetActive(false); go.name = "C6 " + kind; go.transform.position = at;
        var data = ScriptableObject.CreateInstance<BuildingData>(); data.kind = kind; data.maxHealth = 300;
        var b = go.AddComponent<T>();
        typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(b, new object[] { data });
        go.SetActive(true); return b;
    }
    static async Task Idle()
    {
        var end = DateTime.UtcNow.AddSeconds(30);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(25);
        Check(!SaveSystem.Busy, "load finished within 30 unscaled seconds");
        Time.timeScale = 0;
    }
    static async Task<SaveFileV1> Load(SaveFileV1 f)
    {
        Check(SaveValidator.Validate(f, out var error), "fixture: " + error);
        var path = Path.Combine(SaveStorage.Root, "c6-fixture.json");
        SaveStorage.WriteAtomic(path, JsonUtility.ToJson(f));
        Check(SaveSystem.TryLoad(path, out error), "load accepted: " + error);
        await Idle(); Check(GameSession.Instance.GameStarted, "restored playable session");
        return SaveSnapshot.Capture();
    }
    public static Task<string> Main() => Run();
    public static async Task<string> Run()
    {
        count = 0; Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplC6-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Idle();
            var options = new NewGameOptions { startingCount = 2, seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest,
                startingTeam = new[] { StartingCandidate.Roll(0), StartingCandidate.Roll(1) } };
            foreach (var c in options.startingTeam) c.traits.values.Clear();
            SaveSystem.NewGame(options); await Idle();
            foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            // Bootstrap integration is owned elsewhere; create the campaign explicitly in the fixture.
            var rocket = RocketCampaign.Instance ?? new GameObject("C6 RocketCampaign").AddComponent<RocketCampaign>();
            rocket.Restore(null);
            var at = CommanderRoster.Instance.Commanders[0].Position;
            var processor = Building<Processor>(BuildingKind.Carpentry, at + Vector3.right * 4);
            typeof(Processor).GetMethod("RestoreState", Hidden).Invoke(processor, new object[] {
                new Processor.State { current = 0, progress = 1, makerId = "departed-maker", ruined = true } });
            typeof(BuildingBase).GetMethod("RestoreHealth", Hidden).Invoke(processor, new object[] { 0f });
            Check(processor.CurrentHealth == 0 && !processor.enabled, "processor ruin retains zero health");
            var workshop = Building<Workshop>(BuildingKind.Workshop, at + Vector3.right * 7);
            typeof(Workshop).GetMethod("RestoreState", Hidden).Invoke(workshop, new object[] {
                new Workshop.State { materialVersion = 1, jobs = new List<Workshop.Job> {
                    new Workshop.Job { recipe = EquipmentRecipe.Mandible, work = 2, materialsPaid = true, makerId = "departed-maker" } } },
                CommanderRoster.Instance.Commanders.ToList() });
            var farm = Building<BuildingBase>(BuildingKind.Farm, at + Vector3.forward * 5);
            farm.gameObject.AddComponent<ResourceNode>(); var plot = farm.gameObject.AddComponent<FarmPlot>();
            plot.Configure(FarmCrop.Fungus, false); plot.Data.seedVersion = 1; plot.Data.harvests = 2; plot.Data.nextCrop = (int)FarmCrop.Honeydew;
            FarmSeeds.Restore(new FarmSeeds.State { counts = new[] { 3, 2, 1 }, collected = new List<string> { "wild:one" } });
            Building<AirshipYard>(BuildingKind.AirshipYard, at + Vector3.forward * 9);
            var tank = Building<SupplyFacility>(BuildingKind.FluidTank, at + Vector3.left * 4);
            tank.RestoreSupply(new SupplyFacility.State { fluid = (int)AntColony.Data.ResourceType.Water, amount = 10, work = 2 });
            var crop = Building<StarterCropBed>(BuildingKind.StarterMushroomBed, at + Vector3.left * 7);
            crop.RestoreCrop(new StarterCropBed.State { planted = true, growth = 12, wateredSeconds = 20 });
            var f = SaveSnapshot.Capture();
            Check(SaveFileV1.CurrentVersion == 19, "v19 retained");
            Check(SaveValidator.TryParse(JsonUtility.ToJson(f), out var parsed, out var error), "JsonUtility full snapshot: " + error);
            Check(parsed.buildings.Where(b => b.kind != "AirshipYard").All(b => b.airship == null), "unrelated inline airships discarded by kind");
            Invalid(f, x => x.farmSeeds.counts = new[] { 1, 2 }, "farm seeds");
            Invalid(f, x => x.farmSeeds.collected.Add("wild:one"), "farm seeds");
            Invalid(f, x => Find(x, "Farm").farm.harvests = 3, "farm state");
            Invalid(f, x => Find(x, "Farm").farm.seedVersion = 2, "farm state");
            Invalid(f, x => Find(x, "Farm").farm.nextCrop = 99, "farm state");
            Invalid(f, x => Find(x, "Workshop").workshop.materialVersion = 2, "craft material version");
            Invalid(f, x => Find(x, "Workshop").workshop.jobs[0].materialsPaid = false, "craft payment");
            Invalid(f, x => Find(x, "Carpentry").processor.progress = float.NaN, "processor state");
            Invalid(f, x => Find(x, "Carpentry").health = 1, "processor state");
            Invalid(f, x => Find(x, "Carpentry").processor.orders.Add(new Processor.Order { recipe = 99 }), "processor orders");
            Invalid(f, x => Find(x, "FluidTank").supply.amount = SupplyFacility.Capacity + 1, "supply facility");
            Invalid(f, x => Find(x, "StarterMushroomBed").starterCrop.wateredSeconds = float.PositiveInfinity, "starter crop");
            Invalid(f, x => x.rocket.crisisResumeAt = x.gameSeconds + 3 * GameCalendar.SecondsPerDay + 1, "rocket crisis grace");
            var legacy = Copy(f); legacy.rocket = null; legacy.farmSeeds = null; legacy.world.pause = null;
            Check(SaveValidator.TryParse(JsonUtility.ToJson(legacy), out _, out error), "legacy null inline states: " + error);

            // Saved carriers use explicit identities; missing old IDs map to transport:<index>.
            var ship = WorldMapManager.Instance.CreateTransport(false, at);
            Check(ship != null, "vehicle fixture created");
            f = SaveSnapshot.Capture();
            var transport = f.world.transports.Last(); transport.bossCarrierId = "c6-carrier"; transport.foodDebt = .75f; transport.dockingRemaining = .5f;
            f.campaign.bosses.obtained[0] = true; f.campaign.bosses.claimed[0] = 0;
            f.campaign.bosses.cargo.Add(new BossProgress.Parcel { id = "c6-parcel", carrier = "c6-carrier", boss = 0, cycle = 0, count = 2 });
            Invalid(f, x => x.world.transports.Add(Copy(x.world.transports.Last())), "boss carrier identity");
            Invalid(f, x => x.campaign.bosses.cargo[0].carrier = "missing", "boss cargo carrier");
            Invalid(f, x => x.world.transports.Last().cargoFood = GameBalance.VehicleCargo - 1, "boss inclusive cargo capacity");
            Invalid(f, x => { x.campaign.bosses.cargo.Clear(); x.world.transports.Last().foodDebt = float.NaN; }, "expedition state"); // 핵심물 적재 초과가 먼저 걸리지 않게 비운다
            Invalid(f, x => { x.campaign.bosses.cargo.Clear(); x.world.transports.Last().dockingRemaining = ExpeditionRules.DockSeconds + 1; }, "expedition state");
            Invalid(f, x => { x.campaign.bosses.cargo.Clear(); x.world.transports.Last().walking = true; x.world.transports.Last().aircraft = true; }, "expedition state");
            var oldCarrier = Copy(f); oldCarrier.world.transports.Last().bossCarrierId = null;
            oldCarrier.campaign.bosses.cargo[0].carrier = "transport:" + (oldCarrier.world.transports.Count - 1);
            Check(SaveValidator.Validate(oldCarrier, out error), "legacy carrier index: " + error);
            f.world.pause = new SimulationPause.State { speed = 3, reasons = new List<string> { "manual" }, incidents = new List<string> { "test:1" } };
            f.world.defenseReturn.disabled.Add(f.commanders[0].personalState.id);
            var economy = new RocketEconomy(); economy.Observe(f.gameSeconds, 100, 2);
            f.rocket.economy = economy;
            f.rocket.crisisResumeAt = f.gameSeconds + 3 * GameCalendar.SecondsPerDay;
            var pad = Find(f, "AirshipYard");
            pad.airship = new AirshipYard.State { hull = true, engine = true, phase = RocketPhase.Preparing,
                preparationRemaining = AirshipYard.PreparationSeconds - 1, fuelType = (int)AntColony.Data.ResourceType.Special, fuelUnits = 15,
                snapshot = economy.Preview(f.gameSeconds, 100, 2) };
            Invalid(f, x => Find(x, "AirshipYard").airship.launchRequested = true, "rocket launch passengers");
            Invalid(f, x => Find(x, "AirshipYard").airship.snapshot.taken = false, "airship");
            var actual = await Load(f);
            Check(Find(actual, "Carpentry").health == 0 && Find(actual, "Carpentry").processor.ruined
                && Find(actual, "Carpentry").processor.makerId == "departed-maker", "ruin and absent maker restored");
            Check(Find(actual, "Workshop").workshop.jobs[0].materialsPaid && Find(actual, "Workshop").workshop.jobs[0].work == 2, "paid unfinished work restored");
            Check(actual.farmSeeds.counts.SequenceEqual(new[] { 3, 2, 1 }) && Find(actual, "Farm").farm.harvests == 2, "seed ledger and third-harvest cursor restored");
            Check(Find(actual, "FluidTank").supply.amount == 10 && Find(actual, "StarterMushroomBed").starterCrop.wateredSeconds == 20, "fluid and prepaid crop water restored");
            Check(actual.world.transports.Last().bossCarrierId == "c6-carrier" && actual.campaign.bosses.cargo.Single().count == 2, "parcel and carrier restored once");
            Check(actual.world.transports.Last().foodDebt == .75f && actual.world.transports.Last().dockingRemaining == .5f, "supply and docking restored");
            Check(actual.world.pause.speed == 3 && actual.world.pause.reasons.Contains("manual")
                && actual.world.defenseReturn.disabled.Contains(actual.commanders[0].personalState.id), "pause and defense return restored");
            Check(Find(actual, "AirshipYard").airship.preparationRemaining == pad.airship.preparationRemaining
                && actual.rocket.crisisResumeAt == f.rocket.crisisResumeAt, "preparation and destruction grace restored");
            Check(actual.colony.food == f.colony.food && actual.colony.soil == f.colony.soil
                && actual.randomState == f.randomState, "restore does not repay resources or advance random stream");
            var boarding = Copy(actual); var state = Find(boarding, "AirshipYard").airship;
            state.phase = RocketPhase.Boarding; state.preparationRemaining = 0; state.passengers.Add(0); state.launchRequested = true;
            actual = await Load(boarding);
            Check(Find(actual, "AirshipYard").airship.launchRequested && Find(actual, "AirshipYard").airship.passengers.SequenceEqual(new[] { 0 }), "pending launch restored without committing launch");
            var destroyed = Copy(actual); var ruins = Find(destroyed, "AirshipYard"); ruins.health = 0;
            ruins.airship = new AirshipYard.State { phase = RocketPhase.Destroyed };
            actual = await Load(destroyed);
            Check(Find(actual, "AirshipYard").airship.phase == RocketPhase.Destroyed, "destroyed rocket restored"); // 체력 0 건물은 복원 시 최소 체력으로 남는다(실제 파괴는 Die가 제거)
            Check(SaveSystem.TrySave(false, 0, out error), "actual slot save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "actual slot load: " + error); await Idle();
            Check(SaveSnapshot.Capture().campaign.bosses.cargo.Count == 1, "repeated disk load does not duplicate parcels");
            return "PASS " + count + " (C6 v19 integration; no launch resolution simulated)";
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Random.state = random; }
    }
}
