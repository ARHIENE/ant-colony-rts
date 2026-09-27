using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using AntSel = AntColony.Units.SelectionManager;
using ColonyResource = AntColony.Data.ResourceType;

public static class GatherDesignationChecks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static Button CardButton(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == name);
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "GatherDesig-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready();
            var camera = UnityEngine.Camera.main;
            await Task.Delay(300); Object.FindAnyObjectByType<AntSel>()?.ClearSelection(); await Task.Delay(100);
            var c = CommanderRoster.Instance.Commanders[0];
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = false;
            ResourceNode Make(string name, Vector3 at)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.position = at;
                var node = go.AddComponent<ResourceNode>(); node.ConfigureLoot(ColonyResource.Soil, 20); return node;
            }
            var a = Make("Desig A", c.Position + new Vector3(3, .5f, 0)); var b = Make("Desig B", c.Position + new Vector3(-3, .5f, 0));
            camera.transform.position = c.Position + Vector3.up * 30; camera.transform.rotation = Quaternion.Euler(90, 0, 0); Physics.SyncTransforms();

            var forbid = CardButton("Forbid Gathering"); var clear = CardButton("Clear Designation");
            Check(forbid.gameObject.activeInHierarchy && clear.gameObject.activeInHierarchy, "buttons visible with nothing selected");
            forbid.onClick.Invoke();
            Check(GatherDesignation.IsActive && GatherDesignation.Forbidding && GatherDesignation.ConsumesPointerInput, "forbid mode active");
            var sa = camera.WorldToScreenPoint(a.transform.position);
            Check(GatherDesignation.ApplyClick(new Vector2(sa.x, sa.y), true) && a.GatheringForbidden && !b.GatheringForbidden, "click forbids one node");
            var sb = camera.WorldToScreenPoint(b.transform.position);
            var rect = Rect.MinMaxRect(Mathf.Min(sa.x, sb.x) - 20, Mathf.Min(sa.y, sb.y) - 20, Mathf.Max(sa.x, sb.x) + 20, Mathf.Max(sa.y, sb.y) + 20);
            Check(GatherDesignation.Apply(rect, true) == 1 && b.GatheringForbidden, "drag forbids remaining node only");
            c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); c.CommandGather(a);
            Check(c.CurrentResourceNode != a, "forbidden node rejects manual gather");

            clear.onClick.Invoke();
            Check(GatherDesignation.IsActive && !GatherDesignation.Forbidding, "clear mode active");
            Check(GatherDesignation.Apply(rect, false) == 2 && !a.GatheringForbidden && !b.GatheringForbidden, "drag clears both");

            BuildScreen.Open(); await Task.Delay(100);
            Check(!GatherDesignation.IsActive, "opening build screen ends designation");
            BuildScreen.Back(); BuildScreen.Back(); await Task.Delay(100);
            forbid.onClick.Invoke(); GameMenuController.Instance.Pause(); await Task.Delay(100);
            Check(!GatherDesignation.IsActive, "menu ends designation");
            GameMenuController.Instance.Resume(); await Task.Delay(100);

            a.GatheringForbidden = true;
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Object.Destroy(a.gameObject); Object.Destroy(b.gameObject);
            return "PASS GatherDesignationChecks " + checks;
        }
        finally { GatherDesignation.End(); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
