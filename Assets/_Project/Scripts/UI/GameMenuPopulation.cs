using System;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // Phase 4 인구 창: 인구 구성·살 자리·민심·이주 수요(원인 4가지)·세금 슬라이더·병역 제도·병역 나이.
    public sealed partial class GameMenuController
    {
        public void Population()
        {
            var pop = ColonyPopulation.Instance; var pool = AntPool.Instance;
            if (pop == null || pool == null) return;
            var f = Frame("인구");
            var p = L.Plate(f, "Population", 300, 100, 840, 660);
            L.Label(p, "인구 · 이주", 26, 24, 12, 792, 40, MenuTheme.Accent);
            L.Label(p, "주거·시설·안전·민심이 좋으면 시민이 이주해 옵니다. 살 자리는 주거 건물(건설 → 생활)로 늘립니다.", 14, 24, 54, 792, 30, MenuTheme.Muted);
            L.Label(p, $"인구 <b>{pop.Total}</b> / 살 자리 {pop.HousingCapacity}     어린 {pop.S.young} · 성체 {pool.Total} · 늙은 {pop.S.old}     출전 {pool.Assigned}",
                16, 24, 92, 792, 30).gameObject.AddComponent<MenuTooltip>().Message = "노년 시민은 연간 15% 자연사(소수 이월)합니다. 올해 노년이 된 시민은 다음 해부터 대상입니다. 동원·예약 노년 정산 대기분은 복귀 전 제외하며, 장수의 노화는 별도입니다.";

            L.Label(p, $"<b>민심</b>  {pop.S.sentiment:0} / 100{(pop.Unrest ? "  <color=#d9534f>바닥: 인구 탈주·병력 동원 불가</color>" : "")}", 14, 24, 132, 792, 24);
            L.Meter(p, 24, 158, 792, 8, pop.S.sentiment / 100f, pop.Unrest ? MenuTheme.Danger : MenuTheme.Hp);
            var nextArrival = pop.S.immigrationStopped ? "수용 중단" : $"다음 {GameBalancePopulation.ImmigrationSeconds - pop.S.immigrationSeconds:0}초";
            L.Label(p, $"<b>이주 수요</b>  {pop.Demand:0} / 200  <color=#b1b3a7>(3일마다 빈 정원 × 10% × 수요/100 · 서비스 {pop.PublicServiceCoverage:P0} · {nextArrival})</color>", 13, 24, 176, 600, 24);
            // G33: 수용 중단 중에는 주기 시간과 소수 누적이 모두 멈춘다(ColonyPopulation.Tick).
            L.Button(p, "Immigration Stop", pop.S.immigrationStopped ? "이주 수용 재개" : "이주 수용 중단", 632, 172, 184, 28,
                () => { pop.SetImmigrationStopped(!pop.S.immigrationStopped); Population(); }, "중단하면 새 시민이 오지 않고, 이주 주기와 누적도 멈춥니다.", pop.S.immigrationStopped);
            L.Meter(p, 24, 202, 792, 8, pop.Demand / 100f, MenuTheme.Accent);
            (string name, float value)[] causes = { ("주거", pop.HousingDemand), ("시설", pop.FacilityDemand), ("안전", pop.SafetyDemand), ("민심", pop.S.sentiment) };
            for (var i = 0; i < causes.Length; i++)
            {
                L.Label(p, $"{causes[i].name} {causes[i].value:0}", 13, 24 + i * 200, 218, 190, 22, MenuTheme.Muted);
                L.Meter(p, 24 + i * 200, 242, 180, 6, causes[i].value / 100f, MenuTheme.Accent);
            }

            L.Line(p, 24, 266, 792);
            var taxLabel = L.Label(p, "", 15, 24, 278, 600, 26);
            // 우선 납부 재료(2026-10-10): 지역 기초 원재료 3종 중 하나. 누를 때마다 다음 재료로.
            var basics = ColonyPopulation.TaxMaterials; var mainIndex = Mathf.Max(0, Array.IndexOf(basics, pop.S.taxMaterial));
            L.Button(p, "TaxMaterial", $"우선 납부: {basics[mainIndex].DisplayName()} ▸", 632, 276, 184, 30,
                () => { pop.SetTaxMaterial(basics[(mainIndex + 1) % basics.Length]); Population(); },
                $"재료 세금의 {GameBalance.TaxMainMaterialShare:P0}를 이 재료로, 나머지는 {string.Join("·", Array.ConvertAll(basics, b => b.DisplayName()))}로 고르게 받습니다.");
            var track = L.Box(p, "TaxSlider", 24, 310, 400, 18, MenuTheme.Well);
            var handle = L.Box(track, "Handle", 0, 0, 16, 18, MenuTheme.Accent);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.wholeNumbers = true; slider.minValue = 0; slider.maxValue = Mathf.RoundToInt(GameBalance.MaxTaxRate * 100); slider.value = Mathf.RoundToInt(pop.S.taxRate * 100);
            var input = MenuTheme.Input(p, Mathf.RoundToInt(pop.S.taxRate * 100).ToString()); input.name = "TaxInput";
            L.Place((RectTransform)input.transform, 436, 304, 70, 30); input.characterLimit = 3; input.textComponent.fontSize = 14;
            void Tax() => taxLabel.text = $"<b>세금</b> {pop.S.taxRate:P0} · {ColonyPopulation.FocusName(pop.S.taxFocus)}  →  다음 식량 +{pop.NextFoodTax} · 재료 +{pop.NextMaterialTax}"
                + $"  <color=#b1b3a7>(다음 납세 {pop.NextTaxSeconds:0}초 · 높을수록 민심↓)</color>";
            slider.onValueChanged.AddListener(v => { pop.SetTaxRate(v / 100f); input.SetTextWithoutNotify(((int)v).ToString()); Tax(); });
            input.onEndEdit.AddListener(v => { if (int.TryParse(v, out var n)) pop.SetTaxRate(n / 100f); slider.SetValueWithoutNotify(Mathf.RoundToInt(pop.S.taxRate * 100)); input.SetTextWithoutNotify(Mathf.RoundToInt(pop.S.taxRate * 100).ToString()); Tax(); });
            var fx = 520;
            foreach (TaxFocus focus in Enum.GetValues(typeof(TaxFocus)))
            {
                var captured = focus;
                L.Button(p, "TaxFocus " + focus, (pop.S.taxFocus == focus ? "● " : "") + ColonyPopulation.FocusName(focus), fx, 302, 96, 32,
                    () => { pop.SetTaxFocus(captured); Population(); }, $"식량 {ColonyPopulation.FoodShare(focus):P0} · 재료 {1 - ColonyPopulation.FoodShare(focus):P0}\n총세액·민심 효과는 같습니다. 부족한 쪽으로 납세 비율만 바꿉니다. 재료 납세에는 광석·연료·가공품이 포함되지 않습니다.", pop.S.taxFocus == focus);
                fx += 100;
            }
            Tax();
            taxLabel.gameObject.AddComponent<MenuTooltip>().Message = "현재 인구·세율·생산력 유지 시 다음 지급 예상액입니다. 이미 누적된 세액과 남은 기간의 행정 성과 감소를 반영합니다. 인구·동원·방침이 바뀌면 이후 구간만 달라집니다.\n30% 초과는 민심 불이익이 더 커집니다. 40% 초과는 민심이 높아도 매월 자유 시민 일부가 떠납니다. 단기 징수액과 이주·인력 유지를 함께 비교하세요.";

            L.Line(p, 24, 344, 792);
            L.Label(p, $"<b>병역 제도</b>  병력 상한 {pop.MobilizationLimit(EnemyAlert.CrisisActive)} (지금 출전 {pool.Assigned})", 15, 24, 356, 792, 26);
            var i2 = 0;
            foreach (MilitaryPolicy policy in Enum.GetValues(typeof(MilitaryPolicy)))
            {
                var captured = policy; var usable = pop.CanUse(policy);
                var rate = ColonyPopulation.PolicyRate(policy, policy == MilitaryPolicy.Reserve);
                var tip = $"성체의 {rate:P0}까지 병력, 민심 매달 -{ColonyPopulation.PolicySentiment(policy)}"
                    + (policy == MilitaryPolicy.Reserve ? "\n침입 중에만 25%, 평소 5%. 원정에는 못 데려감" : "")
                    + (policy == MilitaryPolicy.Total ? "\n세금 절반" : "")
                    + (usable ? "" : "\n과학 연구로 해금");
                var button = L.Button(p, "Policy " + policy, (pop.S.policy == policy ? "● " : "") + ColonyPopulation.PolicyName(policy),
                    24 + i2++ * 200, 390, 190, 40, () => { if (pop.TrySetPolicy(captured)) Population(); }, tip, pop.S.policy == policy);
                button.interactable = usable;
            }
            L.Button(p, "ElderlyService", "병역 나이: " + (pop.S.elderlyService ? "늙은 시민 포함" : "성체만"), 24, 444, 300, 36,
                () => { pop.S.elderlyService = !pop.S.elderlyService; Population(); }, "늙은 시민도 병사로 쓸지 정합니다(병력 상한에 포함, 모자라면 늙은 시민으로 보충).");
            // 2026-10-08: 시민 생산력·행정 성과·재개발 보상 수준(수치 잠정).
            L.Line(p, 24, 492, 792);
            L.Label(p, $"<b>시민 생산력</b> ×{ColonyPopulation.Productivity:0.00}  <color=#b1b3a7>(주거 ×{ColonyPopulation.HousingProductivity:0.00} · 연구 +{ColonyPopulation.ResearchProductivity:P0} · 시설 +{ColonyPopulation.FacilityProductivity:P0})</color>", 14, 24, 502, 792, 24);
            L.Label(p, $"<b>행정 성과</b> {pop.Administration:P0}  <color=#b1b3a7>(징수 {pop.TaxCollection:P0} · 민심 +{GameBalance.AdminSentiment * pop.Administration:0.#} · 이주 +{GameBalance.AdminDemand * pop.Administration:0.#} · 행정 책상에서 정치 장수가 업무)</color>", 14, 24, 528, 792, 24);
            L.Label(p, $"<b>재개발 보상</b> 기본의 {pop.S.redevelopCompensation:P0}", 14, 24, 560, 300, 30);
            void Compensation(float step) { pop.S.redevelopCompensation = Mathf.Clamp(Mathf.Round((pop.S.redevelopCompensation + step) * 10f) / 10f, GameBalance.RedevelopMinCompensation, GameBalance.RedevelopMaxCompensation); Population(); }
            L.Button(p, "Compensation Down", "−10%", 236, 558, 70, 32, () => Compensation(-.1f), "보상이 기본보다 적으면 재개발 때 민심이 내려갑니다.");
            L.Button(p, "Compensation Up", "+10%", 312, 558, 70, 32, () => Compensation(.1f), "보상을 올리면 비용이 늘고 불만은 줄어듭니다.");
            L.Button(p, "Close", "닫기", 676, 600, 140, 40, Resume, null, true);
            L.Button(p, "Civic Services", "공공서비스 · 대피", 24, 600, 200, 40, CivicServices);
        }
    }
}
