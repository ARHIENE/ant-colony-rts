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
using Object = UnityEngine.Object;

public static class ImplB4Checks
{
    static int count;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("B4 FAIL " + count + ": " + label); count++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }
    static GameObject Template(BuildingKind kind) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
    static T Put<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var template = Template(kind); Check(template != null, "template " + kind);
        var go = Object.Instantiate(template, position, Quaternion.identity); made.Add(go); go.SetActive(true); return go.GetComponent<T>();
    }
    static void Wash(CommanderAnt c, float seconds) => typeof(CommanderAnt)
        .GetMethod("TickHygiene", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void StopWash(CommanderAnt c) => typeof(CommanderAnt)
        .GetMethod("LeaveWash", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);

    public static async Task<string> Main()
    {
        count = 0; made.Clear(); Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var timeScale = Time.timeScale;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB4-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            Check(GameCalendar.SecondsPerDay == 150, "G08 existing day length");
            var h = new CommanderHygieneState(); h.Tick(150, false, 0);
            Check(Near(h.hygiene, 75), "G60 loses 25 per day");
            h.Tick(600, false, 0); Check(h.hygiene == 0 && h.Valid, "G60 clamps at zero without health state");
            foreach (var pair in new[] { (30f, 0), (29.99f, -3), (10f, -3), (9.99f, -6), (0f, -6) })
                Check(GameBalanceNeeds.HygieneMood(pair.Item1) == pair.Item2, "G60 strict mood threshold " + pair.Item1);
            Check(GameBalanceNeeds.Washed(0, 0) == 50 && GameBalanceNeeds.Washed(29, 0) == 70, "G60 toilet +50 capped 70");
            Check(GameBalanceNeeds.Washed(80, 0) == 80, "toilet never lowers hygiene");
            Check(GameBalanceNeeds.Washed(29, 1) == 59 && GameBalanceNeeds.Washed(90, 1) == 100, "G60 basin +30");
            Check(GameBalanceNeeds.Washed(0, 2) == 100, "G60 shower fills 100");
            Check(GameBalanceNeeds.WaterCost(0) == 0 && GameBalanceNeeds.WaterCost(1) == 1 && GameBalanceNeeds.WaterCost(2) == 3, "G60 water costs");
            Check(!AntColony.Data.ResourceType.Water.IsMaterial() && !ResourceLabels.Materials.Contains(AntColony.Data.ResourceType.Water), "water cannot be selected as construction material");
            h.hygiene = 0; h.Tick(.1f, true, 1); Check(h.hygiene == 30, "G60 travel relief");
            h.hygiene = 0; h.Tick(.1f, true, 1); Check(h.hygiene == 0, "G60 no second relief same day");
            h.Tick(.1f, true, 2); Check(h.hygiene == 30, "G60 next travel day relief");
            h.hygiene = 80; h.Tick(.1f, true, 3); Check(h.hygiene > 79, "travel never lowers hygiene");
            h.travelDay = -2; Check(!h.Valid, "invalid travel state rejected");
            Check(GameBalanceNeeds.ExpectationMood(0) == 15 && GameBalanceNeeds.ExpectationMood(1) == 5
                && GameBalanceNeeds.ExpectationMood(2) == 0, "G31 expectations three tiers");
            foreach (var pair in new[] { (36f, 0), (35f, 1), (20f, 2), (5f, 3) })
                Check(GameBalanceNeeds.MoodWarning(pair.Item1) == pair.Item2, "G31 warning " + pair.Item1);

            var options = new NewGameOptions { startingCount = 1, seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest,
                startingTeam = new[] { StartingCandidate.Roll(0) } };
            options.startingTeam[0].traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var c = CommanderRoster.Instance.Commanders.Single(); c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false);
            c.PersonalState.expectationTier = 2; Check(Near(c.Mood, 60), "G31 base starting mood 60");
            c.PersonalState.expectationTier = 0; Check(Near(c.Mood, 75), "G31 low wealth total 75");
            c.PersonalState.expectationTier = 1; Check(Near(c.Mood, 65), "G31 settlement total 65");
            c.PersonalState.expectationTier = 3; Check(!c.PersonalState.Validate(out _), "invalid expectation rejected");
            c.PersonalState.expectationTier = 0;
            var wounds = c.PersonalState.health.wounds.Count;
            c.PersonalState.hygiene.hygiene = 0; c.TickPersonal(.1f);
            Check(!c.IsDead && c.PersonalState.health.wounds.Count == wounds, "G60 zero hygiene causes no injury/death");
            Check(c.PersonalState.moodFactors.Any(f => f.reason == "위생 불량" && f.value == -6), "G60 existing AddMood reason");
            c.PersonalState.sleep.asleep = true; c.PersonalState.hygiene.hygiene = 100; c.TickPersonal(150);
            Check(Near(c.Hygiene, 75), "G10 hygiene decays during sleep independently of duty"); c.PersonalState.sleep.asleep = false;
            c.PersonalState.hygiene.hygiene = 0; c.PersonalState.hygiene.retrySeconds = 0;
            Wash(c, .1f); Check(c.IsWashing && c.PersonalState.hygiene.temporary, "G60 no facilities starts temporary relief");
            c.RestorePersonalState(c.CapturePersonalState());
            Check(c.PersonalState.hygiene.temporary && c.IsWashing, "G60 temporary relief JSON resume");
            Wash(c, 3); Check(c.Hygiene == 30 && !c.IsWashing, "G60 temporary relief only to 30");
            Check(c.PersonalState.moodFactors.Any(f => f.reason == "임시 용변" && f.value == -3 && f.remaining == 150), "G60 temporary mood one day");

            var rm = ResourceManager.Instance;
            rm.AddCapacity(AntColony.Data.ResourceType.Water, 100); rm.Add(AntColony.Data.ResourceType.Water, 10);
            var toilet = Put<HygieneFixture>(BuildingKind.Toilet, c.Position + Vector3.right);
            var water = rm.GetAmount(AntColony.Data.ResourceType.Water);
            c.PersonalState.hygiene.retrySeconds = 0; c.PersonalState.hygiene.hygiene = 29;
            Wash(c, .1f); Check(c.WashSpot == toilet && c.PersonalState.hygiene.washSeconds == 3, "G60 three second dry toilet");
            Wash(c, 3); Check(c.Hygiene == 70 && rm.GetAmount(AntColony.Data.ResourceType.Water) == water, "G60 dry toilet caps and costs no water");
            toilet.gameObject.SetActive(false);
            foreach (var kind in new[] { BuildingKind.Washbasin, BuildingKind.Shower })
            {
                var spot = Put<HygieneFixture>(kind, c.Position + Vector3.right);
                c.PersonalState.hygiene.hygiene = 20; water = rm.GetAmount(AntColony.Data.ResourceType.Water);
                Wash(c, .1f); Check(c.WashSpot == spot, "G60 starts " + kind);
                Check(rm.GetAmount(AntColony.Data.ResourceType.Water) == water - GameBalanceNeeds.WaterCost(spot.KindIndex), "G60 water spent once at join " + kind);
                Check(spot.Join(c) && rm.GetAmount(AntColony.Data.ResourceType.Water) == water - GameBalanceNeeds.WaterCost(spot.KindIndex), "duplicate join does not spend water");
                Wash(c, GameBalance.WashSeconds); Check(c.Hygiene == (kind == BuildingKind.Shower ? 100 : 50), "G60 completed " + kind);
                rm.TrySpend(AntColony.Data.ResourceType.Water, rm.GetAmount(AntColony.Data.ResourceType.Water));
                Check(!spot.CanUse && !spot.Join(c), "G60 water shortage blocks " + kind);
                rm.Add(AntColony.Data.ResourceType.Water, 10); spot.gameObject.SetActive(false);
            }
            foreach (var kind in new[] { BuildingKind.SleepingMat, BuildingKind.SingleBed, BuildingKind.Toilet, BuildingKind.Washbasin,
                BuildingKind.Campfire, BuildingKind.BigTable })
            {
                Check(ScienceEffects.BuildingUnlocked(kind), "G10/G31 no research " + kind);
                var t = Template(kind); Check(t != null && t.GetComponent<PowerNode>() == null, "G31 no power " + kind);
            }
            var mat = Template(BuildingKind.SleepingMat).GetComponent<BuildingBase>().Data;
            Check(mat.foodCost == 0 && mat.soilCost == 0 && mat.specialCost == 0 && mat.constructionAnts == 0 && mat.buildTimeSeconds == 0, "G31 free sleeping spot");
            var fire = Put<RecreationSpot>(BuildingKind.Campfire, c.Position + Vector3.forward);
            Check(fire.Seats == GameBalance.CampfireSeats && fire.MinimumUsers == 1, "G31/G54 campfire usable solo, 6 seats");

            c.PersonalState.meal.satiety = 20; var food = c.PersonalState.carriedRations.Total;
            c.TickDuty(.1f); Check(c.IsEating && c.PersonalState.carriedRations.Total == food - c.RationPortion, "G10 personal food before work");
            c.TickDuty(GameBalance.EatSeconds * 2); Check(c.Satiety > 90, "G10 existing meal completion");
            c.PersonalState.carriedRations.Take(c.PersonalState.carriedRations.Total);
            rm.Add(AntColony.Data.ResourceType.Food, c.RationPortion);
            c.PersonalState.meal.satiety = 20; c.PersonalState.meal.retrySeconds = 0;
            c.TickDuty(.1f); Check(c.IsEating && c.PersonalState.meal.quality == 0, "G10 existing raw food fallback");
            c.TickDuty(GameBalance.EatSeconds * 2);
            var playSeconds = GameSession.Instance.PlaySeconds; var gameSeconds = GameSession.Instance.GameSeconds;
            GameSession.Instance.MarkStarted(playSeconds, GameCalendar.DaySeconds + 1);
            c.PersonalState.sleep.fatigue = 80;
            c.TickDuty(1);
            Check(c.SleepsRough && c.Fatigue < 80, "G10 existing floor sleep without bed");
            GameSession.Instance.MarkStarted(playSeconds, gameSeconds); c.TickDuty(.1f);
            c.PersonalState.hygiene.travelDay = 12; c.PersonalState.expectationTier = 1;
            var saved = c.CapturePersonalState(); c.RestorePersonalState(saved);
            Check(c.PersonalState.hygiene.travelDay == 12 && c.PersonalState.expectationTier == 1, "G31/G60 JSON fields roundtrip");
            c.StartMentalBreak(MentalBreak.Flee);
            Check(c.PersonalState.mentalBreak == MentalBreak.Wander && !c.IsDeparting, "G31 final commander wanders instead of fleeing");
            return "PASS " + count + " B4 checks";
        }
        finally
        {
            foreach (var c in CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders : Array.Empty<CommanderAnt>())
                if (c != null) StopWash(c);
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = timeScale;
        }
    }
}
