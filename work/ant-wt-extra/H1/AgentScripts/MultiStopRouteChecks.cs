namespace AntColony.Regression
{
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
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;

public static class MultiStopRouteChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Rally(ExpeditionTransport ship)
    {
        foreach (var c in ship.Crew) { c.CommandStop(); Check(c.Agent.Warp(ship.Position + Vector3.right * 3), "crew rally"); }
    }
    static SaveFileV1 Clone(SaveFileV1 value) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(value));
    public static async Task<string> Main()
    {
        checks = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "MultiStop-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 261004 }); await Ready();
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            var world = WorldMapManager.Instance;
            var sites = world.Sites.Where(s => s.Kind == ExpeditionSiteKind.Settlement).Take(4).ToArray();
            foreach (var site in sites)
            {
                typeof(ExpeditionSite).GetMethod("TransferToPlayer", Private).Invoke(site, null);
                foreach (var node in site.Colony.GetComponentsInChildren<ResourceNode>(true)) { node.gameObject.SetActive(true); node.Extract(float.MaxValue); }
            }
            var commander = CommanderRoster.Instance.Commanders[0]; commander.WorkState.duty = CommanderDuty.Deployed;
            AntPool.Instance.Breed(10); Check(commander.TryAssign(1), "collector deployed");
            Check(world.CanCreateTransport(commander.Position, out var position), "transport location");
            var ship = world.CreateTransport(false, position); var blocker = world.CreateTransport(false, position + Vector3.right * 10);
            Check(commander.Agent.Warp(ship.Position + Vector3.right * 3) && ship.TryBoard(new[] { commander }), "collector boards");
            Check(!ship.Route.TryAddStop(sites[1]), "cannot append before starting");
            Check(ship.Route.TryStart(sites[0]) && ship.Route.TryAddStop(sites[1]) && ship.Route.TryAddStop(sites[2]), "three stops accepted");
            Check(!ship.Route.TryAddStop(sites[3]) && !ship.Route.TryAddStop(sites[1]) && !ship.Route.TryAddStop(null), "limit duplicate and null rejected");
            Check(ship.Route.Stops.Count == 3 && ship.Route.StopIndex == 0, "ordered route starts at zero");
            Check(blocker.TryDepart(sites[1]), "next stop occupied");
            ship.Route.Tick(1); ship.Tick(ship.TravelSeconds); Rally(ship); ship.Route.Tick(1);
            Check(ship.State == ExpeditionState.Deployed && ship.Site == sites[0] && sites[0].Visitor == ship, "occupied next stop waits without returning or losing current claim");
            blocker.Tick(blocker.TravelSeconds); Check(blocker.TryReturn(), "blocking transport returns"); blocker.Tick(blocker.TravelSeconds);
            ship.DepositResources(ResourceType.Food, 7); ship.Route.Tick(1);
            Check(ship.State == ExpeditionState.Outbound && ship.Site == sites[1] && ship.Route.StopIndex == 1, "route hops to second stop");
            Check(sites[0].Visitor == null && sites[1].Visitor == ship && ship.GetCargo(ResourceType.Food) == 7 && commander.IsEmbarked, "hop transfers claim and preserves cargo and crew");

            var captured = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(captured, out var error), "outbound multi-stop snapshot: " + error);
            var dto = captured.world.transports.First(t => t.route.running).route;
            Check(dto.stops.Length == 3 && dto.stop == 1, "save records all stops and current index");
            var invalid = Clone(captured); invalid.world.transports.First(t => t.route.running).route.stop = 3;
            Check(!SaveValidator.Validate(invalid, out _), "out of range stop rejected");
            invalid = Clone(captured); var bad = invalid.world.transports.First(t => t.route.running).route; bad.stops[1] = bad.stops[0];
            Check(!SaveValidator.Validate(invalid, out _), "duplicate saved stop rejected");
            var legacy = Clone(captured); var old = legacy.world.transports.First(t => t.route.running).route; old.stops = null; old.stop = 0;
            Check(SaveValidator.Validate(legacy, out error), "legacy single destination accepted: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save in transit: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load in transit: " + error); await Ready();
            world = WorldMapManager.Instance; ship = world.Transports.First(s => s.Route.IsRunning); sites = ship.Route.Stops.ToArray(); commander = ship.Crew[0];
            Check(ship.Route.StopIndex == 1 && ship.Route.Stops.Count == 3 && ship.Site == sites[1] && ship.GetCargo(ResourceType.Food) == 7, "multi-stop route restored during hop");
            ship.Tick(ship.TravelSeconds); Rally(ship); ship.Route.Tick(1);
            Check(ship.State == ExpeditionState.Outbound && ship.Site == sites[2] && ship.Route.StopIndex == 2, "third stop follows second");
            ship.Tick(ship.TravelSeconds); Rally(ship); ship.Route.Tick(1);
            Check(ship.State == ExpeditionState.Returning && ship.Route.WaitSeconds == TransportRoute.IntervalSeconds, "last stop returns home with interval");
            ship.Tick(ship.TravelSeconds); ship.Route.Tick(TransportRoute.IntervalSeconds);
            Check(ship.State == ExpeditionState.Outbound && ship.Site == sites[0] && ship.Route.StopIndex == 0, "next circuit starts from first stop");
            ship.Tick(ship.TravelSeconds); Rally(ship); ship.DepositResources(ResourceType.Food, ship.CargoCapacity); ship.Route.Tick(1);
            Check(ship.CargoFull && ship.State == ExpeditionState.Returning && ship.Route.StopIndex == 0, "full cargo returns early");
            ship.Route.Stop(); ship.Tick(ship.TravelSeconds);
            Check(!ship.Route.IsRunning && ship.Crew.Count == 0 && commander.Transport == null, "stopped return unloads crew safely");

            var panel = Object.FindFirstObjectByType<WorldMapPanel>();
            var button = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Add Stop");
            var rect = (RectTransform)button.transform;
            Check(rect.sizeDelta.x > 0 && rect.sizeDelta.y > 0 && rect.anchoredPosition.x >= 524, "waypoint button fits existing map footer");
            return "PASS " + checks + " multi-stop transport checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}

}
