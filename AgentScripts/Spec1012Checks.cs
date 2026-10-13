using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-12 확정 기획 검사: ① 작업표 0~9·실제 이동 거리·오래 기다린 요청 ② 창고 실물 재고(산소미포함식)·허용 품목·우선순위·꺼내기·운반 규칙 ③ 건설 ④ 철거·이동·개보수 ⑤ 식품 신선도·냉장고·냉동고·썩은 식품 ⑥ 요리 ⑦ 작업 관리
public static class Spec1012Checks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static async Task Until(Func<bool> done, float seconds, string message)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!done() && DateTime.UtcNow < end) await Task.Delay(50);
        Check(done(), message);
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static object Call(object target, string name, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        for (var t = type; t != null; t = t.BaseType)
        {
            var m = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .FirstOrDefault(x => x.Name == name && x.GetParameters().Length == args.Length);
            if (m != null) return m.Invoke(target is Type ? null : target, args);
        }
        throw new Exception("no method " + name);
    }
    static void Drop(Resource t, int n, Vector3 at) => Call(typeof(DiplomacyManager), "DropFloor", t, n, at);
    static Vector3 Ground(Vector3 p) => UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 6, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : p;
    static float Loose(Resource t) => ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == t).Sum(n => n.AmountRemaining);
    static void ClearLoose() { foreach (var n in ResourceNode.Available.Where(n => n != null && n.IsLooseCargo).ToList()) Object.DestroyImmediate(n.gameObject); }

    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1012-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261012, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            await Priorities(list);
            await Storage(list);
            list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            await Construction(list);
            list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            await Removal(list);
            list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            await Food(list);
            list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            await Cooking(list);
            list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            await Tasks(list);
            return $"PASS Spec1012Checks {checks}";
        }
        finally { Time.timeScale = 1; SaveStorage.RootOverride = previousRoot; }
    }

    // ① 작업표 0~9(기본 5), 이전 저장 1~5 → 2p−1, 실제 이동 거리, 거리까지 같으면 오래 기다린 요청.
    static async Task Priorities(System.Collections.Generic.List<CommanderAnt> list)
    {
        var a = list[0];
        Check(CommanderWorkState.MaxPriority == 9 && CommanderWorkState.DefaultPriority == 5, "work table range 0~9, default 5");
        Check(a.SetJobPriority(CommanderJobs.Building, 9) && a.JobPriority(CommanderJobs.Building) == 9, "priority 9 accepted");
        Check(!a.SetJobPriority(CommanderJobs.Building, 10), "priority 10 rejected");
        Check(a.SetJobEnabled(CommanderJobs.Hauling, true) && a.JobPriority(CommanderJobs.Hauling) == 5, "newly enabled job defaults to 5");
        a.SetJobEnabled(CommanderJobs.All, false);

        // 이전 저장(v16) 이관: 1~5 → 1,3,5,7,9.
        var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var err0), "snapshot valid " + err0);
        file.version = 16; var ps = file.commanders[0].personalState.work; ps.priorities = new[] { 0, 1, 2, 3, 4, 5, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
        Check(SaveValidator.Validate(file, out var err1), "v16 save migrates " + err1);
        Check(file.version == SaveFileV1.CurrentVersion && ps.priorities.Take(6).SequenceEqual(new[] { 0, 1, 3, 5, 7, 9 }), "v16 priorities scaled to 1~9: " + string.Join(",", ps.priorities));

        // 실제 이동 거리: 갈 수 있는 곳은 유한, 직선거리 이상.
        var here = Ground(a.Position + Vector3.right * 6);
        var travel = a.TravelDistance(here);
        Check(!float.IsInfinity(travel) && travel >= Vector3.Distance(a.Position, here) - .5f, $"travel distance measured ({travel:0.0})");

        // 거리까지 같으면 먼저 요청된 대상: 좌우 대칭 두 예정지 중 먼저 보인 쪽.
        var left = Site(Ground(a.Position + Vector3.left * 4)); await Task.Delay(100);
        var right = Site(Ground(a.Position + Vector3.right * 4));
        WorkPriorities.RequestedAt(left); await Task.Delay(50); WorkPriorities.RequestedAt(right);
        Check(WorkPriorities.RequestedAt(left) < WorkPriorities.RequestedAt(right), "request age recorded");
        var dl = Mathf.Round(a.TravelDistance(left.Position) * 2) / 2; var dr = Mathf.Round(a.TravelDistance(right.Position) * 2) / 2;
        if (Mathf.Approximately(dl, dr))
        {
            a.SetJobPriority(CommanderJobs.Building, 5); a.TickDuty(2);
            Check(a.ConstructionTarget == left, "equal distance: older request first");
        }
        a.CommandStop(); a.SetJobEnabled(CommanderJobs.All, false);
        // 대상 우선순위가 거리보다 앞선다.
        var far = Site(Ground(a.Position + Vector3.forward * 12)); WorkPriorities.Set(far, 9, false);
        a.SetJobPriority(CommanderJobs.Building, 5); a.TickDuty(2);
        Check(a.ConstructionTarget == far, "target priority beats distance");
        a.CommandStop(); a.SetJobEnabled(CommanderJobs.All, false);
        foreach (var s in new[] { left, right, far }) Object.DestroyImmediate(s.gameObject);
    }
    static BuildingConstructionSite Site(Vector3 at)
    {
        var go = new GameObject("Spec1012 site"); go.transform.position = at;
        var site = go.AddComponent<BuildingConstructionSite>(); site.Initialize(null, 1000); return site;
    }

    // ② 창고 실물 재고.
    static async Task Storage(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        Check(rm.BaseBin != null && rm.Bins.Contains(rm.BaseBin), "stockpile bin exists");
        foreach (Resource t in Enum.GetValues(typeof(Resource)))
            Check(rm.GetAmount(t) == rm.Bins.Where(b => b.Usable).Sum(b => b.Amount(t)), "total = sum of bins " + t);
        var store = Object.FindObjectsByType<Storage>().FirstOrDefault(s => s.isActiveAndEnabled)
            ?? Build<Storage>(BuildingKind.Storage, Ground(WorldMapManager.Instance.HomePosition + Vector3.right * 10));
        await Task.Delay(100);
        var bin = store.Bin; Check(bin != null && rm.Bins.Contains(bin) && bin.Capacity(Resource.Soil) > 0, "storage registers its own bin");
        Check(!bin.Allows(Resource.RottenFood) && bin.Allows(Resource.Food) && bin.Allows(Resource.Stone), "new storage allows all but rotten food");

        // 우선순위 높은 창고가 입고 목적지. 같은 품목을 비축더미가 이미 갖고 있어도 자동으로 옮기지 않는다.
        rm.SetPriority(bin, 9); rm.SetPriority(rm.BaseBin, 5);
        var baseSoil = rm.BaseBin.Amount(Resource.Soil); var storeSoil = bin.Amount(Resource.Soil);
        Check(rm.Destinations(Resource.Soil, WorldMapManager.Instance.HomePosition, null).First() == bin, "highest priority bin is destination");
        rm.Add(Resource.Soil, 7);
        Check(bin.Amount(Resource.Soil) == storeSoil + 7 && rm.BaseBin.Amount(Resource.Soil) == baseSoil, "income goes to priority bin; no rebalancing of stored goods");

        // 허용 해제 → 그 품목 전부 바닥. 다시 허용해도 자동으로 되넣지 않는다(바닥 물자는 운반 대상).
        var total = rm.GetAmount(Resource.Soil); var inStore = bin.Amount(Resource.Soil); var loose0 = Loose(Resource.Soil);
        rm.SetAllowed(bin, Resource.Soil, false);
        Check(bin.Amount(Resource.Soil) == 0 && rm.GetAmount(Resource.Soil) == total - inStore && Mathf.Approximately(Loose(Resource.Soil) - loose0, inStore), "disallow ejects stock to floor");
        Check(!rm.Destinations(Resource.Soil, store.Position, null).Contains(bin), "disallowed bin is not a destination");
        rm.SetAllowed(bin, Resource.Soil, true);

        // 운반: 바닥 더미를 운반 장수가 우선순위 높은 창고로 옮긴다.
        var hauler = list[1]; hauler.SetJobPriority(CommanderJobs.Hauling, 9);
        var loose = Loose(Resource.Soil); var before = bin.Amount(Resource.Soil);
        Time.timeScale = 3;
        await Until(() => Loose(Resource.Soil) < .5f && !hauler.IsCarrying, 90, $"hauler stores floor soil (left {Loose(Resource.Soil):0.0})");
        Time.timeScale = 0;
        Check(bin.Amount(Resource.Soil) >= before + Mathf.FloorToInt(loose) - 1, $"floor soil arrived in priority-9 storage ({bin.Amount(Resource.Soil)} vs {before}+{loose})");
        hauler.SetJobEnabled(CommanderJobs.All, false); hauler.CommandStop();

        // 같은 물자 묶음 운반: 가까운 두 더미를 한 번에 집는다.
        Drop(Resource.Stone, 3, Ground(hauler.Position + Vector3.right * 3));
        Drop(Resource.Stone, 3, Ground(hauler.Position + Vector3.right * 6));
        hauler.SetJobPriority(CommanderJobs.Hauling, 9); Time.timeScale = 2;
        await Until(() => hauler.IsCarrying && hauler.CarriedAmount >= 5.5f || Loose(Resource.Stone) < .5f, 60, "hauler bundles same-type piles");
        Check(hauler.CarriedAmount >= 5.5f || !hauler.IsCarrying, $"carried both piles ({hauler.CarriedAmount:0.0})");
        await Until(() => !hauler.IsCarrying, 60, "bundled load delivered"); Time.timeScale = 0;
        hauler.SetJobEnabled(CommanderJobs.All, false); hauler.CommandStop();

        // 받을 창고가 없으면 현재 위치에 내려놓는다.
        foreach (var b in rm.Bins) rm.SetAllowed(b, Resource.Clay, false);
        Call(hauler, "RestoreCargo", 4f, Resource.Clay); var clay0 = Loose(Resource.Clay);
        Call(hauler, "ReturnCargoToStorage");
        Check(!hauler.IsCarrying && Mathf.Approximately(Loose(Resource.Clay) - clay0, 4), "no destination: cargo dropped in place");
        foreach (var b in rm.Bins) rm.SetAllowed(b, Resource.Clay, true);

        // 꺼내기·전체 비우기.
        rm.Add(Resource.Wood, 5); var wood = bin.Amount(Resource.Wood);
        Check(rm.Eject(bin, Resource.Wood) == wood && bin.Amount(Resource.Wood) == 0, "per-item withdraw to floor");
        // 목적지 예약: 남은 공간을 다른 장수가 예약하면 그만큼 빠진다.
        var free = bin.Free(Resource.Leaf); rm.Reserve("other", bin, Resource.Leaf, free);
        Check(bin.Free(Resource.Leaf) == 0 && bin.Free(Resource.Leaf, "other") == free, "destination space reservation");
        rm.Reserve("other", null, default, 0);

        // 저장 왕복: 칸별 재고·허용·우선순위 유지.
        rm.SetAllowed(bin, Resource.Sand, false); rm.SetPriority(bin, 8);
        var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "valid " + error);
        var dto = file.buildings.First(b => b.storeBin != null && b.storeBin.priority == 8);
        Check((dto.storeBin.allowed & (1L << (int)Resource.Sand)) == 0 && file.colony.baseBin != null, "bins captured");
        Check(SaveSystem.TrySave(false, 1, out error), "save " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out error), "load " + error); await Ready(); await Task.Delay(300);
        rm = ResourceManager.Instance;
        var back = Object.FindObjectsByType<Storage>().Select(s => s.Bin).FirstOrDefault(b => b != null && b.priority == 8);
        Check(back != null && !back.Allows(Resource.Sand) && back.Amount(Resource.Soil) == dto.storeBin.amounts[(int)Resource.Soil], "storage bin restored");
        Check(rm.BaseBin.Amount(Resource.Food) == file.colony.baseBin.amounts[(int)Resource.Food], "base bin restored");
        foreach (var x in CommanderRoster.Instance.Commanders) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
    }

    // ③ 건설: 부족해도 배치 → 예약(우선순위·먼저 지시)·부분 예약 → 배송 → 전부 도착 후 시작할 때 소비. 일시정지·취소·재료 변경·저장.
    static BuildingConstructionSite Needy(Vector3 at, int soil, int food = 0)
    {
        var go = new GameObject("Spec1012 needy"); go.transform.position = at; var s = go.AddComponent<BuildingConstructionSite>(); s.Initialize(null, 3);
        s.Require(Resource.Soil, soil); s.Require(Resource.Food, food); return s;
    }
    static void Spend(Resource t, int keep) { var rm = ResourceManager.Instance; rm.TrySpend(t, Mathf.Max(0, rm.GetAmount(t) - keep)); }
    static async Task Construction(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        foreach (var s in Object.FindObjectsByType<BuildingConstructionSite>()) Object.DestroyImmediate(s.gameObject);
        var home = Ground(WorldMapManager.Instance.HomePosition + Vector3.forward * 6);
        rm.AddCapacity(Resource.Soil, 1000); rm.Add(Resource.Soil, 500); Spend(Resource.Soil, 25);

        // 우선순위 높은 곳 → 같으면 먼저 지시한 곳. 부분 예약.
        var first = Needy(home, 20); var second = Needy(home + Vector3.right * 3, 20); var urgent = Needy(home + Vector3.left * 3, 10);
        WorkPriorities.Set(urgent, 8, false);
        BuildingConstructionSite.Allocate(); await Task.Delay(50); BuildingConstructionSite.Allocate();
        Check(urgent.Needs[0].reserved == 10 && first.Needs[0].reserved == 15 && second.Needs[0].reserved == 0, $"reserve by priority then order, partial ({urgent.Needs[0].reserved}/{first.Needs[0].reserved}/{second.Needs[0].reserved})");
        Check(second.WaitReason == BuildingConstructionSite.Wait.MaterialShortage, "no stock = material shortage");
        // 순위를 바꿔도 이미 예약한 몫은 빼앗지 않는다. 새로 생긴 재고부터 새 순서.
        WorkPriorities.Set(second, 9, false); await Task.Delay(50); BuildingConstructionSite.Allocate();
        Check(first.Needs[0].reserved == 15 && second.Needs[0].reserved == 0, "priority change keeps existing reservations");
        rm.Add(Resource.Soil, 8); await Task.Delay(50); BuildingConstructionSite.Allocate();
        Check(second.Needs[0].reserved == 8 && first.Needs[0].reserved == 15, "new stock goes to the now-higher site");
        // 일시정지: 예약 해제, 재개하면 다시 예약.
        second.SetPaused(true); BuildingConstructionSite.Allocate();
        Check(second.Paused && second.Needs[0].reserved == 0 && second.WaitReason == BuildingConstructionSite.Wait.Paused, "pause releases reservations");
        foreach (var s in new[] { second, urgent }) Object.DestroyImmediate(s.gameObject);
        await Task.Delay(50);

        // 배송·시공: 운반 장수가 가져오고, 전부 도착하기 전엔 시공하지 않으며, 시작할 때 소비한다.
        var hauler = list[0]; var builder = list[1];
        hauler.SetJobPriority(CommanderJobs.Hauling, 9); builder.SetJobPriority(CommanderJobs.Building, 9);
        var stock = rm.GetAmount(Resource.Soil);
        Check(!first.Ready && !first.Started, "not ready before delivery");
        Time.timeScale = 3;
        await Until(() => first.Ready, 120, $"materials delivered ({first.Delivered(Resource.Soil)}/20, stock {rm.GetAmount(Resource.Soil)})");
        Check(rm.GetAmount(Resource.Soil) <= stock - 20 + 8, "delivered soil left storage");
        await Until(() => first == null, 120, "site built after delivery");
        Time.timeScale = 0;
        foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }

        // 시작 전 취소 = 예약 해제·도착분 바닥 / 시작 후 취소 = 재료 100% 바닥.
        var pre = Needy(home, 12); pre.Deliver("t", Resource.Soil, 7); var floor0 = Loose(Resource.Soil);
        pre.Cancel(); await Task.Delay(50);
        Check(Mathf.Approximately(Loose(Resource.Soil) - floor0, 7), "cancel before start drops delivered to floor");
        var post = Needy(home, 12); post.Deliver("t", Resource.Soil, 12); post.BeginWork(); floor0 = Loose(Resource.Soil);
        Check(post.Started, "work start consumes materials");
        post.Cancel(); await Task.Delay(50);
        Check(Mathf.Approximately(Loose(Resource.Soil) - floor0, 12), "cancel after start returns 100% to floor");
        ClearLoose();

        // 시작 전 재료 변경: 도착분 바닥, 새 재료로 다시 요청.
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { BuildingKind.Storage, UnitRole.Worker });
        var building = Object.Instantiate(template, home, Quaternion.identity); building.SetActive(false);
        var mgo = new GameObject("Spec1012 material"); mgo.transform.position = home;
        var msite = mgo.AddComponent<BuildingConstructionSite>(); msite.Initialize(building, 5); msite.Require(Resource.Soil, 10); msite.Deliver("t", Resource.Soil, 4);
        floor0 = Loose(Resource.Soil);
        Check(msite.ChangeMaterial(Resource.Stone) && msite.Needs.Any(n => n.type == Resource.Stone && n.need == 10 && n.delivered == 0) && !msite.Needs.Any(n => n.type == Resource.Soil)
            && building.GetComponent<BuildingBase>().MainMaterial == Resource.Stone, "material change before start");
        Check(Mathf.Approximately(Loose(Resource.Soil) - floor0, 4), "old delivered material dropped");
        ClearLoose();

        // 저장: 재료를 기다리는 예정지가 있어도 저장되고, 필요·도착·우선순위·일시정지가 돌아온다.
        WorkPriorities.Set(msite, 7, false); msite.Deliver("t", Resource.Stone, 3); msite.SetPaused(true);
        Check(SaveSystem.TrySave(false, 2, out var error), "save with waiting blueprint " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 2), out error), "load " + error); await Ready(); await Task.Delay(300);
        var back = Object.FindObjectsByType<BuildingConstructionSite>().FirstOrDefault(x => x.Building != null);
        Check(back != null && back.Paused && WorkPriorities.Level(back) == 7 && back.Needs.Any(n => n.type == Resource.Stone && n.need == 10 && n.delivered == 3)
            && back.Building.GetComponent<BuildingBase>().MainMaterial == Resource.Stone && !back.Building.activeSelf, "blueprint restored");
        back.Cancel();
        foreach (var x in CommanderRoster.Instance.Commanders) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
    }

    // ④ 철거·이동·개보수: 시작 때 사용 중지·내용물 바닥·생산 취소, 완료 70% 바닥, 이동 회수→포장→설치, 개보수 예약·시작 소비·취소 100%.
    static async Task Removal(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        foreach (var s in Object.FindObjectsByType<BuildingConstructionSite>()) Object.DestroyImmediate(s.gameObject);
        var spot = Ground(WorldMapManager.Instance.HomePosition + Vector3.back * 14);

        // 창고 철거: 지시만으로는 그대로, 실제 시작 때 사용 중지·내용물 바닥, 취소하면 기능 복구(내용물은 되넣지 않음).
        var store = Build<Storage>(BuildingKind.Storage, spot); await Task.Delay(100);
        rm.SetPriority(store.Bin, 9); rm.Add(Resource.Stone, 6); var stone = store.Bin.Amount(Resource.Stone);
        Check(stone >= 6, "storage holds stone");
        var demo = Demolition.OrderDemolish(store);
        Check(demo != null && store.enabled && store.Bin.Amount(Resource.Stone) == stone, "order alone keeps storage working");
        var floor0 = Loose(Resource.Stone);
        demo.BeginWork();
        Check(!store.enabled && store.Bin.Amount(Resource.Stone) == 0 && Mathf.Approximately(Loose(Resource.Stone) - floor0, stone), "demolition start stops use and drops contents");
        demo.Cancel(); await Task.Delay(50);
        Check(store != null && store.enabled && store.Bin.Amount(Resource.Stone) == 0, "cancel restores function, contents stay on floor");
        // 완료: 건설 재료 70% 바닥.
        ClearLoose(); var cost = store.Data.soilCost; var mat = store.MainMaterial;
        demo = Demolition.OrderDemolish(store); demo.BeginWork(); ClearLoose(); demo.Complete(); await Task.Delay(50);
        Check(store == null && Mathf.Approximately(Loose(mat), Mathf.FloorToInt(cost * GameBalance.DemolishRefundShare)), $"demolition returns 70% to floor ({Loose(mat)} of {cost})");
        ClearLoose();

        // 생산 시설 철거 시작: 진행 중 생산 취소(재료 반환 없음), 주문은 남는다.
        var kind = ((BuildingKind[])Enum.GetValues(typeof(BuildingKind))).FirstOrDefault(Processor.IsKind);
        var bench = Build<Processor>(kind, spot + Vector3.right * 4); await Task.Delay(50);
        foreach (var i in bench.Recipes[0].inputs) { rm.AddCapacity(i.type, 100); rm.Add(i.type, i.amount * 2); }
        bench.AddOrder(0, Workshop.OrderMode.Count, 2); bench.Work(list[0], .01f);
        Check(bench.Current != null, "production started");
        var inputs = bench.Recipes[0].inputs.Select(i => rm.GetAmount(i.type)).ToArray();
        var benchDemo = Demolition.OrderDemolish(bench); benchDemo.BeginWork();
        Check(bench.Current == null && bench.Recipes[0].inputs.Select(i => rm.GetAmount(i.type)).SequenceEqual(inputs) && Loose(bench.Recipes[0].inputs[0].type) < .5f, "demolition cancels production without refund");
        benchDemo.Cancel(); Object.DestroyImmediate(bench.gameObject);

        // 이동: 회수(원래 자리) → 포장 → 새 위치 설치. 회수 후 취소하면 포장된 채 원래 자리에 남는다.
        var decoKind = ((BuildingKind[])Enum.GetValues(typeof(BuildingKind))).First(k => Decoration.IsKind(k) && k != BuildingKind.Pillar);
        var deco = Build<BuildingBase>(decoKind, spot + Vector3.left * 4); await Task.Delay(50);
        var origin = deco.Position; var to = Ground(spot + Vector3.left * 9);
        Check(Demolition.CanMove(deco), "decoration movable");
        var move = Demolition.OrderMove(deco, to);
        Check(move != null && (move.Position - origin).sqrMagnitude < 9, "move pickup happens at origin");
        move.BeginWork(); move.Complete(); await Task.Delay(50);
        Check(move != null && Demolition.IsPacked(deco) && !deco.enabled && (deco.Position - origin).sqrMagnitude < .01f && (move.Position - to).sqrMagnitude < 9, "packed, install site at destination");
        move.Cancel(); await Task.Delay(50);
        Check(Demolition.IsPacked(deco) && !deco.enabled && (deco.Position - origin).sqrMagnitude < .01f, "cancel after pickup leaves packed item on floor");
        var again = Demolition.OrderMove(deco, to);
        Check(again != null && (again.Position - to).sqrMagnitude < 9, "packed item reinstalls directly at destination");
        // 저장: 포장된 가구·설치 단계 이동 예정지가 돌아온다.
        Check(SaveSystem.TrySave(false, 0, out var error), "save with packed move " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error); await Ready(); await Task.Delay(300);
        rm = ResourceManager.Instance;
        again = Object.FindObjectsByType<BuildingConstructionSite>().FirstOrDefault(x => x.TryGetComponent<DemolitionSite>(out var m) && m.Move);
        deco = again != null ? again.GetComponent<DemolitionSite>().Target : null;
        Check(again != null && deco != null && again.GetComponent<DemolitionSite>().Packed && Demolition.IsPacked(deco) && !deco.enabled, "packed move restored");
        again.BeginWork(); again.Complete(); await Task.Delay(50);
        Check(deco != null && !Demolition.IsPacked(deco) && deco.enabled && (deco.Position - to).sqrMagnitude < 1, "installed at new position, unpacked");

        // 개보수: 새 재료 예약·배송 → 시작 때 사용 중지 → 완료 시 기존 재료 70% 바닥. 취소는 새 재료 100%·기능 복구.
        ClearLoose();
        var store2 = Build<Storage>(BuildingKind.Storage, spot + Vector3.forward * 5); await Task.Delay(100);
        var oldMat = store2.MainMaterial; var newMat = oldMat == Resource.Stone ? Resource.Wood : Resource.Stone; var need = store2.Data.soilCost;
        var ren = Demolition.OrderRenovate(store2, newMat);
        Check(ren != null && ren.Needs.Any(n => n.type == newMat && n.need == need) && store2.enabled, "renovation reserves new material, building still in use");
        ren.Deliver("t", newMat, need); ren.BeginWork();
        Check(!store2.enabled, "renovation start stops use");
        ren.Cancel(); await Task.Delay(50);
        Check(store2.enabled && Mathf.Approximately(Loose(newMat), need), "renovation cancel: new material 100% back, function restored");
        ClearLoose();
        ren = Demolition.OrderRenovate(store2, newMat); ren.Deliver("t", newMat, need); ren.BeginWork(); ren.Complete(); await Task.Delay(50);
        Check(store2.MainMaterial == newMat && store2.enabled && Mathf.Approximately(Loose(oldMat), Mathf.FloorToInt(need * GameBalance.DemolishRefundShare)), "renovation complete: old 70% to floor");
        ClearLoose();
        foreach (var x in CommanderRoster.Instance.Commanders) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
    }

    // ⑤ 식품 신선도: 물량별 보존 시간, 상온·바닥은 그대로, 냉장고는 늦추고 냉동고는 멈춘다(내부 식품만). 다 되면 썩은 식품. 꺼내기·바닥·저장도 신선도 유지.
    // 정상 식품이 없으면 썩은 식품을 먹는다(기분 하락·식중독 위험).
    static async Task Food(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        var spot = Ground(WorldMapManager.Instance.HomePosition + Vector3.forward * 12);
        var fridge = Build<ColdStorage>(BuildingKind.Fridge, spot); var freezer = Build<ColdStorage>(BuildingKind.Freezer, spot + Vector3.right * 4);
        await Task.Delay(100);
        Check(fridge.Bin != null && freezer.IsFreezer && !fridge.IsFreezer, "fridge/freezer are storage bins");
        Check(fridge.Bin.Allows(Resource.Food) && !fridge.Bin.Allows(Resource.Stone) && !fridge.Bin.Allows(Resource.RottenFood), "cold storage holds normal food only");
        foreach (var c in new[] { fridge, freezer }) typeof(ColdStorage).GetProperty("Coldness").SetValue(c, 1f);
        Check(Mathf.Approximately(fridge.DecayRate, ColdStorage.FridgeRate) && freezer.DecayRate == 0 && rm.BaseBin.DecayRate == 1, "decay rates: room 1, fridge slower, freezer 0");

        // 같은 시간에 상온·냉장·냉동 보존 시간이 다르게 줄어든다.
        rm.SetPriority(fridge.Bin, 9); rm.AddCapacity(Resource.Food, 50); rm.Add(Resource.Food, 6);
        rm.SetPriority(fridge.Bin, 1); rm.SetPriority(freezer.Bin, 9); rm.Add(Resource.Food, 6); rm.SetPriority(freezer.Bin, 1);
        Check(fridge.Bin.Amount(Resource.Food) >= 6 && freezer.Bin.Amount(Resource.Food) >= 6, "food stored in fridge and freezer");
        var life = FoodLots.ShelfLife(Resource.Food);
        float S(StoreBin b) => b.Lots(Resource.Food).Stalest;
        var f0 = S(fridge.Bin); var z0 = S(freezer.Bin); var b0 = rm.BaseBin.Amount(Resource.Food) > 0 ? rm.BaseBin.Lots(Resource.Food).Freshest : -1; // 가장 오래된 묶음은 그새 썩어 빠질 수 있다
        rm.Spoil(100);
        Check(Mathf.Abs(f0 - S(fridge.Bin) - 100 * ColdStorage.FridgeRate) < .5f && Mathf.Approximately(S(freezer.Bin), z0), "fridge slows, freezer stops decay");
        Check(b0 < 0 || Mathf.Abs(b0 - rm.BaseBin.Lots(Resource.Food).Freshest - 100) < .5f, $"room temperature decays at full rate ({b0} → {rm.BaseBin.Lots(Resource.Food).Freshest})");

        // 다 되면 썩은 식품. 냉장고는 썩은 식품을 받지 않으므로 바닥으로 나온다.
        var fridgeFood = fridge.Bin.Amount(Resource.Food); var rotten0 = Loose(Resource.RottenFood);
        rm.SetPriority(rm.BaseBin, 5);
        var baseFood = rm.BaseBin.Amount(Resource.Food); rm.TakeFrom(rm.BaseBin, Resource.Food, baseFood); // 상온 칸은 비워 결과를 냉장고만 보게 한다
        rm.Spoil(life / ColdStorage.FridgeRate + 10);
        Check(fridge.Bin.Amount(Resource.Food) == 0 && freezer.Bin.Amount(Resource.Food) >= 6, "fridge food spoiled, frozen food kept");
        Check(Mathf.Approximately(Loose(Resource.RottenFood) - rotten0, fridgeFood), $"spoiled amount becomes rotten food on floor ({Loose(Resource.RottenFood) - rotten0} of {fridgeFood})");

        // 꺼내기: 신선도 그대로 바닥 더미로. 바닥은 상온으로 부패한다.
        ClearLoose();
        var shelf = S(freezer.Bin); var n = freezer.Bin.Amount(Resource.Food);
        rm.Eject(freezer.Bin, Resource.Food);
        var pile = ResourceNode.Available.First(x => x != null && x.IsLooseCargo && x.ResourceType == Resource.Food);
        var floor = pile.GetComponent<AntColony.World.FloorFood>();
        Check(floor != null && Mathf.Approximately(floor.Sync().Stalest, shelf) && floor.Lots.Total == n, "ejected food keeps freshness on floor");
        // 저장 왕복: 바닥 식품 신선도가 돌아온다.
        floor.Age(500); var floorShelf = floor.Sync().Stalest;
        Check(SaveSystem.TrySave(false, 2, out var error), "save " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 2), out error), "load " + error); await Ready(); await Task.Delay(300);
        rm = ResourceManager.Instance;
        pile = ResourceNode.Available.FirstOrDefault(x => x != null && x.IsLooseCargo && x.ResourceType == Resource.Food);
        floor = pile != null ? pile.GetComponent<AntColony.World.FloorFood>() : null;
        Check(floor != null && Mathf.Abs(floor.Sync().Stalest - floorShelf) < 2, "floor food freshness restored");
        floor.Age(floor.Sync().Freshest + 1); await Task.Delay(50);
        Check(Loose(Resource.Food) < .5f && Loose(Resource.RottenFood) >= n - .5f, "floor food spoils into rotten food");

        // 썩은 식품 식사: 정상 식품이 없을 때만.
        ClearLoose();
        foreach (var b in rm.Bins.ToList()) { rm.TakeFrom(b, Resource.Food, b.Amount(Resource.Food)); rm.TakeFrom(b, Resource.SimpleMeal, b.Amount(Resource.SimpleMeal)); rm.TakeFrom(b, Resource.FineMeal, b.Amount(Resource.FineMeal)); }
        rm.AddCapacity(Resource.RottenFood, 50); rm.SetAllowed(rm.BaseBin, Resource.RottenFood, true); rm.Add(Resource.RottenFood, 20);
        var eater = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember); var rotten = rm.GetAmount(Resource.RottenFood); // 불러오기 뒤 새 장수
        Check(rotten >= 20, "rotten food stored");
        Call(eater, "StartMeal", new object[] { null });
        var state = eater.PersonalState.meal;
        Check(state.rotten && eater.IsEating && rm.GetAmount(Resource.RottenFood) < rotten, "no normal food: eats rotten food");
        state.eatSeconds = 0; Call(eater, "FinishMeal");
        Check(eater.PersonalState.moodFactors.Any(f => f.reason == "식사: 썩은 식품") && !state.rotten, "rotten meal mood penalty");
        rm.Add(Resource.Food, 20); rotten = rm.GetAmount(Resource.RottenFood);
        Call(eater, "StartMeal", new object[] { null });
        Check(!eater.PersonalState.meal.rotten && rm.GetAmount(Resource.RottenFood) == rotten, "normal food eaten first");
        eater.PersonalState.meal.eatSeconds = 0; eater.PersonalState.meal.poisonSeconds = 0;
        foreach (var c in Object.FindObjectsByType<ColdStorage>()) Object.DestroyImmediate(c.gameObject); ClearLoose();
    }

    static UnityEngine.UI.Button Btn(string part) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
        .FirstOrDefault(b => b.isActiveAndEnabled && (b.name.Contains(part) || b.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.Contains(part))));

    // ⑥ 요리: 생산 목록(지정 횟수·재고 유지·계속), 등급은 레시피, 시작할 때 재료 소비, 시작 전 정지는 손실 없음·시작 후 취소는 재료 손실,
    // 요리사가 바뀌어도 진행 보존, 완성 요리는 새 보존 기간으로 주변 바닥. 식사는 좋은 요리 → 간단한 요리 → 날것 순.
    static async Task Cooking(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        var k = Build<Kitchen>(BuildingKind.Hearth, Ground(WorldMapManager.Instance.HomePosition + Vector3.left * 12)); await Task.Delay(100);
        Check(k != null && !k.IsTable && k.Orders.Count == 1 && k.Orders[0].mode == Workshop.OrderMode.KeepStock, "new kitchen gets default keep-stock order");
        k.Orders.Clear(); rm.AddCapacity(Resource.Food, 200); rm.Add(Resource.Food, 60);
        var fine = Kitchen.Recipes[1]; var simple = Kitchen.Recipes[0];
        Check(k.AddOrder(1, Workshop.OrderMode.Count, 1) && k.NeedsCook, "fine meal order needs a cook");
        var food0 = rm.GetAmount(Resource.Food);
        k.Paused = true;
        Check(!k.NeedsCook && k.WaitReason == "일시정지" && rm.GetAmount(Resource.Food) == food0, "paused before start: nothing consumed");
        k.Paused = false;
        Check(k.Work(list[0], .01f) && k.Current == fine && rm.GetAmount(Resource.Food) == food0 - fine.food && k.Orders.Count == 0, "ingredients consumed at cook start; count order done");
        k.CancelCurrent();
        Check(k.Current == null && rm.GetAmount(Resource.Food) == food0 - fine.food, "cancel after start loses ingredients");
        // 진행 보존·인계: 한 요리사가 절반, 다른 요리사가 마무리.
        k.AddOrder(0, Workshop.OrderMode.Count, 1);
        k.Work(list[0], simple.seconds * .4f / list[0].WorkRate(CommanderActivity.Cooking));
        var half = k.Progress; Check(half > .3f && half < .5f, $"progress kept ({half:P0})");
        k.Work(list[1], simple.seconds * 2);
        Check(k.Current == null && Mathf.Approximately(Loose(Resource.SimpleMeal), 1), "another cook finishes; meal placed on floor");
        var meal = ResourceNode.Available.First(n => n != null && n.IsLooseCargo && n.ResourceType == Resource.SimpleMeal).GetComponent<AntColony.World.FloorFood>();
        Check(meal != null && meal.Sync().Freshest > FoodLots.ShelfLife(Resource.SimpleMeal) - 5, "finished meal has fresh shelf life");
        // 재고 유지: 바닥 요리도 재고로 세어 과잉 생산하지 않는다.
        k.AddOrder(0, Workshop.OrderMode.KeepStock, 1);
        Check(!k.NeedsCook && k.WaitReason == "목표 달성", "keep-stock counts floor meals");
        k.Orders.Clear(); ClearLoose();

        // 식사: 좋은 요리 → 간단한 요리 → 날것.
        foreach (var b in rm.Bins.ToList()) foreach (var t in new[] { Resource.Food, Resource.SimpleMeal, Resource.FineMeal, Resource.RottenFood }) rm.TakeFrom(b, t, b.Amount(t));
        rm.AddCapacity(Resource.SimpleMeal, 20); rm.AddCapacity(Resource.FineMeal, 20);
        rm.Add(Resource.SimpleMeal, 1); rm.Add(Resource.FineMeal, 1); rm.Add(Resource.Food, 20);
        Check(rm.GetAmount(Resource.SimpleMeal) == 1 && rm.GetAmount(Resource.FineMeal) == 1, "meals stored");
        var eater = list[2]; var state = eater.PersonalState.meal;
        Call(eater, "StartMeal", new object[] { null });
        Check(state.quality == 2 && rm.GetAmount(Resource.FineMeal) == 0, "fine meal eaten first");
        state.eatSeconds = 0; Call(eater, "StartMeal", new object[] { null });
        Check(state.quality == 1 && rm.GetAmount(Resource.SimpleMeal) == 0, "then simple meal");
        state.eatSeconds = 0; var raw = rm.GetAmount(Resource.Food); Call(eater, "StartMeal", new object[] { null });
        Check(state.quality == 0 && rm.GetAmount(Resource.Food) < raw, "then raw food");
        state.eatSeconds = 0; state.poisonSeconds = 0; Call(eater, "FinishMeal");
        var nearTable = Object.FindObjectsByType<Kitchen>().Any(x => x.isActiveAndEnabled && (x.IsTable || x.Data.kind == BuildingKind.Kitchen) && (x.Position - eater.Position).sqrMagnitude < 16);
        Check(nearTable != eater.PersonalState.moodFactors.Any(f => f.reason == "식탁 없이 식사"), "no-table mood penalty only without a table");
        state.poisonSeconds = 0;

        // 저장: 주문·정지·진행 중 요리. 옛 저장(값 치른 비축 조리)은 간단한 요리 진행으로 이어진다.
        k.AddOrder(1, Workshop.OrderMode.Forever, 1); k.Paused = true; rm.Add(Resource.Food, 20); k.Paused = false; k.Work(list[0], .5f); k.Paused = true;
        var progress = k.Progress;
        Check(SaveSystem.TrySave(false, 0, out var error), "save " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error); await Ready(); await Task.Delay(300);
        k = Object.FindObjectsByType<Kitchen>().FirstOrDefault(x => x.Data.kind == BuildingKind.Hearth && x.Paused);
        Check(k != null && k.Current == Kitchen.Recipes[1] && Mathf.Abs(k.Progress - progress) < .01f && k.Orders.Any(o => o.mode == Workshop.OrderMode.Forever), "kitchen orders/pause/progress restored");
        var legacy = new Kitchen.State { paid = true, progress = 3 }; typeof(Kitchen).GetProperty("Meals").SetValue(k, legacy); // 저장 복원 경로(internal set)
        Check(k.Current == Kitchen.Recipes[0] && k.Orders.Count == 1 && Kitchen.Valid(legacy), "legacy paid cooking continues as simple meal");
    }

    // ⑦ 작업 관리: 선택한 예정지·요리 시설의 막힌 사유 개수, 필요/예약/도착 수량, 일괄 우선순위·일시정지·재개·취소(두 번 확인).
    static async Task Tasks(System.Collections.Generic.List<CommanderAnt> list)
    {
        var rm = ResourceManager.Instance; ClearLoose();
        foreach (var s in Object.FindObjectsByType<BuildingConstructionSite>()) Object.DestroyImmediate(s.gameObject);
        var k = Object.FindObjectsByType<Kitchen>().First(x => x.Data.kind == BuildingKind.Hearth); k.Paused = false;
        Spend(Resource.Soil, 0);
        var site = Needy(Ground(WorldMapManager.Instance.HomePosition + Vector3.right * 9), 30); await Task.Delay(50);
        Check(GameMenuController.TaskCandidates().Contains(site) && GameMenuController.TaskCandidates().Contains(k), "sites and kitchens are task candidates");
        var menu = GameMenuController.Instance;
        menu.ShowTasks(new System.Collections.Generic.List<Component> { site, k }); await Task.Delay(50);
        var texts = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Select(t => t.text).ToList();
        Check(texts.Any(t => t.Contains("막힌 작업") && t.Contains("재료 부족")), "blocked summary shows reason counts: " + string.Join("|", texts.Where(t => t.Contains("막힌"))));
        Check(texts.Any(t => t.Contains("필요 30") && t.Contains("도착 0")), "needed/reserved/arrived quantities shown");
        Btn("선택 전체 일시정지").onClick.Invoke(); await Task.Delay(20);
        Check(site.Paused && k.Paused, "batch pause");
        Btn("선택 전체 재개").onClick.Invoke(); await Task.Delay(20);
        Check(!site.Paused && !k.Paused, "batch resume");
        GatherDesignation.PriorityLevel = 8; Btn("선택 전체에").onClick.Invoke(); await Task.Delay(20);
        Check(WorkPriorities.Level(site) == 8 && WorkPriorities.Level(k) == 8, "batch priority");
        WorkPriorities.Set(k, WorkPriorities.Default, false); GatherDesignation.PriorityLevel = WorkPriorities.Default;
        site.Deliver("t", Resource.Soil, 10);
        Btn("선택 전체 취소").onClick.Invoke(); await Task.Delay(20);
        Check(site != null && Btn("한 번 더 누르면") != null, "batch cancel needs a second press");
        Btn("한 번 더 누르면").onClick.Invoke(); await Task.Delay(100);
        Check(site == null && Mathf.Approximately(Loose(Resource.Soil), 10), "batch cancel: site removed, delivered material on floor");
        menu.Resume(); ClearLoose(); k.Orders.Clear(); k.CancelCurrent();
    }
}
