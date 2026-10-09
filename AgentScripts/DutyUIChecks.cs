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
using AntSelectionManager = AntColony.Units.SelectionManager;

public static class DutyUIChecks
{
    static int checks;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL " + checks + ": " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static Button Button(string name) => GameMenuController.Instance.GetComponentsInChildren<Button>().SingleOrDefault(b => b.name == name)
        ?? Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
    static void Click(string name) { var b = Button(name); Check(b.interactable, name + " enabled"); b.onClick.Invoke(); }
    static Toggle[] Choices() => GameMenuController.Instance.GetComponentsInChildren<Toggle>().Where(t => t.name.StartsWith("Deploy ")).ToArray();
    static Slider Slider(Toggle t) => t.transform.parent.GetComponentInChildren<Slider>();
    static void Select(CommanderAnt c)
    {
        var selection = Object.FindFirstObjectByType<AntSelectionManager>(); selection.ClearSelection();
        typeof(AntSelectionManager).GetMethod("AddToSelection", Private).Invoke(selection, new object[] { c.GetComponent<AntColony.Units.SelectableObject>() });
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "DutyUI-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready();
            var testSettings = settings.Clone(); testSettings.pauseSimulationOnMenu = true; testSettings.autoSaveEnabled = false; UserSettings.Apply(testSettings);
            var list = GameMenuController.SortedCommanders(); var c = list[0]; var other = list[1];
            foreach (var commander in list) { commander.SetJobEnabled(CommanderJobs.All, false); commander.CommandStop(); }
            var menu = GameMenuController.Instance;
            menu.Roster(); Click("Work Schedule"); await Task.Delay(100);
            Check(menu.ScreenName == "작업표" && Time.timeScale == 0, "work schedule opens with menu pause setting");
            var toggles = menu.GetComponentsInChildren<Toggle>();
            Check(toggles.Length == list.Length * 14, "14 jobs for every commander");
            var jobs = new[] { CommanderJobs.Nursing, CommanderJobs.Repair, CommanderJobs.Cleaning, CommanderJobs.Building, CommanderJobs.Art, CommanderJobs.Crafting,
                CommanderJobs.Research, CommanderJobs.Administration, CommanderJobs.Cooking, CommanderJobs.Hunting, CommanderJobs.Hauling, CommanderJobs.Farming, CommanderJobs.Fishing, CommanderJobs.Gathering }; // 작업표 왼쪽부터 우선순위 순
            for (var i = 0; i < jobs.Length; i++)
            {
                toggles[i].isOn = true; Check(c.AllowsJob(jobs[i]), "checkbox enables " + jobs[i]);
                toggles[i].isOn = false; Check(!c.AllowsJob(jobs[i]), "checkbox disables " + jobs[i]);
            }
            Check(!menu.GetComponentsInChildren<Text>().Any(t => t.text.Contains("쓰러짐")), "healthy civilian is not downed");
            Click("Close Duty"); await Task.Delay(100);
            BuildScreen.Open(); await Task.Delay(100); Click("Tab 군사"); await Task.Delay(100);
            Click("Build ConscriptionPost"); await Task.Delay(100);
            Check(BuildScreen.Picking, "conscription construction is reachable");
            while (BuildScreen.Back()) { }
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.ConscriptionPost, UnitRole.Worker });
            var go = Object.Instantiate(template, c.Position + Vector3.right * 5, Quaternion.identity); go.SetActive(true);
            var post = go.GetComponent<ConscriptionPost>();
            // Phase 4 병역 상한(모병제 5%)에 막히지 않게 국민개병(40%)으로 둔다.
            var granted = CampaignResearch.Instance.CaptureState(); granted.completed.Add((int)ScienceTechnology.TotalMobilization); CampaignResearch.Instance.RestoreState(granted);
            Check(ColonyPopulation.Instance.TrySetPolicy(MilitaryPolicy.Total), "total mobilization policy");
            menu.OpenConscription(); await Task.Delay(100);
            Check(menu.ScreenName == "징집소" && !Button("Deploy Formation").interactable, "empty formation blocked");
            var choices = Choices();
            Check(choices.Length == list.Length, "formation lists every commander");
            foreach (var t in choices) { t.isOn = true; Slider(t).value = Slider(t).maxValue; }
            await Task.Delay(100);
            Check(!Button("Deploy Formation").interactable, "not enough free ants blocked");
            foreach (var t in choices) t.isOn = false;
            choices[0].isOn = true; Slider(choices[0]).value = 4;
            choices[1].isOn = true; Slider(choices[1]).value = 3;
            await Task.Delay(100); var free = AntPool.Instance.Free; Click("Deploy Formation"); await Task.Delay(100);
            Check(c.IsDeployed && other.IsDeployed && c.TroopCount == 4 && other.TroopCount == 3 && AntPool.Instance.Free == free - 7, "UI formation deploys atomically");
            Select(c); await Task.Delay(100);
            var texts = HudConsole.Center.GetComponentsInChildren<Text>();
            Check(texts.Any(t => t.text.Contains("체력") && t.text.Contains("병력")), "personal health and troops shown separately");
            Click("Return To Post"); Check(c.IsReturning, "return button orders return");
            c.Agent.Warp(c.WorkState.returnPosition); c.TickDuty(.1f); await Task.Delay(100);
            Check(!c.IsDeployed && AntPool.Instance.Free == free - 3, "return refunds survivors");
            Check(!HudConsole.Center.GetComponentsInChildren<Text>().Any(t => t.text.Contains("병력")), "civilian troop count hidden"); // HUD v4: 병력은 체력 줄에 출전 중만 표시
            Check(!Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.name == "Return To Post"), "civilian return hidden");
            other.ReturnToPost(); other.Agent.Warp(other.WorkState.returnPosition); other.TickDuty(.1f);
            menu.ShowConscription(post); await Task.Delay(100); choices = Choices(); choices[0].isOn = true;
            c.WorkState.health = 0; await Task.Delay(100);
            Check(!choices[0].interactable && !choices[0].isOn && !Button("Deploy Formation").interactable, "stale downed choice removed");
            c.WorkState.health = GameBalance.CommanderHealth; await Task.Delay(100);
            choices[0].isOn = true; await Task.Delay(100); post.gameObject.SetActive(false); await Task.Delay(100);
            Check(!Button("Deploy Formation").interactable, "unavailable post disables deployment");
            post.gameObject.SetActive(true); menu.Resume();
            c.SetJobEnabled(CommanderJobs.Farming, true);
            Check(SaveSystem.TrySave(false, 0, out var error), "save schedule: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load schedule: " + error); await Ready();
            menu.WorkSchedule(); await Task.Delay(100);
            Check(menu.GetComponentsInChildren<Toggle>()[11].isOn, "work checkbox restored from save"); // 농사 = 작업표 12번째 칸(행정 추가)
            Canvas.ForceUpdateCanvases();
            var schedule = menu.GetComponentsInChildren<RectTransform>().Single(r => r.name == "WorkSchedule");
            foreach (var t in menu.GetComponentsInChildren<Toggle>().Take(6))
            {
                var corners = new Vector3[4]; ((RectTransform)t.transform).GetWorldCorners(corners);
                Check(corners.All(v => schedule.rect.Contains(schedule.InverseTransformPoint(v))), "checkbox inside panel bounds");
            }
            menu.Resume();
            return "PASS DutyUIChecks " + checks;
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings); Time.timeScale = 0; }
    }
}
