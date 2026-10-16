using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.World;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 목장 개체 목록(2026-10-11 기획): 필터(종·성장·길들임·부상·현재 작업)·정렬(이름·나이·건강·허기)은 표시에만 영향.
    // 선택한 개체에 포획·운반·도축·방생 일괄 요청과 일괄 취소. 실행 불가 개체는 사유·마릿수를 보여 주고 빼며, 확인은 두 번 눌러 실제 대상을 본 뒤 실행한다.
    // 여러 종 일괄 운반은 선택한 모든 종을 받는 유효한 우리만 목적지. 일괄 요청은 별도 우선권 없이 기존 작업 우선순위를 따른다.
    public sealed partial class GameMenuController
    {
        private readonly HashSet<Critter> picked = new HashSet<Critter>();
        private string pendingBulk; // 확인 대기 중인 일괄 요청
        private static Species? fltSpecies; private static CritterStage? fltStage; private static int fltTame, fltHurt, fltWork, fltDisease, sortBy; // 0 = 전체
        private static readonly string[] SortNames = { "이름", "나이", "건강", "허기" };

        private static bool Working(Critter a) => a.Clinic != null || a.CaptureDesignated || a.TransportTo != null || a.SlaughterOrdered;
        private static string WorkLabel(Critter a) => a.Clinic != null ? "치료 시설" : a.SlaughterStarted ? "도축 중" : a.SlaughterOrdered ? "도축 대기"
            : a.Carrier != null ? "운반 중" : a.TransportTo != null ? "운반 요청" : a.CaptureDesignated ? "포획 요청" : a.Bound ? "결박" : a.Handler != null ? "작업 중" : a.Eating ? "먹는 중" : "—";

        // 이 우리의 개체 + 원래 이 우리 소속인 치료 시설 환자 + 우리 밖에서 결박·쓰러짐·포획 요청된 개체(데려오기용).
        private static List<Critter> RanchCandidates(Ranch r) => Critter.All.Where(c => c != null && c.Monster != null && !c.Monster.IsDead
            && (c.Pen == r && c.Clinic == null || c.Clinic != null && c.ReturnPen == r || c.Pen == null && c.Clinic == null && (c.Bound || c.Downed || c.CaptureDesignated))).ToList();

        private void RanchList(Ranch r)
        {
            var all = RanchCandidates(r);
            picked.RemoveWhere(a => a == null || a.Monster == null || a.Monster.IsDead || !all.Contains(a));
            var list = all.Where(a => (fltSpecies == null || a.Species == fltSpecies) && (fltStage == null || a.Stage == fltStage)
                && (fltDisease == 0 || fltDisease == 1 && a.Sick || fltDisease == 2 && a.Contagious || fltDisease == 3 && a.TreatOrder)
                && (fltTame == 0 || (fltTame == 1) == a.Tame) && (fltHurt == 0 || (fltHurt == 1) == a.Injured) && (fltWork == 0 || (fltWork == 1) == Working(a)));
            list = sortBy switch { 1 => list.OrderByDescending(a => a.Age), 2 => list.OrderBy(a => a.HealthFraction), 3 => list.OrderBy(a => a.Hunger), _ => list.OrderBy(a => a.DisplayName) };
            var shown = list.ToList();

            MenuTheme.Text(content, $"<b>개체 목록</b> {shown.Count}/{all.Count}마리 — 필터·정렬은 표시에만 영향을 주며 작업 순서를 바꾸지 않습니다.", 17, 34);
            var species = System.Enum.GetValues(typeof(Species)).Cast<Species>().ToList();
            var listContent = content;
            content = V5FilterRow(listContent);
            MenuTheme.Button(content, $"종: {(fltSpecies is Species sp ? SpeciesInfo.For(sp).name : "전체")} ▸", () => { fltSpecies = fltSpecies == null ? species[0] : species.IndexOf(fltSpecies.Value) + 1 < species.Count ? species[species.IndexOf(fltSpecies.Value) + 1] : (Species?)null; ShowRanch(r); });
            MenuTheme.Button(content, $"성장: {(fltStage == null ? "전체" : fltStage == CritterStage.Baby ? "새끼" : fltStage == CritterStage.Adult ? "성체" : "노령")} ▸", () => { fltStage = fltStage == null ? CritterStage.Baby : fltStage == CritterStage.Old ? (CritterStage?)null : fltStage + 1; ShowRanch(r); });
            MenuTheme.Button(content, $"길들임: {new[] { "전체", "길들임", "야생" }[fltTame]} ▸", () => { fltTame = (fltTame + 1) % 3; ShowRanch(r); });
            MenuTheme.Button(content, $"부상: {new[] { "전체", "부상", "정상" }[fltHurt]} ▸", () => { fltHurt = (fltHurt + 1) % 3; ShowRanch(r); });
            MenuTheme.Button(content, $"현재 작업: {new[] { "전체", "요청·치료 중", "없음" }[fltWork]} ▸", () => { fltWork = (fltWork + 1) % 3; ShowRanch(r); });
            MenuTheme.Button(content, $"정렬: {SortNames[sortBy]} ▸", () => { sortBy = (sortBy + 1) % SortNames.Length; ShowRanch(r); });
            MenuTheme.Button(content, $"질병: {new[] { "전체", "증상 있음", "전염성", "치료 지시" }[fltDisease]} ▸", () => { fltDisease = (fltDisease + 1) % 4; ShowRanch(r); });
            content = listContent;
            MenuTheme.Button(content, $"표시된 {shown.Count}마리 모두 선택", () => { picked.UnionWith(shown); pendingBulk = null; ShowRanch(r); });
            MenuTheme.Button(content, $"선택 해제({picked.Count}마리 선택됨)", () => { picked.Clear(); pendingBulk = null; ShowRanch(r); }).interactable = picked.Count > 0;

            BulkActions(r);

            foreach (var a in shown)
            {
                var at = a.Clinic != null ? " · 치료 시설" : a.Pen == null ? " · 우리 밖" : "";
                var row = MenuLayout.Cell(content, "Animal " + a.DisplayName, 74, MenuTheme.Plate2);
                MenuLayout.Button(row, "Select", picked.Contains(a) ? "✓" : "", 4, 20, 32, 32,
                    () => { if (!picked.Remove(a)) picked.Add(a); pendingBulk = null; ShowRanch(r); });
                MenuLayout.Label(row, $"{a.DisplayName} · {a.StageName} · {(a.Tame ? "길들임" : "야생")}{at}", 15, 46, 4, 510, 30);
                MenuLayout.Label(row, $"체력 {a.HealthFraction:P0}{(a.HeavyInjury ? " 중상" : a.Injured ? " 경상" : "")} · 배고픔 {a.Hunger:0} · {(a.Sick ? a.Contagious ? "전염성 질환" : "질환" : WorkLabel(a))}", 14, 46, 36, 510, 30, MenuTheme.Muted);
                MenuLayout.Button(row, "Details", "상세", 570, 20, 64, 32, () => ShowCritter(a));
            }
        }

        // 실행 불가 사유(없으면 null).
        private static string CaptureBlock(Critter a) => a.Bound ? "이미 결박" : a.Clinic != null ? "치료 시설 안" : a.Carrier != null ? "운반 중" : a.CaptureDesignated ? "이미 요청됨" : null;
        private static string CarryBlock(Critter a) => a.Clinic != null && !a.Carriable ? "치료 시설 안" : a.Carrier != null ? "운반 중" : !a.Carriable ? "결박·쓰러짐 아님" : null;
        private static string ReleaseBlock(Critter a) => a.CanRelease ? null : "치료 시설 안";
        private static string Excluded(IEnumerable<Critter> list, System.Func<Critter, string> block)
        {
            var groups = list.Select(block).Where(x => x != null).GroupBy(x => x).Select(g => $"{g.Key} {g.Count()}").ToList();
            return groups.Count > 0 ? " · 제외: " + string.Join(", ", groups) : "";
        }

        private void BulkActions(Ranch r)
        {
            if (picked.Count == 0) { MenuTheme.Text(content, "개체를 눌러 선택하면 포획·운반·도축·방생을 한 번에 요청할 수 있습니다.", 15, 28); return; }
            var sel = picked.ToList();
            void Confirmable(string key, string label, List<Critter> ok, string excluded, System.Action apply, bool danger = false)
            {
                var b = MenuTheme.Button(content, pendingBulk == key ? $"<color={(danger ? "#d9534f" : "#f2c94c")}>확인: {label} — {Names(ok)}</color> (다시 누르면 실행)" : $"{label} {ok.Count}마리{excluded}",
                    () => { if (pendingBulk == key) { apply(); pendingBulk = null; } else pendingBulk = key; ShowRanch(r); });
                b.interactable = ok.Count > 0;
            }
            MenuTheme.Text(content, $"<b>선택 {sel.Count}마리 일괄 요청</b> — 포획(현장 결박)과 운반(목적 우리)은 별도 요청입니다.", 16, 30);
            var cap = sel.Where(a => CaptureBlock(a) == null).ToList();
            Confirmable("capture", "포획 요청", cap, Excluded(sel, CaptureBlock), () => { foreach (var a in cap) a.CaptureDesignated = true; });
            // 운반: 선택한 모든 종을 받는 운영 중인 우리만 목적지.
            var carry = sel.Where(a => CarryBlock(a) == null).ToList();
            var kinds = carry.Select(a => a.Species).Distinct().ToList();
            var pens = FindObjectsByType<Ranch>(UnityEngine.FindObjectsSortMode.None).Where(p => p.Operating && Ranch.InRoom(p.Room) == p && kinds.All(p.Accepts)).ToList();
            if (carry.Count > 0 && pens.Count == 0)
                MenuTheme.Text(content, $"<color=#d9534f>운반: 선택한 모든 종({string.Join("·", kinds.Select(k => SpeciesInfo.For(k).name))})을 받는 공통 목적지가 없습니다 — 선택을 나눠 요청하세요.</color>", 15, 30);
            else if (carry.Count == 0) MenuTheme.Text(content, "운반 요청 0마리" + Excluded(sel, CarryBlock), 15, 28);
            foreach (var pen in pens)
            {
                var dest = pen;
                Confirmable("carry:" + pen.Key, $"운반 요청 → {pen.Data.displayName}", carry, Excluded(sel, CarryBlock), () => { foreach (var a in carry) a.TransportTo = dest; });
            }
            var kill = sel.Where(a => a.SlaughterBlock == null).ToList();
            Confirmable("slaughter", "도축 요청", kill, Excluded(sel, a => a.SlaughterBlock), () => { foreach (var a in kill) a.RequestSlaughter(); }, true);
            var free = sel.Where(a => ReleaseBlock(a) == null).ToList();
            Confirmable("release", "방생(즉시 사라짐)", free, Excluded(sel, ReleaseBlock), () => { foreach (var a in free) { picked.Remove(a); a.Release(); } }, true);
            // 일괄 취소: 시작 전 요청만. 포획 후 취소하면 결박은 유지, 운반 중이면 그 자리에 내려놓는다. 진행 중 도축은 취소 불가.
            var cancellable = sel.Where(a => a.CaptureDesignated || a.TransportTo != null || a.SlaughterOrdered && !a.SlaughterStarted).ToList();
            var started = sel.Count(a => a.SlaughterStarted);
            MenuTheme.Button(content, $"일괄 취소 {cancellable.Count}마리(포획·운반·도축 요청){(started > 0 ? $" · 진행 중 도축 {started}마리는 취소 불가" : "")}", () => {
                foreach (var a in cancellable)
                {
                    a.CaptureDesignated = false;
                    if (a.TransportTo != null) { a.TransportTo = null; a.Carrier?.DropCarriedCritter(); }
                    a.CancelSlaughter();
                }
                pendingBulk = null; ShowRanch(r);
            }).interactable = cancellable.Count > 0;
        }

        // 개체 이름 변경(개체 화면): 비우면 종 이름. 알림은 '이름(종)'.
        private void RenameField(Critter a)
        {
            MenuTheme.Text(content, $"이름: {a.DisplayName} — 아래 칸에 입력 후 Enter(비우면 기본 이름 '{a.Info.name}', 최대 16자)", 15, 28);
            var input = MenuTheme.Input(content, a.CustomName);
            input.contentType = InputField.ContentType.Standard; input.characterLimit = 16; input.name = "CritterName";
            input.onEndEdit.AddListener(v => { a.CustomName = v; ShowCritter(a); });
        }
    }
}
