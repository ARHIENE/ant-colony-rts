namespace AntColony.Regression
{
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;

// 새 Play 세션에서 실행. 실제 채집/이동을 유지하고 원정 이동 시간만 Tick으로 진행한다.
public static class TransportRouteChecks
{
    static int checks;
    static void Check(bool ok, string message)
    { if (!ok) throw new Exception("FAIL: " + message); checks++; }
    static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static async Task<bool> Wait(Func<bool> condition, int seconds = 120)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(25);
        return condition();
    }
    static void Move(CommanderAnt c, Vector3 position)
    {
        c.CommandStop(); c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas), "walkable test position");
        c.transform.position = hit.position; c.ApplyMovementMode(); Physics.SyncTransforms();
    }

    public static string Diagnose() => string.Join("\n", WorldMapManager.Instance.Transports.Select(s =>
        $"{s.name} {s.State} route={s.Route.Status} running={s.Route.IsRunning} cargo={s.GetCargo(ResourceType.Food)}/{s.GetCargo(ResourceType.Soil)} "
        + string.Join(";", s.Crew.Select(c => $"{c.name} pos={c.Position} distance={Vector3.Distance(c.Position, s.Position)} work={c.IsWorking} carrying={c.IsCarrying} path={c.Agent.pathStatus} pending={c.Agent.pathPending} remaining={c.Agent.remainingDistance} troops={c.TroopCount}"))));

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "Play mode required");
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        var world = WorldMapManager.Instance;
        var site = world.Sites[0];
        var rm = ResourceManager.Instance;
        rm.AddCapacity(ResourceType.Food, 10000); rm.AddCapacity(ResourceType.Soil, 10000);
        rm.Add(ResourceType.Food, 8000); rm.Add(ResourceType.Soil, 8000);
        AntPool.Instance.Breed(100);
        var c = CommanderRoster.Instance.Commanders[0];
        c.CommandStop(); Check(c.TrySetRole(UnitRole.Worker), "worker role");
        if (c.TroopCount < 5) Check(c.TryAssign(5 - c.TroopCount), "worker troops");
        Check(world.CanCreateTransport(c.Position, out var position), "transport placement");
        var ship = world.CreateTransport(false, position);
        Check(!ship.Route.TryStart(site), "hostile and empty routes rejected");
        Move(c, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { c }) && ship.TryDepart(site), "conquest trip");
        ship.Tick(ship.TravelSeconds);
        foreach (var b in site.Colony.GetComponentsInChildren<BuildingBase>()) b.TakeDamage(float.MaxValue);
        foreach (var guard in site.GetComponentsInChildren<WildMonster>()) guard.TakeDamage(float.MaxValue);
        Check(await Wait(() => site.Cleared), "conquest cleared");
        Check(site.TryResolveConquest(ConquestDisposition.Annexed), "annexed route target");
        Set(site.Settlement, "elapsed", -10000f); // 생산 시간을 분리해 수송 보존량을 정확히 검증한다.
        var nodes = site.Colony.GetComponentsInChildren<ResourceNode>(true);
        var food = nodes.First(n => n.ResourceType == ResourceType.Food);
        var soil = nodes.First(n => n.ResourceType == ResourceType.Soil);
        food.Extract(food.AmountRemaining); soil.Extract(soil.AmountRemaining);
        food.AddStock(12); soil.AddStock(7);
        Check(ship.TryReturn(), "return after annexation"); ship.Tick(ship.TravelSeconds);
        Check(!ship.Route.TryStart(site), "empty route rejected on annexed site");
        Move(c, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { c }), "board auto crew");
        Check(!ship.Route.TryStart(world.Sites[2]) && !ship.Route.TryStart(world.Sites[4]), "boss and neutral route rejected");
        var other = world.CreateTransport(false, position + Vector3.forward * 10);
        Check(other.TryDepart(site), "another transport occupies destination");
        var ui = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        if (!ui.IsOpen) ui.Toggle();
        Set(ui, "selectedShip", ship); Set(ui, "selectedSite", site);
        var start = ui.PanelRect.Find("WorldMap/Start Auto").GetComponent<UnityEngine.UI.Button>();
        var stop = ui.PanelRect.Find("WorldMap/Stop Auto").GetComponent<UnityEngine.UI.Button>();
        start.onClick.Invoke();
        Check(ship.Route.IsRunning && ship.Route.Destination == site, "UI starts chosen route");
        ship.Route.Tick(float.NaN); ship.Route.Tick(float.PositiveInfinity); ship.Route.Tick(-1);
        ship.Route.Tick(1);
        Check(ship.State == ExpeditionState.Home && site.Visitor == other, "occupied site waits without stealing visitor");
        Check(!ship.Route.TryStart(site) && !ship.TryUnloadCrew() && !ship.TryBoard(new[] { c })
            && !ship.TryDepart(world.Sites[1]), "active route rejects restart, manifest edits and other destinations");
        other.Tick(other.TravelSeconds); Check(other.TryReturn(), "other transport leaves"); other.Tick(other.TravelSeconds);
        ship.Route.Tick(1); Check(ship.State == ExpeditionState.Outbound, "route departs when site is free");
        ship.Tick(ship.TravelSeconds);
        Check(!ship.TryStation(ship.Crew), "auto crew cannot be stationed");
        var beforeFood = rm.GetAmount(ResourceType.Food);
        Check(await Wait(() => ship.State == ExpeditionState.Returning), "real collection automatically returns: " + Diagnose());
        Check(ship.GetCargo(ResourceType.Food) == 12 && ship.GetCargo(ResourceType.Soil) == 7
            && rm.GetAmount(ResourceType.Food) == beforeFood, "both resources remain cargo until arrival");
        rm.Add(ResourceType.Food, 10000); rm.Add(ResourceType.Soil, 10000);
        ship.Tick(ship.TravelSeconds); ship.Route.Tick(120);
        Check(ship.State == ExpeditionState.Home && ship.HasCargo && c.IsEmbarked && ship.Crew.Count == 1,
            "full home storage preserves cargo and crew and blocks next departure");
        Check(!ship.TryDepart(site), "manual departure cannot bypass auto storage wait");
        Check(rm.TrySpend(12, 7), "make exact storage room");
        Check(await Wait(() => !ship.HasCargo), "stored cargo unloads when room opens");
        Check(rm.GetAmount(ResourceType.Food) == rm.GetCapacity(ResourceType.Food)
            && rm.GetAmount(ResourceType.Soil) == rm.GetCapacity(ResourceType.Soil), "cargo delivered exactly once");
        ship.Route.Tick(1);
        Check(ship.State == ExpeditionState.Home && ship.Route.WaitSeconds > 0, "repeat waits at home");
        Check(!ship.TryDepart(site), "manual departure cannot bypass repeat interval");
        food.AddStock(11); soil.AddStock(6); rm.TrySpend(100, 100);
        beforeFood = rm.GetAmount(ResourceType.Food);
        var beforeSoil = rm.GetAmount(ResourceType.Soil);
        ship.Route.Tick(TransportRoute.IntervalSeconds);
        Check(ship.State == ExpeditionState.Outbound && c.IsEmbarked, "same crew departs for second cycle");
        ship.Tick(ship.TravelSeconds);
        Check(await Wait(() => ship.State == ExpeditionState.Returning), "second automatic collection completes");
        stop.onClick.Invoke();
        Check(!ship.Route.IsRunning && c.IsEmbarked && ship.HasCargo, "stop during return preserves passengers and cargo");
        ship.Tick(ship.TravelSeconds); ship.Route.Tick(1000);
        Check(ship.State == ExpeditionState.Home && ship.Crew.Count == 0 && c.Transport == null && !c.IsEmbarked
            && rm.GetAmount(ResourceType.Food) == beforeFood + 11 && rm.GetAmount(ResourceType.Soil) == beforeSoil + 6,
            "stopped return unloads once and never repeats");
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { c }), "board for cancellation check");
        Check(ship.Route.TryStart(site), "restart at home"); ship.Route.Tick(1);
        ship.Route.Stop(); ship.Tick(ship.TravelSeconds); ship.Route.Tick(1000);
        Check(ship.State == ExpeditionState.Deployed && !c.IsEmbarked && !c.IsWorking && c.Transport == ship,
            "outbound stop arrives normally without issuing work");
        Check(ship.TryReturn(), "manual return after stop"); ship.Tick(ship.TravelSeconds);
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { c }), "board for gathering stop");
        food.AddStock(20); soil.AddStock(9);
        Check(ship.Route.TryStart(site), "start gathering cancellation trip"); ship.Route.Tick(1); ship.Tick(ship.TravelSeconds);
        Check(await Wait(() => c.IsCarrying), "real gather in progress"); ship.Route.Stop();
        Check(c.IsCarrying && !ship.TryReturn(), "stop keeps carried resources and return remains guarded");
        Check(await Wait(() => !c.IsCarrying && !c.IsWorking), "current gather deposits safely after stop");
        Check(ship.State == ExpeditionState.Deployed && ship.HasCargo, "stopped route does not return or issue next gather");
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryReturn(), "manual collection return"); ship.Tick(ship.TravelSeconds);
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { c }) && ship.Route.TryStart(site), "home stop setup");
        ship.Route.Stop();
        Check(!c.IsEmbarked && c.Transport == null && ship.Crew.Count == 0, "home stop releases reserved crew");
        Canvas.ForceUpdateCanvases();
        foreach (var button in new[] { start, stop })
        {
            var rect = (RectTransform)button.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Check(corners.All(p => ui.PanelRect.rect.Contains(ui.PanelRect.InverseTransformPoint(p))), "route button within panel");
            Check(button.GetComponentInChildren<UnityEngine.UI.Text>().preferredWidth <= rect.rect.width, "route button text fits");
        }
        return "PASS: " + checks + " automatic transport / cargo conservation / cancellation / UI checks";
    }
}
}
