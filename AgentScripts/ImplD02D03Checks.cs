using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class ImplD02D03Checks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("D02/D03 FAIL: " + label); checks++; File.WriteAllText("Temp/ImplD02D03Checks-progress.txt", checks + " " + label); }
    static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) < .002f, label + " " + a + "/" + b);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(25);
        Check(!AntColony.Save.SaveSystem.Busy, "load finished"); Time.timeScale = 0;
    }
    static void Rules()
    {
        var traits = new AntColony.Units.CommanderTraits(); traits.values.Clear();
        foreach (int s in new[] { 0, 10, 20 })
        {
            Near(AntColony.Core.AssessmentRules.Error(s, traits), 30 - s, "error width");
            Near(AntColony.World.InvestigationRules.Discovery(s, 1, traits), (30 - s) * .01f, "neutral p");
            Near(AntColony.World.InvestigationRules.Discovery(s, .5f, traits), (30 - s) * .005f, "friendly p");
            Near(AntColony.World.InvestigationRules.Discovery(s, 1.5f, traits), (30 - s) * .015f, "hostile p");
            Near(AntColony.World.InvestigationRules.Capture(s, traits), (50 - 2 * s) * .01f, "conditional q");
        }
        foreach (var trait in new[] { AntColony.Units.CommanderTrait.GoldInvestigation, AntColony.Units.CommanderTrait.DiamondInvestigation })
        {
            traits.values.Clear(); traits.values.Add(trait); float mult = trait == AntColony.Units.CommanderTrait.GoldInvestigation ? .8f : .7f;
            Near(AntColony.Core.AssessmentRules.Error(10, traits), 20 * mult, "Huginn current error");
            Near(AntColony.World.InvestigationRules.Discovery(10, 1.5f, traits), .3f * mult, "Huginn discovery");
            Near(AntColony.World.InvestigationRules.Capture(10, traits), .3f * mult, "Huginn captivity");
        }
        Near(AntColony.Core.AssessmentRules.Stars(100, 100), 3, "median three");
        Near(AntColony.Core.AssessmentRules.Stars(112.5f, 100), 3.5f, "halfway rounds upward");
        Near(AntColony.Core.AssessmentRules.Stars(0, 100), 1, "minimum");
        Near(AntColony.Core.AssessmentRules.Stars(400, 100), 5, "maximum");
        Near(AntColony.Core.AssessmentRules.Median(new float[] { 1, 10, 3, 5 }, 2), 4, "even median");
        Near(AntColony.Core.AssessmentRules.Median(Array.Empty<float>(), 32), 32, "empty population floor");
        Check(AntColony.Units.CommanderTalents.Count == 16 && (int)AntColony.Units.CommanderActivity.Investigation == 15, "16 skills and stable slot");
        var empty = JsonUtility.FromJson<AntColony.Core.TalentAssessmentState>("{}");
        Check(AntColony.Core.TalentAssessment.Validate(empty, 0, 0), "old empty state marker");
        Check(AntColony.Save.SaveFileV1.CurrentVersion == 19, "version stays 19");
    }
    static void SeedRisk(bool discovered, bool captured, float p, float q)
    {
        for (int seed = 0; seed < 100000; seed++)
        {
            UnityEngine.Random.InitState(seed); float a = UnityEngine.Random.value, b = UnityEngine.Random.value;
            if ((a < p) == discovered && (!discovered || (b < q) == captured)) { UnityEngine.Random.InitState(seed); return; }
        }
        throw new Exception("risk seed not found");
    }
    static void Home(AntColony.World.ExpeditionTransport ship)
    {
        for (int i = 0; i < 20 && ship.IsWithdrawing; i++) ship.Tick(.5f);
        Check(ship.State == AntColony.World.ExpeditionState.Returning, "physical withdrawal finished");
        ship.Tick(ship.TravelSeconds + 1);
        Check(ship.State == AntColony.World.ExpeditionState.Home, "successful home landing");
    }
    static AntColony.World.ExpeditionTransport Board(AntColony.World.WorldMapManager world, params AntColony.Units.CommanderAnt[] people)
    {
        var ship = world.CreateTransport(false, world.HomePosition + Vector3.right * 8, true);
        // Isolate physical boarding from route finding; the real TryBoard still reserves and boards.
        foreach (var c in people)
        {
            c.Transport?.TryUnloadCrew(); c.CommandStop(); c.Agent.enabled = false; c.transform.position = ship.Position;
            c.ApplyMovementMode(); c.PersonalState.carriedRations = new AntColony.Core.FoodLots();
        }
        Check(ship.TryBoard(people), "board real order queue"); ship.Tick(.01f);
        Check(ship.Crew.Count == people.Length && ship.PendingCrewOrderCount == 0, "crew boarded");
        return ship;
    }
    public static async Task<string> Main()
    {
        checks = 0;
        var root = AntColony.Save.SaveStorage.RootOverride; float scale = Time.timeScale;
        var settings = AntColony.Core.UserSettings.Current.Clone(); var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; isolated.autoPauseThreat = false;
        AntColony.Core.UserSettings.Apply(isolated, false);
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "D02D03-" + Guid.NewGuid().ToString("N"));
        try
        {
            Rules(); Check(Application.isPlaying, "Play required for integration"); await Ready();
            var team = Enumerable.Range(0, 3).Select(i => AntColony.Core.StartingCandidate.Roll(i)).ToArray();
            foreach (var t in team) { t.traits.values.Clear(); t.ageYears = 20; }
            AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions { seed = 261025, mapSize = AntColony.Core.MapSize.Small, startingCount = 3, startingTeam = team });
            await Ready();
            var world = AntColony.World.WorldMapManager.Instance; var assessment = world.Assessment;
            var people = AntColony.Core.CommanderRoster.Instance.Commanders.Take(3).ToArray();
            foreach (var c in people) { c.SetJobEnabled(AntColony.Units.CommanderJobs.All, false); c.CommandStop(); }
            string first = people[0].PersonalState.id;
            Check(assessment.AssignEvaluator(people[0]), "assign evaluator");
            people[0].PersonalState.meal.eatSeconds = 2;
            assessment.Tick(5); Check(assessment.Rating(first) == null && assessment.Progress == 0, "assessor meal pauses initial work");
            people[0].PersonalState.meal.eatSeconds = 0;
            assessment.Tick(9.99f); Check(assessment.Rating(first) == null, "first evaluation takes ten seconds");
            assessment.Tick(.01f); Check(assessment.Rating(first).HasValue, "first report at ten seconds");
            assessment.Tick(20); Check(assessment.Reports.Count == 3, "one target at a time");
            var before = assessment.Capture(); var bias = JsonUtility.ToJson(before.biases);
            float at = before.reports[0].at; assessment.Tick(150);
            Check(assessment.Reports.First(r => r.target == first).at >= at + 150, "daily refresh without another initial delay");
            Check(JsonUtility.ToJson(assessment.Capture().biases) == bias, "daily update keeps fixed biases");
            Check(assessment.AssignEvaluator(null), "remove evaluator"); at = assessment.Reports.First(r => r.target == first).at;
            assessment.Tick(300); Near(assessment.Reports.First(r => r.target == first).at, at, "absence preserves date");
            var stale = assessment.Rating(first).Value;
            people[2].Talents.levels[0] = Mathf.Min(20, people[2].Talents.levels[0] + 1);
            Near(assessment.Rating(first).Value.Current, stale.Current, "absence preserves displayed stars despite baseline change");
            assessment.AssignEvaluator(people[0]); assessment.Tick(10);
            Check(JsonUtility.ToJson(assessment.Capture().biases) == bias, "returning evaluator retains pair bias");
            people[0].Traits.values.Add(AntColony.Units.CommanderTrait.DiamondInvestigation); assessment.Tick(150);
            var exact = assessment.Reports.First(r => r.target == first);
            Check(exact.exactPotential && exact.potentialError == 0, "Huginn exact potential");
            Near(exact.potential, people[0].Talents.potential, "exact original PA");
            people[0].Traits.values.Clear(); assessment.AssignEvaluator(people[1]); assessment.Tick(30);
            Check(assessment.Reports.First(r => r.target == first).exactPotential, "ordinary assessor preserves exact PA");
            foreach (var r in assessment.Reports)
            {
                var rating = assessment.Rating(r.target).Value;
                Check(rating.Skills.Length == 16 && rating.Skills.All(s => s >= 1 && s <= 5 && s * 2 == Mathf.Floor(s * 2)), "all 16 half-star skills");
                Check(rating.PotentialLow <= rating.PotentialHigh, "potential interval");
            }
            assessment.SetInterested(first, true);
            Check(assessment.Capture().reports.First(r => r.target == first).interested, "interest flag persists in saved state");
            var site = world.Sites.First(s => AntColony.World.DiplomacyManager.Instance.Faction(s) != null && assessment.Targets(s).Count > 0);
            var faction = AntColony.World.DiplomacyManager.Instance.Faction(site); faction.affinity = 0; faction.war = false;
            Array.Clear(faction.treaties, 0, faction.treaties.Length);
            int expected = Mathf.Min(3, assessment.Targets(site).Count);
            var ship = Board(world, people[0], people[2]);
            Check(!ship.TryInvestigate(site, people[0], people[2]), "low food needs explicit force");
            Check(!ship.TryInvestigate(site, people[0], people[2], "missing-target", true) && ship.Investigation == null, "zero focus candidates prevents departure");
            Check(ship.TryInvestigate(site, people[0], people[2], allowInsufficientFood: true), "search departs without facility/cost");
            Check(ship.Investigation.targets.Count == expected && ship.Investigation.targets.Distinct().Count() == expected, "random unique search up to three");
            Check(!ship.TryInvestigate(site, people[0]), "duplicate mission rejected");
            ship.Tick(ship.TravelSeconds); Check(ship.State == AntColony.World.ExpeditionState.Deployed && people[0].IsEmbarked, "arrive undercover");
            int skill = people[0].Talents.Level(AntColony.Units.CommanderActivity.Investigation);
            float p = AntColony.World.InvestigationRules.Discovery(skill, 1, people[0].Traits), q = AntColony.World.InvestigationRules.Capture(skill, people[0].Traits);
            ship.Tick(74.9f); Check(!ship.Investigation.riskChecked, "no risk before midpoint");
            SeedRisk(false, false, p, q); ship.Tick(.1f); Check(ship.Investigation.riskChecked, "single midpoint roll");
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var error), "save local mission: " + error);
            string json = File.ReadAllText(AntColony.Save.SaveSlots.PathFor(false, 0));
            Check(AntColony.Save.SaveValidator.TryParse(json, out var saved, out error), "parse mission: " + error);
            Check(saved.assessment.present && saved.assessment.missions[0].riskChecked, "marker and risk persist");
            var old = Newtonsoft.Json.Linq.JObject.Parse(json); old.Remove("assessment");
            Check(AntColony.Save.SaveValidator.TryParse(old.ToString(), out var legacy, out error), "old save without assessment: " + error);
            var absent = JsonUtility.FromJson<AntColony.Core.TalentAssessmentState>("{}");
            var savedState = assessment.Capture(); assessment.Restore(absent);
            Check(assessment.Reports.Count == 0 && assessment.Missions.Count == 0, "explicit absent marker restores defaults");
            assessment.Restore(savedState);
            var corrupt = Newtonsoft.Json.Linq.JObject.Parse(json); corrupt["assessment"]["missions"][0]["elapsed"] = -1;
            Check(!AntColony.Save.SaveValidator.TryParse(corrupt.ToString(), out _, out _), "negative progress rejected before loading");

            corrupt = Newtonsoft.Json.Linq.JObject.Parse(json); corrupt["assessment"]["missions"][0]["ship"] = 999;
            Check(!AntColony.Save.SaveValidator.TryParse(corrupt.ToString(), out _, out _), "invalid ship reference rejected");
            var duplicate = JsonUtility.FromJson<AntColony.Core.TalentAssessmentState>(JsonUtility.ToJson(savedState));
            duplicate.biases.Add(duplicate.biases[0]);
            Check(!AntColony.Core.TalentAssessment.Validate(duplicate, world.Transports.Count, world.Sites.Count), "duplicate bias pairs rejected");
            Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 0), out error), "load mission: " + error); await Ready();
            world = AntColony.World.WorldMapManager.Instance; assessment = world.Assessment;
            ship = world.Transports.First(t => t.Investigation != null);
            Check(ship.Investigation.riskChecked && ship.Crew.All(c => c.IsEmbarked), "restored concealed crew/risk");
            Check(assessment.Reports.First(r => r.target == first).interested, "interest flag survives real save/load");
            var pendingIds = ship.Investigation.targets.ToArray();
            SeedRisk(true, true, p, q); ship.Tick(74.9f);
            Check(ship.Investigation.outcome == AntColony.World.InvestigationOutcome.Investigating, "no reroll after load, not complete early");
            ship.Tick(.1f); Check(ship.Investigation.outcome == AntColony.World.InvestigationOutcome.Completed, "full 150 seconds");
            Check(pendingIds.All(id => !assessment.Reports.Any(r => r.target == id)), "completed data withheld until home");
            Check(assessment.ExpelInvestigation(ship), "late expulsion event"); Home(ship);
            Check(pendingIds.All(id => assessment.Rating(id).HasValue), "completed late expulsion still reports at home");
            people = AntColony.Core.CommanderRoster.Instance.Commanders.Take(3).ToArray();
            var focus = pendingIds[0]; var permanent = JsonUtility.ToJson(assessment.Reports.First(r => r.target == focus));
            ship = Board(world, people[0], people[2]);
            Check(ship.TryInvestigate(site = world.Sites.First(s => assessment.Targets(s).Any(t => t.Id == focus)), people[0], people[2], focus, true), "focused repeat one target");
            Check(ship.Investigation.targets.Count == 1, "focus one"); ship.Tick(ship.TravelSeconds);
            SeedRisk(true, false, p, q); ship.Tick(75);
            Check(ship.Investigation.outcome == AntColony.World.InvestigationOutcome.Expelled && ship.Investigation.pending.Count == 0, "midpoint expulsion invalidates whole batch"); Home(ship);
            Check(JsonUtility.ToJson(assessment.Reports.First(r => r.target == focus)) == permanent, "expulsion preserves permanent report");
            ship = Board(world, people[0], people[2]); Check(ship.TryInvestigate(site, people[0], people[2], focus, true), "capture trip"); ship.Tick(ship.TravelSeconds);
            SeedRisk(true, true, p, q); ship.Tick(75);
            Check(ship.Investigation.outcome == AntColony.World.InvestigationOutcome.Captured && people[0].IsCaptive && !people[2].IsCaptive, "captured evaluator, independent escort return");
            Check(AntColony.World.DiplomacyManager.Instance.HeldCaptives(AntColony.World.DiplomacyManager.Instance.Faction(site)).Contains(people[0]), "existing ransom path");
            Check(AntColony.Save.SaveSystem.TrySave(false, 1, out error), "captivity save: " + error);
            Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 1), out error), "captivity load: " + error); await Ready();
            Check(AntColony.Core.CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == first).IsCaptive, "captivity restored at foreign site");
            var result = "PASS: " + checks + " D02/D03 checks"; File.WriteAllText("Temp/ImplD02D03Checks-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/ImplD02D03Checks-result.txt", e.ToString()); throw; }
        finally
        {
            AntColony.Save.SaveStorage.RootOverride = root;
            AntColony.Core.UserSettings.Apply(settings, false); Time.timeScale = scale; UnityEngine.Random.state = random;
        }
    }
}
