using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;

public static class Review1031L1Checks
{
    private static int count;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("L1 실패: " + name); count++; }
    private static T Managed<T>() => (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
    private static void Set(object target, string name, object value)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static WorkerAnt Cargo(AntColony.Data.ResourceType type, float amount)
    {
        var worker = Managed<WorkerAnt>();
        Set(worker, "carriedType", type); Set(worker, "carriedAmount", amount);
        return worker;
    }

    public static Task<string> Main()
    {
        count = 0;
        var total = new Dictionary<AntColony.Data.ResourceType, int>();
        foreach (var tier in ScienceLabUpgrades.Steps(1, 6))
        {
            var required = ScienceLabUpgrades.RequiredResearch(tier);
            Check(CampaignResearch.Technologies[(int)required].Tier <= tier, "승급 선행의 상위 연구소 순환 없음 " + tier);
            var visited = new HashSet<ScienceTechnology>();
            void Visit(ScienceTechnology t)
            {
                if (!visited.Add(t)) return;
                var definition = CampaignResearch.Technologies[(int)t];
                Check(definition.Tier <= tier, "승급 선행의 전체 경로 등급 " + t);
                foreach (var prerequisite in definition.Prerequisites) Visit(prerequisite);
            }
            Visit(required);
            foreach (var cost in ScienceLabUpgrades.Cost(tier))
            {
                Check(cost.amount > 0 && cost.type != AntColony.Data.ResourceType.Soil && cost.type != AntColony.Data.ResourceType.Food, "임시 식량·흙 비용 교체 " + tier);
                total[cost.type] = total.GetValueOrDefault(cost.type) + cost.amount;
            }
            Check(ScienceLabUpgrades.Description(tier).Contains((tier + 1) + "등급"), "승급 화면 실제 다음 등급 " + tier);
        }
        Check(total[AntColony.Data.ResourceType.Iron] == 40 && total[AntColony.Data.ResourceType.Steel] == 140, "전체 승급 철·강철 합계");
        Check(total[AntColony.Data.ResourceType.Component] == 10 && total[AntColony.Data.ResourceType.ElectronicComponent] == 60 && total[AntColony.Data.ResourceType.Titanium] == 80 && total[AntColony.Data.ResourceType.StellarAlloy] == 80, "기획 실물 부품·전자부품·성휘합금 합계");
        Check(!ScienceLabUpgrades.Steps(6, 6).Any() && !ScienceLabUpgrades.Cost(6).Any(), "최고 연구소 추가 차감 없음");
        var rocketPath = new HashSet<ScienceTechnology>();
        void AddRocket(ScienceTechnology technology)
        {
            if (!rocketPath.Add(technology)) return;
            foreach (var prerequisite in CampaignResearch.Technologies[(int)technology].Prerequisites) AddRocket(prerequisite);
        }
        foreach (var technology in new[] { ScienceTechnology.Hull, ScienceTechnology.Engine, ScienceTechnology.Cocoons }) AddRocket(technology);
        foreach (var tier in ScienceLabUpgrades.Steps(1, 6)) AddRocket(ScienceLabUpgrades.RequiredResearch(tier));
        var rocketDefinitions = rocketPath.Select(t => CampaignResearch.Technologies[(int)t]).ToArray();
        Check(rocketDefinitions.Length == 12 && rocketDefinitions.Sum(d => d.Work) == 29100, "승급 선행 포함 로켓12연구·총연구량");
        Check(rocketDefinitions.Sum(d => d.Food) == 2900 && rocketDefinitions.Sum(d => d.Soil) == 2900 && rocketDefinitions.Sum(d => d.Special) == 500, "승급 연구 포함 총비용·중복 선행 제외");

        var food = Cargo(AntColony.Data.ResourceType.SimpleMeal, 3);
        var other = Cargo(AntColony.Data.ResourceType.FineMeal, 7);
        var delivering = Cargo(AntColony.Data.ResourceType.SimpleMeal, 5);
        Set(delivering, "deliverySite", Managed<Kitchen>());
        var outbound = Cargo(AntColony.Data.ResourceType.SimpleMeal, 11);
        Set(outbound, "targetDeposit", Managed<ExpeditionTransport>());
        var remote = Cargo(AntColony.Data.ResourceType.SimpleMeal, 13);
        var workers = new[] { food, other, delivering, outbound, remote };
        Check(ProductionStock.Carried(AntColony.Data.ResourceType.SimpleMeal, workers, w => !ReferenceEquals(w, remote)) == 3,
            "운반 실물 1회만 집계·다른품목/원료배송/수송적재/타지역 제외");
        Check(!delivering.CountsForHomeStock && !outbound.CountsForHomeStock, "배송·수송분 재고 제외");
        Set(food, "carriedAmount", 0f);
        Check(ProductionStock.Carried(AntColony.Data.ResourceType.SimpleMeal, workers, w => !ReferenceEquals(w, remote)) == 0, "입고 뒤 운반분 0·창고와 중복 없음");
        var fractionA = Cargo(AntColony.Data.ResourceType.SimpleMeal, .6f); var fractionB = Cargo(AntColony.Data.ResourceType.SimpleMeal, .6f);
        Check(ProductionStock.Carried(AntColony.Data.ResourceType.SimpleMeal, new[] { fractionA, fractionB }, w => true) == 1, "개별 올림 없이 운반 총량 정수화");

        var population = new ColonyPopulation.State { old = 100, agingDue = 20 };
        Check(ColonyPopulation.NaturalDeaths(population) == 12 && population.old == 88 && population.agingDue == 20, "노년 연간15%·동원예약 정산 대기 제외");
        population = new ColonyPopulation.State { old = 1 };
        for (var year = 0; year < 6; year++) Check(ColonyPopulation.NaturalDeaths(population) == 0, "소수 자연사 이월 " + year);
        Check(ColonyPopulation.NaturalDeaths(population) == 1 && population.old == 0, "소규모 노년 영구 생존 방지");
        population = new ColonyPopulation.State { old = 20, agingDue = 20, oldDeathRemainder = .5 };
        Check(ColonyPopulation.NaturalDeaths(population) == 0 && population.old == 20 && population.oldDeathRemainder == .5, "모두 동원 중이면 정산 대기 인구·이월 보존");
        population.agingDue = 0;
        Check(ColonyPopulation.NaturalDeaths(population) == 3 && population.old == 17, "복귀 정산 후 남은 노년만 자연사");
        var elderlyMilitary = new ColonyPopulation.State { old = 100, elderlyService = true };
        var eligibleBefore = ColonyPopulation.MilitaryAgeCount(30, elderlyMilitary);
        elderlyMilitary.agingDue = 20;
        Check(ColonyPopulation.MilitaryAgeCount(30, elderlyMilitary) == eligibleBefore, "노년 병역 편입 뒤 전체 군역인구 유지·중복 차감 금지");
        elderlyMilitary.elderlyService = false;
        Check(ColonyPopulation.MilitaryAgeCount(30, elderlyMilitary) == 30, "나이 확대 해제 시 신규 노년 동원 제외");
        foreach (var invalid in new[] { -1d, 1d, double.NaN, double.PositiveInfinity })
        {
            population.oldDeathRemainder = invalid;
            Check(!ColonyPopulation.ValidCivicState(population), "비정상 자연사 이월 거부 " + invalid);
        }
        Check(ColonyPopulation.ValidCivicState(new ColonyPopulation.State()), "구저장 자연사 기본0 유효");

        var debt = new ColonyPopulation.State { schoolFoodDebt = 2.3f, clinicFoodDebt = 3.6f, schoolMaterialDebt = 1.1f, clinicMedicineDebt = 2.2f };
        var recovery = ColonyPopulation.CivicRecoveryText(debt, 4, 0, 2);
        Check(recovery.Contains("식량 5(부족 1)") && recovery.Contains("흙 1(부족 1)") && recovery.Contains("기초약 2(부족 0)"), "전체복구 공유 식량 이중사용 금지·부족액");
        recovery = ColonyPopulation.CivicRecoveryText(new ColonyPopulation.State { schoolFoodDebt = .3f, schoolMaterialDebt = .15f }, 0, 0, 0);
        Check(recovery.Contains("식량 0(부족 0)") && recovery.Contains("흙 0(부족 0)"), "소수 부채는 즉시 복구 재고에 올림하지 않음");
        Check(Kitchen.SmokedFood.inputs.Any(i => i.type == AntColony.Data.ResourceType.Food), "훈연식 통합 원료 최소안 유지");
        Check(ColonyEvents.SupplyPrice(AntColony.Data.ResourceType.Soil) == 0, "사막 학당 안내: 기초상점 흙 판매로 오안내하지 않음");

        var civilization = new Civilization { contacted = true, hiddenRevealed = true };
        civilization.treaties[(int)TreatyKind.Trade] = 100;
        Check(DiplomacyManager.PeriodicTradeEnabled(civilization, 50), "구저장 정기 교환 허용 기본값");
        civilization.periodicTradePaused = true;
        Check(!DiplomacyManager.PeriodicTradeEnabled(civilization, 50) && civilization.HasTreaty(TreatyKind.Trade, 50) && civilization.hiddenRevealed, "정기 교환 중단만·협정 정보 보존");
        civilization.periodicTradePaused = false;
        Check(DiplomacyManager.PeriodicTradeEnabled(civilization, 50) && !DiplomacyManager.PeriodicTradeEnabled(civilization, 100), "재개·만료 경계");
        civilization.war = true; Check(!DiplomacyManager.PeriodicTradeEnabled(civilization, 50), "전쟁 중 정기 교환 금지");
        civilization.war = false; civilization.extinct = true;
        Check(!DiplomacyManager.PeriodicTradeEnabled(civilization, 50), "소멸 문명 제외");
        civilization.extinct = false;
        var manager = Managed<DiplomacyManager>();
        var diplomaticState = new DiplomacyManager.State(); diplomaticState.civilizations.Add(civilization);
        Set(manager, "<Data>k__BackingField", diplomaticState);
        var price = manager.PriceFactor(civilization);
        Check(manager.SetPeriodicTradePaused(civilization, true) && civilization.periodicTradePaused, "실제 설정 경로 중단");
        Check(manager.PriceFactor(civilization) == price, "정기 교환 중단과 협상 가격 혜택 분리");
        Check(manager.SetPeriodicTradePaused(civilization, false) && !civilization.periodicTradePaused, "실제 설정 경로 재개");
        Check(!manager.SetPeriodicTradePaused(new Civilization { contacted = true }, true), "미등록 상대 설정 거부");

        // 실제 이관 메서드를 호출하되 필수 섹션 누락으로 씬 의존 검사 전에 종료한다.
        var save = new SaveFileV1 { version = 22, options = null };
        save.population.oldDeathRemainder = .5;
        save.diplomacy = new DiplomacyManager.State();
        save.diplomacy.civilizations.Add(new Civilization { periodicTradePaused = true });
        Check(!SaveValidator.Validate(save, out var error) && error == "Missing required sections.", "구저장 이관 뒤 신뢰경계 거부 유지");
        Check(save.version == SaveFileV1.CurrentVersion && save.population.oldDeathRemainder == 0 && !save.diplomacy.civilizations[0].periodicTradePaused, "v22→현재 기본값 이관");
        save.population.oldDeathRemainder = .5; save.diplomacy.civilizations[0].periodicTradePaused = true;
        SaveValidator.Validate(save, out _);
        Check(save.population.oldDeathRemainder == .5 && save.diplomacy.civilizations[0].periodicTradePaused, "현재 저장 설정 보존·반복 이관 없음");
        save.savedAtUtc = DateTime.UtcNow.ToString("O"); save.population.oldDeathRemainder = double.NaN;
        Check(!SavePreflight.Validate(save, out error) && error.Contains("natural death remainder"), "Preflight 비정상 이월 선차단");
        return Task.FromResult($"L1 오프라인 {count}검사 통과");
    }

    // 격리된 시작 완료 장면에서만 호출. 이 작업에서는 Unity 실행 금지로 실행 잔여.
    public static async Task<string> RunScene()
    {
        var result = await Main();
        var population = ColonyPopulation.Instance;
        var diplomacy = DiplomacyManager.Instance;
        if (population == null || diplomacy == null) throw new Exception("시작 완료 장면 필요");
        var originalRemainder = population.S.oldDeathRemainder;
        // 새 게임은 미접촉일 수 있어 검사 동안만 접촉 처리 후 되돌린다.
        var civilization = diplomacy.Data.civilizations.First(c => !c.extinct);
        var originalPaused = civilization.periodicTradePaused; var originalContacted = civilization.contacted;
        try
        {
            civilization.contacted = true;
            population.S.oldDeathRemainder = .625;
            diplomacy.SetPeriodicTradePaused(civilization, true);
            var snapshot = SaveSnapshot.Capture();
            var json = UnityEngine.JsonUtility.ToJson(snapshot);
            Check(SaveValidator.TryParse(json, out var parsed, out var error), "실스냅샷 JSON " + error);
            Check(SavePreflight.Validate(parsed, out error), "실스냅샷 Preflight " + error);
            Check(parsed.population.oldDeathRemainder == .625 && parsed.diplomacy.civilizations.First(c => c.id == civilization.id).periodicTradePaused, "실스냅샷 신규 필드 보존");
            return result + "; 장면 JSON/Preflight 통과 (전체 복원·실생산·UI·장기플레이 별도)";
        }
        finally
        {
            population.S.oldDeathRemainder = originalRemainder;
            diplomacy.SetPeriodicTradePaused(civilization, originalPaused);
            civilization.contacted = originalContacted;
        }
    }
}
