using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.UI;
using AntColony.Save;
using UnityEngine;

public static class ThreeWaves1112BreakChecks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log = "";
    static void Check(bool ok, string message)
    {
        log += (ok ? "PASS " : "FAIL ") + message + "\n";
        File.WriteAllText("work/three-waves-1112/break-checks.txt", log);
        if (!ok) throw new Exception(message);
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride)) throw new Exception("Isolated fixture required");
        SimulationPause.UserSpeed(0);
        var settings = UserSettings.Current.Clone();
        var c = CommanderRoster.Instance.Commanders.First();
        var other = CommanderRoster.Instance.Commanders.Skip(1).First();
        var recovery = typeof(CommanderAnt).GetMethod("TickRecovery", Hidden);
        var tickBreak = typeof(CommanderAnt).GetMethod("TickBreak", Hidden);
        try
        {
            c.CommandStop(); c.PersonalState.health = new HealthState { migrated = true };
            c.PersonalState.mentalBreak = MentalBreak.None; c.WorkState.health = 0;
            var food = ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food);
            c.StartMentalBreak(MentalBreak.Binge);
            Check(c.PersonalState.mentalBreak == MentalBreak.None && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) == food && !c.PersonalState.moodFactors.Any(f => f.reason == "Catharsis"), "unconscious commander cannot start binge or gain catharsis");
            c.WorkState.health = 50; c.Health.bloodLoss = .99f;
            c.StartMentalBreak(MentalBreak.AttackCommander);
            Check(c.Health.Downed && c.PersonalState.mentalBreak == MentalBreak.None, "clinically downed commander cannot start violence");
            c.Health.bloodLoss = 0; c.PersonalState.meal.satiety = 80;
            c.PersonalState.lastCombatSeconds = 100; c.PersonalState.mentalBreak = MentalBreak.AttackCommander;
            recovery.Invoke(c, new object[] { 30f });
            Check(c.PersonalHealth == 50, "violent breakdown prevents personal recovery");
            c.PersonalState.mentalBreak = MentalBreak.None; c.PersonalState.rageRemaining = 10;
            recovery.Invoke(c, new object[] { 30f });
            Check(c.PersonalHealth == 50, "revenge rage prevents personal recovery");
            c.PersonalState.rageRemaining = 0; c.PersonalState.mentalBreak = MentalBreak.Idle;
            recovery.Invoke(c, new object[] { 30f });
            Check(c.PersonalHealth > 50, "nonviolent idle breakdown does not disable physical healing");
            c.PersonalState.mentalBreak = MentalBreak.AttackCommander; c.PersonalState.breakRemaining = 40; c.WorkState.health = 0;
            c.PersonalState.moodFactors.RemoveAll(f => f.reason == "Catharsis");
            tickBreak.Invoke(c, new object[] { 1f });
            Check(c.PersonalState.mentalBreak == MentalBreak.None && !c.PersonalState.moodFactors.Any(f => f.reason == "Catharsis"), "incapacitation cancels ongoing violence without catharsis reward");
            c.WorkState.health = 50; c.PersonalState.joy.playSeconds = 10; c.PersonalState.joy.freeWalk = true; c.PersonalState.sleep.asleep = true;
            var enabled = settings.Clone(); enabled.autoPauseThreat = true; UserSettings.Apply(enabled, false);
            SimulationPause.Restore(new SimulationPause.State { speed = 1 });
            c.StartMentalBreak(MentalBreak.AttackCommander);
            Check(Time.timeScale == 0 && SimulationPause.Reasons.Any(r => r.StartsWith("incident:break:" + c.PersonalState.id)), "violent breakdown immediately obeys enabled threat auto-pause");
            Check(!c.IsAsleep && !c.IsPlaying, "violent breakdown leaves sleeping and recreation states");
            Check(CommanderOverhead.Activity(c) == "동료 공격", "overhead and selection state identify friendly attack in Korean");
            await Task.Delay(1100); ScreenCapture.CaptureScreenshot("work/three-waves-1112/screens/break-alert.png"); await Task.Delay(300);
            Check(ToastManager.VisibleLines.Any(t => t.Contains("동료 공격") && t.Contains("피해")), "visible threat gives a concrete response");
            Check(ToastManager.VisibleLines.Count(t => t.Contains(c.CommanderName + ": 동료 공격")) == 1, "violent breakdown has one alert instead of duplicate toast and incident");
            SimulationPause.UserSpeed(1); ThreatIncidents.Scan(true);
            Check(!SimulationPause.Reasons.Any(r => r.StartsWith("incident:break:")), "repeated scan of same breakdown does not pause again after resume");
            SimulationPause.UserSpeed(0);
            c.PersonalState.mentalBreak = MentalBreak.None; ThreatIncidents.Scan(false);
            Check(!SimulationPause.Capture().active.Any(a => a == "break:" + c.PersonalState.id), "ended breakdown clears active threat tracking");
            other.CommandStop(); other.WorkState.health = 50; other.PersonalState.health = new HealthState { migrated = true };
            other.PersonalState.mentalBreak = MentalBreak.None;
            var disabled = settings.Clone(); disabled.autoPauseThreat = false; UserSettings.Apply(disabled, false);
            SimulationPause.Restore(new SimulationPause.State { speed = 1 });
            other.StartMentalBreak(MentalBreak.AttackBuilding);
            Check(!SimulationPause.Reasons.Any(r => r.StartsWith("incident:break:" + other.PersonalState.id)), "disabled threat auto-pause setting remains respected");
            Check(CommanderOverhead.Activity(other) == "시설 공격", "building attack has its own readable state");
            SimulationPause.UserSpeed(0); other.PersonalState.mentalBreak = MentalBreak.None;
            c.PersonalState.mentalBreak = MentalBreak.AttackCommander; c.PersonalState.breakRemaining = 30;
            c.PersonalState.lastCombatSeconds = 100; c.Agent.Warp(other.Position);
            typeof(CommanderAnt).GetField("breakAttackTimer", Hidden).SetValue(c, 0f);
            var before = other.PersonalHealth; tickBreak.Invoke(c, new object[] { .1f });
            Check(other.PersonalHealth < before && c.PersonalState.lastCombatSeconds == 0, "actual violent hit records combat time and applies ordinary damage");
            c.PersonalState.mentalBreak = MentalBreak.None; var attackerHp = c.PersonalHealth;
            recovery.Invoke(c, new object[] { 10f });
            Check(c.PersonalHealth == attackerHp, "recent combat still delays healing after violence ends");
            return log + "Discard fixture before natural continuation.";
        }
        finally { UserSettings.Apply(settings, false); SimulationPause.UserSpeed(0); }
    }
}
