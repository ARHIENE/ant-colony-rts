using System;
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

// SAVE-only staging for screenshots. Run in a disposable Play session; never writes a save slot.
public static class SaveFeatureCapture
{
    static CommanderAnt patient;
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        if (SaveSystem.Busy) throw new Exception("Scene not ready");
        Time.timeScale = 0;
    }
    public static async Task<string> Prepare()
    {
        await Ready();
        var settings = UserSettings.Current.Clone(); settings.autoSaveEnabled = false; UserSettings.Apply(settings, false);
        SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250925 }); await Ready();
        patient = CommanderRoster.Instance.Commanders[0];
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { BuildingKind.Infirmary, UnitRole.Worker });
        var hospital = Object.Instantiate(template, patient.Position + Vector3.forward * 4, Quaternion.identity).GetComponent<Infirmary>();
        hospital.name = "Infirmary"; hospital.gameObject.SetActive(true);
        patient.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Serious, remaining = 240 });
        CampaignResearch.Instance.RestoreState(new CampaignResearch.State { completed = { (int)ScienceTechnology.Herbs, (int)ScienceTechnology.Infirmary } });
        if (!hospital.TryAdmit(patient)) throw new Exception("Could not admit patient");
        patient.TickPersonal(60);
        GameMenuController.Instance.Details(patient);
        await Task.Delay(150); Canvas.ForceUpdateCanvases();
        foreach (var scroll in Object.FindObjectsByType<ScrollRect>()) if (scroll.isActiveAndEnabled) scroll.verticalNormalizedPosition = 0;
        return "Treatment active, 180 seconds remaining. Capture composited screen with UI.";
    }
    public static async Task<string> StorageBefore()
    {
        GameMenuController.Instance.Resume(); Time.timeScale = 0;
        await Task.Delay(100);
        return Capacities();
    }
    public static async Task<string> StorageAfter()
    {
        var state = CampaignResearch.Instance.CaptureState();
        state.completed.Add((int)ScienceTechnology.FungalFarming);
        state.completed.Add((int)ScienceTechnology.Fermentation);
        CampaignResearch.Instance.RestoreState(state);
        await Task.Delay(100);
        return Capacities();
    }
    static string Capacities() => string.Join(", ", new[] { AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.Soil, AntColony.Data.ResourceType.Special }
        .Select(t => t + " capacity=" + ResourceManager.Instance.GetCapacity(t)));
}
