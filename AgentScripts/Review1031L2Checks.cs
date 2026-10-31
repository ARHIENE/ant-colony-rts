using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using AntColony.Boss;
using AntColony.Core;
using AntColony.World;

public static class Review1031L2Checks
{
    private static int checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        var c = new Civilization();
        Check(AIEraProgress.Valid(c), "구저장 시대 기본값");
        Check(!AIEraProgress.Tick(c, 60, 0) && c.resources[0] == 300, "생존 건물 없으면 연구 정지");
        for (int era = 1; era < 6; era++)
        {
            c.resources[0] = c.resources[1] = 10000;
            Check(!AIEraProgress.Tick(c, 1, 1) && c.eraPaid && c.resources[0] == 10000 - AIEraProgress.Cost(era), "연구비 1회 결제");
            Check(!AIEraProgress.Tick(c, 0, 1), "정지 중 연구 없음");
            Check(AIEraProgress.Tick(c, AIEraProgress.Work(era) - 1, 1) && c.era == era + 1, "6시대 순차 발전");
            Check(!c.eraPaid && c.eraWork == 0 && AIEraProgress.Valid(c), "완료 상태 유효");
        }
        Check(!AIEraProgress.Tick(c, 10000, 5) && c.era == 6, "미래 이후 승급 없음");
        Check(AIEraProgress.AttackBonus(1) == 0 && AIEraProgress.AttackBonus(6) == 10 && AIEraProgress.ArmorBonus(6) == 5, "시대 전투 보정");
        foreach (float bad in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            var a = new Civilization { eraWork = bad }; Check(!AIEraProgress.Valid(a), "손상 연구량 거부");
            Check(!AIEraProgress.Tick(new Civilization(), bad, 1), "잘못된 시간 무시");
        }
        var poor = new Civilization { resources = new[] { 39, 300, 100 } };
        Check(!AIEraProgress.Tick(poor, 600, 1) && poor.resources[1] == 300 && !poor.eraPaid, "연구비 부족 부분결제 없음");
        var interrupted = new Civilization(); AIEraProgress.Tick(interrupted, 100, 1);
        var balance = interrupted.resources[0]; AIEraProgress.Tick(interrupted, 100, 0);
        Check(interrupted.eraWork == 100 && interrupted.resources[0] == balance, "건물 소실 연구 보존");
        AIEraProgress.Tick(interrupted, 500, 1); Check(interrupted.era == 2 && interrupted.resources[0] == balance, "연구 재개 중복 비용 없음");
        c.resources[0] = int.MaxValue; AIEraProgress.Credit(c, 5); Check(c.resources[0] == int.MaxValue, "외교 재고 오버플로 없음");
        var history = new CampaignHistory.State();
        for (int i = 0; i < 100000; i++) history.milestones.Add(new CampaignHistory.Entry { seconds = i, kind = i % 2 == 0 ? "전투" : "생산", name = "사건" + i, result = "결과" });
        CampaignHistory.Compact(history);
        Check(history.milestones.Count == 1500 && history.summaries.Sum(a => a.count) == 98500, "기록 개수 보존 집계");
        Check(history.summaries.Count == 2 && CampaignHistory.Validate(history), "집계 저장 유효");
        Check(history.summaries[0].first == 0 && history.summaries[0].last == 98498, "최초/최종 시각 보존");
        CampaignHistory.Compact(history); Check(history.summaries.Sum(a => a.count) == 98500, "압축 반복 중복 없음");
        var s = new BossProgress.State(); var p = new BossProgress(s);
        for (int i = 0; i < BossProgress.GuardRewardsPerCycle; i++) Check(p.ClaimGuardXp(BossId.B, 0) == 25, "회차 XP 지급");
        Check(p.ClaimGuardXp(BossId.B, 0) == 0 && p.ClaimGuardXp(BossId.B, 1) == 0, "예산 초과/다른 회차 거부");
        Check(BossProgress.Validate(s), "호위 예산 저장 유효");
        s.guardRewards[1]++; Check(!BossProgress.Validate(s), "손상 예산 거부");
        var mixed = new BossProgress.State(); var mixedProgress = new BossProgress(mixed);
        Check(mixedProgress.ClaimGuardExperience(BossId.C, 0, 190) == 190 && mixedProgress.ClaimGuardXp(BossId.C, 0) == 10
            && mixedProgress.ClaimGuardExperience(BossId.C, 0, 10) == 0, "호위 시간/처치 XP 공유 예산");
        mixed.revealed[2] = true; Check(mixedProgress.RecordKill(BossId.C, 1), "보스 처치");
        Check(!mixedProgress.TryRespawn(BossId.C, BossProgress.RespawnSeconds + 1, true, false, false) && mixed.guardRewards[2] == 200, "아군 잔류 예산 리셋 없음");
        Check(mixedProgress.TryRespawn(BossId.C, BossProgress.RespawnSeconds + 1, false, false, false)
            && mixed.guardRewards[2] == 0 && mixedProgress.ClaimGuardXp(BossId.C, 0) == 0
            && mixedProgress.ClaimGuardXp(BossId.C, 1) == 25, "실제 다음 회차만 예산 갱신");
        Check(BossProgress.Validate(mixed), "회차 전환 저장 유효");
        Check(ExpeditionTransport.ExpectedFood(0, 10, .5f) == 1, "남은 식량 부채 반영");
        Check(ExpeditionTransport.ExpectedFood(GameCalendar.SecondsPerDay, 10, .5f) == 11, "예상 소비 올림");
        OwnershipChecks();
        ConstructionCraftChecks();
        return $"L2 오프라인 {checks}검사 통과";
    }
    private static void ConstructionCraftChecks()
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var yard = (AirshipYard)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AirshipYard));
        var state = new AirshipYard.State(); typeof(AirshipYard).GetField("state", instance).SetValue(yard, state);
        var work = new WorkContribution(); work.Add("A", 74.9f, true); work.Add("B", 25.1f, true);
        var foundation = new BabelState { work = 100 };
        var accept = typeof(AirshipYard).GetMethod("AcceptBabelFoundation", instance);
        accept.Invoke(yard, new object[] { foundation, work }); Check(!state.babelEligible && state.builders.Count == 2, "로켓 공통 시공자 기록 보존");
        work.Add("A", .1f, true); foundation.eligible = work.Majority(100) != null;
        accept.Invoke(yard, new object[] { foundation, work });
        Check(state.babelEligible && state.builders[0].id == "A" && state.builders[0].work == 75, "실제 단일 시공자75% 연결");
        Check(AirshipYard.Validate(state, 0, out _), "공통 바벨→로켓 저장 유효");
        foundation.used = true; accept.Invoke(yard, new object[] { foundation, work });
        Check(state.foundationUsed, "재건 이력 공통 상태 연결");
        var bench = (Workshop)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Workshop));
        var job = new Workshop.Job { bossJob = true, boss = 3, recovery = true };
        typeof(Workshop).GetField("jobs", instance).SetValue(bench, new List<Workshop.Job> { job });
        var required = typeof(Workshop).GetProperty("RequiredInputs", instance);
        (AntColony.Data.ResourceType type, int amount)[] Requirements() => ((IEnumerable<(AntColony.Data.ResourceType, int)>)required.GetValue(bench)).ToArray();
        var inputs = Requirements();
        Check(inputs.Length == 2 && inputs.Any(i => i.type == AntColony.Data.ResourceType.Steel && i.amount == 100)
            && inputs.Any(i => i.type == AntColony.Data.ResourceType.Component && i.amount == 10), "복구 제작 실물 배송 요구");
        job.materialsPaid = true; Check(Requirements().Length == 0, "지불 후 재배송 없음");
        job.materialsPaid = false; job.recovery = false; Check(Requirements().Length == 0, "D탐지 핵심물 비용과 일반재료 분리");
        job.boss = 1; Check(Requirements().Sum(i => i.amount) == 10, "B탐지 목재/돌 배송");
        var data = new BossProgress.State { craftBoss = 3, craftRecovery = true, craftRemaining = 60 }; var progress = new BossProgress(data);
        var setWork = typeof(BossProgress).GetMethod("SetCraftWork", instance);
        setWork.Invoke(progress, new object[] { 59.999f }); Check(progress.Home(BossId.D) == 0, "미완료 핵심물 지급 없음");
        setWork.Invoke(progress, new object[] { 60f }); Check(progress.Home(BossId.D) == 1 && data.craftBoss == -1, "제작 완료 정확히 1회");
        setWork.Invoke(progress, new object[] { 60f }); Check(progress.Home(BossId.D) == 1, "완료 중복 호출 보상 없음");
        data.craftBoss = 1; data.craftRecovery = false; data.craftRemaining = 60;
        progress.CancelCraft(); Check(data.home[0] == 1 && data.craftBoss == -1 && data.craftRemaining == 0, "주문 취소 핵심물 반환");
        progress.CancelCraft(); Check(data.home[0] == 1, "중복 취소 반환 없음");
        var valid = typeof(ColonyPopulation).GetMethod("ValidCivilianState", BindingFlags.Static | BindingFlags.NonPublic);
        var pop = new ColonyPopulation.State { nextCivilianLocation = 2 };
        pop.civilianLocations.Add(new CivilianLocation { id = 1, count = 5, temporary = true, moving = true });
        Check((bool)valid.Invoke(null, new object[] { pop }), "임시 거처 이동 저장");
        pop.civilianLocations[0].housing = true; Check(!(bool)valid.Invoke(null, new object[] { pop }), "임시/영구 주거 중복 거부");
        pop.civilianLocations[0].housing = false; pop.civilianLocations[0].moving = false; pop.civilianLocations[0].arrived = true; pop.civilianLocations[0].sheltered = true;
        Check((bool)valid.Invoke(null, new object[] { pop }), "임시 거처 도착 보호 저장");
        var resident = pop.civilianLocations[0]; resident.temporary = resident.sheltered = false; resident.rehousing = true;
        resident.housingDestination = new Vector3(float.NaN, 0, 0);
        Check(!(bool)valid.Invoke(null, new object[] { pop }), "손상 주거 목적지 거부");
        resident.housingDestination = new Vector3(4, 0, 4);
        Check((bool)valid.Invoke(null, new object[] { pop }), "NavMesh 도착점과 주거 목적지 분리 저장");
    }
    private static void OwnershipChecks()
    {
        var f = new SaveFileV1();
        Check(SaveL2.Valid(f), "빈 구저장 추가 소유권 유효");
        var c = new CommanderDto { personalState = new CommanderPersonalState() }; c.personalState.id = "l2-test";
        f.commanders.Add(c);
        var a = new BuildingDto { kind = "AirshipYard", health = 100, airship = new AirshipYard.State() };
        var b = new BuildingDto { kind = "AirshipYard", health = 100, airship = new AirshipYard.State() };
        f.buildings.Add(a); f.buildings.Add(b);
        a.airship.reservedCommanders.Add("l2-test"); Check(SaveL2.Valid(f), "한 발사대 장수 예약");
        b.airship.reservedCommanders.Add("l2-test"); Check(!SaveL2.Valid(f), "두 로켓 장수 중복 거부"); b.airship.reservedCommanders.Clear();
        c.personalState.dead = true; Check(!SaveL2.Valid(f), "사망 예약 거부"); c.personalState.dead = false;
        c.location = 1; Check(!SaveL2.Valid(f), "원정 중 장수 예약 거부"); c.location = 0;
        a.airship.passengers.Add(0); Check(!SaveL2.Valid(f), "이미 탑승한 장수 예약 거부"); a.airship.passengers.Clear();
        b.airship.passengers.Add(0); Check(!SaveL2.Valid(f), "다른 로켓 탑승과 예약 중복 거부"); b.airship.passengers.Clear();
        a.airship.phase = RocketPhase.Launched; Check(!SaveL2.Valid(f), "발사 후 잔류 예약 거부"); a.airship.phase = RocketPhase.Construction;
        var group = new CivilianLocation { id = 1, count = 10, moving = true };
        f.population.civilianLocations.Add(group); a.airship.reservedGroups.Add(1);
        Check(SaveL2.Valid(f), "이동 중 시민 목적지 예약");
        group.moving = false; Check(!SaveL2.Valid(f), "이동/도착 전 시민 예약 거부"); group.moving = true;
        b.airship.reservedGroups.Add(1); Check(!SaveL2.Valid(f), "시민 중복 예약 거부"); b.airship.reservedGroups.Clear();
        group.count = 0; Check(!SaveL2.Valid(f), "소실 시민 예약 거부"); group.count = 10;
        group.temporary = true; Check(!SaveL2.Valid(f), "임시 거처 이동과 로켓 예약 중복 거부"); group.temporary = false;
        a.health = 0; Check(!SaveL2.Valid(f), "파괴 발사대 예약 거부"); a.health = 100;
        var bench = new BuildingDto { kind = "Workshop" }; f.buildings.Add(bench);
        var job = new Workshop.Job { bossJob = true, boss = 1, recovery = true, work = 20, materialsPaid = true, makerId = "l2-test" };
        bench.workshop.jobs.Add(job); f.campaign.bosses.craftBoss = 1; f.campaign.bosses.craftRemaining = 40; f.campaign.bosses.craftRecovery = true;
        Check(SaveL2.Valid(f), "제작대/글로벌 진행 동일 소유권");
        Check(job.Copy().bossJob && job.Copy().boss == 1 && job.Copy().work == 20 && job.Copy().materialsPaid, "작업 캡처/복원 복사 필드");
        f.campaign.bosses.craftRemaining = 41; Check(!SaveL2.Valid(f), "제작 진행 불일치 거부"); f.campaign.bosses.craftRemaining = 40;
        bench.workshop.jobs.Add(job.Copy()); Check(!SaveL2.Valid(f), "보스 중복 작업 거부"); bench.workshop.jobs.RemoveAt(1);
        var enemy = new MonsterDto { guardVersion = 1, hasRuntime = true, runtime = new WildMonsterDto { parentSite = 0 }, guardBoss = 1, guardCycle = 0 };
        f.monsters.Add(enemy); Check(SaveL2.Valid(f), "호위 회차 저장");
        enemy.guardCycle = 1; Check(!SaveL2.Valid(f), "미래 호위 회차 거부"); enemy.guardCycle = 0;
        f.diplomacy = DiplomacyManager.InitialState(1, false); f.diplomacy.civilizations[0].era = 0;
        a.airship.reservedCommanders = null; a.airship.reservedGroups = null; f.history.summaries = null; f.campaign.bosses.guardRewards = null;
        SaveL2.Normalize(f);
        Check(a.airship.reservedCommanders.Count == 0 && a.airship.reservedGroups.Count == 0 && f.history.summaries.Count == 0
            && f.campaign.bosses.guardRewards.Length == 4 && f.diplomacy.civilizations[0].era == 1, "구저장 누락 기본값 연결");
        Check(SaveL2.Valid(f), "기본값 복구 후 소유권 유효");
        f.campaign.bosses.guardRewards = new int[0]; enemy.guardVersion = 0; enemy.guardBoss = 0; enemy.guardCycle = 0;
        SaveL2.Normalize(f);
        Check(f.campaign.bosses.guardRewards.Length == 4 && enemy.guardBoss == -1, "JsonUtility 구저장 0·빈 배열 기본값");
    }
    private static async Task Drain(IEnumerator routine)
    {
        while (routine.MoveNext())
            if (routine.Current is IEnumerator nested) await Drain(nested); else await Task.Yield();
    }
    public static async Task<string> RunScene()
    {
        Check(Application.isPlaying, "격리 새 게임 Play 필요");
        var before = SaveSnapshot.Capture();
        Check(SavePreflight.Validate(before, out var error), "장면 baseline: " + error);
        var restore = typeof(SaveSnapshot).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
        float speed = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            Check(SaveValidator.TryParse(JsonUtility.ToJson(before), out var parsed, out error), "실제 JSON: " + error);
            await Drain((IEnumerator)restore.Invoke(null, new object[] { parsed }));
            var after = SaveSnapshot.Capture();
            Check(SavePreflight.Validate(after, out error), "실제 복원 Preflight: " + error);
            Check(JsonUtility.ToJson(before.campaign.bosses) == JsonUtility.ToJson(after.campaign.bosses), "보스 예산/제작 진행 왕복");
            Check(before.history.summaries.Sum(s => s.count) == after.history.summaries.Sum(s => s.count), "압축 기록 왕복");
            Check(before.diplomacy.civilizations.Select(c => (c.era, c.eraWork, c.eraPaid)).SequenceEqual(after.diplomacy.civilizations.Select(c => (c.era, c.eraWork, c.eraPaid))), "AI 연구 왕복");
            return "L2 실제 저장 왕복 통과. 실행 잔여: 실물 배송/담당 작업, 바벨 재건, 현지 장비 교환, 시민 이동/입주, 두 발사대 예약, 화면 폭·전투 밸런스.";
        }
        finally { await Drain((IEnumerator)restore.Invoke(null, new object[] { before })); Time.timeScale = speed; }
    }
}
