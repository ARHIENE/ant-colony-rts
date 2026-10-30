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
            int food = 0, material = 0, special = 0; float work = 0;
            foreach (var t in remaining)
            {
                var d = CampaignResearch.Technologies[(int)t];
                var progress = r.Active?.Technology == t ? r.Progress : r.SavedProgress(t);
                work += Mathf.Max(0, d.Work - Mathf.Max(0, progress) - r.PendingInsight(t));
                if (progress >= 0) continue; // 이미 지불한 진행/중단 연구는 다시 비용에 넣지 않는다.
                food += d.Food; material += d.Soil; special += d.Special;
            }
            return $"미완 선행 포함 {remaining.Count}개 · 추가 식량 {food} / 재료 {material} / Special {special}\n남은 연구량 {work:0} · 담당 인원/등급/생활 시간에 따라 소요 시간 변동\n연구소 승급·건설·표본/핵심물 확보 비용은 별도";
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
            _ => "—"
        };

        private string ResearchState(CampaignResearch r, ScienceDefinition d)
        {
            if (r.Has(d.Technology)) return "완료";
            if (r.Active == d) return (r.Paused ? "정지" : "진행 중") + $" {r.Progress / d.Work:P0}";
            return r.BlockReason(d.Technology) == "" ? "선택 가능" : "잠김";
        }

        public void Science()
        {
            var r = CampaignResearch.Instance;
            if (r == null) { Screen("Science", "과학"); MenuTheme.Text(content, "과학 화면 준비 중입니다."); MenuTheme.Button(content, "Back", Back); return; }
            var window = V5Window("과학 / 연구", "한 번에 하나 · 완료 후 다음 연구는 직접 선택", 676);
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
                L.Label(row, ResearchState(r, d), 12, 404, 0, 98, 42, MenuTheme.Muted, TextAnchor.MiddleRight);
            }
            if (defs.Length == 0) MenuTheme.Text(content, "연구 항목 미구현", 13, 40);
            // G06: 반입한 표본으로 자원 연구(연구 전에는 표본 회수만 가능, 완료 후 본격 채굴).
            foreach (var sample in r.PendingSamples.ToArray())
            {
                var type = sample; var running = r.ActiveResource == type;
                var row = L.Cell(content, "Sample " + type, 42, MenuTheme.Plate2);
                L.Button(row, "Sample Research " + type, "표본 연구: " + type.DisplayName(), 8, 5, 390, 32, () => {
                    if (!r.TryStartResourceResearch(type)) ToastManager.Show("다른 연구가 진행 중이면 표본 연구를 시작할 수 없습니다.");
                    Science();
                }, "연구량 " + CampaignResearch.ResourceResearchWork + " · 완료하면 이 자원을 본격 채굴", running).interactable = !running && r.Active == null && r.ActiveResource == null;
                L.Label(row, running ? $"진행 중 {r.ResourceProgress / CampaignResearch.ResourceResearchWork:P0}" : "표본 확보", 12, 404, 0, 98, 42, MenuTheme.Muted, TextAnchor.MiddleRight);
            }

            // 오른쪽: 현재 시대에서 선택한 연구 상세와 조작
            var def = researchPick.HasValue ? CampaignResearch.Technologies[(int)researchPick.Value] : null;
            content = V5Section(window, def == null ? "연구 선택" : $"{def.Name} · {CampaignResearch.EraNames[def.Tier - 1]} 시대", 554, 112, 534, 298);
            if (def == null) MenuTheme.Text(content, "선택한 시대에 표시할 연구 항목이 없습니다.", 13, 40);
            else
            {
                var saved = r.SavedProgress(def.Technology);
                var cost = r.Active == def || saved >= 0 ? "비용 지불 완료" : $"식량 {def.Food} · 재료 {def.Soil}" + (def.Special > 0 ? $" · 특수 {def.Special}" : "");
                var progress = r.Active == def ? r.Progress : Mathf.Max(0, saved);
                MenuTheme.Text(content, $"상태 {ResearchState(r, def)} · 연구량 {progress:0}/{def.Work:0} · {cost}" + (r.Insight(def.Technology) > 0 ? $" · 천기20%({r.PendingInsight(def.Technology):0} 대기)" : "") + $"\n해금: {ResearchEffect(def.Technology)}"
                    + "\n선행: " + (def.Prerequisites.Length == 0 ? "없음" : string.Join(", ", def.Prerequisites.Select(p => (r.Has(p) ? "✓ " : "✗ ") + CampaignResearch.Technologies[(int)p].Name))), 15, 82);
                var meter = L.Cell(content, "ResearchProgress", 12);
                L.Meter(meter, 0, 3, 494, 6, progress / def.Work, MenuTheme.Accent);
                var reason = r.BlockReason(def.Technology);
                var actions = L.Cell(content, "Research Commands", 38);
                L.Button(actions, "Start Research", r.Active == def && r.Paused ? "연구 재개" : "연구 지시", 0, 0, 110, 32, () => {
                    if (r.Active == def) r.SetPaused(false);
                    else if (!r.TryStart(def.Technology)) ToastManager.Show("연구를 시작할 수 없습니다. 자원과 선행 연구를 확인하세요.");
                    Science();
                }, reason, primary: true).interactable = !r.Has(def.Technology) && (r.Active == def ? r.Paused : reason == "");
                L.Button(actions, "Stop Research", "연구 정지", 118, 0, 110, 32, () => { r.SetPaused(true); Science(); }, "진행도를 보존합니다.")
                    .interactable = r.Active == def && !r.Paused;
                if (reason != "" && r.Active != def) MenuTheme.Text(content, reason, 12, 36).color = MenuTheme.Warning;
                MenuTheme.Text(content, ResearchBudgetText(r, def), 12, 66).color = MenuTheme.Muted;
            }

            // 하단: 연구소별 담당 장수·대기 사유
            content = V5Section(window, "연구소 배정", 12, 428, 1076, 176);
            var labs = AntColony.Core.SceneObjects.All<ScienceLab>();
            if (labs.Length == 0) MenuTheme.Text(content, "과학 연구소가 없습니다. 건설 메뉴에서 지으세요.", 15, 30);
            var assigned = labs.Select(l => l.Target).Where(c => c != null).ToHashSet();
            foreach (var lab in labs)
            {
                var wait = r.LabWaitReason(lab);
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
                dropdown.interactable = lab.Target != null || (r.Active != null && lab.Tier >= r.Active.Tier);
                L.Label(row, wait == "" ? "참여 가능" : wait, 12, 530, 0, 490, 36, MenuTheme.Muted);
            }
            refreshDutyScreen = null;
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
                MenuTheme.Button(content, $"연구소 {lab.Tier}등급 강화 (식량 {lab.Tier * 60} / 재료 {lab.Tier * 80})", () => { if (!captured.TryUpgrade()) ToastManager.Show("강화 조건을 충족하지 못했거나 자원이 부족합니다."); ScienceFacilities(); }).interactable = lab.Tier < ScienceLab.MaxTier && !lab.Busy;
            }
            content = V5Section(window, "과학 시설 · 로켓", 376, 104, 712, 544);
            RocketSection(research);
            V5Fit(window);
        }
    }
}
