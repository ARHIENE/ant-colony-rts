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
            Check(await Until(() => wildAphid.Bound, 60), $"captured critter bound on site (job {keeper.AnimalJob}, designated {wildAphid.CaptureDesignated}, dist {Vector3.Distance(keeper.Position, wildAphid.transform.position):0.0}, satiety {keeper.Satiety:0}, eating {keeper.IsEating}, moving {keeper.Agent.hasPath})");
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

            // 13. 도축(2026-10-11 수정): 자동 도축·도축 금지 없음. 유저가 개체를 요청한 경우에만 현장 도축 → 사체 → 도축대(등록 순서·허용 종·일시정지) → 요리 장수 해체.
            var table = Put<RanchFacility>(BuildingKind.ButcherTable, home + new Vector3(4, 0, -4)); await Frames(2);
            var st = ranch.Setting(Species.Aphid); st.target = 1; ranch.Setting(Species.Spider).autoCare = false; // 12단계에서 켠 돌봄이 먼저 잡히지 않게
            keeper.SetJobEnabled(CommanderJobs.Husbandry, true); keeper.SetJobEnabled(CommanderJobs.Hauling, false); keeper.CommandStop();
            Check(ranch.CountOf(Species.Aphid) > st.target, "aphids over target");
            keeper.TickDuty(2); Check(keeper.AnimalJob != AnimalTask.Slaughter, "no automatic slaughter over target: " + keeper.AnimalJob);
            keeper.CommandStop();
            Check(a2.RequestSlaughter() && a2.SlaughterOrdered && !a2.RequestSlaughter(), "manual slaughter request (once)");
            Check(a2.CancelSlaughter() && !a2.SlaughterOrdered, "cancel before start");
            a2.RequestSlaughter();
            var carcasses = CritterCarcass.All.Count;
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.Slaughter, "keeper takes requested slaughter: " + keeper.AnimalJob);
            Check(await Until(() => a2 == null || a2.Monster.IsDead, 60), "keeper slaughtered on site: " + keeper.AnimalJob);
            Check(CritterCarcass.All.Count == carcasses + 1, "slaughter leaves carcass");
            // 강제 중단: 실제 도축 시작 후에는 유저 취소 불가, 장수가 멈추면 부분 피해만 입고 생존·요청 유지(재작업은 처음부터).
            S(a1).hunger = 100; a1.Monster.RestoreHealth(a1.Info.health); a1.RequestSlaughter(); keeper.CommandStop(); keeper.TickDuty(2);
            Check(await Until(() => a1.SlaughterStarted, 30), "slaughter started");
            Check(!a1.CancelSlaughter() && a1.SlaughterOrdered, "started slaughter cannot be cancelled");
            keeper.CommandStop();
            Check(a1 != null && !a1.Monster.IsDead && !a1.SlaughterStarted && a1.SlaughterOrdered && a1.HealthFraction < .999f, "interrupted slaughter: partial damage, survives: " + a1.HealthFraction);
            Check(a1.CancelSlaughter() && !a1.SlaughterOrdered, "cancel after interruption");
            keeper.SetJobEnabled(CommanderJobs.Husbandry, false); a1.Monster.RestoreHealth(a1.Info.health);
            // 새끼 사체는 성체 산출의 절반(사용자 확정 잠정값).
            var babyMeat = Critter.Spawn(Species.Silkworm, In(5, 2), false, 0); var babyInfo = babyMeat.Info; await Frames(1);
            typeof(Critter).GetMethod("Die", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(babyMeat, new object[] { "test" }); await Frames(2);
            var babyCarcass = CritterCarcass.All.Last();
            Check(Mathf.Approximately(babyCarcass.Meat, babyInfo.meat * GameBalance.CritterBabyYield), "baby carcass yields half: " + babyCarcass.Meat);
            Object.DestroyImmediate(babyCarcass.gameObject);
            // 도축대 일시정지: 새 운반 없음 → 재개하면 운반.
            keeper.SetJobEnabled(CommanderJobs.Hauling, true);
            foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo).ToList()) n.gameObject.SetActive(false); // 다른 바닥 물건 운반이 먼저 잡히지 않게
            table.Paused = true; keeper.TickDuty(2); Check(keeper.AnimalJob != AnimalTask.HaulCarcass, "paused table gets no new hauling");
            table.Paused = false; keeper.CommandStop();
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.HaulCarcass, "hauler takes carcass to butcher table: " + keeper.AnimalJob + " carcasses " + CritterCarcass.All.Count + " table " + table.Usable);
            Check(await Until(() => table.Carcasses == 1, 90), "carcass at butcher table");
            keeper.CommandStop();
            // 부패: 대기열로 옮겨도 남은 부패 시간을 보존하고, 일시정지 중에도 줄어든다.
            var decay0 = table.Queue[0].decay; Check(decay0 > 0 && decay0 < Corpse.Lifetime, "carcass keeps its remaining decay time on table: " + decay0);
            table.Paused = true; table.TickDecay(10); Check(Mathf.Abs(table.Queue[0].decay - (decay0 - 10)) < .01f, "decay continues while paused"); table.Paused = false;
            decay0 = table.Queue[0].decay;
            // 허용 종 해제: 대기 사체는 옆 바닥에 사체로 내려놓는다(남은 부패 시간 유지). 다시 허용하면 운반.
            var onFloor = CritterCarcass.All.Count; table.ToggleSpecies(Species.Aphid);
            var droppedCarcass = CritterCarcass.All.Last(); Check(Mathf.Abs(droppedCarcass.Corpse.Data.remaining - decay0) < .5f, "dropped carcass keeps remaining decay: " + droppedCarcass.Corpse.Data.remaining + " vs " + decay0);
            Check(table.Carcasses == 0 && CritterCarcass.All.Count == onFloor + 1 && !table.AllowsSpecies(Species.Aphid), "disallowed species dropped as carcass");
            keeper.TickDuty(2); Check(keeper.AnimalJob != AnimalTask.HaulCarcass, "disallowed species not hauled");
            table.ToggleSpecies(Species.Aphid); keeper.CommandStop(); keeper.TickDuty(2);
            Check(await Until(() => table.Carcasses == 1, 90), "re-allowed carcass hauled again");
            var entryMeat = table.Queue[0].meat;
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Hauling, false); keeper.SetJobEnabled(CommanderJobs.Cooking, true);
            int Floor(Resource t) => ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == t && (n.transform.position - table.Position).sqrMagnitude < 16).Sum(n => Mathf.RoundToInt(n.AmountRemaining));
            var food = Floor(Resource.Food);
            // 일시정지하면 해체 진행도 보존, 재개하면 이어서.
            keeper.TickDuty(2); Check(await Until(() => table.ButcherProgress > 0, 30), "butchering progresses");
            table.Paused = true; keeper.TickDuty(1); var kept = table.ButcherProgress;
            Check(!table.NeedsButcher && kept > 0 && table.Carcasses == 1, "pause keeps progress and carcass");
            table.Paused = false; keeper.CommandStop(); keeper.TickDuty(2);
            Check(await Until(() => table.Carcasses == 0, 60), "cook butchers carcass after resume");
            Check(Floor(Resource.Food) >= food + Mathf.FloorToInt(entryMeat), "per-carcass yield dropped beside table: " + food + "->" + Floor(Resource.Food) + " meat " + entryMeat);
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Cooking, false);

            // 14. 포식: 먹을 게 없는 배고픈 거미가 합사한 진딧물을 사냥, 처음 먹을 때 한 번 알림.
            feeder.DumpAll(Resource.Leaf); foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo).ToList()) n.gameObject.SetActive(false);
            S(spider).hunger = 5;
            var announced = false; // 다 먹으면 사체가 사라지고 알림도 만료되므로 기다리는 동안 관찰한다
            Check(await Until(() => { announced |= CritterCarcass.All.Any(k => k.Announced) || AntColony.UI.ToastManager.VisibleLines.Any(l => l.Contains("잡아먹혔습니다")); return spider.Hunger > 20 && announced; }, 90), "spider hunted and ate prey: " + spider.Hunger);
            Check(announced, "predation notice once: " + string.Join(",", CritterCarcass.All.Select(k => k.Species + ":" + k.Meat)));
            S(spider).hunger = 100;

            // 15. 치료(2026-10-11 수정): 간호(=의료) 작업. 재고 예약으로 중복 사용 방지, 치료가 약제대 생산보다 먼저.
            // 경상 투약은 투약 당시 경상만 가속. 중상 수술은 기여도 가중 성공률·실패/후유증·회복 기간·수동 재수술.
            var patient = Critter.Spawn(Species.Aphid, In(2, 5), true); await Frames(2); Pen(patient); S(patient).hunger = 100;
            var patient2 = Critter.Spawn(Species.Aphid, In(3, 5), true); await Frames(2); Pen(patient2); S(patient2).hunger = 100;
            Object.DestroyImmediate(spider.gameObject); // 수술 중 포식 방지
            rm.TrySpend(Resource.AnimalMedicine, rm.GetAmount(Resource.AnimalMedicine), ResourceReason.Upkeep); rm.TrySpend(Resource.SurgeryKit, rm.GetAmount(Resource.SurgeryKit), ResourceReason.Upkeep);
            patient.Monster.Wound(patient.Info.health * .3f); patient2.Monster.Wound(patient2.Info.health * .3f); rm.Add(Resource.AnimalMedicine, 1);
            keeper.SetJobEnabled(CommanderJobs.Nursing, true); novice.SetJobEnabled(CommanderJobs.Nursing, true); novice.CommandStop();
            keeper.TickDuty(2); novice.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.Dose && AnimalMedicalStock.Reserved(Resource.AnimalMedicine) == 1 && AnimalMedicalStock.Available(Resource.AnimalMedicine) == 0, "dose reserves the only medicine: " + keeper.AnimalJob);
            Check(novice.AnimalJob != AnimalTask.Dose, "second nurse cannot take reserved medicine: " + novice.AnimalJob);
            novice.CommandStop(); novice.SetJobEnabled(CommanderJobs.Nursing, false);
            var dosed = keeper.AnimalTarget;
            Check(await Until(() => dosed.Medicated > 0, 60), "medicated");
            keeper.CommandStop();
            Check(rm.GetAmount(Resource.AnimalMedicine) == 0 && AnimalMedicalStock.Reserved(Resource.AnimalMedicine) == 0, "medicine consumed once, reservation released");
            var budget = S(dosed).medBudget; Check(budget > 0 && Mathf.Abs(budget - (dosed.Info.health - dosed.Monster.CurrentHealth)) < .01f, "dose covers injury present at dosing: " + budget);
            dosed.Monster.Wound(dosed.Info.health * .05f); Check(Mathf.Approximately(S(dosed).medBudget, budget), "new light injury not added to dose effect");
            // 약품 부족 → 할 수 있는 치료가 없으므로 약제대 생산을 맡는다(간호 작업·의료 능력).
            var bench = Put<Processor>(BuildingKind.MedicineBench, table.Position + new Vector3(0, 0, -3)); await Frames(2); // 도축대 옆(접근 가능 확인된 곳)
            rm.Add(Resource.Leaf, 10); rm.Add(Resource.Honeydew, 5);
            Check(bench.IsMedical && bench.Job == CommanderJobs.Nursing && bench.AddOrder(0, Workshop.OrderMode.Count, 1), "medicine bench order");
            keeper.TickDuty(2);
            Check(keeper.ServiceTarget == bench && keeper.ServiceJob == CommanderJobs.Nursing, "nurse produces medicine when no treatment possible: " + keeper.ServiceTarget + " " + keeper.AnimalJob);
            Check(await Until(() => rm.GetAmount(Resource.AnimalMedicine) >= 1, 60), "medicine produced");
            keeper.CommandStop(); keeper.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.Dose, "treatment preferred once medicine exists: " + keeper.AnimalJob);
            keeper.CommandStop(); AnimalMedicalStock.Release(keeper);
            foreach (var c in new[] { patient, patient2 }) c.Monster.RestoreHealth(c.Info.health);
            Object.DestroyImmediate(patient2.gameObject);
            // 수술 공식(잠정안 A): 기본 50% + 의료×2.5%p − 부상 보정 − 후유증×10%p, 5~95%.
            Check(Mathf.Approximately(Critter.SuccessChance(20, .3f, 0), .95f) && Mathf.Approximately(Critter.SuccessChance(0, 0, 0), .3f)
                && Mathf.Approximately(Critter.SuccessChance(0, 0, 3), .3f) && Mathf.Approximately(Critter.DeathOnFail(2), .1f), "surgery formula");
            // 교대: 실제 진행 작업량 비중으로 의료 능력 가중(마지막 집도의만 쓰지 않음).
            patient.Monster.Wound(patient.Monster.CurrentHealth - patient.Info.health * .3f);
            S(patient).surgeryStarted = true; S(patient).surgery = 10; S(patient).surgeryWork = 10; S(patient).surgerySkillWork = 200; S(patient).preHealth = .3f;
            var weighted = patient.PredictedSuccess(novice); var noviceOnly = Critter.SuccessChance(0, .3f, 0);
            Check(Mathf.Abs(weighted - Critter.SuccessChance(10, .3f, 0)) < .001f && weighted > noviceOnly, "takeover chance weighted by contribution: " + weighted);
            S(patient).surgeryStarted = false; S(patient).surgery = S(patient).surgeryWork = S(patient).surgerySkillWork = 0;
            // 수동 지시: 후유증이 있으면(재수술) 집도의를 지정해야 한다. 고위험(최고 예상 성공률 50% 미만)도 마찬가지.
            S(patient).sequelae = 1; Check(patient.SurgeryNeedsManual && !patient.MayOperate(keeper) && patient.AwaitingSurgeon, "re-surgery waits for surgeon");
            patient.SurgeonId = keeper.PersonalState.id; Check(patient.MayOperate(keeper) && !patient.MayOperate(novice), "assigned surgeon only");
            patient.SurgeonId = ""; S(patient).sequelae = 0; Check(!patient.SurgeryNeedsManual, "skilled first surgery is automatic");
            // 키트·빈 치료대를 예약한 뒤 수술. 결과는 확률이라 성공/실패 각각의 상태 규칙을 확인한다.
            rm.TrySpend(Resource.AnimalMedicine, rm.GetAmount(Resource.AnimalMedicine), ResourceReason.Upkeep); // 남은 의약품의 투약이 수술보다 먼저 잡히지 않게
            rm.Add(Resource.SurgeryKit, 1); Check(patient.HeavyInjury, "heavy injury");
            keeper.TickDuty(2); Check(keeper.AnimalJob == AnimalTask.Surgery && clinic.Worker == keeper && AnimalMedicalStock.Reserved(Resource.SurgeryKit) == 1, "surgeon reserves kit and clinic: " + keeper.AnimalJob);
            Check(await Until(() => patient.Clinic == clinic, 60), "patient admitted");
            Check(patient.SlaughterBlock == "치료 시설 안" && !patient.CanRelease && !Demolition.CanDemolish(clinic) && !Demolition.CanMove(clinic), "in clinic: no slaughter, release, demolish or move");
            Check(await Until(() => patient == null || patient.Monster.IsDead || !patient.SurgeryStarted && (patient.Recovering || patient.AwaitingDecision), 120), "surgery resolved");
            Check(rm.GetAmount(Resource.SurgeryKit) == 0 && AnimalMedicalStock.Reserved(Resource.SurgeryKit) == 0, "one kit consumed");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Nursing, false);
            if (patient != null && !patient.Monster.IsDead)
            {
                if (patient.Recovering)
                {
                    Check(patient.HealthFraction > .99f && patient.Sequelae == 0 && patient.TransportTo == null && !patient.Carriable, "success: healed, stays to recover");
                    var h0 = patient.Hunger; Life(patient, 5); Check(Mathf.Approximately(patient.Hunger, h0), "hunger paused during recovery");
                    // 회복 중 새 경상: 시설 안에서도 자연 회복·투약.
                    patient.Monster.Wound(patient.Info.health * .2f); var hp0 = patient.HealthFraction;
                    Check(patient.Injured && !patient.HeavyInjury && patient.Recovering, "new light injury during recovery");
                    Life(patient, Month * .05f); Check(patient.HealthFraction > hp0 && patient.Recovering, "light injury regenerates inside clinic: " + hp0 + "->" + patient.HealthFraction);
                    foreach (var other in Critter.All.Where(c => c != null && c != patient && c.Monster != null && !c.Monster.IsDead)) other.Monster.RestoreHealth(other.Info.health); // 다른 경상 개체가 먼저 잡히지 않게
                    S(patient).medicated = 0; S(patient).medBudget = 0; // 앞 단계 투약 효과가 남아 있으면 중복 투약하지 않는 것이 정상
                    patient.Monster.Wound(patient.Info.health * .1f); rm.Add(Resource.AnimalMedicine, 1);
                    keeper.SetJobEnabled(CommanderJobs.Nursing, true); keeper.TickDuty(2);
                    Check(keeper.AnimalJob == AnimalTask.Dose && keeper.AnimalTarget == patient, "nurse doses new light injury inside clinic: " + keeper.AnimalJob);
                    Check(await Until(() => patient.Medicated > 0, 60), "medicated in clinic");
                    keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Nursing, false); Check(patient.Clinic == clinic && patient.Recovering, "dose keeps recovery in clinic");
                    Life(patient, patient.RecoveryLeft + 1); Check(!patient.Recovering && patient.TransportTo == ranch, "recovery done, return requested");
                }
                else
                {
                    Check(patient.AwaitingDecision && patient.Sequelae == 1 && patient.HeavyInjury && !patient.Carriable, "failure: sequela, waits for choice (no auto retry)");
                    keeper.SetJobEnabled(CommanderJobs.Nursing, true); keeper.TickDuty(2); Check(keeper.AnimalJob != AnimalTask.Surgery, "no automatic re-surgery"); keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Nursing, false);
                    patient.ChooseReturn(); Check(!patient.AwaitingDecision && patient.TransportTo == ranch, "return chosen");
                }
                keeper.SetJobEnabled(CommanderJobs.Hauling, true); keeper.TickDuty(2);
                Check(await Until(() => patient.Clinic == null && patient.Pen == ranch && patient.InOperatingPen, 90), "patient back in pen");
                keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Hauling, false);
                patient.Monster.RestoreHealth(patient.Info.health); S(patient).sequelae = 0;
            }
            else patient = Critter.Spawn(Species.Aphid, In(2, 5), true); // 낮은 확률 사망(5% × 10%): 이후 단계용 개체

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

            // 17-1. 개체 이름·운반 규칙·목장 화면 일괄 요청(2026-10-11 최신 기획, 실제 UI 버튼 클릭).
            UnityEngine.UI.Button Btn(string part) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.isActiveAndEnabled && b.name.Contains(part));
            var named = Critter.Spawn(Species.Aphid, In(3, 2), true); var tameFree = Critter.Spawn(Species.Aphid, In(4, 2), true); await Frames(2); Pen(named); Pen(tameFree);
            named.CustomName = "  꿀이  "; Check(named.DisplayName == "꿀이(진딧물)", "renamed shows 이름(종): " + named.DisplayName);
            named.CustomName = "가나다라마바사아자차카타파하가나다라"; Check(named.CustomName.Length == 16, "name capped at 16");
            named.CustomName = ""; Check(named.DisplayName == "진딧물", "empty name falls back to species");
            named.CustomName = "꿀이";
            Check(!tameFree.Carriable, "tame but unbound critter is not carriable (bound or downed only)");
            // 길들인 개체 포획: 저항·실패 없이 결박.
            foreach (var x in list) { x.CommandStop(); x.SetJobEnabled(CommanderJobs.All, false); }
            keeper.SetJobEnabled(CommanderJobs.Husbandry, true); tameFree.CaptureDesignated = true; keeper.TickDuty(2);
            Check(keeper.AnimalJob == AnimalTask.Capture, "keeper captures tame critter: " + keeper.AnimalJob);
            Check(await Until(() => tameFree.Bound, 30) && tameFree.Carriable, "tame capture binds without resistance");
            keeper.CommandStop(); keeper.SetJobEnabled(CommanderJobs.Husbandry, false);
            // 목장 화면: 전체 선택 → 포획 요청(두 번 눌러 확인) → 일괄 취소.
            var menu = AntColony.UI.GameMenuController.Instance; menu.ShowRanch(ranch); await Frames(1);
            Btn("모두 선택").onClick.Invoke(); await Frames(1);
            Check(Btn("포획 요청") != null && Btn("이미 결박") != null, "bulk capture shows exclusion reason (bound)");
            Btn("포획 요청").onClick.Invoke(); await Frames(1);
            Check(!named.CaptureDesignated && Btn("확인: 포획 요청") != null && Btn("확인: 포획 요청").name.Contains("꿀이(진딧물)"), "first press shows confirmation with names/counts");
            Btn("확인: 포획 요청").onClick.Invoke(); await Frames(1);
            Check(named.CaptureDesignated && !tameFree.CaptureDesignated, "bulk capture applied to eligible only");
            Btn("일괄 취소").onClick.Invoke(); await Frames(1); Check(!named.CaptureDesignated && tameFree.Bound, "bulk cancel clears requests, binding kept");
            // 여러 종 일괄 운반: 모든 종을 받는 우리만 목적지. 공통 목적지가 없으면 안내만.
            var boundSpider = Critter.Spawn(Species.Spider, keeper.Position + new Vector3(-5, 0, 2), false); await Frames(2);
            typeof(Critter).GetMethod("Bind", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boundSpider, null); boundSpider.CustomName = "묶인거미"; tameFree.CustomName = "묶인이"; // 버튼을 정확히 찾기 위한 이름
            var spiderAccepted = ranch.Accepts(Species.Spider); if (spiderAccepted) ranch.ToggleAccept(Species.Spider);
            ((HashSet<Critter>)typeof(AntColony.UI.GameMenuController).GetField("picked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu)).Clear(); // 선택 비우기
            menu.ShowRanch(ranch); await Frames(1);
            // V5 목록(2026-10-12): 행(Animal 이름) 안의 선택 칸을 누른다.
            UnityEngine.UI.Button Pick(string who) => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.isActiveAndEnabled && b.name.Contains("Select") && b.GetComponentsInParent<Transform>().Any(t => t.name.Contains("Animal " + who)));
            Pick("묶인이(진딧물)").onClick.Invoke(); await Frames(1);
            Pick("묶인거미(거미)").onClick.Invoke(); await Frames(1);
            Check(Btn("운반 요청 →") == null && Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("공통 목적지가 없습니다")), "no common destination when a species is not accepted");
            ranch.ToggleAccept(Species.Spider); menu.ShowRanch(ranch); await Frames(1);
            Btn("운반 요청 →").onClick.Invoke(); await Frames(1); Btn("확인: 운반 요청").onClick.Invoke(); await Frames(1);
            Check(boundSpider.TransportTo == ranch && tameFree.TransportTo == ranch, "multi-species bulk transport to common pen");
            Btn("일괄 취소").onClick.Invoke(); await Frames(1); Check(boundSpider.TransportTo == null && boundSpider.Bound, "bulk transport cancel keeps binding");
            // 필터: 종 필터를 바꾸면 목록이 줄어든다(작업 순서와 무관).
            Btn("종: 전체").onClick.Invoke(); await Frames(1); Check(Btn("묶인거미(거미)") == null && Btn("종: 진딧물") != null, "species filter narrows list");
            menu.Resume(); Object.DestroyImmediate(boundSpider.gameObject); if (!spiderAccepted) ranch.ToggleAccept(Species.Spider);
            typeof(AntColony.UI.GameMenuController).GetField("fltSpecies", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);

            // 18. 저장: 우리 설정·먹이통·개체 상태(길들임·나이·소속)·사체.
            foreach (var x in list) x.CommandStop(); await Frames(2);
            st.target = 7; feeder.SetTarget(Resource.Fiber, 15); feeder.Priority = 5;
            table.Paused = true; table.ToggleSpecies(Species.Spider);
            var keep = Critter.Spawn(Species.Aphid, In(4, 4), true); await Frames(2); Pen(keep); // 앞 단계 포식 대상과 무관한 저장 확인용 개체
            var orderedId = keep.Id; keep.RequestSlaughter(); S(keep).sequelae = 2; keep.CustomName = "보존이";
            var tameBee = bee; var beeAge = tameBee.Age; var penKey = ranch.Key; var members = ranch.Animals.Count(); var total = Critter.All.Count(c => c != null);
            string error = null; var saved = false; // 장수가 멈추는 데 몇 프레임 걸릴 수 있다(이동 중이면 저장 거부)
            for (var i = 0; i < 60 && !saved; i++) { foreach (var x in list) x.CommandStop(); saved = SaveSystem.TrySave(false, 0, out error); if (!saved) await Frames(1); }
            Check(saved, "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready(); await Frames(5);
            var r2 = Ranch.Find(penKey); var f2 = RanchFacility.All.First(f => f.IsFeeder);
            Check(r2 != null && r2.Setting(Species.Aphid).target == 7 && f2.Target(Resource.Fiber) == 15 && f2.Priority == 5, "ranch and feeder settings survive load");
            Check(Critter.All.Count(c => c != null) == total && r2.Animals.Count() == members, "critters and membership survive load");
            Check(Critter.All.Any(c => c != null && c.Species == Species.Bee && c.Tame && Mathf.Abs(c.Age - beeAge) < .01f), "tame bee age survives");
            var a1Loaded = Critter.All.FirstOrDefault(c => c != null && c.Id == orderedId); var t2 = RanchFacility.All.First(f => f.IsButcher);
            Check(a1Loaded != null && a1Loaded.SlaughterOrdered && a1Loaded.Sequelae == 2 && a1Loaded.CustomName == "보존이" && t2.Paused && !t2.AllowsSpecies(Species.Spider) && t2.AllowsSpecies(Species.Aphid), "slaughter request, sequelae, butcher settings survive load");
            Check(Critter.All.Where(c => c != null).Select(c => c.Id).Distinct().Count() == Critter.All.Count(c => c != null), "critter ids unique");
            // 옛 도축대 저장(합산 방식) → 사체별 대기열로 이관.
            typeof(RanchFacility).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(t2, new object[] { "{\"meat\":30,\"chitin\":4,\"carcasses\":2,\"butcher\":1}" });
            Check(t2.Carcasses == 2 && Mathf.Approximately(t2.Queue[0].meat, 15) && t2.Queue[1].chitin == 2 && t2.AllowsSpecies(Species.Aphid) && !t2.Paused, "legacy butcher save migrated per carcass");
            t2.TickDecay(Corpse.Lifetime + 1); Check(t2.Carcasses == 0 && t2.ButcherProgress == 0, "fully decayed carcasses leave the queue (cannot be butchered)");

            // 19. 옛 저장(v16 한 종·마릿수 우리) → 길들인 성체 개체로 풀어 놓는다.
            var before = Critter.All.Count(c => c != null);
            typeof(Ranch).GetMethod("RestoreState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r2, new object[] { new Ranch.State { species = (int)Species.Silkworm, count = 3 } }); await Frames(3);
            Check(Critter.All.Count(c => c != null) == before + 3 && Critter.All.Count(c => c != null && c.Species == Species.Silkworm && c.Tame) >= 3, "legacy pen migrated to individuals");
            return "PASS " + checks + " Spec1011Ranch checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
