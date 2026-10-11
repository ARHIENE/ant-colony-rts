using System.Linq;
using AntColony.Core;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 동물 수술·회복(2026-10-11 기획 반영). 수치는 GameBalance의 잠정값(사용자가 고른 잠정안 A).
    // - 성공률: 의료 능력·수술 전 부상 정도·후유증으로 계산. 교대하면 장수별 실제 진행 작업량 비중으로 의료 능력을 가중 평균(대기 시간 제외).
    // - 성공: 경상·중상 완치, 후유증 해소, 시설에서 회복 기간(배고픔 정지) → 완료 알림 1회 → 원래 우리로 운반 요청(없으면 '복귀할 우리 필요').
    // - 실패: 사망 / 악화 / 유지. 생존하면 후유증 +1, 시설에서 재수술·우리 복귀 선택을 기다린다(자동 재시도 없음).
    // - 고위험 첫 수술(가장 유능한 의료 장수 기준 예상 성공률 미만)과 후유증이 있는 재수술은 유저가 집도의를 지정해야 시작한다.
    public sealed partial class Critter
    {
        public int Sequelae => s.sequelae;
        public float RecoveryLeft => s.recovery;
        public bool Recovering => s.recovery > 0;
        public bool AwaitingDecision => s.awaitDecision;
        public string SurgeonId { get => s.surgeon; set => s.surgeon = value ?? ""; }
        public float SurgeryFraction => s.surgery / GameBalance.CritterSurgerySeconds;
        internal bool ReadyToLeaveClinic => !s.surgeryStarted && s.recovery <= 0 && !s.awaitDecision;
        private float operatedAt = -99;
        // 실제로 수술 작업을 받는 중이거나 시설 안에서 회복 중일 때만 배고픔이 멈춘다(집도의 대기·복귀 대기는 진행).
        private bool HungerPausedInClinic => Clinic != null && (s.surgeryStarted && Time.time - operatedAt < .5f || s.recovery > 0 && !HeavyInjury);

        public static float SuccessChance(float medicine, float healthFraction, int sequelae)
        {
            var injury = Mathf.Clamp01(1 - healthFraction / GameBalance.CritterHeavyInjury); // 체력 40%면 0, 0%면 1
            return Mathf.Clamp(GameBalance.SurgeryBaseChance + medicine * GameBalance.SurgeryChancePerMedicine - injury * GameBalance.SurgeryInjuryPenalty
                - sequelae * GameBalance.SurgerySequelaPenalty, GameBalance.SurgeryMinChance, GameBalance.SurgeryMaxChance);
        }
        public static float DeathOnFail(int sequelae) => Mathf.Clamp01(GameBalance.SurgeryFailDeath + sequelae * GameBalance.SurgerySequelaDeath);
        private static float Medicine(CommanderAnt c) => c != null ? c.Talents.Level(CommanderActivity.Medicine) : 0;

        // 이 장수가 (남은 작업을) 맡으면 예상되는 성공률·사망 위험. 진행 중인 수술은 지금까지의 기여를 포함한다.
        public float PredictedSuccess(CommanderAnt c)
        {
            var remaining = Mathf.Max(0, GameBalance.CritterSurgerySeconds - s.surgery);
            var work = s.surgeryStarted ? s.surgeryWork : 0; var skillWork = s.surgeryStarted ? s.surgerySkillWork : 0;
            var med = work + remaining > 0 ? (skillWork + Medicine(c) * remaining) / (work + remaining) : Medicine(c);
            return SuccessChance(med, s.surgeryStarted ? s.preHealth : HealthFraction, s.sequelae);
        }
        public float PredictedDeath(CommanderAnt c) => (1 - PredictedSuccess(c)) * DeathOnFail(s.sequelae);
        public static System.Collections.Generic.IEnumerable<CommanderAnt> Surgeons
            => AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c != null && c.IsColonyMember && c.CanDoJob(CommanderJobs.Nursing));
        public float BestPredictedSuccess => Surgeons.Select(PredictedSuccess).DefaultIfEmpty(0).Max();
        // 재수술(후유증 있음)·고위험 첫 수술은 유저가 집도의를 지정해야 시작한다. 이미 시작한 수술은 다른 의료 장수가 이어받을 수 있다.
        public bool SurgeryNeedsManual => s.sequelae > 0 || BestPredictedSuccess < GameBalance.SurgeryHighRisk;
        public bool MayOperate(CommanderAnt c) => c != null && !s.awaitDecision && (s.surgeryStarted || !SurgeryNeedsManual || s.surgeon == c.PersonalState.id);
        public bool AwaitingSurgeon => HeavyInjury && !s.surgeryStarted && !s.awaitDecision && SurgeryNeedsManual && s.surgeon == "";

        internal void StartSurgery() { s.surgeryStarted = true; s.surgery = 0; s.surgeryWork = s.surgerySkillWork = 0; s.preHealth = HealthFraction; s.awaitDecision = false; }
        internal void AddSurgery(CommanderAnt c, float amount)
        {
            if (!(amount > 0)) return;
            s.surgery += amount; s.surgeryWork += amount; s.surgerySkillWork += amount * Medicine(c); operatedAt = Time.time;
        }

        internal void CompleteSurgery(CommanderAnt last)
        {
            var med = s.surgeryWork > 0 ? s.surgerySkillWork / s.surgeryWork : Medicine(last); // 옛 저장(기여 기록 없음)은 마지막 집도의
            var chance = SuccessChance(med, s.preHealth, s.sequelae);
            var at = Clinic != null ? Clinic.Position : transform.position;
            s.surgeryStarted = false; s.surgery = 0; s.surgeryWork = s.surgerySkillWork = 0; s.surgeon = ""; s.treatOrder = false;
            if (Random.value < chance)
            {
                Monster.RestoreHealth(Info.health); s.sequelae = 0;
                var months = Mathf.Max(GameBalance.SurgeryMinRecoveryMonths, GameBalance.SurgeryRecoveryMonths * (1 - s.preHealth));
                s.recovery = Mathf.Max(s.recovery, months * GameCalendar.SecondsPerMonth); // 회복 중 새 수술이면 기존 잔여와 새 회복 중 긴 쪽
                ToastManager.ShowAt($"{DisplayName} 수술 성공 — 치료대에서 회복합니다.", FocusAction(at));
                return;
            }
            var death = DeathOnFail(s.sequelae); s.sequelae++;
            var roll = Random.value;
            if (roll < death) { Die("수술 실패"); return; } // 사망 알림은 OnKilled(위치 이동 가능)
            var result = "부상 유지";
            if (roll < death + GameBalance.SurgeryFailWorsen)
            {
                Monster.Wound(Mathf.Min(Info.health * GameBalance.SurgeryWorsenDamage, Monster.CurrentHealth - 1)); result = "부상 악화"; // 악화는 사망 판정과 분리
            }
            s.awaitDecision = true;
            ToastManager.ShowAt($"{DisplayName} 수술 실패({result}, 후유증 {s.sequelae}) — 재수술 또는 우리 복귀를 고르세요.", FocusAction(at));
        }

        // 시설 안 회복: 장수 없이 진행. 새 중상이 생기면 멈춘다(새 수술 대상).
        private void TickRecovery(float dt)
        {
            if (s.recovery <= 0 || Clinic == null || HeavyInjury || s.surgeryStarted) return;
            if ((s.recovery -= dt) > 0) return;
            s.recovery = 0; ReturnHome("회복 완료");
        }
        private void ReturnHome(string why)
        {
            TransportTo = ReturnPen != null && ReturnPen.Accepts(Species) ? ReturnPen : null;
            ToastManager.ShowAt($"{DisplayName} {why} — " + (TransportTo != null ? $"{TransportTo.Data.displayName}로 운반 요청" : "복귀할 우리를 지정하세요."),
                FocusAction(Clinic != null ? Clinic.Position : transform.position));
        }

        // 실패 후 선택: 재수술(집도의 지정·예상치 확인 후) 또는 원래 우리로 복귀.
        public void OrderResurgery(CommanderAnt surgeon) { if (!s.awaitDecision || surgeon == null) return; s.awaitDecision = false; s.surgeon = surgeon.PersonalState.id; s.treatOrder = true; }
        public void ChooseReturn() { if (!s.awaitDecision) return; s.awaitDecision = false; ReturnHome("우리 복귀 선택"); }

        // 시설에서 나온다(파괴·철거·입원 취소): 수술 중이었으면 처음부터(키트 반환 없음), 회복 중이면 성공·잔여 회복 보존.
        internal void ExitClinic(Vector3 at)
        {
            if (s.surgeryStarted) { s.surgeryStarted = false; s.surgery = 0; s.surgeryWork = s.surgerySkillWork = 0; }
            s.awaitDecision = false;
            Clinic = null; PutDown(at);
            if (ReturnPen != null) Pen = ReturnPen;
        }
        // 입원했지만 키트를 확보하지 못함: 원래 우리로 돌려보낸다.
        internal void AbortAdmission(Vector3 at) { ExitClinic(at); ReturnHome("수술 키트 부족"); }
    }
}
