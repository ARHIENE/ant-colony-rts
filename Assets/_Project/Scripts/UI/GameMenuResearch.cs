using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
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
            if (r.Active == d) return (r.Paused ? "정지" : "진행") + $" {r.Progress / d.Work:P0}";
            var saved = r.SavedProgress(d.Technology);
            if (saved >= 0) return $"보존 {saved / d.Work:P0}";
            return r.BlockReason(d.Technology) == "" ? "가능" : "잠김";
        }

        public void Science()
        {
            var r = CampaignResearch.Instance;
            if (r == null) { Screen("Science", "과학"); MenuTheme.Text(content, "과학 화면 준비 중입니다."); MenuTheme.Button(content, "Back", Back); return; }
            var window = V5Window("과학 / 연구", "한 번에 하나의 연구 · 연구소마다 장수 1명 · 완료 후 자동으로 다음 연구를 고르지 않음");
            L.Button(window, "Science Facilities", "로켓 · 과학 시설", 20, 677, 200, 30, ScienceFacilities);

            // 왼쪽: 시대별 연구 목록
            content = V5Section(window, "시대별 연구", 20, 104, 420, 544);
            for (var era = 1; era <= CampaignResearch.EraCount; era++)
            {
                var defs = CampaignResearch.Technologies.Where(d => d.Tier == era).ToArray();
                MenuTheme.Text(content, $"{era}. {CampaignResearch.EraNames[era - 1]} 시대" + (defs.Length == 0 ? " — 연구 항목 미정" : ""), 16, 28);
                foreach (var d in defs)
                {
                    var pick = d.Technology;
                    MenuTheme.Button(content, (researchPick == pick ? "▶ " : "") + $"{d.Name} · {ResearchState(r, d)}", () => { researchPick = pick; Science(); }, ResearchEffect(pick));
                }
            }

            // 오른쪽 위: 선택(없으면 진행 중) 연구 상세와 조작
            var def = researchPick.HasValue ? CampaignResearch.Technologies[(int)researchPick.Value] : r.Active;
            content = V5Section(window, def == null ? "연구 선택" : $"{def.Name} · {CampaignResearch.EraNames[def.Tier - 1]} 시대", 454, 104, 626, 262);
            if (def == null) MenuTheme.Text(content, "왼쪽 목록에서 연구를 고르세요. 진행 중인 연구가 없습니다.", 16, 40);
            else
            {
                var saved = r.SavedProgress(def.Technology);
                var cost = r.Active == def || saved >= 0 ? "비용 지불 완료" : $"식량 {def.Food} · 재료 {def.Soil}" + (def.Special > 0 ? $" · 특수 {def.Special}" : "");
                var progress = r.Active == def ? r.Progress : Mathf.Max(0, saved);
                MenuTheme.Text(content, $"상태 {ResearchState(r, def)} · 연구량 {progress:0}/{def.Work:0} · {cost}\n해금: {ResearchEffect(def.Technology)}"
                    + "\n선행: " + (def.Prerequisites.Length == 0 ? "없음" : string.Join(", ", def.Prerequisites.Select(p => (r.Has(p) ? "✓ " : "✗ ") + CampaignResearch.Technologies[(int)p].Name))), 15, 82);
                var reason = r.BlockReason(def.Technology);
                if (r.Active == def)
                    MenuTheme.Button(content, r.Paused ? "연구 재개" : "연구 정지(진행도 보존)", () => { r.SetPaused(!r.Paused); Science(); });
                else if (!r.Has(def.Technology))
                {
                    var label = r.Active == null ? "이 연구 시작" : $"이 연구로 변경({r.Active.Name} 진행도 보존)";
                    MenuTheme.Button(content, label, () => { if (!r.TryStart(def.Technology)) ToastManager.Show("자원이 부족합니다."); Science(); }, reason).interactable = reason == "";
                    if (reason != "") MenuTheme.Text(content, "<color=#d9534f>" + reason + "</color>", 15, 30);
                }
            }

            // 오른쪽 아래: 연구소별 담당 장수·대기 사유
            content = V5Section(window, "연구소 / 담당 장수", 454, 378, 626, 270);
            var labs = FindObjectsByType<ScienceLab>(FindObjectsSortMode.None);
            if (labs.Length == 0) MenuTheme.Text(content, "과학 연구소가 없습니다. 건설 메뉴에서 지으세요.", 15, 30);
            var assigned = labs.Select(l => l.Target).Where(c => c != null).ToHashSet();
            foreach (var lab in labs)
            {
                var wait = r.LabWaitReason(lab);
                MenuTheme.Text(content, $"연구소 {lab.Tier}등급 · {lab.Target?.CommanderName ?? "담당 없음"} · " + (wait == "" ? "<color=#5cb85c>기여 중</color>" : "<color=#f2c94c>" + wait + "</color>"), 15, 28);
                var captured = lab;
                if (lab.Target != null) MenuTheme.Button(content, "담당 해제", () => { captured.ReleaseResearcher(); Science(); });
                else if (r.Active != null && lab.Tier >= r.Active.Tier)
                    // 다른 연구소에 이미 배정된 장수는 후보에서 뺀다(중복 배정 방지).
                    foreach (var c in SortedCommanders().Where(c => !assigned.Contains(c) && !c.IsAwayFromHome && c.CanChangeAllocation && c.AllowsJob(CommanderJobs.Research)))
                        MenuTheme.Button(content, "연구 지시: " + c.CommanderName, () => { if (!c.SendToResearch(captured)) ToastManager.Show("지시할 수 없습니다(경로·작업 중·작업표 연구 금지)."); Science(); });
            }
            refreshDutyScreen = null;
            V5Fit(window);
        }

        // 기존 과학 화면의 로켓·연구소 강화·과학 시설 부분.
        public void ScienceFacilities()
        {
            Screen("Science / Rocket", "로켓 · 과학 시설");
            var research = CampaignResearch.Instance;
            foreach (var lab in FindObjectsByType<ScienceLab>(FindObjectsSortMode.None))
            {
                var captured = lab;
                MenuTheme.Button(content, $"연구소 {lab.Tier}등급 강화 (식량 {lab.Tier * 60} / 재료 {lab.Tier * 80})", () => { if (!captured.TryUpgrade()) ToastManager.Show("강화 조건을 충족하지 못했거나 자원이 부족합니다."); ScienceFacilities(); }).interactable = lab.Tier < ScienceLab.MaxTier && !lab.Busy;
            }
            RocketSection(research);
            MenuTheme.Button(content, "연구 화면", Science);
            MenuTheme.Button(content, "Back", Back);
        }
    }
}
