using UnityEngine;

namespace AntColony.Units
{
    // 장수 채집 숙련도. CommanderProgression처럼 CommanderAnt가 들고 있는 순수 데이터라
    // 보직/관직/병력 변경, 비활성화 어디서도 초기화되지 않는다. 일반 채집·밭·낚시가 모두 같은 값을 쓴다.
    [System.Serializable]
    public class CommanderWorkProficiency
    {
        // ponytail: 프로토타입 잠정 수치. 밸런스 확정 시 이 상수만 고친다.
        public const int MaxLevel = 5;
        public const float UnitsPerLevel = 100f;
        public const float GatherBonusPerLevel = 0.1f;

        [SerializeField, Min(0)] private int level;
        [SerializeField, Min(0f)] private float progress;

        public int Level => level;
        public float Progress => progress;
        public float GatherMultiplier => 1f + level * GatherBonusPerLevel;

        // 실제로 캐낸 양만 넣는다. 0/음수/NaN은 무시하고 남은 소수 진행도는 다음 레벨로 이월된다.
        public void AddGathered(float amount)
        {
            if (!(amount > 0f) || level >= MaxLevel) return;
            progress += amount;
            while (level < MaxLevel && progress >= UnitsPerLevel)
            {
                progress -= UnitsPerLevel;
                level++;
            }
            if (level >= MaxLevel) progress = 0f;
        }
    }
}
