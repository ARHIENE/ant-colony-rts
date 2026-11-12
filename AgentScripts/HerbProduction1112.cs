using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

// Disposable integration fixture: free test storage/bench, but no herb/medicine
// grants, direct extraction, instant work, or scripted HP recovery.
public static class HerbProduction1112
{
    const string Dir = "work/three-waves-review-1112/";
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(100);
        if (SaveSystem.Busy) throw new Exception("Scene load timed out");
        Time.timeScale = 0;
    }
    static GameObject Facility(BuildingKind kind, Vector3 near)
    {
        if (!NavMesh.SamplePosition(near, out var hit, 8, NavMesh.AllAreas)) throw new Exception("No test facility ground");
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, hit.position, Quaternion.identity);
        go.name = "Herb integration " + kind; go.SetActive(true); return go;
    }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var log = new System.Text.StringBuilder();
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HerbProduction1112-" + Guid.NewGuid().ToString("N"));
            await Ready(); UnityEngine.Random.InitState(261106);
            SaveSystem.NewGame(new NewGameOptions { seed = 261106, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 3 });
            await Ready(); GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0);
            var team = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            var home = team[0].Position;
            Facility(BuildingKind.Storage, home + Vector3.right * 9);
            var bench = Facility(BuildingKind.MedicineBench, home + Vector3.forward * 9).GetComponent<Processor>();
            await Task.Delay(300);
            foreach (var c in team)
            {
                c.SetJobEnabled(CommanderJobs.All, false);
                c.SetJobEnabled(CommanderJobs.Gathering | CommanderJobs.Hauling | CommanderJobs.Nursing, true);
                c.Health.medicine = MedicineLevel.NoCare;
            }
            var herb = MaterialDeposits.Spawned.Where(n => n != null && n.ResourceType == ResourceType.Herb
                && team[0].TryWorkApproach(n.transform.position, out _)).OrderBy(n => (n.transform.position - home).sqrMagnitude).First();
            var initial = herb.AmountRemaining;
            if (!herb.RequestWork()) throw new Exception("Herb designation rejected");
            int recipe = Enumerable.Range(0, bench.Recipes.Count).First(i => bench.Recipes[i].output == ResourceType.MedicineBasic);
            if (!bench.AddOrder(recipe, Workshop.OrderMode.Count, 1)) throw new Exception("Medicine order rejected");
            SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(home);
            float start = GameCalendar.GameSeconds; var deadline = DateTime.UtcNow.AddSeconds(180);
            SimulationPause.UserSpeed(3);
            while (DateTime.UtcNow < deadline && GameCalendar.GameSeconds - start < 450)
            {
                var medicine = ResourceManager.Instance.GetAmount(ResourceType.MedicineBasic)
                    + ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.MedicineBasic).Sum(n => n.AmountRemaining)
                    + team.Where(c => c.CarriedType == ResourceType.MedicineBasic).Sum(c => c.CarriedAmount);
                if (medicine >= 1)
                {
                    if (!(herb.AmountRemaining < initial)) throw new Exception("Medicine produced without gathering the natural herb");
                    log.AppendLine("PASS normal designated wild herb gathering -> hauling -> medicine-bench crafting; medicine=" + medicine
                        + " herb=" + initial + "->" + herb.AmountRemaining + " gameSeconds=" + (GameCalendar.GameSeconds - start));
                    return log.ToString();
                }
                await Task.Delay(250);
            }
            throw new Exception("No medicine produced: " + bench.WaitReason + " herbStock=" + ResourceManager.Instance.GetAmount(ResourceType.Herb)
                + " node=" + herb.AmountRemaining + " crew=" + string.Join(";", team.Select(c => c.CommanderName + ":" + c.CurrentActivity + ":" + c.PersonalHealth)));
        }
        catch (Exception e) { log.AppendLine("FAIL " + e.Message); throw; }
        finally
        {
            SimulationPause.UserSpeed(0);
            ScreenCapture.CaptureScreenshot(Dir + "herb-production.png"); await Task.Delay(400);
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
            File.WriteAllText(Dir + "herb-production.txt", log.ToString());
        }
    }
}
