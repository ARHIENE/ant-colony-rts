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
        var savedAutoPause = UserSettings.Current.autoPauseThreat;
        UserSettings.Current.autoPauseThreat = false;
        try
        {
        // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 게임을 시작하고, 선전포고를 위해 월드맵을 연다.
        if (!GameSession.Instance.GameStarted)
        {
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy, 90), "scene ready");
            var team = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            AntColony.Save.SaveSystem.NewGame(new NewGameOptions { startingCount = 3, startingTeam = team });
            Check(await Wait(() => !AntColony.Save.SaveSystem.Busy, 90), "new game ready");
        }
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
        typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(WorldMapManager.Instance, new object[] { true });
        var world = WorldMapManager.Instance;
        var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
        var resources = ResourceManager.Instance;
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        { resources.AddCapacity(type, 10000); resources.Add(type, 1000); }
        AntPool.Instance.Breed(1000); // G04 동원 상한(성인 × 제도 비율)으로 병력 6명 이상 배정하려면 시민이 넉넉해야 한다
        // 불시착 시작은 장수 1~3명이라 검사용 4명을 채운다.
        for (var i = 0; i < 10 && CommanderRoster.Instance.Commanders.Count < 4; i++)
            CommanderRoster.Instance.Create(null, AntColony.Data.CommanderRank.Corporal, new[] { UnitRole.Worker }, UnitRole.Worker, new AntColony.Units.CommanderTraits(), CommanderRoster.Instance.Commanders[0].Position);
        var roster = CommanderRoster.Instance.Commanders;
        foreach (var x in roster) x.SetJobEnabled(AntColony.Units.CommanderJobs.All, false);
        var captive = roster[0]; var escapee = roster[1]; var crew = roster[2]; var collector = roster[3];
        foreach (var c in new[] { escapee, crew, collector })
        {
            c.CommandStop();
            c.WorkState.duty = AntColony.Units.CommanderDuty.Deployed; // 원정 병력은 출전 편성으로 받는다.
            if (!c.HasTroops) Check(c.TryAssign(2), "test commander receives troops");
        }
        captive.CommandStop();
        captive.WorkState.duty = AntColony.Units.CommanderDuty.Deployed; // 수비 병력도 출전 편성으로 받는다.
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
        // 정복한 문명은 멸망할 수 있다. 살아 있는 다른 거점에 실제로 도착해 G62 접촉을 만든다.
        var diplomacy = DiplomacyManager.Instance;
        foreach (var faction in diplomacy.Data.civilizations) faction.war = false;
        Check(!RocketCampaign.SuppressNewCrises, "fresh session has no rocket crisis suppression (G39)");
        Check(!defense.TryStartRaid(), "no raid while every civilization is at peace");
        var contactSite = world.Sites.First(s => s != site && s.Kind == ExpeditionSiteKind.Settlement
            && diplomacy.Faction(s) != null && !diplomacy.Faction(s).war);
        var foe = diplomacy.Faction(contactSite);
        Check(world.CanCreateTransport(world.HomePosition, out var contactPosition), "contact transport placement");
        var contactShip = world.CreateTransport(false, contactPosition);
        Move(collector, contactPosition + Vector3.right * 3);
        Check(contactShip.TryBoard(new[] { collector }) && contactShip.TryDepart(contactSite), "first contact expedition departs");
        contactShip.Tick(contactShip.TravelSeconds);
        Check(foe.contacted && diplomacy.DeclareWar(foe), "first contact permits declaration of war");
        Check(contactShip.TryReturn(), "contact expedition returns"); contactShip.Tick(contactShip.TravelSeconds);
        var before = defense.Remaining;
        defense.Tick(float.NaN); defense.Tick(float.PositiveInfinity); defense.Tick(-1);
        Check(defense.Remaining == before, "invalid time has no effect");
        defense.Tick(1);
        Check(!defense.UnderAttack, "raid waits for faction schedule");
        Check(defense.TryStartRaid(site.Difficulty, foe.id) && defense.Attackers.Count == site.Difficulty && !defense.TryStartRaid(), $"faction wave and no overlap (suppress {AntColony.Core.RocketCampaign.SuppressNewCrises} war {foe.war} extinct {foe.extinct} template {typeof(ExpeditionSite).GetProperty("GuardTemplate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(site) != null} nav {UnityEngine.AI.NavMesh.SamplePosition(site.transform.position + Vector3.forward * 28, out _, 4, UnityEngine.AI.NavMesh.AllAreas)} attackers {defense.Attackers.Count}/{site.Difficulty})");
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
        // 포획/탈출 난수 사이에 OnDowned의 부상 난수가 끼지 않도록 쓰러진 수비자를 먼저 준비한다.
        var poolBefore = AntPool.Instance.Total;
        var captiveTroops = captive.TroopCount;
        var deathMode = GameSession.Instance.Options.commanderDeath;
        try
        {
            GameSession.Instance.Options.commanderDeath = CommanderDeathMode.Gentle;
            captive.TakeDamage(float.MaxValue);
        }
        finally { GameSession.Instance.Options.commanderDeath = deathMode; }
        Check(!captive.IsDead && captive.PersonalHealth == 0 && !captive.HasTroops
            && AntPool.Instance.Total == poolBefore - captiveTroops, "downed fixture loses troops exactly once");
        // 의료의 무작위 출혈/감염은 별도 검사 범위. 부상 상태는 유지해 CaptureInjury의 추가 난수도 막는다.
        captive.Health.wounds.Clear(); captive.Health.illnesses.Clear(); captive.Health.bloodLoss = 0;
        MedicalRules.AddWound(captive.Health, BodyPart.Thorax, WoundKind.Bruise, 1).tended = true;
        Set(captive, "carriedType", ResourceType.Food); Set(captive, "carriedAmount", 4.5f);
        Set(escapee, "carriedType", ResourceType.Food); Set(escapee, "carriedAmount", 2.75f);
        Set(crew, "carriedType", ResourceType.Soil); Set(crew, "carriedAmount", 3f);
        ship.DepositResources(ResourceType.Wood, 17); // G05 보급과 화물 보존 검사를 분리한다.
        Check(world.CanCreateTransport(world.HomePosition, out var routePosition), "second transport placement");
        var automatic = world.CreateTransport(false, routePosition);
        Move(collector, routePosition + Vector3.right * 3);
        if (!collector.HasTroops) Check(collector.TryAssign(2), "replenish home collector before departure");
        Check(automatic.TryBoard(new[] { collector }), "home auto route boards: troops=" + collector.TroopCount
            + " carrying=" + collector.IsCarrying + " away=" + collector.IsAwayFromHome);
        Check(automatic.Route.TryStart(site), "home auto route ready: " + site.Disposition);
        Check(site.Settlement.Garrison.SequenceEqual(new[] { captive, escapee }), "capture fixture preserves defender order");
        // 이미 쓰러지고 부상 중인 첫 장수의 포획은 추가 부상 난수를 소비하지 않는다.
        var randomState = UnityEngine.Random.state;
        var foundSeed = false;
        for (var seed = 0; seed < 10000; seed++)
        {
            UnityEngine.Random.InitState(seed);
            if (UnityEngine.Random.value < .5f && UnityEngine.Random.value >= .5f)
            { UnityEngine.Random.InitState(seed); foundSeed = true; break; }
        }
        try { Check(foundSeed, "bounded capture/escape seed search"); defense.Tick(30); }
        finally { UnityEngine.Random.state = randomState; }
        Check(site.Disposition == ConquestDisposition.Lost && !defense.UnderAttack, "uncontested occupation loses site");
        Check(defense.Prisoners.Count == 1 && captive.Captor == site && captive.IsCaptive && !captive.gameObject.activeSelf,
            "one commander captured and hidden: prisoners=" + defense.Prisoners.Count
            + " captive=" + captive.IsCaptive + " dead=" + captive.IsDead + " health=" + captive.PersonalHealth
            + " captor=" + (captive.Captor == site) + " active=" + captive.gameObject.activeSelf);
        Check(!escapee.IsAwayFromHome && escapee.isActiveAndEnabled && Vector3.Distance(escapee.Position, world.HomePosition) < 15,
            "other commander escapes to home");
        Check(site.Settlement.Garrison.Count == 0 && AntPool.Instance.Total == poolBefore - captiveTroops,
            "captured troops lost once without refund or double count");
        Check(captive.Talents.Level(CommanderActivity.Melee) == level && captive.LabAttackLevel == 1 && captive.Traits == traits,
            "captivity retains identity and growth");
        Check(!captive.TryAssign(1)
            && !captive.TryPowerStrike(), "captives cannot be controlled or upgraded");
        Check(!automatic.Route.IsRunning && automatic.Crew.Count == 0, "lost destination immediately stops home auto route");
        Check(ship.State == ExpeditionState.Returning && crew.IsEmbarked && ship.GetCargo(ResourceType.Wood) == 17
            && ship.GetCargo(ResourceType.Soil) == 3 && !crew.IsCarrying, "deployed crew and cargo evacuate intact");
        Check(Mathf.Abs(site.GetComponentsInChildren<ResourceNode>().Where(n => n.name.StartsWith("Dropped ")).Sum(n => n.AmountRemaining) - 7.25f) < .001f,
            "garrison carried resources remain recoverable without rounding loss");
        var stock = site.Colony.GetStock(ResourceType.Food);
        site.Settlement.Tick(600);
        Check(site.Colony.GetStock(ResourceType.Food) == stock && !defense.TryStartRaid(), "lost site stops production and future raids");
        Check(site.Colony.GetComponentsInChildren<ResourceNode>(true).All(n => !n.CanGather), "lost stock locked");
        Check(!site.TryResolveConquest(ConquestDisposition.Annexed), "living occupiers prevent recapture");
        var homeWood = resources.GetAmount(ResourceType.Wood);
        ship.Tick(ship.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Wood) == homeWood + 17 && !crew.IsAwayFromHome && site.Visitor == null,
            "evacuation returns cargo once and clears visitor");
        ship.Tick(ship.TravelSeconds);
        Check(resources.GetAmount(ResourceType.Wood) == homeWood + 17, "repeated tick cannot duplicate cargo");

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
        // G11: 출혈·질병 없는 장수는 하루 후 의식 회복. 60초 회복 전제는 폐기됐다.
        captive.PersonalState.meal.satiety = 100; captive.Health.wounds.Clear(); // 부위 부상(Downed) 회복은 이 검사 범위 밖: 하루 의식 회복만 본다
        for (var t = 0; t <= GameCalendar.SecondsPerDay; t++) { captive.PersonalState.meal.satiety = 100; captive.TickDuty(1); } // 한 번에 하루를 돌리면 의식 없는 동안 포만도가 0이 된다
        Check(captive.PersonalHealth > 0, $"rescued commander recovers (downed {captive.Health.Downed} stable {captive.StableForRecovery} remain {captive.RecoveryRemaining})");
        Move(captive, ship.Position + Vector3.right * 3);
        Check(ship.TryBoard(new[] { captive }) && ship.TryReturn(), "rescued zero-troop commander can return");
        ship.Tick(ship.TravelSeconds);
        Check(captive.TryAssign(1) && !captive.IsAwayFromHome, "rescued commander can replenish at home");
        var ui = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        if (!ui.IsOpen) ui.Toggle();
        Set(ui, "selectedSite", site);
        await Task.Delay(100);
        Canvas.ForceUpdateCanvases();
        var label = ui.PanelRect.GetComponentsInChildren<Transform>(true).First(t => t.name == "SettlementDefenseStatus").GetComponent<UnityEngine.UI.Text>();
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
        finally { UserSettings.Current.autoPauseThreat = savedAutoPause; }
    }
}
}


