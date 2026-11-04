using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Res = AntColony.Data.ResourceType;

// G27: 최소 API 연결 검사. 비용·시설·후반 연구는 fixture로 준비하며 전체 플레이 완주 판정이 아니다.
public static class ImplG27Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<GameObject> fixtures = new List<GameObject>();
    static readonly StringBuilder results = new StringBuilder();
    static readonly string[] labels = { "new crash start", "personal supplies usable", "hand gathering and wreck salvage", "basic living placement",
        "fixture housing immigration and five troops", "funded walking expedition dispatch and return", "first boss available and damageable",
        "simulated A reward import", "B detector crafted and used", "B/C convergence and simulated D import", "science and sample research",
        "funded late-research rocket construction", "fuel, preparation, boarding and launch request", "save/load headcount and identities" };
    static int headcount, passed, failed;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); }
    static void Result(int stage, Exception error)
    {
        results.AppendLine($"C{stage:00}[{headcount}] {(error == null ? "PASS " + labels[stage - 1] : "FAIL " + error.GetBaseException().Message.Replace('\n', ' ').Replace('\r', ' '))}");
        if (error == null) passed++; else failed++;
    }
    static void Stage(int stage, Action action)
    {
        try { action(); Result(stage, null); } catch (Exception error) { Result(stage, error); }
    }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene load timed out"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "missing template " + kind);
        var go = Object.Instantiate(template, position, Quaternion.identity); fixtures.Add(go); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static void Cleanup()
    {
        foreach (var go in fixtures) if (go != null) Object.DestroyImmediate(go);
        fixtures.Clear();
    }
    static void Reward(BossProgress progress, BossId boss)
    {
        Check(progress.RecordKill(boss, GameCalendar.GameSeconds), "record simulated kill " + boss);
        Check(progress.TryAcquire(boss, "g27:fixture", out var parcel), "acquire " + boss);
        Check(progress.TryDeliver(parcel.id) && !progress.TryDeliver(parcel.id), "deliver once " + boss);
    }
    static void Detector(BossProgress progress, BossId boss)
    {
        Check(progress.TryStartCraft(boss, false, ResourceManager.Instance), "craft detector " + boss);
        progress.TickCraft(60);
        Check(progress.UseDetector(boss) && progress.Revealed(boss), "reveal " + boss);
    }

    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play mode required");
        passed = failed = 0; results.Clear(); fixtures.Clear();
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var random = UnityEngine.Random.state;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "G27-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (var n in new[] { 1, 3 })
            {
                headcount = n; Cleanup();
                try
                {
                    await Ready();
                    var team = Enumerable.Range(0, n).Select(StartingCandidate.Roll).ToArray();
                    foreach (var candidate in team) candidate.traits.values.Clear();
                    CrashLandingStart.Begin(new NewGameOptions { startingCount = n, startingTeam = team,
                        seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest });
                    await Ready();
                }
                catch (Exception error)
                {
                    for (var stage = 1; stage <= 14; stage++) Result(stage, new Exception("start unavailable: " + error.GetBaseException().Message));
                    continue;
                }
                var commanders = CommanderRoster.Instance.Commanders.ToArray();
                var c = commanders.FirstOrDefault();
                Stage(1, () =>
                {
                    Check(commanders.Length == n && commanders.All(x => x.IsColonyMember && !x.IsDead && x.TroopCount == 0), "N living commanders, no troops");
                    Check(ColonyPopulation.Instance.Total == 0, "no initial citizens");
                });
                foreach (var commander in commanders) { commander.CommandStop(); commander.SetJobEnabled(CommanderJobs.All, false); }
                Stage(2, () =>
                {
                    Check(commanders.All(x => x.CarriedRationMeals == 14), "fourteen meals per survivor");
                    var before = c.PersonalState.carriedRations.Total; c.PersonalState.meal.satiety = 20;
                    c.TickDuty(.1f); Check(c.IsEating && c.PersonalState.carriedRations.Total == before - c.RationPortion, "eat carried supplies without storage");
                    c.TickDuty(GameBalance.EatSeconds * 2); Check(c.Satiety > 90, "meal completes");
                });
                Stage(3, () =>
                {
                    var stone = ResourceNode.Available.First(x => x.IsLooseCargo && x.ResourceType == Res.Stone
                        && AntColony.Map.HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(x.transform.position.x, 0, x.transform.position.z)));
                    var before = stone.AmountRemaining;
                    Check(before >= 20 && stone.CanGatherWith(c) && stone.ExtractFor(c, 1) == 1 && stone.AmountRemaining == before - 1, "extract starting stone by hand");
                    var wreck = Object.FindObjectsByType<CrashWreck>(FindObjectsSortMode.None).Single();
                    var wood = ResourceNode.Available.Where(x => x.IsLooseCargo && x.ResourceType == Res.Wood).Sum(x => x.AmountRemaining);
                    wreck.Salvage();
                    Check(ResourceNode.Available.Where(x => x.IsLooseCargo && x.ResourceType == Res.Wood).Sum(x => x.AmountRemaining) == wood + 8, "salvage produces wood");
                });
                Stage(4, () =>
                {
                    var placement = Object.FindFirstObjectByType<BuildingPlacementController>();
                    try
                    {
                        foreach (var kind in new[] { BuildingKind.SleepingMat, BuildingKind.Toilet, BuildingKind.Washbasin, BuildingKind.Campfire })
                            Check(placement.BeginPlacement(kind, UnitRole.Worker, c), "basic living placement " + kind);
                    }
                    finally { placement.CancelPlacement(); }
                });
                // 이후 비용 검사는 새 시작 재고와 구별되는 fixture 자원을 사용한다.
                Stage(5, () =>
                {
                    foreach (var type in new[] { Res.Food, Res.Soil, Res.Special, Res.Wood, Res.Stone, AirshipYard.FuelResource })
                    { ResourceManager.Instance.AddCapacity(type, 10000); ResourceManager.Instance.Add(type, 1000, ResourceReason.Production); }
                    Build<Housing>(BuildingKind.Hut, c.Position + Vector3.forward * 12);
                    var pop = ColonyPopulation.Instance;
                    for (var i = 0; i < 50 && ColonyPopulation.Adults < 10; i++) pop.Immigrate();
                    Check(pop.Total > 0 && ColonyPopulation.Adults >= 10, "housing admits citizens (immigration ticks advanced)");
                    var post = Build<ConscriptionPost>(BuildingKind.ConscriptionPost, c.Position);
                    Check(post.TryDeploy(new[] { c }, new[] { 5 }) && c.TroopCount == 5, "conscription produces five troops");
                    Check(c.ReturnTroops(5) == 5, "return troops before walking expedition");
                    typeof(CommanderAnt).GetMethod("FinishReturn", Hidden).Invoke(c, null);
                });
                Stage(6, () =>
                {
                    var world = WorldMapManager.Instance;
                    var party = world.CreateWalkingParty(new[] { c }); Check(party != null && party.Walking, "walking party without vehicle");
                    Check(party.TryLoadFood(20), "load fixture supplies");
                    var site = world.Sites.First(x => x.Kind == ExpeditionSiteKind.ResourceSite);
                    Check(party.TryDepart(site) && party.State == ExpeditionState.Outbound, "dispatch expedition");
                    party.Tick(party.TravelSeconds); Check(party.State == ExpeditionState.Deployed, "arrive");
                    Check(party.TryReturn(), "return"); party.Tick(party.TravelSeconds);
                    Check(party.State == ExpeditionState.Home && c.Transport == null, "crew reaches home");
                });
                var research = CampaignResearch.Instance;
                Stage(7, () =>
                {
                    Check(research.BossProgress.Revealed(BossId.A) && c.AttackDamage >= 1, "first boss revealed and solo attack available");
                    AntColony.Boss.BossHealth boss = WorldMapManager.Instance.Sites.First(x => x.Kind == ExpeditionSiteKind.BossNest).Boss;
                    Check(boss != null && !boss.IsDead, "first boss spawned");
                    var hp = boss.CurrentHp; boss.TakeDamage(1); Check(boss.CurrentHp < hp, "boss accepts damage"); boss.Heal(1);
                });
                Stage(8, () =>
                {
                    Reward(research.BossProgress, BossId.A);
                    Check(research.BossProgress.Home(BossId.A) == 2 && research.BossProgress.Recipe(BossId.B) && research.BossProgress.Recipe(BossId.C), "simulated A reward imported; both recipes open");
                });
                Stage(9, () => Detector(research.BossProgress, BossId.B));
                Stage(10, () =>
                {
                    Detector(research.BossProgress, BossId.C);
                    Reward(research.BossProgress, BossId.B);
                    Check(!research.BossProgress.Recipe(BossId.D), "one branch insufficient");
                    Reward(research.BossProgress, BossId.C); Detector(research.BossProgress, BossId.D);
                    Reward(research.BossProgress, BossId.D);
                    Check(research.HasBlueprint, "both branches converge; D import unlocks rocket blueprint");
                });
                Stage(11, () =>
                {
                    c.CommandStop(); c.SetJobPriority(CommanderJobs.Research, 5);
                    var lab = Build<ScienceLab>(BuildingKind.ScienceLab, c.Position);
                    try
                    {
                        Check(lab.TryAssign(c) && research.TryStart(ScienceTechnology.Fishing), "first research starts with one researcher");
                        research.Tick(10000); Check(research.Has(ScienceTechnology.Fishing), "research completes");
                        research.RecordSampleDelivery(Res.IronOre, 1);
                        Check(research.TryStartResourceResearch(Res.IronOre), "delivered sample starts research");
                        for (var i = 0; i < 2000 && research.ActiveResource != null; i++) research.Tick(5);
                        Check(research.ResourceResearched(Res.IronOre), "sample research completes");
                    }
                    finally { lab.ReleaseResearcher(); c.CommandStop(); }
                });
                AirshipYard yard = null;
                Stage(12, () =>
                {
                    Check(research.HasBlueprint, "D blueprint required before late research fixture");
                    var state = research.CaptureState(); state.completed = Enum.GetValues(typeof(ScienceTechnology)).Cast<ScienceTechnology>().Select(x => (int)x).ToList();
                    state.active = -1; state.progress = 0; research.RestoreState(state);
                    yard = Build<AirshipYard>(BuildingKind.AirshipYard, c.Position);
                    Check(yard.TryBuild(AirshipPart.Hull) && yard.Remaining > 0, "rocket hull construction starts with funded late research fixture");
                    yard.Tick(300); Check(yard.Hull, "hull completes");
                    Check(yard.TryBuild(AirshipPart.Engine), "engine construction starts"); yard.Tick(300); Check(yard.Ready, "rocket construction completes");
                });
                Stage(13, () =>
                {
                    Check(yard != null && yard.Ready, "constructed rocket required");
                    RocketCampaign.Instance.Economy.Observe(GameCalendar.GameSeconds, 200, ColonyPopulation.Instance.Total);
                    Check(yard.TryLoadFuel(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay)
                        && yard.TryActivate(200, ColonyPopulation.Instance.Total), "fuel and activation accepted");
                    yard.Tick(AirshipYard.PreparationSeconds);
                    Check(yard.Phase == RocketPhase.Boarding && yard.TryBoard(c) && yard.TryDepart(), "preparation completes; survivor boards and launch is queued");
                    yard.Unload();
                });
                // 발사 확정·승리 없이 저장 최소 전제를 검사한다.
                if (yard != null) Object.DestroyImmediate(yard.gameObject);
                try
                {
                    var ids = CommanderRoster.Instance.Commanders.Select(x => x.PersonalState.id).OrderBy(x => x).ToArray();
                    var citizens = ColonyPopulation.Instance.Total;
                    Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
                    Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
                    Check(CommanderRoster.Instance.Count == n && CommanderRoster.Instance.Commanders.All(x => !x.IsDead)
                        && ids.SequenceEqual(CommanderRoster.Instance.Commanders.Select(x => x.PersonalState.id).OrderBy(x => x))
                        && ColonyPopulation.Instance.Total == citizens && GameSession.Instance.Options.startingCount == n, "roundtrip keeps survivor identities, headcount, citizens and starting count");
                    Result(14, null);
                }
                catch (Exception error) { Result(14, error); }
            }
            return results + $"{(failed == 0 ? "PASS" : "FAIL")} ImplG27Checks {passed}/28 stages, {failed} failed";
        }
        finally { Cleanup(); SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Random.state = random; Time.timeScale = 0; }
    }
}
