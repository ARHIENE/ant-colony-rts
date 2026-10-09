using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using RT = AntColony.Data.ResourceType;
using System.Reflection;

// Phase 4: 인구·이주·세금·병역·민심·나이, 여왕방 삭제(비축더미).
public static class Phase4Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static void Grant(params ScienceTechnology[] techs)
    {
        var s = CampaignResearch.Instance.CaptureState();
        foreach (var t in techs) if (!s.completed.Contains((int)t)) s.completed.Add((int)t);
        CampaignResearch.Instance.RestoreState(s);
    }
    public static async Task<string> Main()
    {
        checks = 0;
        if (!Application.isPlaying) throw new Exception("Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
        Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
        var pop = ColonyPopulation.Instance; var pool = AntPool.Instance; var rm = ResourceManager.Instance;
        Check(pop != null && pool != null, "population attached to ant pool");

        // 여왕방 삭제 → 비축더미(반납 지점), 생산·말벌 없음.
        var stockpile = Object.FindAnyObjectByType<Stockpile>();
        Check(stockpile != null && Type.GetType("AntColony.Buildings.QueenChamber, Assembly-CSharp") == null, "queen replaced by stockpile");
        Check(!ColonyEvents.Instance.TryTrigger(ColonyEvent.Wasps), "wasp event removed");

        // 세금(2026-10-08): 납세 시민 × 세율 × 월 5 × 시민 생산력 × 징수율(행정), 1% 단위·최대 50%, 주(75초)마다 식량·재료 현물 지급.
        pop.SetTaxRate(.333f); Check(Mathf.Approximately(pop.S.taxRate, .33f), "tax rounds to 1%");
        pop.SetTaxRate(.9f); Check(Mathf.Approximately(pop.S.taxRate, .5f), "tax capped at 50%");
        pop.SetTaxRate(.2f); pop.S.old = 10; pop.SetTaxFocus(TaxFocus.Balanced);
        Check(pop.Taxpayers == pool.Total + 10 && Mathf.Approximately(pop.MonthlyTax, (pool.Total + 10) * .2f * 5f * ColonyPopulation.Productivity * pop.TaxCollection)
            && pop.WeeklyFood == Mathf.RoundToInt(pop.MonthlyTax * .5f / 4f) && pop.WeeklySoil == Mathf.RoundToInt(pop.MonthlyTax * .5f / 4f), "tax formula");
        var food = rm.GetAmount(RT.Food); var soil0 = rm.GetAmount(RT.Soil); var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        typeof(UpkeepManager).GetMethod("RunCycle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(upkeep, null);
        Check(rm.GetAmount(RT.Food) == food, "upkeep cycle no longer pays tax");
        pop.S.monthSeconds = 0; pop.S.taxWeek = 0; pop.S.taxFood = pop.S.taxSoil = 0; var month = pop.MonthlyTax;
        pop.Tick(ColonyPopulation.WeekSeconds / 2f - .01f);
        Check(rm.GetAmount(RT.Food) == food && rm.GetAmount(RT.Soil) == soil0, "no tax before week ends");
        pop.SetTaxFocus(TaxFocus.Food); pop.Tick(ColonyPopulation.WeekSeconds / 2f + .02f);
        float expFood = month * (.5f * .5f + .5f * .8f) / 4f, expSoil = month * (.5f * .5f + .5f * .2f) / 4f;
        Check(Mathf.Abs(rm.GetAmount(RT.Food) - food - expFood) < 1.01f && Mathf.Abs(rm.GetAmount(RT.Soil) - soil0 - expSoil) < 1.01f
            && pop.S.taxFood < 1 && pop.S.taxSoil < 1 && pop.S.taxWeek < 1, $"weekly tax pays focus share by time ({rm.GetAmount(RT.Food) - food} vs {expFood})");
        pop.SetTaxFocus(TaxFocus.Balanced);

        // 병역 제도: 연구 전엔 모병제만, 상한 5/15/25(침입 중)/40%.
        var adults = pool.Total;
        Check(!pop.TrySetPolicy(MilitaryPolicy.Conscription) && pop.MaxSoldiers(false) == Mathf.FloorToInt(adults * .05f), "volunteer default 5%");
        Grant(ScienceTechnology.ConscriptionLaw, ScienceTechnology.ReserveForces, ScienceTechnology.TotalMobilization);
        Check(pop.TrySetPolicy(MilitaryPolicy.Conscription) && pop.MaxSoldiers(false) == Mathf.FloorToInt(adults * .15f), "conscription 15%");
        Check(pop.Taxpayers == adults - pool.Assigned + pop.S.old, "mobilized soldiers excluded from tax");
        pop.TrySetPolicy(MilitaryPolicy.Reserve);
        Check(pop.MaxSoldiers(false) == Mathf.FloorToInt(adults * .05f) && pop.MaxSoldiers(true) == Mathf.FloorToInt(adults * .25f), "reserve 25% only under threat");
        pop.TrySetPolicy(MilitaryPolicy.Total); var halfTax = pop.MonthlyTax;
        Check(pop.MaxSoldiers(false) == Mathf.FloorToInt(adults * .4f) && Mathf.Approximately(halfTax, (adults - pool.Assigned + pop.S.old) * .2f * 5f * .5f * ColonyPopulation.Productivity * pop.TaxCollection), "total mobilization 40%, half tax");
        Check(!pop.TryDraft(pop.MaxSoldiers(false) - pool.Assigned + 1, false), "draft above cap rejected");

        // 병역 나이 확대: 모자란 성체를 늙은 개미로 채운다.
        pop.TrySetPolicy(MilitaryPolicy.Total); pop.S.elderlyService = true; pop.S.old = 30;
        var freeBefore = pool.Free; var drain = pool.RemoveFree(freeBefore, false);
        Check(pop.TryDraft(5, false) && pool.Free == 5 && pop.S.old == 25, "elderly fill draft shortfall");
        pool.Breed(drain - 5); pop.S.elderlyService = false;

        // 민심 바닥: 동원 불가.
        pop.S.sentiment = 10; Check(pop.Unrest && pop.MaxSoldiers(true) == 0, "unrest blocks mobilization");

        // 달마다: 어린 → 성체, 성체 3% 노화, 늙은 15% 사망, 민심 ±10 이내 변화, 수요·자리가 있으면 이주.
        pop.S.sentiment = 60; pop.S.young = 4; pop.S.old = 20; pop.TrySetPolicy(MilitaryPolicy.Volunteer);
        rm.Add(RT.Food, 10000);
        var free0 = pool.Free; var total0 = pool.Total;
        // 성체 전체 3%. 대기 몫만 즉시 늙고 나머지는 복귀 때로 미룬다.
        var dueNow = Mathf.RoundToInt((total0 + 4) * GameBalance.AdultAging);
        var aging = Mathf.Min(dueNow, Mathf.RoundToInt((free0 + 4) * GameBalance.AdultAging)); var due0 = pop.S.agingDue;
        var hut = Object.Instantiate((GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { BuildingKind.Hut, UnitRole.Worker }), stockpile.Position + Vector3.right * 12, Quaternion.identity);
        hut.SetActive(true); await Task.Yield();
        Check(hut.GetComponent<Housing>().Capacity == GameBalance.HutHousing && pop.HousingCapacity == GameBalance.BaseHousing + GameBalance.HutHousing, "hut adds housing");
        var housingRoom = pop.HousingCapacity - (pool.Total + 4 + 20);
        pop.Monthly();
        Check(pop.S.old == 20 + aging - Mathf.RoundToInt(20 * GameBalance.OldDeath), "aging and old death");
        Check(pop.S.agingDue == due0 + dueNow - aging, "aging of deployed ants deferred");
        Check(Mathf.Abs(pop.S.sentiment - 60) <= 10, "sentiment moves at most 10 per month");
        Check(housingRoom <= 0 || pool.Total + pop.S.young > total0 + 4 - aging, "immigration arrives when room and demand");
        Check(pop.Total <= pop.HousingCapacity || housingRoom <= 0, "immigration bounded by housing");

        // 미룬 노화는 돌아온 개미로 전환하고, 남은 동원 수를 넘는 몫(전사 등)은 버린다.
        Check(pool.TryAssign(5), "assign for deferred aging");
        pop.S.agingDue = 3; var old1 = pop.S.old; var free1 = pool.Free;
        pool.ReturnAssigned(2);
        Check(pop.S.old == old1 + 2 && pop.S.agingDue == 1 && pool.Free == free1, "returning ants age on return");
        pop.S.agingDue = 999; pool.ReturnAssigned(3);
        Check(pop.S.old == old1 + 5 && pop.S.agingDue == pool.Total - pool.Free, "deferred aging capped by mobilized adults");
        pop.S.agingDue = 0;

        // 민심 바닥이면 달마다 5% 탈주. 식량 바닥이어도 시민은 사라지지 않는다(2026-10-08).
        pop.S.sentiment = 5; var freeU = pool.Free; pop.Monthly(); Check(pool.Free < freeU, "unrest desertion");
        rm.TrySpend(rm.GetAmount(RT.Food), 0); pop.SetTaxRate(0);
        // 개미 이탈만 본다. 특성 난수에 따라 장수가 전부 떠나 뒤 검사가 깨지지 않게 기분을 올려 둔다.
        foreach (var k in CommanderRoster.Instance.Commanders) k.PersonalState.AddMood("검사 고정", 100, 999);
        var freeS = pool.Free;
        typeof(UpkeepManager).GetMethod("RunCycle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(upkeep, null);
        Check(pool.Free == freeS, "food shortage keeps citizens");

        // 장수 나이: 첫 틱에 성체 나이, 어린 장수는 일·출전 불가, 늙으면 지혜 작업 +20%, 수명이 다하면 죽음.
        var c = CommanderRoster.Instance.Commanders.First(x => x.IsColonyMember && !x.IsEmbarked);
        c.PersonalState.ageMonths = -1; c.TickPersonal(.01f);
        Check(c.AgeMonths >= GameBalance.ChildMonths && c.AgeMonths <= 72.1f && c.PersonalState.lifespanMonths >= GameBalance.MinLifespan, "starting age assigned");
        c.SetBorn(); Check(c.IsChild && !c.CanDoJob(CommanderJobs.Gathering) && c.AgeLabel == "0세", "child cannot work");
        c.Traits.values.Remove(CommanderTrait.Senile);
        var normal = 0f; c.PersonalState.ageMonths = 40; normal = c.WorkRate(CommanderActivity.Research);
        c.PersonalState.ageMonths = 100; Check(c.IsElder && Mathf.Abs(c.WorkRate(CommanderActivity.Research) - normal * 1.2f) < .001f, "elder wisdom +20%");
        Check(Mathf.Abs(c.WorkRate(CommanderActivity.Strength) / c.Talents.Multiplier(CommanderActivity.Strength) - normal / c.Talents.Multiplier(CommanderActivity.Research) * .8f) < .001f, "elder body -20%");
        // 노망: 늙으면 지혜 보너스 대신 모든 작업 ×0.8, 젊을 때는 영향 없음.
        c.Traits.values.Add(CommanderTrait.Senile);
        Check(Mathf.Abs(c.WorkRate(CommanderActivity.Research) - normal * .8f) < .001f && CommanderTraits.DisplayName(CommanderTrait.Senile) == "노망", "senile elder -20%");
        c.PersonalState.ageMonths = 40; Check(Mathf.Abs(c.WorkRate(CommanderActivity.Research) - normal) < .001f, "senile young unaffected");
        c.Traits.values.Remove(CommanderTrait.Senile); c.PersonalState.ageMonths = 100;
        // 확률 사망 분기로 잘못 들어가도 우연히 통과하지 않도록 생존 난수를 고정한다.
        var randomState = UnityEngine.Random.state;
        for (var seed = 0; ; seed++)
        {
            UnityEngine.Random.InitState(seed);
            var beforeRoll = UnityEngine.Random.state;
            if (UnityEngine.Random.value < .3f) continue;
            UnityEngine.Random.state = beforeRoll; break;
        }
        c.PersonalState.lifespanMonths = 100.001f;
        try { c.TickPersonal(1); } finally { UnityEngine.Random.state = randomState; }
        Check(c.IsDead && c.PersonalState.departure == "Deceased", "dies of old age");

        // 이주 개미떼: 장수 1명 + 개미 5마리.
        var commanders = CommanderRoster.Instance.Count; var ants = pool.Total;
        GameSession.Instance.MarkStarted(0, 0); ColonyEvents.Instance.Restore(new ColonyEvents.State());
        var room = commanders < ScoutPost.DefaultMaxCommanders ? 1 : 0;
        var events = ColonyEvents.Instance;
        // 제안만 오고 바로 합류하지 않는다. 거절하면 아무도 안 옴.
        Check(events.TryTrigger(ColonyEvent.Migration) && events.MigrationPending && pool.Total == ants && CommanderRoster.Instance.Count == commanders, "migration is an offer");
        Check(events.DeclineMigration() && !events.MigrationPending && pool.Total == ants && !events.DeclineMigration(), "decline sends them away once");
        // 기한(90초)이 지나면 거절과 같다.
        events.Restore(new ColonyEvents.State()); events.TryTrigger(ColonyEvent.Migration);
        var saved = events.Capture(); Check(Mathf.Approximately(saved.migrationOffer, EventRules.MigrationOfferSeconds) && ColonyEvents.Validate(saved), "offer saved");
        events.Tick(EventRules.MigrationOfferSeconds + 1); Check(!events.MigrationPending && pool.Total == ants, "offer expires");
        // 수락하면 장수 1명 + 개미 5마리.
        events.Restore(new ColonyEvents.State()); events.TryTrigger(ColonyEvent.Migration);
        Check(events.AcceptMigration() && !events.AcceptMigration() && pool.Total == ants + EventRules.Migrants && CommanderRoster.Instance.Count == commanders + room,
            $"accept brings commander and ants (ants {ants}->{pool.Total} commanders {commanders}->{CommanderRoster.Instance.Count})");

        // 저장: 인구 상태 왕복, v11 이전 저장은 기본 인구로 이관, 잘못된 값 거부.
        pop.S.sentiment = 42; pop.S.young = 7; pop.SetTaxRate(.3f);
        var file = SaveSnapshot.Capture();
        Check(SaveValidator.Validate(file, out var error) && file.population.young == 7 && Mathf.Approximately(file.population.taxRate, .3f), "population saved: " + error);
        var legacy = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(file)); legacy.version = 11; legacy.population = null;
        Check(SaveValidator.Validate(legacy, out error) && legacy.population != null && legacy.population.sentiment == 60, "v11 migrates population: " + error);
        var bad = JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(file)); bad.population.sentiment = float.NaN;
        Check(!SaveValidator.Validate(bad, out _), "NaN sentiment rejected");
        pop.Restore(new ColonyPopulation.State()); pop.Restore(file.population);
        Check(pop.S.young == 7 && Mathf.Approximately(pop.S.sentiment, 42), "population restored");
        Object.Destroy(hut);
        return "PASS " + checks + " Phase 4 checks";
    }
}
