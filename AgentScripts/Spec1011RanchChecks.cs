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
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// 2026-10-11 목장 개편: 구역형 우리(벽으로 두른 방)·합사·과밀·뚫림, 개체(나이·배고픔·길들임), 먹이통 창고·직접 섭식,
// 포획·결박·운반, 돌봄·길들이기, 번식(야생 새끼), 도축→도축대→해체, 포식, 투약·수술, 방생, 위험 작업 지정, 작물 섭식, 저장·옛 저장 이관.
public static class Spec1011RanchChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var t = Template(k); Check(t != null, "template " + k);
        var go = Object.Instantiate(t, new Vector3(p.x, p.y + t.transform.localScale.y * .5f, p.z), Quaternion.identity); go.name = k.ToString(); go.SetActive(true);
        RoomSystem.MarkDirty(); return go.GetComponent<T>();
    }
    static async Task<bool> Until(Func<bool> done, int seconds)
    {
        Time.timeScale = 1;
        for (int i = 0; i < seconds * 10 && !done(); i++) await Task.Delay(100);
        Time.timeScale = 0; return done();
    }
    static async Task Frames(int n) { Time.timeScale = 1; for (var i = 0; i < n; i++) await Task.Yield(); Time.timeScale = 0; }
    static float Month => GameCalendar.SecondsPerMonth;
    static Critter.State S(Critter c) => (Critter.State)typeof(Critter).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c);
    static void Life(Critter c, float seconds) => typeof(Critter).GetMethod("TickLife", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void Behave(Critter c, float seconds) => typeof(Critter).GetMethod("TickBehaviour", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void Pen(Critter c) => typeof(Critter).GetMethod("UpdatePen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null);

    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1011R-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var rm = ResourceManager.Instance; foreach (Resource t in Enum.GetValues(typeof(Resource))) rm.AddCapacity(t, 5000);
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var keeper = list[0]; keeper.Talents.levels[(int)CommanderActivity.Husbandry] = 20; keeper.Talents.levels[(int)CommanderActivity.Medicine] = 20;
            var novice = list[1]; novice.Talents.levels[(int)CommanderActivity.Husbandry] = 0; novice.Talents.levels[(int)CommanderActivity.Medicine] = 0;
            var home = Object.FindAnyObjectByType<Stockpile>().Position;

            // 1. 야생 개체: 개체 단위, 평소 온순(적·함정 대상 아님), 관리 대상 아님.
            var wild = Critter.All.Where(c => c != null).ToList();
            Check(wild.Count == 7 && wild.All(c => !c.Tame && !c.Managed && c.Monster.Docile && !CombatTargeting.CanAttack(UnitRole.Ranged, c.Monster)), "wild critters docile and unmanaged");

            // 2. 우리 = 우리 표지를 둔 벽·문으로 두른 방(지붕 불필요). 6×6 칸.
            var o = new Vector3(Mathf.Floor(keeper.Position.x) + 6, home.y, Mathf.Floor(keeper.Position.z) + 6);
            var walls = new List<BuildingBase>();
            for (var x = 0; x < 8; x++) for (var z = 0; z < 8; z++)
            {
                if (x != 0 && x != 7 && z != 0 && z != 7) continue;
                var p = o + new Vector3(x + .5f, 0, z + .5f);
                walls.Add(x == 0 && z == 3 ? (BuildingBase)Put<Door>(BuildingKind.Door, p) : Put<Wall>(BuildingKind.LeafWall, p));
            }
            Vector3 In(float x, float z) => o + new Vector3(x + .5f, 0, z + .5f);
            var ranch = Put<Ranch>(BuildingKind.Pen, In(1, 1));
            var feeder = Put<RanchFacility>(BuildingKind.Feeder, In(6, 1));
            var station = Put<RanchFacility>(BuildingKind.CareStation, In(1, 6));
            var clinic = Put<RanchFacility>(BuildingKind.AnimalClinic, In(6, 6));
            await Frames(3);
            Check(ranch.Operating && ranch.Cells == 36 && RoomSystem.KindOf(feeder) == RoomKind.Fishery && ranch.Room.Kind == RoomKind.Fishery, "walled room with marker is an operating pen: " + ranch.Cells);

            // 3. 합사·마릿수 무제한·과밀 효율. 우리 안에 들어온 개체는 즉시 등록(스스로 들어와도).
            var a1 = Critter.Spawn(Species.Aphid, In(3, 3), true); var a2 = Critter.Spawn(Species.Aphid, In(4, 3), true);
            var bee = Critter.Spawn(Species.Bee, In(3, 4), false);
            await Frames(3); foreach (var c in new[] { a1, a2, bee }) Pen(c);
            Check(a1.Pen == ranch && bee.Pen == ranch && ranch.Animals.Count() == 3 && ranch.Efficiency == 1, "mixed species registered, no crowding");
            var crowd = Enumerable.Range(0, 12).Select(i => Critter.Spawn(Species.Spider, In(2 + i % 4, 2 + i / 4), true)).ToList();
            await Frames(2); foreach (var c in crowd) Pen(c);
            Check(ranch.Crowding > 1 && ranch.Efficiency < 1 && ranch.Animals.Count() == 15, "overcrowding lowers efficiency but accepts animals");
            foreach (var c in crowd) Object.DestroyImmediate(c.gameObject);
            Check(ranch.Efficiency == 1, "crowding recalculated");

            // 4. 먹이통 = 창고: 운반 장수가 허용 먹이를 목표량까지. 귀한 먹이는 기본 제외. 허용 해제하면 바닥에 쏟는다.
            Check(feeder.Allowed(Resource.Leaf) && !feeder.Allowed(Resource.Honeydew) && feeder.Target(Resource.Honeydew) == 0, "precious feed excluded by default");
            feeder.SetTarget(Resource.Food, 0); rm.Add(Resource.Leaf, 60);
            keeper.SetJobEnabled(CommanderJobs.Hauling, true); keeper.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.FillFeeder && feeder.Incoming(Resource.Leaf) > 0, "hauler fills feeder (reserved in transit)");
            var trace = new System.Text.StringBuilder(); var lastJob = "";
            var ok = await Until(() => { var j = keeper.AnimalJob + "/" + typeof(CommanderAnt).GetField("feedLoaded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(keeper) + "/" + keeper.CivilianWorkReady;
                if (j != lastJob) { lastJob = j; trace.Append($"[{Time.time:0.0} {j} p{keeper.Position} inc{feeder.Incoming(Resource.Leaf)} st{feeder.Stock(Resource.Leaf)}]"); } return feeder.Stock(Resource.Leaf) >= 20; }, 60);
            Check(ok, "feeder stocked to target: " + trace + " feeder " + feeder.Position + " leaf " + rm.GetAmount(Resource.Leaf));
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Hauling, false);
            feeder.SetTarget(Resource.Leaf, 10);
            Check(feeder.Stock(Resource.Leaf) == 10 && ResourceNode.Available.Any(n => n.IsLooseCargo && n.ResourceType == Resource.Leaf && (n.transform.position - feeder.Position).sqrMagnitude < 25), "lower target spills extra on floor");

            // 5. 직접 섭식: 배고프면 접근 가능한 가장 가까운 먹이로 가서 먹는다. 벽 너머 먹이는 접근 불가.
            S(a1).hunger = 10; var outsideLeaf = new GameObject("OutsideLeaf"); outsideLeaf.SetActive(false); outsideLeaf.transform.position = o + new Vector3(-3, 0, -3);
            outsideLeaf.AddComponent<ResourceNode>().ConfigureLoot(Resource.Leaf, 5); outsideLeaf.SetActive(true);
            Check(!a1.Reachable(outsideLeaf.transform.position), "food beyond walls unreachable");
            Check(await Until(() => a1.Hunger > 60, 60), "hungry aphid eats feeder/floor food: " + a1.Hunger);

            // 6. 생산: 먹이·환경을 갖추면 장수 없이 쌓인다. 직접 채취 물건은 한도까지. 바닥 생산물은 바닥에.
            void Fed(Critter c, float months) { var age0 = S(c).age; for (var i = 0; i < months * 10; i++) { S(c).hunger = 100; Life(c, Month * .1f); S(c).age = age0; } } // 나이는 고정(노령사 방지)
            S(a2).hunger = 90; var d0 = a1.DirectStock; Fed(a1, 1);
            Check(a1.DirectStock > d0 + 2 && a1.DirectStock <= GameBalance.CritterDirectCap, "direct product accumulates to cap: " + a1.DirectStock);
            Fed(a1, 3); Check(Mathf.Approximately(a1.DirectStock, GameBalance.CritterDirectCap), "production stops at cap");

            // 7. 직접 채취(사육, 길들인 개체는 안전): 물건은 바닥에 두고 운반 작업이 옮긴다.
            keeper.SetJobEnabled(CommanderJobs.Husbandry, true); ranch.AutoHarvest = true;
            var floorHoney = ResourceNode.Available.Count(n => n.IsLooseCargo && n.ResourceType == Resource.Honeydew);
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.Harvest || keeper.AnimalJob == AnimalTask.Care, "keeper takes ranch work: " + keeper.AnimalJob);
            Check(await Until(() => ResourceNode.Available.Count(n => n.IsLooseCargo && n.ResourceType == Resource.Honeydew) > floorHoney, 60), "harvested honeydew dropped on floor");
            keeper.CommandStop();

            // 8. 돌봄·길들이기: 돌봄대에서 한 마리씩 호출. 돌봄 효과 동안 진행도, 끊기면 감소, 완료 후 영구.
            ranch.AutoHarvest = false; S(bee).care = 0; S(bee).hunger = 90; await Frames(2);
            Check(ranch.NeedsCare(bee) && Ranch.MayHandle(bee, keeper), "wild bee needs care, keeper may handle: " + ranch.NeedsCare(bee) + Ranch.MayHandle(bee, keeper) + " h" + bee.Handler + " ag" + bee.Agitated + " op" + bee.InOperatingPen + " bl" + bee.CareBlocked);
            S(bee).hunger = 90; keeper.TickDuty(2);
            Check(await Until(() => bee.CareRemaining > 0 || bee.CareBlocked, 90), "bee cared");
            keeper.CommandStop();
            if (bee.CareBlocked) { bee.CareBlocked = false; S(bee).care = 60; }
            var tp = bee.TameProgress; Life(bee, Month * .1f); Check(bee.TameProgress > tp, "care raises taming");
            S(bee).care = 0; tp = bee.TameProgress; Life(bee, Month * .1f); Check(bee.TameProgress < tp, "taming decays without care");
            S(bee).care = 9999; Fed(bee, 1.1f); Check(bee.Tame, "bee tamed permanently");
            S(bee).care = 0; S(bee).hunger = 0; Life(bee, Month * .2f); Check(bee.Tame, "starvation does not untame");
            S(bee).hunger = 90; bee.Monster.RestoreHealth(bee.Info.health);
            // 길들이기 불가종: 돌봄 효과는 받지만 진행도 없음.
            var spider = Critter.Spawn(Species.Spider, In(5, 5), false); await Frames(2); Pen(spider);
            S(spider).care = 9999; Fed(spider, 1); Check(!spider.Tame && spider.TameProgress == 0, "untameable gets no taming progress");

            // 9. 번식: 성체 2+가 배부르면 자동, 목표와 무관. 새끼는 야생으로 태어나 생산·번식 불가.
            ranch.Setting(Species.Aphid).target = 1; var aphids = ranch.CountOf(Species.Aphid);
            S(a1).age = S(a2).age = 2; S(a1).hunger = S(a2).hunger = 100; ranch.Tick(Month * 4.2f);
            var babies = ranch.Animals.Where(c => c.Species == Species.Aphid && c.Stage == CritterStage.Baby).ToList();
            Check(ranch.CountOf(Species.Aphid) > aphids && babies.Count > 0 && babies.All(b => !b.Tame), "breeding past target, babies wild: " + aphids + "->" + ranch.CountOf(Species.Aphid) + " babies " + babies.Count + " | " + string.Join(";", Critter.All.Where(c => c != null && c.Species == Species.Aphid).Select(c => $"{c.Pen?.Key} {c.transform.position} h{c.Hunger:0} hp{c.HealthFraction:0.00} age{c.Age:0.0} room{RoomSystem.RoomAt(c.transform.position) == ranch.Room}")) + " a2null " + (a2 == null));
            var baby = babies[0]; S(baby).hunger = 90; Life(baby, Month * .2f); Check(baby.DirectStock == 0, "babies do not produce");
            S(baby).hunger = 0; var age = baby.Age; Life(baby, Month * .2f); Check(baby.Age == age, "starving babies do not grow");
            S(baby).hunger = 90;

            // 10. 뚫린 우리: 벽 하나를 부수면 먹기 외 운영 중단, 설정·소속 보존. 다시 막으면 재개.
            var wallPos = walls[5].Position; Object.DestroyImmediate(walls[5].gameObject); RoomSystem.MarkDirty(); await Frames(2);
            Check(!ranch.Operating && a1.Pen == ranch, "breached pen keeps members");
            S(a1).care = 50; S(a1).direct = 0; Fed(a1, 1);
            Check(a1.CareRemaining == 0 && a1.DirectStock == 0 && ranch.HusbandryTasks(keeper).Count() == 0, "breached pen stops care, production and tasks");
            walls[5] = Put<Wall>(BuildingKind.LeafWall, wallPos); await Frames(2);
            Check(ranch.Operating, "rewalled pen operates again");

            // 11. 포획: 제압 없이 붙잡아 현장 결박. 유저가 목적 우리를 지정하면 운반(장수 한 명당 한 마리), 도착하면 결박 해제.
            var wildAphid = wild.First(c => c != null && c.Species == Species.Aphid);
            wildAphid.Monster.Agent.Warp(keeper.Position + new Vector3(-4, 0, -4)); S(wildAphid).home = new Vec3Dto(wildAphid.transform.position);
            foreach (Species sp in Enum.GetValues(typeof(Species))) ranch.Setting(sp).autoCare = false; // 돌봄이 포획보다 먼저 잡히지 않게
            keeper.CommandStop(); wildAphid.CaptureDesignated = true; keeper.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.Capture, "keeper captures designated critter: " + keeper.AnimalJob);
            Check(await Until(() => wildAphid.Bound, 60), "captured critter bound on site");
            Check(!wildAphid.CaptureDesignated && wildAphid.TransportTo == null, "capture clears designation, waits for destination");
            wildAphid.TransportTo = ranch; keeper.SetJobEnabled(CommanderJobs.Husbandry, false); keeper.SetJobEnabled(CommanderJobs.Hauling, true); feeder.SetTarget(Resource.Leaf, 0);
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.Carry, "hauler carries bound critter: " + keeper.AnimalJob);
            Check(await Until(() => wildAphid.Pen == ranch && !wildAphid.Bound && wildAphid.Carrier == null, 90), "critter delivered into pen, unbound");
            keeper.CommandStop();
            // 결박 만료: 조용히 풀리고 운반 요청 취소.
            var loose = Critter.Spawn(Species.Silkworm, keeper.Position + new Vector3(-6, 0, 3), false); await Frames(2);
            typeof(Critter).GetMethod("Bind", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(loose, null); loose.TransportTo = ranch;
            Life(loose, GameBalance.CritterBindSeconds + 1); Check(!loose.Bound && loose.TransportTo == null, "binding expires silently, request cancelled");

            // 12. 위험: 사육 0 장수에게 야생 거미는 '위험' → 자동 금지, 대기 알림, 유저가 고른 장수만.
            Check(SpeciesInfo.Risk(SpeciesInfo.FailChance(Species.Spider, false, novice)) == HandlingRisk.Danger && !Ranch.MayHandle(spider, novice), "danger blocks auto handling");
            Check(Ranch.MayHandle(spider, keeper), "skilled keeper handles spider");
            novice.SetJobEnabled(CommanderJobs.Husbandry, true); keeper.SetJobEnabled(CommanderJobs.Husbandry, false);
            ranch.Setting(Species.Spider).autoCare = true; S(spider).care = 0; S(spider).hunger = 90; spider.CareBlocked = false;
            Check(ranch.AwaitingManual(spider), "dangerous care waits for manual pick: " + ranch.NeedsCare(spider) + (ranch.CareStationFor(spider) != null) + " h" + spider.Handler + " op" + spider.InOperatingPen);
            spider.ApprovedId = novice.PersonalState.id; Check(Ranch.MayHandle(spider, novice) && !ranch.AwaitingManual(spider), "approved commander may handle");
            spider.ApprovedId = ""; novice.SetJobEnabled(CommanderJobs.Husbandry, false);
            // 취급 실패: 성향대로 행동(적 판정), 진정하면 다시 온순.
            spider.Agitate(novice);
            Check(!spider.Monster.Docile && CombatTargeting.CanAttack(UnitRole.Ranged, spider.Monster), "agitated critter acts per temper");
            Behave(spider, GameBalance.CritterAgitatedSeconds + 1); Check(spider.Monster.Docile && !spider.Agitated, "critter calms down");

            // 13. 도축: 목표 초과분은 나이 많은 성체부터(새끼·도축 금지 제외). 현장 도축 → 사체 → 도축대 → 요리 장수 해체.
            var table = Put<RanchFacility>(BuildingKind.ButcherTable, home + new Vector3(4, 0, -4)); await Frames(2);
            var st = ranch.Setting(Species.Aphid); st.autoSlaughter = true; st.target = 1;
            S(a2).age = 5; a1.ProtectSlaughter = true;
            Check(ranch.SlaughterCandidate(Species.Aphid) == a2, "oldest unprotected adult chosen");
            var carcasses = CritterCarcass.All.Count; keeper.SetJobEnabled(CommanderJobs.Husbandry, true); keeper.SetJobEnabled(CommanderJobs.Hauling, false);
            keeper.TickDuty(2);
            Check(await Until(() => a2 == null || a2.Monster.IsDead, 60), "keeper slaughtered on site: " + keeper.AnimalJob);
            Check(CritterCarcass.All.Count == carcasses + 1, "slaughter leaves carcass");
            st.autoSlaughter = false; keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Husbandry, false); keeper.SetJobEnabled(CommanderJobs.Hauling, true);
            foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo).ToList()) n.gameObject.SetActive(false); // 다른 바닥 물건 운반이 먼저 잡히지 않게
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.HaulCarcass, "hauler takes carcass to butcher table: " + keeper.AnimalJob + " carcasses " + CritterCarcass.All.Count + " table " + table.Usable);
            Check(await Until(() => table.Carcasses == 1, 90), "carcass at butcher table");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Hauling, false); keeper.SetJobEnabled(CommanderJobs.Cooking, true);
            var food = rm.GetAmount(Resource.Food); var chitin = rm.GetAmount(Resource.Chitin);
            keeper.TickDuty(2); Check(await Until(() => table.Carcasses == 0, 60), "cook butchers carcass");
            Check(rm.GetAmount(Resource.Food) > food && rm.GetAmount(Resource.Chitin) > chitin, "butchering yields food and chitin");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Cooking, false);

            // 14. 포식: 먹을 게 없는 배고픈 거미가 합사한 진딧물을 사냥, 처음 먹을 때 한 번 알림.
            feeder.DumpAll(Resource.Leaf); foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo).ToList()) n.gameObject.SetActive(false);
            S(spider).hunger = 5; a1.ProtectSlaughter = false;
            Check(await Until(() => spider.Hunger > 20, 90), "spider hunted and ate prey: " + spider.Hunger);
            Check(CritterCarcass.All.Any(k => k.Announced) || AntColony.UI.ToastManager.VisibleLines.Any(l => l.Contains("잡아먹혔습니다")), "predation notice once: " + string.Join(",", CritterCarcass.All.Select(k => k.Species + ":" + k.Meat)));
            S(spider).hunger = 100;

            // 15. 치료: 경상은 동물용 의약품 투약(간호 작업), 중상은 치료대 수술(키트 1개, 완료 순간 완치) 후 원래 우리로.
            var patient = Critter.Spawn(Species.Aphid, In(2, 5), true); await Frames(2); Pen(patient); S(patient).hunger = 100;
            Object.DestroyImmediate(spider.gameObject); // 수술 중 포식 방지
            patient.Monster.Wound(patient.Info.health * .3f); rm.Add(Resource.AnimalMedicine, 3);
            keeper.SetJobEnabled(CommanderJobs.Nursing, true); keeper.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.Dose, "nurse doses light injury: " + keeper.AnimalJob);
            Check(await Until(() => patient.Medicated > 0, 60), "medicated");
            keeper.CommandStop();
            patient.Monster.Wound(patient.Monster.CurrentHealth - patient.Info.health * .3f); rm.Add(Resource.SurgeryKit, 1);
            Check(patient.HeavyInjury, "heavy injury");
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.Surgery, "surgeon takes heavy injury: " + keeper.AnimalJob);
            Check(await Until(() => !patient.HeavyInjury && patient.HealthFraction > .99f, 120), "surgery heals fully");
            Check(rm.GetAmount(Resource.SurgeryKit) == 0 && patient.TransportTo == ranch, "kit used, returns to pen");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Nursing, false); keeper.SetJobEnabled(CommanderJobs.Hauling, true);
            Check(await Until(() => patient.Clinic == null && patient.Pen == ranch && patient.InOperatingPen, 90), "patient back in pen");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Hauling, false);

            // 16. 작물 섭식: 다 자란 작물을 먹으면 수확량이 줄고, 다 먹히면 다시 파종해야 한다.
            var farm = Put<ResourceNode>(BuildingKind.Farm, home + new Vector3(-6, 0, 6)); await Frames(2);
            typeof(ResourceNode).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(farm, new object[] { 3f, 0f, 0f });
            Check(farm.CropEdible && farm.EatCrop(1) && farm.AmountRemaining < 3, "animals eat ripe crops");
            farm.EatCrop(5); Check(farm.NeedsSowing && !farm.CropEdible, "fully eaten crop needs resowing");

            // 17. 방생: 확인 후 즉시 사라짐(사체 없음). 우리를 철거해도 생물은 남는다.
            carcasses = CritterCarcass.All.Count; var gone = Critter.All.Count(c => c != null);
            patient.Release(); await Frames(2);
            Check(Critter.All.Count(c => c != null) == gone - 1 && CritterCarcass.All.Count == carcasses, "release removes without carcass");
            Check(Demolition.CanDemolish(ranch), "pen with animals can be demolished");

            // 18. 저장: 우리 설정·먹이통·개체 상태(길들임·나이·소속)·사체.
            foreach (var x in list) x.CommandStop(); await Frames(2);
            st.target = 7; feeder.SetTarget(Resource.Fiber, 15); feeder.Priority = 5;
            var tameBee = bee; var beeAge = tameBee.Age; var penKey = ranch.Key; var members = ranch.Animals.Count(); var total = Critter.All.Count(c => c != null);
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready(); await Frames(5);
            var r2 = Ranch.Find(penKey); var f2 = RanchFacility.All.First(f => f.IsFeeder);
            Check(r2 != null && r2.Setting(Species.Aphid).target == 7 && f2.Target(Resource.Fiber) == 15 && f2.Priority == 5, "ranch and feeder settings survive load");
            Check(Critter.All.Count(c => c != null) == total && r2.Animals.Count() == members, "critters and membership survive load");
            Check(Critter.All.Any(c => c != null && c.Species == Species.Bee && c.Tame && Mathf.Abs(c.Age - beeAge) < .01f), "tame bee age survives");

            // 19. 옛 저장(v16 한 종·마릿수 우리) → 길들인 성체 개체로 풀어 놓는다.
            var before = Critter.All.Count(c => c != null);
            typeof(Ranch).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r2, new object[] { new Ranch.State { species = (int)Species.Silkworm, count = 3 } }); await Frames(3);
            Check(Critter.All.Count(c => c != null) == before + 3 && Critter.All.Count(c => c != null && c.Species == Species.Silkworm && c.Tame) >= 3, "legacy pen migrated to individuals");
            return "PASS " + checks + " Spec1011Ranch checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
