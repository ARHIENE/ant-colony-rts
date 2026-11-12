using AntColony.Buildings;
using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        public int SchoolCount => CivicService.Count(CivicServiceKind.School);
        public int ClinicCount => CivicService.Count(CivicServiceKind.Clinic);
        public bool SchoolsOperating => !S.schoolsSuspended;
        public bool ClinicsOperating => !S.clinicsSuspended;
        public float EducationCoverage => S.young == 0 ? 0f : Mathf.Clamp01((SchoolsOperating ? SchoolCount * 20f : 0f) / S.young);
        public float MedicalCoverage => Total == 0 ? 0f : Mathf.Clamp01((ClinicsOperating ? ClinicCount * 50f : 0f) / Total);
        public int PublicServiceCapacity => 500 + 500 * (Facilities + CivicService.Count(CivicServiceKind.PublicService)
            + (SchoolsOperating ? SchoolCount : 0) + (ClinicsOperating ? ClinicCount : 0));
        public float PublicServiceCoverage => Total <= 500 ? 1f : Mathf.Clamp01(PublicServiceCapacity / (float)Total);
        public float CivicSentiment => (S.educationSeconds > 0 ? S.educationBonus * 50f : 0f) + MedicalCoverage * 5f;

        public void ApplyEducationAtAdulthood()
        {
            S.educationBonus = EducationCoverage * .1f;
            S.educationSeconds = S.educationBonus > 0 ? GameCalendar.SecondsPerSeason : 0;
        }

        private void TickCivicEconomy(float seconds)
        {
            var days = seconds / GameCalendar.SecondsPerDay;
            if (SchoolsOperating) { S.schoolFoodDebt += SchoolCount * .3f * days; S.schoolMaterialDebt += SchoolCount * .15f * days; }
            if (ClinicsOperating) { S.clinicFoodDebt += ClinicCount * .3f * days; S.clinicMedicineDebt += ClinicCount * .15f * days; }
            S.educationSeconds = Mathf.Max(0, S.educationSeconds - seconds);
            if (S.educationSeconds == 0) S.educationBonus = 0;
        }

        // 소수 비용은 저장하고 정수 몫만 지불한다. 재고 부족분은 다음 납세일에 재시도한다.
        public void PayCivicUpkeep()
        {
            PayCivicDebt(ref S.schoolFoodDebt, AntColony.Data.ResourceType.Food);
            PayCivicDebt(ref S.schoolMaterialDebt, AntColony.Data.ResourceType.Soil);
            PayCivicDebt(ref S.clinicFoodDebt, AntColony.Data.ResourceType.Food);
            PayCivicDebt(ref S.clinicMedicineDebt, AntColony.Data.ResourceType.MedicineBasic);
            S.schoolsSuspended = S.schoolFoodDebt >= .99999f || S.schoolMaterialDebt >= .99999f;
            S.clinicsSuspended = S.clinicFoodDebt >= .99999f || S.clinicMedicineDebt >= .99999f;
        }

        private static void PayCivicDebt(ref float debt, AntColony.Data.ResourceType resource)
        {
            var rm = ResourceManager.Instance;
            if (rm == null) return;
            var amount = Mathf.Min(Mathf.FloorToInt(debt + .00001f), rm.GetAmount(resource));
            if (amount > 0 && rm.TrySpend(resource, amount, ResourceReason.Upkeep)) debt = Mathf.Max(0, debt - amount);
        }

        public static string CivicRecoveryText(State state, int food, int soil, int medicine)
        {
            int Due(float debt) => Mathf.FloorToInt(debt + .00001f);
            var requiredFood = Due(state.schoolFoodDebt) + Due(state.clinicFoodDebt);
            var requiredSoil = Due(state.schoolMaterialDebt);
            var requiredMedicine = Due(state.clinicMedicineDebt);
            return $"전체 복구 재고: 식량 {requiredFood}(부족 {Mathf.Max(0, requiredFood - food)}) / 흙 {requiredSoil}(부족 {Mathf.Max(0, requiredSoil - soil)}) / 기초약 {requiredMedicine}(부족 {Mathf.Max(0, requiredMedicine - medicine)})"
                + "\n종류별 전체 중단 · 매일 아침 재고로 정산 후 재개 · 식량은 학당 먼저 지불";
        }

        public static bool ValidCivicState(State s) => s != null
            && ValidNaturalDeaths(s)
            && FiniteRange(s.educationBonus, .1f) && FiniteRange(s.educationSeconds, GameCalendar.SecondsPerSeason)
            && FiniteRange(s.schoolFoodDebt, 1e7f) && FiniteRange(s.schoolMaterialDebt, 1e7f)
            && FiniteRange(s.clinicFoodDebt, 1e7f) && FiniteRange(s.clinicMedicineDebt, 1e7f)
            && ValidCivilianState(s);
        private static bool FiniteRange(float value, float max) => value >= 0 && value <= max;
    }
}
