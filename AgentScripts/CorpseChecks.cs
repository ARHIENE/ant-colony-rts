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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;
using RT = AntColony.Data.ResourceType;
using AntSelection = AntColony.Units.SelectionManager;

public static class CorpseChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }
    static void Near(float a, float b, string label) => Check(Mathf.Abs(a - b) < .01f, label + " " + a + "/" + b);
    static float Mood(CommanderAnt c, string reason) => c.PersonalState.moodFactors.Find(m => m.reason == reason)?.value ?? 0;
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "ready"); Time.timeScale = 0;
    }
    static void Warp(CommanderAnt c, Vector3 position)
    {
        c.CommandStop(); Check(NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas) && c.Agent.Warp(hit.position), "warp");
    }
    static Corpse Body(CommanderAnt c, CorpseKind kind = CorpseKind.Ant, int count = 1)
        => Corpse.Spawn(new Corpse.State { name = "검사", kind = kind, count = count, position = c.Position });
    static void Reset(CommanderAnt c)
    {
        c.CommandStop(); c.WorkState.jobs = CommanderJobs.None; c.WorkState.resting = false;
        c.WorkState.health = GameBalance.CommanderHealth; c.WorkState.recoverySeconds = 0;
        c.PersonalState.meal = new CommanderMealState(); c.PersonalState.joy = new CommanderJoyState();
        c.PersonalState.sleep = new CommanderSleepState(); c.PersonalState.mentalBreak = MentalBreak.None;
        c.PersonalState.rageRemaining = 0; c.PersonalState.injuries.Clear(); c.PersonalState.moodFactors.Clear();
        c.Traits.values.Clear(); c.Traits.passions.Clear();
    }
    static async Task Interaction(CommanderAnt a)
    {
        foreach (var c in CommanderRoster.Instance.Commanders) { c.CommandStop(); Reset(c); }
        foreach (var body in Corpse.All.ToArray()) body.Tick(300);
        var corpse = Body(a, count: 2);
        Warp(a, a.Position + Vector3.right * 3);
        var selection = Object.FindAnyObjectByType<AntSelection>();
        var controller = Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>();
        var camera = UnityEngine.Camera.main;
        var position = camera.transform.position; var rotation = camera.transform.rotation;
        var oldKeyboard = Keyboard.current; var keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
        try
        {
            // Exercise the real screen-ray command handlers; no OS pointer or screenshot is needed.
            camera.transform.position = corpse.Position + Vector3.up * 20;
            camera.transform.rotation = Quaternion.Euler(90, 0, 0); Physics.SyncTransforms();
            Vector2 screen = camera.WorldToScreenPoint(corpse.Position + Vector3.up * .25f);
            Check(Physics.Raycast(camera.ScreenPointToRay(screen), out var hit, 500) && hit.collider.GetComponentInParent<Corpse>() == corpse, "screen ray hits corpse");
            typeof(AntSelection).GetMethod("ClickSelectOrClear", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(selection, new object[] { screen, false });
            Check(WorkTargetPanel.Target == corpse, "left click selects corpse panel");
            selection.ClearSelection();
            var command = typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand", BindingFlags.Instance | BindingFlags.NonPublic);
            command.Invoke(controller, new object[] { screen });
            Check(WorkTargetPanel.Target == corpse, "unselected right click opens corpse panel");
            selection.SelectOnly(a.GetComponent<AntColony.Units.SelectableObject>());
            command.Invoke(controller, new object[] { screen });
            Check(a.CorpseTarget == corpse && !a.EatingCorpse, "selected right click cleans"); a.CommandStop();
            InputState.Change(keyboard, new KeyboardState(Key.LeftAlt));
            command.Invoke(controller, new object[] { screen });
            Check(a.CorpseTarget == null && corpse.Data.count == 2, "alt click rejects non-cannibal: target=" + (a.CorpseTarget != null) + " eating=" + a.EatingCorpse + " count=" + corpse.Data.count + " cannibal=" + a.Traits.Has(CommanderTrait.Cannibal) + " alt=" + keyboard.leftAltKey.isPressed);
            a.Traits.TryAdd(CommanderTrait.Cannibal);
            command.Invoke(controller, new object[] { screen });
            Check(a.CorpseTarget == corpse && a.EatingCorpse, "alt right click starts eating");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard); oldKeyboard?.MakeCurrent();
            camera.transform.SetPositionAndRotation(position, rotation);
        }
        float food = ResourceManager.Instance.GetAmount(RT.Food);
        ResourceManager.Instance.AddCapacity(RT.Food, 100);
        Time.timeScale = 1;
        var end = DateTime.UtcNow.AddSeconds(25);
        while (a.CorpseTarget != null && DateTime.UtcNow < end) await Task.Delay(50);
        Time.timeScale = 0;
        Check(a.CorpseTarget == null && corpse.Data.count == 1, "real update movement and five-second eating complete");
        Near(ResourceManager.Instance.GetAmount(RT.Food), food + 10, "real update eating reward once");
        corpse.Tick(300); Reset(a);
        var hunted = Corpse.Spawn(new Corpse.State { name = "운반 검사", position = a.Position, kind = CorpseKind.Wildlife, food = 20 });
        a.SetJobEnabled(CommanderJobs.Cleaning, true); a.TickDuty(2);
        Check(a.CorpseTarget == null && hunted.Available, "automatic cleaning preserves hunted food");
        a.SetJobEnabled(CommanderJobs.Hauling, true); a.TickDuty(2);
        Check(a.CurrentResourceNode == hunted.GetComponent<ResourceNode>() && a.CorpseTarget == null, "hauling wins for unmarked hunted corpse");
        a.CommandStop(); a.SetJobEnabled(CommanderJobs.Hauling, false);
        WorkTargetPanel.Select(hunted); await Task.Delay(100);
        GameObject.Find("EatCorpse").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Check(CommanderRoster.Instance.Commanders.Any(c => c.CurrentResourceNode == hunted.GetComponent<ResourceNode>()), "panel transport button starts hauling");
        foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
        hunted.Priority = true; a.TickDuty(2);
        Check(a.CorpseTarget == hunted, "priority explicitly permits hunted corpse cleanup");
        a.CommandStop(); hunted.Tick(300);
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var oldRoot = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Corpse-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260930, mapSize = MapSize.Small, commanderDeath = CommanderDeathMode.Gentle });
            await Ready(); await Task.Delay(300); GameMenuController.Instance.Resume(); Time.timeScale = 0;
            GameSession.Instance.MarkStarted(0, 10);
            var cs = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember).ToArray();
            foreach (var c in cs) Reset(c);
            var a = cs[0]; var b = cs[1]; var victim = cs[2];
            Warp(a, WorldMapManager.Instance.HomePosition + Vector3.right * 8); Warp(b, a.Position + Vector3.right);
            var corpse = Body(a, count: 3);
            Check(corpse.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "corpse death pose");
            Check(corpse.GetComponent<IDamageable>() == null && corpse.GetComponent<AntColony.Units.SelectableObject>() == null, "body has no combat identity");
            Check(!a.StartCorpseWork(corpse, true), "non-cannibal cannot eat");
            Check(a.StartCorpseWork(corpse), "manual cleaning");
            Check(!b.StartCorpseWork(corpse), "exclusive claim");
            a.TickDuty(2); Near(a.CorpseProgress, 2, "clean progress");
            a.CommandMove(a.Position); Check(a.CorpseTarget == null && corpse.Handler == null, "move releases claim");
            a.CommandStop(); a.Traits.TryAdd(CommanderTrait.Undertaker); a.Traits.TryAdd(CommanderTrait.Neat);
            a.TickPersonal(.01f); Near(Mood(a, "시체 주변"), -3, "neat corpse penalty");
            Check(a.StartCorpseWork(corpse), "restart cleaning");
            a.TickDuty(3); Near(a.CorpseProgress, 4.5f, "neat cleaning +50%");
            a.TickDuty(.34f); Check(!corpse.gameObject.activeSelf && a.CorpseTarget == null, "whole pile cleaned");
            Near(Mood(a, "시체 정리"), 5, "undertaker reward");
            a.TickPersonal(.01f); Near(Mood(a, "시체 주변"), 0, "penalty removed");
            Reset(a); corpse = Body(a);
            a.TickDuty(2); Check(a.CorpseTarget == null, "disabled cleaning respected");
            a.SetJobEnabled(CommanderJobs.Cleaning, true); a.TickDuty(2); Check(a.CorpseTarget == corpse, "autonomous cleaning starts");
            a.CommandStop(); a.SetJobEnabled(CommanderJobs.Cleaning, false);
            corpse.Tick(300); Check(!corpse.gameObject.activeSelf, "expiry removes corpse");
            corpse = Body(a); Check(a.StartCorpseWork(corpse), "claim before expiry"); corpse.Tick(300);
            Check(a.CorpseTarget == null, "expiry releases worker");
            corpse = Body(a); Check(a.StartCorpseWork(corpse), "start before sleep");
            GameSession.Instance.MarkStarted(0, 610); a.TickDuty(.01f);
            Check(a.CorpseTarget == null && corpse.Handler == null, "sleep releases claim");
            GameSession.Instance.MarkStarted(0, 10); corpse.Tick(300);
            Reset(a); Reset(b); b.Traits.TryAdd(CommanderTrait.Undertaker);
            var workNode = ResourceNode.Available.First(n => n.CanGather && !n.RequiresFishing && n.GetComponentInParent<ExpeditionSite>() == null && a.TryWorkApproach(n.transform.position, out _));
            a.CommandGather(workNode); b.CommandGather(workNode);
            Check(a.IsWorking && b.IsWorking, "joint work for relationship");
            var rel = a.PersonalState.Relation(b.PersonalState.id); rel.value = rel.nearbySeconds = 0;
            typeof(CommanderAnt).GetMethod("TickRelations", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(a, new object[] { 60f });
            Near(rel.value, 1.6f, "undertaker incoming relation x0.8");
            a.CommandStop(); b.CommandStop();
            var nearBody = Body(a); var priorityBody = Body(a); priorityBody.transform.position += Vector3.right * 2; priorityBody.Priority = true;
            a.SetJobEnabled(CommanderJobs.Cleaning, true); a.TickDuty(2);
            Check(a.CorpseTarget == priorityBody, "priority beats nearest"); a.CommandStop();
            nearBody.Tick(300); priorityBody.Tick(300);

            Reset(a); Reset(b); Warp(b, a.Position + Vector3.right);
            a.Traits.TryAdd(CommanderTrait.Cannibal); Check(!a.Traits.TryAdd(CommanderTrait.Neat), "exclusive traits");
            b.Traits.TryAdd(CommanderTrait.ColdBlooded);
            var witness = cs[3]; Reset(witness); Warp(witness, a.Position + Vector3.forward);
            var foodBefore = ResourceManager.Instance.GetAmount(RT.Food);
            ResourceManager.Instance.AddCapacity(RT.Food, 100);
            corpse = Body(a, CorpseKind.ColonyCommander);
            float relationBefore = witness.PersonalState.Relation(a.PersonalState.id).value;
            Check(a.StartCorpseWork(corpse, true), "manual cannibal meal"); a.TickDuty(5);
            Check(ResourceManager.Instance.GetAmount(RT.Food) == foodBefore + 10, "food once");
            Near(a.Satiety, 100, "satiety restored"); Near(Mood(a, "동족 포식"), 15, "cannibal mood");
            Near(Mood(witness, "동족 포식 목격"), -8, "witness mood"); Near(Mood(b, "동족 포식 목격"), 0, "cold blood exemption");
            Near(witness.PersonalState.Relation(a.PersonalState.id).value, relationBefore - 10, "directional witness relationship");
            Near(Mood(a, "아군 장수 시체 포식"), -3, "colony commander mood loss"); Near(Mood(b, "아군 장수 시체 포식"), -3, "cold blood still loses"); Near(Mood(witness, "아군 장수 시체 포식"), -3, "witness mood loss");
            a.TickDuty(5); Check(ResourceManager.Instance.GetAmount(RT.Food) == foodBefore + 10, "no double reward");
            corpse = Body(a, count: 2); a.PersonalState.meal.satiety = 10; a.TickDuty(.1f);
            Check(a.CorpseTarget == corpse && a.EatingCorpse, "hungry cannibal starts automatically"); a.TickDuty(5);
            Check(corpse.Data.count == 1 && corpse.Available && corpse.Handler == null, "eat one body from pile");
            corpse.Tick(300);
            corpse = Body(a, CorpseKind.Wildlife); Check(!a.StartCorpseWork(corpse, true), "wildlife not cannibal food"); corpse.Tick(300);

            // Actual casualties and downing: living downed commanders must not become bodies.
            foreach (var c in cs) Reset(c);
            b.Traits.TryAdd(CommanderTrait.Undertaker);
            Warp(victim, b.Position + Vector3.right);
            var before = Corpse.All.Count;
            victim.TakeDamage(10000);
            Check(!victim.IsDead && victim.PersonalHealth == 0 && Corpse.All.Count == before, "nonfatal downing leaves no corpse");
            Near(Mood(b, "Downed: " + victim.CommanderName), -4, "undertaker halves downing mood");
            Reset(victim);
            var options = GameSession.Instance.Options.Clone(); options.commanderDeath = CommanderDeathMode.Harsh; GameSession.Instance.SetOptions(options);
            int seed = 0; for (; seed < 100; seed++) { UnityEngine.Random.InitState(seed); if (UnityEngine.Random.value < .3f) break; }
            UnityEngine.Random.InitState(seed); victim.TakeDamage(10000);
            Check(victim.IsDead && Corpse.All.Count == before + 1 && Corpse.All.Last().Data.kind == CorpseKind.ColonyCommander, "fatal commander body");
            Near(Mood(b, "Death: " + victim.CommanderName), -5, "undertaker halves death mood");
            var count = Corpse.All.Count; victim.TakeDamage(10000); Check(Corpse.All.Count == count, "death idempotent");
            foreach (var body in Corpse.All.ToArray()) body.Tick(300);

            var campGo = new GameObject("Corpse check camp"); var camp = campGo.AddComponent<PrisonerCamp>(); campGo.transform.position = a.Position;
            var enemyGo = new GameObject("Capture check"); enemyGo.transform.position = a.Position;
            var enemy = enemyGo.AddComponent<EnemyCommander>(); enemy.ConfigureCommander("포로 검사", CommanderRank.Sergeant, new[] { UnitRole.Worker }, new CommanderTraits());
            enemy.TakeDamage(10000);
            Check(enemy.WasCaptured && Corpse.All.Count == 0, "captured enemy leaves no body");
            Check(camp.Execute(camp.Count - 1) && Corpse.All.Count == 1, "executed prisoner leaves body");
            Check(!camp.Execute(-1) && Corpse.All.Count == 1, "execution idempotent");
            Object.Destroy(campGo); Object.Destroy(enemyGo); foreach (var body in Corpse.All.ToArray()) body.Tick(300); await Task.Delay(50);

            // Save one partly consumed pile and an in-progress cleaning job, then reload the real scene.
            Reset(a); a.Traits.TryAdd(CommanderTrait.Undertaker);
            foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
            corpse = Body(a, count: 2); corpse.Priority = true; corpse.Tick(40);
            Check(a.StartCorpseWork(corpse), "save work started"); a.TickDuty(2);
            var meatBody = Corpse.Spawn(new Corpse.State { name = "사냥 검사", position = a.Position + Vector3.right * 3, kind = CorpseKind.Wildlife, food = 20 });
            meatBody.Tick(10); var meat = meatBody.GetComponent<ResourceNode>(); meat.Extract(3); meat.GatheringForbidden = true;
            var workerId = a.PersonalState.id;
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "valid corpse save: " + error);
            Check(file.corpses.Count == 2 && file.corpses[0].workerId == workerId, "corpse captured with worker");
            Check(!file.nodes.Any(n => (n.position.ToVector3() - meatBody.Position).sqrMagnitude < .01f), "hunted food not duplicated in node saves");
            var json = JsonUtility.ToJson(file); var path = Path.Combine(SaveStorage.Root, "corpse.json"); SaveStorage.WriteAtomic(path, json);
            Check(SaveSystem.TryLoad(path, out error), "load accepted: " + error); await Ready(); await Task.Delay(100);
            a = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == workerId); corpse = Corpse.All.Single(c => c.Data.kind == CorpseKind.Ant);
            Check(corpse.Priority && corpse.Data.count == 2 && corpse.Data.remaining <= 260 && corpse.Data.remaining > 259, "corpse state restored");
            Check(a.CorpseTarget == corpse && corpse.Handler == a, "worker reconnected"); Near(a.CorpseProgress, 2, "work progress restored");
            a.TickDuty(3); Check(a.CorpseTarget == null && !corpse.gameObject.activeSelf, "restored job completes once");
            meatBody = Corpse.All.Single(); meat = meatBody.GetComponent<ResourceNode>();
            Check(meat.AmountRemaining == 17 && meat.GatheringForbidden, "hunted food and designation restored");
            Check(ActivityIcons.Get("치우기") != null && ActivityIcons.Get("동족 포식") != null, "activity icons");
            Check(DetailTabs.Describe(a, 2).Contains("장의사"), "localized trait");
            GameMenuController.Instance.WorkSchedule();
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text == "치우기"), "cleaning work column");
            GameMenuController.Instance.Resume(); Time.timeScale = 0; WorkTargetPanel.Select(meatBody); await Task.Delay(100);
            Check(GameObject.Find("EatCorpse").GetComponentInChildren<UnityEngine.UI.Text>().text == "사냥 식량 운반", "hunted food action");
            GameObject.Find("TargetAction").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(meatBody.Priority, "priority button wired");

            var invalid = JsonUtility.FromJson<SaveFileV1>(json); invalid.corpses[0].remaining = float.NaN;
            Check(!SaveValidator.Validate(invalid, out _), "reject corrupt timer");
            invalid = JsonUtility.FromJson<SaveFileV1>(json); invalid.corpses.Add(invalid.corpses[0]);
            Check(!SaveValidator.Validate(invalid, out _), "reject duplicate worker claim");
            var legacy = JsonUtility.FromJson<SaveFileV1>(json); legacy.version = 10;
            foreach (var c in legacy.commanders) c.personalState.work.jobs &= ~CommanderJobs.Cleaning;
            Check(SaveValidator.Validate(legacy, out error) && legacy.version == SaveFileV1.CurrentVersion && legacy.corpses.Count == 0
                && legacy.commanders.All(c => (c.personalState.work.jobs & CommanderJobs.Cleaning) != 0), "v10 migration: " + error);
            await Interaction(a);
            return "PASS " + checks + " corpse, cleaning, traits, death/capture, UI, screen-ray input, real-time work, save/reload and migration checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}
