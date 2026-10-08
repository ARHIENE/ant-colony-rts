using System;
using System.IO;
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
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

public static class AutonomousDutyChecks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + count + ": " + label); count++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode"); var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Duty-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260926, mapSize = MapSize.Small }); await Ready();
            var list = CommanderRoster.Instance.Commanders.ToArray(); var c = list[0]; var other = list[1];
            Check(list.All(x => x.TroopCount == 0 && !x.IsDeployed), "new commanders are civilians");
            Check(AntPool.Instance.Assigned == 0, "no starting troop assignment");
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            Check(c.CanStartConstruction, "civilian can construct without troops");
            Check(CombatTargeting.IsAlive(c), "civilian targetable");
            var hp = c.PersonalHealth; c.TakeDamage(3 + c.Armor);
            Check(c.PersonalHealth == hp - 3, "civilian damage hits personal health");
            Check(!c.TryAssign(1), "civilian cannot bypass conscription");
            Check(!c.SetJobEnabled((CommanderJobs)8192, true), "invalid job rejected"); // 4096은 치우기(Cleaning)
            var node = ResourceNode.Available.First(n => n.CanGather && !n.IsRaidLoot && n.GetComponentInParent<ExpeditionSite>() == null && c.TryWorkApproach(n.transform.position, out _));
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = true;
            c.SetJobEnabled(CommanderJobs.Gathering | CommanderJobs.Fishing | CommanderJobs.Farming, true);
            c.TickDuty(2); Check(!c.IsWorking, "forbidden nodes skipped");
            node.GatheringForbidden = false; c.TickDuty(2);
            Check(c.CurrentResourceNode == node && c.IsWorking, "autonomous gathering starts");
            c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop();
            c.TickDuty(2); Check(!c.IsWorking, "disabled work stays idle");
            node.GatheringForbidden = true;
            var pile = new GameObject("Automatic gather check").AddComponent<ResourceNode>();
            pile.transform.position = c.Position; pile.ConfigureLoot(Resource.Soil, 30);
            int soilBefore = ResourceManager.Instance.GetAmount(Resource.Soil);
            c.SetJobEnabled(CommanderJobs.Gathering | CommanderJobs.Hauling, true); // 바닥 전리품은 운반 작업(2026-09-28)
            var until = DateTime.UtcNow.AddSeconds(25); Time.timeScale = 8;
            while (ResourceManager.Instance.GetAmount(Resource.Soil) < soilBefore + 15 && DateTime.UtcNow < until) await Task.Delay(50);
            Time.timeScale = 0;
            Check(ResourceManager.Instance.GetAmount(Resource.Soil) >= soilBefore + 15, "automatic gathering returns and repeats deliveries");
            c.SetJobEnabled(CommanderJobs.All, false);
            typeof(WorkerAnt).GetMethod("SuspendWork", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = true;
            var siteGo = new GameObject("Duty blueprint"); var site = siteGo.AddComponent<BuildingConstructionSite>();
            siteGo.transform.position = c.Position; site.Initialize(null, 10);
            c.SetJobEnabled(CommanderJobs.Building, true); c.TickDuty(2);
            Check(c.ConstructionTarget == site && site.HasBuilder, "autonomous builder claims blueprint");
            other.CommandBuild(site); Check(!other.IsConstructing, "blueprint has only one builder");
            c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); await Task.Delay(100);
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { BuildingKind.ConscriptionPost, UnitRole.Worker });
            Check(template != null && !template.activeSelf, "conscription template");
            var postGo = Object.Instantiate(template, c.Position + Vector3.right * 5, Quaternion.identity);
            postGo.name = "ConscriptionPost"; postGo.SetActive(true); var post = postGo.GetComponent<ConscriptionPost>();
            Check(BuildingPlacementController.LockReason(BuildingKind.ConscriptionPost) != null, "one home post limit");
            // Phase 4 병역 상한(모병제 5%)에 막히지 않게 국민개병(40%)으로 둔다.
            var granted = CampaignResearch.Instance.CaptureState(); granted.completed.Add((int)ScienceTechnology.TotalMobilization); CampaignResearch.Instance.RestoreState(granted);
            Check(ColonyPopulation.Instance.TrySetPolicy(MilitaryPolicy.Total), "total mobilization policy");
            var free = AntPool.Instance.Free;
            Check(!post.TryDeploy(new[] { c, c }, new[] { 2, 2 }) && AntPool.Instance.Free == free, "duplicate deployment atomic");
            Check(!post.TryDeploy(new[] { c, other }, new[] { 2, other.CommandLimit + 1 }) && AntPool.Instance.Free == free, "invalid batch atomic");
            var suspended = new GameObject("Suspended blueprint").AddComponent<BuildingConstructionSite>();
            suspended.transform.position = c.Position; suspended.Initialize(null, 10); c.CommandBuild(suspended);
            Check(post.TryDeploy(new[] { c, other }, new[] { 4, 3 }), "formation deployment: lim=" + c.CommandLimit + "/" + other.CommandLimit + " free=" + AntPool.Instance.Free + " max=" + (AntColony.Core.ColonyPopulation.Instance == null ? -1 : AntColony.Core.ColonyPopulation.Instance.MaxSoldiers(false)) + " assigned=" + AntPool.Instance.Assigned);
            Check(suspended != null && !suspended.HasBuilder && !c.IsConstructing && suspended.RemainingWork == 10, "deployment preserves prepaid blueprint");
            suspended.Complete(); await Task.Delay(50);
            Check(c.IsDeployed && c.TroopCount == 4 && other.TroopCount == 3 && AntPool.Instance.Free == free - 7, "deployment accounting");
            Check(!c.CanStartConstruction, "deployed does not build");
            c.CommandGather(node); Check(!c.IsWorking, "deployed does not gather at home");
            hp = c.PersonalHealth; c.TakeDamage(2 + c.Armor);
            Check(c.TroopCount == 2 && c.PersonalHealth == hp, "troops absorb first damage");
            c.TakeDamage(3 + c.Armor);
            Check(c.TroopCount == 0 && c.PersonalHealth == hp - 1, "overflow hits personal health");
            Check(c.CanReceiveOrders && CombatTargeting.IsAlive(c), "zero troops remains alive and controllable");
            Check(other.ReturnToPost(), "manual return accepted");
            other.Agent.Warp(other.WorkState.returnPosition); other.TickDuty(.1f);
            Check(!other.IsDeployed && other.TroopCount == 0 && AntPool.Instance.Free == free - 4, "survivors returned to pool");
            c.Agent.Warp(c.WorkState.returnPosition);
            foreach (var monster in Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None)) monster.enabled = false;
            c.TickDuty(19);
            Check(!c.IsReturning, "return waits 20 seconds"); c.TickDuty(1);
            Check(c.IsReturning, "quiet period triggers return; quiet=" + c.WorkState.quietSeconds + " reachable=" + c.CanReach(c.WorkState.returnPosition) + " enemy=" + CombatTargeting.FindNearestEnemy(c.Position, GameBalance.ReturnEnemyRadius, c.Role)); c.TickDuty(.1f);
            Check(!c.IsDeployed, "auto return resumes civilian duty");
            var lab = Build<ScienceLab>(BuildingKind.ScienceLab, c.Position + Vector3.left * 5);
            var research = CampaignResearch.Instance;
            Check(research.TryStart(ScienceTechnology.Resin), "research order queued");
            c.SetJobEnabled(CommanderJobs.Research, true); c.TickDuty(2);
            Check(lab.Target == null && c.ScienceAssignment == null, "research is not picked up autonomously (2026-10-08)");
            Check(c.SendToResearch(lab), "player designates researcher"); for (var i = 0; i < 40 && c.ScienceAssignment == null; i++) { c.TickDuty(.5f); await Task.Delay(50); }
            Check(lab.Target == c && c.ScienceAssignment == lab, "designated researcher reaches lab");
            research.Tick(10000); Check(research.Has(ScienceTechnology.Resin) && research.Active == null, "designated researcher completes science, no auto repeat");
            c.TickDuty(2); Check(c.ScienceAssignment == null, "completed research releases worker");
            var rs = research.CaptureState(); rs.completed.Add((int)ScienceTechnology.Blades); research.RestoreState(rs);
            ResourceManager.Instance.Add(Resource.Special, 20);
            ResourceManager.Instance.Add(Resource.Soil, 100);
            var workshop = Build<Workshop>(BuildingKind.Workshop, c.Position + Vector3.right * 5);
            Check(workshop.TryEnqueue(EquipmentRecipe.Mandible), "craft order queued");
            c.SetJobEnabled(CommanderJobs.Crafting, true); c.TickDuty(2);
            Check(workshop.Crafter == c, "automatic crafting assignment");
            Check(post.TryDeploy(new[] { c }, new[] { 2 }) && workshop.Crafter == null, "deployment interrupts crafting safely");
            Check(workshop.Jobs.Count == 1, "craft queue preserved on deployment");
            c.ReturnToPost(); c.Agent.Warp(c.WorkState.returnPosition); c.TickDuty(.1f);
            c.SetJobEnabled(CommanderJobs.All, false);
            typeof(WorkerAnt).GetMethod("RestoreCargo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { 2.5f, Resource.Soil });
            c.SetJobEnabled(CommanderJobs.Farming, true); node.GatheringForbidden = true;
            Check(SaveSystem.TrySave(false, 0, out var error), "save with cargo: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            c = CommanderRoster.Instance.Commanders[0];
            Check(c.CarriedAmount == 2.5f && c.CarriedType == Resource.Soil, "cargo conserved across load");
            Check(c.WorkState.jobs == CommanderJobs.Farming && c.PersonalHealth == hp - 1, "work table and health restored");
            Check(Object.FindFirstObjectByType<ConscriptionPost>() != null, "post restored");
            Check(ResourceNode.Available.All(n => n.GatheringForbidden), "forbidden nodes restored");
            var file = SaveSnapshot.Capture(); file.version = 7;
            file.commanders[0].troopCount = 2; file.colony.antsAssigned += 2; file.colony.antsFree -= 2;
            Check(SaveValidator.TryParse(JsonUtility.ToJson(file), out var migrated, out error), "v7 migration: " + error);
            Check(migrated.version == SaveFileV1.CurrentVersion && migrated.commanders[0].personalState.work.duty == CommanderDuty.Deployed, "legacy troops preserved as deployed");
            c.WorkState.health = float.NaN; Check(!c.PersonalState.Validate(out _), "nonfinite health rejected"); c.WorkState.health = 10;
            c.TakeDamage(1000); Check(c.PersonalHealth == 0 && !c.CanReceiveOrders, "downed civilian stops");
            return "PASS AutonomousDutyChecks " + count;
        }
        finally { SaveStorage.RootOverride = oldRoot; Time.timeScale = 0; }
    }
}
