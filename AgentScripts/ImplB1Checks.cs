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
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplB1Checks
{
    static int count;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string text) { if (!value) throw new Exception("B1 FAIL " + count + ": " + text); count++; }
    static void Pool(int free, int assigned = 0, int reserved = 0) => typeof(AntPool)
        .GetMethod("RestoreCounts", Hidden).Invoke(AntPool.Instance, new object[] { free, assigned, reserved });
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
    static GameObject Template(BuildingKind kind) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }

    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB1-" + Guid.NewGuid().ToString("N"));
        var random = UnityEngine.Random.state;
        GameObject postObject = null, scoutObject = null, secondScoutObject = null, barracksObject = null;
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 1, seed = 261023, mapSize = MapSize.Small,
                biome = MapBiome.Forest, startingTeam = new[] { StartingCandidate.Roll(0) } };
            options.startingTeam[0].traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var population = ColonyPopulation.Instance;
            var commander = CommanderRoster.Instance.Commanders.Single();
            commander.CommandStop(); commander.SetJobEnabled(CommanderJobs.All, false);
            population.S.sentiment = 60; population.S.old = 0;
            foreach (var kind in new[] { BuildingKind.ConscriptionPost, BuildingKind.ScoutPost })
            {
                Check(ScienceEffects.BuildingUnlocked(kind), "G32/G36 no research gate " + kind);
                Check(EarlyConstruction.HasFixedCost(kind) && EarlyConstruction.Cost(kind).Length == 1
                    && EarlyConstruction.Cost(kind)[0].type == AntColony.Data.ResourceType.Wood
                    && EarlyConstruction.Cost(kind)[0].amount == 20, "G32/G36 fixed twenty wood " + kind);
                var template = Template(kind);
                var data = template.GetComponent<BuildingBase>().Data;
                Check(data.foodCost == 0 && data.soilCost == 20 && data.specialCost == 0
                    && data.constructionAnts == 0, "G32/G36 no citizen construction gate " + kind);
            }
            foreach (MilitaryPolicy policy in Enum.GetValues(typeof(MilitaryPolicy)))
            foreach (var threat in new[] { false, true })
            foreach (var adults in new[] { 0, 9, 10, 20, 100 })
            {
                population.S.policy = policy; Pool(adults);
                Check(population.MobilizationLimit(threat) == Mathf.Max(Mathf.FloorToInt(adults * ColonyPopulation.PolicyRate(policy, threat)), adults >= 10 ? 5 : 0), "G04 limit " + policy + "/" + adults + "/" + threat);
            }
            population.S.policy = MilitaryPolicy.Volunteer; Pool(7, 3);
            Check(population.AvailableMilitary(false) == 2 && population.Taxpayers == 7, "G04 remaining and deployed tax exclusion");
            Check(!population.TryMilitaryDraft(3, false), "G04 refuses excess total");
            population.S.elderlyService = true; population.S.old = 100;
            Check(population.MobilizationLimit(false) == 5, "G04 limit uses adults even with elderly service");
            population.S.old = 0; population.S.elderlyService = false;
            population.S.sentiment = 0;
            Check(population.AvailableMilitary(false) == 0, "G04 unrest still blocks draft");
            population.S.sentiment = 60; Pool(10);
            var limit = population.MobilizationLimit(false);
            barracksObject = new GameObject("B1 training barracks"); barracksObject.AddComponent<Barracks>();
            Check(population.MobilizationLimit(false) == limit, "G32 barracks/training adds no capacity");
            Object.DestroyImmediate(barracksObject); barracksObject = null;
            var postTemplate = Template(BuildingKind.ConscriptionPost);
            postObject = Object.Instantiate(postTemplate); postObject.transform.position = commander.Position - Vector3.forward * 4;
            postObject.SetActive(true); var post = postObject.GetComponent<ConscriptionPost>();
            Check(post.AvailableTroops(commander) == Mathf.Min(5, commander.FreeRanks), "G32/G04 min of available citizens and command room");
            Pool(1000);
            Check(post.AvailableTroops(commander) == commander.FreeRanks, "G04 command room is the smaller limit"); Pool(10);
            Check(!post.TryDeploy(new[] { commander }, new[] { 6 }), "G04 excess formation rejected");
            Check(post.TryDeploy(new[] { commander }, new[] { 5 }), "G32 first five troops with conscription only");
            Check(commander.TroopCount == 5 && population.Taxpayers == 5, "G04 deployed citizens remain adults and pay no tax");
            Check(!commander.TryAssign(1), "G04 reinforcement respects mobilization limit");
            postObject.SetActive(false);
            Check(!post.TryDeploy(new[] { commander }, new[] { 1 }) && commander.TroopCount == 5
                && AntPool.Instance.Assigned == 5, "G32 disabled post blocks new formation and preserves troops");
            Object.DestroyImmediate(postObject); postObject = null;
            Check(commander.TroopCount == 5 && AntPool.Instance.Assigned == 5, "G32 destroyed post preserves existing troops");

            scoutObject = new GameObject("B1 scout"); var scout = scoutObject.AddComponent<ScoutPost>();
            secondScoutObject = new GameObject("B1 second scout"); var second = secondScoutObject.AddComponent<ScoutPost>();
            scoutObject.transform.position = commander.Position;
            Set(scout, "baseChance", 0f); Set(scout, "chancePerCommander", 0f); Set(scout, "maxChance", 0f);
            var history = CampaignHistory.Instance;
            history.Data.scoutRecruitSlot = CommanderRoster.Instance.Count; history.Data.scoutRecruitFailures = 0;
            for (var failure = 0; failure < 3; failure++)
            {
                Check(Mathf.Abs(scout.CurrentChance - failure * .2f) < .0001f, "G36 plus twenty percentage points " + failure);
                // 실패 경로를 확정해 실제 Resolve의 누적과 초소 간 공유를 검사한다.
                var seed = 0;
                do { UnityEngine.Random.InitState(seed++); } while (UnityEngine.Random.value <= scout.CurrentChance);
                UnityEngine.Random.InitState(seed - 1);
                Set(scout, "<Companion>k__BackingField", commander);
                typeof(ScoutPost).GetMethod("Resolve", Hidden).Invoke(scout, null);
                Check(scout.ConsecutiveFailures == failure + 1 && second.ConsecutiveFailures == failure + 1, "G36 actual failure shared across posts");
            }
            var saved = JsonUtility.FromJson<CampaignHistory.State>(JsonUtility.ToJson(history.Capture()));
            Check(CampaignHistory.Validate(saved), "G36 state validates"); history.Restore(saved);
            Check(scout.ConsecutiveFailures == 3 && scout.CurrentChance == 1f, "G36 fourth attempt guaranteed after state restore");
            var before = CommanderRoster.Instance.Count;
            Set(scout, "<Companion>k__BackingField", commander);
            typeof(ScoutPost).GetMethod("Resolve", Hidden).Invoke(scout, null);
            Check(CommanderRoster.Instance.Count == before + 1 && scout.SuccessCount == 1 && scout.ConsecutiveFailures == 0,
                "G36 fourth actual recruitment and next slot reset");
            history.Data.scoutRecruitSlot = before; history.Data.scoutRecruitFailures = 2;
            Check(second.ConsecutiveFailures == 0, "G36 changed empty slot clears old failures");
            return "IMPL-B1 " + count + " checks passed";
        }
        finally
        {
            foreach (var go in new[] { postObject, scoutObject, secondScoutObject, barracksObject }) if (go != null) Object.DestroyImmediate(go);
            UnityEngine.Random.state = random;
            SaveStorage.RootOverride = root; UserSettings.Apply(settings, false);
        }
    }
}
