using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

namespace AntColony.Units
{
    // 장수 의료 작업(2026-10-15): 처치·투약(의료 작업), 환자 급식(의료), 구조 운반(운반), 수술(의료), 병상 휴식(환자 쪽).
    // 우선순위: 생명 위험 → 중증 → 경증 → 대상 우선순위 → 거리 → 오래 기다린 요청. 진행 중인 처치·수술은 끝내고 이동한다.
    public enum MedJob { None, Tend, Feed, Rescue, Surgery }
    public partial class CommanderAnt
    {
        public const float FeedSeconds = 3f, SurgeryWork = 20f;
        private MedJob medJob; private CommanderAnt medPatient; private float medWork;
        private PrisonerCamp medCamp; private Prisoner medPrisoner; // 포로 처치·급식(2026-10-15)
        public MedJob MedicalJob => medJob;
        public CommanderAnt MedicalPatient => medPatient;
        public CommanderAnt RescueCarrier { get; private set; }
        public HealthState Health => personalState.health;
        public bool IsMedicalPatient => personalState.treating || PersonalHealth <= 0;

        // 환자 쪽 상태: 치명·쓰러짐·알림.
        private void TickHealthState(float seconds)
        {
            var h = personalState.health;
            if (h.Fatal && !IsDead)
            {
                var cause = h.bloodLoss >= 1 ? "출혈" : h.illnesses.FirstOrDefault(i => i.severity >= 1) is Illness ill ? MedicalRules.IllnessName(ill.kind) : "치명상";
                AntColony.UI.ToastManager.SetCrisis("med:" + personalState.id, null);
                OnDowned(cause, true); return;
            }
            if (h.Downed && PersonalHealth > 0) { CommandStop(); DropCargo(); WorkState.health = 0; WorkState.unconsciousSeconds = 0; WorkState.recoverySeconds = GameCalendar.SecondsPerDay; }
            // 병상 퇴원: 처치가 끝나고 중상이 아니며 대기 중인 수술이 없을 때.
            if (TreatmentFacility != null && !h.Downed && !h.Serious && !h.NeedsTend && !h.surgeries.Any(s => s.Active)) { TreatmentFacility.Release(this); MoodEvent("치료 완료", 3); }
            if (!IsColonyMember || IsHostile) return;
            if ((careAlertTimer -= seconds) <= 0)
            {
                careAlertTimer = 1f; var wait = CareWaitReason(this);
                AntColony.UI.ToastManager.SetPinned("medwait:" + personalState.id, wait == "" || wait.StartsWith("치료 안 함") ? null : $"{commanderName}: {wait}",
                    () => AntColony.UI.GameMenuController.Instance?.ShowHealth(this), "건강 화면");
            }
            string alert = h.Downed ? $"{commanderName} 쓰러짐" : null;
            if (h.LifeThreatening) alert = $"{commanderName} 위독 — " + (h.Bleeding ? $"출혈 예상 사망 {h.SecondsToBleedOut:0}초" : "질병 위독");
            AntColony.UI.ToastManager.SetCrisis("med:" + personalState.id, alert, () => AntColony.UI.GameMenuController.Instance?.ShowHealth(this), "건강 화면");
        }

        private float careAlertTimer;
        // 치료가 진행되지 못하는 이유(건강 화면·오른쪽 알림 공용). 빈 문자열 = 문제 없음.
        public static string CareWaitReason(CommanderAnt p)
        {
            var h = p.Health;
            if (h.medicine == MedicineLevel.NoCare) return "치료 안 함 설정";
            if (p.PersonalHealth <= 0 && p.TreatmentFacility == null && p.RescueCarrier == null && FreeBed(p.Position) == null) return "구조할 자리 없음(빈 의료 침상 필요)";
            if (!h.NeedsTend) return "";
            if (!HomePatients().Any(d => d != p && d.CivilianWorkReady && d.AllowsJob(CommanderJobs.Nursing)) && !p.CanSelfTend) return "치료할 장수 없음";
            var meds = MedicalRules.BestMedicine(h.medicine, FreeMedicine) >= 0;
            if (h.illnesses.Any(i => i.NeedsTend) && !meds && !h.wounds.Any(w => w.NeedsTend))
                return h.medicine == MedicineLevel.NoMedicine ? "약 없이 설정 — 질병 투약 불가" : "약품 부족(투약 불가)";
            return "";
        }
        // ---- 의료 장수 쪽 작업 후보 ----
        // 수술 예약분을 뺀 사용 가능 약품(처치·투약이 수술용 약을 쓰지 않게).
        private static int FreeMedicine(ResourceType t) => (ResourceManager.Instance?.GetAmount(t) ?? 0) - Reserved(t, null);
        private static int Urgency(HealthState h) => h.LifeThreatening ? 3 : h.Serious ? 2 : 1;
        private bool MedicineAllowed(HealthState h) => MedicalRules.BestMedicine(h.medicine, FreeMedicine) >= 0;
        private bool Taken(CommanderAnt patient, MedJob job) => Active.OfType<CommanderAnt>().Any(c => c != this && c.medJob == job && c.medPatient == patient);
        private static IEnumerable<CommanderAnt> HomePatients() => (CommanderRoster.Instance != null ? CommanderRoster.Instance.Commanders : Enumerable.Empty<CommanderAnt>())
            .Where(p => p != null && p.isActiveAndEnabled && !p.IsDead && p.IsColonyMember && !p.IsAwayFromHome && !p.IsEmbarked && !p.IsCaptive);

        private IEnumerable<(Component target, Func<bool> start)> CommanderNursingTasks()
        {
            var list = new List<(int rank, Component target, Func<bool> start)>();
            foreach (var p in HomePatients())
            {
                var h = p.Health; var patient = p;
                if (MedicalRules.NeedsDoctor(h, MedicineAllowed(h)) && !Taken(p, MedJob.Tend) && (p != this || CanSelfTend))
                    list.Add((Urgency(h), p, () => StartMed(MedJob.Tend, patient)));
                if (p.IsMedicalPatient && p.Satiety < GameBalance.EatBelowSatiety && p != this && !Taken(p, MedJob.Feed) && HasFood())
                    list.Add((Urgency(h), p, () => StartMed(MedJob.Feed, patient)));
                var op = h.surgeries.FirstOrDefault(s => s.Active);
                if (op != null && p != this && SurgeryBlock(p, op, this) == "" && !Taken(p, MedJob.Surgery))
                    list.Add((Urgency(h), p, () => StartMed(MedJob.Surgery, patient)));
            }
            foreach (var camp in AntColony.Core.SceneObjects.All<PrisonerCamp>().Where(c => c.isActiveAndEnabled))
                foreach (var pr in camp.Prisoners)
                {
                    var h = pr.PersonalState.health; if (h == null) continue;
                    var (cp, pp) = (camp, pr);
                    if (MedicalRules.NeedsDoctor(h, MedicalRules.BestMedicine(h.medicine, FreeMedicine) >= 0)
                        && !Active.OfType<CommanderAnt>().Any(c => c != this && c.medPrisoner == pr))
                        list.Add((Urgency(h), camp, () => StartCampMed(MedJob.Tend, cp, pp)));
                }
            if (list.Count == 0) yield break;
            var top = list.Max(x => x.rank);
            foreach (var x in list.Where(x => x.rank == top)) yield return (x.target, x.start);
        }
        public bool HasLifeThreateningPatient => HomePatients().Any(p => p.Health.LifeThreatening);
        private static bool HasFood() => ResourceManager.Instance != null && new[] { ResourceType.FineMeal, ResourceType.SimpleMeal, ResourceType.Food, ResourceType.RottenFood }.Any(t => ResourceManager.Instance.GetAmount(t) > 0);

        // 구조(운반 작업): 쓰러진 장수를 빈 의료 침상으로. 이동 중 사망 위험이 큰 미처치 출혈은 의료 장수가 현장에서 먼저 처치한다.
        private IEnumerable<(Component target, Func<bool> start)> RescueTasks()
        {
            var doctors = HomePatients().Any(d => d.AllowsJob(CommanderJobs.Nursing) && d.CivilianWorkReady && !d.IsMedicalPatient);
            foreach (var p in HomePatients())
            {
                if (p == this || PersonalHealth <= 0 || p.PersonalHealth > 0 || p.TreatmentFacility != null || p.RescueCarrier != null || Taken(p, MedJob.Rescue)) continue;
                if (doctors && p.Health.LifeThreatening && p.Health.wounds.Any(w => w.NeedsTend && MedicalRules.BleedFactor(w) > 0)) continue;
                if (FreeBed(p.Position) == null) continue;
                var patient = p;
                yield return (p, () => StartMed(MedJob.Rescue, patient));
            }
        }
        // 포로 급식(운반 작업): 배고픈 포로에게 창고 식량을 가져다 준다.
        private IEnumerable<(Component target, Func<bool> start)> PrisonerFeedTasks()
        {
            if (!HasFood()) yield break;
            foreach (var camp in AntColony.Core.SceneObjects.All<PrisonerCamp>().Where(c => c.isActiveAndEnabled))
                foreach (var pr in camp.Prisoners.Where(p => p.PersonalState.meal.satiety < GameBalance.EatBelowSatiety))
                {
                    if (Active.OfType<CommanderAnt>().Any(c => c != this && c.medPrisoner == pr)) continue;
                    var (cp, pp) = (camp, pr);
                    yield return (camp, () => StartCampMed(MedJob.Feed, cp, pp));
                }
        }
        private bool StartCampMed(MedJob job, PrisonerCamp camp, Prisoner p)
        {
            if (camp == null || p == null || !CivilianWorkReady || IsCarrying || !TryWorkApproach(camp.transform.position, out var approach)) return false;
            CommandStop(); medJob = job; medCamp = camp; medPrisoner = p; medWork = 0; SetMoveDestination(approach); return true;
        }
        private bool TickCampMed(float seconds)
        {
            if (medCamp == null || !medCamp.isActiveAndEnabled || !medCamp.Prisoners.Contains(medPrisoner)) { EndMed(); return false; }
            if ((medCamp.transform.position - Position).sqrMagnitude > 16) return true;
            StopMoving(); medWork += seconds * WorkRate(medJob == MedJob.Tend ? CommanderActivity.Medicine : CommanderActivity.Strength);
            var h = medPrisoner.PersonalState.health; var rm = ResourceManager.Instance;
            if (medJob == MedJob.Tend)
            {
                GainExperience(CommanderActivity.Medicine, seconds);
                if (medWork < MedicalRules.TendSeconds) return true;
                var tier = MedicalRules.BestMedicine(h.medicine, FreeMedicine);
                var used = tier >= 0 && rm.TrySpend(MedicalRules.Medicines[tier], 1, ResourceReason.Treatment);
                MedicalRules.ApplyTend(h, MedicalRules.TendQuality(talents.Level(CommanderActivity.Medicine), used ? tier : -1, false, Health.Manipulation, false), used);
            }
            else
            {
                if (medWork < FeedSeconds) return true;
                if (rm != null && rm.TrySpend(Mathf.CeilToInt(GameBalance.MealFood), 0, reason: ResourceReason.Upkeep)) medPrisoner.PersonalState.meal.satiety = 100;
            }
            EndMed(); return true;
        }
        internal static Infirmary FreeBed(Vector3 near) => AntColony.Core.SceneObjects.All<Infirmary>()
            .Where(i => i.isActiveAndEnabled && !i.IsDead && i.HasFreeBed).OrderBy(i => (i.Position - near).sqrMagnitude).FirstOrDefault();

        private bool StartMed(MedJob job, CommanderAnt patient)
        {
            if (patient == null || (patient == this ? job != MedJob.Tend || !CanSelfTend : !CivilianWorkReady) || IsCarrying) return false;
            if (patient != this && !TryWorkApproach(patient.Position, out var approach)) return false;
            CommandStop(); medJob = job; medPatient = patient; medWork = 0;
            if (patient != this && TryWorkApproach(patient.Position, out approach)) SetMoveDestination(approach);
            return true;
        }
        internal void EndMed()
        {
            if (medJob == MedJob.Rescue && medPatient != null && medPatient.RescueCarrier == this) medPatient.RescueCarrier = null;
            medJob = MedJob.None; medPatient = null; medWork = 0; medCamp = null; medPrisoner = null;
        }

        // TickDuty에서 다른 작업보다 먼저 부른다. true면 이번 틱은 의료 작업.
        private bool TickMed(float seconds)
        {
            if (medJob == MedJob.None) return false;
            if (medCamp != null) return TickCampMed(seconds);
            var p = medPatient;
            if (p == null || p.IsDead || (p == this ? !CanSelfTend : !CivilianWorkReady) || p.IsAwayFromHome) { if (p != null && p.RescueCarrier == this) p.RescueCarrier = null; EndMed(); return false; }
            if (medJob == MedJob.Rescue) return TickRescue(p);
            if (p != this && (p.Position - Position).sqrMagnitude > 6.25f)
            {
                if (!IsFlying && !Agent.pathPending && !Agent.hasPath && TryWorkApproach(p.Position, out var approach)) SetMoveDestination(approach);
                return true;
            }
            StopMoving();
            var rate = WorkRate(CommanderActivity.Medicine);
            medWork += seconds * rate; GainExperience(CommanderActivity.Medicine, seconds);
            switch (medJob)
            {
                case MedJob.Tend:
                    if (medWork < MedicalRules.TendSeconds * (p == this ? 2 : 1)) return true;
                    var h = p.Health; var rm = ResourceManager.Instance;
                    var tier = MedicalRules.BestMedicine(h.medicine, FreeMedicine);
                    var quality = MedicalRules.TendQuality(talents.Level(CommanderActivity.Medicine), tier, p.TreatmentFacility != null, Health.Manipulation, p == this);
                    // ponytail: 약품은 처치 완료 순간 창고에서 차감(실물 운반 없음). 요리 재료처럼 배송으로 바꾸려면 IMaterialSink를 쓴다.
                    var used = tier >= 0 && h.illnesses.Any(i => i.NeedsTend) && rm.TrySpend(MedicalRules.Medicines[tier], 1, ResourceReason.Treatment);
                    if (!used && tier >= 0 && h.wounds.Any(w => w.NeedsTend) && rm.TrySpend(MedicalRules.Medicines[tier], 1, ResourceReason.Treatment)) used = true;
                    if (!used) quality = MedicalRules.TendQuality(talents.Level(CommanderActivity.Medicine), -1, p.TreatmentFacility != null, Health.Manipulation, p == this);
                    MedicalRules.ApplyTend(h, quality, used);
                    EndMed(); return true;
                case MedJob.Feed:
                    if (medWork < FeedSeconds) return true;
                    p.FeedInBed(); EndMed(); return true;
                case MedJob.Surgery:
                    return TickSurgery(p, seconds, rate);
            }
            EndMed(); return false;
        }

        // 스스로 먹지 못하는 환자에게 먹인다(식단·썩은 식품 규칙은 그대로).
        internal void FeedInBed()
        {
            StartMeal(null);
            if (MealState.eatSeconds > 0) { MealState.eatSeconds = 0; FinishMeal(); }
        }

        private bool TickRescue(CommanderAnt p)
        {
            var bed = p.TreatmentFacility;
            if (p.RescueCarrier == null)
            {
                if (p.PersonalHealth > 0 || bed != null) { EndMed(); return false; } // 일어났거나 이미 병상
                if ((p.Position - Position).sqrMagnitude > 6.25f)
                {
                    if (!Agent.pathPending && !Agent.hasPath && TryWorkApproach(p.Position, out var a)) SetMoveDestination(a);
                    return true;
                }
                var target = FreeBed(Position);
                if (target == null) { EndMed(); return false; }
                p.RescueCarrier = this; rescueBed = target;
                if (TryWorkApproach(target.Position, out var b)) SetMoveDestination(b); else { p.RescueCarrier = null; EndMed(); return false; }
                return true;
            }
            // 운반 중: 환자는 장수 위치를 따라온다(출혈·질병은 계속 진행).
            p.Agent.Warp(Position);
            if (rescueBed == null || !rescueBed.isActiveAndEnabled || !rescueBed.HasFreeBed)
            {
                rescueBed = FreeBed(Position);
                if (rescueBed == null) { p.RescueCarrier = null; EndMed(); return false; } // 자리가 없어지면 내려놓는다
                if (TryWorkApproach(rescueBed.Position, out var b)) SetMoveDestination(b);
            }
            if ((rescueBed.Position - Position).sqrMagnitude > 16) return true;
            p.RescueCarrier = null; rescueBed.Admit(p); EndMed();
            return true;
        }
        private Infirmary rescueBed;

        // ---- 병상 휴식(환자 쪽, TickDuty) ----
        // 병상 휴식 우선순위가 켜진 작업 중 가장 높은 작업 우선순위 이상이면 일 대신 병상으로 간다. 수술 대기는 항상 병상.
        private bool TickBedRest()
        {
            var h = Health;
            if (TreatmentFacility != null || !CanReceiveOrders || IsAwayFromHome || IsDeployed || WorkPriorities.Red) return false;
            var wantsSurgery = h.surgeries.Any(s => s.Active);
            var topJob = AutoJobs.Where(AllowsJob).Select(j => WorkState.Priority(j)).DefaultIfEmpty(0).Max();
            if (!wantsSurgery && !(h.Serious && h.bedRest > 0 && h.bedRest >= topJob)) return false;
            return SendToTreatment();
        }

        // ---- 수술 ----
        public static (ResourceType type, int amount)[] SurgeryCost(SurgeryKind k) => k == SurgeryKind.Regenerate
            ? new[] { (ResourceType.MedicineNormal, 1), (ResourceType.Special, GameBalance.RegenerationSpecial) }
            : new[] { (ResourceType.MedicineNormal, 1) };
        public static string SurgeryName(SurgeryKind k) => k == SurgeryKind.Amputate ? "절단" : "부위 재생";
        // 다른 활성 수술이 예약한 양을 뺀 실제 사용 가능 재고(예약과 보유 총량을 혼동하지 않는다).
        public static int Reserved(ResourceType t, SurgeryOrder except) => HomePatients().SelectMany(p => p.Health.surgeries)
            .Where(s => s != except && s.stage == SurgeryStage.Waiting).SelectMany(s => SurgeryCost(s.kind)).Where(c => c.type == t).Sum(c => c.amount);
        public static string RequestBlock(CommanderAnt p, SurgeryKind k, BodyPart part)
        {
            var h = p.Health;
            if (h.surgeries.Any(s => s.Active)) return "이미 대기 중인 수술이 있습니다";
            if (k == SurgeryKind.Amputate && (MedicalRules.Vital(part) || h.Missing(part))) return "절단할 수 없는 부위입니다";
            if (k == SurgeryKind.Regenerate && !ScienceEffects.Has(ScienceTechnology.Regeneration)) return "부위 재생 연구가 필요합니다";
            if (k == SurgeryKind.Regenerate && !h.wounds.Any(w => w.part == part && w.Permanent)) return "흉터·상실이 없는 부위입니다";
            return "";
        }
        public static bool RequestSurgery(CommanderAnt p, SurgeryKind k, BodyPart part)
        {
            if (p == null || RequestBlock(p, k, part) != "") return false;
            p.Health.surgeries.Add(new SurgeryOrder { kind = k, part = part }); return true;
        }
        // 시작 전 취소만 가능(예약 해제). 시작 후에는 취소 불가.
        public static bool CancelSurgery(CommanderAnt p, SurgeryOrder s)
        {
            if (s == null || s.stage != SurgeryStage.Waiting) return false;
            p.Health.surgeries.Remove(s); return true;
        }
        // 수술 대기 사유(빈 문자열 = 이 집도의로 진행 가능). surgeon이 null이면 집도의 조건은 보지 않는다.
        public static string SurgeryBlock(CommanderAnt p, SurgeryOrder s, CommanderAnt surgeon)
        {
            var h = p.Health;
            if (p.TreatmentFacility == null) return "의료용 침상에 있어야 합니다(바닥 자리 수술 불가)";
            if (h.wounds.Any(w => w.NeedsTend && MedicalRules.BleedFactor(w) > 0)) return "출혈 처치가 먼저 필요합니다";
            if (s.stage == SurgeryStage.Waiting)
                foreach (var (type, amount) in SurgeryCost(s.kind))
                    if ((ResourceManager.Instance?.GetAmount(type) ?? 0) - Reserved(type, s) < amount) return $"{type.DisplayName()} 부족";
            if (surgeon == null) return "";
            if (surgeon == p) return "자기 자신은 수술할 수 없습니다";
            if (s.surgeonId != "" && surgeon.PersonalState.id != s.surgeonId) return "지정 집도의만 수술합니다";
            if (!surgeon.AllowsJob(CommanderJobs.Nursing)) return "의료 작업이 꺼진 장수";
            return "";
        }
        public static CommanderAnt AutoSurgeon(CommanderAnt p) => HomePatients().Where(c => c != p && c.AllowsJob(CommanderJobs.Nursing) && c.CivilianWorkReady && !c.IsMedicalPatient)
            .OrderByDescending(c => c.Talents.Level(CommanderActivity.Medicine)).FirstOrDefault();
        public static float SuccessChance(HealthState h, float skill) => Mathf.Clamp(.35f + skill * .03f - h.bloodLoss * .3f - (1 - h.Consciousness) * .1f, .05f, .97f);
        public float PredictedSuccess(SurgeryOrder s, CommanderAnt surgeon)
        {
            var skill = surgeon != null ? surgeon.Talents.Level(CommanderActivity.Medicine) : 0;
            // 교대가 있었으면 지금까지의 실제 작업 비중과 남은 몫을 섞는다.
            var avg = s.work > 0 ? (s.weighted + (SurgeryWork - s.work) * skill) / SurgeryWork : skill;
            return SuccessChance(Health, avg);
        }

        private bool TickSurgery(CommanderAnt p, float seconds, float rate)
        {
            var h = p.Health; var s = h.surgeries.FirstOrDefault(x => x.Active);
            if (s == null) { EndMed(); return false; }
            if (SurgeryBlock(p, s, this) != "" && s.stage == SurgeryStage.Waiting) { EndMed(); return false; }
            if (s.stage == SurgeryStage.Waiting)
            {
                // 시작 순간 물자 소비(그 전에는 예약만). 시작 후에는 취소 불가.
                foreach (var (type, amount) in SurgeryCost(s.kind)) if (!ResourceManager.Instance.TrySpend(type, amount, ResourceReason.Treatment)) { EndMed(); return false; }
                s.stage = SurgeryStage.Started;
            }
            if (p.TreatmentFacility == null) { EndMed(); return false; } // 병상 이탈: 진행 보존·정지
            var work = seconds * rate;
            s.work += work; s.weighted += work * talents.Level(CommanderActivity.Medicine);
            medWork = 0;
            if (s.work < SurgeryWork) return true;
            FinishSurgery(p, s);
            EndMed(); return true;
        }
        // 성공 판정은 각 집도의의 실제 작업 비중으로 가중한 의료 능력(마지막 1% 작업자만 보지 않는다).
        internal static void FinishSurgery(CommanderAnt p, SurgeryOrder s)
        {
            var h = p.Health; var skill = s.work > 0 ? s.weighted / s.work : 0;
            var chance = SuccessChance(h, skill);
            if (MedicalRules.Roll() < chance)
            {
                s.stage = SurgeryStage.Done; s.result = "성공";
                if (s.kind == SurgeryKind.Amputate)
                {
                    h.wounds.RemoveAll(w => w.part == s.part); h.illnesses.RemoveAll(i => i.part == s.part && i.kind == IllnessKind.WoundInfection);
                    h.wounds.Add(new Wound { part = s.part, kind = WoundKind.Missing, damage = MedicalRules.MaxHp(s.part) * .3f, tended = true, quality = Mathf.Clamp01(skill / 20f), age = MedicalRules.FreshMissingSeconds, infectionRolled = true });
                }
                else h.wounds.RemoveAll(w => w.part == s.part && w.Permanent);
                // 성공해도 회복이 필요하다: 처치된 수술 상처가 시간에 따라 낫는다.
                h.wounds.Add(new Wound { part = MedicalRules.Vital(s.part) ? s.part : BodyPart.Thorax, kind = WoundKind.Surgical, damage = 3, tended = true, quality = Mathf.Clamp01(skill / 20f), infectionRolled = true });
            }
            else
            {
                // 실패: 시술 미완료 + 추가 손상, 낮은 확률로 사망(현재 환자 상태로 위험 결정, 실패 횟수 벌점 없음). 자동 재시도 없음.
                s.stage = SurgeryStage.Failed; s.result = "실패 — 시술 미완료·추가 손상";
                var target = h.Missing(s.part) ? BodyPart.Thorax : s.part;
                h.wounds.Add(new Wound { part = target, kind = WoundKind.Cut, damage = MedicalRules.MaxHp(target) * .3f });
                if (MedicalRules.Roll() < .08f + h.bloodLoss * .3f + (1 - h.Consciousness) * .2f && CommanderDeathRuntime.Mode != CommanderDeathMode.Gentle)
                { s.result = "실패 — 사망"; p.OnDowned("수술 실패", true); }
            }
            CampaignHistory.Record("수술", p.CommanderName, SurgeryName(s.kind) + " " + s.result, true);
            AntColony.UI.ToastManager.Show($"{p.CommanderName} {SurgeryName(s.kind)}({MedicalRules.PartName(s.part)}) {s.result}");
            h.surgeries.RemoveAll(x => !x.Active && x != s);
        }
    }
}
