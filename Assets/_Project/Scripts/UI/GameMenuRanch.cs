using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;

namespace AntColony.UI
{
    // 목장 화면(2026-10-11 개편): 우리 관리(자동 치료·채취, 종별 자동 돌봄·목표 마릿수·자동 도축·받을 종), 개체 관리, 먹이통 설정.
    public sealed partial class GameMenuController
    {
        private object pendingRelease; // 방생 확인(두 번 누르면 실행)

        public void ShowRanch(Ranch r)
        {
            if (r == null) { Resume(); return; }
            Screen(r.Data.displayName + " / 목장");
            var animals = r.Animals.ToList();
            MenuTheme.Text(content, (r.Operating ? $"우리 {r.Cells}칸 · 필요 공간 {r.NeededSpace:0.#}" + (r.Crowding > 1 ? $" · <color=#d9534f>과밀 {r.Crowding:P0}: 생산·번식 효율 {r.Efficiency:P0}</color>" : "")
                : "<color=#d9534f>뚫린 우리(벽·울타리·문으로 둘러싸이지 않음)</color> — 먹기 외 생산·번식·돌봄·자동 작업 중단, 다시 막으면 재개")
                + $" · {animals.Count}마리", 17, 45);
            MenuTheme.Button(content, $"자동 치료: {(r.AutoTreat ? "켬" : "끔")}", () => { r.AutoTreat = !r.AutoTreat; ShowRanch(r); });
            MenuTheme.Button(content, $"자동 채취: {(r.AutoHarvest ? "켬" : "끔")}", () => { r.AutoHarvest = !r.AutoHarvest; ShowRanch(r); });
            foreach (var sp in System.Enum.GetValues(typeof(Species)).Cast<Species>())
            {
                var info = SpeciesInfo.For(sp); var list = animals.Where(a => a.Species == sp).ToList(); var known = r.KnownSpecies.Contains(sp);
                MenuTheme.Button(content, $"{info.name} 받기: {(r.Accepts(sp) ? "허용" : "금지")}", () => { r.ToggleAccept(sp); ShowRanch(r); });
                if (!known && list.Count == 0) continue;
                var st = r.Setting(sp);
                MenuTheme.Text(content, $"{info.name}({SpeciesInfo.TemperName(info.temper)}{(info.tameable ? "" : " · 길들이기 불가")}) — 성체 {list.Count(a => a.Stage != CritterStage.Baby)} · 새끼 {list.Count(a => a.Stage == CritterStage.Baby)} · 길들임 {list.Count(a => a.Tame)} · 굶주림 {list.Count(a => a.Starving)}", 16, 30);
                MenuTheme.Button(content, $"  목표 {st.target}마리 (−)", () => { st.target = System.Math.Max(0, st.target - 1); ShowRanch(r); });
                MenuTheme.Button(content, $"  목표 {st.target}마리 (+)", () => { st.target++; ShowRanch(r); });
                MenuTheme.Button(content, $"  자동 돌봄: {(st.autoCare ? "켬" : "끔")}", () => { st.autoCare = !st.autoCare; ShowRanch(r); });
                MenuTheme.Button(content, $"  목표 초과분 자동 도축: {(st.autoSlaughter ? "켬" : "끔")}", () => { st.autoSlaughter = !st.autoSlaughter; ShowRanch(r); });
                if (list.Count > 0)
                    MenuTheme.Button(content, Equals(pendingRelease, (r, sp)) ? $"  <color=#d9534f>정말 {list.Count}마리 방생?</color> (다시 누르면 즉시 사라짐)" : $"  {info.name} 전부 방생({list.Count}마리)",
                        () => { if (Equals(pendingRelease, (r, sp))) { foreach (var a in list) a.Release(); pendingRelease = null; } else pendingRelease = (r, sp); ShowRanch(r); });
            }
            MenuTheme.Button(content, "새로고침", () => ShowRanch(r));
            MenuTheme.Button(content, "게임 재개", Resume);
        }

        public void ShowCritter(Critter a)
        {
            if (a == null || a.Monster == null || a.Monster.IsDead) { Resume(); return; }
            var info = a.Info;
            Screen($"{info.name} / 개체");
            var where = a.Clinic != null ? (a.SurgeryStarted ? $"수술 중 {a.SurgeryProgress:0.#}/{GameBalance.CritterSurgerySeconds}" : a.TransportTo == null ? "치료대 — 복귀할 우리 필요" : "치료대 — 복귀 대기")
                : a.Carrier != null ? $"운반 중({a.Carrier.CommanderName})" : a.Bound ? "결박됨" : a.Pen == null ? "야생" : a.InOperatingPen ? a.Pen.Data.displayName : "뚫린 우리 / 우리 밖";
            MenuTheme.Text(content, $"{SpeciesInfo.TemperName(info.temper)} · {a.StageName} {a.Age:0.#}개월 · " + (a.Tame ? "길들임" : info.tameable ? $"야생(길들이기 {a.TameProgress:P0})" : "야생(길들이기 불가)")
                + $" · 배고픔 {a.Hunger:0} · 체력 {a.HealthFraction:P0}{(a.HeavyInjury ? " 중상" : a.Injured ? " 경상" : "")}{(a.Medicated > 0 ? " · 투약 중" : "")}", 17, 45);
            MenuTheme.Text(content, $"{where} · 돌봄 효과 {(a.CareRemaining > 0 ? $"{a.CareRemaining:0}초" : "없음")}{(a.CareBlocked ? " · <color=#d9534f>자동 돌봄 재시도 중지</color>" : "")} · 직접 채취 {info.direct.DisplayName()} {a.DirectStock:0.#}"
                + (a.TransportTo != null ? $" · 운반 요청: {a.TransportTo.Data.displayName}" : ""), 16, 40);
            if (!a.Tame && !a.Bound && a.Carrier == null && a.Clinic == null)
                MenuTheme.Button(content, a.CaptureDesignated ? "포획 지정 취소" : "포획 지정(사육 장수가 붙잡아 현장에 결박)", () => { a.CaptureDesignated = !a.CaptureDesignated; ShowCritter(a); });
            // 운반: 해당 종을 받는 우리를 목적지로(과밀은 제한 사유가 아님). 결박·쓰러짐·길들임·치료 후 대기 개체만.
            if (a.Carriable || a.Carrier != null)
            {
                foreach (var pen in FindObjectsByType<Ranch>(UnityEngine.FindObjectsSortMode.None).Where(p => p.Operating && p.Accepts(a.Species) && Ranch.InRoom(p.Room) == p))
                    MenuTheme.Button(content, $"운반: {pen.Data.displayName} ({pen.Cells}칸, {pen.Animals.Count()}마리)", () => { a.TransportTo = pen; ShowCritter(a); });
                if (a.TransportTo != null) MenuTheme.Button(content, "운반 취소(대기 개체는 그대로, 운반 중이면 그 자리에 내려놓음)", () => { a.TransportTo = null; a.Carrier?.DropCarriedCritter(); ShowCritter(a); });
            }
            else if (a.TransportTo == null) MenuTheme.Text(content, "운반하려면 먼저 포획해 결박하세요(길들인·쓰러진 개체는 바로 운반).", 15, 30);
            if (a.InOperatingPen)
            {
                MenuTheme.Button(content, a.CareOrder ? "돌봄 1회 지시됨" : "돌봄 1회 지시", () => { a.CareOrder = true; ShowCritter(a); });
                if (a.CareBlocked) MenuTheme.Button(content, "돌봄 재허용", () => { a.CareBlocked = false; ShowCritter(a); });
            }
            if (a.Injured) MenuTheme.Button(content, a.TreatOrder ? "치료 지시됨" : "치료 지시(경상 투약 / 중상 수술)", () => { a.TreatOrder = true; ShowCritter(a); });
            MenuTheme.Button(content, $"도축 금지: {(a.ProtectSlaughter ? "켬" : "끔")}", () => { a.ProtectSlaughter = !a.ProtectSlaughter; ShowCritter(a); });
            // 위험한 야생 개체: 다룰 장수를 유저가 고른다(성공 보장 없음).
            if (!a.Tame && !a.Downed)
            {
                MenuTheme.Text(content, a.ApprovedId != "" ? "<color=#f2c94c>위험 작업 장수 지정됨</color>" : "위험한 작업(포획·돌봄·채취·도축·치료)은 장수를 골라 지시합니다.", 15, 30);
                foreach (var c in SortedCommanders().Where(c => c.AllowsJob(CommanderJobs.Husbandry) || c.AllowsJob(CommanderJobs.Nursing)))
                {
                    var chance = SpeciesInfo.FailChance(a.Species, false, c); var picked = c;
                    MenuTheme.Button(content, $"{c.CommanderName} · 사육 {c.Talents.Level(CommanderActivity.Husbandry)} · 의료 {c.Talents.Level(CommanderActivity.Medicine)} · {SpeciesInfo.RiskName(SpeciesInfo.Risk(chance))} ({chance:P0} 실패)",
                        () => { a.ApprovedId = picked.PersonalState.id; ShowCritter(a); });
                }
            }
            MenuTheme.Button(content, ReferenceEquals(pendingRelease, a) ? "<color=#d9534f>정말 방생?</color> (다시 누르면 즉시 사라짐)" : "방생",
                () => { if (ReferenceEquals(pendingRelease, a)) { pendingRelease = null; a.Release(); Resume(); } else { pendingRelease = a; ShowCritter(a); } });
            MenuTheme.Button(content, "새로고침", () => ShowCritter(a));
            MenuTheme.Button(content, "게임 재개", Resume);
        }

        public void ShowFeeder(RanchFacility f)
        {
            if (f == null || !f.IsFeeder) { Resume(); return; }
            Screen(f.Data.displayName + " / 먹이통");
            MenuTheme.Text(content, "운반 장수가 허용한 먹이를 목표량까지 채웁니다(높은 우선순위부터). 허용을 끄거나 목표를 낮추면 초과분은 바로 바닥에 떨어집니다. 접근할 수 있는 생물이 함께 먹습니다.", 16, 50);
            MenuTheme.Button(content, $"운반 우선순위 {f.Priority} (−)", () => { f.Priority--; ShowFeeder(f); });
            MenuTheme.Button(content, $"운반 우선순위 {f.Priority} (+)", () => { f.Priority++; ShowFeeder(f); });
            foreach (var t in f.FeedTypes)
            {
                var type = t;
                MenuTheme.Button(content, $"{t.DisplayName()}: {(f.Allowed(t) ? "허용" : "금지")}{(SpeciesInfo.Precious(t) ? " (귀한 먹이)" : "")} · 재고 {f.Stock(t):0} / 목표 {f.Target(t)} · 운반 중 {f.Incoming(t):0}", () => { f.ToggleAllowed(type); ShowFeeder(f); });
                MenuTheme.Button(content, $"  {t.DisplayName()} 목표 −5", () => { f.SetTarget(type, f.Target(type) - 5); ShowFeeder(f); });
                MenuTheme.Button(content, $"  {t.DisplayName()} 목표 +5", () => { f.SetTarget(type, f.Target(type) + 5); ShowFeeder(f); });
                if (f.Stock(t) >= 1) MenuTheme.Button(content, $"  {t.DisplayName()} 직접 반출(바닥에)", () => { f.DumpAll(type); ShowFeeder(f); });
            }
            MenuTheme.Button(content, "게임 재개", Resume);
        }
    }
}
