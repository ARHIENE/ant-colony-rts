using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

// Chunked, observable playthrough. Orders use the game's normal costs, work and combat.
// No resource grants, completed construction, enemy damage, or health overrides.
public static class EarlyFlow1113Probe
{
    const string Dir = "work/early-flow-1113/";
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] public class RestoreState { public UserSettingsData settings; public string root; public int resolution, addedResolution; public bool active; }
    [Serializable] public class Orders
    {
        public string label = "step";
        public float seconds = 60, speed = 3;
        public string[] build = new string[0];
        public string[] buildAt = new string[0]; // kind,home-relative x,home-relative z; normal placement validation still applies.
        public string research = "", prioritize = "";
        public bool startWave, stop, designate;
        public bool combat, defendLine, focusLeader;
        public bool muster;
        public float retreatHealth = 35;
        public bool makeMedicine;
        public bool resetResourcePriorities;
        public bool prioritizeConstruction;
        public bool resetConstructionPriorities;
        public bool recoveryRoles, cookedDiet;
        public bool watchBreaks, evadeBreaks;
        public string pauseGathering = "";
        public string[] priorities = new string[0];
        public string haul = "";
    }
    static void Note(string value) => File.AppendAllText(Dir + "play.txt", $"[{GameCalendar.GameSeconds:0.0}s / {DateTime.UtcNow:HH:mm:ss}] {value}\n");
    static Vector3 Home => (Vector3)typeof(ResourceManager).GetProperty("HomePosition", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    static CommanderAnt[] Team => CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
    static BuildingBase[] Buildings => SceneObjects.All<BuildingBase>().Where(b => b.isActiveAndEnabled && !b.IsDead && b.Data != null).ToArray();
    static async Task Wait(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(100);
        if (!condition()) throw new Exception("Scene readiness timeout");
    }
    static (object group, EditorWindow view, PropertyInfo index) View()
    {
        var asm = typeof(Editor).Assembly;
        var sizes = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizes).GetProperty("instance").GetValue(null);
        var group = sizes.GetMethod("GetGroup").Invoke(singleton, new[] { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        var type = asm.GetType("UnityEditor.GameView");
        return (group, EditorWindow.GetWindow(type), type.GetProperty("selectedSizeIndex", BindingFlags.Public | Hidden));
    }
    public static async Task<string> Prepare()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        Directory.CreateDirectory(Dir + "screens");
        if (File.Exists(Dir + "restore.json") && JsonUtility.FromJson<RestoreState>(File.ReadAllText(Dir + "restore.json")).active)
            throw new Exception("An isolated run is already active; finish it before preparing again");
        var v = View();
        var s = new RestoreState { settings = UserSettings.Current.Clone(), root = SaveStorage.RootOverride, resolution = (int)v.index.GetValue(v.view), addedResolution = -1, active = true };
        File.WriteAllText(Dir + "restore.json", JsonUtility.ToJson(s, true));
        try
        {
            var asm = typeof(Editor).Assembly;
            s.addedResolution = (int)v.group.GetType().GetMethod("GetBuiltinCount").Invoke(v.group, null) + (int)v.group.GetType().GetMethod("GetCustomCount").Invoke(v.group, null);
            var size = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), 1440, 900, "Early flow review");
            v.group.GetType().GetMethod("AddCustomSize").Invoke(v.group, new[] { size });
            File.WriteAllText(Dir + "restore.json", JsonUtility.ToJson(s, true));
            v.index.SetValue(v.view, s.addedResolution); v.view.Focus(); v.view.Repaint();
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "EarlyFlow1113-" + Guid.NewGuid().ToString("N"));
            var settings = s.settings.Clone(); settings.autoSaveEnabled = settings.autoPauseDowned = settings.autoPauseThreat = false;
            UserSettings.Apply(settings, false);
            await Wait(() => !SaveSystem.Busy && GameMenuController.Instance != null && GameSession.Exists);
            UnityEngine.Random.InitState(261106);
            StartingCandidate[] team; int rolls = 0;
            do { team = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray(); rolls++; }
            while (StartingCandidate.Missing(team).Length > 0 && rolls < 100);
            SaveSystem.NewGame(new NewGameOptions { seed = 261106, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 3, startingTeam = team });
            await Wait(() => !SaveSystem.Busy && WaveDefense.Instance != null);
            GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(200);
            File.WriteAllText(Dir + "play.txt", "CONTINUOUS NATURAL RUN: 3 commanders, seed 261106; game orders via official CLI, not manual mouse gameplay.\n");
            Note("START candidate rolls=" + rolls + " isolatedRoot=" + SaveStorage.RootOverride);
            var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>(); camera.FocusOn(Home); camera.enabled = false;
            SceneObjects.Any<SelectionManager>()?.SelectOnly(Team[0].GetComponent<SelectableObject>());
            Designate();
            if (SceneObjects.All<CrashWreck>().Any(w => w.isActiveAndEnabled)) throw new Exception("New baseline must not contain crash wrecks");
            Place(BuildingKind.Storage); for (int i = 0; i < 3; i++) Place(BuildingKind.SleepingMat);
            // Hearth and research lab are ordered at the next observation boundary.
            Snapshot(); await Capture("00-start"); return "Prepared isolated new game, paused; " + Status();
        }
        catch { Finish(); throw; }
    }
    static void Designate()
    {
        foreach (var type in new[] { ResourceType.Food, ResourceType.Wood, ResourceType.Soil })
        {
            var nodes = ResourceNode.Available.Where(n => n.NeedsDesignation && n.ResourceType == type && !n.IsDepleted && Vector3.Distance(n.transform.position, Home) < 50)
                .OrderBy(n => Vector3.Distance(n.transform.position, Home)).Take(3).ToArray();
            foreach (var n in nodes) n.RequestWork();
            Note($"DESIGNATE {type} x{nodes.Length}");
        }
    }
    static bool Place(BuildingKind kind)
    {
        Physics.SyncTransforms(); var pc = SceneObjects.Any<BuildingPlacementController>();
        if (!pc.BeginPlacement(kind, UnitRole.Worker, null)) { Note("BUILD rejected " + kind); return false; }
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { pc.PendingKind, UnitRole.Worker });
        for (int radius = 4; radius <= 25; radius += 3) for (int i = 0; i < 24; i++)
        {
            var a = i * Mathf.PI / 12; var p = Home + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
            if (!NavMesh.SamplePosition(p, out var hit, 2, NavMesh.AllAreas)) continue;
            var pos = (Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition", Hidden).Invoke(pc, new object[] { template, hit.position });
            if (!(bool)typeof(BuildingPlacementController).GetMethod("CellValid", Hidden).Invoke(pc, new object[] { pos })) continue;
            int count = pc.PlaceLine(new List<Vector3> { pos }, new List<bool> { true }); Note($"BUILD {kind} accepted={count} at {pos}"); return count > 0;
        }
        pc.CancelPlacement(); Note("BUILD no valid position " + kind); return false;
    }
    static string Status() => $"wave={WaveDefense.Instance.Wave} inWave={WaveDefense.Instance.InWave} left={WaveDefense.Instance.PeaceLeft:0} enemies={WaveDefense.Instance.RemainingEnemies} food={ResourceManager.Instance.GetAmount(ResourceType.Food)} soil={ResourceManager.Instance.GetAmount(ResourceType.Soil)} wood={ResourceManager.Instance.GetAmount(ResourceType.Wood)} screen={GameMenuController.Instance.ScreenName}";
    static void PlaceAt(string order)
    {
        var parts = order.Split(',');
        var kind = (BuildingKind)Enum.Parse(typeof(BuildingKind), parts[0]);
        var target = Home + new Vector3(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), 0,
            float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
        var pc = SceneObjects.Any<BuildingPlacementController>();
        if (!pc.BeginPlacement(kind, UnitRole.Worker, null)) { Note("BUILD AT rejected " + order); return; }
        try
        {
            Physics.SyncTransforms();
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { pc.PendingKind, UnitRole.Worker });
            foreach (var offset in new[] { Vector3.zero, Vector3.right * 3, Vector3.left * 3, Vector3.forward * 3, Vector3.back * 3,
                new Vector3(3,0,3), new Vector3(-3,0,3), new Vector3(3,0,-3), new Vector3(-3,0,-3) })
            {
                if (!NavMesh.SamplePosition(target + offset, out var hit, 2, NavMesh.AllAreas)) continue;
                var pos = (Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition", Hidden).Invoke(pc, new object[] { template, hit.position });
                if (!(bool)typeof(BuildingPlacementController).GetMethod("CellValid", Hidden).Invoke(pc, new object[] { pos })) continue;
                Note("BUILD AT " + order + " pos=" + pos + " accepted=" + pc.PlaceLine(new List<Vector3> { pos }, new List<bool> { true }));
                return;
            }
            Note("BUILD AT no valid nearby cell " + order);
        }
        finally { pc.CancelPlacement(); }
    }
    static void Snapshot()
    {
        Note(Status() + " frame=" + Time.frameCount + " speed=" + Time.timeScale);
        foreach (var c in Team) Note($"HERO {c.CommanderName} hp={c.PersonalHealth:0.0} down={c.Health.Downed} satiety={c.Satiety:0} mood={c.Mood:0} sleep={c.IsAsleep} work={c.IsWorking} activity={c.CurrentActivity} node={c.CurrentResourceNode?.name} delivery={c.DeliverySite != null} build={c.ConstructionTarget?.BuildingKind} care={CommanderAnt.CareWaitReason(c)} pos={c.Position}");
        Note("BUILDINGS " + string.Join(", ", Buildings.Select(b => b.Data.kind + "@" + b.transform.position + $" hp={b.CurrentHealth:0}/{b.MaxHealth:0}")));
        Note("STOCK " + string.Join(", ", Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().Where(t => ResourceManager.Instance.GetAmount(t) > 0).Select(t => t + "=" + ResourceManager.Instance.GetAmount(t))));
        foreach (var c in Team) Note($"RECOVERY {c.CommanderName} {c.RecoveryStatus} moldProtection={c.Health.moldImmunityRemaining:0.0} break={c.PersonalState.mentalBreak} illness=" + string.Join(",", c.Health.illnesses.Select(i => $"{i.kind}:severity={i.severity:0.00},immunity={i.immunity:0.00},tend={i.tendLeft:0}")));
        Note("ALERTS " + string.Join(" | ", ToastManager.VisibleLines));
        foreach (var site in SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled)) Note($"SITE {site.BuildingKind} wait={site.WaitReason} work={site.RemainingWork:0}");
        foreach (var p in SceneObjects.All<Processor>().Where(p => p.isActiveAndEnabled)) Note($"PROCESSOR {p.Data?.kind} wait={p.WaitReason} progress={p.Progress:0.00}");
        var r = CampaignResearch.Instance; Note("RESEARCH " + r.Active?.Name + " " + r.TimeEstimate + " progress=" + r.Progress
            + " completed=" + string.Join(",", CampaignResearch.Technologies.Where(t => r.Has(t.Technology)).Select(t => t.Technology)));
        foreach (var lab in SceneObjects.All<ScienceLab>().Where(l => l.isActiveAndEnabled && !l.IsDead)) Note("LAB target=" + lab.Target?.CommanderName + " wait=" + r.LabWaitReason(lab));
        foreach (var k in SceneObjects.All<Kitchen>().Where(k => k.isActiveAndEnabled && !k.IsDead && !k.IsTable))
            Note("KITCHEN " + k.Data.kind + " wait=" + k.WaitReason + " progress=" + k.Progress.ToString("0.00") + " staged=" + k.StagedDescription);
        Note("MEALS total=" + ProductionStock.Count(ResourceType.SimpleMeal) + " stocked=" + ResourceManager.Instance.GetAmount(ResourceType.SimpleMeal));
        if (WaveDefense.Instance.InWave)
            foreach (var e in SceneObjects.Any<LocalIncursions>().Visitors.Where(e => e != null && !e.IsDead && !e.Retreating))
                Note("ENEMY " + e.GetType().Name + " hp=" + e.CurrentHealth.ToString("0.0") + " bodies=" + e.HordeCount + " pos=" + e.Position);
    }
    static async Task Capture(string label)
    {
        if (Time.timeScale == 0) await Task.Delay(1100); // Let unscaled HUD refresh before a paused capture.
        ScreenCapture.CaptureScreenshot(Dir + "screens/" + label + ".png"); await Task.Delay(300);
    }
    public static Task<string> ResumeCheckpoint() => ResumeSaved("checkpoint-save.json", "Resume normal isolated checkpoint");
    public static async Task<string> ReopenCheckpoint()
    {
        var previousLog = File.ReadAllText(Dir + "play.txt");
        await Prepare();
        File.WriteAllText(Dir + "play.txt", previousLog);
        return await ResumeCheckpoint();
    }
    public static Task<string> ResumeOriginal() => ResumeSaved("after-wave3-raw.json", "POST-FIX continuation of unmodified first-three-wave snapshot");
    public static Task<string> ResumeFinal() => ResumeSaved("postfix-final-save.json", "FINAL visual reload of the normally saved natural continuation");
    static async Task<string> ResumeSaved(string file, string heading)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Prepared isolated game required");
        if (!SaveSystem.TryLoad(Path.GetFullPath(Dir + file), out var error)) throw new Exception(error);
        await Wait(() => !SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted);
        SimulationPause.UserSpeed(0);
        var camera = SceneObjects.Any<AntColony.Camera.IsometricCameraController>(); camera.FocusOn(Home); camera.enabled = false;
        Note(heading);
        Snapshot(); await Capture("postfix-resume"); return Status();
    }
    public static async Task<string> Step()
    {
        if (!Application.isPlaying || !File.Exists(Dir + "restore.json") || !JsonUtility.FromJson<RestoreState>(File.ReadAllText(Dir + "restore.json")).active) throw new Exception("Prepared Play session required");
        var o = JsonUtility.FromJson<Orders>(File.ReadAllText(Dir + "orders.json"));
        if (o.seconds < 0 || o.seconds > 120 || o.speed < 1 || o.speed > 5) throw new Exception("Step duration 0..120 game seconds, speed 1..5");
        Note("STEP " + JsonUtility.ToJson(o));
        try
        {
            if (o.stop) foreach (var c in Team) c.CommandStop();
            if (o.muster)
                for (int i = 0; i < Team.Length; i++)
                    if (Team[i].PersonalHealth > 0 && Team[i].TryWorkApproach(Home + new Vector3((i - 1) * 2, 0, 0), out var rally))
                        Team[i].CommandMove(rally);
            foreach (var entry in o.priorities ?? new string[0])
            {
                var parts = entry.Split(',');
                Team[int.Parse(parts[0])].SetJobPriority((CommanderJobs)Enum.Parse(typeof(CommanderJobs), parts[1]), int.Parse(parts[2]));
            }
            if (!string.IsNullOrEmpty(o.haul))
            {
                var type = (ResourceType)Enum.Parse(typeof(ResourceType), o.haul);
                var worker = Team[1];
                var pile = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == type && n.CanGatherWith(worker))
                    .OrderBy(n => (n.transform.position - worker.Position).sqrMagnitude).FirstOrDefault();
                if (pile != null) worker.CommandGather(pile);
                Note("PLAYER direct haul " + type + " found=" + (pile != null));
            }
            if (o.cookedDiet)
            {
                CommanderAnt.CommonDiet = CommanderAnt.DietSimple | CommanderAnt.DietFine;
                foreach (var c in Team) c.PersonalState.meal.diet = -1;
                Note("PLAYER uses cooked-food common diet");
            }
            if (o.recoveryRoles && Team.Length >= 3)
            {
                Team[0].SetJobPriority(CommanderJobs.Nursing, 9);
                Team[1].SetJobPriority(CommanderJobs.Gathering, 9);
                Team[2].SetJobPriority(CommanderJobs.Cooking, 9);
                var herb = ResourceNode.Available.Where(n => n.ResourceType == ResourceType.Herb && !n.IsDepleted)
                    .OrderBy(n => (n.transform.position - Team[0].Position).sqrMagnitude).FirstOrDefault();
                if (herb != null) Team[0].CommandGather(herb);
                Note("PLAYER assigns nursing / gathering / cooking priorities and orders first herb collection");
            }
            if (o.designate) Designate();
            if (o.prioritizeConstruction)
            {
                foreach (var site in SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled)) WorkPriorities.Set(site, 8, false);
                Note("PLAYER prioritizes existing construction sites=8");
            }
            if (o.resetConstructionPriorities)
            {
                foreach (var site in SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled)) WorkPriorities.Set(site, WorkPriorities.Default, false);
                Note("PLAYER restores construction target priorities to normal");
            }
            if (!string.IsNullOrEmpty(o.pauseGathering))
                foreach (var type in o.pauseGathering.Split(','))
                    foreach (var node in ResourceNode.Available.Where(n => n.NeedsDesignation && n.WorkRequested && n.ResourceType.ToString() == type).ToArray()) node.PauseWork(true);
            if (!string.IsNullOrEmpty(o.prioritize))
            {
                var type = (ResourceType)Enum.Parse(typeof(ResourceType), o.prioritize);
                foreach (var n in ResourceNode.Available.Where(n => n.NeedsDesignation && n.WorkRequested)) WorkPriorities.Set(n, n.ResourceType == type ? 8 : 5, false);
                Note("PLAYER sets resource priority " + type + "; job priorities unchanged");
            }
            if (o.resetResourcePriorities)
            {
                foreach (var n in ResourceNode.Available.Where(n => n.WorkRequested || n.IsLooseCargo)) WorkPriorities.Set(n, WorkPriorities.Default, false);
                Note("PLAYER restores resource target priorities to normal");
            }
            foreach (var b in o.build ?? new string[0]) Place((BuildingKind)Enum.Parse(typeof(BuildingKind), b));
            foreach (var b in o.buildAt ?? new string[0]) PlaceAt(b);
            if (o.makeMedicine)
            {
                var herbs = ResourceNode.Available.Where(n => n.NeedsDesignation && n.ResourceType == ResourceType.Herb && !n.IsDepleted).OrderBy(n => (n.transform.position - Home).sqrMagnitude).Take(2).ToArray();
                foreach (var n in herbs) n.RequestWork();
                var bench = SceneObjects.All<Processor>().FirstOrDefault(p => p.isActiveAndEnabled && !p.IsDead && p.IsMedical);
                if (bench != null && !bench.Orders.Any(order => bench.Recipes[order.recipe].output == ResourceType.MedicineBasic))
                {
                    int recipe = Enumerable.Range(0, bench.Recipes.Count).First(i => bench.Recipes[i].output == ResourceType.MedicineBasic);
                    Note("MEDICINE order=" + bench.AddOrder(recipe, Workshop.OrderMode.KeepStock, 6));
                }
                else Note("MEDICINE bench unavailable or already ordered; herbs=" + herbs.Length);
            }
            if (!string.IsNullOrEmpty(o.research))
            {
                var tech = (ScienceTechnology)Enum.Parse(typeof(ScienceTechnology), o.research);
                bool accepted = CampaignResearch.Instance.TryStart(tech); Note("RESEARCH start " + tech + " accepted=" + accepted + " blocked=" + CampaignResearch.Instance.BlockReason(tech) + " shortage=" + CampaignResearch.Instance.ResearchCostShortage(tech));
                var lab = SceneObjects.All<ScienceLab>().FirstOrDefault(l => l.isActiveAndEnabled && !l.IsDead);
                if ((accepted || CampaignResearch.Instance.Active?.Technology == tech) && lab != null && lab.Target == null)
                    foreach (var c in Team) if (c.SendToResearch(lab)) { Note("ASSIGN researcher " + c.CommanderName); break; }
            }
            if (o.defendLine) { int i = 0; foreach (var c in Team) c.CommandMove(Home + new Vector3((i++ - 1) * 2, 0, 2)); Note("PLAYER rallies behind towers"); }
            if (o.startWave)
            {
                float saved = WaveDefense.Instance.PeaceLeft;
                var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude).First(b => b.name == "WaveAction");
                button.onClick.Invoke(); Note("PLAYER WaveAction saved=" + saved + " accepted=" + WaveDefense.Instance.InWave);
            }
            float start = GameCalendar.GameSeconds, previous = start, next = start;
            if (o.watchBreaks)
            {
                var settings = UserSettings.Current.Clone(); settings.autoPauseThreat = true; UserSettings.Apply(settings, false);
            }
            float nextEvade = start;
            bool wasWave = WaveDefense.Instance.InWave;
            var assigned = new Dictionary<CommanderAnt, WildMonster>();
            var nextRetreat = new Dictionary<CommanderAnt, float>();
            bool hadHerbs = CampaignResearch.Instance.Has(ScienceTechnology.Herbs);
            bool hadResin = CampaignResearch.Instance.Has(ScienceTechnology.Resin);
            var activity = new Dictionary<string, float>();
            var previousHealth = Team.ToDictionary(c => c, c => c.PersonalHealth);
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(30, o.seconds / o.speed * 3 + 15));
            SimulationPause.UserSpeed(o.speed);
            while (GameCalendar.GameSeconds - start < o.seconds && DateTime.UtcNow < deadline && GameMenuController.Instance.ScreenName != "COLONY LOST")
            {
                if (Time.timeScale == 0 && SimulationPause.Reasons.Any(r => r.StartsWith("incident:")))
                { Note("PLAYER ATTENTION: automatic incident pause " + string.Join(",", SimulationPause.Reasons)); break; }
                if (o.evadeBreaks && GameCalendar.GameSeconds >= nextEvade)
                {
                    nextEvade = GameCalendar.GameSeconds + 2;
                    var attacker = Team.FirstOrDefault(c => c.PersonalState.mentalBreak == MentalBreak.AttackCommander && c.PersonalHealth > 0);
                    if (attacker != null) foreach (var c in Team.Where(c => c != attacker && c.CanReceiveOrders && c.PersonalHealth > 0))
                    {
                        var away = c.Position - attacker.Position; away.y = 0;
                        if (away.sqrMagnitude < .1f) away = Vector3.right;
                        if (NavMesh.SamplePosition(c.Position + away.normalized * 12, out var destination, 4, NavMesh.AllAreas))
                            c.CommandMove(destination.position);
                    }
                }
                float dt = Mathf.Max(0, GameCalendar.GameSeconds - previous); previous = GameCalendar.GameSeconds;
                if (!hadHerbs && CampaignResearch.Instance.Has(ScienceTechnology.Herbs)) { hadHerbs = true; Note("MILESTONE Herbs completed"); }
                if (!hadResin && CampaignResearch.Instance.Has(ScienceTechnology.Resin)) { hadResin = true; Note("MILESTONE Resin completed"); }
                foreach (var c in Team)
                {
                    if (!o.combat && previousHealth.TryGetValue(c, out var hp) && c.PersonalHealth < hp - .01f)
                        Note($"PEACE DAMAGE {c.CommanderName} {hp:0.0}->{c.PersonalHealth:0.0} breaks=" + string.Join(",", Team.Select(a => a.CommanderName + ":" + a.PersonalState.mentalBreak)));
                    previousHealth[c] = c.PersonalHealth;
                    string key = c.Health.Downed ? "downed" : c.IsAsleep ? "sleep" : c.IsEating || c.IsWashing || c.IsPlaying || c.PersonalState.treating ? "needs" : c.IsGatheringAnimation && c.CurrentResourceNode != null && c.CurrentResourceNode.IsRegrowing ? "growth_wait" : c.IsWorking || c.ServiceTarget != null || c.ScienceAssignment != null ? "work" : c.Agent != null && (c.Agent.hasPath || c.Agent.pathPending) ? "move" : "idle_or_combat";
                    activity[key] = (activity.TryGetValue(key, out var old) ? old : 0) + dt;
                }
                if (o.combat && WaveDefense.Instance.InWave)
                {
                    var local = SceneObjects.Any<LocalIncursions>();
                    var near = local.Visitors.Where(e => e != null && !e.IsDead && !e.Retreating && e.isActiveAndEnabled && Vector3.Distance(e.Position, Home) < (o.defendLine ? 12 : 30)).ToArray();
                    foreach (var c in Team.Where(c => !c.Health.Downed && c.PersonalHealth > 0))
                    {
                        if (c.PersonalHealth < o.retreatHealth)
                        {
                            var threats = local.Visitors.Where(e => e != null && !e.IsDead && !e.Retreating && e.isActiveAndEnabled).ToArray();
                            var closest = threats.OrderBy(e => (e.Position - c.Position).sqrMagnitude).FirstOrDefault();
                            if (closest != null && Vector3.Distance(c.Position, closest.Position) < 14
                                && (!nextRetreat.TryGetValue(c, out var nextOrder) || GameCalendar.GameSeconds >= nextOrder))
                            {
                                var away = c.Position - closest.Position; away.y = 0; if (away.sqrMagnitude < .1f) away = Vector3.right;
                                var candidates = Enumerable.Range(0, 8).Select(i => c.Position + Quaternion.Euler(0, i * 45, 0) * away.normalized * 12)
                                    .Where(p => c.TryWorkApproach(p, out _))
                                    .OrderByDescending(p => Mathf.Min(100, threats.Min(e => (e.Position - p).sqrMagnitude)) - Vector3.Distance(p, Home) * 3).ToArray();
                                if (candidates.Length > 0 && c.TryWorkApproach(candidates[0], out var escape))
                                { c.CommandMove(escape); Note("RETREAT " + c.CommanderName + " away from " + closest.Position + " to " + escape); }
                                nextRetreat[c] = GameCalendar.GameSeconds + 3;
                            }
                            continue;
                        }
                        var enemy = near.OrderByDescending(e => o.focusLeader && e is EnemyCommander)
                            .ThenBy(e => (e.Position - c.Position).sqrMagnitude).FirstOrDefault();
                        if (enemy != null && (!assigned.TryGetValue(c, out var previousTarget) || previousTarget == null || previousTarget.IsDead
                            || o.focusLeader && enemy is EnemyCommander && !(previousTarget is EnemyCommander)))
                        {
                            assigned[c] = enemy;
                            c.CommandAttack(enemy); Note((enemy is EnemyCommander ? "FOCUS LEADER " : "ATTACK TARGET ") + c.CommanderName + " -> " + enemy.Position);
                        }
                    }
                    if (near.Length > 0) SceneObjects.Any<AntColony.Camera.IsometricCameraController>().FocusOn(near.Aggregate(Vector3.zero, (a, e) => a + e.Position) / near.Length);
                }
                if (GameCalendar.GameSeconds >= next)
                {
                    next += o.combat ? 10 : 30; Snapshot(); await Capture(o.label + "-" + (int)GameCalendar.GameSeconds);
                }
                if (WaveDefense.Instance.InWave != wasWave) { Note("WAVE TRANSITION " + Status()); break; }
                await Task.Delay(o.combat ? 200 : 250);
            }
            Note("COMMANDER_SECONDS " + string.Join(", ", activity.Select(p => p.Key + "=" + p.Value.ToString("0.0"))));
            if (DateTime.UtcNow >= deadline) throw new Exception("Chunk watchdog: game clock did not complete; see play log");
            return Status();
        }
        finally { SimulationPause.UserSpeed(0); Snapshot(); await Capture(o.label + "-end"); }
    }
    public static string Preserve() { SimulationPause.UserSpeed(0); File.WriteAllText(Dir + "after-wave3-raw.json", (string)typeof(SaveSystem).Assembly.GetType("AntColony.Save.SaveOptionalStates").GetMethod("ToJson", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { SaveSnapshot.Capture(), true })); var text = string.Join("\n", Corpse.All.Where(c => c.Data.remaining <= 0).Select(c => JsonUtility.ToJson(c.Capture()))); File.WriteAllText(Dir + "expired-corpses.jsonl", text); return "Captured unmodified snapshot (not a validated save) and expired corpses: " + text; }
    public static string Checkpoint() { if (string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Isolated root required"); if (!SaveSystem.TrySave(false, 0, out var error)) throw new Exception(error); File.Copy(SaveSlots.PathFor(false, 0), Dir + "checkpoint-save.json", true); return SaveSlots.PathFor(false, 0); }
    public static string Finish()
    {
        if (!File.Exists(Dir + "restore.json")) return "No active restore file";
        var s = JsonUtility.FromJson<RestoreState>(File.ReadAllText(Dir + "restore.json"));
        if (!s.active) return "Already restored";
        SimulationPause.UserSpeed(0); SaveStorage.RootOverride = s.root; UserSettings.Apply(s.settings, false);
        var v = View(); v.index.SetValue(v.view, s.resolution);
        if (s.addedResolution >= 0) v.group.GetType().GetMethod("RemoveCustomSize").Invoke(v.group, new object[] { s.addedResolution });
        s.active = false; File.WriteAllText(Dir + "restore.json", JsonUtility.ToJson(s, true));
        return "Settings, save root and Game View restored; Play paused";
    }
    public static async Task<string> SaveFinal()
    {
        if (string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Isolated root required");
        SimulationPause.UserSpeed(0);
        if (!SaveSystem.TrySave(false, 0, out var error)) throw new Exception(error);
        File.Copy(SaveSlots.PathFor(false, 0), Dir + "postfix-final-save.json", true);
        SceneObjects.Any<AntColony.Camera.IsometricCameraController>().FocusOn(Home);
        SceneObjects.Any<SelectionManager>()?.SelectOnly(Team.OrderBy(c => c.PersonalHealth).First().GetComponent<SelectableObject>());
        Snapshot(); await Capture("postfix-final"); return "Normal isolated save succeeded; " + Status();
    }
    public static async Task<string> ReviewUI()
    {
        SimulationPause.UserSpeed(0);
        var menu = GameMenuController.Instance;
        var panel = (RectTransform)typeof(GameMenuController).GetField("tooltipPanel", Hidden).GetValue(menu);
        var tip = (UnityEngine.UI.Text)typeof(GameMenuController).GetField("tip", Hidden).GetValue(menu);
        UnityEngine.UI.Button Find(string name) => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include).First(b => b.name == name);
        Rect Box(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y); }
        try
        {
            BuildScreen.Open(); await Task.Delay(300);
            foreach (var target in new[] { ("저장", "Storage"), ("작업", "ScienceLab"), ("방어", "AcidTower"), ("타일", "SoilWall") })
            {
                Find("Tab " + target.Item1).onClick.Invoke(); await Task.Delay(750);
                var button = Find("Build " + target.Item2);
                var tooltip = button.GetComponent<MenuTooltip>();
                tooltip.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                await Task.Delay(200); Canvas.ForceUpdateCanvases();
                if (!panel.gameObject.activeInHierarchy || tip.text != tooltip.Message) throw new Exception("Tooltip not visible: " + target.Item2);
                var box = Box(panel);
                if (box.xMin < 0 || box.yMin < 0 || box.xMax > Screen.width + 1 || box.yMax > Screen.height + 1 || tip.preferredHeight > tip.rectTransform.rect.height + 1) throw new Exception("Tooltip clipped: " + target.Item2);
                foreach (var control in button.transform.root.GetComponentsInChildren<UnityEngine.UI.Button>())
                    if (control.gameObject.activeInHierarchy && box.Overlaps(Box((RectTransform)control.transform))) throw new Exception("Tooltip covers " + control.name);
                if (!BuildScreen.TooltipArea((RectTransform)button.transform, out var area) || area == null || Mathf.Abs(Box(area).xMin - box.xMin) > 1) throw new Exception("Tooltip is outside right description column");
                Note("UI tooltip PASS " + target.Item2 + " " + box + " text=" + tip.text);
                await Capture("ui-" + target.Item2);
                tooltip.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            }
            return "Four live build tooltips visible, unclipped, no active button overlap";
        }
        finally { for (int i = 0; i < 3 && BuildScreen.IsOpen; i++) BuildScreen.Back(); menu.Tooltip(""); SimulationPause.UserSpeed(0); }
    }
    public static string Inspect()
    {
        var rows = new List<string>();
        foreach (var bin in ResourceManager.Instance.Bins)
            rows.Add("BIN " + bin.Host?.name + " usable=" + bin.Usable + " pos=" + bin.Position + " food=" + bin.Amount(ResourceType.Food)
                + " cap=" + bin.Capacity(ResourceType.Food) + " free=" + bin.Free(ResourceType.Food) + " allowed=" + bin.Allows(ResourceType.Food));
        rows.Add("FLOOR " + string.Join(",", ResourceNode.Available.Where(n => n.IsLooseCargo && !n.IsDepleted).GroupBy(n => n.ResourceType).Select(g => g.Key + ":nodes=" + g.Count() + ":amount=" + g.Sum(n => n.AmountRemaining))));
        foreach (var c in Team) foreach (var bin in ResourceManager.Instance.Bins)
            rows.Add("BIN ROUTE " + c.CommanderName + " host=" + bin.Host?.name + " reach=" + c.TryWorkApproach(bin.Position, out var approach) + " approach=" + approach);
        foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo && !n.IsDepleted && n.ResourceType == ResourceType.Food).OrderBy(n => (n.transform.position - Home).sqrMagnitude).Take(1))
            foreach (var c in Team) rows.Add("FOOD ROUTE " + c.CommanderName + " canGather=" + n.CanGatherWith(c) + " reach=" + c.TryWorkApproach(n.transform.position, out _) + " jobs=" + c.AllowsJob(CommanderJobs.Hauling));
        foreach (var c in Team) rows.Add($"{c.CommanderName} cargo={c.CarriedAmount} type={c.CarriedType} gather={c.WorkState.Priority(CommanderJobs.Gathering)} build={c.WorkState.Priority(CommanderJobs.Building)} haul={c.WorkState.Priority(CommanderJobs.Hauling)} sleep={c.IsAsleep}");
        foreach (var c in Team) rows.Add($"AGENT {c.CommanderName} state={typeof(WorkerAnt).GetField("state", Hidden).GetValue(c)} pos={c.Position} node={c.CurrentResourceNode?.transform.position} destination={c.Agent.destination} left={c.Agent.remainingDistance} path={c.Agent.pathStatus} pending={c.Agent.pathPending} velocity={c.Agent.velocity}");
        foreach (var p in SceneObjects.All<Processor>().Where(p => p.isActiveAndEnabled && p.IsMedical)) rows.Add("MEDICAL " + p.WaitReason + " paused=" + p.Paused + " needsIngredients=" + p.NeedsIngredients + " needsWork=" + p.NeedsWork + " orders=" + p.Orders.Count + " ingredients=" + string.Join(",", p.IngredientNeeds.Select(n => n.type + ":" + n.amount)));
        rows.Add("HERB STOCK=" + ResourceManager.Instance.GetAmount(ResourceType.Herb));
        foreach (var s in SceneObjects.All<BuildingConstructionSite>().Where(s => s.isActiveAndEnabled))
        {
            rows.Add($"SITE {s.BuildingKind} pos={s.Position} ready={s.Ready} wait={s.WaitReason} " + string.Join(",", s.Needs.Select(n => $"{n.type} need={n.need} delivered={n.delivered} reserved={n.reserved} transit={s.InTransit(n.type)}")));
            foreach (var c in Team) rows.Add($"ROUTE {c.CommanderName} directSite={c.TravelDistance(s.Position):0.0} includingPickup={typeof(CommanderAnt).GetMethod("WorkTravelDistance", Hidden).Invoke(c, new object[] { s, true })}");
        }
        foreach (var n in ResourceNode.Available.Where(n => !n.IsDepleted && (n.ResourceType == ResourceType.Soil || n.ResourceType == ResourceType.Food)).OrderBy(n => (n.transform.position - Home).sqrMagnitude).Take(24)) rows.Add($"NODE {n.name} type={n.ResourceType} amount={n.AmountRemaining} loose={n.IsLooseCargo} designated={n.WorkRequested} dist={Vector3.Distance(n.transform.position, Home):0} pos={n.transform.position}");
        var result = string.Join("\n", rows); File.WriteAllText(Dir + "inspect-" + (int)GameCalendar.GameSeconds + ".txt", result); return result;
    }
    public static string InspectCombat()
    {
        var rows = SceneObjects.Any<LocalIncursions>().Visitors.Where(e => e != null && !e.IsDead).Select(e =>
        {
            var target = typeof(WildMonster).GetField("currentTarget", Hidden).GetValue(e) as Component;
            var agent = e.GetComponent<NavMeshAgent>();
            return $"{e.name} aim={e.Aim} target={target?.name}/{target?.GetType().Name} pos={e.Position} targetpos={target?.transform.position} path={agent?.pathStatus} remaining={agent?.remainingDistance} range={typeof(WildMonster).GetField("attackRange", Hidden).GetValue(e)}";
        });
        var result = string.Join("\n", rows); File.WriteAllText(Dir + "combat-inspect-" + (int)GameCalendar.GameSeconds + ".txt", result); return result;
    }
    public static string InspectTower()
    {
        var tower = SceneObjects.All<AcidTower>().First(t => t.isActiveAndEnabled && !t.IsDead);
        var rows = tower.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials.Where(m => m != null).Select(m =>
            r.name + " | " + m.name + " | " + m.shader.name + " | color=" + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : m.color.ToString())
            + " | emission=" + (m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "none")));
        var result = string.Join("\n", rows); File.WriteAllText(Dir + "tower-materials.txt", result); return result;
    }
    public static string RecoveryAudit()
    {
        var rows = new List<string>();
        foreach (var c in Team) rows.Add(c.CommanderName + " hp=" + c.PersonalHealth + " combat=" + c.IsInCombat + " lastCombat=" + c.PersonalState.lastCombatSeconds + " break=" + c.PersonalState.mentalBreak + " rage=" + c.PersonalState.rageRemaining + " cargo=" + c.CarriedType + ":" + c.CarriedAmount + " health=" + JsonUtility.ToJson(c.Health) + " mood=" + JsonUtility.ToJson(c.PersonalState));
        rows.AddRange(CampaignHistory.Instance.Data.recent.Skip(Math.Max(0, CampaignHistory.Instance.Data.recent.Count - 25)).Select(e => e.seconds + " " + e.kind + " " + e.name + " " + e.result));
        var result = string.Join("\n", rows); File.WriteAllText(Dir + "recovery-audit.txt", result); return result;
    }
    public static async Task<string> ReviewHealth()
    {
        SimulationPause.UserSpeed(0);
        var c = Team.OrderBy(x => x.PersonalHealth).First();
        var menu = GameMenuController.Instance;
        try
        {
            menu.ShowHealth(c); await Task.Delay(1100);
            var text = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Exclude)
                .First(t => t.text.Contains(c.RecoveryStatus));
            if (text.preferredHeight > text.rectTransform.rect.height + 1) throw new Exception("Recovery health status overflows its text box");
            await Capture("postfix-health"); return c.CommanderName + " HP=" + c.PersonalHealth + " / " + c.RecoveryStatus + " / text fits";
        }
        finally { menu.Resume(); SimulationPause.UserSpeed(0); }
    }
    public static async Task<string> PinConstructionMaterial()
    {
        if (!PrototypeResourceHud.Pinned.Contains(ResourceType.Soil)) PrototypeResourceHud.TogglePin(ResourceType.Soil);
        await Task.Delay(1100);
        var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude).First(b => b.name == "Pinned Soil");
        var rect = (RectTransform)button.transform;
        if (!button.gameObject.activeInHierarchy || rect.rect.height < 20) throw new Exception("Soil stock is not readable in the resource HUD");
        await Capture("postfix-resources");
        return "Added Soil to existing visible resource pins through the normal pin action; other pins preserved";
    }
}

