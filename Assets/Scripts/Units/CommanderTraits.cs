using UnityEngine;

namespace AntColony.Units
{
    // 장수 성격. 기획: 전투 스탯에 직접 영향을 주고, 충성심과 함께 포로 회유 확률을 좌우한다.
    public enum CommanderPersonality
    {
        Balanced,
        Brave,
        Cautious,
        Devoted
    }

    // 장수 고유 성격과 충성심. CommanderProgression과 같이 GameObject가 아닌 순수 데이터라
    // 보직·관직·병력 변경으로 초기화되지 않는다. 번식 유전과 포로 회유 판정이 이 값을 읽는다.
    [System.Serializable]
    public class CommanderTraits
    {
        // ponytail: 성격 수치와 충성심 범위는 1차 프로토타입 잠정값이다. 기획 확정 시 이 표만 고치면 된다.
        public const int MinLoyalty = 0;
        public const int MaxLoyalty = 100;

        [SerializeField] private CommanderPersonality personality = CommanderPersonality.Balanced;
        [SerializeField, Range(MinLoyalty, MaxLoyalty)] private int loyalty = 50;

        public CommanderPersonality Personality => personality;
        public int Loyalty => loyalty;

        // 용감형은 공격을 얻고 방어를 잃는다. 신중형은 그 반대다. 진영 무관 공통 규칙이다.
        public int AttackBonus => personality switch
        {
            CommanderPersonality.Brave => 2,
            CommanderPersonality.Cautious => -1,
            _ => 0
        };

        public int ArmorBonus => personality switch
        {
            CommanderPersonality.Cautious => 2,
            CommanderPersonality.Brave => -1,
            _ => 0
        };

        public CommanderTraits() { }

        public CommanderTraits(CommanderPersonality personality, int loyalty)
        {
            this.personality = personality;
            this.loyalty = Mathf.Clamp(loyalty, MinLoyalty, MaxLoyalty);
        }

        public void SetLoyalty(int value) => loyalty = Mathf.Clamp(value, MinLoyalty, MaxLoyalty);

        public void AddLoyalty(int delta) => SetLoyalty(loyalty + delta);

        public static CommanderTraits Random()
        {
            var count = System.Enum.GetValues(typeof(CommanderPersonality)).Length;
            return new CommanderTraits((CommanderPersonality)UnityEngine.Random.Range(0, count),
                UnityEngine.Random.Range(30, 71));
        }

        // 기획: 태어난 장수는 부모의 유전 + 랜덤 스탯을 함께 가진다.
        // 성격은 부모 중 한쪽을 그대로 물려받고, 충성심은 부모 평균에 ±10 편차를 준다.
        public static CommanderTraits Inherit(CommanderTraits first, CommanderTraits second)
        {
            if (first == null && second == null) return Random();
            if (first == null) return new CommanderTraits(second.personality, second.loyalty);
            if (second == null) return new CommanderTraits(first.personality, first.loyalty);

            var personality = UnityEngine.Random.value < .5f ? first.personality : second.personality;
            var averageLoyalty = Mathf.RoundToInt((first.loyalty + second.loyalty) * .5f);
            return new CommanderTraits(personality, averageLoyalty + UnityEngine.Random.Range(-10, 11));
        }
    }
}
