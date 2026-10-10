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

// 2026-10-09: Codex 지적 수정(정지 시 공사 보존·공동 건설·취소 환급·작업 우선순위 5단계·주보조 동반 성장·고세율 이탈)
// + 외교 협상(담당 장수·반응·역제안·인내·결렬/재개·보류 알림·자동 정지·초과분 바닥 보관) + 포로 외교(몸값·제3자 금지·압수·귀환·무효) + 최후통첩·평판.
public static class Spec1009Checks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static BuildingConstructionSite Site(Vector3 at, float work)
    {
        var go = new GameObject("Spec1009 site"); go.transform.position = at;
        var site = go.AddComponent<BuildingConstructionSite>(); site.Initialize(null, work); return site;
    }
    public static async Task<string> Main()
    {
        checks = 0; var previousRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Spec1009-" + Guid.NewGuid().ToString("N"));
        try
        {
            Check(Application.isPlaying, "play mode"); await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261009, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            var rm = ResourceManager.Instance;
            foreach (Resource type in new[] { Resource.Food, Resource.Soil, Resource.Special }) { rm.AddCapacity(type, 20000); rm.Add(type, 5000); } // 재료 종류는 한도를 공유하므로 기본 3종만(2026-10-10)
            var list = CommanderRoster.Instance.Commanders.Where(x => x.IsColonyMember).ToList();
            foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var a = list[0]; var b = list[1];

            // 1·3. 정지(식사 등)해도 예정지·진행도 유지, 여러 장수가 함께 짓는다.
            var site = Site(a.Position, 1000);
            a.CommandBuild(site); Check(a.ConstructionTarget == site, "first builder");
            Time.timeScale = 1; await Task.Delay(1500);
            var r0 = site.RemainingWork; await Task.Delay(1000); var solo = r0 - site.RemainingWork;
            b.Agent.Warp(a.Position); b.CommandBuild(site); Check(b.ConstructionTarget == site, "second builder joins same blueprint");
            await Task.Delay(1500); var r1 = site.RemainingWork; await Task.Delay(1000); var pair = r1 - site.RemainingWork;
            Check(solo > 0 && pair > solo * 1.3f, $"co-building adds work speed ({solo:0.00} -> {pair:0.00})");
            a.CommandStop(); await Task.Delay(300);
            Check(site != null && a.ConstructionTarget == null && b.ConstructionTarget == site, "stopping one builder keeps blueprint");
            var r2 = site.RemainingWork; await Task.Delay(800); Check(site.RemainingWork < r2, "other builder continues");
            b.CommandStop(); Time.timeScale = 0; Check(site != null && site.RemainingWork > 0, "stop never cancels prepaid blueprint");
            // 플레이어 취소 = 건설비 전액 반환.
            site.SetRefund(10, 20, 3); var food = rm.GetAmount(Resource.Food); var soil = rm.GetAmount(Resource.Soil); var special = rm.GetAmount(Resource.Special);
            site.Cancel(); await Task.Delay(50);
            Check(site == null && rm.GetAmount(Resource.Food) == food + 10 && rm.GetAmount(Resource.Soil) == soil + 20 && rm.GetAmount(Resource.Special) == special + 3, "cancel refunds 100%");
            // 창고가 가득 차면 넘치는 반환분은 사라지지 않고 바닥 운반 더미로 남는다.
            var full = Site(a.Position, 1000); full.SetRefund(0, 30, 0);
            var space = rm.GetCapacity(Resource.Soil) - rm.GetAmount(Resource.Soil); rm.Add(Resource.Soil, space - 10);
            var looseBefore = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == Resource.Soil).Sum(n => n.AmountRemaining);
            full.Cancel(); await Task.Delay(50);
            var looseAfter = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == Resource.Soil).Sum(n => n.AmountRemaining);
            Check(rm.GetAmount(Resource.Soil) == rm.GetCapacity(Resource.Soil) && Mathf.Approximately(looseAfter - looseBefore, 20), $"overflow refund dropped near storage ({looseAfter - looseBefore})");
            foreach (var n in ResourceNode.Available.Where(n => n.IsLooseCargo).ToList()) Object.Destroy(n.gameObject);
            rm.TrySpend(0, rm.GetAmount(Resource.Soil) - soil - 20); await Task.Delay(50);
            // 바닥 장비는 운반 작업 장수가 장비 보관함으로 옮긴다.
            var inv = EquipmentInventory.Instance; Check(inv != null && !inv.Full, "equipment inventory has room");
            var floorGear = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Move };
            var loot = EquipmentLoot.Drop(a.Position + Vector3.right * 4, new[] { floorGear });
            a.SetJobEnabled(CommanderJobs.Hauling, true); a.TickDuty(2);
            Check(loot.Collector == a, "hauler heads to floor equipment");
            Time.timeScale = 1; for (int i = 0; i < 60 && loot != null; i++) await Task.Delay(100); Time.timeScale = 0;
            Check(loot == null && inv.Items.Contains(floorGear), "floor equipment hauled into inventory");
            a.SetJobEnabled(CommanderJobs.Hauling, false); a.CommandStop();

            // 4. 작업 종류 우선순위 5단계: 높은 단계가 작업표 왼쪽 순서보다 먼저.
            var node = ResourceNode.Available.Where(n => n.CanGather && CommanderAnt.JobFor(n) == CommanderJobs.Gathering && n.GetComponentInParent<ExpeditionSite>() == null && !n.IsRaidLoot && a.TryWorkApproach(n.transform.position, out _))
                .OrderBy(n => (n.transform.position - a.Position).sqrMagnitude).First();
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = n != node;
            var job = CommanderJobs.Gathering;
            var prioritySite = Site(a.Position, 50);
            Check(a.SetJobPriority(CommanderJobs.Building, 1) && a.SetJobPriority(job, 5) && a.JobPriority(job) == 5 && a.JobPriority(CommanderJobs.Building) == 1, "set priorities");
            a.TickDuty(2); Check(a.CurrentResourceNode == node && a.ConstructionTarget == null, "higher priority gathering beats left-side building");
            a.CommandStop(); a.SetJobPriority(CommanderJobs.Building, 5); a.SetJobPriority(job, 2); a.TickDuty(2);
            Check(a.ConstructionTarget == prioritySite, "higher priority building chosen");
            a.CommandStop(); Check(a.SetJobPriority(job, 0) && !a.AllowsJob(job) && a.JobPriority(job) == 0, "priority 0 forbids");
            Check(a.SetJobEnabled(job, true) && a.JobPriority(job) == CommanderWorkState.DefaultPriority, "re-enabled job is normal");
            var ws = Copy(a.WorkState); Check(ws.Valid && ws.Priority(CommanderJobs.Building) == 5, "priorities serialize");
            ws.priorities = null; Check(ws.Valid && ws.Priority(CommanderJobs.Building) == 3, "legacy save defaults to normal");
            ws.priorities = new int[CommanderWorkState.JobCount]; ws.priorities[0] = 9; Check(!ws.Valid, "invalid priority rejected");
            Object.Destroy(prioritySite.gameObject); foreach (var x in list) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            foreach (var n in ResourceNode.Available) n.GatheringForbidden = false;

            // 5. PA 한도에서 주·보조가 함께 성장(주 능력이 여유를 독점하지 않음).
            var t = new CommanderTalents(); t.potential = 1;
            t.Train(CommanderActivity.Gathering, 200, _ => 1);
            Check(t.Current <= 1.01f && t.Value((int)CommanderActivity.Strength) > .1f && t.Value((int)CommanderActivity.Gathering) > t.Value((int)CommanderActivity.Strength),
                $"main/support share PA room ({t.Value((int)CommanderActivity.Gathering):0.00}/{t.Value((int)CommanderActivity.Strength):0.00})");

            // 2. 최고 세율 유지는 이탈 없이 버틸 수 없다.
            var pop = ColonyPopulation.Instance; pop.SetTaxRate(GameBalance.MaxTaxRate); pop.TrySetPolicy(MilitaryPolicy.Volunteer);
            pop.S.sentiment = 60; var before = pop.Total;
            for (int i = 0; i < 8; i++) pop.Monthly();
            Check(pop.S.sentiment <= GameBalance.UnrestSentiment + 10.5f, "max tax drags sentiment low: " + pop.S.sentiment);
            pop.S.sentiment = 60; var free = AntPool.Instance.Free; pop.S.young = 0;
            var housingFull = pop.HousingCapacity <= pop.Total;
            pop.Monthly();
            Check(ToastManager.VisibleLines.Any(l => l.Contains("높은 세금")) || housingFull && AntPool.Instance.Free < free, "high tax flight even without unrest");
            pop.SetTaxRate(.2f);

            // 외교 준비.
            var world = WorldMapManager.Instance; var d = DiplomacyManager.Instance;
            typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(world, new object[] { true });
            var civSite = world.Sites.First(s => d.Faction(s) != null); var c = d.Faction(civSite); d.Contact(civSite);
            var holdSite = world.Sites.First(s => s.Defense == null && d.Faction(s) == null && s.Kind == ExpeditionSiteKind.ResourceSite); // 억류 장소(상실한 편입 거점 역할)
            typeof(ExpeditionSite).GetMethod("RestoreState", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(holdSite, new object[] { true, ConquestDisposition.Lost });
            var c2 = d.Data.civilizations.First(x => x != c && !x.extinct); d.Contact(world.Sites.First(s => d.Faction(s) == c2));
            c.affinity = 0; c.resources = new[] { 10000, 10000, 1000 };
            Check(DiplomacyManager.CanNegotiate(a) && !DiplomacyManager.CanNegotiate(null), "negotiator eligibility");

            // 협상: 담당 장수 필수 → 좋은 조건은 정치력과 무관하게 성사.
            var n1 = d.StartTalk(c, TalkKind.Trade); n1.give.resources[0] = 200; n1.take.resources[2] = 1;
            Check(d.Propose(n1).Contains("담당"), "negotiator required");
            var low = list.OrderBy(x => x.Talents.Level(CommanderActivity.Politics)).First(DiplomacyManager.CanNegotiate);
            n1.negotiator = low.PersonalState.id; special = rm.GetAmount(Resource.Special);
            Check(d.Reaction(c, n1) == TalkReaction.Warm, "good offer reads warm");
            Check(d.Propose(n1) == "수락" && !d.Data.talks.Contains(n1) && rm.GetAmount(Resource.Special) == special + 1, "good offer accepted by low politics negotiator");
            // 역제안: 부족한 가치를 우리 자원으로 채워 다시 제시.
            var n2 = d.StartTalk(c, TalkKind.Trade); n2.negotiator = a.PersonalState.id; n2.give.resources[0] = 80; n2.take.resources[1] = 100;
            var counter = d.Propose(n2);
            Check(counter.StartsWith("역제안") && n2.give.resources[0] > 80 && n2.patience < 100, "counter offer raises our side: " + counter);
            Check(d.Propose(n2) == "수락", "accepting counter terms closes deal");
            // 무리한 제안 반복 → 인내 소진 → 결렬·재협상 대기 → 크게 개선하면 바로 재개.
            var n3 = d.StartTalk(c, TalkKind.Trade); n3.negotiator = a.PersonalState.id; n3.give.resources[0] = 1; n3.take.resources[0] = 500;
            var reply = ""; for (int i = 0; i < 10 && d.Data.talks.Contains(n3); i++) reply = d.Propose(n3);
            Check(!d.Data.talks.Contains(n3) && c.talksCooldown > d.Data.elapsed && reply.Contains("결렬"), "patience runs out");
            var n4 = d.StartTalk(c, TalkKind.Trade); n4.negotiator = a.PersonalState.id; n4.give.resources[0] = 100; n4.take.resources[0] = 90;
            Check(d.Propose(n4).Contains("대기"), "cooldown blocks ordinary offer");
            n4.give.resources[0] = 1000; Check(d.Propose(n4) == "수락", "much better offer reopens talks early");
            // 받는 자원이 창고를 넘치면 주변 바닥에 둔다.
            var room = rm.GetCapacity(Resource.Soil) - rm.GetAmount(Resource.Soil);
            c.resources[1] = room + 100; var n5 = d.StartTalk(c, TalkKind.Trade); n5.negotiator = a.PersonalState.id; n5.take.resources[1] = room + 30; n5.give.resources[2] = (room + 30) / 5 + 50;
            Check(d.Propose(n5) == "수락" && rm.GetAmount(Resource.Soil) == rm.GetCapacity(Resource.Soil), "storage filled");
            Check(Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).Any(r => r.IsLooseCargo && r.ResourceType == Resource.Soil && Mathf.Approximately(r.AmountRemaining, 30)), "overflow left on floor for haulers");

            // 포로: 자국 장수 억류 → 부상 판정·장비 압수 → 몸값으로 석방 → 거리 비례 안전 귀환(장비 함께 도착).
            int injured = 0; for (int i = 0; i < 100; i++) { var s = new CommanderPersonalState(); DiplomacyManager.CaptureInjury(s, CommanderTraits.Random()); if (s.injuries.Count > 0) injured++; }
            Check(injured >= 60, "capture injures most healthy commanders: " + injured);
            var hurt = new CommanderPersonalState(); hurt.AddInjury(CommanderTraits.Random(), true); var count = hurt.injuries.Count;
            DiplomacyManager.CaptureInjury(hurt, CommanderTraits.Random()); Check(hurt.injuries.Count == count, "already injured keeps existing injuries only");
            var captive = list[2]; captive.CommandStop(); captive.PersonalState.equipment.Clear();
            var gear = EquipmentRecipes.Create(EquipmentRecipe.Anklet, 2); captive.PersonalState.equipment.Add(gear);
            typeof(SettlementDefense).GetMethod("RestorePrisoner", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(holdSite.Defense, new object[] { captive });
            d.CapturedBy(captive, c); var injuries = captive.PersonalState.injuries.Count; var captiveId = captive.PersonalState.id; var captiveName = captive.CommanderName;
            Check(captive.PersonalState.equipment.Count == 0 && c.equipment.Contains(gear) && d.SeizedOwnerName(gear.id) == captiveName, "equipment confiscated by captor");
            Check(d.RansomableIds(c).Contains(captiveId) && !d.RansomableIds(c2).Contains(captiveId), "only captor can ransom");
            Check(d.RansomValue(captiveId, c) > 0, "ransom value");
            // 제3자 포로 금지: c2 출신 포로는 c에게 넘길 수 없다.
            var camp = Build<BuildingBase>(BuildingKind.PrisonerCamp, a.Position + Vector3.forward * 8).GetComponent<PrisonerCamp>(); await Task.Delay(50);
            Check(camp.TryCapture("Foreign", CommanderRank.Sergeant, new[] { UnitRole.Melee }, CommanderTraits.Random()), "camp capture");
            var foreign = camp.Prisoners.Last(); foreign.PersonalState.originFaction = c2.id;
            Check(!DiplomacyManager.ReleasableTo(c).Contains(foreign) && DiplomacyManager.ReleasableTo(c2).Contains(foreign), "own-faction release only");
            var bad = d.StartTalk(c, TalkKind.Demand); bad.take.prisoners.Add(foreign.PersonalState.id); bad.negotiator = a.PersonalState.id;
            Check(d.TradeReason(c, bad.give, bad.take, false).Contains("포로"), "third-party prisoner purchase rejected"); d.EndTalk(bad, null);
            // 무효: 거래 대상 포로가 사라지면 무효.
            var stale = d.StartTalk(c2, TalkKind.Trade); stale.give.prisoners.Add(foreign.PersonalState.id);
            camp.Execute(camp.Prisoners.Count - 1); Check(d.CheckInvalid(stale), "executed prisoner invalidates talk");
            GameMenuController.Instance.OpenTalk(stale); await Task.Delay(50);
            Check(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(x => x.name == "Talk Confirm"), "invalid talk shows confirm only");
            GameMenuController.Instance.Resume(); d.EndTalk(stale, null);
            // 몸값 거래.
            var ransom = d.StartTalk(c, TalkKind.Trade); ransom.negotiator = a.PersonalState.id;
            ransom.take.prisoners.Add(captiveId); ransom.take.equipment.Add(gear.id); ransom.give.resources[0] = 3000;
            var eta = d.HomecomingSeconds(captiveId, c);
            Check(eta >= DiplomacyManager.HomecomingBaseSeconds, "homecoming at least 3 months");
            Check(d.Propose(ransom) == "수락", "ransom accepted"); await Task.Delay(50);
            Check(captive == null && d.Data.homecomings.Count == 1 && d.Data.homecomings[0].gear.Contains(gear) && !EquipmentInventory.Instance.Items.Contains(gear), "released commander travels home with returned gear");
            Check(!CommanderRoster.Instance.Commanders.Any(x => x.PersonalState.id == captiveId), "travelling commander not in colony");
            d.Data.nextMonth = d.Data.elapsed + 100000; d.Data.elapsed = d.Data.homecomings[0].arrive - 1; d.Tick(.5f);
            Check(d.Data.homecomings.Count == 1, "not home before eta"); d.Tick(1f);
            var back = CommanderRoster.Instance.Commanders.FirstOrDefault(x => x.PersonalState.id == captiveId);
            Check(back != null && back.PersonalState.equipment.Contains(gear) && back.PersonalState.injuries.Count == injuries && d.Data.homecomings.Count == 0, "commander arrives injured with gear");
            // 리뷰(10-10): 적에게 돌려준 포로도 같은 규칙으로 귀환하고 도착해야 그 세력으로 복귀한다(압수 장비 동반).
            Check(camp.TryCapture("Returner", CommanderRank.Sergeant, new[] { UnitRole.Melee }, CommanderTraits.Random()), "camp capture 2");
            var ret = camp.Prisoners.Last(); ret.PersonalState.originFaction = c.id;
            var retGear = EquipmentRecipes.Create(EquipmentRecipe.Anklet, 1); ret.PersonalState.equipment.Add(retGear); d.Seized(ret, camp.transform.position);
            Check(EquipmentInventory.Instance.Items.Contains(retGear), "enemy gear seized");
            var release = new TradeOffer(); release.prisoners.Add(ret.PersonalState.id); release.equipment.Add(retGear.id);
            Check(d.TryTrade(c, release, new TradeOffer(), false, out var releaseError), "release trade " + releaseError);
            var trip = d.Data.homecomings.SingleOrDefault(h => h.commander == ret);
            Check(trip != null && trip.to == c.id && trip.gear.Contains(retGear) && !camp.Prisoners.Contains(ret) && !c.prisoners.Contains(ret) && !c.equipment.Contains(retGear), "released enemy travels home with gear");
            Check(trip.arrive - d.Data.elapsed >= DiplomacyManager.HomecomingBaseSeconds, "enemy homecoming uses same 3-month rule");
            d.Data.elapsed = trip.arrive - 1; d.Tick(.5f); Check(!c.prisoners.Contains(ret), "enemy not home before eta");
            d.Tick(1f); Check(c.prisoners.Contains(ret) && c.equipment.Contains(retGear) && !d.Data.homecomings.Contains(trip), "enemy commander arrives at own faction");

            // 상대 요청: 공물. 줄인 역제안은 거절, 원래 양은 수락. 거절하면 관계 악화.
            var request = typeof(DiplomacyManager).GetMethod("Request", BindingFlags.NonPublic | BindingFlags.Instance);
            c2.resources = new[] { 1000, 1000, 100 };
            request.Invoke(d, new object[] { c2, TalkKind.Tribute, 100 }); var tribute = d.Talks(c2).First(x => x.kind == TalkKind.Tribute);
            tribute.negotiator = a.PersonalState.id; Check(d.RefusalRisk(tribute).Contains("공격 위험"), "refusal risk shown");
            tribute.give.resources[0] = 40; Check(d.Respond(tribute, true).Contains("역제안 거절") && d.Data.talks.Contains(tribute), "too small counter refused");
            tribute.give.resources[0] = 100; food = rm.GetAmount(Resource.Food);
            Check(d.Respond(tribute, true) == "수락" && rm.GetAmount(Resource.Food) == food - 100, "tribute paid");
            request.Invoke(d, new object[] { c2, TalkKind.Tribute, 100 }); var aff = c2.affinity; var refuse = d.Talks(c2).First();
            refuse.negotiator = a.PersonalState.id; d.Respond(refuse, false); Check(c2.affinity <= aff - 10, "tribute refusal hurts relations");

            // 보류: 협상 페이지는 게임을 멈추고 닫으면 이전 속도로, 오른쪽 알림에 고정.
            var held = d.StartTalk(c, TalkKind.Trade); held.negotiator = a.PersonalState.id; held.give.resources[0] = 10;
            Time.timeScale = 2; GameMenuController.Instance.OpenTalk(held); Check(Time.timeScale == 0, "negotiation pauses game");
            GameMenuController.Instance.Resume(); Check(Time.timeScale == 2, "closing restores speed"); Time.timeScale = 0;
            await Task.Delay(600);
            Check(d.Data.talks.Contains(held) && ToastManager.PinnedKeys.Contains("talks:" + c.id), "held talk pinned in notifications");
            Check(ToastManager.VisibleLines.Any(l => l.Contains(c.name)), "pinned notice visible");

            // 최후통첩: 거절 → 기한 안 실행/철회. 철회는 위협 신뢰 하락, 기한 초과는 철회, 실행은 선전포고.
            c.threatCred = 0; var other = d.Data.civilizations.First(x => x != c && !x.extinct); var otherCred = other.threatCred;
            var ult = d.StartTalk(c, TalkKind.Demand); ult.negotiator = a.PersonalState.id; ult.take.resources[0] = 10000; ult.ultimatum = true; c.threatCred = -100;
            d.Propose(ult); Check(ult.deadline > d.Data.elapsed, "rejected ultimatum opens decision window");
            c.threatCred = 0; d.WithdrawUltimatum(ult); Check(c.threatCred == -20 && other.threatCred == otherCred - 5 && !d.Data.talks.Contains(ult), "withdraw costs threat credibility");
            var late = d.StartTalk(c, TalkKind.Demand); late.negotiator = a.PersonalState.id; late.take.resources[0] = 10000; late.ultimatum = true; c.threatCred = -100;
            d.Propose(late); d.Data.elapsed = late.deadline; d.Tick(.1f);
            Check(!d.Data.talks.Contains(late) && d.Data.reputation.Any(r => r.Contains("철회")), "expired ultimatum counts as withdrawal");
            Check(d.ReputationSummary().Contains("·"), "reputation described");
            // 저장: 협상·평판·압수 기록 보존.
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "snapshot valid " + error);
            Check(file.diplomacy.talks.Any(x => x.id == held.id) && file.diplomacy.reputation.Count > 0, "talks and reputation saved");
            var broken = Copy(file); broken.diplomacy.talks[0].patience = 999; Check(!SaveValidator.Validate(broken, out _), "invalid talk rejected");
            var exec = d.StartTalk(c, TalkKind.Demand); exec.negotiator = a.PersonalState.id; exec.take.resources[0] = 10000; exec.ultimatum = true; c.threatCred = -100;
            d.Propose(exec); d.ExecuteUltimatum(exec); Check(c.war && c.threatCred == -85, "executing ultimatum declares war and restores credibility");
            return "PASS " + checks + " Spec1009 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = previousRoot; }
    }
}
