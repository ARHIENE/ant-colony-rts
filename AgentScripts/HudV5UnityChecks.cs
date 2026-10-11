using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.UI;
using AntColony.Core;
using AntColony.Save;
using AntColony.Data;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public static class HudV5UnityChecks
{
    static int count;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(100);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Put<T>(BuildingKind kind) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, new Vector3(10, 0, 10), Quaternion.identity);
        go.SetActive(true); return go.GetComponent<T>();
    }
    static Button Button(string prefix) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .First(b => b.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith(prefix)));
    static void Window(int sections)
    {
        Canvas.ForceUpdateCanvases();
        var window = GameObject.Find("V5Window"); Check(window != null, "V5 window exists");
        var scrolls = window.GetComponentsInChildren<ScrollRect>();
        Check(scrolls.Length == sections, "independent scroll sections");
        foreach (var scroll in scrolls)
        {
            Check(scroll.content != null && scroll.viewport != null, "scroll connected");
            Check(scroll.viewport.rect.width > 0 && scroll.viewport.rect.height > 0, "visible viewport");
        }
        Check(window.GetComponentsInChildren<Button>().Any(b => b.name == "Close"), "fixed close");
    }
    public static async Task<string> Main()
    {
        count = 0; var previous = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "HudV5-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300); Time.timeScale = 0;
            var menu = GameMenuController.Instance;
            var ranch = Put<Ranch>(BuildingKind.Pen);
            menu.ShowRanch(ranch); Window(3);
            var auto = ranch.AutoTreat; Button("자동 치료:").onClick.Invoke(); Check(ranch.AutoTreat != auto, "auto treatment button bound");
            var animal = Critter.Spawn(Species.Aphid, new Vector3(12, 0, 12), true);
            menu.ShowCritter(animal); Window(2);
            var input = Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).First();
            input.onEndEdit.Invoke("V5검사"); Check(animal.DisplayName.Contains("V5검사"), "rename bound");
            Button("포획 요청").onClick.Invoke(); Check(animal.CaptureDesignated, "capture bound");
            Button("포획 요청 취소").onClick.Invoke(); Check(!animal.CaptureDesignated, "capture cancel bound");
            var medicine = Put<Processor>(BuildingKind.MedicineBench);
            GameObject.Find("Animal Medical").GetComponent<Button>().onClick.Invoke(); Window(2);
            Check(!Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Any(), "medical tab hides rename input");
            Button("약제대 생산 목록:").onClick.Invoke();
            Check(GameObject.Find("ProductionModes") != null, "medical links to real production bench");
            GameObject.Find("Mode KeepStock").GetComponent<Button>().onClick.Invoke();
            var amountInput = GameObject.Find("OrderAmount").GetComponent<InputField>();
            amountInput.onEndEdit.Invoke("17");
            Button("목록 추가:").onClick.Invoke();
            Check(medicine.Orders.Last().mode == Workshop.OrderMode.KeepStock && medicine.Orders.Last().amount == 17, "typed amount and selected mode reach actual order");
            GameObject.Find("OrderAmount").GetComponent<InputField>().onEndEdit.Invoke("999999");
            Button("목록 추가:").onClick.Invoke();
            Check(medicine.Orders.Last().amount == Workshop.MaxOrderAmount, "input uses real order limit");
            GameObject.Find("Mode Forever").GetComponent<Button>().onClick.Invoke();
            Check(GameObject.Find("OrderAmount") == null, "forever mode hides amount");
            menu.ShowCritter(animal);
            GameObject.Find("Animal Overview").GetComponent<Button>().onClick.Invoke();
            Check(Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Any(), "overview tab restores rename");
            menu.Resume(); WorkTargetPanel.Select(animal.Monster); await Task.Delay(150);
            Check(GameObject.Find("AnimalCommands") != null && GameObject.Find("TargetCommands") == null, "animal selection uses animal commands");
            GameObject.Find("Animal Capture").GetComponent<Button>().onClick.Invoke();
            Check(animal.CaptureDesignated, "HUD capture request bound");
            GameObject.Find("Animal Capture").GetComponent<Button>().onClick.Invoke();
            Check(!animal.CaptureDesignated, "HUD capture cancellation bound");
            Check(!GameObject.Find("Animal Carry").GetComponent<Button>().interactable, "unbound animal cannot be carried");
            Check(!GameObject.Find("Animal Treat").GetComponent<Button>().interactable, "healthy animal treatment disabled");
            GameObject.Find("Animal Release").GetComponent<Button>().onClick.Invoke();
            Check(animal != null && animal.gameObject.activeInHierarchy, "release opens confirmation without releasing");
            menu.Resume(); WorkTargetPanel.Clear();
            var feeder = Put<RanchFacility>(BuildingKind.Feeder); menu.ShowFeeder(feeder); Window(2);
            var priority = feeder.Priority; Button("운반 우선순위 " + priority + " (+)").onClick.Invoke(); Check(feeder.Priority == priority + 1, "feeder priority bound");
            var butcher = Put<RanchFacility>(BuildingKind.ButcherTable); menu.ShowButcher(butcher); Window(2);
            var paused = butcher.Paused; Button(paused ? "재개(" : "일시정지").onClick.Invoke(); Check(butcher.Paused != paused, "butcher pause bound");
            menu.ShowProcessor(Put<Processor>(BuildingKind.Carpentry)); Window(2);
            menu.ShowWorkshop(Put<Workshop>(BuildingKind.Workshop)); Window(2);
            menu.ShowRenovate(feeder); Window(2);
            menu.WorkSchedule();
            var c = CommanderRoster.Instance.Commanders.First(x => x != null && !x.IsDead && x.CanDoJob(CommanderJobs.Hauling));
            c.SetJobEnabled(CommanderJobs.Hauling, false);
            var row = GameObject.Find("Duty " + c.CommanderName);
            var cell = row.GetComponentsInChildren<Button>().First(b => b.name == "Priority Hauling");
            cell.onClick.Invoke(); Check(c.AllowsJob(CommanderJobs.Hauling) && c.JobPriority(CommanderJobs.Hauling) == 1, "left click enables priority 1");
            ExecuteEvents.Execute(cell.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right }, ExecuteEvents.pointerClickHandler);
            Check(!c.AllowsJob(CommanderJobs.Hauling), "right click forbids at zero");
            menu.Resume(); BuildScreen.Open(); Canvas.ForceUpdateCanvases();
            Check(GameObject.Find("BuildWindowV5") != null && BuildScreen.IsOpen, "build dialog opens");
            var buildCanvas = GameObject.Find("BuildWindowV5").GetComponent<Canvas>();
            Check(buildCanvas != null && buildCanvas.overrideSorting && buildCanvas.sortingOrder == 100, "build window above world labels");
            Check(buildCanvas.GetComponent<GraphicRaycaster>() != null, "build window receives clicks");
            GameObject.Find("Build SoilWall").GetComponent<Button>().onClick.Invoke();
            Check(!BuildScreen.Picking, "locked building cannot be selected");
            BuildScreen.OpenDormitory();
            await Task.Delay(100);
            Check(BuildScreen.Picking, "building selects picker");
            Check(GameObject.Find("Compare Material") != null, "material comparison present");
            Check(GameObject.Find("Compare Material").GetComponentsInChildren<Button>().Length == MaterialInfo.Structural.Length, "all structural materials listed together");
            var material = MaterialInfo.Structural.Last();
            GameObject.Find("Material " + material).GetComponent<Button>().onClick.Invoke();
            var screen = Object.FindFirstObjectByType<BuildScreen>();
            Check(Equals(typeof(BuildScreen).GetField("selectedMaterial", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(screen), material), "material row selects real placement material");
            BuildScreen.Back(); BuildScreen.Back(); Check(!BuildScreen.IsOpen, "build back closes");
            return "PASS " + count;
        }
        finally { SaveStorage.RootOverride = previous; Time.timeScale = 0; }
    }
}
