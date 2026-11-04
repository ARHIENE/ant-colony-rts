using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 불시착 1~2명인 새 Play 세션에서 실행. 비축더미/기존 병력/기존 수송을 전제하지 않는다.
public static class ImplC2Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception("C2 FAIL " + count + ": " + label); count++; }
    static object Call(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Hidden | BindingFlags.Public).Invoke(target, args);
    static void Property(object target, string property, object value)
        => target.GetType().GetField("<" + property + ">k__BackingField", Hidden).SetValue(target, value);
    static void State(ExpeditionTransport ship, ExpeditionState state, ExpeditionSite site, float remaining = 0)
    { Property(ship, "State", state); Property(ship, "Site", site); Property(ship, "Remaining", remaining); }
    static ExpeditionTransport Ship(WorldMapManager world, Vector3 at, bool air = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.position = at;
        var ship = go.AddComponent<ExpeditionTransport>(); ship.Initialize(air);
        ((List<ExpeditionTransport>)world.Transports).Add(ship);
        return ship;
    }
    // 검사 실행기는 메인 메뉴에서 시작하므로 검사 전용 불시착 게임을 먼저 연다(임시 저장 루트·자동저장 끔).
    public static async Task<string> Main()
    {
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "ImplC2-" + Guid.NewGuid().ToString("N"));
        try
        {
            for (var until = DateTime.UtcNow.AddSeconds(90); SaveSystem.Busy && DateTime.UtcNow < until;) await Task.Delay(50);
            var team = new[] { StartingCandidate.Roll(0) }; team[0].traits.values.Clear(); // G03: 작업 금지 특성이면 시작이 막힌다
            CrashLandingStart.Begin(new NewGameOptions { startingCount = 1, seed = 261024, mapSize = MapSize.Small, biome = MapBiome.Forest, startingTeam = team });
            for (var until = DateTime.UtcNow.AddSeconds(90); SaveSystem.Busy && DateTime.UtcNow < until;) await Task.Delay(50);
            await Task.Delay(300); Time.timeScale = 0;
            return Run();
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }

    public static string Run()
    {
        count = 0;
        Check(Application.isPlaying && !SaveSystem.Busy, "ready Play session required");
        var world = WorldMapManager.Instance; var roster = CommanderRoster.Instance;
        var research = CampaignResearch.Instance; var map = HomeMapBuilder.Instance;
        Check(world != null && roster != null && research != null && map != null, "scene services");
        var oldResearch = JsonUtility.FromJson<CampaignResearch.State>(JsonUtility.ToJson(research.CaptureState()));
        var oldSettings = UserSettings.Current.Clone(); var isolated = oldSettings.Clone(); isolated.autoSaveEnabled = false;
        var oldRandom = UnityEngine.Random.state; var oldScale = Time.timeScale; var oldBounds = map.WorldBounds;
        var oldFloor = ResourceNode.Available.ToArray();
        var oldFloorAmounts = oldFloor.Where(n => n != null).Select(n => (node: n, amount: n.AmountRemaining)).ToArray();
        var bins = ResourceManager.Instance.Bins.Select(b => (bin: b, state: Call(b, "Capture"))).ToArray();
        var history = CampaignHistory.Instance?.Capture();
        var ships = new List<ExpeditionTransport>(); var commanders = new List<CommanderAnt>();
        GameObject siteObject = null, enemy = null;
        try
        {
            UserSettings.Apply(isolated, false); Time.timeScale = 0;
            Check(!ExpeditionTransportHomeEnemies(), "fresh home has no enemies");
            Check(HomeReturnPoint.TryAir(out var at), "fixture home NavMesh landing");
            foreach (var label in new[] { "C2 first", "C2 second" })
            {
                var c = roster.Create(label, CommanderRank.Sergeant, new[] { UnitRole.Melee }, UnitRole.Melee, new CommanderTraits(), at);
                Check(c != null, "own commander fixture"); commanders.Add(c);
                c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop();
            }
            var first = commanders[0]; var second = commanders[1];
            var ship = Ship(world, at); ships.Add(ship);
            research.RestoreState(new CampaignResearch.State());

            // G42: 불응 중에는 명부/위치 그대로, 해소 후 탑승. 취소/이탈 명령 제거.
            first.PersonalState.rageRemaining = 20;
            Check(ship.TryBoard(new[] { first }) && ship.PendingCrewOrderCount == 1 && first.Transport == null,
                "G42 board accepted but waits during rage");
            Check(ship.CrewOrderStatus.Contains(first.OrderRefusalReason), "G42 refusal reason displayed");
            var savedOrders = Call(ship, "CaptureCrewOrders");
            ship.CancelCrewOrder(first); Check(ship.PendingCrewOrderCount == 0, "G42 explicit cancellation");
            Call(ship, "RestoreCrewOrders", savedOrders);
            Check(ship.PendingCrewOrderCount == 1 && first.Transport == null, "G42 restore has no boarding side effect");
            ship.Tick(.01f); Check(first.Transport == null, "G42 restored refusal still waits");
            first.PersonalState.rageRemaining = 0; ship.Tick(.01f);
            Check(first.Transport == ship && first.IsEmbarked && ship.PendingCrewOrderCount == 0, "G42 executes after rage");
            first.PersonalState.mentalBreak = MentalBreak.Binge;
            Check(ship.TryUnloadCrew() && first.IsEmbarked && ship.PendingCrewOrderCount == 1, "G42 unload waits for binge");
            first.PersonalState.mentalBreak = MentalBreak.None; ship.Tick(.01f);
            Check(first.Transport == null && !first.IsEmbarked, "G42 unload after refusal ends");
            ship.transform.position = first.Position;
            first.PersonalState.rageRemaining = 20; ship.TryBoard(new[] { first });
            var other = Ship(world, first.Position); ships.Add(other);
            Check(other.TryBoard(new[] { first }) && ship.PendingCrewOrderCount == 0, "G42 new ship replaces previous order");
            other.CancelCrewOrder(first); first.PersonalState.rageRemaining = 0;
            second.PersonalState.rageRemaining = 20; ship.TryBoard(new[] { second });
            second.gameObject.SetActive(false); ship.Tick(.01f);
            Check(ship.PendingCrewOrderCount == 0, "G42 inactive passenger removed"); second.gameObject.SetActive(true);
            second.PersonalState.rageRemaining = 0;

            siteObject = new GameObject("C2 boss/site fixture"); siteObject.transform.position = first.Position + Vector3.forward * 22;
            var site = siteObject.AddComponent<ExpeditionSite>();
            Property(site, "Kind", ExpeditionSiteKind.BossNest);
            ((List<ExpeditionSite>)world.Sites).Add(site);
            var stronghold = siteObject.AddComponent<BossStronghold>(); stronghold.Initialize(BossId.A, site);
            ship.transform.position = first.Position;
            Check(ship.TryBoard(new[] { first }), "board reward collector");
            State(ship, ExpeditionState.Deployed, site); Call(ship, "LandCrew");
            var p = research.BossProgress; Check(p.RecordKill(BossId.A, 0), "prepare real core parcel");
            Check(ship.AddCargo(AntColony.Data.ResourceType.Soil, ship.CargoCapacity - 2) == ship.CargoCapacity - 2, "fill ordinary cargo leaving two slots");
            Check(ship.TryCollectRewards() && !ship.BlueprintCargo && ship.EquipmentCargo.Count == 0,
                "B6 boss dispatch no legacy blueprint/equipment");
            Check(ship.CargoLoad == ship.CargoCapacity && ship.HasCargo && ship.CargoFull, "B6 shared capacity counts core once");
            Check(ship.AddCargo(AntColony.Data.ResourceType.Soil, 1) == 0 && !ship.TryCollectRewards(), "B6 full cargo and duplicate reward blocked");

            // G69: 실패하면 Returning/Site/승객/핵심물을 그대로 유지한다.
            State(ship, ExpeditionState.Returning, site);
            first.PersonalState.rageRemaining = 0;
            Call(first, "SetEmbarked", true, ship.Position);
            Property(map, "WorldBounds", new Bounds()); ship.Tick(ship.TravelSeconds);
            Check(ship.State == ExpeditionState.Returning && first.IsEmbarked && !p.Delivered(BossId.A) && ship.HasCargo,
                "G69 missing return point preserves whole transport");
            Property(map, "WorldBounds", oldBounds);
            enemy = new GameObject("C2 residual enemy"); enemy.transform.position = at; enemy.AddComponent<WildMonster>();
            Check(ExpeditionTransportHomeEnemies(), "G69 residual enemy detected");
            ship.Tick(.01f); Check(ship.State == ExpeditionState.Returning && !p.Delivered(BossId.A), "G69 enemy blocks return and delivery");
            Object.DestroyImmediate(enemy); enemy = null;
            Check(HomeReturnPoint.TryGround(ship.Position, out _), "G69 ground edge exists");
            first.PersonalState.rageRemaining = 10;
            ship.Tick(.01f);
            Check(ship.State == ExpeditionState.Home && first.IsEmbarked && p.Delivered(BossId.A) && p.Home(BossId.A) == 2,
                "B6 permanent import precedes ordinary unloading while crew refuses");
            first.PersonalState.rageRemaining = 0; ship.Tick(.01f); Call(ship, "UnloadCargo");
            Check(first.Transport == null && !ship.HasCargo && p.Home(BossId.A) == 2, "G69 unload completes once");

            // 실물 합계로 창고/바닥 중복 지급을 확인한다(창고 없는 불시착도 허용).
            float Physical() => ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Soil)
                + ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == AntColony.Data.ResourceType.Soil
                    && n.GetComponentInParent<ExpeditionSite>() == null).Sum(n => n.AmountRemaining);
            var before = Physical(); ship.AddCargo(AntColony.Data.ResourceType.Soil, 7);
            Call(ship, "UnloadCargo"); Call(ship, "UnloadCargo");
            Check(Physical() == before + 7 && ship.CargoLoad == 0, "G69 floor/storage conservation without double payment");

            p.TryRespawn(BossId.A, BossProgress.RespawnSeconds, false, false, false);
            p.RecordKill(BossId.A, BossProgress.RespawnSeconds); p.TryAcquire(BossId.A, BossRewardTransfer.Carrier(other), out _);
            other.gameObject.SetActive(false); other.gameObject.SetActive(true);
            Check(BossRewardTransfer.CargoLoad(other) == 2, "B6 normal disable/rebuild is not loss");
            other.TakeDamage(float.MaxValue);
            Check(BossRewardTransfer.CargoLoad(other) == 0 && p.Obtained(BossId.A), "B6 confirmed destruction loses cargo but keeps recovery eligibility");

            // G15: 동시 접안만 배타, 기존 부대가 남아 있어도 후속 부대 전개.
            var follow = Ship(world, at); ships.Add(follow);
            State(ship, ExpeditionState.Deployed, site); typeof(ExpeditionTransport).GetProperty("DockingRemaining").SetValue(ship, ExpeditionRules.DockSeconds);
            State(follow, ExpeditionState.Outbound, site); follow.Tick(follow.TravelSeconds);
            Check(follow.State == ExpeditionState.Outbound, "G15 docking interval blocks concurrent arrival");
            ship.Tick(ExpeditionRules.DockSeconds); follow.Tick(.01f);
            Check(follow.State == ExpeditionState.Deployed && ship.State == ExpeditionState.Deployed, "G15 follow-up joins deployed party");

            Property(site, "Disposition", ConquestDisposition.Annexed);
            var settlement = siteObject.AddComponent<AnnexedSettlement>(); Property(site, "Settlement", settlement);
            ship.transform.position = first.Position;
            State(ship, ExpeditionState.Home, null); Check(ship.TryBoard(new[] { first }), "station fixture board");
            State(ship, ExpeditionState.Deployed, site); Call(ship, "LandCrew"); Call(first, "RestoreTroops", 1, 0f);
            first.PersonalState.rageRemaining = 20;
            Check(ship.TryStation(new[] { first }) && first.Transport == ship && first.Garrison == null, "G42 station waits");
            first.PersonalState.rageRemaining = 0; ship.Tick(.01f);
            Check(first.Garrison == settlement && first.Transport == null, "G42 station executes after refusal");

            var diplomacy = DiplomacyManager.Instance;
            Check(diplomacy != null, "homecoming service");
            var prisoner = (Prisoner)typeof(DiplomacyManager).GetMethod("Snapshot", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { second });
            prisoner.PersonalState.id = Guid.NewGuid().ToString("N");
            var h = new Homecoming { commander = prisoner, from = "C2 fixture" };
            Property(map, "WorldBounds", new Bounds());
            Check(!(bool)Call(diplomacy, "Arrive", h) && h.commander == prisoner, "G69 homecoming waits without safe point");
            Property(map, "WorldBounds", oldBounds);
            Check((bool)Call(diplomacy, "Arrive", h), "G69 released prisoner arrives at home edge");
            var returned = roster.Commanders.Single(c => c.PersonalState.id == prisoner.PersonalState.id); commanders.Add(returned);
            Check(!returned.IsAwayFromHome, "G69 arrived prisoner remains home member");
            return "PASS " + count + " C2 checks";
        }
        finally
        {
            Property(map, "WorldBounds", oldBounds);
            foreach (var c in commanders)
                if (c != null) { Call(c, "RestoreTroops", 0, 0f); Call(roster, "Forget", c); Object.DestroyImmediate(c.gameObject); }
            foreach (var s in ships)
            {
                ((List<ExpeditionTransport>)world.Transports).Remove(s);
                if (s != null) Object.DestroyImmediate(s.gameObject);
            }
            if (siteObject != null)
            { ((List<ExpeditionSite>)world.Sites).Remove(siteObject.GetComponent<ExpeditionSite>()); Object.DestroyImmediate(siteObject); }
            if (enemy != null) Object.DestroyImmediate(enemy);
            foreach (var n in ResourceNode.Available.ToArray()) if (n != null && !oldFloor.Contains(n)) Object.DestroyImmediate(n.gameObject);
            foreach (var f in oldFloorAmounts)
                if (f.node != null)
                { if (f.node.AmountRemaining > f.amount) f.node.Extract(f.node.AmountRemaining - f.amount); else f.node.AddStock(f.amount - f.node.AmountRemaining); }
            foreach (var b in bins) Call(b.bin, "Apply", b.state);
            if (history != null) CampaignHistory.Instance?.Restore(history);
            research.RestoreState(oldResearch); UserSettings.Apply(oldSettings, false);
            UnityEngine.Random.state = oldRandom; Time.timeScale = oldScale;
        }
    }

    static bool ExpeditionTransportHomeEnemies() => (bool)typeof(ExpeditionTransport)
        .GetMethod("HomeHasEnemies", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
}
