using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 12개월=1세. 어린 장수는 첫 1년 동안 0→18세로 성장한다. 기대수명은 사망 기한이 아니다.
    public partial class CommanderAnt
    {
        public float AgeMonths => personalState.ageMonths;
        public bool IsChild => personalState.ageMonths >= 0 && personalState.ageMonths < AdultAgeMonths;
        public bool IsElder => personalState.ageMonths >= ElderAgeMonths;
        public string AgeLabel => personalState.ageMonths < 0 ? "—" : $"{Mathf.FloorToInt(personalState.ageMonths / 12f)}세";

        public const float AdultAgeMonths = 18 * 12, ElderAgeMonths = 65 * 12, ExpectedAgeMonths = 80 * 12;
        public void SetBorn() { personalState.ageMonths = 0; personalState.ageScaleVersion = 1; personalState.lifespanMonths = ExpectedAgeMonths; }
        internal static void MigrateAge(CommanderPersonalState state)
        {
            if (state.ageScaleVersion != 0) return;
            if (state.ageMonths >= 12 && state.ageMonths <= 96)
                state.ageMonths = Mathf.Lerp(AdultAgeMonths, ElderAgeMonths, (state.ageMonths - 12) / 84);
            else if (state.ageMonths >= 0 && state.ageMonths < 12) state.ageMonths *= 18;
            state.ageScaleVersion = 1; state.lifespanMonths = ExpectedAgeMonths;
        }
        public static float AgingChance(float years) => years < 65 ? 0 : Mathf.Clamp01(.02f + (years - 65) * .002f);
        private void TickAge(float seconds)
        {
            personalState.birthRecoverySeconds = Mathf.Max(0, personalState.birthRecoverySeconds - seconds);
            MigrateAge(personalState);
            if (AgeMonths < 0) personalState.ageMonths = Random.Range(18 * 12f, 30 * 12f);
            var previous = AgeMonths;
            var months = seconds * 12 / GameCalendar.SecondsPerYear;
            if (IsChild)
            {
                var childMonths = Mathf.Min(months, (AdultAgeMonths - previous) / 18);
                personalState.ageMonths += childMonths * 18; months -= childMonths;
            }
            personalState.ageMonths += months;
            for (int year = Mathf.Max(65, Mathf.FloorToInt(previous / 12) + 1); year <= Mathf.FloorToInt(AgeMonths / 12); year++)
            {
                if (MedicalRules.Roll() < AgingChance(year))
                    Health.wounds.Add(new Wound { part = BodyPart.Thorax, kind = WoundKind.Scar, damage = MedicalRules.MaxHp(BodyPart.Thorax) * .1f, tended = true });
                if (MedicalRules.Roll() < AgingChance(year)) traits.TryAdd(CommanderTrait.Senile);
            }
        }

        private float AgeMoveMultiplier => IsElder ? .85f : 1f;
        private float AgeLearningMultiplier => IsChild ? 1.5f : 1f;
        private float AgeWorkMultiplier(CommanderActivity a) => !IsElder ? 1f
            : traits.Has(CommanderTrait.Senile) ? .8f
            : a == CommanderActivity.Command || a == CommanderActivity.Research || a == CommanderActivity.Medicine || a == CommanderActivity.Art ? 1.2f
            : a == CommanderActivity.Melee || a == CommanderActivity.Strength ? .8f : 1f;
    }
}
