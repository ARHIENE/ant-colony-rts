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
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplC4Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("C4 FAIL " + checks + ": " + message); checks++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;
    static void Pool(int free, int assigned = 0, int reserved = 0) => typeof(AntPool)
        .GetMethod("RestoreCounts", Hidden).Invoke(AntPool.Instance, new object[] { free, assigned, reserved });
    static void Resolve(AirshipYard yard) => typeof(AirshipYard).GetMethod("ResolveLaunch", Hidden).Invoke(yard, null);
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("C4 load timeout");
        Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplC4-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 1, seed = 261024, mapSize = MapSize.Small,
                biome = MapBiome.Forest, startingTeam = new[] { StartingCandidate.Roll(0) } };
            options.startingTeam[0].traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            return Run();
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Random.state = random; }
    }

    // 1~2명 불시착으로 시작한 전용 검사 Play 세션에서 호출한다.
    public static string Run()
    {
        checks = 0;
        Check(Application.isPlaying && !SaveSystem.Busy, "dedicated Play session ready");
        var pop = ColonyPopulation.Instance; var rm = ResourceManager.Instance; var roster = CommanderRoster.Instance;
        Check(pop != null && rm != null && roster != null && roster.Count >= 1 && roster.Count <= 2, "crash survivors without stockpile assumption");
        var population = pop.Capture();
        int free = AntPool.Instance.Free, assigned = AntPool.Instance.Assigned, reserved = AntPool.Instance.Reserved;
        var rocketState = RocketCampaign.Instance.Capture();
        var researchState = CampaignResearch.Instance.CaptureState();
        var bin = rm.BaseBin.Capture();
        AirshipYard yard = null; BuildingData definition = null; GameObject floor = null, natural = null, shipObject = null;
        Action<AirshipYard, RocketSnapshot, float> assault = (y, s, dt) => { };
        bool capacityAdded = false;
        try
        {
            foreach (var c in roster.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            pop.S.young = pop.S.old = pop.S.agingDue = 0; pop.S.elderlyService = true; pop.S.sentiment = 60;
            pop.S.policy = MilitaryPolicy.Total;
            Pool(0, 5, 200); pop.S.old = 8; pop.S.agingDue = 7;
            Check(pop.AvailableMilitary(true) == 1 && !pop.TryMilitaryDraft(2, true), "G38 no redrafting pending elderly");
            Check(pop.TryMilitaryDraft(1, true) && pop.S.old == 7 && pop.S.agingDue == 7, "G38 only unassigned elderly enter pool");
            Pool(100); pop.S.old = pop.S.agingDue = 0;
            pop.S.adminWork = pop.AdminDemandWork;
            float work = pop.AdminDemandWork;
            typeof(ColonyPopulation).GetMethod("TickAdministration", Hidden).Invoke(pop, new object[] { GameCalendar.SecondsPerDay / 2 });
            Check(Near(pop.S.adminWork, work / 2), "G17 half day consumes half daily workload");
            Check(ColonyPopulation.TaxMaterials.Contains(AntColony.Data.ResourceType.Wood)
                && ColonyPopulation.TaxMaterials.Contains(AntColony.Data.ResourceType.Stone), "G14 wood and stone in basic taxes");
            pop.S.taxFood = 0; pop.S.taxSoil = 100; pop.S.taxMaterial = AntColony.Data.ResourceType.Wood;
            int wood = rm.GetAmount(AntColony.Data.ResourceType.Wood), stone = rm.GetAmount(AntColony.Data.ResourceType.Stone);
            rm.AddCapacity(AntColony.Data.ResourceType.Soil, 1000);
            capacityAdded = true;
            pop.PayTax();
            Check(rm.GetAmount(AntColony.Data.ResourceType.Wood) > wood && rm.GetAmount(AntColony.Data.ResourceType.Stone) > stone, "G14 actual basic material supply");

            var before = RocketEconomy.CaptureOwned();
            floor = new GameObject("C4 loose special"); floor.SetActive(false);
            floor.AddComponent<ResourceNode>().ConfigureLoot(AntColony.Data.ResourceType.Special, 7); floor.SetActive(true);
            Check(Near(RocketEconomy.CaptureOwned().wealth - before.wealth, 70), "G45 floor property counted once");
            natural = new GameObject("C4 unmined special"); natural.SetActive(false);
            natural.AddComponent<ResourceNode>().ConfigureNatural(AntColony.Data.ResourceType.Special, 99, 0); natural.SetActive(true);
            Check(Near(RocketEconomy.CaptureOwned().wealth - before.wealth, 70), "G45 unmined resource excluded");
            var token = new object(); rm.Reserve(token, rm.BaseBin, AntColony.Data.ResourceType.Special, 7);
            Check(Near(RocketEconomy.CaptureOwned().wealth - before.wealth, 70), "G45 destination reservation adds no property");
            rm.Reserve(token, rm.BaseBin, AntColony.Data.ResourceType.Special, 0);
            shipObject = new GameObject("C4 cargo transport");
            var ship = shipObject.AddComponent<ExpeditionTransport>();
            var cargo = (Dictionary<AntColony.Data.ResourceType, int>)typeof(ExpeditionTransport).GetField("cargo", Hidden).GetValue(ship);
            cargo[AntColony.Data.ResourceType.Special] = 3;
            Check(Near(RocketEconomy.CaptureOwned().wealth - before.wealth, 100), "G45 transport physical cargo counted once");
            Pool(80, 15, 5);
            Check(Near(RocketEconomy.CaptureOwned().population, 100 + roster.Count), "G43/G45 global assigned and reserved count once");
            pop.ObserveRocketEconomy();
            Check(RocketCampaign.Instance.Economy.samples.Count > 0, "G45 observation from population");
            var history = new RocketEconomy(); history.Observe(0, 100, 20); history.Observe(300, 0, 0);
            Check(Near(history.Preview(900, 0, 0).averageWealth, 100f / 3), "G45 time weighted history");

            definition = ScriptableObject.CreateInstance<BuildingData>(); definition.kind = BuildingKind.AirshipYard; definition.maxHealth = 500;
            yard = new GameObject("C4 rocket").AddComponent<AirshipYard>(); yard.transform.position = roster.Commanders[0].Position;
            typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(yard, new object[] { definition });
            yard.RestoreState(new AirshipYard.State { hull = true, engine = true }, roster.Commanders);
            rm.Add(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay * FinalBattleEconomy.DefenseDays, ResourceReason.Other);
            int fuel = rm.GetAmount(AirshipYard.FuelResource);
            Check(yard.TryLoadFuel(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay), "G52 load all fifteen days");
            Check(!yard.TryLoadFuel(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay)
                && rm.GetAmount(AirshipYard.FuelResource) == fuel - 15 * AirshipYard.FuelUnitsPerDay, "G52 one payment only");
            Check(yard.ActivationReserves.Single().Amount == 15 * AirshipYard.FuelUnitsPerDay, "G68 real reserve requirement");
            RocketCampaign.AssaultTick += assault; // 검사 한정 타이머 구독. 실제 생성기 구현을 대신하지 않는다.
            var owned = RocketEconomy.CaptureOwned();
            Check(yard.TryActivate(owned.wealth, owned.population), "G20 activation"); yard.Tick(AirshipYard.PreparationSeconds);
            Check(yard.Phase == RocketPhase.Boarding, "G20 fifteen days completed");
            Pool(12); pop.S.young = 2; pop.S.old = 1; pop.S.agingDue = 0; pop.S.civilianLocations.Clear();
            var group = new CivilianLocation { id = 1, count = 15, position = yard.Position, moving = true };
            pop.S.civilianLocations.Add(group); pop.S.nextCivilianLocation = 2;
            Check(yard.TryBoard(1) == 0, "G16 no boarding before arrival");
            group.moving = false; group.arrived = true;
            group.position += Vector3.right * 20;
            Check(yard.TryBoard(1) == 0, "G16 arrival at distant shelter is not rocket arrival");
            group.position = yard.Position;
            Check(yard.TryBoard(1) == 10 && yard.OccupiedSeats == 10 && pop.Total == 5, "G16 seats and actual population transfer");
            Check(yard.TryBoard(1) == 0, "G16 full rocket cannot remove more population");
            var saved = JsonUtility.FromJson<AirshipYard.State>(JsonUtility.ToJson(yard.CaptureState(roster.Commanders)));
            Check(saved.civilianPassengers == 10 && saved.civilianYoung == 2 && AirshipYard.Validate(saved, roster.Count, out _), "G41 citizen passenger JSON");
            yard.RestoreState(saved, roster.Commanders);
            Check(pop.Total == 5 && yard.CivilianPassengers == 10, "G41 restore does not pay passengers twice");
            var outcome = CampaignOutcome.Capture(yard.Passengers);
            Check(outcome.citizensEscaped == 10 && outcome.citizensStaying == 5, "G43 removed passengers not subtracted twice");
            yard.Unload(); Check(pop.Total == 15 && pop.S.young == 2 && yard.CivilianPassengers == 0, "G16 disembark preserves age groups and total");
            yard.Unload(); Check(pop.Total == 15, "G16 disembark idempotent");
            saved.civilianPassengers = 11;
            Check(!AirshipYard.Validate(saved, roster.Count, out _), "G41 citizen seat overflow rejected");
            saved.civilianPassengers = 10;
            // 실제 파괴 콜백의 시민 반환을 확인한 다음 별도 발사대에서 시민만 발사한다.
            yard.RestoreState(saved, roster.Commanders); Pool(4); pop.S.young = 0; pop.S.old = 1;
            yard.TakeDamage(float.MaxValue);
            Check(pop.Total == 15 && yard.CivilianPassengers == 0 && yard.Phase == RocketPhase.Destroyed, "G20 destruction unloads survivors once");
            Check(!yard.TryLoadFuel(AirshipYard.FuelResource, 1), "G52 destroyed rocket cannot load fuel");
            Object.DestroyImmediate(yard.gameObject);
            yard = new GameObject("C4 rebuilt rocket").AddComponent<AirshipYard>();
            typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(yard, new object[] { definition });
            Pool(4); pop.S.young = 0; pop.S.old = 1;
            yard.RestoreState(saved, roster.Commanders);
            Check(yard.TryDepart(), "G20 civilian-only launch request"); Resolve(yard);
            Check(yard.Phase == RocketPhase.Launched && RocketCampaign.Instance.Ending.citizensEscaped == 10
                && RocketCampaign.Instance.Ending.citizensStaying == 5, "G43 civilian-only ending retained");
            yard.Unload(); Check(pop.Total == 5, "G43 launched passengers never returned by scene cleanup");
            return "PASS " + checks;
        }
        finally
        {
            RocketCampaign.AssaultTick -= assault;
            if (yard != null) { yard.Unload(); Object.DestroyImmediate(yard.gameObject); }
            if (floor != null) Object.DestroyImmediate(floor); if (natural != null) Object.DestroyImmediate(natural);
            if (shipObject != null) Object.DestroyImmediate(shipObject); if (definition != null) Object.DestroyImmediate(definition);
            Pool(free, assigned, reserved); pop.Restore(population); RocketCampaign.Instance.Restore(rocketState);
            CampaignResearch.Instance.RestoreState(researchState);
            typeof(StoreBin).GetMethod("Apply", Hidden).Invoke(rm.BaseBin, new object[] { bin });
            if (capacityAdded) rm.AddCapacity(AntColony.Data.ResourceType.Soil, -1000);
        }
    }
}
