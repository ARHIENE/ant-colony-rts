using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 포로 관리(2026-10-15): 실제 수용소 포로의 건강·급식·치료 설정·경비·석방, 귀환 중인 장수 목록.
    public sealed partial class GameMenuController
    {
        private Prisoner prisonerPick; private bool releaseConfirm, executeConfirm;

        public void ShowPrisoners(PrisonerCamp camp)
        {
            if (camp == null || !camp.isActiveAndEnabled) { Resume(); return; }
            if (prisonerPick == null || !camp.Prisoners.Contains(prisonerPick)) { prisonerPick = camp.Prisoners.FirstOrDefault(); releaseConfirm = executeConfirm = false; }
            var window = V5Window("포로 수용소", $"정원 {camp.Count}/{camp.Capacity} · 경비 {(camp.Guarded ? "있음(탈출 시 제압 판정)" : "없음")} · 움직일 수 없는 포로는 탈출하지 않음");
            content = V5Section(window, "포로", 20, 104, 420, 330);
            if (camp.Count == 0) MenuTheme.Text(content, "수용 중인 포로가 없습니다.", 15, 30);
            foreach (var p in camp.Prisoners.ToArray())
            {
                var pick = p; var h = p.PersonalState.health;
                MenuTheme.Button(content, (p == prisonerPick ? "▶ " : "") + $"{p.Name} · {h?.Summary ?? "건강"} · 배고픔 {100 - p.PersonalState.meal.satiety:0}", () => { prisonerPick = pick; releaseConfirm = executeConfirm = false; ShowPrisoners(camp); });
            }
            var dm = DiplomacyManager.Instance;
            content = V5Section(window, "귀환 중", 20, 446, 420, 202);
            var returning = dm != null ? dm.Data.homecomings : null;
            if (returning == null || returning.Count == 0) MenuTheme.Text(content, "귀환 중인 장수가 없습니다.", 15, 30);
            else foreach (var r in returning)
                    MenuTheme.Text(content, $"{r.commander?.Name} · {(string.IsNullOrEmpty(r.to) ? "우리 본거지로" : "상대 세력으로")} · 남은 {(r.arrive - dm.Data.elapsed) / GameCalendar.SecondsPerMonth:0.0}개월"
                        + (r.gear.Count > 0 ? $" · 동반 장비 {r.gear.Count}" : ""), 14, 26);

            content = V5Section(window, prisonerPick == null ? "선택 없음" : prisonerPick.Name, 454, 104, 626, 544);
            if (prisonerPick == null) { V5Fit(window); return; }
            var ph = prisonerPick.PersonalState.health ??= new HealthState();
            MenuTheme.Text(content, $"{ph.Summary} · 의식 {ph.Consciousness:P0} · 이동 {ph.Moving:P0} · 기분 {PrisonerCamp.MoodOf(prisonerPick):0}\n" + HealthLines(ph), 14, 90);
            var meds = L.Cell(content, "PrisonerMedicine", 44); var i = 0;
            foreach (MedicineLevel m in System.Enum.GetValues(typeof(MedicineLevel)))
            { var pickM = m; L.Button(meds, "PMed " + m, MedicalRules.MedicineName(m), i++ * 100, 0, 96, 40, () => { ph.medicine = pickM; ShowPrisoners(camp); }, primary: ph.medicine == m); }
            MenuTheme.Text(content, "치료는 의료 장수, 급식은 운반 장수가 수용소에 와서 합니다.", 14, 24);
            var p0 = prisonerPick;
            MenuTheme.Button(content, releaseConfirm ? "확인: 조건 없이 석방(거리 비례 안전 귀환, 압수 장비는 함께 보내지 않음)" : "석방", () =>
            { if (releaseConfirm) camp.ReleaseFree(p0); releaseConfirm = !releaseConfirm; ShowPrisoners(camp); });
            MenuTheme.Button(content, executeConfirm ? "확인: 처형" : "처형(확인 필요)", () => { if (executeConfirm) camp.Execute(p0); executeConfirm = !executeConfirm; ShowPrisoners(camp); });
            refreshDutyScreen = null;
            V5Fit(window);
        }
    }
}
