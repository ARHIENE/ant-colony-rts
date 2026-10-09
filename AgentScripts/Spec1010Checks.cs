using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-10: 철거(70% 반환)·가구 이동, 제작 생산 목록(지정 수량·재고 유지·계속 생산), 재개발 Shift 배치, 연구 주제·제작 분류별 보조 능력.
public static class Spec1010Checks
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
    static void Grant(params ScienceTechnology[] techs)
    {
        var state = CampaignResearch.Instance.CaptureState();
        foreach (var t in techs) if (!state.completed.Contains((int)t)) state.completed.Add((int)t);
        CampaignResearch.Instance.RestoreState(state);
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
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1010-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261010, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var rm = ResourceManager.Instance;
            foreach (Resource type in Enum.GetValues(typeof(Resource))) { rm.AddCapacity(type, 20000); rm.Add(type, 5000); }
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var a = list[0];
            a.SetJobEnabled(CommanderJobs.Building, true);

            // 1. 철거: 장수가 현장에서 끝내야 사라지고 건설비 70% 반환. 벽은 이동 불가.
            var kitchen = Build<BuildingBase>(BuildingKind.Kitchen, a.Position + Vector3.forward * 5);
            Check(Demolition.CanDemolish(kitchen) && Demolition.CanMove(kitchen), "furniture can be demolished and moved");
            var wall = Build<BuildingBase>(BuildingKind.SoilWall, a.Position + Vector3.back * 5);
            Check(Demolition.CanDemolish(wall) && !Demolition.CanMove(wall), "walls are demolished, not moved");
            Check(!Demolition.CanDemolish(Object.FindFirstObjectByType<Stockpile>()), "starting stockpile stays");
            var soil = rm.GetAmount(Resource.Soil); var food = rm.GetAmount(Resource.Food); var d = kitchen.Data;
            var site = Demolition.OrderDemolish(kitchen);
            Check(site != null && Demolition.OrderDemolish(kitchen) == null, "one demolition order per building");
            Check(kitchen != null && rm.GetAmount(Resource.Soil) == soil, "nothing happens until a builder works");
            a.TickDuty(2); Check(a.ConstructionTarget == site, "builder picks demolition as building work");
            Check(await Until(() => kitchen == null, 40), "builder demolishes on site");
            Check(rm.GetAmount(Resource.Soil) == soil + Mathf.FloorToInt(d.soilCost * GameBalance.DemolishRefundShare)
                && rm.GetAmount(Resource.Food) == food + Mathf.FloorToInt(d.foodCost * GameBalance.DemolishRefundShare), "demolition refunds 70%");
            // 철거 예정지 취소는 반환 없이 표식만 지운다.
            var wallSite = Demolition.OrderDemolish(wall); soil = rm.GetAmount(Resource.Soil);
            wallSite.Cancel(); await Task.Delay(50);
            Check(wall != null && !Demolition.Pending(wall) && rm.GetAmount(Resource.Soil) == soil, "cancelled demolition keeps building, no refund");

            // 2. 가구 이동: 비용 없이 장수가 새 위치로 옮긴다.
            var pot = Build<BuildingBase>(BuildingKind.FlowerPot, a.Position + Vector3.right * 4);
            var to = pot.Position + Vector3.right * 3; soil = rm.GetAmount(Resource.Soil);
            var moveSite = Demolition.OrderMove(pot, to);
            Check(moveSite != null, "move order");
            a.CommandStop(); a.TickDuty(2); Check(a.ConstructionTarget == moveSite, "builder picks move job");
            Check(await Until(() => moveSite == null, 40), "move finished");
            Check(pot != null && Mathf.Abs(pot.Position.x - to.x) < .01f && Mathf.Abs(pot.Position.z - to.z) < .01f && rm.GetAmount(Resource.Soil) == soil, "furniture moved for free");
            a.SetJobEnabled(CommanderJobs.Building, false); a.CommandStop();

            // 3. 생산 목록: 대기열이 비면 목록 순서대로 1개씩 넣고 그때 비용을 낸다.
            Grant(ScienceTechnology.Blades, ScienceTechnology.Trinkets);
            var w = Build<Workshop>(BuildingKind.Workshop, a.Position + Vector3.left * 6);
            var inv = EquipmentInventory.Instance; inv.Items.Clear();
            soil = rm.GetAmount(Resource.Soil);
            Check(w.AddOrder(EquipmentRecipe.Mandible, Workshop.OrderMode.Count, 2), "count order added");
            Check(rm.GetAmount(Resource.Soil) == soil, "orders cost nothing until queued");
            w.Refill(); Check(w.Jobs.Count == 1 && w.Orders[0].amount == 1 && rm.GetAmount(Resource.Soil) == soil - 40, "refill queues one unit and pays");
            w.Refill(); Check(w.Jobs.Count == 1, "refill waits for empty queue");
            w.Cancel(0); w.Refill(); Check(w.Orders.Count == 0 && w.Jobs.Count == 1, "count order removed when last unit queued");
            w.Cancel(0);
            Check(w.AddOrder(EquipmentRecipe.Anklet, Workshop.OrderMode.KeepStock, 1), "keep-stock order");
            w.Refill(); Check(w.Jobs.Count == 1 && w.Jobs[0].recipe == EquipmentRecipe.Anklet, "keep-stock produces below target");
            w.Cancel(0); inv.Add(EquipmentRecipes.Create(EquipmentRecipe.Anklet, 1));
            Check(Workshop.Stock(EquipmentRecipe.Anklet) == 1, "stock counts matching items");
            w.Refill(); Check(w.Jobs.Count == 0, "keep-stock stops at target");
            Check(w.AddOrder(EquipmentRecipe.Antenna, Workshop.OrderMode.Forever, 1), "forever order");
            w.Refill(); Check(w.Jobs.Count == 1 && w.Jobs[0].recipe == EquipmentRecipe.Antenna, "keep-stock skipped, forever produces");
            w.Cancel(0); w.Refill(); Check(w.Orders.Count == 2 && w.Jobs.Count == 1, "forever order stays");
            // 장수 자율 제작: 제작이 켜진 장수가 와서 만든다.
            var crafter = list[1]; crafter.SetJobEnabled(CommanderJobs.Crafting, true); crafter.Agent.Warp(w.Position + Vector3.right * 3); crafter.TickDuty(2);
            Check(await Until(() => w.Crafter == crafter, 20), "crafter assigned automatically");
            var strength = crafter.Talents.Value((int)CommanderActivity.Art);
            w.Tick(5); Check(crafter.Talents.Value((int)CommanderActivity.Art) > strength, "trinket crafting trains art as support");
            crafter.SetJobEnabled(CommanderJobs.Crafting, false); w.Orders.Clear(); w.Release(); w.Cancel(0); crafter.CommandStop();
            // 저장: 생산 목록 보존·검증.
            w.AddOrder(EquipmentRecipe.Mandible, Workshop.OrderMode.KeepStock, 3);
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "snapshot valid " + error);
            var dto = file.buildings.First(b => b.kind == "Workshop" && b.workshop.orders.Count == 1);
            Check(dto.workshop.orders[0].mode == Workshop.OrderMode.KeepStock && dto.workshop.orders[0].amount == 3, "orders saved");
            dto.workshop.orders[0].amount = 999; Check(!SaveValidator.Validate(file, out _), "invalid order rejected");
            w.Orders.Clear();

            // 4. 재개발 Shift 배치: 줄 배치 칸에 작은 집이 있으면 교체 공사가 된다.
            var hut = Build<Housing>(BuildingKind.Hut, a.Position + Vector3.forward * 12 + Vector3.right * 8);
            var placement = Object.FindFirstObjectByType<BuildingPlacementController>();
            Check(placement.BeginPlacement(BuildingKind.House, UnitRole.Worker, null), "house placement");
            var placed = placement.PlaceLine(new List<Vector3> { hut.Position }, new List<bool> { true });
            Check(placed == 1 && Object.FindObjectsByType<RedevelopmentSite>().Any(s => s.Target == hut), "shift line over a hut starts redevelopment");
            foreach (var s in Object.FindObjectsByType<BuildingConstructionSite>()) s.Cancel();
            await Task.Delay(50);

            // 5. 연구 주제별 보조 능력.
            Check(CampaignResearch.Topic(ScienceTechnology.FungalFarming) == CommanderActivity.Farming && CampaignResearch.Topic(ScienceTechnology.Herbs) == CommanderActivity.Medicine
                && CampaignResearch.Topic(ScienceTechnology.Vehicle) == CommanderActivity.Crafting && CampaignResearch.Topic(ScienceTechnology.Fishing) == null, "research topics");
            Check(EquipmentRecipes.Topic(EquipmentRecipe.Shield) == CommanderActivity.Strength && EquipmentRecipes.Topic(EquipmentRecipe.Wings) == CommanderActivity.Research, "craft topics");
            var r = list[2]; var farm = r.Talents.Value((int)CommanderActivity.Farming); var str = r.Talents.Value((int)CommanderActivity.Strength);
            r.GainExperience(CommanderActivity.Research, 50, CampaignResearch.Topic(ScienceTechnology.FungalFarming));
            Check(r.Talents.Value((int)CommanderActivity.Farming) > farm || r.Talents.Value((int)CommanderActivity.Farming) >= 20, "farming research trains farming");
            Check(r.Talents.Value((int)CommanderActivity.Strength) <= str, "topic support replaces default (none for research)");
            return "PASS " + checks + " Spec1010 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
