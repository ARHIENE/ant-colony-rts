using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public static class Review1029EconomyChecks
{
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        var count = 0; var report = new StringBuilder();
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        void Near(double a, double b, string why) => Check(Math.Abs(a - b) < .0001, why);
        float Flight(float rate) => AntColony.Core.GameBalance.UnrestDesertion * AntColony.Core.ColonyTaxRules.FlightRatio(rate);
        double Tax(int payers, double rate) => payers * rate * AntColony.Core.GameBalance.TaxPerAntMonth
            * AntColony.Core.GameCalendar.SecondsPerDay / AntColony.Core.GameCalendar.SecondsPerMonth
            * (1 - AntColony.Core.GameBalance.AdminTaxLoss);

        // 11/13/14/16: 미동원 성체·기본 생산력·행정0. 시민 자체 소비는 별도 회계.
        foreach (var focus in new[] { AntColony.Core.TaxFocus.Food, AntColony.Core.TaxFocus.Balanced, AntColony.Core.TaxFocus.Material })
        {
            var share = AntColony.Core.ColonyPopulation.FoodShare(focus);
            Near(share + (1 - share), 1, "납세 방침은 배분만 변경");
            var nominalFood = 3 * 2 * AntColony.Core.GameBalance.MealFood;
            var citizens = (int)Math.Ceiling(nominalFood / (Tax(1, .2) * share));
            Check(Tax(citizens, .2) * share >= nominalFood && Tax(citizens - 1, .2) * share < nominalFood, "3명분 기초 식량 대체 경계");
            report.AppendLine($"11/14 {focus}: 3명/하루 명목12식량 지원 최소 납세자 {citizens}명");
        }
        Near(Tax(20, .2) * .5, 4.25, "움막 만석 재료 세금");
        Near(AntColony.Core.GameBalance.HutSoil / (Tax(20, .2) * .5), 7.0588235, "13 재료분 회수일");
        Check(AntColony.Core.GameBalancePopulation.ImmigrationSeconds == 450, "이주 3일 주기");
        foreach (var n in new[] { 1, 20, 100, 500 })
        {
            var ages = AntColony.Core.GameBalancePopulation.ArrivalAges(n, .1f, .1f);
            Check(ages[0] + ages[1] + ages[2] == n, "이주 연령 배분 보존");
        }
        Check(AntColony.Core.GameBalancePopulation.ArrivalAges(100, .1f, .1f)[1] == 80, "이주민 전원이 즉시 성인이 아님");
        var population = 0; var payers = 0; var materialPaid = 0; var paybackDay = 0;
        float migrationCarry = 0, taxCarry = 0;
        while (materialPaid < AntColony.Core.GameBalance.HutSoil && paybackDay < 90)
        {
            paybackDay++; taxCarry += (float)Tax(payers, .2) * .5f;
            var paid = (int)Math.Floor(taxCarry); materialPaid += paid; taxCarry -= paid;
            if (paybackDay % 3 != 0) continue;
            migrationCarry += (20 - population) * AntColony.Core.GameBalancePopulation.ImmigrationShare
                * (population == 0 ? 1 : AntColony.Core.GameBalancePopulation.Demand(.5f, 0, 0, 0));
            var arrived = Math.Min(20 - population, (int)Math.Floor(migrationCarry));
            migrationCarry -= arrived; population += arrived;
            var ages = AntColony.Core.GameBalancePopulation.ArrivalAges(arrived, .1f, .1f);
            payers += ages[1] + ages[2];
        }
        Check(paybackDay > 8 && paybackDay < 90, "빈 움막 이주/정수세액 포함 회수는 즉시 만석 하한보다 느림");
        report.AppendLine($"13 빈 움막 단독/3일 이주·고정 연령비율10/80/10·정수납세 포함: 총 재료30 회수 {paybackDay}일/{paybackDay*2.5:0.#}분. 건설 직후 두 주기0·추가 피해/유지비/연간노화 제외, 특정 납부 재료만의 회수는 아님");
        foreach (var rate in new[] { .2f, .3f, .4f, .5f })
            report.AppendLine($"15 세율 {rate:P0}: 기본 목표민심 {AntColony.Core.ColonyTaxRules.BaseSentiment(rate):0.##}, 월 강제이탈 {Flight(rate):P1}");
        Near(AntColony.Core.ColonyTaxRules.BaseSentiment(.2f), 60, "20% 기본민심");
        Near(AntColony.Core.ColonyTaxRules.BaseSentiment(.4f), 35, "40% 비선형 불만");
        Near(AntColony.Core.ColonyTaxRules.BaseSentiment(.5f), 10, "50% 불안정");
        Near(Flight(.4f), 0, "40% 이탈 경계");
        Near(Flight(.45f), .025, "45% 월 2.5% 독립 이탈");
        Near(Flight(.5f), .05, "50% 월 5% 독립 이탈");
        Check(Flight(.401f) > 0, "40% 바로 위에서도 이탈");
        var mobilizedPenalty = AntColony.Core.ColonyPopulation.PolicySentiment(AntColony.Core.MilitaryPolicy.Total);
        Check(AntColony.Core.ColonyTaxRules.BaseSentiment(.4f) - mobilizedPenalty <= AntColony.Core.GameBalance.UnrestSentiment,
            "국민개병·무보정에서는40%가 민심 이탈 경계");
        Check(AntColony.Core.ColonyTaxRules.BaseSentiment(.3f) - mobilizedPenalty > AntColony.Core.GameBalance.UnrestSentiment,
            "같은 조건30%는 민심 이탈 경계 위");

        // 19: 만석·기본보상·행정0. 보상 식량은 재료와 따로 표기, 특별자원도 별도.
        var discount = typeof(AntColony.Buildings.Redevelopment).GetMethod("DiscountedCost", BindingFlags.Static | BindingFlags.NonPublic);
        int Discount(int cost) => (int)discount.Invoke(null, new object[] { cost });
        Check(UnityEngine.Mathf.CeilToInt(200 * AntColony.Core.GameBalance.RedevelopCostShare) == 121, "원래 float 상향 반올림 추가징수 재현");
        Check(Discount(200) == 120 && Discount(80) == 48, "정수 경계 정확한60% 비용");
        Check(Discount(31) == 19 && Discount(0) == 0, "비정수 비용 올림·무료 비용 보존");
        foreach (var oldSeats in new[] { 20, 50 })
        {
            var newSeats = oldSeats == 20 ? 50 : 120;
            var newSoil = oldSeats == 20 ? AntColony.Core.GameBalance.HouseSoil : AntColony.Core.GameBalance.ApartmentSoil;
            var rebuildSoil = Discount(newSoil)
                + Math.Ceiling(oldSeats * AntColony.Core.GameBalance.RedevelopSoilPerResident);
            Check(rebuildSoil / (newSeats - oldSeats) > newSoil / (double)newSeats, "재개발이 순증 좌석당 항상 싸지 않음");
            report.AppendLine($"19 {oldSeats}→{newSeats}: 재개발 재료 {rebuildSoil}/순증{newSeats-oldSeats}석 vs 신축 {newSoil}/{newSeats}석, 보상 식량 별도");
        }

        // 20: 복구된 주거·서비스100%·민심60·의료/행정0의 총인구 회복. 노화/추가사망 제외.
        var demand = AntColony.Core.GameBalancePopulation.Demand(.5f, 0, 0, 0);
        Near(demand, 1.5, "기준 이주 수요");
        var people = 500; double remainder = 0; var cycles = 0;
        while (people < 975 && cycles < 100)
        {
            remainder += (1000 - people) * AntColony.Core.GameBalancePopulation.ImmigrationShare * demand;
            var arrived = Math.Min(1000 - people, (int)Math.Floor(remainder));
            people += arrived; remainder -= arrived; cycles++;
        }
        Check(cycles == 19 && people >= 975, "손실500명 중95% 회복은19주기");
        report.AppendLine($"20 1000→500명, 정원복구 후 95% 총인구 회복 {cycles*3}게임일/{cycles*7.5:0.#}실제분. 연령·병력·생산 회복 보장은 아님");
        return $"PASS {count} 경제 계산 검사\n" + report;
    }
}
