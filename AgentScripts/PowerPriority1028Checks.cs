using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
public static class PowerPriority1028Checks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Power " + checks + ": " + label); checks++; }
    static T Put<T>(BuildingKind kind, Vector3 at) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, at, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    static async Task Ready() { var until = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main(bool baseline = false)
    {
        checks = 0; made.Clear(); var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PowerPriority-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready();
            if (!baseline) { SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready(); GameMenuController.Instance.Resume(); }
            Time.timeScale = 0;
            var at = new Vector3(300, 0, 300);
            var battery = Put<PowerNode>(BuildingKind.Battery, at); var freezer = Put<ColdStorage>(BuildingKind.Freezer, at);
            var lamp = Put<PowerNode>(BuildingKind.ElectricLamp, at);
            Check(PowerGrid.NetworkOf(lamp).Contains(battery), "isolated connected fixture");
            typeof(PowerNode).GetProperty("Charge").SetValue(battery, 20f); PowerGrid.Tick(1);
            var report = $"20J available, freezer20W/lamp10W: freezer={freezer.Powered}, lamp={lamp.Powered}, battery={battery.Charge}";
            if (baseline) return "BASELINE " + report;
            Check(freezer.Powered && !lamp.Powered && battery.Charge == 0, "protect refrigeration first and consume exact energy");
            var priority = typeof(BuildingBase).GetProperty("PowerPriorityOverride");
            WorkTargetPanel.Select(freezer);
            var panel = UnityEngine.Object.FindObjectsByType<WorkTargetPanel>().First();
            var layout = UnityEngine.Object.FindObjectsByType<HudResponsiveLayout>().First();
            var layoutTick = typeof(HudResponsiveLayout).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            layoutTick.Invoke(layout, null);
            panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "V6 Details").onClick.Invoke();
            layoutTick.Invoke(layout, null);
            typeof(WorkTargetPanel).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
            var down = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "PowerPriorityDown");
            Check(down.gameObject.activeInHierarchy && down.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("8"), $"selected object power priority visible self={down.gameObject.activeSelf}, hierarchy={down.gameObject.activeInHierarchy}, text={down.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text}, target={WorkTargetPanel.Target}, demand={freezer.Demand}, picking={BuildScreen.Picking}");
            var rowRoot = (RectTransform)down.transform.parent; var oldWidth = rowRoot.rect.width;
            var up = (RectTransform)panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "PowerPriorityUp").transform;
            var action = (RectTransform)panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "TargetAction").transform;
            foreach (var width in new[] { 540f, 808f })
            {
                rowRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width); Canvas.ForceUpdateCanvases();
                typeof(WorkTargetPanel).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
                var dr = (RectTransform)down.transform;
                Check(dr.anchoredPosition.x + dr.rect.width <= up.anchoredPosition.x && up.anchoredPosition.x + up.rect.width <= action.anchoredPosition.x, "priority/action geometry " + width);
            }
            rowRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldWidth);
            down.onClick.Invoke(); Check((int)priority.GetValue(freezer) == 7, "actual priority button changes setting");
            priority.SetValue(lamp, 9); priority.SetValue(freezer, 1);
            typeof(PowerNode).GetProperty("Charge").SetValue(battery, 20f); PowerGrid.Tick(1);
            Check(lamp.Powered && !freezer.Powered && battery.Charge == 10, "player can prioritize lighting instead");
            typeof(PowerNode).GetProperty("Charge").SetValue(battery, 30f); PowerGrid.Tick(1);
            Check(lamp.Powered && freezer.Powered && battery.Charge == 0, "enough energy powers all");
            PowerGrid.Tick(1); Check(!lamp.Powered && !freezer.Powered, "empty network cannot create energy");
            var snapshot = SaveSnapshot.Capture(); Check(SaveValidator.Validate(snapshot, out var error), "valid priorities " + error);
            var dto = snapshot.buildings.First(b => b.kind == "Freezer"); dto.powerPriority = 10;
            Check(!SaveValidator.Validate(snapshot, out error), "out-of-range saved priority rejected"); dto.powerPriority = 0;
            Check(SaveValidator.Validate(snapshot, out error), "old missing/zero priority accepted as default");
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "save/load " + error); await Ready();
            freezer = SceneObjects.All<ColdStorage>().First(b => (b.Position - at).sqrMagnitude < 1);
            Check((int)priority.GetValue(freezer) == 1, "priority survives actual load");
            return "PASS " + checks + " power priority checks; " + report;
        }
        finally { WorkTargetPanel.Clear(); foreach (var go in made) if (go != null) UnityEngine.Object.DestroyImmediate(go); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
