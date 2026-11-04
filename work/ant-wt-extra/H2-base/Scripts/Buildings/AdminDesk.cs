using AntColony.Core;
using AntColony.Units;

namespace AntColony.Buildings
{
    // 행정 시설(2026-10-08 기획, 이름·비용 잠정): 작업표 '행정'이 허용된 장수가 와서 정치 기술로 업무를 처리한다.
    public sealed class AdminDesk : BuildingBase
    {
        public bool NeedsWork => !IsDead && isActiveAndEnabled && ColonyPopulation.Instance != null && ColonyPopulation.Instance.NeedsAdministration;
        public bool Work(CommanderAnt c, float seconds)
        {
            if (!NeedsWork || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            ColonyPopulation.Instance.AddAdministration(c.WorkRate(CommanderActivity.Politics) * seconds);
            c.GainExperience(CommanderActivity.Politics, seconds);
            return true;
        }
    }
}
