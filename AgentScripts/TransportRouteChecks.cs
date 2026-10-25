namespace AntColony.Regression
{
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
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
    static async Task<bool> Wait(Func<bool> condition, int seconds = 300) // 날씨·바이옴 작업 속도 보정 여유
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

    public static string Diagnose() => string.Join("\n", new[] { $"timeScale={Time.timeScale} screen={AntColony.UI.GameMenuController.Instance?.ScreenName}" }.Concat(WorldMapManager.Instance.Transports.Select(s =>
        $"{s.name} {s.State} route={s.Route.Status} running={s.Route.IsRunning} cargo={s.GetCargo(ResourceType.Wood)}/{s.GetCargo(ResourceType.Soil)} "
        + string.Join(";", s.Crew.Select(c => $"{c.name} pos={c.Position} distance={Vector3.Distance(c.Position, s.Position)} work={c.IsWorking} carrying={c.IsCarrying} path={c.Agent.pathStatus} pending={c.Agent.pathPending} remaining={c.Agent.remainingDistance} troops={c.TroopCount} vel={c.Agent.velocity.magnitude:0.00} speed={c.Agent.speed:0.00} stopped={c.Agent.isStopped} dest={c.Agent.destination} state={typeof(WorkerAnt).GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(c)} node={c.CurrentResourceNode?.name} sleep={c.WorkState.resting} cap={c.LoadCapacity} carried={c.CarriedAmount}")))));

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "Play mode required");
        // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 게임을 직접 시작하고 시간을 흐르게 한다.
        var savedRoot = SaveStorage.RootOverride; var savedTax = -1f;
        var savedAutoPause = UserSettings.Current.autoPauseThreat;
        UserSettings.Current.autoPauseThreat = false;
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "TransportRoute-" + Guid.NewGuid().ToString("N"));
        try
        {
        Check(await Wait(() => !SaveSystem.Busy), "scene ready");
        var candidate = StartingCandidate.Roll(0); candidate.traits.values.Clear();
        SaveSystem.NewGame(new NewGameOptions { seed = 250926, startingCount = 1, startingTeam = new[] { candidate } });
        Check(await Wait(() => !SaveSystem.Busy), "new game ready");
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        var world = WorldMapManager.Instance;
        typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(world, new object[] { true });
        var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
        var rm = ResourceManager.Instance;
        rm.AddCapacity(ResourceType.Food, 10000); rm.Add(ResourceType.Food, 8000); // 기존 보급 재고는 유지한다.
        rm.AddCapacity(ResourceType.Soil, 10000); // 목재·흙은 같은 재료 한도를 사용한다.
        rm.Add(ResourceType.Wood, 8000); rm.Add(ResourceType.Soil, 8000);
        AntPool.Instance.Breed(100);
        foreach (var x in CommanderRoster.Instance.Commanders) x.SetJobEnabled(CommanderJobs.All, false); // 홈 자율 채집이 자원 비교를 흔들지 않게 한다.
        var c = CommanderRoster.Instance.Commanders[0];
        c.CommandStop(); // 역할은 무기로 정해지고, 모든 장수가 채집할 수 있다.
        // 원정 탑승은 병력이 필요하다: 평시 민간인은 출전 편성(CampaignChecks와 같은 방식)으로 병력을 받는다.
        c.WorkState.duty = CommanderDuty.Deployed; if (c.TroopCount < 5) Check(c.TryAssign(5 - c.TroopCount), "expedition mobilization");
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
        var wood = nodes.First(n => n.ResourceType == ResourceType.Food);
        var soil = nodes.First(n => n.ResourceType == ResourceType.Soil);
        wood.Extract(wood.AmountRemaining); soil.Extract(soil.AmountRemaining);
        // G05 보급은 식량 화물을 정상 소비한다. 보존량 검사는 소비되지 않는 목재·흙으로 격리한다.
        wood.ConfigureLoot(ResourceType.Wood, 12); wood.gameObject.SetActive(true); soil.AddStock(7); // 다 캔 식량 노드는 비활성이라 다시 켠다
        Check(ship.TryReturn(), "return after annexation"); ship.Tick(ship.TravelSeconds);
        Check(await Wait(() => ship.PendingCrewOrderCount == 0 && ship.Crew.Count == 0, 30), "G42 home unload completes before empty-route check");
        Check(!ship.Route.TryStart(site), "empty route rejected on annexed site");
        Move(c, ship.Position + Vector3.right * 3);
        Check(c.Transport == ship || ship.TryBoard(new[] { c }), "board auto crew"); // G42: 이미 승무원이면 다시 탑승 지시는 거부된다
        Check(!ship.Route.TryStart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest)) && !ship.Route.TryStart(world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite)), "boss and neutral route rejected");
        Check(!ship.Route.TryStart(new[] { site, site }) && !ship.Route.TryStart(new[] { site, site, site, site }), "D09 duplicate and over-3 stops rejected");
        var other = world.CreateTransport(false, position + Vector3.forward * 10);
        Check(other.TryDepart(site), "another transport occupies destination");
        var ui = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        if (!ui.IsOpen) ui.Toggle();
        Set(ui, "selectedShip", ship); Set(ui, "selectedSite", site);
        var start = ui.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "WorldMap").Find("Start Auto").GetComponent<UnityEngine.UI.Button>();
        var stop = ui.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "WorldMap").Find("Stop Auto").GetComponent<UnityEngine.UI.Button>();
        start.onClick.Invoke();
        Check(ship.Route.IsRunning && ship.Route.Destination == site, "UI starts chosen route");
        ship.Route.Tick(float.NaN); ship.Route.Tick(float.PositiveInfinity); ship.Route.Tick(-1);
        ship.Route.Tick(1);
        Check(ship.State == ExpeditionState.Outbound && site.Visitor == other, "G15 occupied site permits follow-up departure without stealing visitor");
        Check(!ship.Route.TryStart(site) && !ship.TryUnloadCrew() && !ship.TryBoard(new[] { c })
            && !ship.TryDepart(world.Sites[1]), "active route rejects restart, manifest edits and other destinations");
        other.Tick(other.TravelSeconds);
        ship.Tick(ship.TravelSeconds);
        Check(ship.State == ExpeditionState.Outbound && other.State == ExpeditionState.Deployed, "G15 simultaneous docking waits");
        other.Tick(ExpeditionRules.DockSeconds);
        ship.Tick(.01f);
        Check(ship.State == ExpeditionState.Deployed && other.State == ExpeditionState.Deployed, "G15 both parties stay after docking interval");
        Check(other.TryReturn(), "other transport leaves"); other.Tick(other.TravelSeconds);
        Check(!ship.TryStation(ship.Crew), "auto crew cannot be stationed");
        savedTax = ColonyPopulation.Instance.S.taxRate; ColonyPopulation.Instance.SetTaxRate(0); // G08 아침 세금(재료)이 본거지 목재 비교를 흔들지 않게
        var beforeWood = rm.GetAmount(ResourceType.Wood);
        Check(await Wait(() => ship.State == ExpeditionState.Returning), "real collection automatically returns: " + Diagnose());
        Check(ship.GetCargo(ResourceType.Wood) == 12 && ship.GetCargo(ResourceType.Soil) == 7
            && rm.GetAmount(ResourceType.Wood) == beforeWood, "both resources remain cargo until arrival: wood="
            + ship.GetCargo(ResourceType.Wood) + "/12 soil=" + ship.GetCargo(ResourceType.Soil) + "/7 homeWood="
            + rm.GetAmount(ResourceType.Wood) + "/" + beforeWood + " foodDebt=" + ship.FoodDebt + $" woodNode={wood.AmountRemaining}/loose={wood.IsLooseCargo}/can={wood.CanGather}/with={wood.CanGatherWith(c)}/active={wood.isActiveAndEnabled} soilNode={soil.AmountRemaining} crew={string.Join(",", ship.Crew.Select(x => x.name + ':' + x.CarriedType + x.CarriedAmount))}");
        rm.Add(ResourceType.Wood, 10000); rm.Add(ResourceType.Soil, 10000);
        ship.Tick(ship.TravelSeconds);
        Check(ship.State == ExpeditionState.Home && !ship.HasCargo && c.IsEmbarked && ship.Crew.Count == 1,
            "G69 full storage unloads overflow to floor once and preserves auto crew");
        Check(rm.GetAmount(ResourceType.Wood) == rm.GetCapacity(ResourceType.Wood)
            && rm.GetAmount(ResourceType.Soil) == rm.GetCapacity(ResourceType.Soil), "cargo delivered exactly once");
        ship.Route.Tick(1);
        Check(ship.State == ExpeditionState.Home && ship.Route.WaitSeconds > 0, "repeat waits at home");
        Check(!ship.TryDepart(site), "manual departure cannot bypass repeat interval");
        wood.AddStock(11); soil.AddStock(6); Check(rm.TrySpend(ResourceType.Wood, 100) && rm.TrySpend(ResourceType.Soil, 100), "free room for second delivery");
        beforeWood = rm.GetAmount(ResourceType.Wood);
        var beforeSoil = rm.GetAmount(ResourceType.Soil);
        ship.Route.Tick(TransportRoute.IntervalSeconds);
        Check(ship.State == ExpeditionState.Outbound && c.IsEmbarked, "same crew departs for second cycle");
        ship.Tick(ship.TravelSeconds);
        Check(await Wait(() => ship.State == ExpeditionState.Returning), "second automatic collection completes: " + Diagnose());
        stop.onClick.Invoke();
        Check(!ship.Route.IsRunning && c.IsEmbarked && ship.HasCargo, "stop during return preserves passengers and cargo");
        ship.Tick(ship.TravelSeconds); ship.Route.Tick(1000);
        Check(ship.State == ExpeditionState.Home && ship.Crew.Count == 0 && c.Transport == null && !c.IsEmbarked
            && rm.GetAmount(ResourceType.Wood) == beforeWood + 11 && rm.GetAmount(ResourceType.Soil) == beforeSoil + 6,
            "stopped return unloads once and never repeats");
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { c }), "board for cancellation check");
        Check(ship.Route.TryStart(site), "restart at home"); ship.Route.Tick(1);
        ship.Route.Stop(); ship.Tick(ship.TravelSeconds); ship.Route.Tick(1000);
        Check(ship.State == ExpeditionState.Deployed && !c.IsEmbarked && !c.IsWorking && c.Transport == ship,
            "outbound stop arrives normally without issuing work");
        Check(ship.TryReturn(), "manual return after stop"); ship.Tick(ship.TravelSeconds);
        Move(c, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { c }), "board for gathering stop");
        wood.AddStock(20); soil.AddStock(9);
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
        finally { SaveStorage.RootOverride = savedRoot; UserSettings.Current.autoPauseThreat = savedAutoPause; if (savedTax >= 0 && ColonyPopulation.Instance != null) ColonyPopulation.Instance.SetTaxRate(savedTax); }
    }
}
}

