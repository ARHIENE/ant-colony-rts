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
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run in fresh Play mode via official unity command run_script. Player save slots stay untouched.
public static class InfirmaryChecks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static SaveFileV1 Clone(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    static Button Button(string prefix) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .First(b => b.isActiveAndEnabled && b.name.StartsWith(prefix));
    static void Injure(CommanderAnt c)
    {
        c.PersonalState.injuries.Clear();
        c.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Serious, remaining = 240 });
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "InfirmaryChecks-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250925, commanderDeath = CommanderDeathMode.Gentle });
            await Ready();
            var roster = CommanderRoster.Instance;
            var a = roster.Commanders[0]; var b = roster.Commanders[1]; var c = roster.Commanders[2];
            foreach (var commander in roster.Commanders) commander.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.Infirmary, UnitRole.Worker });
            var hospital = Object.Instantiate(template, a.Position + Vector3.forward * 3, Quaternion.identity).GetComponent<Infirmary>();
            hospital.name = "Infirmary"; hospital.gameObject.SetActive(true);
            Check(hospital.Data.foodCost == 40 && hospital.Data.soilCost == 40 && hospital.Data.constructionAnts == 4 && hospital.Data.buildTimeSeconds == 8, "construction costs match spec");
            Injure(a); Injure(b); Injure(c);
            Check(!hospital.TryAdmit(a), "research required");
            Check(!Object.FindFirstObjectByType<BuildingPlacementController>().BeginInfirmaryPlacement(), "placement research gate");
            a.TickPersonal(10); Check(a.PersonalState.injuries[0].remaining == 240, "untreated serious injury does not heal");
            CampaignResearch.Instance.RestoreState(new CampaignResearch.State { completed = { (int)ScienceTechnology.Herbs, (int)ScienceTechnology.Infirmary } });
            b.Agent.Warp(a.Position); c.Agent.Warp(a.Position);
            a.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Wings, severity = InjurySeverity.Permanent, remaining = 240 });
            GameMenuController.Instance.Details(a);
            Check(Button("Treat at ").interactable, "treatment UI enabled"); Button("Treat at ").onClick.Invoke();
            Check(a.TreatmentFacility == hospital && a.PersonalState.treating && !a.CanReceiveOrders, "UI admits and blocks orders");
            Check(hospital.TryAdmit(b) && !hospital.TryAdmit(c) && !hospital.TryAdmit(a), "two beds and no duplicate patient");
            var position = a.Position; a.CommandMove(position + Vector3.right * 10);
            Check(!a.Agent.hasPath && a.Position == position && !a.CanChangeAllocation, "treatment prevents move and allocation");
            a.TickPersonal(0); a.TickPersonal(float.NaN); a.TickPersonal(float.PositiveInfinity);
            Check(a.PersonalState.injuries[0].remaining == 240, "invalid and paused ticks do not heal");
            // Infirmary의 선행 연구 Herbs(약초 처방)가 완료돼 중상 치료가 4분 -> 3분(속도 4/3배)이다.
            a.TickPersonal(90); Check(Mathf.Abs(a.PersonalState.injuries[0].remaining - 120) < .01f, "treatment progresses (herbs 3 min)");
            Button("Stop treatment").onClick.Invoke();
            a.TickPersonal(10); Check(a.CanReceiveOrders && Mathf.Abs(a.PersonalState.injuries[0].remaining - 120) < .01f, "cancel preserves progress");
            Check(hospital.TryAdmit(a), "resume treatment");
            var troops = a.TroopCount; var loyalty = a.Traits.Loyalty;
            a.TickPersonal(120);
            Check(a.CanReceiveOrders && a.TreatmentFacility == null && a.PersonalState.injuries.Count == 1
                && a.PersonalState.injuries[0].severity == InjurySeverity.Permanent, "completion releases patient and preserves permanent injury");
            Check(a.Traits.Loyalty == loyalty + 3 && a.TroopCount == troops && !hospital.TryAdmit(a), "completion reward without troop healing");
            a.TickPersonal(1); Check(a.Traits.Loyalty == loyalty + 3, "completion reward once");
            b.StartMentalBreak(MentalBreak.Idle); Check(!b.PersonalState.treating && hospital.Patients.Count == 0, "break interrupts treatment");
            b.TickPersonal(61); Check(hospital.TryAdmit(b), "readmit after break");
            hospital.gameObject.SetActive(false); Check(!b.PersonalState.treating && b.CanReceiveOrders, "disabled building releases patients");
            hospital.gameObject.SetActive(true);
            b.ReturnTroops(b.TroopCount); Check(hospital.TryAdmit(b), "zero troop commander can recover");
            b.TickPersonal(45);
            foreach (var commander in roster.Commanders) commander.CommandStop();
            var expected = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(expected, out var error), "valid patient snapshot: " + error);
            var legacy = Clone(expected); legacy.buildings.RemoveAll(x => x.kind == "Infirmary");
            foreach (var commander in legacy.commanders) commander.personalState.treating = false;
            var legacyJson = System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(legacy), "\"patients\":\\[[^\\]]*\\],", "");
            Check(SaveValidator.TryParse(legacyJson, out _, out error), "old saves without patient fields: " + error);
            var broken = Clone(expected); broken.buildings.First(x => x.kind == "Infirmary").patients.Add(999);
            Check(!SaveValidator.Validate(broken, out _), "invalid patient ID rejected");
            broken = Clone(expected); var d = broken.buildings.First(x => x.kind == "Infirmary"); d.patients.Add(d.patients[0]);
            Check(!SaveValidator.Validate(broken, out _), "duplicate patient rejected");
            broken = Clone(expected); broken.commanders[1].location = 3;
            Check(!SaveValidator.Validate(broken, out _), "captive patient rejected");
            Check(SaveSystem.TrySave(false, 0, out error), "save accepted: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error); await Ready();
            hospital = Object.FindFirstObjectByType<Infirmary>(); b = hospital.Patients.Single();
            Check(b.PersonalState.treating && b.TreatmentFacility == hospital && !b.CanReceiveOrders, "patient ownership restored");
            Check(Mathf.Abs(b.PersonalState.injuries[0].remaining - 180) < 1, "remaining treatment saved");
            loyalty = b.Traits.Loyalty; b.TickPersonal(180);
            Check(b.CanReceiveOrders && b.PersonalState.injuries.Count == 0 && b.Traits.Loyalty == loyalty + 3, "loaded treatment finishes");
            Injure(b); Check(hospital.TryAdmit(b), "readmit for destroyed building check");
            hospital.TakeDamage(float.MaxValue); b.TickPersonal(10);
            Check(!b.PersonalState.treating && b.PersonalState.injuries[0].remaining == 240, "destroyed facility cannot heal before OnDisable");
            return "PASS " + checks + " infirmary checks; UI, capacity, recovery, interruptions, save/reload";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}
