namespace AntColony.Regression
{
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;

// 새 Play 세션. 실제 진격/교전을 확인하고 점령 시간만 직접 진행한다.
public static class SettlementDefenseChecks
{
    static int checks;
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception("FAIL: " + message); checks++; }
    static void Set(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) { field.SetValue(target, value); return; }
        }
        throw new Exception("Missing field " + name);
    }
    static async Task<bool> Wait(Func<bool> condition, int seconds = 30)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(25);
        return condition();
    }
    static void Move(CommanderAnt c, Vector3 position)
    {
        c.CommandStop();
        c.Agent.enabled = false;
        Check(NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas), "walkable position");
        c.transform.position = hit.position;
        c.ApplyMovementMode();
        Physics.SyncTransforms();
    }
    static void KillInvaders(SettlementDefense defense)
    { foreach (var enemy in defense.Attackers.ToArray()) if (enemy != null) enemy.TakeDamage(float.MaxValue); }

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "Play mode required");
        // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 게임을 시작하고, 선전포고를 위해 월드맵을 연다.
        if (!GameSession.Instance.GameStarted)
        {
            while (AntColony.Save.SaveSystem.Busy) await Task.Delay(50);
            AntColony.Save.SaveSystem.NewGame(new NewGameOptions());
            while (AntColony.Save.SaveSystem.Busy) await Task.Delay(50);
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
        }
        typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(WorldMapManager.Instance, new object[] { true });
        var world = WorldMapManager.Instance;
        var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
        var resources = ResourceManager.Instance;
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        { resources.AddCapacity(type, 10000); resources.Add(type, 1000); }
        AntPool.Instance.Breed(100);
        var roster = CommanderRoster.Instance.Commanders;
        var captive = roster[0]; var escapee = roster[1]; var crew = roster[2]; var collector = roster[3];
        foreach (var c in new[] { escapee, crew, collector })
        {
            c.CommandStop();
            if (!c.HasTroops) Check(c.TryAssign(2), "test commander receives troops");
        }
        captive.CommandStop();
        Check(captive.TryAssign(8), "defender receives troops");
        captive.Talents.Add(CommanderActivity.Melee, 250);
        captive.CompleteLabUpgrade(true, 3);
        var level = captive.Talents.Level(CommanderActivity.Melee);
        var traits = captive.Traits;
        Check(world.CanCreateTransport(captive.Position, out var position), "transport placement");
        var ship = world.CreateTransport(false, position);
        foreach (var c in new[] { captive, escapee, crew }) Move(c, position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { captive, escapee, crew }) && ship.TryDepart(site), "depart with garrison and crew");
        ship.Tick(ship.TravelSeconds);
        foreach (var b in site.Colony.GetComponentsInChildren<BuildingBase>()) b.TakeDamage(float.MaxValue);
        foreach (var enemy in site.GetComponentsInChildren<WildMonster>()) enemy.TakeDamage(float.MaxValue);
        Check(await Wait(() => site.Cleared), "initial conquest");
        Check(site.TryResolveConquest(ConquestDisposition.Annexed), "annex creates defense");
        var defense = site.Defense;
        Check(defense != null && !defense.UnderAttack, "no instant raid on annexation");
        Check(ship.TryStation(new[] { captive, escapee }), "station defenders");
        Move(crew, site.Landing + Vector3.right * 20);
        Move(escapee, site.Landing + Vector3.right * 20);
        Move(captive, site.Landing + Vector3.forward * 2);
        // 6단계: 편입 거점 침공은 교전 중 문명의 일정으로만 시작된다.
        var foe = DiplomacyManager.Instance.Data.civilizations[0];
        Check(!defense.TryStartRaid(), "no raid while every civilization is at peace");
        DiplomacyManager.Instance.DeclareWar(foe);
        var before = defense.Remaining;
        defense.Tick(float.NaN); defense.Tick(float.PositiveInfinity); defense.Tick(-1);
        Check(defense.Remaining == before, "invalid time has no effect");
        defense.Tick(1);
        Check(!defense.UnderAttack, "raid waits for faction schedule");
        Check(defense.TryStartRaid(site.Difficulty, foe.id) && defense.Attackers.Count == site.Difficulty && !defense.TryStartRaid(), "faction wave and no overlap");
        foreach (var enemy in defense.Attackers)
        {
            Set(enemy, "attackDamage", 1f);
            Set(enemy, "currentHealth", 10f);
            enemy.GetComponent<NavMeshAgent>().speed = 20;
        }
        Check(await Wait(() => !defense.UnderAttack), "invaders actually advance and garrison wins through automatic combat");
        Check(site.Disposition == ConquestDisposition.Annexed && defense.Remaining > 290, "victory keeps site and resets interval");
        Check(world.SettlementNotice.Contains("successful"), "victory notification");

        Move(captive, site.Landing + Vector3.right * 22);
        Check(defense.TryStartRaid(), "next raid starts");
        foreach (var enemy in defense.Attackers) enemy.GetComponent<NavMeshAgent>().speed = 25;
        Check(await Wait(() => defense.Attackers.Any(a => a != null && (a.Position - site.Landing).sqrMagnitude <= 36)),
            "unguarded invaders reach landing on navmesh");
        // 실제 교전과 별개로, 방어 병력의 점령 저지를 시간 진행으로 확인한다.
        Move(captive, site.Landing + Vector3.right * 3);
        defense.Tick(30);
        Check(defense.CaptureProgress == 0 && site.Disposition == ConquestDisposition.Annexed, "combat defender prevents occupation");
        Move(captive, site.Landing + Vector3.right * 22);
        Set(captive, "carriedType", ResourceType.Food); Set(captive, "carriedAmount", 4.5f);
        Set(escapee, "carriedType", ResourceType.Food); Set(escapee, "carriedAmount", 2.75f);
        Set(crew, "carriedType", ResourceType.Soil); Set(crew, "carriedAmount", 3f);
        ship.DepositResources(ResourceType.Food, 17);
        Check(world.CanCreateTransport(world.HomePosition, out var routePosition), "second transport placement");
        var automatic = world.CreateTransport(false, routePosition);
        Move(collector, routePosition + Vector3.right * 3);
        if (!collector.HasTroops) Check(collector.TryAssign(2), "replenish home collector before departure");
        Check(automatic.TryBoard(new[] { collector }), "home auto route boards: troops=" + collector.TroopCount
            + " carrying=" + collector.IsCarrying + " away=" + collector.IsAwayFromHome);
        Check(automatic.Route.TryStart(site), "home auto route ready: " + site.Disposition);
        var poolBefore = AntPool.Instance.Total;
        var captiveTroops = captive.TroopCount;
        // RNG를 한 번만 고정해 같은 상실 사건에서 포로/탈출 두 갈래를 모두 검증한다.
        var randomState = UnityEngine.Random.state;
        for (var seed = 0; ; seed++)
        {
            UnityEngine.Random.InitState(seed);
            if (UnityEngine.Random.value < .5f && UnityEngine.Random.value >= .5f)
            { UnityEngine.Random.InitState(seed); break; }
        }
        defense.Tick(30);
        UnityEngine.Random.state = randomState;
        Check(site.Disposition == ConquestDisposition.Lost && !defense.UnderAttack, "uncontested occupation loses site");
        Check(defense.Prisoners.Count == 1 && captive.Captor == site && captive.IsCaptive && !captive.gameObject.activeSelf,
            "one commander captured and hidden");
        Check(!escapee.IsAwayFromHome && escapee.isActiveAndEnabled && Vector3.Distance(escapee.Position, world.HomePosition) < 15,
            "other commander escapes to home");
        Check(site.Settlement.Garrison.Count == 0 && AntPool.Instance.Total == poolBefore - captiveTroops,
            "captured troops lost once without refund or double count");
        Check(captive.Talents.Level(CommanderActivity.Melee) == level && captive.LabAttackLevel == 1 && captive.Traits == traits,
            "captivity retains identity and growth");
        Check(!captive.TryAssign(1)
            && !captive.TryPowerStrike(), "captives cannot be controlled or upgraded");
        Check(!automatic.Route.IsRunning && automatic.Crew.Count == 0, "lost destination immediately stops home auto route");
        Check(ship.State == ExpeditionState.Returning && crew.IsEmbarked && ship.GetCargo(ResourceType.Food) == 17
            && ship.GetCargo(ResourceType.Soil) == 3 && !crew.IsCarrying, "deployed crew and cargo evacuate intact");
        Check(Mathf.Abs(site.GetComponentsInChildren<ResourceNode>().Where(n => n.name.StartsWith("Dropped ")).Sum(n => n.AmountRemaining) - 7.25f) < .001f,
            "garrison carried resources remain recoverable without rounding loss");
        var stock = site.Colony.GetStock(ResourceType.Food);
        site.Settlement.Tick(600);
        Check(site.Colony.GetStock(ResourceType.Food) == stock && !defense.TryStartRaid(), "lost site stops production and future raids");
        Check(site.Colony.GetComponentsInChildren<ResourceNode>(true).All(n => !n.CanGather), "lost stock locked");
        Check(!site.TryResolveConquest(ConquestDisposition.Annexed), "living occupiers prevent recapture");
        var homeFood = resources.GetAmount(ResourceType.Food);
        ship.Tick(ship.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Food) == homeFood + 17 && !crew.IsAwayFromHome && site.Visitor == null,
            "evacuation returns cargo once and clears visitor");
        ship.Tick(ship.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Food) == homeFood + 17, "repeated tick cannot duplicate cargo");

        Move(crew, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { crew }) && ship.TryDepart(site), "lost site can be invaded again");
        ship.Tick(ship.TravelSeconds);
        Check(!ship.TryStation(ship.Crew), "lost site rejects new garrison");
        KillInvaders(defense);
        Check(site.TryResolveConquest(ConquestDisposition.Annexed), "defeated occupiers allow recapture");
        Check(!captive.IsCaptive && captive.isActiveAndEnabled && !captive.HasTroops && captive.Garrison == site.Settlement
            && defense.Prisoners.Count == 0 && site.GetComponents<SettlementDefense>().Length == 1,
            "recapture rescues original commander without troops or duplicate defense component");
        Check(captive.Talents.Level(CommanderActivity.Melee) == level && captive.LabAttackLevel == 1 && captive.Traits == traits,
            "rescue preserves progression and traits");
        Move(captive, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { captive }) && ship.TryReturn(), "rescued zero-troop commander can return");
        ship.Tick(ship.TravelSeconds);
        Check(captive.TryAssign(1) && !captive.IsAwayFromHome, "rescued commander can replenish at home");
        var ui = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        if (!ui.IsOpen) ui.Toggle();
        Set(ui, "selectedSite", site);
        await Task.Delay(100);
        Canvas.ForceUpdateCanvases();
        var label = ui.PanelRect.Find("SettlementDefenseStatus").GetComponent<UnityEngine.UI.Text>();
        Check(label.text.Contains("Defense ready"), "defense state visible in panel");
        var corners = new Vector3[4]; ((RectTransform)label.transform).GetWorldCorners(corners);
        Check(corners.All(c => ui.PanelRect.rect.Contains(ui.PanelRect.InverseTransformPoint(c))), "defense label within panel");
        Check(ui.transform.Find("SettlementNotice").GetComponent<UnityEngine.UI.Text>().text.Contains("rescued"), "global rescue notification");

        // 빈 수송수단이 출발한 사이 거점을 잃어도 적지에 고립되지 않고 복귀한다.
        Move(captive, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { captive }) && ship.TryDepart(site), "second deployment");
        ship.Tick(ship.TravelSeconds);
        Check(ship.TryStation(ship.Crew) && ship.TryReturn(), "leave rescued commander as garrison");
        ship.Tick(ship.TravelSeconds);
        Check(ship.TryDepart(site), "empty transport outbound for pickup");
        Check(defense.TryStartRaid(), "raid during outbound transport");
        foreach (var enemy in defense.Attackers) enemy.GetComponent<NavMeshAgent>().Warp(site.Landing);
        Move(captive, site.Landing + Vector3.right * 20);
        Set(defense, "captureChance", 1f);
        defense.Tick(30);
        Check(captive.IsCaptive && ship.State == ExpeditionState.Returning && ship.Crew.Count == 0,
            "empty outbound transport turns back on site loss");
        ship.Tick(ship.TravelSeconds);
        Check(site.Visitor == null && ship.State == ExpeditionState.Home, "outbound evacuation frees site");
        Move(escapee, ship.Position + Vector3.right * 3);
        if (!escapee.HasTroops) Check(escapee.TryAssign(2), "rescue party replenished");
        Check(ship.TryBoard(new[] { escapee }) && ship.TryDepart(site), "rescue party departs");
        ship.Tick(ship.TravelSeconds);
        KillInvaders(defense);
        Check(site.TryResolveConquest(ConquestDisposition.Abandoned), "abandon after victory also frees prisoners");
        Check(!captive.IsCaptive && !captive.IsAwayFromHome && captive.isActiveAndEnabled
            && Vector3.Distance(captive.Position, world.HomePosition) < 15 && defense.Prisoners.Count == 0,
            "abandoned-site rescued commander returns home");
        Check(!site.TryResolveConquest(ConquestDisposition.Annexed) && !defense.TryStartRaid(), "abandoned site cannot resurrect defense");
        Check(ship.TryReturn(), "rescue party can leave abandoned site");
        ship.Tick(ship.TravelSeconds);
        return "PASS: " + checks + " settlement invasion / capture / escape / evacuation / rescue checks";
    }
}
}


