using System;
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

public static class ImplB9Checks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int count;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception("B9 FAIL " + count + ": " + message); count++; }
    private static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }
    private static void Resolve(AirshipYard yard) => typeof(AirshipYard).GetMethod("ResolveLaunch", Hidden).Invoke(yard, null);
    private static AirshipYard NewYard(CommanderAnt near, BuildingData definition)
    {
        var go = new GameObject("B9 test rocket"); go.transform.position = near.Position;
        var yard = go.AddComponent<AirshipYard>();
        typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(yard, new object[] { definition });
        yard.RestoreState(new AirshipYard.State { hull = true, engine = true }, CommanderRoster.Instance.Commanders);
        return yard;
    }
    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB9-" + Guid.NewGuid().ToString("N"));
        var random = UnityEngine.Random.state;
        AirshipYard yard = null, rebuilt = null; GameObject captiveSite = null; BuildingData definition = null;
        Action<AirshipYard, RocketSnapshot, float> assault = null;
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 2, commanderDeath = CommanderDeathMode.Harsh,
                seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest,
                startingTeam = new[] { StartingCandidate.Roll(0), StartingCandidate.Roll(1) } };
            foreach (var candidate in options.startingTeam) candidate.traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var commanders = CommanderRoster.Instance.Commanders.ToArray();
            Check(commanders.Length == 2, "two crash survivors");
            foreach (var c in commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            var campaign = RocketCampaign.Instance;
            Check(campaign != null, "campaign installed without stockpile");
            Check(AirshipYard.PreparationSeconds == 2250 && GameCalendar.SecondsPerSeason == 900, "G08 calendar reuse");
            var history = new RocketEconomy();
            history.Observe(0, 100, 10); history.Observe(300, 0, 0);
            var preview = history.Preview(900, 0, 0);
            Check(Mathf.Abs(preview.averageWealth - 100f / 3) < .001f && preview.Wealth == preview.averageWealth, "time weighted average, not sample mean");
            Check(history.Preview(900, 200, 40).Wealth == 200, "max current and average");
            history.Observe(1200, 0, 0);
            Check(history.Preview(1200, 0, 0).Wealth == 0, "expired season trimmed");
            Check(new RocketEconomy().Preview(0, 1, 1) == null, "missing observations block activation");
            bool rejected = false;
            try { history.Observe(float.NaN, 1, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid observation rejected");
            Check(!AirshipYard.Validate(new AirshipYard.State { preparationRemaining = float.NaN }, 2, out _), "invalid preparation rejected");
            Check(!AirshipYard.Validate(new AirshipYard.State { phase = RocketPhase.Boarding, hull = true, engine = true }, 2, out _), "active state needs fuel and snapshot");
            Check(AirshipYard.Validate(new AirshipYard.State(), 2, out _), "old construction state compatible");
            Check(!AirshipYard.Validate(new AirshipYard.State { fuelType = 2 }, 2, out _), "empty fuel manifest has no resource type");
            Check(!AirshipYard.Validate(new AirshipYard.State { phase = RocketPhase.Preparing, hull = true, engine = true,
                fuelType = (int)AntColony.Data.ResourceType.Special, fuelUnits = 15, snapshot = new RocketSnapshot() }, 2, out _), "zero preparation cannot remain preparing");
            Check(!AirshipYard.Validate(new AirshipYard.State { phase = RocketPhase.Destroyed,
                passengers = new System.Collections.Generic.List<int> { 0 } }, 2, out _), "destroyed rocket cannot own passengers");

            definition = ScriptableObject.CreateInstance<BuildingData>(); definition.kind = BuildingKind.AirshipYard;
            definition.maxHealth = 300; definition.soilCost = 150;
            yard = NewYard(commanders[0], definition);
            Check(yard.Capacity == 10 && !yard.TryDepart(), "base seats and no premature launch");
            yard.RestoreState(new AirshipYard.State { hull = true, engine = true, cocoons = GameBalance.MaxCocoons }, commanders);
            Check(yard.Capacity == 1010, "module seats cap");
            yard.RestoreState(new AirshipYard.State { hull = true, engine = true }, commanders);
            Check(!yard.TryBoard(commanders[0]), "no boarding before preparation");
            campaign.Economy.Observe(GameCalendar.GameSeconds, 200, 2);
            Check(!yard.TryActivate(200, 2), "fuel required");
            typeof(ResourceManager).GetMethod("RestoreState", Hidden).Invoke(ResourceManager.Instance, new object[] { 0, 0, 0, 1000, 1000, 1000 });
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Special, 14);
            Check(!yard.TryLoadFuel(AntColony.Data.ResourceType.Special, 1)
                && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Special) == 14, "fuel shortage consumes nothing");
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Special, 1);
            Check(yard.TryLoadFuel(AntColony.Data.ResourceType.Special, 1)
                && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Special) == 0, "full fifteen day manifest consumed once");
            Check(!yard.TryLoadFuel(AntColony.Data.ResourceType.Special, 1), "no duplicate loading");
            int assaultTicks = 0;
            assault = (y, s, seconds) => { if (y == yard || y == rebuilt) { assaultTicks++; s.currentWealth = 0; } };
            RocketCampaign.AssaultTick += assault;
            Check(yard.TryActivate(200, 2) && yard.Phase == RocketPhase.Preparing, "activate");
            Check(RocketCampaign.Operating && RocketCampaign.SuppressNewCrises, "new crises suppressed");
            Check(yard.ModificationsLocked && !Demolition.CanDemolish(yard) && !Demolition.CanMove(yard)
                && !Demolition.CanRenovate(yard) && !yard.TryBuild(AirshipPart.Cocoon), "active modifications locked");
            yard.TakeDamage(1); yard.Tick(150);
            Check(yard.PreparationRemaining == 2100, "damage and no external power do not reset timer");
            Check(yard.FixedSnapshot.Wealth == 200, "assault callback cannot mutate frozen snapshot");
            var saved = yard.CaptureState(commanders);
            yard.RestoreState(JsonUtility.FromJson<AirshipYard.State>(JsonUtility.ToJson(saved)), commanders);
            Check(yard.PreparationRemaining == 2100 && yard.FixedSnapshot.Wealth == 200, "rocket JSON roundtrip");
            yard.Tick(2100); Check(yard.Phase == RocketPhase.Boarding, "fifteen days completed");
            yard.Tick(150); Check(assaultTicks == 3 && RocketCampaign.SuppressNewCrises, "assault continues while ready");
            Check(CampaignOutcome.Preview(yard.Passengers, commanders[0]).Contains("예상 탈출 장수 1명 / 잔류·외부 장수 1명")
                && !commanders[0].IsEmbarked && yard.Passengers.Count == 0, "boarding confirmation previews proposed outcome without mutating ownership");
            Check(yard.TryBoard(commanders[0]), "board survivor");
            Check(CampaignOutcome.Survivor(commanders[0]), "embarked commander survives");
            var ending = CampaignOutcome.Capture(yard.Passengers);
            Check(ending.commanders.Count(e => e.category == EndingCategory.Escaped) == 1
                && ending.commanders.Count(e => e.category == EndingCategory.Staying) == 1, "exclusive outcome classification");
            var duplicate = JsonUtility.FromJson<CampaignOutcome.State>(JsonUtility.ToJson(ending)); duplicate.commanders.Add(duplicate.commanders[0]);
            Check(!CampaignOutcome.Validate(duplicate), "duplicate ending identity rejected");
            Check(ending.citizensEscaped + ending.citizensStaying == ColonyPopulation.Instance.Total, "citizen total counted once");
            yard.Unload(); Check(CampaignOutcome.Survivor(commanders[0]), "disembarked commander survives");
            commanders[0].PersonalState.treating = true;
            Check(CampaignOutcome.Survivor(commanders[0]), "treatment survives"); commanders[0].PersonalState.treating = false;

            captiveSite = new GameObject("B9 captive site"); var site = captiveSite.AddComponent<ExpeditionSite>();
            var captor = typeof(CommanderAnt).GetProperty("Captor");
            foreach (var c in commanders) captor.SetValue(c, site);
            Check(!CampaignOutcome.HasSurvivors, "last captive is defeat");
            Check(CampaignOutcome.Classify(commanders[0], true) == EndingCategory.Captive, "captivity overrides stale boarding");
            Check(!CampaignOutcome.Preview(yard.Passengers, commanders[0]).Contains("예상 탈출 장수 1명"), "captive candidate cannot appear escaped");
            var prisoner = new Prisoner("B9 released", CommanderRank.Sergeant, new[] { UnitRole.Worker }, new CommanderTraits());
            prisoner.PersonalState.id = Guid.NewGuid().ToString();
            var homecoming = new Homecoming { commander = prisoner };
            DiplomacyManager.Instance.Data.homecomings.Add(homecoming);
            Check(CampaignOutcome.HasSurvivors, "safe homecoming prevents defeat");
            Check(CampaignOutcome.Capture(yard.Passengers).commanders.Count(e => e.id == prisoner.PersonalState.id
                && e.category == EndingCategory.Outside) == 1, "released homecoming recorded once as outside");
            prisoner.PersonalState.dead = true;
            Check(!CampaignOutcome.HasSurvivors, "dead homecoming is not a survivor");
            prisoner.PersonalState.dead = false;
            homecoming.to = "foreign"; Check(!CampaignOutcome.HasSurvivors, "foreign homecoming is not our survivor");
            DiplomacyManager.Instance.Data.homecomings.Remove(homecoming);
            foreach (var c in commanders) captor.SetValue(c, null);
            Check(CampaignOutcome.CaptureWarning.Contains("구출은 남은 아군 필요"), "capture warning contract");
            var one = options.Clone(); one.startingCount = 1; GameSession.Instance.SetOptions(one);
            var campaignSave = campaign.Capture(); campaignSave.initialCommanders = 1; campaign.Restore(campaignSave);
            Check(CampaignOutcome.SingleHarshWarning.Contains("30%") && CampaignOutcome.SingleHarshWarning.Contains("첫 쓰러짐"), "single harsh explicit warning");
            one.commanderDeath = CommanderDeathMode.Gentle; GameSession.Instance.SetOptions(one);
            Check(CampaignOutcome.SingleHarshWarning == null, "independent death mode");
            GameSession.Instance.SetOptions(options);
            campaign.SetHome(new Vector3(7, 0, 9));
            var dto = new SaveFileV1 { rocket = campaign.Capture() };
            var copy = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(dto)); campaign.Restore(copy.rocket);
            Check(HomeReturnPoint.Anchor == new Vector3(7, 0, 9), "home anchor JSON independent of buildings");
            Check(SaveFileV1.CurrentVersion == 19 && RocketCampaign.Validate(null), "version unchanged and old campaign compatible");

            Check(yard.TryBoard(commanders[0]) && yard.TryDepart() && !CampaignResearch.Instance.Departed, "launch queued until end of tick");
            Check(yard.CaptureState(commanders).launchRequested && !yard.TryDepart(), "pending launch saved and duplicate request rejected");
            yard.TakeDamage(100000); Resolve(yard);
            Check(yard.Phase == RocketPhase.Destroyed && !CampaignResearch.Instance.Departed, "same tick destruction beats launch");
            Check(!commanders[0].IsEmbarked && RocketCampaign.SuppressNewCrises, "destroyed rocket unloads, starts grace");
            float destroyedAt = GameCalendar.GameSeconds;
            GameSession.Instance.MarkStarted(0, destroyedAt + 449);
            Check(RocketCampaign.SuppressNewCrises, "three day grace before boundary");
            GameSession.Instance.MarkStarted(0, destroyedAt + 450);
            Check(!RocketCampaign.SuppressNewCrises, "three day grace exact boundary");
            rebuilt = NewYard(commanders[0], definition);
            Check(rebuilt.Phase == RocketPhase.Construction && !rebuilt.FuelLoaded, "rebuild begins fresh");
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Special, 15);
            campaign.Economy.Observe(GameCalendar.GameSeconds, 100, 2);
            Check(rebuilt.TryLoadFuel(AntColony.Data.ResourceType.Special, 1) && rebuilt.TryActivate(100, 2), "rebuilt rocket can activate");
            rebuilt.Tick(AirshipYard.PreparationSeconds);
            Check(rebuilt.TryBoard(commanders[0]) && rebuilt.TryDepart(), "rebuilt launch ready");
            Resolve(rebuilt);
            Check(rebuilt.Phase == RocketPhase.Launched && CampaignResearch.Instance.Departed && campaign.Ending != null, "launch resolves before defeat");
            return "PASS " + count;
        }
        finally
        {
            if (assault != null) RocketCampaign.AssaultTick -= assault;
            if (yard != null) Object.Destroy(yard.gameObject);
            if (rebuilt != null) Object.Destroy(rebuilt.gameObject);
            if (captiveSite != null) Object.Destroy(captiveSite);
            if (definition != null) Object.Destroy(definition);
            UnityEngine.Random.state = random;
            SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0;
        }
    }
}
