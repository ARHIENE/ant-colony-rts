using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;

namespace AntColony.UI
{
    // 목장 화면(2026-10-11 개편): 우리 관리(자동 치료·채취, 종별 자동 돌봄·목표 마릿수·받을 종), 개체 목록·일괄 요청(GameMenuRanchBulk.cs), 개체 관리(이름·도축·수술·방생), 먹이통, 도축대.
    // 자동 도축·도축 금지 설정은 폐기(2026-10-11 기획). 도축·방생은 두 번 눌러 확인한다.
    public sealed partial class GameMenuController
    {
        private object pendingRelease; // 방생 확인(두 번 누르면 실행)
        private object pendingSlaughter; // 도축 요청 확인(두 번 누르면 실행)
        private Critter critterTabTarget;
        private bool critterMedicalTab;
        public void OpenCritterTab(Critter a, bool medical)
        {
            critterTabTarget = a; critterMedicalTab = medical; ShowCritter(a);
        }

        public void ConfirmCritterAction(Critter a, bool release)
        {
            if (a == null) return;
            if (release) { if (!a.CanRelease) return; pendingRelease = a; }
            else { if (a.SlaughterBlock != null) return; pendingSlaughter = a; }
            OpenCritterTab(a, false);
        }
        // 확인창: 실제 적용 대상의 이름·마릿수(같은 이름은 묶어서).
        private static string Names(System.Collections.Generic.IList<Critter> list) => string.Join(", ", list.GroupBy(a => a.DisplayName).Select(g => $"{g.Key} {g.Count()}마리"));

        public void ShowRanch(Ranch r)
        {
            if (r == null) { Resume(); return; }
            var window = V5Window(r.Data.displayName + " / 목장", "우리 설정 · 개체 선택 · 일괄 요청");
            content = V5Section(window, "우리 현황", 20, 104, 360, 190);
            var animals = r.Animals.ToList();
            MenuTheme.Text(content, (r.Operating ? $"우리 {r.Cells}칸 · 필요 공간 {r.NeededSpace:0.#}" + (r.Crowding > 1 ? $" · <color=#d9534f>과밀 {r.Crowding:P0}: 생산·번식 효율 {r.Efficiency:P0}</color>" : "")
                : "<color=#d9534f>뚫린 우리(벽·울타리·문으로 둘러싸이지 않음)</color> — 먹기 외 생산·번식·돌봄·자동 작업 중단, 다시 막으면 재개")
                + $" · {animals.Count}마리", 17, 45);
            var risk = AntColony.World.PenHygiene.Risk(r);
            MenuTheme.Text(content, $"배설물 {AntColony.World.PenHygiene.ManureIn(r):0}개 · 위생 " + (risk < .3f ? "양호" : $"<color=#{(risk < .7f ? "f2c94c" : "d9534f")}>{AntColony.World.PenHygiene.Level(risk)}</color>") + " — 운반 장수가 창고·퇴비통으로 수거", 15, 30);
            MenuTheme.Button(content, $"자동 치료: {(r.AutoTreat ? "켬" : "끔")}", () => { r.AutoTreat = !r.AutoTreat; ShowRanch(r); });
            MenuTheme.Button(content, $"자동 채취: {(r.AutoHarvest ? "켬" : "끔")}", () => { r.AutoHarvest = !r.AutoHarvest; ShowRanch(r); });
            content = V5Section(window, "종별 설정", 20, 306, 360, 342);
            foreach (var sp in System.Enum.GetValues(typeof(Species)).Cast<Species>())
            {
                var info = SpeciesInfo.For(sp); var list = animals.Where(a => a.Species == sp).ToList(); var known = r.KnownSpecies.Contains(sp);
                MenuTheme.Button(content, $"{info.name} 받기: {(r.Accepts(sp) ? "허용" : "금지")}", () => { r.ToggleAccept(sp); ShowRanch(r); });
                if (!known && list.Count == 0) continue;
                var st = r.Setting(sp);
                MenuTheme.Text(content, $"{info.name}({SpeciesInfo.TemperName(info.temper)}{(info.tameable ? "" : " · 길들이기 불가")}) — 성체 {list.Count(a => a.Stage != CritterStage.Baby)} · 새끼 {list.Count(a => a.Stage == CritterStage.Baby)} · 길들임 {list.Count(a => a.Tame)} · 굶주림 {list.Count(a => a.Starving)} · 목표 {st.target}(현황·초과 알림)", 16, 30);
                MenuTheme.Button(content, $"  목표 {st.target}마리 (−)", () => { st.target = System.Math.Max(0, st.target - 1); ShowRanch(r); });
                MenuTheme.Button(content, $"  목표 {st.target}마리 (+)", () => { st.target++; ShowRanch(r); });
                MenuTheme.Button(content, $"  자동 돌봄: {(st.autoCare ? "켬" : "끔")}", () => { st.autoCare = !st.autoCare; ShowRanch(r); });
            }
            content = V5Section(window, "개체 목록 / 일괄 요청", 394, 104, 686, 544);
            RanchList(r); // 개체 목록·필터·정렬·일괄 요청(GameMenuRanchBulk.cs)
            MenuTheme.Button(content, "새로고침", () => ShowRanch(r));
            V5Fit(window);
        }

        // 치료 시설 상태 한 줄(WorkTargetPanel·개체 화면 공용).
        public static string ClinicStatus(Critter a)
        {
            var surgeon = a.Handler != null && a.Handler.AnimalJob == AnimalTask.Surgery ? a.Handler : null;
            if (a.SurgeryStarted) return $"{a.DisplayName} 수술 중 {a.SurgeryFraction:P0} · " + (surgeon != null ? $"집도의 {surgeon.CommanderName} · 예상 성공 {a.PredictedSuccess(surgeon):P0}" : "집도의 대기(다른 의료 장수가 이어받음, 배고픔 진행)");
            if (a.AwaitingDecision) return $"{a.DisplayName} 수술 실패 — 재수술 또는 우리 복귀 선택 대기(배고픔 진행)";
            if (a.Recovering) return $"{a.DisplayName} " + (a.HeavyInjury ? "회복 중 새 중상 — 새 수술 필요(회복 정지)" : $"회복 중 · 남은 {a.RecoveryLeft / GameCalendar.SecondsPerMonth:0.00}개월(배고픔 정지)");
            if (a.HeavyInjury) return $"{a.DisplayName} 수술 대기";
            return $"{a.DisplayName} " + (a.TransportTo != null ? $"복귀 대기 → {a.TransportTo.Data.displayName}" : "복귀할 우리 필요(개체 화면에서 지정)");
        }

        public void ShowCritter(Critter a)
        {
            if (a == null || a.Monster == null || a.Monster.IsDead) { Resume(); return; }
            if (critterTabTarget != a) { critterTabTarget = a; critterMedicalTab = false; }
            var info = a.Info;
            var window = V5Window($"{a.DisplayName} / 개체", "상태 · 관리 · 동물 의료");
            MenuLayout.Button(window, "Animal Overview", "개요", 20, 100, 120, 34, () => { critterMedicalTab = false; ShowCritter(a); }, primary: !critterMedicalTab);
            MenuLayout.Button(window, "Animal Medical", "의료", 148, 100, 120, 34, () => { critterMedicalTab = true; ShowCritter(a); }, primary: critterMedicalTab);
            if (critterMedicalTab)
            {
                content = V5Section(window, "건강 / 치료 진행", 20, 148, 520, 500);
                var status = MenuTheme.Text(content, "", 17, 90);
                void RefreshStatus()
                {
                    if (a == null || a.Monster == null || a.Monster.IsDead) { Resume(); return; }
                    status.text = $"체력 {a.HealthFraction:P0} · " + (a.HeavyInjury ? "중상" : a.Injured ? "경상" : "부상 없음")
                        + (a.Clinic != null ? "\n" + ClinicStatus(a) : a.Recovering ? "\n시설 밖 — 회복 정지" : "")
                        + (a.Medicated > 0 ? $"\n투약 효과 중 · {a.Medicated:0}초" : "")
                        + "\n" + a.DiseaseStatus
                        + (a.Sequelae > 0 ? "\n" + a.SequelaEffect : "");
                }
                RefreshStatus();
                refreshDutyScreen = RefreshStatus;
                MenuTheme.Text(content, "경상은 자연 회복하며 투약하면 빨라집니다. 투약 이후 생긴 경상에는 기존 약효가 적용되지 않습니다.\n중상은 치료대에서 수술하며, 실패 후에는 재수술 또는 우리 복귀를 선택합니다.", 15, 100);
                content = V5Section(window, "투약 / 수술", 554, 148, 526, 500);
                MenuTheme.Text(content, a.DiseaseStatus, 15, 60);
                if (a.NeedsClinicFood) MenuTheme.Text(content, "급식 대기 — 의료 장수가 허용 먹이를 가져옵니다(대기 중 배고픔 진행).", 15, 50);
                MedicalSection(a);
                MenuTheme.Button(content, "새로고침", () => ShowCritter(a));
                V5Fit(window);
                return;
            }
            content = V5Section(window, "개체 상태", 20, 148, 520, 500);
            var where = a.Clinic != null ? ClinicStatus(a)
                : a.Carrier != null ? $"운반 중({a.Carrier.CommanderName})" : a.Bound ? "결박됨" : a.Pen == null ? (a.Recovering ? "시설 밖 회복 정지 — 빈 치료대 재입원 대기" : "야생") : a.InOperatingPen ? a.Pen.Data.displayName : "뚫린 우리 / 우리 밖";
            MenuTheme.Text(content, $"{SpeciesInfo.TemperName(info.temper)} · {a.StageName} {a.Age:0.#}개월 · " + (a.Tame ? "길들임" : info.tameable ? $"야생(길들이기 {a.TameProgress:P0})" : "야생(길들이기 불가)")
                + $" · 배고픔 {a.Hunger:0} · 체력 {a.HealthFraction:P0}{(a.HeavyInjury ? " 중상" : a.Injured ? " 경상" : "")}{(a.Medicated > 0 ? " · 투약 효과 중" : "")}{(a.Sequelae > 0 ? $" · <color=#d9534f>{a.SequelaEffect}</color>" : "")}", 17, 45);
            MenuTheme.Text(content, $"{where} · 돌봄 효과 {(a.CareRemaining > 0 ? $"{a.CareRemaining:0}초" : "없음")}{(a.CareBlocked ? " · <color=#d9534f>자동 돌봄 재시도 중지</color>" : "")} · 직접 채취 {info.direct.DisplayName()} {a.DirectStock:0.#}"
                + (a.TransportTo != null ? $" · 운반 요청: {a.TransportTo.Data.displayName}" : "") + (a.Handler != null ? $" · 담당 {a.Handler.CommanderName}" : ""), 16, 40);
            RenameField(a);
            content = V5Section(window, "요청 / 담당 장수", 554, 148, 526, 500);
            if (!a.Bound && a.Carrier == null && a.Clinic == null)
                MenuTheme.Button(content, a.CaptureDesignated ? "포획 요청 취소" : a.Tame ? "포획 요청(길들인 개체는 저항 없이 바로 결박)" : "포획 요청(사육 장수가 붙잡아 현장에 결박)", () => { a.CaptureDesignated = !a.CaptureDesignated; ShowCritter(a); });
            // 운반: 해당 종을 받는 우리를 목적지로(과밀은 제한 사유가 아님). 결박·쓰러짐·길들임·치료 후 대기 개체만.
            if (a.Carriable || a.Carrier != null)
            {
                foreach (var pen in FindObjectsByType<Ranch>(UnityEngine.FindObjectsSortMode.None).Where(p => p.Operating && p.Accepts(a.Species) && Ranch.InRoom(p.Room) == p))
                    MenuTheme.Button(content, $"운반: {pen.Data.displayName} ({pen.Cells}칸, {pen.Animals.Count()}마리)", () => { a.TransportTo = pen; ShowCritter(a); });
                if (a.TransportTo != null) MenuTheme.Button(content, "운반 취소(대기 개체는 그대로, 운반 중이면 그 자리에 내려놓음)", () => { a.TransportTo = null; a.Carrier?.DropCarriedCritter(); ShowCritter(a); });
            }
            else if (a.TransportTo == null && a.Clinic == null) MenuTheme.Text(content, "운반은 결박되거나 쓰러진 개체만 — 먼저 포획 요청하세요.", 15, 30);
            if (a.InOperatingPen)
            {
                MenuTheme.Button(content, a.CareOrder ? "돌봄 1회 지시됨" : "돌봄 1회 지시", () => { a.CareOrder = true; ShowCritter(a); });
                if (a.CareBlocked) MenuTheme.Button(content, "돌봄 재허용", () => { a.CareBlocked = false; ShowCritter(a); });
            }
            SlaughterSection(a);
            // 위험한 야생 개체: 다룰 장수를 유저가 고른다(성공 보장 없음).
            if (!a.Tame && !a.Downed && a.Clinic == null)
            {
                MenuTheme.Text(content, a.ApprovedId != "" ? "<color=#f2c94c>위험 작업 장수 지정됨</color>" : "위험한 작업(포획·돌봄·채취·도축·치료)은 장수를 골라 지시합니다.", 15, 30);
                foreach (var c in SortedCommanders().Where(c => c.AllowsJob(CommanderJobs.Husbandry) || c.AllowsJob(CommanderJobs.Nursing)))
                {
                    var chance = SpeciesInfo.FailChance(a.Species, false, c); var picked = c;
                    MenuTheme.Button(content, $"{c.CommanderName} · 사육 {c.Talents.Level(CommanderActivity.Husbandry)} · 의료 {c.Talents.Level(CommanderActivity.Medicine)} · {SpeciesInfo.RiskName(SpeciesInfo.Risk(chance))} ({chance:P0} 실패)",
                        () => { a.ApprovedId = picked.PersonalState.id; ShowCritter(a); });
                }
            }
            if (a.CanRelease)
                MenuTheme.Button(content, ReferenceEquals(pendingRelease, a) ? $"<color=#d9534f>정말 {a.DisplayName} 방생?</color> (다시 누르면 즉시 사라짐)" : "방생",
                    () => { if (ReferenceEquals(pendingRelease, a)) { pendingRelease = null; a.Release(); Resume(); } else { pendingRelease = a; ShowCritter(a); } });
            else MenuTheme.Text(content, "치료 시설 안에서는 방생할 수 없습니다(우리로 복귀한 뒤 가능).", 15, 30);
            MenuTheme.Button(content, "새로고침", () => ShowCritter(a));
            V5Fit(window);
        }

        // 개별 도축 요청·취소. 실제 도축을 시작하면 취소 불가.
        private void SlaughterSection(Critter a)
        {
            if (a.SlaughterStarted) { MenuTheme.Text(content, "<color=#d9534f>도축 중 — 취소할 수 없습니다.</color>", 15, 30); return; }
            if (a.SlaughterOrdered) { MenuTheme.Button(content, "도축 요청 취소" + (a.Bound ? "(이미 결박됐으면 결박은 유지)" : ""), () => { a.CancelSlaughter(); ShowCritter(a); }); return; }
            if (a.SlaughterBlock != null) { MenuTheme.Text(content, $"도축 요청 불가: {a.SlaughterBlock}", 15, 30); return; }
            MenuTheme.Button(content, ReferenceEquals(pendingSlaughter, a) ? $"<color=#d9534f>도축 요청 확인: {a.DisplayName} — 1마리</color> (다시 누르면 요청)" : "도축 요청(사육 장수가 현장 도축, 야생은 먼저 포획)",
                () => { if (ReferenceEquals(pendingSlaughter, a)) { a.RequestSlaughter(); pendingSlaughter = null; } else pendingSlaughter = a; ShowCritter(a); });
        }

        // 치료: 경상 투약 / 중상 수술(고위험 첫 수술·재수술은 집도의 지정) / 실패 후 재수술·복귀 선택. 재고는 예약분을 뺀 사용 가능 수량.
        private void MedicalSection(Critter a)
        {
            int Avail(ResourceType t) => AnimalMedicalStock.Available(t);
            string Stock(ResourceType t, string n) => $"{n} 사용 가능 {Avail(t)}(예약 {AnimalMedicalStock.Reserved(t)})" + (Avail(t) < 1 ? " <color=#d9534f>부족</color>" : "");
            MenuTheme.Text(content, Stock(ResourceType.AnimalMedicine, "동물용 의약품") + " · " + Stock(ResourceType.SurgeryKit, "수술 키트") + (a.HeavyInjury && !a.SurgeryStarted ? " · 수술 1회에 키트 1개" : ""), 15, 50);
            var benches = FindObjectsByType<Processor>(UnityEngine.FindObjectsSortMode.None).Where(p => p.IsMedical && p.isActiveAndEnabled).ToArray();
            foreach (var bench in benches)
                MenuTheme.Button(content, "약제대 생산 목록: " + bench.Data.displayName, () => ShowProcessor(bench));
            if (benches.Length == 0) MenuTheme.Text(content, "사용 가능한 약제대 없음 — 건설 > 의료에서 건설하세요.", 15, 40);
            if (!a.Injured && !a.Sick && !a.Recovering && a.Clinic == null) MenuTheme.Text(content, "치료가 필요한 부상이 없습니다.", 17, 40);
            if ((a.Injured || a.NeedsDiseaseMedicine) && !a.SurgeryStarted && !a.AwaitingDecision)
            {
                var needed = a.HeavyInjury ? ResourceType.SurgeryKit : ResourceType.AnimalMedicine;
                MenuTheme.Button(content, a.TreatOrder ? "치료 지시됨" : a.HeavyInjury ? "수술 치료 지시" : "투약 지시", () => {
                    if (Avail(needed) < 1 || a.TreatOrder || (!a.HeavyInjury && a.Medicated > 0)) return;
                    a.TreatOrder = true; ShowCritter(a);
                }).interactable = Avail(needed) > 0 && !a.TreatOrder && (a.HeavyInjury || a.Medicated <= 0);
            }
            void Surgeons(string verb, System.Action<CommanderAnt> pick)
            {
                foreach (var c in Critter.Surgeons.OrderByDescending(a.PredictedSuccess))
                {
                    var picked = c;
                    MenuTheme.Button(content, $"{verb}: {c.CommanderName} · 의료 {c.Talents.Level(CommanderActivity.Medicine)} · 예상 성공 {a.PredictedSuccess(c):P0} · 사망 위험 {a.PredictedDeath(c):P0}" + (a.SurgeonId == c.PersonalState.id ? " (지정됨)" : ""), () => { if (Avail(ResourceType.SurgeryKit) < 1) return; pick(picked); ShowCritter(a); }).interactable = Avail(ResourceType.SurgeryKit) > 0;
                }
            }
            if (a.AwaitingDecision)
            {
                MenuTheme.Text(content, $"<color=#d9534f>수술 실패</color> — 재수술(새 키트 필요, 현재 부상·의료 능력으로 위험 계산) 또는 원래 우리 복귀를 고르세요.", 15, 30);
                Surgeons("재수술", c => a.OrderResurgery(c));
                MenuTheme.Button(content, "우리 복귀(부상 유지)", () => { a.ChooseReturn(); ShowCritter(a); });
                return;
            }
            if (!a.HeavyInjury || a.SurgeryStarted) return;
            if (a.SurgeryNeedsManual)
            {
                MenuTheme.Text(content, (a.Sequelae > 0 ? "재수술" : "고위험 수술") + $"(가장 유능한 의료 장수 기준 예상 성공 {a.BestPredictedSuccess:P0}) — 집도의를 지정해야 시작합니다.", 15, 30);
                Surgeons("집도의 지정", c => { a.SurgeonId = c.PersonalState.id; a.TreatOrder = true; });
            }
            else MenuTheme.Text(content, $"예상 성공 {a.BestPredictedSuccess:P0}(가장 유능한 의료 장수 기준) — 자동 치료 또는 치료 지시로 시작", 15, 30);
        }

        public void ShowButcher(RanchFacility f)
        {
            if (f == null || !f.IsButcher) { Resume(); return; }
            var window = V5Window(f.Data.displayName + " / 도축대", "허용 종 · 등록 순서대로 해체");
            content = V5Section(window, "운영 설정", 20, 104, 520, 544);
            MenuTheme.Text(content, f.Paused ? "<color=#f2c94c>일시정지</color> — 사체·진행도 보존. 새 운반·해체는 시작하지 않고, 이미 운반 중인 사체는 받습니다."
                : "운반 장수가 허용한 종의 사체를 옮기고 요리 작업 장수가 등록 순서대로 해체합니다. 요리 능력은 속도에만 영향, 생산물은 도축대 옆 바닥에 생깁니다.", 16, 50);
            MenuTheme.Button(content, f.Paused ? "재개(진행도 이어서)" : "일시정지", () => { f.Paused = !f.Paused; ShowButcher(f); });
            foreach (var sp in System.Enum.GetValues(typeof(Species)).Cast<Species>())
            {
                var s = sp;
                MenuTheme.Button(content, $"{SpeciesInfo.For(sp).name} 사체: {(f.AllowsSpecies(sp) ? "허용" : "금지")}" + (f.AllowsSpecies(sp) ? " (금지하면 대기 중인 이 종 사체는 옆 바닥에 내려놓음)" : ""), () => { f.ToggleSpecies(s); ShowButcher(f); });
            }
            content = V5Section(window, "사체 대기열", 554, 104, 526, 544);
            MenuTheme.Text(content, $"대기열 {f.Carcasses}구", 17, 30);
            for (var i = 0; i < f.Queue.Count; i++)
            {
                var e = f.Queue[i]; var name = System.Enum.IsDefined(typeof(Species), e.species) ? SpeciesInfo.For((Species)e.species).name : "종 미상(옛 저장)";
                MenuTheme.Text(content, $"{i + 1}. {name} · 고기 {e.meat:0} · 갑각 {e.chitin}" + (i == 0 && f.ButcherProgress > 0 ? $" · 해체 {f.ButcherProgress / GameBalance.ButcherSeconds:P0}" : ""), 15, 26);
            }
            MenuTheme.Button(content, "새로고침", () => ShowButcher(f));
            V5Fit(window);
        }

        public void ShowFeeder(RanchFacility f)
        {
            if (f == null || !f.IsFeeder) { Resume(); return; }
            var window = V5Window(f.Data.displayName + " / 먹이통", "허용 먹이 · 목표 재고 · 반출");
            content = V5Section(window, "운영 설정", 20, 104, 360, 544);
            MenuTheme.Text(content, "운반 장수가 허용한 먹이를 목표량까지 채웁니다(높은 우선순위부터). 허용을 끄거나 목표를 낮추면 초과분은 바로 바닥에 떨어집니다. 접근할 수 있는 생물이 함께 먹습니다.", 16, 50);
            MenuTheme.Button(content, $"운반 우선순위 {f.Priority} (−)", () => { f.Priority--; ShowFeeder(f); });
            MenuTheme.Button(content, $"운반 우선순위 {f.Priority} (+)", () => { f.Priority++; ShowFeeder(f); });
            content = V5Section(window, "먹이 재고", 394, 104, 686, 544);
            foreach (var t in f.FeedTypes)
            {
                var type = t;
                MenuTheme.Button(content, $"{t.DisplayName()}: {(f.Allowed(t) ? "허용" : "금지")}{(SpeciesInfo.Precious(t) ? " (귀한 먹이)" : "")} · 재고 {f.Stock(t):0} / 목표 {f.Target(t)} · 운반 중 {f.Incoming(t):0}", () => { f.ToggleAllowed(type); ShowFeeder(f); });
                MenuTheme.Button(content, $"  {t.DisplayName()} 목표 −5", () => { f.SetTarget(type, f.Target(type) - 5); ShowFeeder(f); });
                MenuTheme.Button(content, $"  {t.DisplayName()} 목표 +5", () => { f.SetTarget(type, f.Target(type) + 5); ShowFeeder(f); });
                if (f.Stock(t) >= 1) MenuTheme.Button(content, $"  {t.DisplayName()} 직접 반출(바닥에)", () => { f.DumpAll(type); ShowFeeder(f); });
            }
            V5Fit(window);
        }
    }
}
