using System;
using AntColony.Core;
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
            L.Label(p, "시설·식량·안전·민심이 좋으면 개미가 이주해 옵니다. 살 자리는 주거 건물(건설 → 생활)로 늘립니다.", 14, 24, 54, 792, 30, MenuTheme.Muted);
            L.Label(p, $"인구 <b>{pop.Total}</b> / 살 자리 {pop.HousingCapacity}     어린 {pop.S.young} · 성체 {pool.Total} · 늙은 {pop.S.old}     출전 {pool.Assigned}",
                16, 24, 92, 792, 30);

            L.Label(p, $"<b>민심</b>  {pop.S.sentiment:0} / 100{(pop.Unrest ? "  <color=#d9534f>바닥: 인구 탈주·병력 동원 불가</color>" : "")}", 14, 24, 132, 792, 24);
            L.Meter(p, 24, 158, 792, 8, pop.S.sentiment / 100f, pop.Unrest ? MenuTheme.Danger : MenuTheme.Hp);
            L.Label(p, $"<b>이주 수요</b>  {pop.Demand:0} / 100  <color=#968976>({GameBalance.MinImmigrationDemand} 이상이면 매달 이주)</color>", 14, 24, 176, 792, 24);
            L.Meter(p, 24, 202, 792, 8, pop.Demand / 100f, MenuTheme.Accent);
            (string name, float value)[] causes = { ("식량", pop.FoodDemand), ("시설", pop.FacilityDemand), ("안전", pop.SafetyDemand), ("민심", pop.S.sentiment) };
            for (var i = 0; i < causes.Length; i++)
            {
                L.Label(p, $"{causes[i].name} {causes[i].value:0}", 13, 24 + i * 200, 218, 190, 22, MenuTheme.Muted);
                L.Meter(p, 24 + i * 200, 242, 180, 6, causes[i].value / 100f, MenuTheme.Accent);
            }

            L.Line(p, 24, 266, 792);
            var taxLabel = L.Label(p, "", 15, 24, 278, 792, 26);
            var track = L.Box(p, "TaxSlider", 24, 310, 400, 18, MenuTheme.Well);
            var handle = L.Box(track, "Handle", 0, 0, 16, 18, MenuTheme.Accent);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.wholeNumbers = true; slider.minValue = 0; slider.maxValue = Mathf.RoundToInt(GameBalance.MaxTaxRate * 20); slider.value = Mathf.RoundToInt(pop.S.taxRate * 20);
            void Tax() => taxLabel.text = $"<b>세금</b> {pop.S.taxRate:P0}  →  30초마다 Food +{pop.TaxPerCycle}  <color=#968976>(높을수록 식량↑, 민심·수요↓)</color>";
            slider.onValueChanged.AddListener(v => { pop.SetTaxRate(v / 20f); Tax(); });
            Tax();

            L.Line(p, 24, 344, 792);
            L.Label(p, $"<b>병역 제도</b>  병력 상한 {pop.MaxSoldiers(EnemyAlert.CrisisActive)} (지금 출전 {pool.Assigned})", 15, 24, 356, 792, 26);
            var i2 = 0;
            foreach (MilitaryPolicy policy in Enum.GetValues(typeof(MilitaryPolicy)))
            {
                var captured = policy; var usable = pop.CanUse(policy);
                var rate = ColonyPopulation.PolicyRate(policy, policy == MilitaryPolicy.Reserve);
                var tip = $"성체의 {rate:P0}까지 병력, 민심 매달 -{ColonyPopulation.PolicySentiment(policy)}"
                    + (policy == MilitaryPolicy.Reserve ? "\n침입 중에만 25%, 평소 5%. 원정에는 못 데려감" : "")
                    + (policy == MilitaryPolicy.Total ? "\n세금 절반" : policy != MilitaryPolicy.Volunteer ? "\n출전 병사는 세금을 안 냄" : "")
                    + (usable ? "" : "\n과학 연구로 해금");
                var button = L.Button(p, "Policy " + policy, (pop.S.policy == policy ? "● " : "") + ColonyPopulation.PolicyName(policy),
                    24 + i2++ * 200, 390, 190, 40, () => { if (pop.TrySetPolicy(captured)) Population(); }, tip, pop.S.policy == policy);
                button.interactable = usable;
            }
            L.Button(p, "ElderlyService", "병역 나이: " + (pop.S.elderlyService ? "늙은 개미 포함" : "성체만"), 24, 444, 300, 36,
                () => { pop.S.elderlyService = !pop.S.elderlyService; Population(); }, "늙은 개미도 병사로 쓸지 정합니다(병력 상한에 포함, 모자라면 늙은 개미로 보충).");
            L.Button(p, "Close", "닫기", 676, 600, 140, 40, Resume, null, true);
        }
    }
}
