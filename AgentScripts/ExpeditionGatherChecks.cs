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

// 2026-10-06: 원정지 수동 채집 1회 왕복 후 운송선 앞에서 멈추는 버그 재현·회귀 검사(WorldMapChecks 해당 구간만 빠르게).
public static class ExpeditionGatherChecks
{
    static int checks;
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); checks++; }
    static object Get(object target, string field)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        { var f = type.GetField(field, Flags); if (f != null) return f.GetValue(target); }
        return "missing";
    }
    static async Task<bool> Wait(Func<bool> condition, int seconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
        return condition();
    }
    static void Move(CommanderAnt c, Vector3 position)
    {
        c.CommandStop(); c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas), "walkable test position");
        c.transform.position = hit.position; c.ApplyMovementMode(); Physics.SyncTransforms();
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode required");
        Check(await Wait(() => !SaveSystem.Busy, 60), "menu ready");
        SaveSystem.NewGame(new NewGameOptions { seed = 261013, mapSize = MapSize.Small, biome = MapBiome.Forest });
        Check(await Wait(() => !SaveSystem.Busy, 90), "new game ready");
        Time.timeScale = 1;
        var world = WorldMapManager.Instance;
        var resources = ResourceManager.Instance;
        foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { resources.AddCapacity(type, 10000); resources.Add(type, 8000); }
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); typeof(WorkerAnt).GetMethod("SuspendWork", Flags).Invoke(c, null); }
        var commander = CommanderRoster.Instance.Commanders[0];

        // 운송선: 연구소를 바로 두고 연구·건조는 Tick으로 끝낸다.
        Check(world.CanCreateTransport(commander.Position, out var labPos), "room for lab");
        var template = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(l => l.name.EndsWith("Template"));
        var lab = Object.Instantiate(template, labPos, Quaternion.identity); lab.gameObject.SetActive(true);
        await Task.Yield();
        Check(lab.TryUpgrade() && lab.TryResearch(false), "vehicle research starts");
        var scientist = CommanderRoster.Instance.Commanders.Skip(1).First(x => x.CanDoJob(CommanderJobs.Research));
        Move(scientist, lab.Position + Vector3.right * 3);
        Check(lab.TryAssign(scientist), "researcher assigned");
        CampaignResearch.Instance.Tick(10000);
        Check(world.VehicleResearched && lab.TryConstruct(false), "vehicle construction starts");
        lab.Tick(ScienceLab.ConstructionSeconds);
        Check(world.Transports.Count == 1, "transport built");
        var vehicle = world.Transports[0];

        // 원정 → 중립 자원지에서 노드 2개를 차례로 수동 채집
        var field = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
        Move(commander, vehicle.Position + Vector3.right * 3);
        commander.WorkState.duty = CommanderDuty.Deployed;
        Check(commander.TryAssign(5), "crew mobilized");
        Check(vehicle.TryBoard(new[] { commander }) && vehicle.TryDepart(field), "departs to resource site");
        vehicle.Tick(vehicle.TravelSeconds);
        Check(commander.IsAwayFromHome && !commander.IsEmbarked, "crew landed");
        var nodes = field.GetComponentsInChildren<ResourceNode>();
        Check(nodes.Length == 2, "two nodes");
        var round = 0;
        foreach (var node in nodes)
        {
            round++;
            typeof(ResourceNode).GetField("amountRemaining", Flags).SetValue(node, 1f);
            Move(commander, node.transform.position + Vector3.left);
            commander.CommandGather(node);
            var ok = await Wait(() => node.IsDepleted && !commander.IsCarrying, 45);
            Check(ok, $"round {round} harvest returns to transport: remaining={node.AmountRemaining} carrying={commander.IsCarrying} state={Get(commander, "state")}"
                + $" deposit={Get(commander, "targetDeposit")} cargo={vehicle.CargoLoad}/{vehicle.CargoCapacity} agentOn={commander.Agent.enabled} navOn={commander.Agent.isOnNavMesh}"
                + $" path={commander.Agent.pathStatus} remain={commander.Agent.remainingDistance:0.00} stop={commander.Agent.stoppingDistance:0.00} hasPath={commander.Agent.hasPath}"
                + $" pos={commander.Position} dest={commander.Agent.destination} vehicle={vehicle.Position} dist={Vector3.Distance(commander.Position, vehicle.Position):0.00} ts={Time.timeScale}");
            Check(vehicle.GetCargo(node.ResourceType) == 1, "cargo stored " + round);
        }
        Check(vehicle.TryReturn(), "returns");
        vehicle.Tick(vehicle.TravelSeconds);
        return "PASS " + checks + " expedition gather checks";
    }
}
