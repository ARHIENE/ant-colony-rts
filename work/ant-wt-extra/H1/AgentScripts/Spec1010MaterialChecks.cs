using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-10 자원 분화: 사육 기술·작업(15종), 원재료 노드(고갈·재성장), 가공대, 주재료 선택·개보수, 우선 납부 재료, 저장.
public static class Spec1010MaterialChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        NavMesh.SamplePosition(position, out var hit, 15, NavMesh.AllAreas);
        var go = Object.Instantiate(template, hit.position + Vector3.up * template.transform.position.y, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static async Task<bool> Until(Func<bool> done, int seconds)
    {
        Time.timeScale = 1;
        for (int i = 0; i < seconds * 10 && !done(); i++) await Task.Delay(100);
        Time.timeScale = 0; return done();
    }

    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1010M-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261011, mapSize = MapSize.Small, biome = MapBiome.Forest }); await Ready(); await Task.Delay(300);
            var rm = ResourceManager.Instance;
            rm.AddCapacity(Resource.Food, 5000); rm.AddCapacity(Resource.Soil, 5000); rm.AddCapacity(Resource.Special, 500);
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var a = list[0];

            // 1. 사육 기술·작업 15종.
            Check(CommanderTalents.Count == 15 && CommanderWorkState.JobCount == 15 && a.AllowsJob(CommanderJobs.All) == false, "15 skills/jobs");
            a.SetJobEnabled(CommanderJobs.Husbandry, true); Check(a.AllowsJob(CommanderJobs.Husbandry) && CommanderAnt.SkillFor(CommanderJobs.Husbandry) == CommanderActivity.Husbandry, "husbandry job uses husbandry skill");
            a.SetJobEnabled(CommanderJobs.Husbandry, false);

            // 2. 재료 종류·한도: 재료는 종류마다 같은 한도(흙 한도)를 쓴다.
            Check(ResourceLabels.Materials.Length >= 20 && Resource.Wood.IsMaterial() && !Resource.Food.IsMaterial(), "material types");
            Check(rm.GetCapacity(Resource.Wood) == rm.GetCapacity(Resource.Soil), "shared material capacity");
            rm.Add(Resource.Wood, 30); Check(rm.GetAmount(Resource.Wood) == 30 && rm.TrySpend(Resource.Wood, 10) && rm.GetAmount(Resource.Wood) == 20, "add/spend material");

            // 3. 원재료 노드: 시드로 생성, 숲은 목재가 많고, 장수가 채집해 창고에 넣는다.
            Check(MaterialDeposits.Spawned.Count > 20 && MaterialDeposits.Spawned.Count(n => n != null) > 15, "material deposits spawned");
            var wood = MaterialDeposits.Spawned.Where(n => n != null && n.ResourceType == Resource.Wood).ToList();
            var stone = MaterialDeposits.Spawned.First(n => n != null && n.ResourceType == Resource.Stone);
            Check(wood.Count == 4 && wood[0].RegrowSeconds > 0 && stone.RegrowSeconds == 0, "plants regrow, stone depletes");
            Check(MaterialDeposits.Multiplier(MapBiome.Forest, Resource.Wood) > 1 && MaterialDeposits.Multiplier(MapBiome.Desert, Resource.Wood) < 1, "regional variation");
            var catalog = (ResourceNode[])typeof(SaveCatalog).GetField("Nodes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Check(catalog.All(n => n.GetComponent<MaterialDeposit>() == null), "deposits outside scene catalog");
            var gatherer = list[1]; gatherer.SetJobEnabled(CommanderJobs.Gathering, true);
            var before = rm.GetAmount(Resource.Wood);
            foreach (var n in ResourceNode.Available.Where(n => n.ResourceType != Resource.Wood)) n.GatheringForbidden = true;
            gatherer.TickDuty(2);
            Check(gatherer.CurrentResourceNode != null && gatherer.CurrentResourceNode.ResourceType == Resource.Wood, "commander gathers wood");
            Check(await Until(() => rm.GetAmount(Resource.Wood) > before, 60), "wood delivered to storage");
            gatherer.SetJobEnabled(CommanderJobs.Gathering, false); gatherer.CommandStop();
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = false;

            // 4. 가공대: 생산 목록에 따라 제작 장수가 원재료를 가공품으로.
            var benchAt = a.Position + new Vector3(9, 0, -7);
            foreach (var x in list.Where(x => (x.Position - benchAt).sqrMagnitude < 16)) x.Agent.Warp(a.Position); // 가공대 자리에 서 있는 장수는 비킨다
            var bench = Build<Processor>(BuildingKind.Carpentry, benchAt); await Task.Delay(50);
            Check(bench != null && bench.Recipes.Count == 1 && RoomSystem.KindOf(bench) == RoomKind.ProcessingRoom && Demolition.CanMove(bench), "carpentry is room furniture");
            rm.Add(Resource.Wood, 10); var woodNow = rm.GetAmount(Resource.Wood);
            Check(bench.AddOrder(0, Workshop.OrderMode.Count, 2) && bench.NeedsWork, "processor order");
            var crafter = list[2]; crafter.SetJobEnabled(CommanderJobs.Crafting, true); crafter.TickDuty(2);
            Check(crafter.ServiceTarget == bench, "crafter goes to processor");
            Check(await Until(() => rm.GetAmount(Resource.Plank) >= 2, 60), "two planks made");
            Check(rm.GetAmount(Resource.Wood) == woodNow - 4 && bench.Orders.Count == 0 && !bench.NeedsWork, "inputs consumed, count order done");
            bench.AddOrder(0, Workshop.OrderMode.KeepStock, 2); Check(!bench.NeedsWork, "keep-stock satisfied");
            bench.Orders.Clear(); crafter.SetJobEnabled(CommanderJobs.Crafting, false); crafter.CommandStop();
            var smelter = Processor.RecipesFor(BuildingKind.Smelter);
            Check(smelter.Any(r => r.output == Resource.Iron) && smelter.Any(r => r.output == Resource.Steel && r.inputs.Any(i => i.type == Resource.Iron)), "three-step metal chain");

            // 5. 주재료 선택: 재료 몫을 고른 재료로 내고 내구도가 재료를 따른다. 취소는 그 재료로 100% 반환.
            var placement = Object.FindFirstObjectByType<BuildingPlacementController>();
            Check(placement.BeginPlacement(BuildingKind.Kitchen, UnitRole.Worker, null), "kitchen placement");
            placement.PendingMaterial = Resource.Wood; rm.Add(Resource.Wood, 100); var w0 = rm.GetAmount(Resource.Wood); var s0 = rm.GetAmount(Resource.Soil);
            var spot = a.Position + Vector3.forward * 9;
            NavMesh.SamplePosition(spot, out var spotHit, 10, NavMesh.AllAreas);
            var placed = placement.PlaceLine(new List<Vector3> { spotHit.position }, new List<bool> { true });
            var kitchenCost = ((GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { BuildingKind.Kitchen, UnitRole.Worker })).GetComponent<BuildingBase>().Data.soilCost;
            Check(placed == 1 && rm.GetAmount(Resource.Wood) == w0 - kitchenCost && rm.GetAmount(Resource.Soil) == s0, "wood paid instead of soil");
            var kSite = Object.FindObjectsByType<BuildingConstructionSite>().Single();
            kSite.Cancel(); await Task.Delay(50); Check(rm.GetAmount(Resource.Wood) == w0, "cancel refunds the chosen material");
            placement.CancelPlacement();
            Check(placement.BeginPlacement(BuildingKind.Kitchen, UnitRole.Worker, null), "kitchen placement 2");
            placement.PendingMaterial = Resource.Wood; placement.PlaceLine(new List<Vector3> { spotHit.position }, new List<bool> { true });
            Object.FindObjectsByType<BuildingConstructionSite>().Single().Complete(); await Task.Delay(50);
            placement.CancelPlacement();
            var kitchen = Object.FindObjectsByType<Kitchen>().First(k => k.MainMaterial == Resource.Wood);
            Check(Mathf.Approximately(kitchen.MaxHealth, kitchen.Data.maxHealth * MaterialInfo.For(Resource.Wood).durability), "durability follows material");
            Check(!Demolition.MaterialSelectable(BuildingKind.Hut) && !Demolition.MaterialSelectable(BuildingKind.LeafWall), "housing/walls keep own material");

            // 6. 개보수: 새 재료 선차감·사용 중지, 취소 100% 반환·복구, 완료 시 기존 재료 70% 반환.
            rm.Add(Resource.Stone, 200); var st0 = rm.GetAmount(Resource.Stone);
            var rSite = Demolition.OrderRenovate(kitchen, Resource.Stone);
            Check(rSite != null && rm.GetAmount(Resource.Stone) == st0 - kitchen.Data.soilCost && !kitchen.enabled && Demolition.Pending(kitchen), "renovation charged, building paused");
            rSite.Cancel(); await Task.Delay(50);
            Check(kitchen.enabled && kitchen.MainMaterial == Resource.Wood && rm.GetAmount(Resource.Stone) == st0 && !Demolition.Pending(kitchen), "cancel restores");
            var wBefore = rm.GetAmount(Resource.Wood);
            rSite = Demolition.OrderRenovate(kitchen, Resource.Stone);
            var builder = list[3]; builder.SetJobEnabled(CommanderJobs.Building, true); builder.TickDuty(2);
            Check(builder.ConstructionTarget == rSite, "builder takes renovation");
            Check(await Until(() => rSite == null, 60), "renovation finished on site");
            Check(kitchen.enabled && kitchen.MainMaterial == Resource.Stone && rm.GetAmount(Resource.Wood) == wBefore + Mathf.FloorToInt(kitchen.Data.soilCost * GameBalance.DemolishRefundShare), "material swapped, 70% old refunded");
            builder.SetJobEnabled(CommanderJobs.Building, false); builder.CommandStop();

            // 7. 우선 납부 재료: 지역 기초 원재료, 우선 재료가 60%.
            var pop = ColonyPopulation.Instance;
            Check(ColonyPopulation.TaxMaterials.Contains(Resource.Wood), "forest taxes wood");
            pop.SetTaxMaterial(Resource.Wood); pop.SetTaxMaterial(Resource.IronOre); Check(pop.S.taxMaterial == Resource.Wood, "only regional basics");
            var wt = rm.GetAmount(Resource.Wood); var lt = rm.GetAmount(Resource.Leaf); pop.S.taxSoil = 10.5f; pop.S.taxFood = 0; pop.PayTax();
            Check(rm.GetAmount(Resource.Wood) == wt + 6 && rm.GetAmount(Resource.Leaf) == lt + 2 && pop.S.taxSoil < 1, "60% priority material, rest split");

            // 8. 저장: 재료 보유량·노드 잔량·주재료·가공 목록이 불러오기 후에도 같다.
            stone.Extract(37); var stoneLeft = stone.AmountRemaining; var stoneIndex = MaterialDeposits.Spawned.IndexOf(stone);
            bench.AddOrder(0, Workshop.OrderMode.Forever, 1);
            var amounts = ResourceLabels.Materials.ToDictionary(m => m, m => rm.GetAmount(m));
            var kitchenPos = kitchen.Position;
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            rm = ResourceManager.Instance;
            Check(ResourceLabels.Materials.All(m => rm.GetAmount(m) == amounts[m]), "material amounts survive load");
            Check(Mathf.Approximately(MaterialDeposits.Spawned[stoneIndex].AmountRemaining, stoneLeft), "deposit amount survives load");
            var loadedKitchen = Object.FindObjectsByType<Kitchen>().OrderBy(k => (k.Position - kitchenPos).sqrMagnitude).First();
            Check(loadedKitchen.MainMaterial == Resource.Stone, "main material survives load");
            var loadedBench = Object.FindObjectsByType<Processor>().Single();
            Check(loadedBench.Orders.Count == 1 && loadedBench.Orders[0].mode == Workshop.OrderMode.Forever, "processor orders survive load");
            Check(ColonyPopulation.Instance.S.taxMaterial == Resource.Wood, "tax material survives load");
            return "PASS " + checks + " Spec1010Material checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
