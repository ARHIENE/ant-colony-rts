namespace AntColony.Regression
{
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
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;
using SelectionManager = AntColony.Units.SelectionManager;
using SelectableObject = AntColony.Units.SelectableObject;

public static class PlayableLoopChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool condition, string label)
    { if (!condition) throw new Exception("FAIL: " + label); checks++; }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static async Task Wait(Func<bool> predicate, string label, int seconds = 30)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!predicate() && DateTime.UtcNow < end) await Task.Delay(50);
        Check(predicate(), label);
    }
    static async Task Ready()
    { await Wait(() => !SaveSystem.Busy, "scene ready", 60); await Task.Delay(300); }
    static void Move(CommanderAnt commander, Vector3 point)
    {
        commander.CommandStop();
        Check(NavMesh.SamplePosition(point, out var hit, 5, NavMesh.AllAreas), "walkable crew position");
        Check(commander.Agent.Warp(hit.position), "crew warp");
    }
    static async Task Carry(CommanderAnt commander)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "CargoCheck"; go.SetActive(false);
        Check(NavMesh.SamplePosition(commander.Position + Vector3.right * 2, out var hit, 5, NavMesh.AllAreas), "resource position");
        go.transform.position = hit.position; go.transform.localScale = Vector3.one * .3f;
        var node = go.AddComponent<ResourceNode>(); node.ConfigureLoot(ResourceType.Food, 1000);
        go.SetActive(true); commander.CommandGather(node);
        await Wait(() => (float)typeof(WorkerAnt).GetField("carriedAmount", Private).GetValue(commander) >= 2f, "actual harvesting starts");
        commander.CommandStop(); Object.Destroy(go);
        Check(commander.IsCarrying && !commander.IsWorking, "stop preserves carried cargo");
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play required"); checks = 0;
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PlayableLoop-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 23456 });
            await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            var progress = Object.FindAnyObjectByType<BetaProgress>();
            Check(progress.CurrentObjective.Contains("/60 ants"), "objective shows live population requirement");
            var commander = CommanderRoster.Instance.Commanders[0];
            if (!commander.HasTroops) Check(commander.TryAssign(2), "assign builder troops");
            var selection = Object.FindAnyObjectByType<SelectionManager>();
            selection.ClearSelection(); Call(selection, "AddToSelection", commander.GetComponent<SelectableObject>());
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var template = Object.FindObjectsByType<Storage>(FindObjectsInactive.Include).Single(s => s.name == "StorageTemplate");
            var foodBonus = template.Data.foodCapacityBonus;
            var resources = ResourceManager.Instance;
            var before = resources.GetCapacity(ResourceType.Food);
            var specialBefore = resources.GetCapacity(ResourceType.Special);
            var storageCount = Object.FindObjectsByType<Storage>().Length;
            var button = Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "Build StorageButton");
            button.onClick.Invoke(); Check(placement.IsPlacing, "HUD enters storage placement");
            placement.CancelPlacement(); Check(resources.GetCapacity(ResourceType.Food) == before, "preview adds no capacity");
            Check(placement.BeginStoragePlacement(), "storage placement API");
            var point = Vector3.zero; var found = false;
            for (var i = 0; i < 64 && !found; i++)
            {
                var candidate = commander.Position + Quaternion.Euler(0, i * 45, 0) * Vector3.forward * (5 + i / 8 * 3);
                if (!NavMesh.SamplePosition(candidate, out var hit, 2, NavMesh.AllAreas) || !commander.CanReach(hit.position)) continue;
                var center = (Vector3)Call(placement, "GetPlacementPosition", template.gameObject, hit.position);
                if ((bool)Call(placement, "HasObstruction", center)) continue;
                point = hit.position; found = true;
            }
            Check(found, "clear reachable construction location");
            var free = AntPool.Instance.Free;
            var soil = resources.GetAmount(ResourceType.Soil);
            Set(placement, "placementValid", true);
            Call(placement, "TryPlace", (Vector3)Call(placement, "GetPlacementPosition", template.gameObject, point), point);
            Check(resources.GetAmount(ResourceType.Soil) == soil - template.Data.soilCost, "storage charges once");
            Check(AntPool.Instance.Free == free - template.Data.constructionAnts, "construction reserves workforce");
            Check(resources.GetCapacity(ResourceType.Food) == before, "unfinished storage grants no capacity");
            await Wait(() => Object.FindObjectsByType<Storage>().Length == storageCount + 1, "commander walks and completes warehouse");
            Check(resources.GetCapacity(ResourceType.Food) == before + template.Data.foodCapacityBonus, "food capacity expanded once");
            Check(resources.GetCapacity(ResourceType.Special) == specialBefore + template.Data.specialCapacityBonus, "loot storage expanded");
            Check(AntPool.Instance.Free == free, "builders return to free pool");
            var storage = Object.FindObjectsByType<Storage>().OrderBy(s => Vector3.Distance(s.Position, point)).First();
            await Carry(commander);
            Check(!commander.TryReturnCargo(Object.FindAnyObjectByType<Barracks>()), "non-deposit building rejected");
            Check(commander.TryReturnCargo(storage), "interrupted cargo resumes to warehouse");
            await Wait(() => !commander.IsCarrying && !commander.IsWorking, "real deposit completes");
            commander.CommandStop(); await Task.Delay(100);
            Check(SaveSystem.TrySave(false, 0, out var error), "built storage save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "built storage load: " + error);
            await Ready();
            resources = ResourceManager.Instance;
            Check(Object.FindObjectsByType<Storage>().Length == storageCount + 1, "warehouse type restored");
            Check(resources.GetCapacity(ResourceType.Food) == before + foodBonus, "load does not duplicate capacity");
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            commander = CommanderRoster.Instance.Commanders[0];
            var world = WorldMapManager.Instance;
            var ship = world.CreateTransport(false, commander.Position + Vector3.right * 3);
            progress = Object.FindAnyObjectByType<BetaProgress>();
            Check(progress.CurrentObjective.Contains("BOARD"), "empty transport has boarding instructions");
            Check(ship.TryBoard(new[] { commander }), "board crew");
            Check(progress.CurrentObjective.Contains("READY"), "loaded transport has departure instructions");
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
            Check(ship.TryDepart(site), "depart neutral resource site");
            Check(progress.CurrentObjective.Contains("TRAVELLING"), "travel countdown objective");
            ship.Tick(ship.TravelSeconds);
            Check(!placement, "old scene objects disposed");
            Move(commander, ship.Position + Vector3.right * 4);
            await Carry(commander);
            Check(!commander.TryReturnCargo(Object.FindAnyObjectByType<QueenChamber>()), "remote home delivery rejected");
            Check(!ship.TryReturn(), "carried resources block premature departure");
            var foodAtHome = resources.GetAmount(ResourceType.Food);
            Check(commander.TryReturnCargo(ship), "resume deposit into assigned transport");
            await Wait(() => !commander.IsCarrying && !commander.IsWorking, "expedition cargo delivered");
            Check(ship.GetCargo(ResourceType.Food) >= 2, "harvested resources remain in cargo");
            Check(resources.GetAmount(ResourceType.Food) == foodAtHome, "expedition cargo is not credited at home yet");
            Check(ship.TryReturn(), "return after delivery");
            Check(progress.CurrentObjective.Contains("RETURNING"), "return countdown objective");
            ship.Tick(ship.TravelSeconds);
            Check(commander.Transport == null && !commander.IsEmbarked, "crew arrives home");
            return "PASS: " + checks + " playable loop / warehouse / cargo / objectives checks";
        }
        finally { SaveStorage.RootOverride = oldRoot; Encyclopedia.ResetCache(); }
    }
}
}
