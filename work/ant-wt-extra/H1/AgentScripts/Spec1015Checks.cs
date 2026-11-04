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
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-15 HUD V5 → Unity 실제 구현 검사. 영역별 메서드(연구·장비·농사 …)를 순서대로 실행한다.
public static class Spec1015Checks
{
    static int checks;
    static readonly List<string> done = new List<string>();
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable position");
        Check(c.Agent.Warp(hit.position), "warp");
    }
    static ScienceLab Lab(Vector3 p)
    {
        var template = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
        var lab = Object.Instantiate(template, p, Quaternion.identity); lab.name = "ScienceLab"; lab.gameObject.SetActive(true); return lab;
    }
    static void Rich() { var rm = ResourceManager.Instance; foreach (var t in new[] { Resource.Food, Resource.Soil, Resource.Special }) if (rm.GetCapacity(t) < 6000) rm.AddCapacity(t, 6000); rm.Add(Resource.Food, 5000); rm.Add(Resource.Soil, 5000); rm.Add(Resource.Special, 2000); }

    // UI 겹침 검사: 창 안 섹션·고정 버튼의 사각형이 서로 겹치지 않고 창 밖으로 나가지 않는다.
    public static void NoOverlap(string screen)
    {
        var window = Object.FindObjectsByType<RectTransform>().FirstOrDefault(r => r.name == "V5Window" && r.gameObject.activeInHierarchy);
        Check(window != null, screen + " V5 window");
        var parts = new List<RectTransform>();
        foreach (Transform child in window) if (child.gameObject.activeSelf && child is RectTransform rt && child.GetComponent<UnityEngine.UI.Text>() == null && child.name != "Line") parts.Add(rt);
        Rect R(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y); }
        var w = R(window);
        for (var i = 0; i < parts.Count; i++)
        {
            var a = R(parts[i]);
            Check(a.xMin >= w.xMin - 1 && a.xMax <= w.xMax + 1 && a.yMin >= w.yMin - 1 && a.yMax <= w.yMax + 1, $"{screen}: {parts[i].name} inside window");
            for (var j = i + 1; j < parts.Count; j++)
            {
                var b = R(parts[j]);
                var ov = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1 && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1;
                Check(!ov, $"{screen}: {parts[i].name} overlaps {parts[j].name}");
            }
        }
    }

    public static async Task<string> Main()
    {
        checks = 0; done.Clear(); var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1015-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261015, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false; // 무작위 침입이 검사 흐름·저장을 막지 않게(다른 검사와 동일)
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            Rich();
            await Research(list); done.Add("Research");
            Equipment(list); done.Add("Equipment");
            await Farm(list); done.Add("Farm");
            await Logistics(list); done.Add("Logistics");
            await Medical(list); done.Add("Medical");
            await Hygiene(list); done.Add("Hygiene");
            await Death(list); done.Add("Death");
            await Prisoners(list); done.Add("Prisoners");
            await SaveRoundtrip(); done.Add("Save");
            return $"PASS Spec1015Checks {checks} [{string.Join(",", done)}]";
        }
        finally { Time.timeScale = 1; GameMenuController.Instance?.Resume(); SaveStorage.RootOverride = previousRoot; }
    }

    static async Task Until(Func<bool> ok, float seconds, string label)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!ok() && DateTime.UtcNow < end) await Task.Delay(50);
        Check(ok(), label);
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static readonly FieldInfo SowField = typeof(ResourceNode).GetField("sowRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
    static object Call(Type type, string name, params object[] args)
        => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).First(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(null, args);
    static Vector3 Ground(Vector3 p) => NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas) ? hit.position : p;

    static void Equipment(List<CommanderAnt> list)
    {
        var inv = EquipmentInventory.Instance; var c = list[2];
        foreach (var e in c.PersonalState.equipment.ToArray()) inv.Unequip(c, e);
        var w1 = new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.Mandible, quality = 1 };
        var w2 = new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.AcidSprayer, quality = 2 };
        Check(inv.Add(w1) && inv.Add(w2) && !inv.Add(w1), "add instances, no duplicate id");
        int Total() => inv.Items.Count + CommanderRoster.Instance.Commanders.Sum(x => x.PersonalState.equipment.Count);
        var total = Total();
        Check(inv.Equip(c, w1) && c.Weapon == w1 && !inv.Items.Contains(w1), "equip");
        Check(inv.Equip(c, w2) && c.Weapon == w2 && inv.Items.Contains(w1) && Total() == total, "swap keeps old in inventory, no dupes");
        Check(!inv.Equip(list[3], w2), "cannot take another commander's equipped item");
        // 원정 중(본거지 밖)은 보관함 원격 장착·해제 불가
        var go = new GameObject("FakeTransport"); go.SetActive(false); var t = go.AddComponent<ExpeditionTransport>();
        typeof(CommanderAnt).GetProperty("Transport").SetValue(c, t);
        Check(c.IsAwayFromHome && inv.ChangeBlock(c, w1, w2).StartsWith("본거지 밖") && !inv.Equip(c, w1) && !inv.Unequip(c, w2), "remote equip blocked");
        typeof(CommanderAnt).GetProperty("Transport").SetValue(c, null); Object.DestroyImmediate(go);
        Check(inv.Unequip(c, w2) && c.Weapon == null && inv.Items.Contains(w2) && Total() == total, "unequip returns item");
        Check(inv.Equip(c, w2), "re-equip for save");
        // 반환 장비는 자동 장착하지 않음: 귀환 처리 경로(StoreEquipment) → 보관함
        var gear = new EquipmentItem { slot = EquipmentSlot.Armor, armor = ArmorKind.Coating };
        Call(typeof(DiplomacyManager), "StoreEquipment", new List<EquipmentItem> { gear }, c.Position);
        Check(inv.Items.Contains(gear) && c.EquippedArmor != gear, "returned gear stored, not auto-equipped");
        GameMenuController.Instance.ShowEquipment(c); NoOverlap("equipment"); GameMenuController.Instance.Resume();
    }

    static async Task Farm(List<CommanderAnt> list)
    {
        Daytime();
        var farmer = list.First(x => !x.IsSleepTime && x.CanDoJob(CommanderJobs.Farming)); farmer.SetJobEnabled(CommanderJobs.Farming, true); farmer.WorkState.duty = CommanderDuty.Civilian;
        var a = Build<BuildingBase>(BuildingKind.Farm, Ground(farmer.Position + Vector3.right * 5));
        var pa = a.GetComponent<FarmPlot>() ?? a.gameObject.AddComponent<FarmPlot>(); pa.Configure(FarmCrop.Fungus, false);
        var na = pa.Node;
        Check(na.NeedsSowing && !pa.Planted, "new plot waits sowing");
        Time.timeScale = 3;
        await Until(() => !na.NeedsSowing, 60, "farmer sows by work table");
        Time.timeScale = 0; na.TickGrowth(100000); Check(!na.IsDepleted, "crop matured");
        Time.timeScale = 3;
        await Until(() => na.IsDepleted, 60, "farmer harvests");
        await Until(() => ResourceNode.Available.Any(n => n != null && n.IsLooseCargo && n.ResourceType == Resource.Food && (n.transform.position - a.Position).sqrMagnitude < 36), 10, "harvest dropped on floor at farm");
        Time.timeScale = 0;
        Check(na.IsRegrowing && !na.NeedsSowing && pa.Planted, "planted crop regrows after harvest (no resow)");
        // 뿌리뽑기 → 빈 밭, 자동 재파종 없음
        pa.OrderUproot(true); Check(na.CanGather && na.HoldsHandWork, "uproot order creates work");
        Time.timeScale = 3; await Until(() => pa.Data.fallow, 60, "farmer uproots"); Time.timeScale = 0;
        Check(!pa.Planted && !na.CanGather && !na.NeedsSowing && !na.IsRegrowing, "uprooted plot stays empty");
        na.TickGrowth(100000); Check(na.IsDepleted && pa.Data.fallow, "empty plot does not regrow");
        pa.Replant(); Check(na.NeedsSowing && !pa.Data.fallow, "replant order needs sowing");
        farmer.SetJobEnabled(CommanderJobs.Farming, false); farmer.CommandStop();
        // 병충해: 실제 칸 좌표의 상하좌우만 전파, 대각선 없음
        var size = a.GetComponent<Renderer>().bounds.size;
        // 실제 배치처럼 격자(1칸)에 맞춘다: 3칸 밭은 중심이 x.5.
        a.transform.position = new Vector3(Mathf.Floor(a.Position.x) + size.x % 2 * .5f, a.Position.y, Mathf.Floor(a.Position.z) + size.z % 2 * .5f);
        var b = Build<BuildingBase>(BuildingKind.Farm, a.Position + new Vector3(size.x, 0, 0));
        var d = Build<BuildingBase>(BuildingKind.Farm, a.Position + new Vector3(-size.x, 0, size.z));
        var pb = b.GetComponent<FarmPlot>() ?? b.gameObject.AddComponent<FarmPlot>(); pb.Configure(FarmCrop.Fungus, false);
        var pd = d.GetComponent<FarmPlot>() ?? d.gameObject.AddComponent<FarmPlot>(); pd.Configure(FarmCrop.Fungus, false);
        foreach (var p in new[] { pa, pb, pd }) { SowField.SetValue(p.Node, 0f); p.Data.pest = false; }
        Check(pa.Neighbors().Contains(pb) && !pa.Neighbors().Contains(pd), "orthogonal neighbor yes, diagonal no");
        pa.Data.pest = true;
        for (var i = 0; i < 200; i++) pa.Tick(FarmPlot.PestCheckSeconds);
        Check(pb.Data.pest && !pd.Data.pest, "pest spread orthogonally only");
        var tb = pb.Node.RegrowTimeRemaining; pb.Node.TickGrowth(10); var slowed = tb - pb.Node.RegrowTimeRemaining;
        var tc = pd.Node.RegrowTimeRemaining; pd.Node.TickGrowth(10); var normal = tc - pd.Node.RegrowTimeRemaining;
        Check(normal > 0 && Mathf.Abs(slowed - normal * FarmPlot.PestGrowth) < .05f, $"pest halves growth ({slowed} vs {normal})");
        GameMenuController.Instance.ShowFarm(pb); NoOverlap("farm"); GameMenuController.Instance.Resume();
        Object.DestroyImmediate(d.gameObject);
    }

    static async Task SaveRoundtrip()
    {
        var r = CampaignResearch.Instance; var progress = r.Progress; var active = r.Active?.Technology;
        var fallow = FarmPlot.All.Count(f => f.Data.fallow); var pests = FarmPlot.All.Count(f => f.Data.pest);
        var equipped = CommanderRoster.Instance.Commanders.Where(x => x.PersonalState.equipment.Count > 0).ToDictionary(x => x.PersonalState.id, x => string.Join(",", x.PersonalState.equipment.Select(e => e.id).OrderBy(i => i)));
        var stored = EquipmentInventory.Instance.Items.Select(e => e.id).OrderBy(i => i).ToList();
        foreach (var f in Object.FindObjectsByType<ColdStorage>()) typeof(ColdStorage).GetProperty("Coldness").SetValue(f, .7f); // 무전원이라 앞 단계 동안 식었을 수 있음
        var cold = Object.FindObjectsByType<ColdStorage>().Select(f => f.Coldness).DefaultIfEmpty(-1).Max();
        CommanderAnt.CommonDiet = CommanderAnt.DietSimple | CommanderAnt.DietFine;
        var hp = CommanderRoster.Instance.Commanders.First(x => !x.IsDead); Clear(hp);
        MedicalRules.AddWound(hp.Health, BodyPart.HandR, WoundKind.Acid, 2).tended = true; hp.Health.illnesses.Add(new Illness { kind = IllnessKind.WoundInfection, part = BodyPart.HandR, severity = .3f, immunity = .2f, tendLeft = 40 });
        CommanderAnt.RequestSurgery(hp, SurgeryKind.Amputate, BodyPart.HandR); hp.Health.medicine = MedicineLevel.Basic; hp.Health.bedRest = 7;
        var hpId = hp.PersonalState.id; var hpJson = JsonUtility.ToJson(hp.Health);
        var deaths = Memorial.Deaths.Count; var tombs = string.Join("|", Tombstone.All.Select(t => JsonUtility.ToJson(t.Data2)).OrderBy(x => x));
        var compost = string.Join("|", Object.FindObjectsByType<CompostBin>().Select(b => JsonUtility.ToJson(b.Data2) + b.RawCount).OrderBy(x => x));
        var dietId = CommanderRoster.Instance.Commanders[1].PersonalState.id; CommanderRoster.Instance.Commanders[1].PersonalState.meal.diet = CommanderAnt.DietRaw;
        Check(SaveSystem.TrySave(false, 1, out var error), "save " + error);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out error), "load " + error); await Ready(); await Task.Delay(300);
        r = CampaignResearch.Instance;
        Check(r.Active?.Technology == active && Mathf.Abs(r.Progress - progress) < .01f, "research restored");
        Check(FarmPlot.All.Count(f => f.Data.fallow) == fallow && FarmPlot.All.Count(f => f.Data.pest) == pests, "farm state restored");
        foreach (var kv in equipped)
        {
            var c = CommanderRoster.Instance.Commanders.First(x => x.PersonalState.id == kv.Key);
            Check(string.Join(",", c.PersonalState.equipment.Select(e => e.id).OrderBy(i => i)) == kv.Value, "equipment instances restored " + c.CommanderName);
        }
        Check(EquipmentInventory.Instance.Items.Select(e => e.id).OrderBy(i => i).SequenceEqual(stored), "inventory instances restored");
        Check(cold > .5f && Object.FindObjectsByType<ColdStorage>().Any(f => Mathf.Abs(f.Coldness - cold) < .05f), "fridge coldness restored " + cold);
        Check(CommanderAnt.CommonDiet == (CommanderAnt.DietSimple | CommanderAnt.DietFine)
            && CommanderRoster.Instance.Commanders.First(x => x.PersonalState.id == dietId).PersonalState.meal.diet == CommanderAnt.DietRaw, "diets restored");
        CommanderAnt.CommonDiet = CommanderAnt.AllDiet;
        var hp2 = CommanderRoster.Instance.Commanders.First(x => x.PersonalState.id == hpId);
        Check(JsonUtility.ToJson(hp2.Health) == hpJson, "health (wounds/illness/surgery/settings) restored");
        Check(Memorial.Deaths.Count == deaths && deaths > 0, "death records restored");
        Check(string.Join("|", Tombstone.All.Select(t => JsonUtility.ToJson(t.Data2)).OrderBy(x => x)) == tombs, "tombs restored");
        Check(string.Join("|", Object.FindObjectsByType<CompostBin>().Select(b => JsonUtility.ToJson(b.Data2) + b.RawCount).OrderBy(x => x)) == compost, "compost restored");
    }

    // 낮 시간으로 맞춘다(밤에는 장수가 자서 실제 작업 흐름 검사가 멈춘다).
    static void Daytime() { var gs = GameCalendar.GameSeconds; GameSession.Instance.MarkStarted(10, gs - gs % GameCalendar.SecondsPerDay + 30); }
    static async Task Logistics(List<CommanderAnt> list)
    {
        Daytime();
        var rm = ResourceManager.Instance;
        // 요리 재료 실제 운반: 요리사가 창고에서 집어 조리대로 → 조리 시작 때 소비 → 완성 요리 바닥
        var cook = list.Skip(4).Concat(list).First(x => !x.IsSleepTime && !x.IsAsleep && x.CanDoJob(CommanderJobs.Cooking)); // 야행성·요리 불가 특성 제외
        cook.SetJobEnabled(CommanderJobs.Cooking, true); cook.WorkState.duty = CommanderDuty.Civilian;
        var k = Build<Kitchen>(BuildingKind.Kitchen, Ground(cook.Position + Vector3.forward * 6));
        k.Orders.Clear(); Check(k.AddOrder(0, Workshop.OrderMode.Count, 1), "cook order");
        var r = Kitchen.Recipes[0];
        Check(k.NeedsIngredients && !k.NeedsCook && k.Staged == 0, "kitchen waits for ingredients (no remote spend)");
        var sawCourier = false;
        Time.timeScale = 3;
        await Until(() => { sawCourier |= cook.DeliverySite == (IMaterialSink)k; return k.Staged >= r.food || k.Current != null; }, 60, "cook fetched ingredients");
        Check(sawCourier, "ingredients carried by cook");
        await Until(() => ResourceNode.Available.Any(n => n != null && n.IsLooseCargo && n.ResourceType == Resource.SimpleMeal), 60, "meal cooked onto floor");
        Time.timeScale = 0; cook.SetJobEnabled(CommanderJobs.Cooking, false); cook.CommandStop();
        Check(k.Staged == 0 && k.Current == null, "ingredients consumed at start");
        // 철거: 놓아둔 재료는 바닥으로(손실 없음)
        Call(k, "Deliver", cook, Resource.Food, 7);
        var floor0 = ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == Resource.Food).Sum(n => n.AmountRemaining);
        Call(k, "EjectStaged");
        var floor1 = ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == Resource.Food).Sum(n => n.AmountRemaining);
        Check(k.Staged == 0 && Mathf.Abs(floor1 - floor0 - 7) < .01f, "staged ingredients dropped on removal");
        GameMenuController.Instance.ShowKitchen(k); NoOverlap("kitchen"); GameMenuController.Instance.Resume();
        Object.DestroyImmediate(k.gameObject);
        // 식단: 공통 = 날것만 → 요리가 있어도 날것을 먹음, 장수별 덮어쓰기 = 좋은 요리만
        rm.Add(Resource.SimpleMeal, 5); rm.Add(Resource.FineMeal, 5);
        var eater = list[0];
        CommanderAnt.CommonDiet = CommanderAnt.DietRaw;
        int simple0 = rm.GetAmount(Resource.SimpleMeal), fine0 = rm.GetAmount(Resource.FineMeal), food0 = rm.GetAmount(Resource.Food);
        Call(eater, "StartMeal", new object[] { null });
        Check(rm.GetAmount(Resource.SimpleMeal) == simple0 && rm.GetAmount(Resource.FineMeal) == fine0 && rm.GetAmount(Resource.Food) < food0, "common raw-only diet eats raw");
        eater.PersonalState.meal.eatSeconds = 0; eater.PersonalState.meal.diet = CommanderAnt.DietFine;
        Call(eater, "StartMeal", new object[] { null });
        Check(rm.GetAmount(Resource.FineMeal) == fine0 - 1 && eater.PersonalState.meal.quality == 2, "per-commander override eats fine meal");
        eater.PersonalState.meal.eatSeconds = 0; eater.PersonalState.meal.diet = -1; CommanderAnt.CommonDiet = CommanderAnt.AllDiet;
        GameMenuController.Instance.ShowDiet(eater); NoOverlap("diet"); GameMenuController.Instance.Resume();
        // 냉각 정도 저장용 냉장고
        var fridge = Build<ColdStorage>(BuildingKind.Fridge, Ground(eater.Position + Vector3.left * 6));
        typeof(ColdStorage).GetProperty("Coldness").SetValue(fridge, .7f);
    }
    static object Call(object target, string name, params object[] args)
    {
        for (var t = target.GetType(); t != null; t = t.BaseType)
        {
            var m = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).FirstOrDefault(x => x.Name == name && x.GetParameters().Length == args.Length);
            if (m != null) return m.Invoke(target, args);
        }
        throw new Exception("no method " + name);
    }

    static void Unlock(ScienceTechnology t)
    {
        var st = (CampaignResearch.State)typeof(CampaignResearch).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(CampaignResearch.Instance);
        if (!st.completed.Contains((int)t)) st.completed.Add((int)t);
    }
    static CommanderAnt Awake(List<CommanderAnt> list, CommanderJobs job, params CommanderAnt[] not)
        => list.First(x => !not.Contains(x) && !x.IsDead && !x.IsSleepTime && !x.IsAsleep && x.PersonalHealth > 0 && !x.Health.Injured && (job == CommanderJobs.None || x.CanDoJob(job)));
    static void Clear(CommanderAnt c) { c.Health.wounds.Clear(); c.Health.illnesses.Clear(); c.Health.bloodLoss = 0; c.Health.surgeries.Clear(); }

    static async Task Medical(List<CommanderAnt> list)
    {
        Daytime(); var rm = ResourceManager.Instance;
        foreach (var x in list) x.SetJobEnabled(CommanderJobs.All, false);
        Unlock(ScienceTechnology.Infirmary);
        // 1) 부위별 능력: 다리 상실 → 이동 저하, 찔림 → 출혈·예상 사망 시간
        var h = new HealthState();
        MedicalRules.AddWound(h, BodyPart.LegL, WoundKind.Missing, 1).age = 999;
        Check(h.Moving < .6f && h.Sight == 1 && !h.Bleeding, "missing legs reduce moving only");
        MedicalRules.AddWound(h, BodyPart.Abdomen, WoundKind.Stab, 6);
        Check(h.Bleeding && h.SecondsToBleedOut < 10000 && !float.IsInfinity(h.SecondsToBleedOut), "stab bleeds with ETA");
        var eta = h.SecondsToBleedOut; MedicalRules.ApplyTend(h, .8f, false);
        Check(h.SecondsToBleedOut > eta * 2, "tending slows bleeding");
        // 흉터·상실은 일반 처치로 복구되지 않음
        var h2 = new HealthState(); MedicalRules.AddWound(h2, BodyPart.EyeL, WoundKind.Scar, .3f); MedicalRules.AddWound(h2, BodyPart.HandL, WoundKind.Missing, 1).age = 999;
        MedicalRules.ApplyTend(h2, 1, true); MedicalRules.Tick(h2, 100000, true, false);
        Check(h2.wounds.Count == 2 && h2.Missing(BodyPart.HandL), "scar/missing not healed by treatment");
        // 처치 완료 ≠ 완치: 처치 후에도 상처가 남고 시간이 지나야 낫는다
        var h3 = new HealthState(); MedicalRules.AddWound(h3, BodyPart.LegR, WoundKind.Bruise, 3); MedicalRules.ApplyTend(h3, .5f, false);
        Check(h3.wounds.Count == 1 && h3.wounds[0].tended, "tended but not healed");
        MedicalRules.Tick(h3, 10000, false, false); Check(h3.wounds.All(w => w.Permanent), "heals over time");
        // 2) 약품 허용 등급: 범위 안에서 가장 좋은 재고
        int Stock(Resource t) => t == Resource.MedicineBasic || t == Resource.MedicineAdvanced ? 5 : 0;
        Check(MedicalRules.BestMedicine(MedicineLevel.Basic, Stock) == 0 && MedicalRules.BestMedicine(MedicineLevel.Normal, Stock) == 0
            && MedicalRules.BestMedicine(MedicineLevel.Advanced, Stock) == 2 && MedicalRules.BestMedicine(MedicineLevel.NoMedicine, Stock) == -1, "medicine allowance picks best allowed stock");
        Check(MedicalRules.TendQuality(20, -1, true, 1, false) <= .5f && MedicalRules.TendQuality(5, 2, true, 1, false) > MedicalRules.TendQuality(5, 0, true, 1, false)
            && MedicalRules.TendQuality(10, 1, false, 1, true) < MedicalRules.TendQuality(10, 1, false, 1, false), "quality: no-med cap, tier, self penalty");
        // 3) 실제 의료 장수 처치(약 없이)
        var doctor = Awake(list, CommanderJobs.Nursing); var patient = Awake(list, CommanderJobs.None, doctor);
        Clear(patient); patient.Health.medicine = MedicineLevel.NoMedicine; patient.Health.selfTend = false;
        MedicalRules.AddWound(patient.Health, BodyPart.LegL, WoundKind.Cut, 2);
        Check(CommanderAnt.CareWaitReason(patient) == "치료할 장수 없음", "no doctor reason");
        doctor.SetJobEnabled(CommanderJobs.Nursing, true); doctor.WorkState.duty = CommanderDuty.Civilian;
        Time.timeScale = 3; await Until(() => patient.Health.wounds.All(w => w.tended), 60, "doctor tends patient"); Time.timeScale = 0;
        Check(patient.Health.wounds.First(w => w.kind == WoundKind.Cut).quality <= .5f, "no-medicine tend quality capped");
        // 질병 투약은 약이 있어야: 약 없이 설정이면 투약 대기 사유
        patient.Health.illnesses.Add(new Illness { kind = IllnessKind.Mold, part = BodyPart.Thorax });
        Check(!MedicalRules.NeedsDoctor(patient.Health, false) && CommanderAnt.CareWaitReason(patient).Contains("투약 불가"), "illness needs medicine");
        rm.Add(Resource.MedicineBasic, 3); patient.Health.medicine = MedicineLevel.Normal;
        var basic0 = rm.GetAmount(Resource.MedicineBasic);
        Time.timeScale = 3; await Until(() => patient.Health.illnesses.All(i => i.tendLeft > 0), 60, "doctor gives medicine"); Time.timeScale = 0;
        Check(rm.GetAmount(Resource.MedicineBasic) == basic0 - 1 && patient.Health.illnesses[0].quality > 0, "medicine consumed for illness tend");
        // 4) 쓰러짐 → 구조 운반 → 의료 침상 → 급식
        var bed = Build<Infirmary>(BuildingKind.Infirmary, Ground(doctor.Position + Vector3.forward * 8));
        var hauler = Awake(list, CommanderJobs.Hauling, doctor, patient); hauler.SetJobEnabled(CommanderJobs.Hauling, true); hauler.WorkState.duty = CommanderDuty.Civilian;
        Clear(patient); foreach (var leg in new[] { BodyPart.LegL, BodyPart.LegR }) MedicalRules.AddWound(patient.Health, leg, WoundKind.Bruise, 7.2f);
        foreach (var w in patient.Health.wounds) { w.tended = true; w.quality = 1; w.infectionRolled = true; }
        patient.TickPersonal(.1f); patient.TickDuty(.1f);
        Check(patient.Health.Downed && patient.PersonalHealth <= 0, "pain/moving downs commander");
        Time.timeScale = 3; await Until(() => patient.TreatmentFacility == bed, 90, "hauler rescues to medical bed"); Time.timeScale = 0;
        patient.PersonalState.meal.satiety = 10;
        Time.timeScale = 3; await Until(() => patient.Satiety > 90, 60, "doctor feeds bedridden patient"); Time.timeScale = 0;
        hauler.SetJobEnabled(CommanderJobs.All, false);
        // 5) 수술: 바닥 불가·약품 예약·시작 시 소비·시작 후 취소 불가·기여도 가중·성공/실패/사망
        Clear(patient); MedicalRules.AddWound(patient.Health, BodyPart.LegR, WoundKind.Burn, 4).tended = true;
        var normal = rm.GetAmount(Resource.MedicineNormal); if (normal > 0) rm.TrySpend(Resource.MedicineNormal, normal, ResourceReason.Other);
        Check(CommanderAnt.RequestSurgery(patient, SurgeryKind.Amputate, BodyPart.LegR), "request amputation");
        Check(!CommanderAnt.RequestSurgery(patient, SurgeryKind.Amputate, BodyPart.Head), "vital part not amputatable");
        var op = patient.Health.surgeries[0];
        Check(CommanderAnt.SurgeryBlock(patient, op, null).Contains("부족"), "missing medicine blocks surgery");
        rm.Add(Resource.MedicineNormal, 1);
        Check(CommanderAnt.SurgeryBlock(patient, op, null) == "", "in bed + medicine → ready");
        var other = list.Last(x => x != patient && x != doctor);
        Clear(other); CommanderAnt.RequestSurgery(other, SurgeryKind.Amputate, BodyPart.EyeL);
        Check(CommanderAnt.Reserved(Resource.MedicineNormal, other.Health.surgeries[0]) == 1, "reservation counted separately from stock");
        CommanderAnt.CancelSurgery(other, other.Health.surgeries[0]); Check(other.Health.surgeries.Count == 0 && CommanderAnt.Reserved(Resource.MedicineNormal, null) == 1, "cancel before start releases reservation");
        bed.Release(patient); Check(CommanderAnt.SurgeryBlock(patient, op, null).Contains("침상"), "floor surgery blocked");
        Call(bed, "Admit", patient);
        MedicalRules.Roll = () => 0f; // 성공
        var med0 = rm.GetAmount(Resource.MedicineNormal);
        Time.timeScale = 3; await Until(() => op.stage != SurgeryStage.Waiting, 60, "surgeon starts operation");
        Check(rm.GetAmount(Resource.MedicineNormal) == med0 - 1 && !CommanderAnt.CancelSurgery(patient, op), "start consumes, no cancel after start");
        await Until(() => !op.Active, 90, "operation completes"); Time.timeScale = 0;
        Check(op.stage == SurgeryStage.Done && patient.Health.Missing(BodyPart.LegR) && patient.Health.wounds.Any(w => w.kind == WoundKind.Surgical), "success: purpose resolved, recovery wound");
        // 실패(생존)·실패(사망)를 결정적으로 재현. 기여도 가중: 20 작업 중 절반은 의료 0, 절반은 20 → 평균 10
        var fail = new SurgeryOrder { kind = SurgeryKind.Amputate, part = BodyPart.EyeR, stage = SurgeryStage.Started, work = 20, weighted = 10 * 0 + 10 * 20 };
        patient.Health.surgeries.Add(fail);
        Check(Mathf.Approximately(CommanderAnt.SuccessChance(patient.Health, fail.weighted / fail.work), CommanderAnt.SuccessChance(patient.Health, 10)), "success uses contribution-weighted skill");
        var rolls = new Queue<float>(new[] { .999f, .999f }); MedicalRules.Roll = () => rolls.Count > 0 ? rolls.Dequeue() : .999f;
        var woundsBefore = patient.Health.wounds.Count;
        Call(typeof(CommanderAnt), "FinishSurgery", patient, fail);
        Check(fail.stage == SurgeryStage.Failed && !patient.IsDead && patient.Health.wounds.Count == woundsBefore + 1 && !patient.Health.Missing(BodyPart.EyeR), "failure: not done, extra damage, alive");
        Check(!patient.Health.surgeries.Any(s => s.Active), "no automatic retry");
        if (CommanderDeathRuntime.Mode != CommanderDeathMode.Gentle)
        {
            var victim = list.Last(x => x != patient && x != doctor && !x.IsDead); Clear(victim);
            var deadly = new SurgeryOrder { kind = SurgeryKind.Amputate, part = BodyPart.EyeR, stage = SurgeryStage.Started, work = 20, weighted = 0 };
            victim.Health.surgeries.Add(deadly);
            rolls = new Queue<float>(new[] { .999f, 0f }); MedicalRules.Roll = () => rolls.Count > 0 ? rolls.Dequeue() : .999f;
            Call(typeof(CommanderAnt), "FinishSurgery", victim, deadly);
            Check(victim.IsDead && deadly.result.Contains("사망"), "failure: death path");
        }
        MedicalRules.Roll = () => UnityEngine.Random.value;
        doctor.SetJobEnabled(CommanderJobs.All, false); doctor.CommandStop();
        // 6) 출혈 사망(사망 모드에 따름)
        var bleeder = list.First(x => !x.IsDead && x != patient && x != doctor); Clear(bleeder);
        MedicalRules.AddWound(bleeder.Health, BodyPart.Thorax, WoundKind.Stab, 12);
        for (var i = 0; i < 400 && !bleeder.IsDead; i++) bleeder.TickPersonal(5);
        Check(CommanderDeathRuntime.Mode == CommanderDeathMode.Gentle ? !bleeder.IsDead : bleeder.IsDead, "bleed-out follows death mode " + CommanderDeathRuntime.Mode);
        // 7) 옛 저장 이관
        var old = new CommanderPersonalState(); old.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Serious, remaining = 100 });
        old.injuries.Add(new CommanderInjury { part = InjuryPart.Head, severity = InjurySeverity.Permanent }); old.infected = true; old.health.migrated = false;
        MedicalRules.Migrate(old);
        Check(old.injuries.Count == 0 && !old.infected && old.health.wounds.Count == 2 && old.health.wounds.Any(w => w.kind == WoundKind.Scar) && old.health.illnesses.Any(i => i.kind == IllnessKind.Mold), "old save migration");
        // 8) 2026-10-18 캐릭터 전환: 팔 상실이면 손도 못 씀, 옛 개미 부위(v17) → 두 팔·두 다리(v18)
        var arm = new HealthState(); MedicalRules.AddWound(arm, BodyPart.ArmL, WoundKind.Missing, 1).age = 999;
        Check(arm.PartFraction(BodyPart.HandL) == 0 && arm.Manipulation <= .5f && arm.Moving == 1, "missing arm disables hand, not legs");
        var ant = new CommanderPersonalState(); var ah = ant.health;
        ah.wounds.Add(new Wound { part = (BodyPart)5, kind = WoundKind.Cut, damage = 2.5f });            // 왼큰턱 → 왼손(비율 유지 4/5)
        ah.wounds.Add(new Wound { part = (BodyPart)8, kind = WoundKind.Missing, damage = 1, age = 999 }); // 다리1 상실
        ah.wounds.Add(new Wound { part = (BodyPart)10, kind = WoundKind.Missing, damage = 1, age = 999 });// 다리3 상실 → 왼다리 상실 하나로
        ah.wounds.Add(new Wound { part = (BodyPart)3, kind = WoundKind.Missing, damage = 1, age = 999 }); // 더듬이 상실 → 머리 흉터(사망 아님)
        ah.wounds.Add(new Wound { part = (BodyPart)11, kind = WoundKind.Cut, damage = 5 });             // 다리4 5/6 → 오른다리
        ah.wounds.Add(new Wound { part = (BodyPart)13, kind = WoundKind.Cut, damage = 5 });             // 다리6 5/6 → 오른다리, 합계는 상한 90%
        ah.surgeries.Add(new SurgeryOrder { kind = SurgeryKind.Amputate, part = (BodyPart)4 });          // 더듬이 절단 대기 → 취소
        ah.surgeries.Add(new SurgeryOrder { kind = SurgeryKind.Amputate, part = (BodyPart)6 });          // 오른큰턱 절단 → 오른손 절단 유지
        ant = JsonUtility.FromJson<CommanderPersonalState>(JsonUtility.ToJson(ant)); ah = ant.health;
        BodyMigration.Health(ant);
        Check(Mathf.Approximately(ah.wounds.First(w => w.part == BodyPart.HandL).damage, 2f), "mandible → hand keeps damage ratio");
        Check(ah.wounds.Count(w => w.part == BodyPart.LegL && w.kind == WoundKind.Missing) == 1, "merged leg loss kept once");
        Check(!ah.Fatal && ah.wounds.Any(w => w.part == BodyPart.Head && w.kind == WoundKind.Scar) && !ah.Missing(BodyPart.Head), "antenna loss → head scar, not death");
        Check(ah.wounds.Where(w => w.part == BodyPart.LegR && !w.Permanent).Sum(w => w.damage) <= MedicalRules.MaxHp(BodyPart.LegR) * .9f + .001f && ah.PartFraction(BodyPart.LegR) > 0, "merged damage capped");
        Check(ah.surgeries.Count == 1 && ah.surgeries[0].part == BodyPart.HandR, "antenna amputation cancelled, mandible amputation → hand");
        Check(ah.wounds.All(w => System.Enum.IsDefined(typeof(BodyPart), w.part)), "all parts valid after migration");
        GameMenuController.Instance.ShowHealth(patient); NoOverlap("health"); GameMenuController.Instance.Resume();
    }

    static async Task Frames(int n) { for (var i = 0; i < n; i++) await Task.Yield(); await Task.Delay(30 * n); }
    static float Floor(Resource t) => ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == t).Sum(n => n.AmountRemaining);

    static async Task Hygiene(List<CommanderAnt> list)
    {
        Daytime(); var rm = ResourceManager.Instance;
        foreach (var x in list) x.SetJobEnabled(CommanderJobs.All, false);
        var hauler = list.First(x => !x.IsDead && !x.IsSleepTime && x.CanDoJob(CommanderJobs.Hauling) && x.PersonalHealth > 0);
        // 나무 등 장애물이 우리·문 앞에 걸리면 내부 NavMesh가 끊겨 수거가 간헐 실패한다 → 빈 자리를 고른다
        var o = Enumerable.Range(0, 15).Select(i => new Vector3(Mathf.Floor(hauler.Position.x) - 14 - i % 3 * 10, hauler.Position.y, Mathf.Floor(hauler.Position.z) - 4 + (i / 3 - 2) * 10))
            .First(c => Enumerable.Range(0, 81).Select(k => c + new Vector3(k % 9 + .5f, 0, k / 9 + .5f)).All(p => NavMesh.SamplePosition(p, out var h, 1f, NavMesh.AllAreas)
                && new Vector2(h.position.x - p.x, h.position.z - p.z).sqrMagnitude < .09f && hauler.CanReach(h.position)));
        for (var x = 0; x < 8; x++) for (var z = 0; z < 8; z++)
        {
            if (x != 0 && x != 7 && z != 0 && z != 7) continue;
            var p = o + new Vector3(x + .5f, 0, z + .5f);
            if (x == 7 && z == 3) Build<Door>(BuildingKind.Door, p); else Build<Wall>(BuildingKind.LeafWall, p);
        }
        Vector3 In(float x, float z) => o + new Vector3(x + .5f, 0, z + .5f);
        var pen = Build<Ranch>(BuildingKind.Pen, In(1, 1)); await Frames(3);
        Check(pen.Operating, "test pen operating");
        var aphid = Critter.Spawn(Species.Aphid, In(3, 3), true); await Frames(2);
        typeof(Critter).GetMethod("UpdatePen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(aphid, null);
        Check(aphid.InOperatingPen, "aphid in pen");
        var risk0 = PenHygiene.Risk(pen);
        typeof(Critter).GetMethod("TickExcretion", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(aphid, new object[] { 1.5f });
        Check(PenHygiene.ManureIn(pen) >= 3 && PenHygiene.Risk(pen) > risk0, "animal leaves manure floor items, pen hygiene worsens");
        // 퇴비통이 우선순위 높은 목적지: 운반 장수가 바닥 배설물을 수거하면 위생 원인이 해소된다
        var bin = Build<CompostBin>(BuildingKind.CompostBin, Ground(hauler.Position + Vector3.right * 4)); await Frames(2);
        bin.Bin.priority = 9;
        foreach (var n in ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType != Resource.Manure).ToList()) Object.DestroyImmediate(n.gameObject); // 다른 바닥 화물과 경쟁하지 않게
        hauler.SetJobEnabled(CommanderJobs.Hauling, true); hauler.WorkState.duty = CommanderDuty.Civilian;
        Time.timeScale = 3; await Until(() => PenHygiene.ManureIn(pen) < 1 && bin.RawCount >= 3, 120, "hauler collects manure into bin"); Time.timeScale = 0;
        hauler.SetJobEnabled(CommanderJobs.All, false); hauler.CommandStop();
        Check(bin.RawCount >= 3 && PenHygiene.Risk(pen) == 0, "manure stored in compost bin, hygiene cleared");
        // 퇴비통 안 원료는 소굴 재고가 아니다(식사·제작에 쓰이지 않음)
        var rottenOutside = rm.GetAmount(Resource.RottenFood);
        rm.AddTo(bin.Bin, Resource.RottenFood, 10);
        Check(rm.GetAmount(Resource.RottenFood) == rottenOutside, "compost contents not spendable");
        bin.Data2.target = 999; bin.Tick(.1f);
        Check(bin.Data2.active && bin.Data2.manure + bin.Data2.rotten == CompostBin.Batch, "batch starts without worker");
        var manureFloor = Floor(Resource.Manure); var rottenFloor = Floor(Resource.RottenFood); var batch = (bin.Data2.manure, bin.Data2.rotten);
        bin.Abort();
        Check(!bin.Data2.active && Mathf.Abs(Floor(Resource.Manure) - manureFloor - batch.manure) < .01f && Mathf.Abs(Floor(Resource.RottenFood) - rottenFloor - batch.rotten) < .01f, "abort returns raw amounts to floor");
        rm.AddTo(bin.Bin, Resource.Manure, 10); bin.Tick(.1f); var fert0 = Floor(Resource.Fertilizer);
        bin.Tick(CompostBin.Seconds + 1);
        Check(Floor(Resource.Fertilizer) - fert0 >= CompostBin.Yield - .01f, "compost yields fertilizer on floor");
        bin.Data2.target = 0; bin.Tick(.1f); Check(bin.Bin.allowed == 0, "target reached stops new input");
        bin.Data2.target = 999; bin.Tick(.1f);
        var inBin = bin.Bin.Amount(Resource.RottenFood); var floorR = Floor(Resource.RottenFood);
        bin.SetAllowed(Resource.RottenFood, false);
        Check(bin.Bin.Amount(Resource.RottenFood) == 0 && Mathf.Abs(Floor(Resource.RottenFood) - floorR - inBin) < .01f, "disallow ejects raw to floor");
        GameMenuController.Instance.ShowCompost(bin); NoOverlap("compost"); GameMenuController.Instance.Resume();
    }

    static async Task Death(List<CommanderAnt> list)
    {
        Daytime(); Rich();
        foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.Social.departure = DepartureState.None; x.Social.pendingDeparture = false; } // 앞 단계 굶주림 이탈 정리
        var alive = list.Where(x => !x.IsDead && x.isActiveAndEnabled).ToList();
        var dead = alive[alive.Count - 1]; var friend = alive.First(x => x != dead && !x.IsSleepTime && x.CanDoJob(CommanderJobs.Hauling) && x.PersonalHealth > 0);
        friend.PersonalState.Relation(dead.PersonalState.id).value = 60;
        var gear = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Move };
        dead.PersonalState.equipment.Clear(); dead.PersonalState.equipment.Add(gear);
        var deathPos = dead.Position;
        typeof(CommanderAnt).GetMethod("OnDowned", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dead, new object[] { "검사", true });
        Check(dead.IsDead && Memorial.Find(dead.PersonalState.id)?.cause == "검사", "death recorded with cause");
        Check(Object.FindObjectsByType<EquipmentLoot>().Any(l => l.Items.Contains(gear)), "home death drops equipment at site");
        var body = Tombstone.BodyOf(dead.PersonalState.id);
        Check(body != null && !body.Edible, "commander body left, not food");
        body.Tick(100000); Check(body != null && body.Remains && body.Available, "unburied body decays to remains, not removed");
        // 미안치 시신 목격 기분
        friend.Agent.Warp(body.Position + Vector3.right); Memorial.Tick(friend, 1);
        Check(friend.PersonalState.moodFactors.Any(f => f.reason == "미안치 시신 목격" && f.value < 0), "seeing unburied body hurts mood");
        // 묘비: 등록 → 안치 지시 → 운반 → 안치
        var tomb = Build<Tombstone>(BuildingKind.Tombstone, Ground(friend.Position + Vector3.forward * 5)); await Frames(2);
        var tomb2 = Build<Tombstone>(BuildingKind.Tombstone, Ground(friend.Position + Vector3.forward * 7)); await Frames(2);
        var rec = Memorial.Find(dead.PersonalState.id);
        Check(tomb.Register(rec) && tomb2.RegisterBlock(rec) != "", "one tomb per deceased");
        Check(tomb.OrderBurial(true), "burial order");
        friend.SetJobEnabled(CommanderJobs.Hauling, true); friend.WorkState.duty = CommanderDuty.Civilian;
        Time.timeScale = 3; await Until(() => tomb.Data2.body, 90, "hauler inters body"); Time.timeScale = 0;
        friend.SetJobEnabled(CommanderJobs.All, false); friend.CommandStop();
        Check(Tombstone.BodyOf(dead.PersonalState.id) == null && tomb.Data2.remains, "body moved into tomb (no decay)");
        Check(!Demolition.CanDemolish(tomb), "tomb with body cannot be demolished");
        // 위로: 관계에 따라, 중첩·영구 누적 없음(쿨다운)
        friend.PersonalState.moodFactors.RemoveAll(f => f.reason.StartsWith("추모")); friend.PersonalState.Relation(dead.PersonalState.id).memorialCooldown = 0; // 운반 중 지나침 위로가 이미 발동했을 수 있음
        Check(Memorial.Comfort(friend, tomb) && friend.PersonalState.moodFactors.Count(f => f.reason.StartsWith("추모")) == 1, "passing mourn comfort");
        Check(!Memorial.Comfort(friend, tomb), "no repeated stacking during cooldown");
        Memorial.Tick(friend, Memorial.ComfortCooldown + 1); // 묘비 옆이면 Tick이 지나침 위로를 바로 다시 준다
        Check(friend.PersonalState.Relation(dead.PersonalState.id).memorialCooldown > 0 || Memorial.Comfort(friend, tomb), "comfort available after cooldown");
        // 꺼내기·이장
        var moved = tomb.TakeOut(tomb2);
        Check(moved != null && moved.Remains && tomb.Empty && tomb2.Data2.deceasedId == dead.PersonalState.id && tomb2.Data2.burialOrdered && Demolition.CanDemolish(tomb), "take out & rebury order");
        GameMenuController.Instance.ShowMemorial(tomb2); NoOverlap("memorial"); GameMenuController.Instance.Resume();
    }

    static async Task Prisoners(List<CommanderAnt> list)
    {
        Daytime(); var dm = DiplomacyManager.Instance;
        foreach (var x in list.Where(x => !x.IsDead)) x.SetJobEnabled(CommanderJobs.All, false);
        var alive = list.Where(x => !x.IsDead && x.isActiveAndEnabled && x.PersonalHealth > 0 && !x.IsSleepTime).ToList();
        var doctor = alive.First(x => x.CanDoJob(CommanderJobs.Nursing)); var feeder = alive.First(x => x != doctor && x.CanDoJob(CommanderJobs.Hauling));
        var camp = Build<PrisonerCamp>(BuildingKind.PrisonerCamp, Ground(doctor.Position + Vector3.left * 6)); await Frames(2);
        Check(camp != null && camp.TryCapture("검사 포로", CommanderRank.Sergeant, new[] { UnitRole.Melee }, new CommanderTraits()), "capture prisoner");
        var p = camp.Prisoners.Last(); var civ = dm.Data.civilizations.First(c => !c.extinct); p.PersonalState.originFaction = civ.id;
        p.PersonalState.health ??= new HealthState(); p.PersonalState.health.migrated = true;
        MedicalRules.AddWound(p.PersonalState.health, BodyPart.LegR, WoundKind.Cut, 3);
        doctor.SetJobEnabled(CommanderJobs.Nursing, true); doctor.WorkState.duty = CommanderDuty.Civilian;
        Time.timeScale = 3; await Until(() => p.PersonalState.health.wounds.All(w => w.tended), 60, "doctor tends prisoner at camp"); Time.timeScale = 0;
        doctor.SetJobEnabled(CommanderJobs.All, false); doctor.CommandStop();
        p.PersonalState.meal.satiety = 5; feeder.SetJobEnabled(CommanderJobs.Hauling, true); feeder.WorkState.duty = CommanderDuty.Civilian;
        Time.timeScale = 3; await Until(() => p.PersonalState.meal.satiety > 90, 60, "hauler feeds prisoner"); Time.timeScale = 0;
        feeder.SetJobEnabled(CommanderJobs.All, false); feeder.CommandStop();
        // 이동 불가 포로는 탈출하지 않는다
        typeof(PrisonerCamp).GetField("escapeChance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(camp, 1f);
        foreach (var leg in new[] { BodyPart.LegL, BodyPart.LegR }) MedicalRules.AddWound(p.PersonalState.health, leg, WoundKind.Missing, 1).age = 999;
        Check(!PrisonerCamp.CanMove(p), "prisoner cannot move");
        for (var i = 0; i < 5; i++) camp.Tick(31);
        Check(camp.Prisoners.Contains(p), "immobile prisoner does not escape");
        // 석방: 게임 3개월 + 실제 거리 × 계수(연속 비례)
        var sites = WorldMapManager.Instance.Sites.Take(2).ToList();
        if (sites.Count == 2)
        {
            float D(ExpeditionSite s) => Vector2.Distance(s.MapPosition, new Vector2(.5f, .5f));
            var extra0 = dm.HomecomingSeconds(sites[0]) - DiplomacyManager.HomecomingBaseSeconds; var extra1 = dm.HomecomingSeconds(sites[1]) - DiplomacyManager.HomecomingBaseSeconds;
            Check(Mathf.Abs(extra0 - D(sites[0]) * DiplomacyManager.HomecomingDistanceSeconds) < .5f && Mathf.Abs(extra1 - D(sites[1]) * DiplomacyManager.HomecomingDistanceSeconds) < .5f, "return time = 3 months + distance × factor");
        }
        Check(Mathf.Approximately(DiplomacyManager.HomecomingBaseSeconds, 3 * GameCalendar.SecondsPerMonth), "base 3 months (15 real minutes at 1x)");
        var before = dm.Data.homecomings.Count;
        Check(camp.ReleaseFree(p) && dm.Data.homecomings.Count == before + 1 && !camp.Prisoners.Contains(p), "free release starts homecoming");
        var h = dm.Data.homecomings.Last();
        Check(h.gear.Count == 0 && h.arrive - dm.Data.elapsed >= DiplomacyManager.HomecomingBaseSeconds - .5f, "released enemy returns safely after travel time");
        dm.Tick(1); Check(dm.Data.homecomings.Contains(h), "not arrived early");
        GameMenuController.Instance.ShowPrisoners(camp); NoOverlap("prisoners"); GameMenuController.Instance.Resume();
    }

    static async Task Research(List<CommanderAnt> list)
    {
        var r = CampaignResearch.Instance; var a = list[0]; var b = list[1];
        foreach (var x in new[] { a, b }) { x.SetJobEnabled(CommanderJobs.Research, true); x.WorkState.duty = CommanderDuty.Civilian; }
        var lab1 = Lab(a.Position + Vector3.forward * 4); var lab2 = Lab(b.Position + Vector3.back * 4);
        Check(lab1.TryAssign(a), "assign a"); Check(!lab2.TryAssign(a), "same commander not in two labs");
        Check(lab2.TryAssign(b), "assign b");
        // 시대 벽: 1등급 연구소만으로는 2시대 연구 불가.
        Check(r.BlockReason(ScienceTechnology.Vehicle) != "" && !r.TryStart(ScienceTechnology.Vehicle), "tier wall blocks era 2");
        var food0 = ResourceManager.Instance.GetAmount(Resource.Food);
        Check(r.TryStart(ScienceTechnology.FungalFarming), "start research");
        var paid = food0 - ResourceManager.Instance.GetAmount(Resource.Food);
        Check(paid > 0, "first start pays cost");
        r.Tick(2); var two = r.Progress;
        var expected = 2 * (a.WorkRate(CommanderActivity.Research) + b.WorkRate(CommanderActivity.Research));
        Check(Mathf.Abs(two - expected) < .05f, $"two labs contribute ({two} vs {expected})");
        Check(r.LabWaitReason(lab1) == "" && r.LabWaitReason(lab2) == "", "labs contributing");
        r.SetPaused(true); r.Tick(5); Check(r.Progress == two && r.LabWaitReason(lab1) == "연구 정지됨", "pause stops progress");
        r.SetPaused(false);
        // 변경: 이전 진행 보존, 돌아올 때 비용 재차감 없음.
        Check(r.TryStart(ScienceTechnology.Resin) && Mathf.Approximately(r.SavedProgress(ScienceTechnology.FungalFarming), two) && r.Progress == 0, "switch preserves progress");
        var food1 = ResourceManager.Instance.GetAmount(Resource.Food);
        Check(r.TryStart(ScienceTechnology.FungalFarming) && Mathf.Approximately(r.Progress, two), "switch back resumes");
        Check(ResourceManager.Instance.GetAmount(Resource.Food) == food1, "no double charge on resume");
        Check(r.SavedProgress(ScienceTechnology.Resin) == 0, "resin kept as paid partial");
        // 저장 왕복(연구 상태)
        var json = JsonUtility.ToJson(r.CaptureState()); var st = JsonUtility.FromJson<CampaignResearch.State>(json);
        Check(CampaignResearch.Validate(st, out var err), "state valid " + err);
        r.RestoreState(st); Check(Mathf.Approximately(r.Progress, two) && r.SavedProgress(ScienceTechnology.Resin) == 0, "state roundtrip");
        // 담당 없음 대기 사유
        lab2.ReleaseResearcher(); Check(r.LabWaitReason(lab2) == "담당 장수 없음", "missing researcher reason");
        // 완료 후 자동 선택 없음
        r.Tick(100000); Check(r.Has(ScienceTechnology.FungalFarming) && r.Active == null, "complete without auto next");
        r.Tick(100); Check(r.Active == null && !r.Has(ScienceTechnology.Resin), "no auto research after completion");
        // 실제 화면: 열기·겹침 없음·선택·시작
        var menu = GameMenuController.Instance; menu.Science(); await Task.Yield();
        NoOverlap("research");
        var btn = Object.FindObjectsByType<UnityEngine.UI.Button>().FirstOrDefault(x => x.GetComponentInChildren<UnityEngine.UI.Text>()?.text.StartsWith("흙벽 공법") == true);
        Check(btn != null, "research list button"); btn.onClick.Invoke(); await Task.Yield();
        var start = Object.FindObjectsByType<UnityEngine.UI.Button>().FirstOrDefault(x => x.GetComponentInChildren<UnityEngine.UI.Text>()?.text == "이 연구 시작");
        Check(start != null && start.interactable, "start button for resin");
        start.onClick.Invoke(); await Task.Yield();
        Check(r.Active?.Technology == ScienceTechnology.Resin && Mathf.Approximately(r.Progress, 0), "UI resumes paid partial");
        menu.Resume(); lab1.ReleaseResearcher();
    }
}
