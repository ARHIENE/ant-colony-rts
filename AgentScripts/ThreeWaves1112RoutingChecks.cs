using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

// Separate disposable fixture before the natural playthrough. Never a balance result.
public static class ThreeWaves1112RoutingChecks
{
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log = "";
    static void Check(bool pass, string name) { log += (pass ? "PASS " : "FAIL ") + name + "\n"; File.WriteAllText("work/three-waves-1112/routing-checks.txt", log); if (!pass) throw new Exception(name); }
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Isolated prepared Play required");
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
        var c = team[0]; var rm = ResourceManager.Instance;
        var site = SceneObjects.All<BuildingConstructionSite>().First(s => s.isActiveAndEnabled && s.BuildingKind == BuildingKind.Storage);
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, new object[] { BuildingKind.Storage, UnitRole.Worker });
        var go = UnityEngine.Object.Instantiate(template, new Vector3(20000, 0, 20000), Quaternion.identity); go.SetActive(true);
        await Task.Delay(200);
        var storage = go.GetComponent<Storage>();
        Check(storage.Bin != null && rm.AddTo(storage.Bin, ResourceType.Soil, 12) == 12, "fixture remote stocked warehouse");
        NavMesh.SamplePosition(c.Position + Vector3.right * 5, out var floorHit, 3, NavMesh.AllAreas);
        typeof(DiplomacyManager).GetMethod("DropFloor", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(ResourceType), typeof(int), typeof(Vector3) }, null)
            .Invoke(null, new object[] { ResourceType.Soil, 12, floorHit.position });
        await Task.Delay(150);
        var source = typeof(WorkerAnt).GetMethod("TryMaterialSource", Hidden);
        var distance = typeof(CommanderAnt).GetMethod("WorkTravelDistance", Hidden);
        var need = site.Needs.First(n => n.type == ResourceType.Soil); need.reserved = 10;
        object[] Pick() => new object[] { site, ResourceType.Soil, 10, null, null, Vector3.zero, 0 };
        var args = Pick();
        Check((bool)source.Invoke(c, args) && args[3] == null && args[4] is ResourceNode, "unreachable warehouse falls back to nearby loose material");
        Check(!float.IsInfinity((float)distance.Invoke(c, new object[] { site, true })), "fallback pickup has finite work distance");
        Check(c.CommandFetchFor(site) && c.CurrentResourceNode == (ResourceNode)args[4], "issued order uses the same floor source"); c.CommandStop();
        NavMesh.SamplePosition(c.Position + Vector3.left * 7, out var storeHit, 3, NavMesh.AllAreas);
        go.transform.position = storeHit.position; Physics.SyncTransforms();
        need.reserved = 10; args = Pick();
        Check((bool)source.Invoke(c, args) && ReferenceEquals(args[3], storage.Bin) && args[4] == null, "reachable warehouse remains preferred over floor");
        Check(c.CommandFetchFor(site) && ReferenceEquals(typeof(WorkerAnt).GetField("fetchBin", Hidden).GetValue(c), storage.Bin), "issued order uses the same warehouse"); c.CommandStop();
        foreach (var n in site.Needs) { n.delivered = n.need; n.reserved = 0; }
        foreach (var hero in team) { hero.CommandStop(); hero.PersonalState.sleep.asleep = true; }
        Check(site.WaitReason == BuildingConstructionSite.Wait.WorkerBusy, "all sleeping builders report busy, not unreachable");
        c.PersonalState.sleep.asleep = false; c.Agent.enabled = false;
        Check(c.CivilianWorkReady, "awake builder still eligible for work");
        Check(team[1].TryWorkApproach(site.Position, out _), "sleeping builder has a physical route");
        Check(site.WaitReason == BuildingConstructionSite.Wait.Unreachable, "sleeping reachable builder does not hide awake builder's blocked route");
        c.Agent.enabled = true;
        return log + "Disposable fixture must be discarded before natural play.";
    }
}
