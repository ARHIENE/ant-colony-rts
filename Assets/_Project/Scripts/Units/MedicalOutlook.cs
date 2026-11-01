using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    // 현재 조건이 유지될 때의 회복 작업량. 재투약·감염·흉터 무작위 판정은 예측하지 않는다.
    public static class MedicalOutlook
    {
        public static string RecoveryLines(HealthState health, bool resting) => string.Join("\n",
            health.wounds.Where(w => !w.Permanent).Select(w => $"{MedicalRules.PartName(w.part)} {MedicalRules.WoundName(w.kind)} · 회복 약 {WoundSeconds(w, resting):0}초")
                .Concat(health.illnesses.Select(i => MedicalRules.IllnessName(i.kind) + " · " + IllnessTrend(i, resting))));

        public static float WoundSeconds(Wound wound, bool resting) => wound.Permanent ? float.PositiveInfinity
            : wound.damage / ((wound.tended ? MedicalRules.HealTended * (.5f + wound.quality) : MedicalRules.HealUntended) * (resting ? 1.5f : 1f));

        public static string IllnessTrend(Illness illness, bool resting)
        {
            var immunitySeconds = Mathf.Max(0, 1 - illness.immunity) / ((illness.kind == IllnessKind.Mold ? .0045f : .005f) * (resting ? 1.3f : 1f));
            return illness.immunity >= 1 ? "면역 완성 · 심각도 회복 중"
                : $"면역 완성까지 약 {immunitySeconds:0}초(현재 휴식 유지) · " + (illness.tendLeft > 0 ? $"투약 품질 {illness.quality:P0}" : "투약 효과 없음");
        }
    }
}
