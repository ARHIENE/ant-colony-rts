using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public static class Review1029K1Checks
{
    private static int count;
    private static void Check(bool value, string name) { if (!value) throw new Exception("K1 FAIL: " + name); count++; }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .0001f;
    private static object Call(Type t, string name, params object[] args) => t.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static object Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    private static T Managed<T>() => (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
    public static async Task<string> Main()
    {
        count = 0;
        var work = new AntColony.Core.WorkContribution();
        work.Add("builder", 74.9f, true); work.Add("other", 25.1f, false);
        Check(work.Majority(100) == null, "바벨/천기 75% 미만");
        work.Add("builder", .1f, true); Check(work.Majority(100) == "builder", "75% 경계 포함");
        Check(work.Valid, "기여도 정상");
        var combined = new AntColony.Core.WorkContribution(); combined.Add("A", 40, true); combined.Add("B", 40, true); combined.Add("C", 20, false);
        Check(combined.Majority(100) == null, "복수 보유자 기여도 합산 금지");
        var before = combined.total; combined.Add("A", float.NaN, true); combined.Add("A", float.PositiveInfinity, true); combined.Add("A", -1, true);
        Check(combined.total == before, "비정상 작업량 거부");
        combined.qualified.Add(new AntColony.Core.WorkContribution.Entry { id = "A", work = 1 }); Check(!combined.Valid, "중복 기여자 저장 거부");
        foreach (var n in new[] { 1, 2, 3, 4, 5, 100, 200 })
            Check(AntColony.Buildings.BabelState.RebuildCost(n) == (int)Math.Ceiling(n * .75m), "바벨 정수 자재 " + n);
        Check(AntColony.Buildings.BabelState.RebuildCost(200) == 150, "float 올림 오차 회귀");
        Check(AntColony.Buildings.BabelState.Valid(null), "구저장 바벨 기본값");
        Check(!AntColony.Buildings.BabelState.Valid(new AntColony.Buildings.BabelState { work = float.NaN }), "바벨 NaN 거부");
        var sole = new AntColony.Core.SoleWorker(); sole.Note("A", true); sole.Note("A", true);
        Check(sole.Empowered && sole.Valid, "낙토/식선/하멜른 전과정 동일 보유자");
        sole.Note("B", true); sole.Note("A", true); Check(!sole.Empowered, "다른 장수 관여 해제 유지");
        sole = new AntColony.Core.SoleWorker(); sole.Note("", false); sole.Note("A", true); Check(!sole.Empowered, "일반 개미 작업 후 자격 소급 금지");
        sole = new AntColony.Core.SoleWorker(); sole.Note("A", false); sole.Note("A", true); Check(!sole.Empowered, "중간 특성 획득 소급 금지");
        var food = new AntColony.Core.FoodLots(); food.Add(1, 40, 21, true); food.Add(1, 40, 2, false);
        Check(food.lots.Count == 2, "동일 신선도도 요리사 숙련/식선 분리");
        var one = food.Take(1); Check(one.CookingSkill == 20 && one.Inspired, "실제 섭취 묶음 숙련20/식선");
        Check(food.CookingSkill == 1 && !food.Inspired, "다른 묶음 숙련1/일반");
        one.Age(10); var move = new AntColony.Core.FoodLots(); move.Add(one); Check(Near(move.Stalest, 30), "수송 적재 신선도 보존");
        var unload = move.Take(1); Check(Near(unload.Stalest, 30) && unload.Inspired && unload.CookingSkill == 20, "하역 숙련/식선/신선도 보존");
        Check(unload.Age(30) == 1 && unload.Total == 0, "수송 중 부패 경계");
        var legacy = new AntColony.Core.FoodLots(); legacy.Add(2, 100); Check(legacy.CookingSkill == 5 && !legacy.Inspired, "구저장 요리 숙련5/식선없음");
        legacy.lots.Add(new AntColony.Core.FoodLots.Lot { amount = 1, shelf = 30, cookSkillPlusOne = 22 }); Check(!legacy.Valid, "잘못된 숙련 저장 거부");
        Check(AntColony.Data.CookedFoods.Quality(AntColony.Data.ResourceType.SmokedFood) == 1, "훈연식 간단식 식단");
        Check(AntColony.Data.ResourceLabels.IsEdible(AntColony.Data.ResourceType.SmokedFood), "훈연식 섭취 가능");
        Check(AntColony.Core.FoodLots.ShelfLife(AntColony.Data.ResourceType.SmokedFood) == AntColony.Core.FoodLots.ShelfLife(AntColony.Data.ResourceType.SimpleMeal), "별도 보존 보너스 없음");
        Check((int)AntColony.Data.ResourceType.SmokedFood == (int)AntColony.Data.ResourceType.FriedRice + 1, "자원 enum 끝 추가 호환");
        Check(AntColony.Buildings.Kitchen.Recipes.Last().output == AntColony.Data.ResourceType.SmokedFood, "기존 제조법 번호 보존/훈연 연결");
        Check(AntColony.Buildings.Kitchen.SmokedFood.inputs.Single(i => i.type == AntColony.Data.ResourceType.Food).amount == 1, "통합 식량1+목재 → 1끼");
        Check(AntColony.Buildings.Kitchen.SmokedFood.inputs.Single(i => i.type == AntColony.Data.ResourceType.Wood).amount == 1, "훈연 목재1");
        var meal = new AntColony.Units.CommanderPersonalState();
        Check((bool)Call(typeof(AntColony.Units.CommanderAnt), "DiamondMealAvailable", meal), "식선 첫 섭취 허용");
        meal.moodFactors.Add(new AntColony.Units.MoodFactor { reason = "식선: 잊지 못할 한 끼", value = 20, remaining = 50 });
        Check(!(bool)Call(typeof(AntColony.Units.CommanderAnt), "DiamondMealAvailable", meal) && meal.moodFactors[0].remaining == 50, "식선 재섭취 비중첩/비갱신");
        meal.moodFactors[0].remaining = 0; Check((bool)Call(typeof(AntColony.Units.CommanderAnt), "DiamondMealAvailable", meal), "식선 종료 후 허용");
        Check(Near(AntColony.Core.CampaignResearch.PoliticalMultiplier(0), 1), "여래장0명");
        Check(Near(AntColony.Core.CampaignResearch.PoliticalMultiplier(1), .7f), "여래장1명30%");
        Check(Near(AntColony.Core.CampaignResearch.PoliticalMultiplier(2), .55f), "여래장2명45%");
        Check(Near(AntColony.Core.CampaignResearch.PoliticalMultiplier(3), .475f), "여래장3명52.5%");
        Check(AntColony.Core.CampaignResearch.PoliticalMultiplier(1000) >= .4f, "여래장 수렴상한60%");
        foreach (var affinity in new[] { -100f, 0, 100 })
            Check(Near(AntColony.World.DiplomacyManager.ArtworkChance(affinity), Math.Min(1, .5f + affinity / 200 + .3f)), "화룡 +30%p " + affinity);
        Check(Near(AntColony.World.DiplomacyManager.ArtworkChance(-100), .3f), "화룡 최소30%");
        Check(Near(AntColony.World.DiplomacyManager.ArtworkChance(100), 1), "화룡 상한100%");
        Check(AntColony.World.ResourceNode.TreasureCatch(.04999f) && !AntColony.World.ResourceNode.TreasureCatch(.05f), "태공5% 경계");
        Check(AntColony.World.ResourceNode.TreasureEquipment(.09999f) && !AntColony.World.ResourceNode.TreasureEquipment(.1f), "장비10% 조건부 경계");
        var treasures = 0; var gear = 0;
        for (var a = 0; a < 100; a++) for (var b = 0; b < 100; b++)
            if (AntColony.World.ResourceNode.TreasureCatch((a + .5f) / 100)) { treasures++; if (AntColony.World.ResourceNode.TreasureEquipment((b + .5f) / 100)) gear++; }
        Check(treasures == 500 && gear == 50, "태공10000 조합 보물5%/장비0.5%");
        Check(!AntColony.World.ResourceNode.TreasureResources(1).Contains(AntColony.Data.ResourceType.Iron), "신석기 철 제외");
        Check(!AntColony.World.ResourceNode.TreasureResources(2).Contains(AntColony.Data.ResourceType.Iron), "청동기 철 제외");
        Check(AntColony.World.ResourceNode.TreasureResources(3).Contains(AntColony.Data.ResourceType.Iron), "철기 묶음");
        var next = AntColony.Core.CampaignResearch.InsightSuccessor(AntColony.Core.ScienceTechnology.Vehicle, Array.Empty<int>(), Array.Empty<int>());
        Check(next != null && next.Prerequisites.Contains(AntColony.Core.ScienceTechnology.Vehicle), "천기 직접 후속만 선택");
        Check(AntColony.Core.CampaignResearch.InsightSuccessor(AntColony.Core.ScienceTechnology.Vehicle, new[] { (int)next.Technology }, Array.Empty<int>())?.Technology != next.Technology, "완료 후속 제외");
        Check(AntColony.Core.CampaignResearch.InsightSuccessor(AntColony.Core.ScienceTechnology.Vehicle, Array.Empty<int>(), new[] { (int)next.Technology })?.Technology != next.Technology, "통찰 중복 제외");
        var state = new AntColony.Core.CampaignResearch.State();
        Check((bool)Call(typeof(AntColony.Core.CampaignResearch), "ValidInsight", state), "구저장 천기 유효");
        state.insights.Add((int)next.Technology); state.insightApplied.Add((int)next.Technology);
        Check((bool)Call(typeof(AntColony.Core.CampaignResearch), "ValidInsight", state), "통찰 저장 적용 마커");
        state.insightApplied.Add((int)next.Technology); Check(!(bool)Call(typeof(AntColony.Core.CampaignResearch), "ValidInsight", state), "통찰 중복 적용 저장 거부");
        var fields = new[] { (typeof(AntColony.Save.BuildingDto), "babel"), (typeof(AntColony.Save.BuildingDto), "diamondArtwork"),
            (typeof(AntColony.Save.TransportDto), "cargoFreshness"), (typeof(AntColony.Save.SaveFileV1), "constructionAttribution"),
            (typeof(AntColony.World.Critter.State), "tamer"), (typeof(AntColony.Buildings.FarmPlot.State), "diamondCycle") };
        foreach (var field in fields) Check(field.Item1.GetField(field.Item2) != null, "저장 필드 " + field.Item2);
        var transport = new AntColony.Save.TransportDto { cargoFood = 2, cargoFreshness = null };
        Check((bool)Call(typeof(AntColony.World.ExpeditionTransport), "ValidFoodAmounts", transport), "구저장 수송 신선도 기본 허용");
        var cargoLots = new AntColony.Core.FoodLots(); cargoLots.Add(2, 17, 12, true);
        transport.cargoFreshness = new List<AntColony.World.ExpeditionTransport.FoodCargo> { new AntColony.World.ExpeditionTransport.FoodCargo { type = AntColony.Data.ResourceType.Food, lots = cargoLots } };
        Check((bool)Call(typeof(AntColony.World.ExpeditionTransport), "ValidFoodAmounts", transport), "화물 수량/묶음 일치");
        transport.cargoFood = 3; Check(!(bool)Call(typeof(AntColony.World.ExpeditionTransport), "ValidFoodAmounts", transport), "누락 묶음 신선도 재생 거부");
        transport.cargoFood = 1; transport.withdrawal = new AntColony.World.WithdrawState();
        transport.withdrawal.loads.Add(new AntColony.World.WithdrawLoad { type = AntColony.Data.ResourceType.Food, amount = 1 });
        Check((bool)Call(typeof(AntColony.World.ExpeditionTransport), "ValidFoodAmounts", transport), "장수 없는 하역 대기 묶음 보존");
        transport.withdrawal.loads[0].workerId = "courier";
        Check(!(bool)Call(typeof(AntColony.World.ExpeditionTransport), "ValidFoodAmounts", transport), "장수 보유 물량 중복 집계 거부");
        var aura = new AntColony.Core.CampaignResearch.State();
        aura.politicalPresence.Add(new AntColony.Core.CampaignResearch.PoliticalPresence { id = "A", site = -1, remaining = AntColony.Core.GameCalendar.SecondsPerDay * 3 });
        Check((bool)Call(typeof(AntColony.Core.CampaignResearch), "ValidPoliticalPresence", aura), "부재3일 저장 경계");
        aura.politicalPresence[0].remaining += 1; Check(!(bool)Call(typeof(AntColony.Core.CampaignResearch), "ValidPoliticalPresence", aura), "부재3일 초과 거부");
        var diplomacy = new AntColony.World.DiplomacyManager.State(); diplomacy.civilizations.Add(new AntColony.World.Civilization { id = "civ" });
        diplomacy.artworkRetries.Add(new AntColony.World.DiplomacyManager.ArtworkRetry { civ = "civ", purpose = AntColony.World.ArtworkPurpose.Peace, until = 100 });
        Check((bool)Call(typeof(AntColony.World.DiplomacyManager), "ValidArtwork", diplomacy), "화룡 거절 대기 저장");
        diplomacy.artworkRetries.Add(diplomacy.artworkRetries[0]); Check(!(bool)Call(typeof(AntColony.World.DiplomacyManager), "ValidArtwork", diplomacy), "화룡 중복 대기 거부");
        var attribution = new List<AntColony.Save.ConstructionAttributionDto> { new AntColony.Save.ConstructionAttributionDto { order = 7, contributions = work, artist = sole, babelUsed = true } };
        var construction = new List<AntColony.Save.ConstructionDto> { new AntColony.Save.ConstructionDto { order = 7 } };
        var attributionType = typeof(AntColony.Save.SaveSnapshot).Assembly.GetType("AntColony.Save.SaveAttribution");
        Check((bool)Call(attributionType, "Valid", attribution, construction), "공사 저장 보조 연결");
        construction.Clear(); Check(!(bool)Call(attributionType, "Valid", attribution, construction), "고아 공사 기여도 거부");
        // Unity native 객체를 만들지 않고 managed 필드만 주입해 실제 수송/효과 메서드를 실행한다.
        var ship = Managed<AntColony.World.ExpeditionTransport>();
        var quantities = new Dictionary<AntColony.Data.ResourceType, int> { [AntColony.Data.ResourceType.Food] = 2 };
        Set(ship, "cargo", quantities); Set(ship, "withdraw", new AntColony.World.WithdrawState());
        var actualLots = new AntColony.Core.FoodLots(); actualLots.Add(2, 10, 8, true);
        Set(ship, "CargoFood", new List<AntColony.World.ExpeditionTransport.FoodCargo> { new AntColony.World.ExpeditionTransport.FoodCargo { type = AntColony.Data.ResourceType.Food, lots = actualLots } });
        Invoke(ship, "AgeCargoFood", 5f);
        Check(Near(actualLots.Stalest, 5) && ship.GetCargo(AntColony.Data.ResourceType.Food) == 2, "실제 수송 AgeCargoFood 5초");
        var incoming = new AntColony.Core.FoodLots(); incoming.Add(1, 1, 4, false);
        quantities[AntColony.Data.ResourceType.Food] = 3; Set(ship, "IncomingFood", incoming);
        Invoke(ship, "CreditCargoFood", AntColony.Data.ResourceType.Food, 1);
        Check(actualLots.Total == 3 && Near(actualLots.Stalest, 1), "실제 적재 신선도 보존");
        Invoke(ship, "AgeCargoFood", 2f);
        Check(ship.GetCargo(AntColony.Data.ResourceType.Food) == 2 && ship.GetCargo(AntColony.Data.ResourceType.RottenFood) == 1 && Near(actualLots.Stalest, 3), "실제 화물 부패 수량/잔여 시간");
        var actualUnload = (AntColony.Core.FoodLots)Invoke(ship, "TakeCargoFood", AntColony.Data.ResourceType.Food, 1);
        Check(actualUnload.CookingSkill == 7 && actualUnload.Inspired && Near(actualUnload.Stalest, 3), "실제 하역 숙련/식선/보존");
        var research = Managed<AntColony.Core.CampaignResearch>();
        var researchState = new AntColony.Core.CampaignResearch.State { active = (int)next.Technology };
        researchState.insights.Add((int)next.Technology); Set(research, "state", researchState);
        Invoke(research, "ApplyInsight"); var credited = research.Progress;
        Invoke(research, "ApplyInsight");
        Check(Near(credited, next.Work * .2f) && research.Progress == credited && researchState.insightApplied.Count == 1, "실제 천기20% 1회 적용/재선택 방지");
        researchState.politicalPresence.Add(new AntColony.Core.CampaignResearch.PoliticalPresence { id = "absent", site = -1, remaining = AntColony.Core.GameCalendar.SecondsPerDay * 3 });
        Invoke(research, "TickPoliticalPresence", 100f);
        Check(Near(researchState.politicalPresence[0].remaining, 350), "실제 부재 잔여 시간 감소");
        Check(Near(research.SettlementDiscontentMultiplier(-1), .7f), "실제 부재 중 불만30% 유지");
        Invoke(research, "TickPoliticalPresence", 350f);
        Check(researchState.politicalPresence.Count == 0 && Near(research.SettlementDiscontentMultiplier(-1), 1), "실제 부재3일 만료");
        var diner = Managed<AntColony.Units.CommanderAnt>(); var personal = new AntColony.Units.CommanderPersonalState(); Set(diner, "personalState", personal);
        Invoke(diner, "ApplyDiamondMeal"); Check(personal.moodFactors.Single().value == 20 && personal.moodFactors.Single().remaining == AntColony.Core.GameCalendar.SecondsPerDay, "실제 식선20/1일");
        personal.moodFactors[0].remaining = 50; Invoke(diner, "ApplyDiamondMeal");
        Check(personal.moodFactors.Count == 1 && personal.moodFactors[0].remaining == 50, "실제 식선 재섭취 비갱신");
        await Task.CompletedTask;
        return "Review1029K1: PASS " + count + " (actual game DLL; Unity scene/save roundtrip/UI not executed)";
    }
    // Claude 전용 Unity 실행 인계. 디스크·씬 복원은 하지 않고 실제 저장 스냅샷/JSON/사전검증을 검사한다.
    public static async Task<string> SceneSaveChecks()
    {
        var result = await Main();
        var file = AntColony.Save.SaveSnapshot.Capture();
        var json = UnityEngine.JsonUtility.ToJson(file);
        if (!AntColony.Save.SaveValidator.TryParse(json, out var parsed, out var error)) throw new Exception("K1 snapshot parse: " + error);
        if (!AntColony.Save.SavePreflight.Validate(parsed, out error)) throw new Exception("K1 preflight: " + error);
        return result + "; live snapshot/JSON/preflight PASS (scene restore/UI still separate)";
    }
}
