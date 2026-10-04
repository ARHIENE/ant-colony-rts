using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using AntSelection = AntColony.Units.SelectionManager;
using ColonyResource = AntColony.Data.ResourceType;

public static class GatheringUIChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static void Click(string name) => Object.FindObjectsByType<Button>().Single(b => b.name == name && b.gameObject.activeInHierarchy).onClick.Invoke(); // HUD 대상 패널(채집 금지/허용)
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "GatherUI-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready();
            foreach (var commander in CommanderRoster.Instance.Commanders) { commander.SetJobEnabled(CommanderJobs.All, false); commander.CommandStop(); }
            var c = CommanderRoster.Instance.Commanders[0];
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = true;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Gather UI test";
            go.transform.position = c.Position + Vector3.up * .5f;
            var node = go.AddComponent<ResourceNode>(); node.ConfigureLoot(ColonyResource.Soil, 20);
            var selection = Object.FindFirstObjectByType<AntSelection>();
            var camera = UnityEngine.Camera.main; var position = camera.transform.position; var rotation = camera.transform.rotation;
            try
            {
                camera.transform.position = go.transform.position + Vector3.up * 20; camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                Physics.SyncTransforms(); var screen = camera.WorldToScreenPoint(go.transform.position);
                typeof(AntSelection).GetMethod("ClickSelectOrClear", Private).Invoke(selection, new object[] { new Vector2(screen.x, screen.y), false });
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); }
            Check(WorkTargetPanel.Target == node, "node click opens HUD work target panel"); await Task.Delay(100);
            c.CommandGather(node); Check(c.CurrentResourceNode == node, "allowed manual gathering");
            typeof(WorkerAnt).GetMethod("RestoreCargo", Private).Invoke(c, new object[] { 3f, ColonyResource.Soil });
            Click("TargetAction"); await Task.Delay(80);
            Check(node.GatheringForbidden && !node.CanGather && node.Extract(1) == 0, "UI forbids extraction");
            typeof(WorkerAnt).GetMethod("TickMovingToNode", Private).Invoke(c, null);
            Check(c.CarriedAmount == 3 && node.AmountRemaining == 20, "existing cargo and node stock preserved");
            c.CommandStop(); typeof(WorkerAnt).GetMethod("RestoreCargo", Private).Invoke(c, new object[] { 0f, ColonyResource.Soil });
            c.CommandGather(node); Check(!c.IsWorking, "manual gather rejected");
            c.SetJobEnabled(CommanderJobs.Gathering | CommanderJobs.Hauling, true); c.TickDuty(2); // 루트 노드는 운반 작업
            Check(!c.IsWorking, "automatic gather skips forbidden node");
            Click("TargetAction"); await Task.Delay(80);
            c.TickDuty(2); Check(!node.GatheringForbidden && c.CurrentResourceNode == node, "UI permits autonomous gathering again: forbidden=" + node.GatheringForbidden + " current=" + (c.CurrentResourceNode == null ? "none" : c.CurrentResourceNode.name) + " working=" + c.IsWorking);
            c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop();
            Click("TargetAction"); await Task.Delay(80); WorkTargetPanel.Clear();
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(ResourceNode.Available.All(n => n.GatheringForbidden), "forbidden state survives save/load");
            return "PASS GatheringUIChecks " + checks;
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
