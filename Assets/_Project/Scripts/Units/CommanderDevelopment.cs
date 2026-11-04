using System.Linq;

namespace AntColony.Units
{
    public static class CommanderDevelopment
    {
        public static string TrainingText(CommanderTalents talents, CommanderActivity skill)
        {
            var level = talents.Level(skill);
            var progress = level >= CommanderTalents.MaxLevel ? "최고 단계" : $"다음 단계 경험치 {talents.Xp(skill):0.#}/{CommanderTalents.Required(level)}";
            var share = talents.UsageShare(skill);
            var decay = talents.usage.Sum() >= CommanderTalents.MinUsageSample && share < CommanderTalents.MinUsageShare && talents.Value((int)skill) > 10;
            return progress + $" · 기술 기본 작업 배율 ×{talents.Multiplier(skill):0.00}\n최근 활용 {share:P1}"
                + (decay ? " · 활용 부족: 다른 유효 작업을 하면 10단계까지 서서히 감소" : " · 현재 활용 감소 대상 아님")
                + "\n실제 작업에서 성장 · 주 기술 80%/보조 20%(보조 없으면 100%) · 학습 배율·성장 여유에 따라 변동";
        }
    }
}
