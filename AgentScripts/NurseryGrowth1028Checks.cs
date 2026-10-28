using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using FoodResource = AntColony.Data.ResourceType;
public static class NurseryGrowth1028Checks
{
    static int count;
    static void Check(bool value, string label) { if (!value) throw new Exception("Nursery " + count + ": " + label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main(bool baseline = false)
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "NurseryGrowth-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small, startingCount = 3 }); await Ready();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var roster = CommanderRoster.Instance; var a = roster.Commanders[0]; var b = roster.Commanders[1];
            a.CommandStop(); b.CommandStop(); a.transform.position = b.transform.position;
            a.PersonalState.ageMonths = b.PersonalState.ageMonths = CommanderAnt.AdultAgeMonths;
            a.PersonalState.ageScaleVersion = b.PersonalState.ageScaleVersion = 1;
            a.PersonalState.Relation(b.PersonalState.id).value = b.PersonalState.Relation(a.PersonalState.id).value = 70;
            ResourceManager.Instance.AddCapacity(FoodResource.Food, 1000); ResourceManager.Instance.Add(FoodResource.Food, 1000);
            var nursery = new GameObject("Nursery growth check").AddComponent<NurseryChamber>(); nursery.transform.position = b.Position;
            nursery.Tick(30); Check(nursery.BirthCount == 1, "first birth"); nursery.Tick(30);
            var report = "two 30-second ticks produced " + nursery.BirthCount + " births";
            if (baseline) return "BASELINE " + report;
            Check(nursery.BirthCount == 1, "recovery blocks repeated birth");
            Check(a.PersonalState.birthRecoverySeconds == GameBalance.BirthRecoverySeconds && b.PersonalState.birthRecoverySeconds == GameBalance.BirthRecoverySeconds, "both parents recover");
            var ageTick = typeof(CommanderAnt).GetMethod("TickAge", BindingFlags.Instance | BindingFlags.NonPublic);
            ageTick.Invoke(a, new object[] { GameBalance.BirthRecoverySeconds - 1 });
            ageTick.Invoke(b, new object[] { GameBalance.BirthRecoverySeconds - 1 }); nursery.Tick(30);
            Check(nursery.BirthCount == 1, "remaining one second still blocks");
            ageTick.Invoke(a, new object[] { 1f }); ageTick.Invoke(b, new object[] { 1f }); nursery.Tick(30);
            Check(nursery.BirthCount == 2, "new birth after recovery");
            var state = SaveSnapshot.Capture(); Check(SaveValidator.Validate(state, out var error), "valid saved recovery " + error);
            var id = a.PersonalState.id;
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "roundtrip " + error); await Ready();
            a = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == id);
            Check(a.PersonalState.birthRecoverySeconds == GameBalance.BirthRecoverySeconds, "recovery survives real save/load");
            b = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id != id && !c.IsChild);
            a.PersonalState.birthRecoverySeconds = b.PersonalState.birthRecoverySeconds = 0;
            a.PersonalState.Relation(b.PersonalState.id).value = b.PersonalState.Relation(a.PersonalState.id).value = 70;
            a.SetBorn(); Check(NurseryChamber.BreedMultiplier(a, b) == 0, "children cannot become parents");
            a.PersonalState.birthRecoverySeconds = float.NaN;
            Check(!a.PersonalState.Validate(out error), "nonfinite recovery rejected");
            return "PASS " + count + " nursery checks; " + report;
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
