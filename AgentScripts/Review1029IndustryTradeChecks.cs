using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public static class Review1029IndustryTradeChecks
{
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        var count = 0; var report = new StringBuilder();
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        var resources = Enum.GetValues(typeof(AntColony.Data.ResourceType)).Cast<AntColony.Data.ResourceType>().ToArray();
        var studies = resources.Where(AntColony.Core.ResourceStudyRules.RequiresResearch).ToArray();
        Check(studies.Length == 9, "35 실제 채굴 연구9종");
        foreach (var type in new[] { AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.Stone,
            AntColony.Data.ResourceType.CookedRice, AntColony.Data.ResourceType.Plank, AntColony.Data.ResourceType.Water })
            Check(!AntColony.Core.ResourceStudyRules.RequiresResearch(type), "무의미한 표본 연구 제외 " + type);
        Check(AntColony.Core.CampaignResearch.ResourceResearchWork == 300, "표본 연구 기회비용 유지");
        Check(AntColony.Buildings.UndergroundProduction.Grade(AntColony.Data.BuildingKind.DeepDrill) == 4
            && AntColony.Buildings.UndergroundProduction.Grade(AntColony.Data.BuildingKind.AdvancedDeepDrill) == 5, "36 상위 시추기는 자원 등급 구분");
        Check(AntColony.Buildings.UndergroundProduction.RequiredGrade(AntColony.Data.ResourceType.StellarOre) == 5, "성휘석 하위 시추 불가");
        var smelt = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.Smelter);
        var gas = AntColony.Buildings.Processor.RecipesFor(AntColony.Data.BuildingKind.GasSmelter);
        Check(smelt.Any(r => r.output == AntColony.Data.ResourceType.Steel)
            && !gas.Any(r => r.output == AntColony.Data.ResourceType.Steel), "36 가스 제련기가 강철 제련 대체 불가");
        Check(gas.Any(r => r.output == AntColony.Data.ResourceType.TitaniumAlloy
            && r.inputs.Any(i => i.type == AntColony.Data.ResourceType.Steel)), "하위 산업 강철 계속 필요");
        var alloy = gas.Single(r => r.output == AntColony.Data.ResourceType.TitaniumAlloy);
        var titanium = gas.Single(r => r.output == AntColony.Data.ResourceType.Titanium);
        var steel = smelt.First(r => r.output == AntColony.Data.ResourceType.Steel);
        var iron = smelt.First(r => r.output == AntColony.Data.ResourceType.Iron);
        int Input(AntColony.Buildings.Processor.Recipe r, AntColony.Data.ResourceType type) => r.inputs.Single(i => i.type == type).amount;
        var alloyRuns = 100d / alloy.amount;
        var titaniumRuns = alloyRuns * Input(alloy, AntColony.Data.ResourceType.Titanium) / titanium.amount;
        var steelRuns = alloyRuns * Input(alloy, AntColony.Data.ResourceType.Steel) / steel.amount;
        var ironRuns = steelRuns * Input(steel, AntColony.Data.ResourceType.Iron) / iron.amount;
        var work = alloyRuns * alloy.work + titaniumRuns * titanium.work + steelRuns * steel.work + ironRuns * iron.work;
        var ore = titaniumRuns * Input(titanium, AntColony.Data.ResourceType.TitaniumOre)
            + ironRuns * Input(iron, AntColony.Data.ResourceType.IronOre)
            + ironRuns * Input(iron, AntColony.Data.ResourceType.Coal) + steelRuns * Input(steel, AntColony.Data.ResourceType.Coal);
        var drilling = ore / 10 * AntColony.Buildings.SupplyFacility.ProcessSeconds;
        Check(work == 4800 && ore == 550, "37 합금100의 실제 하위 제련 노동·광물 요구");
        Check(drilling == 275 && work + drilling == 5075, "고체 시추까지 총5075작업");
        Check(AntColony.Core.StartSupplyRules.ManualWaterAmount == 10
            && AntColony.Core.StartSupplyRules.ManualWaterSeconds == AntColony.Buildings.SupplyFacility.ProcessSeconds, "펌프는 물10/5초 손일을 무인 처리로 전환");
        Check(AntColony.Buildings.PowerNode.SteamWatts == AntColony.Core.GameBalance.WoodGeneratorWatts * 4,
            "38 증기/가스 출력은 장작4배");
        Check(AntColony.Core.GameBalance.TreadmillWatts == AntColony.Core.GameBalance.WoodGeneratorWatts, "쳇바퀴는 연료 대신 인력");
        var charRecipe = AntColony.Buildings.IndustrialRecipes.Carbonization(AntColony.Data.BuildingKind.CharcoalKiln).Single();
        Check(charRecipe.inputs.Single().amount == 2 && charRecipe.work == 12, "대체연료는 원료·노동 공짜가 아님");
        var oilRequired = typeof(AntColony.Buildings.Processor).GetMethod("OilRequired", BindingFlags.Static | BindingFlags.NonPublic);
        var chemical = AntColony.Buildings.IndustrialRecipes.Chemistry();
        Check((int)oilRequired.Invoke(null, new object[] { chemical.Single(r => r.output == AntColony.Data.ResourceType.Rubber) }) == 0,
            "40 고무 제조법은 원유0");
        Check((int)oilRequired.Invoke(null, new object[] { chemical.Single(r => r.output == AntColony.Data.ResourceType.Plastic) }) == 2,
            "플라스틱 제조법은 원유2");
        var market = new AntColony.World.TradeOffer(); market.resources[0] = 10; market.equipment.Add("one");
        var reason = typeof(AntColony.World.DiplomacyManager).GetMethod("MarketCargoReason", BindingFlags.Static | BindingFlags.NonPublic);
        string Cargo(int space, bool blueprint) => (string)reason.Invoke(null, new object[] { market, space, blueprint });
        Check(Cargo(11, false) == "", "72 자원10+장비1 정확한 여유");
        Check(Cargo(10, false).Contains("부족"), "만재 전 결제 거부");
        Check(Cargo(0, false).Contains("부족"), "수송 여유0 거부");
        market.resources[0] = int.MaxValue; market.resources[1] = int.MaxValue;
        Check(Cargo(int.MaxValue, false).Contains("부족"), "수송 합계 overflow 우회 차단");
        market.resources[0] = market.resources[1] = 0; market.equipment.Clear(); market.blueprint = true;
        Check(Cargo(0, false) == "" && Cargo(100, true).Contains("이미"), "기존 무게없는 설계도/중복 구입 차단");
        Check(new AntColony.World.TradeOffer().resources.Length == 3, "72 문명 즉시교환은 기본3자원");
        Check(AntColony.World.ColonyEvents.SupplyPrice(AntColony.Data.ResourceType.IronOre) == 2
            && AntColony.World.ColonyEvents.SupplyPrice(AntColony.Data.ResourceType.Component) == 8, "73 캐러밴 고급 실물 원료 공급");
        Check(AntColony.World.DiplomacyRules.BlueprintPrice == 150 && AntColony.World.DiplomacyRules.MonthlyTradeAmount == 10, "73 설계도/문명협정 고유 역할 유지");
        report.AppendLine("35 표본 연구9종, 노동300/종, 기술 연구와 단일 작업 슬롯 공유. 미지정 채집/임의 연구 없음");
        report.AppendLine("36/37/40 제련 철14·강철20작업 vs 티타늄14·합금2개20작업: 재료 사슬이 다르므로 일괄 교체할 수 없음. 두 고체 시추기는 모두10개/5작업, 자동화 아님");
        report.AppendLine($"37 합금100: 제련 {work} + 고체광물{ore}개 시추 {drilling} = {work+drilling}작업, 속도1·노동50% 한 명 기준 {(work+drilling)/30:0.##}분. 생활/운반/연료 생산은 추가");
        report.AppendLine($"38 쳇바퀴100W/장수1, 장작100W/목재1·10초, 증기·가스400W/연료1·10초. 물펌프/유체추출은10개/5초 무인(전력·운반 별도)");
        report.AppendLine("72/73 현지 교역은 본거지 결제·방문 화물 수령·귀환 후 사용, 문명/본거지 캐러밴 즉시 수령은 기본자원·장비 범위");
        report.AppendLine("77 AI 확장5동은 경제/시대 성장과 연결되지 않음. 원정/전투 소유 파일 변경 없이 인계, 6시대 경쟁 구현 완료 아님");
        return $"PASS {count} 산업·교역 계산 검사\n" + report;
    }
}
