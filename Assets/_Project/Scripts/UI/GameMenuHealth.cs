using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 장수 건강 화면(2026-10-15): 상태(능력·부위·질병) → 치료 설정 → 수술 목록·대기 사유. 실제 처치는 의료 장수가 와서 한다.
    public sealed partial class GameMenuController
    {
        private (CommanderAnt who, SurgeryKind kind, BodyPart part)? surgeryConfirm;

        public static string HealthLines(HealthState h)
        {
            var lines = h.wounds.GroupBy(w => w.part).Select(g => $"{MedicalRules.PartName(g.Key)} {h.PartFraction(g.Key):P0}: " + string.Join(", ", g.Select(w =>
                MedicalRules.WoundName(w.kind) + (w.Permanent ? "" : w.NeedsTend ? " · 미처치" : $" · 처치 {w.quality:P0}")))).ToList();
            lines.AddRange(h.illnesses.Select(i => $"{MedicalRules.IllnessName(i.kind)} 심각도 {i.severity:P0} · 면역 {i.immunity:P0}"
                + (i.tendLeft > 0 ? $" · 다음 투약까지 {i.tendLeft:0}초" : " · 투약 필요")));
            if (h.bloodLoss > .01f) lines.Add($"출혈량 {h.bloodLoss:P0}" + (h.Bleeding ? $" · 출혈 중(예상 사망 {h.SecondsToBleedOut:0}초)" : ""));
            return lines.Count == 0 ? "이상 없음" : string.Join("\n", lines);
        }

        public void ShowHealth(CommanderAnt c)
        {
            if (c == null) { Resume(); return; }
            var h = c.Health;
            var window = V5Window($"{c.CommanderName} / 건강", "상태 → 치료 설정 → 수술 · 처치 완료와 완치는 다릅니다(시간에 따라 회복)");
            L.Button(window, "Back To Commander", "장수 상세", 20, 677, 160, 30, () => Details(c));

            content = V5Section(window, $"상태 · {h.Summary}", 20, 104, 520, 544);
            var status = MenuTheme.Text(content, "", 15, 150);
            var body = MenuTheme.Text(content, "", 15, 300);
            void Refresh()
            {
                if (c == null) return;
                status.text = $"의식 {h.Consciousness:P0} · 이동 {h.Moving:P0} · 조작 {h.Manipulation:P0}\n시력 {h.Sight:P0} · 통증 {h.Pain:P0}"
                    + $"\n출혈량 {h.bloodLoss:P0}" + (h.Bleeding ? $" · <color=#d9534f>출혈 중 — 예상 사망 {h.SecondsToBleedOut:0}초</color>" : "")
                    + $"\n위치: " + (c.TreatmentFacility != null ? "의료 침상" : c.RescueCarrier != null ? $"구조 운반 중({c.RescueCarrier.CommanderName})" : c.PersonalHealth <= 0 ? "쓰러짐(현장)" : "활동 중")
                    + $" · 배고픔 {100 - c.Satiety:0}" + (c.IsMedicalPatient && c.Satiety < AntColony.Core.GameBalance.EatBelowSatiety ? " · 급식 대기" : "")
                    + "\n" + (CommanderAnt.CareWaitReason(c) is string wait && wait != "" ? "<color=#f2c94c>대기: " + wait + "</color>" : h.NeedsTend ? "의료 장수가 처치하러 옵니다" : "처치 완료 또는 필요 없음");
                body.text = HealthLines(h);
            }
            Refresh(); refreshDutyScreen = Refresh;

            content = V5Section(window, "치료 설정", 554, 104, 526, 262);
            var meds = L.Cell(content, "MedicinePolicy", 44);
            var i = 0;
            foreach (MedicineLevel m in System.Enum.GetValues(typeof(MedicineLevel)))
            {
                var pick = m;
                L.Button(meds, "Medicine " + m, MedicalRules.MedicineName(m), i++ * 100, 0, 96, 40, () => { h.medicine = pick; ShowHealth(c); }, primary: h.medicine == m);
            }
            MenuTheme.Button(content, (h.selfTend ? "☑" : "☐") + " 자가 처치 허용(의식·조작이 있을 때, 품질 불리)", () => { h.selfTend = !h.selfTend; ShowHealth(c); });
            var rest = L.Cell(content, "BedRest", 44);
            L.Label(rest, h.bedRest == 0 ? "병상 휴식: 쉬지 않음" : $"병상 휴식 우선순위 {h.bedRest}(작업 우선순위 이상이면 휴식)", 14, 0, 6, 380, 30, MenuTheme.Muted);
            L.Button(rest, "BedRest Minus", "−", 400, 0, 44, 40, () => { h.bedRest = Mathf.Max(0, h.bedRest - 1); ShowHealth(c); });
            L.Button(rest, "BedRest Plus", "+", 450, 0, 44, 40, () => { h.bedRest = Mathf.Min(9, h.bedRest + 1); ShowHealth(c); });

            content = V5Section(window, "수술", 554, 378, 526, 270);
            var op = h.surgeries.FirstOrDefault(s => s.Active);
            var last = h.surgeries.LastOrDefault(s => !s.Active);
            if (last != null) MenuTheme.Text(content, $"최근: {CommanderAnt.SurgeryName(last.kind)}({MedicalRules.PartName(last.part)}) {last.result}", 14, 26);
            if (op != null)
            {
                var surgeon = op.surgeonId != "" ? CommanderAnt.Active.OfType<CommanderAnt>().FirstOrDefault(x => x.PersonalState.id == op.surgeonId) : CommanderAnt.AutoSurgeon(c);
                var wait = CommanderAnt.SurgeryBlock(c, op, null);
                MenuTheme.Text(content, $"{CommanderAnt.SurgeryName(op.kind)} · {MedicalRules.PartName(op.part)} · " + (op.stage == SurgeryStage.Started ? $"진행 {op.work / CommanderAnt.SurgeryWork:P0}" : "준비 중(물자 예약)")
                    + $"\n필요: " + string.Join(", ", CommanderAnt.SurgeryCost(op.kind).Select(x => $"{x.type.DisplayName()} {x.amount}"))
                    + $" · 집도의: {(op.surgeonId == "" ? "자동" : "지정")} {surgeon?.CommanderName ?? "없음"} · 예상 성공 {c.PredictedSuccess(op, surgeon):P0}"
                    + (wait != "" ? $"\n<color=#f2c94c>대기: {wait}</color>" : ""), 14, 70);
                if (op.stage == SurgeryStage.Waiting)
                {
                    MenuTheme.Button(content, "수술 취소(예약 해제)", () => { CommanderAnt.CancelSurgery(c, op); ShowHealth(c); });
                    MenuTheme.Button(content, "집도의: 자동(능력 높은 순)", () => { op.surgeonId = ""; ShowHealth(c); });
                    foreach (var d in SortedCommanders().Where(d => d != c && d.AllowsJob(CommanderJobs.Nursing)))
                    { var doc = d; MenuTheme.Button(content, $"집도의 지정: {d.CommanderName} · 의료 {d.Talents.Level(CommanderActivity.Medicine)}", () => { op.surgeonId = doc.PersonalState.id; ShowHealth(c); }); }
                }
                else MenuTheme.Text(content, "시작한 수술은 취소할 수 없습니다.", 14, 24);
            }
            else
            {
                foreach (var part in System.Enum.GetValues(typeof(BodyPart)).Cast<BodyPart>())
                    foreach (var kind in new[] { SurgeryKind.Amputate, SurgeryKind.Regenerate })
                    {
                        if (CommanderAnt.RequestBlock(c, kind, part) != "") continue;
                        if (kind == SurgeryKind.Amputate && !h.wounds.Any(w => w.part == part && !w.Permanent) && !h.illnesses.Any(x => x.part == part)) continue; // 손상 없는 부위 절단은 목록에서 뺀다
                        var (pk, pp) = (kind, part);
                        var confirming = surgeryConfirm is var sc && sc.HasValue && sc.Value.who == c && sc.Value.kind == pk && sc.Value.part == pp;
                        var label = $"{CommanderAnt.SurgeryName(kind)}: {MedicalRules.PartName(part)}";
                        if (kind == SurgeryKind.Amputate && !confirming) label += " (되돌릴 수 없음 — 확인 필요)";
                        if (confirming) label = $"확인: {MedicalRules.PartName(part)} 절단 → 부위 상실(복구는 재생 수술만)";
                        MenuTheme.Button(content, label, () =>
                        {
                            if (pk == SurgeryKind.Amputate && !confirming) { surgeryConfirm = (c, pk, pp); ShowHealth(c); return; }
                            surgeryConfirm = null; CommanderAnt.RequestSurgery(c, pk, pp); ShowHealth(c);
                        });
                    }
                MenuTheme.Text(content, "부품 대체 수술은 부품 목록이 정해지면 추가됩니다.", 13, 22);
            }
            V5Fit(window);
        }
    }
}
