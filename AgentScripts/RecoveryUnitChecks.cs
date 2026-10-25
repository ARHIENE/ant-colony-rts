using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;

public static class RecoveryUnitChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "RecoveryUnits-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            var initialDeadline = Time.realtimeSinceStartup + 50;
            while (SaveSystem.Busy && Time.realtimeSinceStartup < initialDeadline) await Task.Delay(50);
            Check(!SaveSystem.Busy, "initial scene ready");
            StartingCandidate[] team;
            do { team = Enumerable.Range(0, 2).Select(StartingCandidate.Roll).ToArray(); } while (StartingCandidate.Missing(team).Length > 0);
            SaveSystem.NewGame(new NewGameOptions { seed = 261025, mapSize = MapSize.Small, startingCount = 2, startingTeam = team });
            var deadline = Time.realtimeSinceStartup + 50;
            while (SaveSystem.Busy && Time.realtimeSinceStartup < deadline) await Task.Delay(50);
            Check(!SaveSystem.Busy, "new game ready");
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            await Task.Yield(); await Task.Yield();
            var commanders = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember).ToArray();
            Check(commanders.Length == 2, "two selected survivors spawn exactly twice: " + commanders.Length);
            var c = commanders.First(x => x.CanDoJob(AntColony.Units.CommanderJobs.Gathering));
            var meshes = c.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).ToArray();
            var bounds = meshes.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var expectedHeight = 1.6f * (.9f + c.PersonalState.appearanceSeed % 3 * .1f);
            Check(Mathf.Abs(bounds.size.y - expectedHeight) < .1f, "animated model height matches appearance " + bounds.size.y);
            Physics.SyncTransforms();
            var collider = c.GetComponent<CapsuleCollider>();
            Check(collider.bounds.max.y >= bounds.max.y - .1f && collider.bounds.min.y <= bounds.min.y + .1f, "click collider covers displayed character");
            var selection = UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            c.PersonalState.sleep.asleep = true;
            var point = UnityEngine.Camera.main.WorldToScreenPoint(bounds.center);
            typeof(AntColony.Units.SelectionManager).GetMethod("ClickSelectOrClear", Private).Invoke(selection, new object[] { (Vector2)point, false });
            Check(selection.GetSelectedObjects().Contains(c.GetComponent<AntColony.Units.SelectableObject>()), "click visible body selects survivor");
            Check(c.IsAsleep, "selection does not wake sleeper");
            c.PersonalState.sleep.asleep = false;
            c.PersonalState.AddMood("recovery check", -90, 999);
            var mood = c.Mood;
            var upkeep = UnityEngine.Object.FindAnyObjectByType<UpkeepManager>();
            var runCycle = typeof(UpkeepManager).GetMethod("RunCycle", Private);
            for (int i = 0; i < 3; i++) runCycle.Invoke(upkeep, null);
            Check(c.CarriedRationMeals > 0 && c.Mood == mood, "empty warehouse cannot starve a supplied survivor");
            Check(CommanderRoster.Instance.Commanders.Count(x => x.IsColonyMember) == 2, "two survivors remain after upkeep");
            c.PersonalState.moodFactors.RemoveAll(f => f.reason == "recovery check");
            var wood = AntColony.World.ResourceNode.Available.Where(n => n.ResourceType == AntColony.Data.ResourceType.Wood && n.CanGatherWith(c))
                .OrderBy(n => (n.transform.position - c.Position).sqrMagnitude).First();
            point = UnityEngine.Camera.main.WorldToScreenPoint(wood.GetComponent<Collider>().bounds.center);
            typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand", Private).Invoke(UnityEngine.Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>(), new object[] { (Vector2)point });
            Check(c.CurrentResourceNode == wood, "right-click orders wood gathering");
            AntColony.UI.GameMenuController.Instance.SetSpeed(1);
            AntColony.UI.GameMenuController.Instance.Science();
            Check(Time.timeScale > 0, "science panel leaves simulation running");
            AntColony.UI.GameMenuController.Instance.Resume();
            AntColony.UI.GameMenuController.Instance.Pause();
            Check(Time.timeScale == 0, "explicit pause menu pauses simulation");
            AntColony.UI.GameMenuController.Instance.Resume();
            return "PASS " + checks + " survivor input/start/supply checks";
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0; }
    }
}
