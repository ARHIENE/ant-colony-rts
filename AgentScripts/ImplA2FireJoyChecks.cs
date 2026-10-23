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
using UnityEngine.AI;
using Object = UnityEngine.Object;

// A2 G54/G56/G57. 실제 Play 실행은 Claude가 맡는다. 임시 저장 경로만 사용한다.
public static class ImplA2FireJoyChecks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL A2 fire/joy: " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static void JoyTick(CommanderAnt c, float seconds) => typeof(CommanderAnt).GetMethod("TickJoy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void Bored(CommanderAnt c)
    {
        c.CommandStop(); c.PersonalState.joy = new CommanderJoyState { joy = 20 };
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplA2FireJoy-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var team = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            CrashLandingStart.Begin(new NewGameOptions { startingCount = 3, startingTeam = team, seed = 261023, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0; GameSession.Instance.MarkStarted(0, 10);
            var cs = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            Check(cs.Length >= 2, "two commanders for minimum users test"); var c = cs[0]; var d = cs[1];
            foreach (var commander in cs) { commander.CommandStop(); commander.SetJobEnabled(CommanderJobs.All, false); commander.Traits.values.Clear(); }

            var old = new CommanderWorkState { jobs = (CommanderJobs)32767, priorities = Enumerable.Range(0, 15).Select(i => i % 9 + 1).ToArray() };
            var previous = old.priorities.ToArray(); Check(old.Valid, "old 15 jobs validate and migrate"); old.MigrateLegacyJobs();
            Check(old.priorities.Length == 16 && old.priorities.Take(15).SequenceEqual(previous) && old.Priority(CommanderJobs.Firefighting) == 9, "15 priorities preserved, firefighting 9");
            Check(new CommanderWorkState().Priority(CommanderJobs.Firefighting) == 9 && CommanderWorkState.JobCount == 16, "new job defaults");
            var disabled = new CommanderWorkState { jobs = CommanderJobs.All & ~CommanderJobs.Firefighting };
            disabled = JsonUtility.FromJson<CommanderWorkState>(JsonUtility.ToJson(disabled));
            Check(disabled.Valid && disabled.Priority(CommanderJobs.Firefighting) == 0, "explicit firefighting disabled survives roundtrip");
            GameMenuController.Instance.WorkSchedule();
            Check(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b => b.name == "Priority Firefighting"), "firefighting UI column");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;

            foreach (var s in RecreationSpot.All.ToArray()) s.gameObject.SetActive(false);
            Bored(c); JoyTick(c, .01f); Check(c.IsPlaying && c.PersonalState.joy.freeWalk, "free walking without facility");
            var joy = c.Joy; JoyTick(c, GameBalance.PlaySeconds);
            Check(Mathf.Abs(c.Joy - (joy - GameBalance.JoyPerSecond * GameBalance.PlaySeconds + GameBalance.PlayJoy * .5f)) < .02f, "free walking recovers 50 percent");

            var camp = Build<RecreationSpot>(BuildingKind.Campfire, c.Position + Vector3.right * 2);
            Check(camp.MinimumUsers == 1 && camp.Seats == 6, "campfire 1 / 6");
            Bored(c); JoyTick(c, .01f); Check(c.PlaySpot == camp, "solo campfire starts"); joy = c.Joy; JoyTick(c, GameBalance.PlaySeconds);
            Check(Mathf.Abs(c.Joy - (joy - GameBalance.JoyPerSecond * GameBalance.PlaySeconds + GameBalance.PlayJoy)) < .02f, "solo campfire full recovery");
            camp.gameObject.SetActive(false);
            var gamble = Build<RecreationSpot>(BuildingKind.GamblingDen, c.Position + Vector3.right * 2);
            Check(gamble.MinimumUsers == 2 && gamble.Seats == 4, "gambling 2 / 4");
            Bored(c); JoyTick(c, .01f); float remain = c.PersonalState.joy.playSeconds; JoyTick(c, 2);
            Check(c.PersonalState.joy.playSeconds == remain && !c.PersonalState.joy.groupStarted, "solo gambling waits");
            Check(NavMesh.SamplePosition(c.Position, out var near, 4, NavMesh.AllAreas) && d.Agent.Warp(near.position), "partner nearby");
            Bored(d); JoyTick(d, .01f); JoyTick(c, .01f); JoyTick(d, .01f);
            Check(c.PersonalState.joy.groupStarted && d.PersonalState.joy.groupStarted, "two start gambling");
            var savedJoy = JsonUtility.FromJson<CommanderJoyState>(JsonUtility.ToJson(c.PersonalState.joy));
            Check(savedJoy.Valid && savedJoy.groupStarted, "joy state roundtrip");
            JoyTick(c, GameBalance.PlaySeconds); JoyTick(d, GameBalance.PlaySeconds); gamble.gameObject.SetActive(false);

            c.PersonalState.joy.joy = 100; c.WorkState.duty = CommanderDuty.Civilian; c.CommandStop();
            var wall = Build<Wall>(BuildingKind.LeafWall, c.Position + Vector3.forward * 2);
            Check(WallFire.Ignite(wall), "ignite"); var fire = wall.GetComponent<WallFire>();
            Check(fire.Managed, "home fire managed"); c.SetJobPriority(CommanderJobs.Firefighting, 9); c.TickDuty(2);
            Check(c.ServiceJob == CommanderJobs.Firefighting && c.ServiceTarget == wall, "autonomous firefighting");
            c.TickDuty(2.5f); Check(fire.Burning && Mathf.Abs(fire.ExtinguishProgress - .5f) < .01f, "half after 2.5s");
            var snapshot = fire.CaptureState(); fire.RestoreState(JsonUtility.FromJson<WallFire.State>(JsonUtility.ToJson(snapshot)));
            Check(Mathf.Abs(fire.ExtinguishProgress - .5f) < .01f, "fire state roundtrip");
            c.TickDuty(2.5f); Check(!fire.Burning, "extinguished after 5s without water or tools");
            Check(WallFire.Ignite(wall), "can reignite"); c.CommandStop(); c.WorkState.duty = CommanderDuty.Deployed; c.TickDuty(2);
            Check(c.ServiceTarget == null && fire.ExtinguishProgress == 0, "deployed does not automatically extinguish");
            Check(c.CommandExtinguish(fire), "explicit deployed firefighting"); c.TickDuty(5);
            Check(!fire.Burning, "explicit deployed finishes"); c.WorkState.duty = CommanderDuty.Civilian;
            Object.Destroy(wall.gameObject); Object.Destroy(camp.gameObject); Object.Destroy(gamble.gameObject);
            return "PASS ImplA2FireJoyChecks " + checks;
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
