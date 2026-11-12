using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.AI;

// Disposable boundary fixture. Explicit illness/position inputs are not natural-play evidence.
public static class ThreeWaves1112MedicalChecks
{
    const string Dir = "work/three-waves-1112/";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log = "";
    static void Check(bool ok, string message)
    {
        log += (ok ? "PASS " : "FAIL ") + message + "\n";
        File.WriteAllText(Dir + "medical-checks.txt", log);
        if (!ok) throw new Exception(message);
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride) || Time.timeScale != 0)
            throw new Exception("Paused isolated fixture required");
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).Take(3).ToArray();
        Check(team.Length == 3, "three fixture commanders available");
        foreach (var c in team)
        {
            c.CommandStop(); c.PersonalState.health = new HealthState { migrated = true };
            c.WorkState.health = 100; c.PersonalState.mentalBreak = MentalBreak.None;
            c.PersonalState.moodFactors.RemoveAll(f => f.reason.StartsWith("Downed: "));
            if (!c.Agent.Warp(team[0].Position)) throw new Exception("Fixture warp failed");
        }
        var patient = team[0]; var observer = team[1];
        var tickStory = typeof(CommanderAnt).GetMethod("TickPersonalStory", Hidden);
        var storyActive = typeof(CommanderAnt).GetField("storyInjuryActive", Hidden);
        void Story(bool downed = false)
        {
            typeof(CommanderAnt).GetField("storyScanAt", Hidden).SetValue(patient, -1f);
            tickStory.Invoke(patient, new object[] { downed });
        }
        int ConcernCount() => observer.PersonalState.moodFactors.Count(f => f.reason == "Downed: " + patient.CommanderName);
        float ConcernRemaining() => observer.PersonalState.moodFactors.FirstOrDefault(f => f.reason == "Downed: " + patient.CommanderName)?.remaining ?? 0;
        patient.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .5f, immunity = .7f });
        storyActive.SetValue(patient, false); Story();
        Check(patient.Health.Serious && ConcernCount() == 0, "serious illness keeps medical priority without false downed mood");
        patient.Health.illnesses.Clear();
        patient.Health.wounds.Add(new Wound { part = BodyPart.ArmL, kind = WoundKind.Bruise, damage = 4 });
        Story(); var concern = observer.PersonalState.moodFactors.FirstOrDefault(f => f.reason == "Downed: " + patient.CommanderName);
        Check(concern != null, "actual major wound creates concern");
        concern.remaining = 90; Story();
        Check(ConcernCount() == 1 && ConcernRemaining() == 90, "same wound does not renew concern every tick");
        patient.RestorePersonalState(patient.CapturePersonalState()); Story();
        Check(ConcernCount() == 1 && ConcernRemaining() == 90, "loaded major wound does not duplicate concern");
        patient.Health.wounds.Clear(); Story(); patient.WorkState.health = 0; Story();
        Check(ConcernRemaining() == 180, "new actual downing still creates concern");
        patient.WorkState.health = 100;
        patient.Health.wounds.Clear(); Story();
        observer.PersonalState.moodFactors.RemoveAll(f => f.reason.StartsWith("Downed: "));
        var missing = new Wound { part = BodyPart.EyeR, kind = WoundKind.Missing, damage = MedicalRules.MaxHp(BodyPart.EyeR), age = MedicalRules.FreshMissingSeconds };
        patient.Health.wounds.Add(missing); Story();
        Check(ConcernCount() == 0, "old permanent loss does not repeatedly create concern");
        missing.age = 0; Story();
        Check(ConcernCount() == 1, "fresh loss creates concern even while patient remains conscious");

        var h = new HealthState();
        h.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .01f, immunity = 1 });
        MedicalRules.Tick(h, 1, false, false);
        Check(h.illnesses.Count == 0 && h.moldImmunityRemaining == GameCalendar.SecondsPerDay, "mold cure grants full one-day protection after tick");
        MedicalRules.Tick(h, 20, false, false);
        Check(h.moldImmunityRemaining == 130, "protection counts down with game time");
        var old = JsonUtility.FromJson<HealthState>("{\"bloodLoss\":0}");
        Check(old.moldImmunityRemaining == 0, "old save missing field defaults to zero");
        var wound = new HealthState();
        wound.illnesses.Add(new Illness { kind = IllnessKind.WoundInfection, severity = .01f, immunity = 1 });
        MedicalRules.Tick(wound, 1, false, false);
        Check(wound.illnesses.Count == 0 && wound.moldImmunityRemaining == 0, "wound infection recovery grants no mold protection");
        patient.PersonalState.health = h;
        patient.PersonalState.meal.satiety = 80;
        Check(patient.StableForRecovery, "post-cure protection is not an active illness and permits recovery");
        ColonyEvents.Infect(patient);
        Check(!patient.HasMold && !patient.CanCatchMold, "direct event infection rejects protected patient");
        observer.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, severity = .2f });
        var spread = typeof(CommanderAnt).GetMethod("TickInfection", Hidden);
        for (int i = 0; i < 100; i++) spread.Invoke(observer, new object[] { EventRules.InfectionSpreadSeconds });
        Check(!patient.HasMold && h.moldImmunityRemaining == 130, "repeated nearby exposure does not bypass protection");
        foreach (var c in team) { c.Health.illnesses.Clear(); c.Health.moldImmunityRemaining = 100; }
        var events = SceneObjects.Any<ColonyEvents>();
        typeof(ColonyEvents).GetField("state", Hidden).SetValue(events, new ColonyEvents.State());
        Check(!events.Eligible(ColonyEvent.Mold) && !events.TryTrigger(ColonyEvent.Mold), "all protected commanders suppress mold event without empty target");
        MedicalRules.Tick(patient.Health, 101, false, false);
        Check(events.Eligible(ColonyEvent.Mold), "same event state becomes eligible when one protection expires");
        Check(events.TryTrigger(ColonyEvent.Mold), "event selects the only susceptible commander");
        Check(patient.HasMold && patient.Health.moldImmunityRemaining == 0, "expired protection permits normal infection");
        patient.Health.illnesses.Clear(); patient.Health.moldImmunityRemaining = 73;
        var copy = patient.CapturePersonalState();
        Check(copy.Validate(out _) && copy.health.moldImmunityRemaining == 73, "personal state clone validates protected health");
        foreach (var bad in new[] { -1f, float.NaN, float.PositiveInfinity, MedicalRules.MoldImmunitySeconds + 1 })
        {
            copy.health.moldImmunityRemaining = bad;
            Check(!copy.Validate(out _), "save validator rejects protection=" + bad);
        }
        string id = patient.PersonalState.id;
        Check(SaveSystem.TrySave(false, 0, out var error), "normal save accepts protection: " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "normal load starts: " + error);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(100);
        SimulationPause.UserSpeed(0);
        var loaded = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == id);
        Check(!SaveSystem.Busy && loaded.Health.moldImmunityRemaining > 72 && loaded.Health.moldImmunityRemaining <= 73,
            "normal scene save/load preserves remaining protection");
        return log + "Discard fixture before natural continuation.";
    }
}
