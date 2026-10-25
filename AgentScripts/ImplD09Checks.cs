using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

// 실제 Route.Tick → 원정 이동 → 철수 → 다음 거점/본거지 경로를 사용한다. 빈 거점으로 채집 시간만 분리한다.
public static class ImplD09Checks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("D09 FAIL: " + label); checks++; }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(25);
        Check(!AntColony.Save.SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Rally(AntColony.World.ExpeditionTransport ship)
    {
        foreach (var c in ship.Crew)
        {
            c.CommandStop(); c.Agent.enabled = false;
            Check(NavMesh.SamplePosition(ship.Position + Vector3.right, out var hit, 8, NavMesh.AllAreas), "rally ground");
            c.transform.position = hit.position; c.ApplyMovementMode();
        }
    }
    static void FinishWithdrawal(AntColony.World.ExpeditionTransport ship)
    {
        for (int n = 0; n < 200 && ship.IsWithdrawing; n++)
        {
            ship.Tick(.25f); ship.Route.Tick(.25f);
            Check(ship.Route.IsRunning, "boarding soldier ledger does not cancel route collectors");
        }
        Check(!ship.IsWithdrawing, "withdrawal completes within actual boarding work");
    }
    static void Arrive(AntColony.World.ExpeditionTransport ship, AntColony.World.ExpeditionSite expected)
    {
        Check(ship.State == AntColony.World.ExpeditionState.Outbound && ship.Site == expected && ship.Remaining > 0, "ordered leg has real travel time");
        ship.Tick(ship.TravelSeconds);
        Check(ship.State == AntColony.World.ExpeditionState.Deployed && ship.Site == expected, "arrives at designated next stop");
        Rally(ship);
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = AntColony.Save.SaveStorage.RootOverride; float oldTime = Time.timeScale;
        bool oldAuto = AntColony.Core.UserSettings.Current.autoSaveEnabled, oldPause = AntColony.Core.UserSettings.Current.autoPauseThreat;
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "D09-" + Guid.NewGuid().ToString("N"));
        AntColony.Core.UserSettings.Current.autoSaveEnabled = false; AntColony.Core.UserSettings.Current.autoPauseThreat = false;
        try
        {
            await Ready(); var candidate = AntColony.Core.StartingCandidate.Roll(0); candidate.traits.values.Clear();
            AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions { seed = 251026, startingCount = 1, startingTeam = new[] { candidate } });
            await Ready();
            var world = AntColony.World.WorldMapManager.Instance; Call(world, "RestoreUnlock", true);
            var c = AntColony.Core.CommanderRoster.Instance.Commanders[0]; c.SetJobEnabled(AntColony.Units.CommanderJobs.All, false);
            AntColony.Core.AntPool.Instance.Breed(100); c.WorkState.duty = AntColony.Units.CommanderDuty.Deployed;
            Check(c.TryAssign(5), "route collector troops");
            var stops = world.Sites.Where(s => s.Kind == AntColony.World.ExpeditionSiteKind.Settlement).Take(3).ToArray();
            Check(stops.Length == 3, "three independent fixtures");
            foreach (var s in stops)
            {
                Call(s, "RestoreState", true, AntColony.World.ConquestDisposition.Annexed);
                foreach (var node in s.Colony.GetComponentsInChildren<AntColony.World.ResourceNode>(true)) node.Extract(node.AmountRemaining);
            }
            var ship = world.CreateTransport(false, c.Position + Vector3.forward * 3);
            MoveHome(c, ship); Check(ship.TryBoard(new[] { c }), "board route collector");
            Check(!ship.Route.TryStart(new[] { stops[0], stops[0] }) && !ship.Route.TryStart(new[] { stops[0], stops[1], stops[2], stops[0] }), "duplicates and four stops rejected");
            Check(ship.Route.TryStart(stops), "start three stops"); ship.Route.Tick(.1f);
            for (int leg = 0; leg < 3; leg++)
            {
                Arrive(ship, stops[leg]); Check(ship.AddCargo(AntColony.Data.ResourceType.Wood, 1) == 1, "retain cumulative cargo");
                ship.Route.Tick(.1f);
                Check(ship.IsWithdrawing && ship.State == AntColony.World.ExpeditionState.Deployed, "leg transition waits for boarding");
                ship.Route.Tick(.1f); Check(ship.Route.IsRunning, "pending continuation preserves route despite next destination");
                if (leg == 1)
                {
                    Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var error), "save active middle leg: " + error);
                    string json = File.ReadAllText(AntColony.Save.SaveSlots.PathFor(false, 0));
                    Check(AntColony.Save.SaveValidator.TryParse(json, out var file, out error), "validate middle-leg cursor: " + error);
                    Check(file.world.transports[0].route.leg == 2 && file.world.transports[0].route.stops.Count == 3, "visited progress persisted");
                    Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 0), out error), "resume route: " + error);
                    await Ready(); world = AntColony.World.WorldMapManager.Instance; ship = world.Transports[0]; c = ship.Crew[0];
                    stops = ship.Route.Stops.ToArray();
                }
                FinishWithdrawal(ship);
                if (leg < 2) Check(ship.State == AntColony.World.ExpeditionState.Outbound && ship.Site == stops[leg + 1], "1 to 2 to 3 without revisiting");
            }
            Check(ship.State == AntColony.World.ExpeditionState.Returning && ship.GetCargo(AntColony.Data.ResourceType.Wood) == 3, "third stop returns with all cargo");
            ship.Tick(ship.TravelSeconds); Check(ship.State == AntColony.World.ExpeditionState.Home && !ship.HasCargo, "home unload before repeat");
            ship.Route.Tick(.1f); Check(ship.State == AntColony.World.ExpeditionState.Home && ship.Route.WaitSeconds > 0, "home repeat interval retained");
            ship.Route.Stop(); MoveHome(c, ship); Check(ship.TryBoard(new[] { c }), "board second cycle");
            Check(ship.Route.TryStart(stops.Take(2).ToArray()), "start two stops"); ship.Route.Tick(.1f); Arrive(ship, stops[0]);
            ship.AddCargo(AntColony.Data.ResourceType.Wood, ship.CargoCapacity - 1);
            Check(ship.ReturnCargoSpace == 1, "one-unit free cargo is sufficient");
            ship.Route.Tick(.1f); FinishWithdrawal(ship); Arrive(ship, stops[1]);
            ship.DepositResources(AntColony.Data.ResourceType.Stone, 2);
            Check(ship.ReturnCargoSpace == 0 && ship.ReservedCargo == 1, "one load split to free cargo and reservation subtracted once");
            ship.Route.Tick(.1f); Check(!ship.IsWithdrawing, "pending load blocks leave decision");
            ship.Tick(2); ship.Route.Tick(.1f); FinishWithdrawal(ship);
            Check(ship.State == AntColony.World.ExpeditionState.Returning && ship.CargoFull, "two-stop cycle returns when exactly full");
            ship.Tick(ship.TravelSeconds); ship.Route.Stop(); MoveHome(c, ship); Check(ship.TryBoard(new[] { c }), "board full-first-stop cycle");
            Check(ship.Route.TryStart(stops), "start capacity rejection trip"); ship.Route.Tick(.1f); Arrive(ship, stops[0]);
            ship.AddCargo(AntColony.Data.ResourceType.Wood, ship.CargoCapacity);
            ship.Route.Tick(.1f); FinishWithdrawal(ship);
            Check(ship.State == AntColony.World.ExpeditionState.Returning && ship.Site == stops[0], "zero free cargo skips second and third stops");
            ship.Route.Stop(); Check(ship.HasCargo, "Stop Auto preserves loaded cargo");
            var result = "PASS: " + checks + " D09 ordered 2/3-stop / capacity / save-load checks"; File.WriteAllText("Temp/save-d09-result.txt", result); return result;
        }
        finally
        {
            AntColony.Save.SaveStorage.RootOverride = oldRoot;
            AntColony.Core.UserSettings.Current.autoSaveEnabled = oldAuto; AntColony.Core.UserSettings.Current.autoPauseThreat = oldPause; Time.timeScale = oldTime;
        }
    }
    static void MoveHome(AntColony.Units.CommanderAnt c, AntColony.World.ExpeditionTransport ship)
    {
        c.CommandStop(); c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(ship.Position + Vector3.right, out var hit, 8, NavMesh.AllAreas), "home boarding point");
        c.transform.position = hit.position; c.ApplyMovementMode();
    }
}
