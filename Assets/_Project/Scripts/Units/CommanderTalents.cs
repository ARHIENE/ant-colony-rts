using System;
using UnityEngine;

namespace AntColony.Units
{
    // 작업과 전투가 공유하는 14종 기술. 경험치는 소수까지 보존한다.
    // 옛 저장(9종)은 읽는 순간 뒤쪽 새 기술을 0으로 채운다(포로·외교 포로 포함 모든 경로 공통).
    // 포텐(2026-10-07 기획): CA = 14종 기술 합(소수 진행 포함), PA = 개체별 총 성장 한도(최대 200, 생성 시 고정).
    // 실제 작업량으로 최근 활용 비중(usage)이 쌓이고, 비중이 낮은 기술은 작업량에 비례해 조금씩 줄며, CA는 PA를 넘지 않는다.
    [Serializable]
    public class CommanderTalents
    {
        public const int Count = 14, MaxLevel = 20, MaxPotential = 200;
        // ponytail: 성장·감소 잠정 수치(기획 미정). 최근 이력 길이(작업량 단위)·감소 시작 표본·최소 유지 비중·작업량당 감소 경험치.
        public const float UsageWindow = 600f, MinUsageSample = 300f, MinUsageShare = .05f, DecayPerWork = .05f, MainShare = .8f;
        public int[] levels = new int[Count];
        public float[] experience = new float[Count];
        public float combatSeconds;
        public int potential; // PA. 0 = 아직 정하지 않음(옛 저장·직접 만든 기술표), 처음 성장할 때 정한다.
        public float[] usage = new float[Count]; // 최근 유효 작업량(시간 상수 UsageWindow로 줄어듦)
        public int Level(CommanderActivity skill) => levels[(int)skill];
        public float Xp(CommanderActivity skill) => experience[(int)skill];
        public static int Required(int level) => (level + 1) * 100;
        public float Multiplier(CommanderActivity skill) => .6f + .04f * Level(skill);
        public float Value(int i) => levels[i] >= MaxLevel ? MaxLevel : levels[i] + experience[i] / Required(levels[i]);
        public float Current { get { float sum = 0; for (int i = 0; i < Count; i++) sum += Value(i); return sum; } }
        public float UsageShare(CommanderActivity skill) { float total = 0; foreach (var u in usage) total += u; return total > 0 ? usage[(int)skill] / total : 0; }

        // 원시 경험치 추가(PA·활용 이력 무시). 저장 이관·검사용. 실제 작업 성장은 Train.
        public void Add(CommanderActivity skill, float amount)
        {
            if (!(amount > 0) || float.IsInfinity(amount) || !Enum.IsDefined(typeof(CommanderActivity), skill)) return;
            int i = (int)skill;
            if (levels[i] >= MaxLevel) return;
            double pool = experience[i] + (double)amount;
            while (levels[i] < MaxLevel && pool >= Required(levels[i])) pool -= Required(levels[i]++);
            experience[i] = levels[i] == MaxLevel ? 0 : (float)pool;
        }

        // 활동별 보조 능력(기획 표 중 지금 구현된 활동만). 주 80% / 보조 20%, 보조가 없으면 주 100%.
        public static CommanderActivity? Support(CommanderActivity main) => main switch
        {
            CommanderActivity.Gathering or CommanderActivity.Building or CommanderActivity.Farming or CommanderActivity.Fishing
                or CommanderActivity.Melee => CommanderActivity.Strength,
            CommanderActivity.Medicine => CommanderActivity.Research,
            CommanderActivity.Cooking or CommanderActivity.Art => CommanderActivity.Crafting,
            CommanderActivity.Strength => CommanderActivity.Gathering, // 실제 운반: 근력 주, 채집 보조
            _ => null
        };

        // 실제 작업 성장. work = 유효 작업량(활용 이력용), rate = 기술별 학습 배율(열정·학습 특성·부상, 이력에는 넣지 않음).
        // topic = 연구 주제·제작 분류로 정해지는 보조 능력(있으면 활동 기본 보조 대신 사용).
        public void Train(CommanderActivity main, float work, Func<CommanderActivity, float> rate, CommanderActivity? topic = null)
        {
            if (!(work > 0) || float.IsInfinity(work) || !Enum.IsDefined(typeof(CommanderActivity), main)) return;
            EnsurePotential();
            var support = topic != null && topic != main ? topic : Support(main);
            float mainShare = support == null ? 1f : MainShare;
            float keep = Mathf.Exp(-work / UsageWindow), total = 0;
            for (int i = 0; i < Count; i++) { usage[i] *= keep; }
            usage[(int)main] += work * mainShare;
            if (support != null) usage[(int)support.Value] += work * (1f - mainShare);
            foreach (var u in usage) total += u;
            // 최근 활용이 부족한 기술 전체에 감소를 나눈다(한 기술에 몰지 않음, 0 아래로 안 내려감).
            if (total >= MinUsageSample)
            {
                int candidates = 0;
                for (int i = 0; i < Count; i++) if (usage[i] / total < MinUsageShare && Value(i) > 0) candidates++;
                if (candidates > 0)
                    for (int i = 0; i < Count; i++) if (usage[i] / total < MinUsageShare && Value(i) > 0) Remove(i, work * DecayPerWork / candidates);
            }
            // 주·보조를 함께 성장시킨 뒤 CA가 PA를 넘으면 넘친 만큼 두 성장분에서 비율대로 되돌린다(주 능력이 여유를 독점하지 않음, 내릴 후보가 없으면 정체).
            int m = (int)main, s = support.HasValue ? (int)support.Value : -1;
            float beforeMain = Value(m), beforeSupport = s >= 0 ? Value(s) : 0;
            Add(main, work * mainShare * rate(main));
            if (s >= 0) Add(support.Value, work * (1f - mainShare) * rate(support.Value));
            float excess = Current - potential;
            if (excess <= 0) return;
            float gainMain = Value(m) - beforeMain, gainSupport = s >= 0 ? Value(s) - beforeSupport : 0, gain = gainMain + gainSupport;
            if (gain <= 0) return;
            float cut = Mathf.Min(excess, gain);
            SetValue(m, Value(m) - cut * gainMain / gain);
            if (s >= 0) SetValue(s, Value(s) - cut * gainSupport / gain);
        }
        private void Remove(int i, float xp)
        {
            if (levels[i] >= MaxLevel) { levels[i] = MaxLevel - 1; experience[i] = Required(levels[i]); }
            double pool = experience[i] - (double)xp;
            while (pool < 0 && levels[i] > 0) pool += Required(--levels[i]);
            experience[i] = (float)Math.Max(0, Math.Min(pool, Required(levels[i]) - .001));
        }
        private void SetValue(int i, float value)
        {
            value = Mathf.Clamp(value, 0, MaxLevel);
            levels[i] = Mathf.Min(MaxLevel, Mathf.FloorToInt(value));
            experience[i] = levels[i] >= MaxLevel ? 0 : Mathf.Min((value - levels[i]) * Required(levels[i]), Required(levels[i]) - .001f);
        }

        // ponytail: PA 분포·유전은 기획 미정. 잠정: max(80, CA+20)~200 균등, 번식은 부모 평균 ±20.
        public void EnsurePotential()
        {
            int floor = Mathf.CeilToInt(Current - .001f);
            if (potential <= 0) potential = Mathf.Clamp(UnityEngine.Random.Range(Mathf.Max(80, floor + 20), MaxPotential + 1), floor, MaxPotential);
            else if (potential < floor) potential = Mathf.Min(MaxPotential, floor);
        }

        public void Generate(CommanderTraits traits, CommanderTalents first = null, CommanderTalents second = null)
        {
            // 씬·프리팹에 직렬화된 기술표는 옛 길이(13)일 수 있어 새로 만든다.
            levels = new int[Count]; experience = new float[Count]; usage = new float[Count]; combatSeconds = 0; potential = 0;
            if (first != null && second != null)
            {
                for (int i = 0; i < Count; i++) levels[i] = Mathf.Clamp(Mathf.RoundToInt((first.levels[i] + second.levels[i]) * .25f) + UnityEngine.Random.Range(0, 4), 0, MaxLevel);
                first.EnsurePotential(); second.EnsurePotential();
                potential = Mathf.Clamp((first.potential + second.potential) / 2 + UnityEngine.Random.Range(-20, 21), 1, MaxPotential);
                EnsurePotential();
                return;
            }
            // 열정이 있는 기술의 추첨 비중을 높여 시작 합계 40을 배분한다.
            for (int point = 0; point < 40;)
            {
                int i = UnityEngine.Random.Range(0, Count);
                if (levels[i] >= MaxLevel || UnityEngine.Random.value > (1 + traits.Flame((CommanderActivity)i) * 2) / 5f) continue;
                levels[i]++; point++;
            }
            EnsurePotential();
        }
        public CommanderTalents Copy() => JsonUtility.FromJson<CommanderTalents>(JsonUtility.ToJson(this));
        public bool Validate()
        {
            if (usage == null || usage.Length != Count) usage = new float[Count]; // 이전 저장에는 활용 이력이 없다.
            if (levels == null || experience == null || levels.Length != Count || experience.Length != Count
                || float.IsNaN(combatSeconds) || combatSeconds < 0 || combatSeconds >= 10
                || potential < 0 || potential > MaxPotential) return false;
            for (int i = 0; i < Count; i++)
                if (levels[i] < 0 || levels[i] > MaxLevel || float.IsNaN(experience[i]) || experience[i] < 0
                    || (levels[i] == MaxLevel ? experience[i] != 0 : experience[i] >= Required(levels[i]))
                    || float.IsNaN(usage[i]) || float.IsInfinity(usage[i]) || usage[i] < 0) return false;
            return true;
        }
    }
}
