using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

// 새 Play 세션에서 eval. 이 파일은 Editor 어셈블리에서 별도로 컴파일한다.
public static class ImplD06D07Checks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("D06/D07 FAIL: " + label); checks++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .002f;
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(25);
        Check(!AntColony.Save.SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(AntColony.Units.CommanderAnt c, Vector3 at)
    {
        c.CommandStop(); c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(at, out var hit, 8, NavMesh.AllAreas), "walkable fixture");
        c.transform.position = hit.position; c.ApplyMovementMode();
    }
    static void Deploy(AntColony.World.ExpeditionTransport ship, AntColony.World.ExpeditionSite site,
        System.Collections.Generic.List<AntColony.Units.CommanderAnt> crew)
    {
        Call(ship, "RestoreState", AntColony.World.ExpeditionState.Deployed, 0f, site, site.Landing, ship.Position, 0, 0, 0);
        Call(ship, "RestoreCrew", crew);
        foreach (var c in crew) Move(c, ship.Position + Vector3.right);
    }
    static float Floor(AntColony.Data.ResourceType type, Vector3 at) => AntColony.World.ResourceNode.Available
        .Where(n => n != null && n.IsLooseCargo && n.ResourceType == type && (n.transform.position - at).sqrMagnitude < 25).Sum(n => n.AmountRemaining);

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = AntColony.Save.SaveStorage.RootOverride;
        bool oldAuto = AntColony.Core.UserSettings.Current.autoSaveEnabled, oldPause = AntColony.Core.UserSettings.Current.autoPauseThreat;
        float oldTime = Time.timeScale;
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "D06D07-" + Guid.NewGuid().ToString("N"));
        AntColony.Core.UserSettings.Current.autoSaveEnabled = false; AntColony.Core.UserSettings.Current.autoPauseThreat = false;
        try
        {
            await Ready();
            var team = Enumerable.Range(0, 3).Select(i => AntColony.Core.StartingCandidate.Roll(i)).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions { seed = 251025, startingCount = 3, startingTeam = team });
            await Ready();
            var world = AntColony.World.WorldMapManager.Instance;
            Call(world, "RestoreUnlock", true);
            var crew = AntColony.Core.CommanderRoster.Instance.Commanders.Take(3).ToList();
            foreach (var c in crew) { c.SetJobEnabled(AntColony.Units.CommanderJobs.All, false); c.CommandStop(); }
            var site = world.Sites.First(s => s.Kind == AntColony.World.ExpeditionSiteKind.Settlement);
            Call(site, "RestoreState", true, AntColony.World.ConquestDisposition.Annexed);
            var ship = world.CreateTransport(false, crew[0].Position);
            Check(Near(ship.MaxHealth, 400) && Near(ship.CurrentHealth, 400), "vehicle full HP");
            var air = world.CreateTransport(true, ship.Position + Vector3.forward * 8);
            Check(Near(air.MaxHealth, 600) && Near(air.CurrentHealth, 600), "aircraft full HP");
            foreach (float max in new[] { 400f, 600f })
            {
                Check(AntColony.World.TransportDamage.Speed(max * .249f, max) == 0, "below 25 disabled");
                Check(Near(AntColony.World.TransportDamage.Speed(max * .25f, max), .75f), "25 included slow");
                Check(Near(AntColony.World.TransportDamage.Speed(max * .499f, max), .75f), "below 50 slow");
                Check(AntColony.World.TransportDamage.Speed(max * .5f, max) == 1, "50 included normal");
            }
            foreach (bool aircraft in new[] { false, true })
            {
                int paid = 0, previous = 0;
                for (int n = 1; n <= 1000; n++)
                {
                    int current = AntColony.World.TransportDamage.RepairCost(aircraft, n / 1000f);
                    paid += current - previous; previous = current;
                }
                Check(paid == AntColony.World.TransportDamage.MaterialCost(aircraft) / 2, "cumulative rounding equals single repair");
            }
            Deploy(ship, site, crew);
            Check(ship.TryReturn() && ship.IsWithdrawing && ship.State == AntColony.World.ExpeditionState.Deployed, "return queues instead of teleporting");
            Check(ship.WithdrawalProgress.People == 3 && Near(ship.WithdrawalProgress.EstimatedSeconds, 2), "two-slot ETA");
            ship.Tick(.5f);
            Check(crew.All(c => !c.IsEmbarked), "half second boards nobody");
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out var error), "save mid-withdrawal: " + error);
            string json = File.ReadAllText(AntColony.Save.SaveSlots.PathFor(false, 0));
            Check(AntColony.Save.SaveValidator.TryParse(json, out var saved, out error), "Validator mid-withdrawal: " + error);
            Check(AntColony.Save.SavePreflight.Validate(saved, out error), "Preflight mid-withdrawal: " + error);
            Check(saved.version == AntColony.Save.SaveFileV1.CurrentVersion && Near(saved.world.transports[0].withdrawal.jobs[0].progress, .5f), "version and progress saved");
            // 삭제한 inline 클래스가 빈 객체가 되어도 marker=false / active=false로 판정해야 한다.
            var legacy = Newtonsoft.Json.Linq.JObject.Parse(json);
            foreach (Newtonsoft.Json.Linq.JObject t in legacy["world"]["transports"])
            { t.Remove("damage"); t.Remove("withdrawal"); t.Remove("cargoAmounts"); t.Remove("bodyCargo"); }
            Check(AntColony.Save.SaveValidator.TryParse(legacy.ToString(), out var old, out error), "old save without fields: " + error);
            Call(air, "RestoreDamage", old.world.transports[1].damage);
            Check(Near(air.CurrentHealth, 600), "missing damage means full aircraft");
            var bad = Newtonsoft.Json.Linq.JObject.Parse(json);
            bad["world"]["transports"][0]["damage"]["health"] = 401;
            Check(!AntColony.Save.SaveValidator.TryParse(bad.ToString(), out _, out _), "over-max HP rejected");
            bad = Newtonsoft.Json.Linq.JObject.Parse(json);
            bad["world"]["transports"][0]["withdrawal"]["jobs"][0]["commanderId"] = "missing";
            Check(!AntColony.Save.SaveValidator.TryParse(bad.ToString(), out _, out _), "missing passenger rejected");
            Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 0), out error), "load mid-withdrawal: " + error);
            await Ready(); world = AntColony.World.WorldMapManager.Instance; ship = world.Transports[0]; crew = ship.Crew.ToList(); site = ship.Site;
            Check(ship.IsWithdrawing && crew.All(c => !c.IsEmbarked), "pending reservations not boarded on load");
            ship.Tick(.5f); Check(crew.Count(c => c.IsEmbarked) == 2, "exactly two parallel jobs complete");
            ship.Tick(1); Check(ship.State == AntColony.World.ExpeditionState.Returning && crew.All(c => c.IsEmbarked), "third passenger completes next second");
            Deploy(ship, site, crew);
            ship.DepositResources(AntColony.Data.ResourceType.Wood, 3);
            ship.DepositResources(AntColony.Data.ResourceType.Stone, 4);
            ship.DepositResources(AntColony.Data.ResourceType.Iron, 5);
            Check(ship.ReservedCargo == 12 && ship.CargoLoad == 0, "physical pending cargo distinct from loaded");
            ship.Tick(1.99f); Check(ship.CargoLoad == 0, "no early cargo commit");
            ship.Tick(.01f); Check(ship.CargoLoad == 7 && ship.ReservedCargo == 5, "two load jobs share slots");
            ship.Tick(2); Check(ship.CargoLoad == 12 && ship.ReservedCargo == 0, "third load completes");
            var loader = crew[0];
            Call(loader, "RestoreCargo", 7f, AntColony.Data.ResourceType.Wood, null);
            Check(!(bool)Call(ship, "TryLoadFromWorker", loader, AntColony.Data.ResourceType.Wood, 7)
                && Near(loader.CarriedAmount, 7) && ship.ReservedCargo == 7, "worker retains physical cargo during loading reservation");
            Check(AntColony.Save.SavePreflight.Validate(AntColony.Save.SaveSnapshot.Capture(), out error), "worker loading reservation save validation: " + error);
            ship.Tick(2.001f / loader.WorkRate(AntColony.Units.CommanderActivity.Strength));
            Check(Near(loader.CarriedAmount, 7) && ship.CargoLoad == 12, "load duration alone does not duplicate worker cargo");
            Check((bool)Call(ship, "TryLoadFromWorker", loader, AntColony.Data.ResourceType.Wood, 7), "worker atomically confirms completed loading");
            Call(loader, "RestoreCargo", 0f, AntColony.Data.ResourceType.Wood, null); // WorkerAnt.Deposit의 실물 제거 경계.
            Check(ship.CargoLoad == 19 && ship.ReservedCargo == 0, "worker cargo counted exactly once after commit");
            // 非식량 고체 건조비(60)의 50%를 손실 비율만큼 사용. 현장에 실린 흙만 소비한다.
            ship.TakeDamage(360); Check(Near(ship.CurrentHealth, 40) && !ship.CanOperate && !ship.TryReturn(), "combat damage disables departure");
            var c0 = crew[0]; float rate = c0.WorkRate(AntColony.Units.CommanderActivity.Building);
            float hp = ship.CurrentHealth;
            ship.WorkRepair(c0, 1); Check(Near(ship.CurrentHealth, hp), "no material waits");
            ship.AddCargo(AntColony.Data.ResourceType.Soil, 30);
            Check(ship.WorkRepair(c0, 7.5f / rate), "emergency repair remains incomplete");
            Check(Near(ship.CurrentHealth, 100) && ship.GetCargo(AntColony.Data.ResourceType.Soil) == 25 && Near(ship.TravelSpeedMultiplier, .75f), "10 to 25 percent costs five actual recipe materials");
            var repairSave = AntColony.Save.SaveSnapshot.Capture();
            Check(AntColony.Save.SavePreflight.Validate(repairSave, out error) && Math.Abs(repairSave.world.transports[0].damage.repairedFraction - .15) < .00001,
                "cumulative repair cost basis survives snapshot: " + error);
            Check(ship.TryReturn(), "repaired ship can withdraw");
            for (int i = 0; i < 8 && ship.IsWithdrawing; i++) ship.Tick(.5f);
            Check(ship.State == AntColony.World.ExpeditionState.Returning, "repair departure completes");
            ship.Tick(4); Check(Near(ship.Remaining, ship.TravelSeconds - 3), "damage slows actual world travel");
            Deploy(ship, site, crew);
            ship.AddCargo(AntColony.Data.ResourceType.Soil, 25); // Deploy 도우미(RestoreState)가 화물을 0으로 초기화한다
            crew[1].WorkState.health = 0;
            Check(ship.TryReturn(), "injured withdrawal accepted");
            float lift = AntColony.World.ExpeditionWithdraw.LiftSeconds / crew[0].WorkRate(AntColony.Units.CommanderActivity.Strength);
            ship.Tick(lift + .001f);
            Check(!crew[1].IsEmbarked && Near(AntColony.World.ExpeditionWithdraw.CarryMultiplier(crew[0]), .75f), "lift does not board and carrying speed .75");
            var at = crew[1].Position; ship.CancelWithdrawal();
            Check(!crew[1].IsEmbarked && (crew[1].Position - at).sqrMagnitude < .01f && Near(AntColony.World.ExpeditionWithdraw.CarryMultiplier(crew[0]), 1), "cancel drops injured at current position");
            crew[1].WorkState.health = 100;
            var corpse = AntColony.World.Corpse.Spawn(new AntColony.World.Corpse.State { name = "검사 유해", commanderId = Guid.NewGuid().ToString("N"),
                position = c0.Position, kind = AntColony.World.CorpseKind.ColonyCommander, count = 1, remaining = 0 });
            Check(ship.TryRecoverBody(corpse, c0) && ship.ReservedCargo == 5, "one remains reserves five cargo");
            ship.Tick(2.001f / c0.WorkRate(AntColony.Units.CommanderActivity.Strength));
            Check(ship.BodyCargo.Count == 1 && ship.ReservedCargo == 0, "body commits only after loading");
            var before = Floor(AntColony.Data.ResourceType.Soil, ship.Position);
            ship.TakeDamage(float.MaxValue);
            Check(ship.IsWreck && !ship.TryReturn() && !ship.TryRepair(c0) && ship.BodyCargo.Count == 0, "irreversible wreck releases body");
            Check(site.Visitor != ship, "wreck does not occupy settlement dock");
            Check(Near(Floor(AntColony.Data.ResourceType.Soil, ship.Position), before + 25), "cargo falls separately from salvage");
            rate = c0.WorkRate(AntColony.Units.CommanderActivity.Building);
            Check(ship.TrySalvage(c0), "begin salvage"); ship.Tick(5 / rate);
            Check(Near(ship.SalvageRemaining, 5) && !ship.Salvaged, "half salvage has no reward");
            Check(AntColony.Save.SaveSystem.TrySave(false, 1, out error), "save half salvage: " + error);
            Check(AntColony.Save.SaveSystem.TryLoad(AntColony.Save.SaveSlots.PathFor(false, 1), out error), "load half salvage: " + error);
            await Ready(); world = AntColony.World.WorldMapManager.Instance; ship = world.Transports[0]; crew = ship.Crew.ToList(); c0 = crew[0]; site = ship.Site;
            rate = c0.WorkRate(AntColony.Units.CommanderActivity.Building);
            Check(ship.IsWreck && Near(ship.CurrentHealth, 0) && Near(ship.SalvageRemaining, 5) && ship.FieldWorkActive, "zero HP and salvage worker/progress restored");
            ship.Tick(5.001f / rate);
            Check(ship.Salvaged && Near(Floor(AntColony.Data.ResourceType.Soil, ship.Position), before + 40), "ten-second salvage gives floor(60*.25) once");
            ship.TakeDamage(100); ship.Tick(100);
            Check(!ship.TrySalvage(c0) && Near(Floor(AntColony.Data.ResourceType.Soil, ship.Position), before + 40), "no duplicate salvage or resurrection");
            Check(AntColony.Save.SaveSystem.TrySave(false, 1, out error), "wreck and salvage saved: " + error);
            var escape = world.CreateTransport(false, ship.Position + Vector3.forward * 6);
            foreach (var c in crew.Take(2)) Call(ship, "DetachCommander", c);
            Deploy(escape, site, crew.Take(2).ToList());
            AntColony.Core.AntPool.Instance.Breed(100);
            Check(AntColony.Core.AntPool.Instance.TryAssign(15), "fixture assigns fifteen soldiers");
            Call(c0, "RestoreTroops", 15, 0f);
            Check(escape.TryReturn(), "start grouped troop boarding"); escape.Tick(10);
            Check(escape.TroopPassengers == 10 && c0.TroopCount == 5 && !c0.IsEmbarked, "completed ten-person group separated from ground soldiers");
            Check(escape.TryEmergencyReturn() && escape.Load == 10 && escape.Crew.Count == 1 && c0.Garrison == site.Settlement,
                "emergency takes completed group and boarded commander only");
            Check(AntColony.Save.SaveSystem.TrySave(false, 2, out error), "anonymous boarded troops preserve Assigned ledger: " + error);
            int free = AntColony.Core.AntPool.Instance.Free;
            escape.Tick(escape.TravelSeconds);
            Check(escape.State == AntColony.World.ExpeditionState.Home && escape.TroopPassengers == 0
                && AntColony.Core.AntPool.Instance.Free == free + 10 && c0.TroopCount == 5, "orphan passengers returned once, ground troops retained");
            var result = "PASS: " + checks + " D06/D07 checks"; File.WriteAllText("Temp/save-d06d07-result.txt", result); return result;
        }
        finally
        {
            AntColony.Save.SaveStorage.RootOverride = oldRoot;
            AntColony.Core.UserSettings.Current.autoSaveEnabled = oldAuto; AntColony.Core.UserSettings.Current.autoPauseThreat = oldPause; Time.timeScale = oldTime;
        }
    }
}
