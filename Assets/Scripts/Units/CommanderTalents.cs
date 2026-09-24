using System;
using UnityEngine;

namespace AntColony.Units
{
    // 작업과 전투가 공유하는 9종 기술. 경험치는 소수까지 보존한다.
    [Serializable]
    public class CommanderTalents
    {
        public const int Count = 9, MaxLevel = 20;
        public int[] levels = new int[Count];
        public float[] experience = new float[Count];
        public float combatSeconds;
        public int Level(CommanderActivity skill) => levels[(int)skill];
        public float Xp(CommanderActivity skill) => experience[(int)skill];
        public static int Required(int level) => (level + 1) * 100;
        public float Multiplier(CommanderActivity skill) => .6f + .04f * Level(skill);
        public void Add(CommanderActivity skill, float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount) || !Enum.IsDefined(typeof(CommanderActivity), skill)) return;
            int i = (int)skill;
            if (levels[i] >= MaxLevel) return;
            double pool = experience[i] + (double)amount;
            while (levels[i] < MaxLevel && pool >= Required(levels[i])) pool -= Required(levels[i]++);
            experience[i] = levels[i] == MaxLevel ? 0 : (float)pool;
        }
        public void Generate(CommanderTraits traits, CommanderTalents first = null, CommanderTalents second = null)
        {
            Array.Clear(levels, 0, Count); Array.Clear(experience, 0, Count); combatSeconds = 0;
            if (first != null && second != null)
            {
                for (int i = 0; i < Count; i++) levels[i] = Mathf.Clamp(Mathf.RoundToInt((first.levels[i] + second.levels[i]) * .25f) + UnityEngine.Random.Range(0, 4), 0, MaxLevel);
                return;
            }
            // 열정이 있는 기술의 추첨 비중을 높여 시작 합계 40을 배분한다.
            for (int point = 0; point < 40;)
            {
                int i = UnityEngine.Random.Range(0, Count);
                if (levels[i] >= MaxLevel || UnityEngine.Random.value > (1 + traits.Flame((CommanderActivity)i) * 2) / 5f) continue;
                levels[i]++; point++;
            }
        }
        public CommanderTalents Copy() => JsonUtility.FromJson<CommanderTalents>(JsonUtility.ToJson(this));
        public bool Validate()
        {
            if (levels == null || experience == null || levels.Length != Count || experience.Length != Count
                || float.IsNaN(combatSeconds) || combatSeconds < 0 || combatSeconds >= 10) return false;
            for (int i = 0; i < Count; i++)
                if (levels[i] < 0 || levels[i] > MaxLevel || float.IsNaN(experience[i]) || experience[i] < 0
                    || (levels[i] == MaxLevel ? experience[i] != 0 : experience[i] >= Required(levels[i]))) return false;
            return true;
        }
    }
}
