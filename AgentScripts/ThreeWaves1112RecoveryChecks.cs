using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.UI;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Save;
using UnityEngine;
using UnityEngine.AI;

// Disposable regression fixture. Health, sleep and time steps below are test inputs, not playthrough results.
public static class ThreeWaves1112RecoveryChecks
{
    const string Dir = "work/three-waves-1112/";
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log = "";
    static void Check(bool pass, string name)
    {
        log += (pass ? "PASS " : "FAIL ") + name + "\n";
        File.WriteAllText(Dir + "recovery-checks.txt", log);
        if (!pass) throw new Exception(name);
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Isolated Play fixture required");
        SimulationPause.UserSpeed(0);
        var c = CommanderRoster.Instance.Commanders.First(x => x != null && !x.IsDead);
        var recovery = typeof(CommanderAnt).GetMethod("TickRecovery", Hidden);
        void Tick(float dt) => recovery.Invoke(c, new object[] { dt });
        void Reset()
        {
            c.CommandStop(); c.PersonalState.health = new HealthState { migrated = true, selfTend = false };
            c.PersonalState.mentalBreak = MentalBreak.None; c.PersonalState.rageRemaining = 0;
            c.PersonalState.sleep.asleep = false; c.PersonalState.meal.satiety = 100;
            c.PersonalState.lastCombatSeconds = GameBalance.AutoReturnSeconds;
            c.WorkState.duty = CommanderDuty.Civilian; c.WorkState.resting = false; c.WorkState.health = 50;
            c.PersonalState.joy.playSeconds = 0; c.PersonalState.joy.joy = 100;
        }
        Reset(); Tick(GameCalendar.SecondsPerDay);
        Check(Mathf.Abs(c.PersonalHealth - 70) < .01f, "stable living commander recovers 20 HP per day");
        Reset(); c.PersonalState.sleep.asleep = true; Tick(GameCalendar.SecondsPerDay);
        Check(Mathf.Abs(c.PersonalHealth - 90) < .01f, "sleep doubles personal recovery");
        Reset(); c.WorkState.resting = true; Tick(GameCalendar.SecondsPerDay);
        Check(Mathf.Abs(c.PersonalHealth - 90) < .01f, "rest order doubles personal recovery");
        Reset(); c.WorkState.health = 99; Tick(GameCalendar.SecondsPerDay);
        Check(c.PersonalHealth == GameBalance.CommanderHealth, "recovery clamps at maximum health");
        Reset(); c.PersonalState.lastCombatSeconds = 0; Tick(10);
        Check(c.PersonalHealth == 50, "recent damage prevents recovery");
        Reset(); c.PersonalState.meal.satiety = 0; Tick(30);
        Check(c.PersonalHealth == 50, "starvation prevents recovery");
        Reset(); c.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .1f }); Tick(30);
        Check(c.PersonalHealth == 50, "illness prevents recovery");
        Reset(); c.Health.wounds.Add(new Wound { part = BodyPart.ArmL, kind = WoundKind.Cut, damage = 2 }); Tick(30);
        Check(c.PersonalHealth == 50, "bleeding or untreated wound prevents recovery");
        Reset(); c.Health.wounds.Add(new Wound { part = BodyPart.ArmL, kind = WoundKind.Bruise, damage = 1, tended = true }); Tick(30);
        Check(c.PersonalHealth > 50, "tended minor wound permits stable recovery");
        Reset(); c.WorkState.duty = CommanderDuty.Deployed; Tick(30);
        Check(c.PersonalHealth == 50, "deployed commander cannot heal through mobilize loop");
        Reset(); c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 0; Tick(GameCalendar.SecondsPerDay);
        Check(c.PersonalHealth == 20, "existing stable unconscious recovery remains 20 HP");
        Reset(); c.WorkState.health = 0; c.WorkState.unconsciousSeconds = 0;
        c.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .1f }); Tick(GameCalendar.SecondsPerDay);
        Check(c.PersonalHealth == 0, "ill unconscious commander is not revived by new recovery");
        Reset(); c.PersonalState.joy.playSeconds = 10; c.PersonalState.joy.freeWalk = true;
        NavMesh.SamplePosition(c.Position + Vector3.right * 4, out var move, 4, NavMesh.AllAreas);
        c.CommandMove(move.position);
        Check(!c.IsPlaying && !c.PersonalState.joy.freeWalk, "direct movement leaves old free-walk state");
        Reset(); c.PersonalState.joy.playSeconds = 10; c.PersonalState.joy.freeWalk = true;
        c.CommandAttackMove(move.position);
        Check(!c.IsPlaying && c.IsFollowingCombatOrder, "attack-move leaves recreation without cancelling combat order");
        Check(CommanderOverhead.Activity(c) == "공격 이동", "attack-move label overrides idle/needs labels");
        Tick(30); Check(c.PersonalHealth == 50, "combat order prevents recovery even in peace");
        c.TickDuty(.1f);
        Check(c.IsFollowingCombatOrder && !c.IsPlaying, "joy tick cannot replace an attack-move order");
        Reset();
        var button = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Exclude).First(b => b.name == "WaveAction");
        button.onClick.Invoke(); await Task.Delay(300);
        Check(WaveDefense.Instance.InWave, "normal wave action creates active defense");
        var enemy = SceneObjects.Any<LocalIncursions>().Visitors.First(e => e != null && !e.IsDead);
        Reset(); Tick(30); Check(c.PersonalHealth == 50, "active wave prevents recovery");
        c.PersonalState.joy.playSeconds = 10; c.PersonalState.joy.freeWalk = true;
        c.CommandAttack(enemy);
        Check(!c.IsPlaying && c.IsInCombat && CommanderOverhead.Activity(c) == "교전", "direct attack clears recreation and displays combat");
        c.CommandStop(); NavMesh.SamplePosition(enemy.Position + Vector3.right * 2, out var close, 4, NavMesh.AllAreas);
        Check(c.Agent.Warp(close.position), "fixture moves commander beside an actual wave enemy");
        c.PersonalState.sleep.asleep = true; c.PersonalState.joy.playSeconds = 10; c.PersonalState.joy.freeWalk = true;
        c.TickDuty(.1f);
        Check(!c.IsAsleep && !c.IsPlaying, "nearby wave threat interrupts sleep and recreation without a repeated order");
        var node = ResourceNode.Available.First(n => n.NeedsDesignation && !n.IsDepleted && n.CanGatherWith(c));
        c.CommandGather(node);
        Check(c.IsWorking, "fixture begins an actual gathering order beside the threat");
        c.TickDuty(.1f);
        Check(!c.IsWorking && c.CurrentResourceNode == null, "nearby threat releases worker state so automatic combat can run");
        c.CommandAttack(enemy);
        var attackTimer = typeof(SoldierAnt).GetField("attackTimer", Hidden);
        attackTimer.SetValue(c, .75f); c.TickDuty(.1f);
        Check(c.IsInCombat && Mathf.Abs((float)attackTimer.GetValue(c) - .75f) < .001f, "defense guard preserves an existing attack and its cooldown");
        SceneObjects.Any<AntColony.Camera.IsometricCameraController>().FocusOn(c.Position);
        await Task.Delay(1100); ScreenCapture.CaptureScreenshot(Dir + "screens/regression-combat.png"); await Task.Delay(300);
        return log + "Discard this fixture before resuming actual play.";
    }
}
