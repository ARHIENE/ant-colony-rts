using System.Linq;
using System.Collections.Generic;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 연구 화면(2026-10-15): 시대별 목록 → 선택 연구 상세(선행·비용·진행·해금) → 연구소별 담당 장수·대기 사유.
    // 연구 진행 자체는 CampaignResearch.Tick(실제 장수 작업)이 담당한다. 이 화면은 지시·정지·변경만 한다.
    public sealed partial class GameMenuController
    {
        private ScienceTechnology? researchPick;
        private int researchEra = 1;

        private static string ResearchBudgetText(CampaignResearch r, ScienceDefinition target)
        {
            var remaining = new HashSet<ScienceTechnology>();
            void Visit(ScienceTechnology t)
            {
                if (r.Has(t) || !remaining.Add(t)) return;
                foreach (var p in CampaignResearch.Technologies[(int)t].Prerequisites) Visit(p);
            }
            Visit(target.Technology);
            var tier = SceneObjects.All<ScienceLab>().Where(l => !l.IsDead && l.isActiveAndEnabled).Select(l => l.Tier).DefaultIfEmpty(1).Max();
            var steps = r.Has(target.Technology) ? System.Array.Empty<int>() : ScienceLabUpgrades.Steps(tier, target.Tier).ToArray();
            foreach (var step in steps) Visit(ScienceLabUpgrades.RequiredResearch(step));
            int food = 0, material = 0, special = 0; float work = 0;
            foreach (var t in remaining)
            {
                var d = CampaignResearch.Technologies[(int)t];
                var progress = r.Active?.Technology == t ? r.Progress : r.SavedProgress(t);
                work += Mathf.Max(0, d.Work - Mathf.Max(0, progress) - r.PendingInsight(t));
                if (progress >= 0) continue; // 이미 지불한 진행/중단 연구는 다시 비용에 넣지 않는다.
                food += d.Food; material += d.Soil; special += d.Special;
            }
            var upgrades = steps.SelectMany(ScienceLabUpgrades.Cost).GroupBy(c => c.type)
                .Select(g => $"{g.Key.DisplayName()} {g.Sum(c => c.amount)}");
            return $"미완 선행·승급 조건 {remaining.Count}개 · 식량 {food} / 흙 {material} / 특수 {special}\n남은 연구량 {work:0} · 연구소"
                + (steps.Length == 0 ? " 추가 승급 없음" : $" 1동 {tier}→{target.Tier}등급: " + string.Join(" / ", upgrades))
                + "\n건설·표본/핵심물 확보·생활·이동 비용 별도";
        }

        // 실제 코드(ScienceEffects 등)에 연결된 해금 효과만 적는다.
        private static string ResearchEffect(ScienceTechnology t) => t switch
        {
            ScienceTechnology.FungalFarming => "밭 수확량 +20%, 넓은 밭 건설",
            ScienceTechnology.HoneydewRanch => "감로 작물 재배",
            ScienceTechnology.Resin => "흙벽·버섯벽·성벽·문 건설",
            ScienceTechnology.Traps => "끈끈이·가시 함정 건설",
            ScienceTechnology.Herbs => "경상 회복·중상 치료 속도 상승",
            ScienceTechnology.Sanitation => "곰팡이 전파 절반, 치료 시간 단축",
            ScienceTechnology.Fermentation => "창고 용량 증가, 냉장고 건설",
            ScienceTechnology.AdvancedCrops => "고급 균류 재배",
            ScienceTechnology.AcidRefining => "산성탑 피해 +25%, 광역 산탑·방어 연구소",
            ScienceTechnology.Watchtowers => "감시탑 건설",
            ScienceTechnology.Infirmary => "의무실 건설",
            ScienceTechnology.Recreation => "휴게실·오락 가구 건설",
            ScienceTechnology.Blades => "근접 무기·산 분사기 제작, 작업장 건설",
            ScienceTechnology.ArmorPlates => "보호 갑옷 제작",
            ScienceTechnology.Vehicle => "바퀴 차량 건조",
            ScienceTechnology.Drainage => "홍수 면역, 가뭄 성장 감소 완화",
            ScienceTechnology.Firebreaks => "산불 피해 절반·확산 차단",
            ScienceTechnology.EfficientTransport => "수송 간격 단축, 화물 +50%",
            ScienceTechnology.Mines => "폭약 매설",
            ScienceTechnology.Regeneration => "영구 손상 부위 재생 치료",
            ScienceTechnology.Trinkets => "장신구 제작",
            ScienceTechnology.AdvancedWeapons => "방패·지휘 신호기 제작, 품질 상승 확률",
            ScienceTechnology.Gliding => "비행 장비 제작(병영 조건)",
            ScienceTechnology.Aircraft => "비행기 건조",
            ScienceTechnology.HeavyTransport => "병력 정원 +50%, 장수 칸 +2",
            ScienceTechnology.Insulation => "추위 영향 절반, 냉동고 건설",
            ScienceTechnology.MigrationTheory => "로켓 발사대 건설",
            ScienceTechnology.Hull => "로켓 선체 건조",
            ScienceTechnology.Cocoons => "동면 캡슐 건조",
            ScienceTechnology.Engine => "로켓 추진기관 건조(설계도 필요)",
            ScienceTechnology.Fishing => "낚시 작업",
            ScienceTechnology.ConscriptionLaw => "징병제",
            ScienceTechnology.ReserveForces => "예비군제",
            ScienceTechnology.TotalMobilization => "국민개병",
            ScienceTechnology.Pharmacy => "일반 약품 제조(약제대, 의료 작업)",
            ScienceTechnology.PrecisionPharmacy => "고급 약품 제조(약제대, 의료 작업)",
            ScienceTechnology.UndergroundSurvey => "지하 탐사기·심층 채굴기·가스 추출/제련·화학 작업대 건설 · 발견 후 표본 연구 별도",
            ScienceTechnology.FormationLine or ScienceTechnology.FormationColumn or ScienceTechnology.FormationWedge
                or ScienceTechnology.FormationDispersed or ScienceTechnology.FormationCover or ScienceTechnology.FormationFocus => "해당 진형 편성 해금 · 인재·편성에서 효과와 불이익 확인",
            ScienceTechnology.SettlementPromotion => "연구소 2등급 승급 조건(철 40 별도)",
            ScienceTechnology.SteamPromotion => "연구소 3등급 승급 조건(강철 60 별도)",
            ScienceTechnology.PetroleumPromotion => "연구소 4등급 승급 조건(강철 80·부품 10 별도)",
            ScienceTechnology.ElectricPromotion => "연구소 5등급 승급 조건(티타늄 80·전자부품 20 별도)",
            ScienceTechnology.AtomicPromotion => "연구소 6등급 승급 조건(성휘합금 80·전자부품 40 별도)",
            ScienceTechnology.FuturePromotion => "미래 시대 최종 승급 연구(추가 연구소 등급 없음)",
            _ => "—"
        };

        private string ResearchState(CampaignResearch r, ScienceDefinition d)
        {
            if (r.Has(d.Technology)) return "완료";
            if (r.Active == d) return (r.Paused ? "정지" : "진행 중") + $" {r.Progress / d.Work:P0}";
            return r.BlockReason(d.Technology) == "" ? "선택 가능" : "잠김";
        }

        public void ScienceDetails()
        {
            var r = CampaignResearch.Instance;
            if (r == null) { Screen("Science", "과학"); MenuTheme.Text(content, "과학 화면 준비 중입니다."); MenuTheme.Button(content, "Back", Back).GetComponentInChildren<UnityEngine.UI.Text>().text = "뒤로"; return; }
            var window = V5Window("과학 / 연구", "한 번에 하나 · 완료 후 다음 연구는 직접 선택", 676);
            var estimate = L.Label(window, r.TimeEstimate, 12, 230, 633, 690, 30, MenuTheme.Muted);
            refreshDutyScreen = () => estimate.text = r.TimeEstimate;
            L.Button(window, "Science Facilities", "로켓 · 과학 시설", 20, 633, 200, 30, ScienceFacilities);

            // 왼쪽: 시대별 연구 목록
            for (var era = 1; era <= CampaignResearch.EraCount; era++)
            {
                var pick = era;
                L.Tab(window, $"{era}. {CampaignResearch.EraNames[era - 1]}", 12 + (era - 1) * 100, 64, 94, researchEra == era,
                    () => { researchEra = pick; researchPick = null; Science(); });
            }
            L.Line(window, 12, 97, 1076);
            content = V5Section(window, CampaignResearch.EraNames[researchEra - 1] + " 시대", 12, 112, 530, 298);
            var defs = CampaignResearch.Technologies.Where(d => d.Tier == researchEra).ToArray();
            if (!researchPick.HasValue || !defs.Any(d => d.Technology == researchPick)) researchPick = defs.FirstOrDefault()?.Technology;
            foreach (var d in defs)
            {
                var pick = d.Technology;
                var row = L.Cell(content, "Research " + pick, 42, MenuTheme.Plate2);
                L.Button(row, "Pick " + pick, d.Name, 8, 5, 390, 32, () => { researchPick = pick; Science(); }, ResearchEffect(pick), researchPick == pick);
                var rowState = L.Label(row, ResearchState(r, d), 12, 404, 0, 98, 42, MenuTheme.Muted, TextAnchor.MiddleRight);
                refreshDutyScreen += () => rowState.text = ResearchState(r, d);
            }
            if (defs.Length == 0) MenuTheme.Text(content, "연구 항목 미구현", 13, 40);
            // G06: 반입한 표본으로 자원 연구(연구 전에는 표본 회수만 가능, 완료 후 본격 채굴).
            foreach (var sample in r.PendingSamples.ToArray())
            {
                var type = sample; var running = r.ActiveResource == type;
                var row = L.Cell(content, "Sample " + type, 42, MenuTheme.Plate2);
                var sampleButton = L.Button(row, "Sample Research " + type, "표본 연구: " + type.DisplayName(), 8, 5, 390, 32, () => {
                    if (r.ActiveResource == type) r.SetPaused(!r.Paused);
                    else if (!r.TryStartResourceResearch(type)) ToastManager.Show("다른 연구가 진행 중이면 표본 연구를 시작할 수 없습니다.");
                    Science();
                }, "연구량 " + CampaignResearch.ResourceResearchWork + " · 완료하면 이 자원을 본격 채굴 · 진행 중 클릭: 정지/재개", running);
                var sampleState = L.Label(row, "", 12, 404, 0, 98, 42, MenuTheme.Muted, TextAnchor.MiddleRight);
                refreshDutyScreen += () => sampleState.text = r.ResourceResearched(type) ? "완료" : r.ActiveResource == type
                    ? $"{(r.Paused ? "정지" : "진행")} {r.ResourceProgress / CampaignResearch.ResourceResearchWork:P0}" : "표본 확보";
                refreshDutyScreen += () => sampleButton.interactable = !r.ResourceResearched(type) && (r.ActiveResource == type || r.Active == null && r.ActiveResource == null);
            }

            // 오른쪽: 현재 시대에서 선택한 연구 상세와 조작
            var def = researchPick.HasValue ? CampaignResearch.Technologies[(int)researchPick.Value] : null;
            content = V5Section(window, def == null ? "연구 선택" : $"{def.Name} · {CampaignResearch.EraNames[def.Tier - 1]} 시대", 554, 112, 534, 298);
            if (def == null) MenuTheme.Text(content, "선택한 시대에 표시할 연구 항목이 없습니다.", 13, 40);
            else
            {
                float Progress() => r.Has(def.Technology) ? def.Work : r.Active == def ? r.Progress : Mathf.Max(0, r.SavedProgress(def.Technology));
                string Detail()
                {
                    var cost = r.Has(def.Technology) || r.Active == def || r.SavedProgress(def.Technology) >= 0 ? "비용 지불 완료"
                        : $"식량 {def.Food} · 흙 {def.Soil}" + (def.Special > 0 ? $" · 특수 {def.Special}" : "");
                    return $"상태 {ResearchState(r, def)} · 연구량 {Progress():0}/{def.Work:0} · {cost}"
                        + (r.Insight(def.Technology) > 0 ? $" · 천기20%({r.PendingInsight(def.Technology):0} 대기)" : "") + $"\n해금: {ResearchEffect(def.Technology)}"
                        + "\n선행: " + (def.Prerequisites.Length == 0 ? "없음" : string.Join(", ", def.Prerequisites.Select(p => (r.Has(p) ? "✓ " : "✗ ") + CampaignResearch.Technologies[(int)p].Name)));
                }
                var detail = MenuTheme.Text(content, Detail(), 15, 82);
                var meter = L.Cell(content, "ResearchProgress", 12);
                var fill = (RectTransform)L.Meter(meter, 0, 3, 494, 6, Progress() / def.Work, MenuTheme.Accent).GetChild(0);
                var reason = r.BlockReason(def.Technology);
                if (reason == "") reason = r.ResearchCostShortage(def.Technology);
                var actions = L.Cell(content, "Research Commands", 38);
                var start = L.Button(actions, "Start Research", r.Active == def && r.Paused ? "연구 재개" : "연구 지시", 0, 0, 110, 32, () => {
                    if (r.Active == def) r.SetPaused(false);
                    else if (!r.TryStart(def.Technology)) ToastManager.Show("연구를 시작할 수 없습니다. 자원과 선행 연구를 확인하세요.");
                    Science();
                }, reason, primary: true);
                var stop = L.Button(actions, "Stop Research", "연구 정지", 118, 0, 110, 32, () => { r.SetPaused(true); Science(); }, "진행도를 보존합니다.");
                var warning = MenuTheme.Text(content, "", 12, 36); warning.color = MenuTheme.Warning;
                var budget = MenuTheme.Text(content, ResearchBudgetText(r, def), 12, 66); budget.color = MenuTheme.Muted;
                refreshDutyScreen += () => {
                    reason = r.BlockReason(def.Technology);
                    if (reason == "") reason = r.ResearchCostShortage(def.Technology);
                    start.interactable = !r.Has(def.Technology) && (r.Active == def ? r.Paused : reason == "");
                    stop.interactable = r.Active == def && !r.Paused;
                    warning.text = r.Active != def && !r.Has(def.Technology) ? reason : "";
                    detail.text = Detail(); fill.sizeDelta = new Vector2(494 * Mathf.Clamp01(Progress() / def.Work), fill.sizeDelta.y);
                    budget.text = ResearchBudgetText(r, def);
                };
            }

            // 하단: 연구소별 담당 장수·대기 사유
            content = V5Section(window, "연구소 배정", 12, 428, 1076, 176);
            var labs = AntColony.Core.SceneObjects.All<ScienceLab>();
            if (labs.Length == 0) MenuTheme.Text(content, "과학 연구소가 없습니다. 건설 메뉴에서 지으세요.", 15, 30);
            var assigned = labs.Select(l => l.Target).Where(c => c != null).ToHashSet();
            foreach (var lab in labs)
            {
                var captured = lab;
                var row = L.Cell(content, "Lab Assignment", 40);
                L.Label(row, $"연구소 {lab.Tier}등급 · 담당 장수", 13, 0, 0, 240, 36);
                var candidates = SortedCommanders().Where(c => c == lab.Target || (lab.Target == null && !assigned.Contains(c) && !c.IsAwayFromHome && c.CanChangeAllocation && c.AllowsJob(CommanderJobs.Research))).ToArray();
                var names = new[] { "미지정" }.Concat(candidates.Select(c => c.CommanderName)).ToArray();
                var selected = System.Array.IndexOf(candidates, lab.Target) + 1;
                var dropdown = GameMenuSelect.Create(row, names, selected, 250, 2, 260, index => {
                    if (index == 0) captured.ReleaseResearcher();
                    else if (!candidates[index - 1].SendToResearch(captured)) ToastManager.Show("지시할 수 없습니다(경로·작업 중·작업표 연구 금지).");
                    Science();
                });
                dropdown.interactable = lab.Target != null || r.ActiveResource != null || (r.Active != null && lab.Tier >= r.Active.Tier);
                var status = L.Label(row, "", 12, 530, 0, 490, 36, MenuTheme.Muted);
                status.gameObject.AddComponent<MenuTooltip>().Message = $"등급 연구 속도 ×{CampaignResearch.LabMultiplier(lab.Tier):0.00} · 장수 연구 능력·특성·건강/생활 상태 별도\n천기: 동일 장수가 전체 필요량의 75% 이상 기여하면 직접 후속 연구 1개에 20% 통찰. 다음 연구는 직접 선택합니다.";
                refreshDutyScreen += () => { if (captured == null) { status.text = "연구소 소실"; return; } var why = r.LabWaitReason(captured); status.text = why == "" ? $"기여 {r.LabRate(captured):0.00}/초" : why; };
            }
            refreshDutyScreen?.Invoke();
            V5Fit(window);
        }

        // 기존 과학 화면의 로켓·연구소 강화·과학 시설 부분.
        public void ScienceFacilities()
        {
            var window = V5Window("로켓 · 과학 시설", "연구소 강화 · 시설 건설 · 로켓 건조와 탑승");
            L.Tab(window, "연구", 12, 64, 70, false, Science);
            L.Tab(window, "시설", 88, 64, 70, true, ScienceFacilities);
            content = V5Section(window, "연구소 강화", 12, 104, 350, 544);
            var research = CampaignResearch.Instance;
            foreach (var lab in AntColony.Core.SceneObjects.All<ScienceLab>())
            {
                var captured = lab;
                var ready = research != null && lab.Tier < ScienceLab.MaxTier && research.Has(ScienceLabUpgrades.RequiredResearch(lab.Tier));
                var stock = ScienceLabUpgrades.StockStatus(lab.Tier, t => ResourceManager.Instance?.GetAmount(t) ?? 0);
                MenuTheme.Button(content, ScienceLabUpgrades.Description(lab.Tier), () => { if (!captured.TryUpgrade()) ToastManager.Show(stock); ScienceFacilities(); },
                    stock + "\n입고된 재고로 1동만 승급 · 바닥/운반/타지역 재고 제외 · 연구 속도 +25%p")
                    .interactable = ready && !lab.Busy && ScienceLabUpgrades.Cost(lab.Tier).All(c => (ResourceManager.Instance?.GetAmount(c.type) ?? 0) >= c.amount);
                MenuTheme.Text(content, (ready ? "" : lab.Tier < ScienceLab.MaxTier ? "승급 연구 필요 · " : "") + stock, 12, 44);
            }
            content = V5Section(window, "과학 시설 · 로켓", 376, 104, 712, 544);
            RocketSection(research);
            V5Fit(window);
        }
    }
}
