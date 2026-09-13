using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    // 장수 전투 경험치/레벨. GameObject가 아니라 CommanderAnt가 들고 있는 순수 데이터이므로
    // 보직/관직/배정 변경, 병력 0, 비활성화 어디서도 초기화되지 않는다.
    [System.Serializable]
    public class CommanderProgression
    {
        public const int MaxLevel = 20;

        // ponytail: 프로토타입 잠정 수치. 밸런스 조정은 이 상수 4개와 XpForNext만 고치면 된다.
        public const int MonsterKillXp = 25;
        public const int BuildingKillXp = 50;
        public const int BossKillXp = 100;

        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int xp;

        public int Level => level;
        public int Xp => xp;

        // 다음 레벨까지 필요한 경험치. 만렙이면 0.
        public int XpToNext => level >= MaxLevel ? 0 : XpForLevel(level);

        // 레벨당 공격력/방어력 +1 (1레벨은 보너스 없음). 공격 보너스는 기존 공격력처럼 1마리분 기준이다.
        public int AttackBonus => level - 1;
        public int ArmorBonus => level - 1;

        public static int XpForLevel(int currentLevel) => 100 * currentLevel;

        // 음수/0은 무시하고, int.MaxValue가 들어와도 만렙에서 깔끔히 멈춘다. 잉여 경험치는 다음 레벨로 이월된다.
        public int AddXp(int amount)
        {
            if (amount <= 0 || level >= MaxLevel) return 0;

            var pool = (long)xp + amount;
            var gained = 0;
            while (level < MaxLevel && pool >= XpForLevel(level))
            {
                pool -= XpForLevel(level);
                level++;
                gained++;
            }
            // 만렙에서는 남은 경험치를 버린다(저장할 곳이 없다). 그 외에는 pool < 요구치이므로 int 변환이 안전하다.
            xp = level >= MaxLevel ? 0 : (int)pool;
            return gained;
        }

        // 처치 보상. 경험치를 주지 않는 대상은 0.
        public static int KillXp(IDamageable target)
        {
            switch (target)
            {
                case BossHealth _: return BossKillXp;
                case WildMonster _: return MonsterKillXp;
                case BuildingBase _: return BuildingKillXp;
                default: return 0;
            }
        }
    }
}
