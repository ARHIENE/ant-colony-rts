using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplC3Checks
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int checks;
    private static void Check(bool ok, string message)
    { if (!ok) throw new Exception("C3 FAIL " + checks + ": " + message); checks++; }
    private static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, Hidden).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Hidden).GetValue(target);
    private static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("C3 load timeout");
        Time.timeScale = 0;
    }
    // Claude의 기존 RunChecks 로더용. 검사 전용 새 불시착 게임을 시작한다.
    public static async Task<string> Main()
    {
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false;
        UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            var candidate = StartingCandidate.Roll(0); candidate.traits.values.Clear();
            CrashLandingStart.Begin(new NewGameOptions { startingCount = 1, seed = 261024,
                mapSize = MapSize.Small, biome = MapBiome.Forest, commanderDeath = CommanderDeathMode.Gentle, startingTeam = new[] { candidate } });
            await Ready();
            return Run();
        }
        finally { UserSettings.Apply(settings, false); }
    }

    public static string Run()
    {
        checks = 0;
        Check(Application.isPlaying && !SaveSystem.Busy, "Play mode after crash landing load");
        var commanders = CommanderRoster.Instance.Commanders.ToArray();
        Check(commanders.Length >= 1 && commanders.Length <= 2, "one or two crash survivors; no stockpile dependency");
        var campaign = RocketCampaign.Instance;
        var existingEvents = ColonyEvents.Instance;
        var savedEvents = existingEvents?.Capture();
        var population = ColonyPopulation.Instance;
        var pool = AntPool.Instance;
        Check(campaign != null && population != null && pool != null, "crash services ready");
        var savedCampaign = campaign.Capture(); var savedPopulation = population.Capture();
        var personals = commanders.Select(c => c.CapturePersonalState()).ToArray();
        var free = pool.Free; var assigned = pool.Assigned; var reserved = pool.Reserved;
        var pause = SimulationPause.Capture(); var random = UnityEngine.Random.state;
        var settings = UserSettings.Current.Clone(); var timeScale = Time.timeScale;
        var gameSeconds = GameSession.Instance.GameSeconds; var playSeconds = GameSession.Instance.PlaySeconds;
        var game = GameManager.Instance;
        var defeated = Get<bool>(game, "defeated");
        var reason = game.DefeatReason;
        var made = new List<GameObject>();
        var sources = Object.FindObjectsByType<ColonyInvasion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var sourceTemplates = sources.Select(s => Get<WildMonster>(s, "raiderTemplate")).ToArray();
        var visitorsBefore = Object.FindAnyObjectByType<LocalIncursions>()?.Visitors.ToArray() ?? Array.Empty<WildMonster>();
        var corpsesBefore = Corpse.All.ToArray();
        Action<AirshipYard, RocketSnapshot, float> listener = null;
        GameObject New(string name) { var go = new GameObject("C3 " + name); made.Add(go); return go; }
        try
        {
            Time.timeScale = 0;
            var quiet = settings.Clone(); quiet.autoPauseRocket = false; quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            var c = commanders[0];
            var yard = New("rocket").AddComponent<AirshipYard>(); yard.transform.position = c.Position;
            var snapshot = new RocketSnapshot { taken = true, at = gameSeconds, currentPopulation = 20, currentWealth = 300 };
            var rocket = new AirshipYard.State { hull = true, engine = true, phase = RocketPhase.Preparing, // G52: 가동 상태는 15일분 연료 적재가 전제
                fuelType = (int)AirshipYard.FuelResource, fuelUnits = 15 * AirshipYard.FuelUnitsPerDay,
                preparationRemaining = AirshipYard.PreparationSeconds, snapshot = snapshot };
            yard.RestoreState(rocket, commanders);
            Check(RocketCampaign.SuppressNewCrises, "G39 preparing suppresses new crises");
            var events = existingEvents != null ? existingEvents : New("events").AddComponent<ColonyEvents>();
            events.Restore(new ColonyEvents.State { cold = 20, flood = 20, drought = 20 });
            for (int i = 0; i <= (int)ColonyEvent.Wasps; i++)
                Check(!events.Eligible((ColonyEvent)i) && !events.TryTrigger((ColonyEvent)i), "G39 crisis blocked " + i);
            Check(events.Eligible(ColonyEvent.Migration), "G39 non hostile migration allowed");
            events.Tick(7);
            Check(events.ColdRemaining == 13 && events.FloodRemaining == 13 && events.DroughtRemaining == 13,
                "G39 existing weather events continue");
            Check(events.Capture().checkRemaining > 0 && events.Capture().checkRemaining <= EventRules.CheckSeconds,
                "G39 lottery timer has no backlog");
            Check(!events.SpawnNightPredators(), "G39 new hostile actors suppressed");
            Check(events.Capture().lastNight == GameCalendar.Day, "G39 suppressed predator day consumed");
            var local = Object.FindAnyObjectByType<LocalIncursions>() ?? New("incursions").AddComponent<LocalIncursions>();
            var remaining = Get<float>(local, "remaining"); Call(local, "Update");
            Check(Get<float>(local, "remaining") == remaining && !local.TrySpawn() && !local.TrySpawnIndependent(2),
                "G39 local timer and direct spawns blocked");

            foreach (var source in sources) Set(source, "raiderTemplate", null);
            var sourceObject = New("invasion source"); sourceObject.SetActive(false);
            var sourceTest = sourceObject.AddComponent<ColonyInvasion>();
            var templateObject = New("raider template"); templateObject.SetActive(false);
            var template = templateObject.AddComponent<WildMonster>();
            Set(sourceTest, "raiderTemplate", template); Set(sourceTest, "spawnPoint", c.transform);
            Check(RocketCampaign.AssaultAvailable, "G20 real generator is available without event subscriber");
            var zero = new RocketSnapshot { taken = true };
            Check((int)Call(sourceTest, "WaveCount", 0, snapshot) >= (int)Call(sourceTest, "WaveCount", 0, zero),
                "G45 fixed wealth and population feed existing wave calculation");
            var observed = 0;
            listener = (y, s, dt) => { observed++; s.currentWealth = 999999; };
            RocketCampaign.AssaultTick += listener;
            RocketCampaign.TickAssault(yard, yard.FixedSnapshot, 1);
            Check(observed == 1 && yard.FixedSnapshot.Wealth == 300, "G45 callback cannot change frozen inputs");
            Check(campaign.Capture().assaultStarted && campaign.Capture().assaultRemaining > 0, "G20 first wave countdown started");
            Check(c.WorkState.holdDeployment, "G61 final assault holds home deployment");
            var due = campaign.Capture(); due.assaultRemaining = 0; campaign.Restore(due);
            RocketCampaign.TickAssault(yard, yard.FixedSnapshot, 1);
            Check(campaign.Capture().assaultWave == 1 && local.Visitors.Any(v => v != null && !visitorsBefore.Contains(v)),
                "G20 actual final wave uses existing NavMesh spawn and registers saved visitors");
            var captured = JsonUtility.FromJson<RocketCampaign.State>(JsonUtility.ToJson(campaign.Capture()));
            campaign.Restore(captured);
            Check(campaign.Capture().assaultRemaining == captured.assaultRemaining && RocketCampaign.Validate(captured),
                "G20 assault timer JSON roundtrip without version change");
            rocket.phase = RocketPhase.Boarding; rocket.preparationRemaining = 0; yard.RestoreState(rocket, commanders);
            RocketCampaign.TickAssault(yard, yard.FixedSnapshot, 1);
            Check(observed == 3, "G20 boarding continues assault");
            var incident = yard.gameObject.AddComponent<RocketIncident>(); Call(incident, "Update"); Call(incident, "Update");
            Check(SimulationPause.Capture().incidents.Count(x => x.Contains(":ready")) == 1, "G20 ready notification deduplicated");
            rocket.phase = RocketPhase.Destroyed; yard.RestoreState(rocket, commanders); campaign.RocketDestroyed();
            Check(!c.WorkState.holdDeployment, "G61 destruction clears deployment hold");
            Check(campaign.Capture().crisisResumeAt == gameSeconds + 3 * GameCalendar.SecondsPerDay, "G39 450 second grace");
            GameSession.Instance.MarkStarted(playSeconds, gameSeconds + 449);
            Check(RocketCampaign.SuppressNewCrises, "G39 grace before boundary");
            GameSession.Instance.MarkStarted(playSeconds, gameSeconds + 450);
            Check(!RocketCampaign.SuppressNewCrises, "G39 crises resume at boundary");
            RocketCampaign.TickAssault(yard, snapshot, 1);
            Check(observed == 3, "G20 destroyed rocket stops assault callbacks");

            rocket.phase = RocketPhase.Construction; rocket.preparationRemaining = 0; rocket.snapshot = null; yard.RestoreState(rocket, commanders); Call(incident, "Update");
            rocket.phase = RocketPhase.Preparing; rocket.preparationRemaining = AirshipYard.PreparationSeconds; rocket.snapshot = snapshot; yard.RestoreState(rocket, commanders); Call(incident, "Update"); Call(incident, "Update");
            Check(SimulationPause.Capture().incidents.Count(x => x.Contains(":activated")) == 1, "G20 activation notification once");
            rocket.phase = RocketPhase.Destroyed; yard.RestoreState(rocket, commanders); Call(incident, "Update"); Call(incident, "Update");
            Check(SimulationPause.Capture().incidents.Count(x => x.Contains(":destroyed")) == 1, "G20 destruction notification once");

            Call(pool, "RestoreCounts", 0, 0, 0);
            population.Restore(new ColonyPopulation.State { young = 3 });
            var position = c.Position + Vector3.right;
            population.S.civilianLocations.Add(new CivilianLocation { id = 1, count = 3, position = position });
            population.S.nextCivilianLocation = 2;
            var monster = New("civilian attacker").AddComponent<WildMonster>(); monster.transform.position = c.Position;
            var target = (AntColony.Core.IDamageable)Call(monster, "FindCivilianTarget");
            Check(target != null && target.Position == position, "G16 selects exposed civilian location");
            target.TakeDamage(9); Check(population.LocatedCivilians == 3, "G16 nine damage accumulates");
            target.TakeDamage(1); Check(population.LocatedCivilians == 2, "G16 ten damage kills one citizen");
            population.SetCivilianProtection(1, true);
            Check(target.IsDead && Call(monster, "FindCivilianTarget") == null, "G16 sheltered civilians not attackable");
            population.SetCivilianProtection(1, false);
            Check(Call(monster, "FindCivilianTarget") != null, "G16 losing protection exposes survivors without instant deaths");

            var home = New("housing").AddComponent<Housing>(); home.transform.position = position;
            population.S.civilianLocations[0].housing = true; population.SyncCivilianLocations();
            Check(population.S.civilianLocations[0].sheltered, "G16 surviving housing protects location");
            var before = population.LocatedCivilians;
            typeof(BuildingBase).GetField("currentHealth", Hidden).SetValue(home, 0f); population.SyncCivilianLocations();
            Check(!population.S.civilianLocations[0].sheltered && population.LocatedCivilians == before,
                "G16 destroyed housing only removes shelter");

            foreach (var commander in commanders)
            {
                commander.PersonalState.health = new HealthState { migrated = true };
                commander.WorkState.health = 0; commander.WorkState.unconsciousSeconds = GameCalendar.SecondsPerDay - 1;
                commander.PersonalState.meal.satiety = 100;
            }
            Set(game, "defeated", false); Call(game, "LateUpdate");
            Check(!Get<bool>(game, "defeated"), "G11 natural recovery route prevents defeat");

            foreach (var commander in commanders)
            {
                commander.PersonalState.health = new HealthState { migrated = true };
                commander.WorkState.health = 0; commander.WorkState.unconsciousSeconds = 0;
                commander.PersonalState.meal.satiety = 0;
            }
            Check(!CommanderAnt.HasRecoveryOrRescueRoute(commanders), "G11 all starving unconscious have no route");
            Set(game, "defeated", false); Call(game, "LateUpdate");
            Check(Get<bool>(game, "defeated") && game.DefeatReason.StartsWith("구조 불가"), "G11 LateUpdate passes rescue impossible reason");
            Check(CampaignOutcome.CaptureWarning.Contains("구출은 남은 아군 필요"), "G50 capture warning shared text");
            return "PASS " + checks;
        }
        finally
        {
            if (listener != null) RocketCampaign.AssaultTick -= listener;
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) Set(sources[i], "raiderTemplate", sourceTemplates[i]);
            var local = Object.FindAnyObjectByType<LocalIncursions>();
            if (local != null) foreach (var visitor in local.Visitors.ToArray())
                if (visitor != null && !visitorsBefore.Contains(visitor)) Object.DestroyImmediate(visitor.gameObject);
            foreach (var corpse in Corpse.All.ToArray()) if (corpse != null && !corpsesBefore.Contains(corpse)) Object.DestroyImmediate(corpse.gameObject);
            foreach (var go in made.AsEnumerable().Reverse()) if (go != null) Object.DestroyImmediate(go);
            for (int i = 0; i < commanders.Length; i++) commanders[i].RestorePersonalState(personals[i]);
            Call(pool, "RestoreCounts", free, assigned, reserved); population.Restore(savedPopulation); campaign.Restore(savedCampaign);
            if (existingEvents != null) existingEvents.Restore(savedEvents);
            Set(game, "defeated", defeated);
            typeof(GameManager).GetProperty("DefeatReason").GetSetMethod(true).Invoke(game, new object[] { reason });
            GameSession.Instance.MarkStarted(playSeconds, gameSeconds);
            SimulationPause.Restore(pause); UserSettings.Apply(settings, false); UnityEngine.Random.state = random; Time.timeScale = timeScale;
            ToastManager.SetCrisis("defeat", null);
        }
    }
}
