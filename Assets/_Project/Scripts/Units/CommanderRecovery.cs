using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        public string OrderRefusalReason => IsDead || PersonalHealth <= 0 || Health.Downed ? "행동 불능"
            : personalState.rageRemaining > 0 ? $"분노 {personalState.rageRemaining:0}초"
            : personalState.mentalBreak != MentalBreak.None ? $"정신 붕괴 {personalState.breakRemaining:0}초"
            : "";
        private bool SelfTendAvailable => !IsDead && !IsAsleep && Health.selfTend && Health.medicine != MedicineLevel.NoCare
            && !IsCaptive && !IsEmbarked && !IsAwayFromHome && !IsDeployed && !IsDeparting
            && personalState.rageRemaining <= 0 && personalState.mentalBreak == MentalBreak.None && AllowsJob(CommanderJobs.Nursing);
        public bool CanSelfTend => SelfTendAvailable && (PersonalHealth > 0 || Health.Downed)
            && Health.Consciousness > .3f && Health.Manipulation > .3f;
        public bool StableForRecovery => !Health.Bleeding && Health.illnesses.Count == 0 && Satiety > 0;
        public float RecoveryRemaining => PersonalHealth > 0 ? 0 : Mathf.Max(0, GameCalendar.SecondsPerDay - WorkState.unconsciousSeconds);
        public string RecoveryStatus => PersonalHealth > 0 ? "의식 있음" : !StableForRecovery ? "회복 대기 · 출혈/질병/기아 해소 필요"
            : Health.Downed ? $"예상 의식 회복 {RecoveryRemaining:0}초 · 부위 회복 필요" : $"예상 의식 회복 {RecoveryRemaining:0}초";
        private bool TickRecovery(float seconds)
        {
            if (PersonalHealth > 0 && !Health.Downed) { WorkState.unconsciousSeconds = 0; return false; }
            if (PersonalHealth > 0) { CommandStop(); DropCargo(); WorkState.health = 0; }
            MealState.satiety = Mathf.Max(0, Satiety - GameBalance.SatietyPerSecond * seconds);
            WorkState.unconsciousSeconds = Mathf.Min(GameCalendar.SecondsPerDay, WorkState.unconsciousSeconds + seconds);
            WorkState.recoverySeconds = RecoveryRemaining;
            if (StableForRecovery && !Health.Downed && RecoveryRemaining == 0)
            { WorkState.health = GameBalance.CommanderHealth * .2f; WorkState.recoverySeconds = WorkState.unconsciousSeconds = 0; }
            if (CanSelfTend)
            {
                if (!TickMed(seconds) && MedicalRules.NeedsDoctor(Health, MedicineAllowed(Health)) && !Taken(this, MedJob.Tend)) StartMed(MedJob.Tend, this);
            }
            else if (medPatient == this) EndMed();
            return true;
        }

        // GameManager 연결용: 모두 쓰러졌어도 가장 낙관적인 자연 회복 경로가 있으면 패배시키지 않는다.
        public static bool HasRecoveryOrRescueRoute(System.Collections.Generic.IEnumerable<CommanderAnt> roster)
        {
            var available = roster.Where(c => c != null && !c.IsDead && !c.IsCaptive && !c.IsDeparting && !c.IsHostile).ToArray();
            if (available.Any(c => c.PersonalHealth > 0 && !c.Health.Downed || c.IsAwayFromHome || c.IsEmbarked)) return true;
            foreach (var c in available)
            {
                var h = JsonUtility.FromJson<HealthState>(JsonUtility.ToJson(c.Health));
                float hunger = c.Satiety, elapsed = c.WorkState.unconsciousSeconds;
                float tendWork = c.medPatient == c && c.medJob == MedJob.Tend ? c.medWork : 0;
                var medicines = MedicalRules.Medicines.ToDictionary(t => t, FreeMedicine);
                var roll = MedicalRules.Roll;
                try
                {
                    MedicalRules.Roll = () => 1f; // 감염·흉터가 생기지 않는 최선의 경우에도 불가능해야 패배.
                    while (hunger > 0)
                    {
                        if (h.Fatal) break;
                        if (elapsed >= GameCalendar.SecondsPerDay && !h.Downed && !h.Bleeding && h.illnesses.Count == 0) return true;
                        var tier = MedicalRules.BestMedicine(h.medicine, t => medicines[t]);
                        // 가능한 처치도 출혈·기아보다 먼저 끝나야 생존 경로다.
                        if (c.SelfTendAvailable && h.Downed && h.Consciousness > .3f && h.Manipulation > .3f
                            && MedicalRules.NeedsDoctor(h, tier >= 0))
                        {
                            tendWork += c.WorkRate(CommanderActivity.Medicine);
                            if (tendWork >= MedicalRules.TendSeconds * 2)
                            {
                                MedicalRules.ApplyTend(h, MedicalRules.TendQuality(c.talents.Level(CommanderActivity.Medicine), tier,
                                    c.TreatmentFacility != null, h.Manipulation, true), tier >= 0);
                                if (tier >= 0) medicines[MedicalRules.Medicines[tier]]--;
                                tendWork = 0;
                            }
                        }
                        MedicalRules.Tick(h, 1, true, false); elapsed++; hunger -= GameBalance.SatietyPerSecond;
                    }
                }
                finally { MedicalRules.Roll = roll; }
            }
            return false;
        }
    }
}
