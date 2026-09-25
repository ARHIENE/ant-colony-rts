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
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Resource = AntColony.Data.ResourceType;

// Fresh Play: unity command run_script --file AgentScripts/Stage4Checks.cs --entry Stage4Checks.Main --timeout_ms 180000 --timeout 190
public static class Stage4Checks
{
    static int checks;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) < .01f, label + " actual=" + a);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void SeasonAt(Season s) => GameSession.Instance.MarkStarted(0, (int)s * 3 * GameCalendar.SecondsPerMonth);
    static void ResetEvents() => ColonyEvents.Instance.Restore(new ColonyEvents.State());
    static void Grant(params ScienceTechnology[] techs)
    {
        var s = CampaignResearch.Instance.CaptureState();
        foreach (var t in techs) if (!s.completed.Contains((int)t)) s.completed.Add((int)t);
        CampaignResearch.Instance.RestoreState(s);
    }
    static T Build<T>(BuildingKind kind, Vector3 p) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template, p, Quaternion.identity); go.name = "Stage4 " + kind; go.SetActive(true); return go.GetComponent<T>();
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop(); Check(NavMesh.SamplePosition(p, out var hit, 15, NavMesh.AllAreas), "walkable position");
        Check(c.Agent.Warp(hit.position), "warp");
    }
    static SaveFileV1 Copy(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage4-" + Guid.NewGuid().ToString("N"));
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 250928, mapSize = MapSize.Small, commanderDeath = CommanderDeathMode.Gentle }); await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false; Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            var events = ColonyEvents.Instance; var rm = ResourceManager.Instance; var pool = AntPool.Instance;
            Check(events != null && CampaignHistory.Instance != null, "runtime systems booted");
            foreach (Resource r in Enum.GetValues(typeof(Resource))) { rm.AddCapacity(r, 10000); rm.Add(r, 9000); }
            CampaignHistory.Instance.Restore(new CampaignHistory.State());
            var h = CampaignHistory.Instance.Data;
            var room = rm.GetCapacity(Resource.Food) - rm.GetAmount(Resource.Food);
            rm.Add(Resource.Food, room + 500);
            Check(h.acquired[0] == room, "only deposited amount counted");
            rm.Add(Resource.Food, 10); Check(h.acquired[0] == room, "overflow not counted");
            Check(!rm.TrySpend(-1, 0) && !rm.TrySpend(int.MaxValue, 0), "invalid and unaffordable spend rejected");
            Check(h.spent.Sum() == 0, "failed spend not recorded");
            Check(rm.TrySpend(7, 8, 9, ResourceReason.Crafting), "spend succeeds");
            Check(h.spent.SequenceEqual(new long[] { 7, 8, 9 }) && h.spentByReason[(int)ResourceReason.Crafting] == 7, "totals and use category recorded");
            var acquired = h.acquired[0]; var spent = h.spent[0];
            typeof(SaveSystem).GetProperty("Busy").SetValue(null, true);
            rm.TrySpend(1, 0); rm.Add(Resource.Food, 1);
            typeof(SaveSystem).GetProperty("Busy").SetValue(null, false);
            Check(h.acquired[0] == acquired && h.spent[0] == spent, "restoration does not double count");
            pool.Breed(10); pool.TryAssign(2); pool.ReturnAssigned(2);
            Check(h.antsProduced == 10 && h.antsLost == 0, "assignment and return not production");
            pool.TryAssign(2); pool.LoseAssigned(2); pool.StarveOne(); Check(h.antsLost == 3, "combat and starvation losses counted");
            for (int i = 0; i < 25; i++) CampaignHistory.Record("검사", "사건 " + i, "결과");
            Check(h.recent.Count == 20 && h.recent[0].name == "사건 5" && h.milestones.Count == 25, "20 recent entries retain cumulative history");
            Check(CampaignHistory.Validate(h), "history valid");
            Check(EventRules.Chance(DifficultyLevel.Gentle) == .5f && EventRules.Chance(DifficultyLevel.Normal) == .6f && EventRules.Chance(DifficultyLevel.Harsh) == 1, "difficulty frequency");
            Check(EventRules.CrisisChance(DifficultyLevel.Gentle) == .4f && EventRules.CrisisChance(DifficultyLevel.Normal) == .6f && EventRules.CrisisChance(DifficultyLevel.Harsh) == .75f, "difficulty crisis ratio");
            foreach (ColonyEvent e in Enum.GetValues(typeof(ColonyEvent)))
            {
                if (e == ColonyEvent.Caravan) Check(!EventRules.InSeason(e, Season.Spring), "caravan deferred until diplomacy");
                else Check(Enum.GetValues(typeof(Season)).Cast<Season>().Any(s => EventRules.InSeason(e, s)), "event has eligible season " + e);
            }
            SeasonAt(Season.Spring); ResetEvents();
            Check(!events.TryTrigger(ColonyEvent.Cold) && !events.TryTrigger(ColonyEvent.Caravan), "season gate and caravan disabled");
            var before = pool.Total;
            Check(events.TryTrigger(ColonyEvent.Migration) && pool.Total == before + 10, "migration grants ten ants");
            Check(!events.TryTrigger(ColonyEvent.Mold), "minimum event gap enforced");
            events.Tick(60); Check(!events.TryTrigger(ColonyEvent.Migration), "same event cooldown enforced");
            Near(events.Capture().cooldowns[(int)ColonyEvent.Migration], 540, "cooldown counts game seconds");
            var snapshot = events.Capture(); events.Tick(0); Near(events.Capture().checkRemaining, snapshot.checkRemaining, "pause preserves scheduler");

            var roster = CommanderRoster.Instance; var c = roster.Commanders[0];
            foreach (var commander in roster.Commanders) commander.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));
            SeasonAt(Season.Winter); ResetEvents(); Check(events.TryTrigger(ColonyEvent.Cold), "winter cold triggers");
            Near(ColonyEvents.MoveMultiplier(c), .75f, "cold movement"); Near(ColonyEvents.GatherMultiplier(c), .8f, "cold gathering");
            var speed = (float)typeof(CommanderAnt).GetProperty("MovementSpeed", Any).GetValue(c);
            Grant(ScienceTechnology.Insulation); Near(ColonyEvents.MoveMultiplier(c), .875f, "insulation halves movement penalty"); Near(ColonyEvents.GatherMultiplier(c), .9f, "insulation halves gathering penalty");
            Check((float)typeof(CommanderAnt).GetProperty("MovementSpeed", Any).GetValue(c) > speed, "actual movement stat uses cold");
            events.Tick(90); Near(ColonyEvents.MoveMultiplier(c), 1, "cold expires");

            var home = WorldMapManager.Instance.HomePosition;
            var farm = Build<BuildingBase>(BuildingKind.Farm, home + Vector3.left * 15);
            var node = farm.GetComponent<ResourceNode>();
            var plot = farm.GetComponent<FarmPlot>() ?? farm.gameObject.AddComponent<FarmPlot>(); plot.Configure(FarmCrop.Fungus, false);
            node.Extract(float.MaxValue); node.TickGrowth(10000); Check(node.AmountRemaining > 0, "farm harvest grows");
            var yield = node.AmountRemaining;
            SeasonAt(Season.Autumn); ResetEvents(); Check(events.TryTrigger(ColonyEvent.Harvest), "autumn harvest event"); Near(node.AmountRemaining, yield * 1.5f, "ready crop receives bonus");
            node.GrantBountifulHarvest(); Near(node.AmountRemaining, yield * 1.5f, "harvest bonus cannot stack");
            node.Extract(float.MaxValue); Check(!node.BountifulHarvest, "bonus consumed once"); node.TickGrowth(10000); Near(node.AmountRemaining, yield, "following harvest normal");
            node.Extract(float.MaxValue); node.GrantBountifulHarvest(); node.TickGrowth(10000); Near(node.AmountRemaining, yield * 1.5f, "growing crop receives next-harvest bonus");

            SeasonAt(Season.Summer); ResetEvents(); Check(events.TryTrigger(ColonyEvent.Drought), "summer drought triggers");
            node.Extract(float.MaxValue); var growth = node.RegrowTimeRemaining; node.TickGrowth(10); Near(node.RegrowTimeRemaining, growth - 5, "drought halves actual growth");
            plot.Configure(FarmCrop.Honeydew, false); Near(ColonyEvents.GrowthMultiplier(node), .8f, "honeydew drought resistance");
            plot.Configure(FarmCrop.Fungus, false); Grant(ScienceTechnology.Drainage); Near(ColonyEvents.GrowthMultiplier(node), .8f, "drainage drought resistance");
            var tech = CampaignResearch.Instance.CaptureState(); tech.completed.Remove((int)ScienceTechnology.Drainage); CampaignResearch.Instance.RestoreState(tech);
            var waterObject = new GameObject("Stage4 Fishing"); waterObject.transform.position = farm.Position + Vector3.forward * 2;
            var fishing = waterObject.AddComponent<ResourceNode>(); typeof(ResourceNode).GetField("requiresFishing", Any).SetValue(fishing, true);
            SeasonAt(Season.Spring); ResetEvents(); Check(events.TryTrigger(ColonyEvent.Flood), "spring flood triggers");
            Check(ColonyEvents.Flooded(node) && !node.CanGather, "near-shore crop blocked");
            growth = node.RegrowTimeRemaining; node.TickGrowth(10); Near(node.RegrowTimeRemaining, growth, "flood suspends growth");
            Grant(ScienceTechnology.Drainage); Check(!ColonyEvents.Flooded(node), "drainage grants flood immunity");
            Object.Destroy(waterObject); ResetEvents();

            var farm2 = Build<BuildingBase>(BuildingKind.Farm, farm.Position + Vector3.forward * 6); var node2 = farm2.GetComponent<ResourceNode>();
            node.TickGrowth(10000); node2.TickGrowth(10000);
            ColonyEvents.Burn(node); Check(node.IsDepleted && node2.IsDepleted, "fire destroys crop and spreads to neighbor");
            node.TickGrowth(10000); node2.TickGrowth(10000);
            var wall = Build<SoilWall>(BuildingKind.SoilWall, farm.Position + Vector3.forward * 3); Physics.SyncTransforms();
            ColonyEvents.Burn(node); Check(node.IsDepleted && !node2.IsDepleted, "soil wall blocks fire spread");
            wall.gameObject.SetActive(false); Object.Destroy(wall.gameObject);
            node.TickGrowth(10000); yield = node.AmountRemaining; var otherYield = node2.AmountRemaining;
            Grant(ScienceTechnology.Firebreaks); ColonyEvents.Burn(node); Near(node.AmountRemaining, yield * .5f, "firebreaks halves crop loss"); Near(node2.AmountRemaining, otherYield, "firebreaks stops spread");
            SeasonAt(Season.Autumn); ResetEvents(); Check(events.TryTrigger(ColonyEvent.Wildfire), "autumn wildfire event triggers");

            ResetEvents(); SeasonAt(Season.Spring); Check(events.TryTrigger(ColonyEvent.Mold), "mold event infects commander");
            Check(roster.Commanders.Count(x => x.PersonalState.infected) == 1, "single initial infection");
            foreach (var commander in roster.Commanders) { commander.PersonalState.infected = false; commander.PersonalState.moldLoss = commander.PersonalState.moldSpread = 0; }
            c.PersonalState.injuries.Clear(); c.TryAssign(4); var troops = c.TroopCount; ColonyEvents.Infect(c); c.TickPersonal(20);
            Check(c.TroopCount == troops - 1, "infection loses one troop per 20 seconds");
            var infirmary = Build<Infirmary>(BuildingKind.Infirmary, home + Vector3.right * 12); Move(c, infirmary.Position + Vector3.right * 3);
            Grant(ScienceTechnology.Infirmary); Check(infirmary.TryAdmit(c), "infected commander admitted without injury");
            var loyalty = c.Traits.Loyalty; c.TickPersonal(29); Check(c.PersonalState.infected, "base treatment not done early");
            infirmary.Release(c); var treatment = c.PersonalState.moldTreatment; c.TickPersonal(1); Near(c.PersonalState.moldTreatment, treatment, "interrupted infection treatment preserved");
            Grant(ScienceTechnology.Sanitation); Near(ScienceEffects.MoldSpreadMultiplier, .5f, "sanitation halves transmission");
            Check(infirmary.TryAdmit(c), "resume infection treatment"); c.TickPersonal(16);
            Check(!c.PersonalState.infected && c.TreatmentFacility == null && c.Traits.Loyalty == loyalty + 3, "sanitation completes remaining treatment and releases patient");
            var other = roster.Commanders[1]; Move(other, c.Position + Vector3.right);
            // 같은 난수에서 25%는 전파, 12.5%는 차단되는 경계를 실제 감염 처리로 확인한다.
            int spreadSeed = 0;
            for (; spreadSeed < 10000; spreadSeed++) { Random.InitState(spreadSeed); var roll = Random.value; if (roll > .125f && roll < .25f) break; }
            Check(spreadSeed < 10000, "transmission boundary seed");
            foreach (var commander in roster.Commanders) commander.PersonalState.infected = true;
            other.PersonalState.infected = false; c.PersonalState.moldLoss = 0; c.PersonalState.moldSpread = 29;
            Random.InitState(spreadSeed); c.TickPersonal(1); Check(!other.PersonalState.infected, "sanitation prevents boundary transmission");
            tech = CampaignResearch.Instance.CaptureState(); tech.completed.Remove((int)ScienceTechnology.Sanitation); CampaignResearch.Instance.RestoreState(tech);
            c.PersonalState.moldSpread = 29; Random.InitState(spreadSeed); c.TickPersonal(1); Check(other.PersonalState.infected, "infection actually spreads within eight meters");
            foreach (var commander in roster.Commanders) { commander.PersonalState.infected = false; commander.PersonalState.moldLoss = commander.PersonalState.moldSpread = commander.PersonalState.moldTreatment = 0; }
            Grant(ScienceTechnology.Sanitation);

            ResetEvents(); var queen = Object.FindFirstObjectByType<QueenChamber>(); Check(queen.TryProduceWorker(), "production begins before wasps");
            Check(events.TryTrigger(ColonyEvent.Wasps), "wasp event spawns");
            var wasps = Object.FindObjectsByType<EventActor>().Where(a => a.Kind == EventActorKind.Wasp).ToArray();
            Check(wasps.Length == 3 && wasps.All(a => a.GetComponent<WildMonster>().IsFlying), "three airborne attackers");
            var remaining = (float)typeof(QueenChamber).GetProperty("ProductionRemaining", Any).GetValue(queen);
            queen.Tick(5); Near((float)typeof(QueenChamber).GetProperty("ProductionRemaining", Any).GetValue(queen), remaining, "wasps pause in-progress production");
            Check(!queen.TryProduceWorker(), "wasps block new production");
            var attacker = wasps[0].GetComponent<WildMonster>(); attacker.transform.position = queen.Position + Vector3.up;
            var queenHp = queen.CurrentHealth; typeof(WildMonster).GetMethod("Update", Any).Invoke(attacker, null);
            Check(queen.CurrentHealth < queenHp, "wasp actually attacks the queen");
            foreach (var wasp in wasps) wasp.GetComponent<WildMonster>().TakeDamage(100);
            Check(!ColonyEvents.ProductionBlocked, "last wasp death resumes production"); var ants = pool.Total; queen.Tick(100); Check(pool.Total == ants + 1, "paused production finishes once");
            ResetEvents(); Check(events.TryTrigger(ColonyEvent.Wanderer), "wanderer spawns at edge");
            var wanderer = Object.FindObjectsByType<EventActor>().Single(a => a.Kind == EventActorKind.Wanderer);
            var count = roster.Count; wanderer.Tick(60); Check(!wanderer.gameObject.activeSelf && roster.Count == count, "wanderer expires without recruitment");
            ResetEvents(); Check(events.TryTrigger(ColonyEvent.Wanderer), "second wanderer fixture");
            wanderer = Object.FindObjectsByType<EventActor>().Single(a => a.Kind == EventActorKind.Wanderer);
            Move(other, wanderer.transform.position); count = roster.Count;
            int recruitSeed = 0; for (; recruitSeed < 10000; recruitSeed++) { Random.InitState(recruitSeed); if (Random.value < .2f) break; }
            Random.InitState(recruitSeed); wanderer.Tick(.1f);
            Check(roster.Count == count + 1 && CampaignHistory.Instance.Data.milestones.Last(e => e.kind == "합류").result == "방랑 장수 영입", "approach recruits and records origin");
            ResetEvents(); Check(events.TryTrigger(ColonyEvent.Driftwood), "driftwood spawns");
            var drift = Object.FindObjectsByType<EventActor>().Where(a => a.Kind == EventActorKind.Food || a.Kind == EventActorKind.Soil).ToArray();
            Check(drift.Length == 2 && drift.Sum(a => a.GetComponent<ResourceNode>().AmountRemaining) == 120, "driftwood is gatherable resources, not instant deposit");
            foreach (var a in drift) a.Tick(300); Check(drift.All(a => !a.gameObject.activeSelf), "driftwood expires after five minutes");

            // Save active hazards, partial actor health, bonus crop and admitted infection together.
            ResetEvents(); ColonyEvents.Infect(c); Check(infirmary.TryAdmit(c), "prepare infected saved patient"); c.TickPersonal(5);
            node.GrantBountifulHarvest();
            var savedEvents = new ColonyEvents.State { cold = 41, flood = 37, drought = 81, checkRemaining = 77, sinceLast = 23 };
            savedEvents.cooldowns[(int)ColonyEvent.Mold] = 433;
            savedEvents.actors.Add(new EventActor.State { kind = EventActorKind.Wasp, position = new Vec3Dto(home + Vector3.forward * 50 + Vector3.up * 2), amount = 7, attackCooldown = .6f });
            savedEvents.actors.Add(new EventActor.State { kind = EventActorKind.Wanderer, position = new Vec3Dto(home + Vector3.back * 50), remaining = 21 });
            savedEvents.actors.Add(new EventActor.State { kind = EventActorKind.Food, position = new Vec3Dto(home + Vector3.right * 30), remaining = 130, amount = 43 });
            events.Restore(savedEvents);
            foreach (var commander in roster.Commanders) commander.CommandStop();
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "v5 snapshot validates: " + error);
            var bad = Copy(file); bad.events.cooldowns[0] = float.NaN; Check(!SaveValidator.Validate(bad, out error), "NaN event timer rejected");
            bad = Copy(file); bad.history.spent[0] = -1; Check(!SaveValidator.Validate(bad, out error), "negative history rejected");
            bad = Copy(file); bad.events.actors[0].kind = (EventActorKind)99; Check(!SaveValidator.Validate(bad, out error), "unknown event actor rejected");
            var legacy = Copy(file); legacy.version = 4; legacy.history = null; legacy.events = null;
            // v4 had no infection-only patient, so clear that v5-only assignment in the legacy fixture.
            foreach (var b in legacy.buildings) b.patients.Clear();
            foreach (var d in legacy.commanders) d.personalState.treating = false;
            Check(SaveValidator.Validate(legacy, out error) && legacy.version == SaveFileV1.CurrentVersion && legacy.events.actors.Count == 0 && legacy.history.spent.Sum() == 0, "v4 migration initializes empty history: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save events: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load events: " + error); await Ready();
            events = ColonyEvents.Instance; var loaded = events.Capture();
            Near(loaded.cold, 41, "cold time restored"); Near(loaded.flood, 37, "flood time restored"); Near(loaded.drought, 81, "drought time restored"); Near(loaded.checkRemaining, 77, "scheduler restored");
            Check(loaded.actors.Count == 3 && loaded.actors.Any(a => a.kind == EventActorKind.Wasp && a.amount == 7), "event actors restored once with damage");
            Check(loaded.actors.Any(a => a.kind == EventActorKind.Food && a.amount == 43 && a.remaining == 130), "partial loot and expiry restored");
            Check(CampaignHistory.Instance.Data.spent.SequenceEqual(file.history.spent) && CampaignHistory.Instance.Data.milestones.Count == file.history.milestones.Count, "history restored without artificial records");
            c = CommanderRoster.Instance.Commanders[0]; Check(c.PersonalState.infected && c.PersonalState.moldTreatment == 10 && c.TreatmentFacility != null, "infection and patient ownership restored");
            Check(Object.FindObjectsByType<ResourceNode>().Any(n => n.BountifulHarvest), "next harvest bonus restored");
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "second save/load: " + error); await Ready();
            Check(Object.FindObjectsByType<EventActor>().Length == 3, "second reload does not duplicate actors");
            GameMenuController.Instance.EventLog(); Canvas.ForceUpdateCanvases();
            Check(GameMenuController.Instance.ScreenName == "이벤트 로그", "event log opens");
            var scroll = Object.FindObjectsByType<UnityEngine.UI.ScrollRect>().First(s => s.isActiveAndEnabled);
            Check(scroll.viewport != null && scroll.content.rect.height > scroll.viewport.rect.height && scroll.viewport.rect.width > 0, "event log scroll layout usable");
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("누적 기록")), "statistics displayed");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            return "PASS " + checks + " stage 4 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
