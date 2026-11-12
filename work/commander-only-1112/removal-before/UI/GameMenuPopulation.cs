using System;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        public void Population()
        {
            var pop = ColonyPopulation.Instance; var pool = AntPool.Instance;
            if (pop == null || pool == null) return;
            var window = V5Window("주거 · 시민", "시민은 수량으로 관리합니다 · 주거와 공공서비스가 이주 수요에 반영됩니다");
            content = V5Section(window, "인구 · 주거", 20, 104, 520, 544);
            MenuTheme.Text(content, $"시민 {pop.Total} / 주거 정원 {pop.HousingCapacity}", 20, 40);
            MenuTheme.Text(content, $"어린 {pop.S.young} · 성체 {ColonyPopulation.Adults} · 늙은 {pop.S.old} · 출전 {pool.Assigned}", 13, 32);
            MenuTheme.Text(content, $"민심 {pop.S.sentiment:0} / 100" + (pop.Unrest ? " · 이탈 위험 / 동원 불가" : ""), 15, 32).color = pop.Unrest ? MenuTheme.Danger : MenuTheme.TextColor;
            MenuTheme.Text(content, $"이주 수요 {pop.Demand:0} / 200 · 서비스 {pop.PublicServiceCoverage:P0}\n" + (pop.S.immigrationStopped ? "이주 수용 중단" : $"다음 이주 {GameBalancePopulation.ImmigrationSeconds-pop.S.immigrationSeconds:0}초"), 13, 52);
            MenuTheme.Text(content, $"주거 {pop.HousingDemand:0} · 시설 {pop.FacilityDemand:0} · 안전 {pop.SafetyDemand:0} · 민심 {pop.S.sentiment:0}", 13, 32);
            MenuTheme.Button(content, pop.S.immigrationStopped ? "이주 수용 재개" : "이주 수용 중단", () => { pop.SetImmigrationStopped(!pop.S.immigrationStopped); Population(); });
            MenuTheme.Text(content, "완성된 시민 주거", 15, 32);
            foreach (var home in Housing.All)
                MenuTheme.Text(content, $"{home.Data.displayName} · 정원 {home.Capacity} · 내구 {home.CurrentHealth:0}/{home.MaxHealth:0}", 13, 36);
            if (!Housing.All.Any()) MenuTheme.Text(content, "완성된 시민 주거가 없습니다. 건설에서 주거를 마련하세요.", 13, 48);
            MenuTheme.Button(content, "주거 건설", () => { Resume(); BuildScreen.Open(); });
            MenuTheme.Button(content, "공공서비스 · 대피", CivicServices);
            content = V5Section(window, "세금 · 병역 · 행정", 554, 104, 526, 544);
            MenuTheme.Text(content, $"세율 {pop.S.taxRate:P0} · 다음 식량 +{pop.NextFoodTax} / 재료 +{pop.NextMaterialTax}", 15, 42);
            MenuTheme.Text(content, "세율이 높으면 민심과 이주에 불리합니다. 30% 초과는 민심 불이익이 커지고, 40% 초과는 시민 이탈을 일으킵니다.", 13, 54);
            var rate = MenuTheme.Input(content, Mathf.RoundToInt(pop.S.taxRate*100).ToString()); rate.name = "TaxInput";
            rate.onEndEdit.AddListener(v => { if(int.TryParse(v,out var n)) pop.SetTaxRate(n/100f); Population(); });
            foreach (TaxFocus focus in Enum.GetValues(typeof(TaxFocus)))
            {
                var pick = focus;
                MenuTheme.Button(content, (pop.S.taxFocus == focus ? "✓ " : "") + ColonyPopulation.FocusName(focus), () => { pop.SetTaxFocus(pick); Population(); }, $"식량 {ColonyPopulation.FoodShare(focus):P0} · 재료 {1-ColonyPopulation.FoodShare(focus):P0}");
            }
            var basics = ColonyPopulation.TaxMaterials; var mainIndex = Mathf.Max(0, Array.IndexOf(basics, pop.S.taxMaterial));
            MenuTheme.Button(content, "우선 납부: " + basics[mainIndex].DisplayName() + " ▸", () => { pop.SetTaxMaterial(basics[(mainIndex+1)%basics.Length]); Population(); });
            MenuTheme.Text(content, $"병역 · 상한 {pop.MobilizationLimit(EnemyAlert.CrisisActive)} / 출전 {pool.Assigned}", 15, 36);
            foreach (MilitaryPolicy policy in Enum.GetValues(typeof(MilitaryPolicy)))
            {
                var pick = policy;
                var tip = $"성체의 {ColonyPopulation.PolicyRate(policy,policy==MilitaryPolicy.Reserve):P0} · 민심 -{ColonyPopulation.PolicySentiment(policy)}";
                var button = MenuTheme.Button(content, (pop.S.policy==policy?"✓ ":"")+ColonyPopulation.PolicyName(policy), () => { if(pop.TrySetPolicy(pick)) Population(); },tip);
                button.interactable = pop.CanUse(policy);
            }
            MenuTheme.Button(content,"병역 나이: "+(pop.S.elderlyService?"늙은 시민 포함":"성체만"),()=>{pop.S.elderlyService=!pop.S.elderlyService;Population();});
            MenuTheme.Text(content,$"시민 생산력 ×{ColonyPopulation.Productivity:0.00}\n주거 ×{ColonyPopulation.HousingProductivity:0.00} · 연구 +{ColonyPopulation.ResearchProductivity:P0} · 시설 +{ColonyPopulation.FacilityProductivity:P0}\n행정 성과 {pop.Administration:P0} · 징수 {pop.TaxCollection:P0}",13,72);
            MenuTheme.Text(content,$"재개발 보상 {pop.S.redevelopCompensation:P0}",15,32);
            void Compensation(float step) { pop.S.redevelopCompensation = Mathf.Clamp(Mathf.Round((pop.S.redevelopCompensation+step)*10f)/10f,GameBalance.RedevelopMinCompensation,GameBalance.RedevelopMaxCompensation);Population(); }
            MenuTheme.Button(content,"보상 −10%",()=>Compensation(-.1f));
            MenuTheme.Button(content,"보상 +10%",()=>Compensation(.1f));
            V5Fit(window);
        }
    }
}
