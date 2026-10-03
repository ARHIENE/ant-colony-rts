using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Units
{
    public enum CommanderPersonality { Balanced, Brave, Cautious, Devoted }
    public enum CommanderTrait
    {
        Lazy, Relaxed, Industrious, Workaholic, Depressive, Pessimist, Optimist, Cheerful,
        Fragile, Sensitive, Easygoing, IronWill, Coward, Cautious, Brave, Reckless,
        Sluggish, Slow, Nimble, Swift, SlowLearner, Genius, LightEater, Glutton,
        Loyal, Ambitious, Cunning, Sociable, Loner, ColdBlooded, Bloodthirsty, Ascetic,
        Wanderer, Homebody, Robust, Frail, Greedy,
        Nocturnal, // 야행성(2026-09-28): 낮에 자고 밤에 일한다. 다른 특성처럼 랜덤.
        Muscular, Mighty, ThinLegs, // 왕근육 ×1.5 / 괴력 ×2 / 가는 다리 ×0.7: 한 짐 운반량의 장수 본인 부분
        CannotBuild, CannotGather, CannotResearch, CannotNurse, CannotCook, Uncultured, Pacifist, Chef,
        // 식사 특성(ONI): 미식가·무딘 미각·주방 테러범·강철 위장·느긋한 식사
        Gourmet, DullTaste, KitchenMenace, IronStomach, SlowEater,
        Undertaker, Cannibal, Neat,
        Senile // 노망(2026-10-03): 늙으면 지혜 보너스 대신 모든 작업·연구 속도 ×0.8(잠정).
    }
    // 기술 13종(2026-09-28): 기존 9종 뒤에 의료·요리·근력·예술. 저장 번호가 밀리지 않게 끝에 붙인다.
    public enum CommanderActivity { Gathering, Building, Farming, Fishing, Crafting, Research, Melee, Ranged, Command, Medicine, Cooking, Strength, Art }
    [Serializable] public class CommanderPassion { public CommanderActivity activity; public int flame; }
    [Serializable]
    public class CommanderTraits
    {
        [SerializeField] private CommanderPersonality personality;
        public List<CommanderTrait> values = new List<CommanderTrait>();
        public List<CommanderPassion> passions = new List<CommanderPassion>();
        public CommanderPersonality Personality => Has(CommanderTrait.Brave) ? CommanderPersonality.Brave : Has(CommanderTrait.Cautious) ? CommanderPersonality.Cautious : Has(CommanderTrait.Loyal) ? CommanderPersonality.Devoted : CommanderPersonality.Balanced;
        // Phase 3(2026-10-01): 충성심 삭제. 기분이 이 값 이하로 한 달 이어지면 탈주·반란 판정. 충직은 더 버티고 교활은 쉽게 떠난다(잠정).
        public int DepartureMood => Has(CommanderTrait.Loyal) ? 10 : Has(CommanderTrait.Cunning) ? 30 : 20;
        public bool Has(CommanderTrait trait) => values.Contains(trait) || (values.Count == 0 && ((trait == CommanderTrait.Brave && personality == CommanderPersonality.Brave) || (trait == CommanderTrait.Cautious && personality == CommanderPersonality.Cautious) || (trait == CommanderTrait.Loyal && personality == CommanderPersonality.Devoted)));
        public int AttackBonus => Has(CommanderTrait.Coward) ? -2 : Has(CommanderTrait.Cautious) ? -1 : Has(CommanderTrait.Brave) ? 2 : Has(CommanderTrait.Reckless) ? 3 : 0;
        public int ArmorBonus => Has(CommanderTrait.Coward) ? 1 : Has(CommanderTrait.Cautious) ? 2 : Has(CommanderTrait.Brave) ? -1 : Has(CommanderTrait.Reckless) ? -3 : 0;
        public float WorkMultiplier => Has(CommanderTrait.Lazy) ? .7f : Has(CommanderTrait.Relaxed) ? .85f : Has(CommanderTrait.Industrious) ? 1.15f : Has(CommanderTrait.Workaholic) ? 1.3f : 1f;
        public float MoveMultiplier => Has(CommanderTrait.Sluggish) ? .8f : Has(CommanderTrait.Slow) ? .9f : Has(CommanderTrait.Nimble) ? 1.1f : Has(CommanderTrait.Swift) ? 1.2f : 1f;
        public float LearningMultiplier => Has(CommanderTrait.SlowLearner) ? .5f : Has(CommanderTrait.Genius) ? 1.5f : 1f;
        public float CarryMultiplier => Has(CommanderTrait.Muscular) ? 1.5f : Has(CommanderTrait.Mighty) ? 2f : Has(CommanderTrait.ThinLegs) ? .7f : 1f;
        public bool Blocks(CommanderJobs job) => job switch {
            CommanderJobs.Building or CommanderJobs.Repair => Has(CommanderTrait.CannotBuild),
            CommanderJobs.Gathering => Has(CommanderTrait.CannotGather), CommanderJobs.Research => Has(CommanderTrait.CannotResearch),
            CommanderJobs.Nursing => Has(CommanderTrait.CannotNurse), CommanderJobs.Cooking => Has(CommanderTrait.CannotCook),
            CommanderJobs.Art => Has(CommanderTrait.Uncultured), CommanderJobs.Hunting => Has(CommanderTrait.Pacifist), _ => false };
        // 미식가 요리 +3 / 주방 테러범 -3: 식사 품질·식중독 판정에만 쓴다.
        public int CookingBonus => Has(CommanderTrait.Gourmet) ? 3 : Has(CommanderTrait.KitchenMenace) ? -3 : 0;
        // 느끼는 식사 등급: 미식가 한 칸 낮게, 무딘 미각·주방 테러범 한 칸 높게.
        public int TasteShift => Has(CommanderTrait.Gourmet) ? -1 : Has(CommanderTrait.DullTaste) || Has(CommanderTrait.KitchenMenace) ? 1 : 0;
        public float FoodMultiplier =>Has(CommanderTrait.LightEater) ? .7f : Has(CommanderTrait.Glutton) ? 1.5f : 1f;
        public float NegativeMoodMultiplier => Has(CommanderTrait.Sensitive) ? 1.5f : Has(CommanderTrait.Easygoing) ? .5f : 1f;
        public int BaseMood => Has(CommanderTrait.Depressive) ? -10 : Has(CommanderTrait.Pessimist) ? -5 : Has(CommanderTrait.Optimist) ? 5 : Has(CommanderTrait.Cheerful) ? 10 : 0;
        public int Flame(CommanderActivity activity) => passions.Find(p => p.activity == activity)?.flame ?? 0;
        public float GrowthMultiplier(CommanderActivity activity) => LearningMultiplier * (1f + .5f * Flame(activity));
        public CommanderTraits() { }
        public static string DisplayName(CommanderTrait trait) => trait switch {
            CommanderTrait.Undertaker => "장의사", CommanderTrait.Cannibal => "동족 포식", CommanderTrait.Neat => "결벽", CommanderTrait.Senile => "노망", _ => trait.ToString() };
        public CommanderTraits(CommanderPersonality legacy) { personality = legacy; }
        private static int Group(CommanderTrait t) => (int)t < 20 ? (int)t / 4 : (int)t < 24 ? 5 + ((int)t - 20) / 2 : -1;
        public bool TryAdd(CommanderTrait t)
        {
            if (values.Count >= 3 || values.Contains(t)) return false;
            foreach (var existing in values)
                if (Group(t) >= 0 && Group(t) == Group(existing) || Conflicts(t, existing)) return false;
            values.Add(t); return true;
        }
        private static bool Conflicts(CommanderTrait a, CommanderTrait b)
        {
            return Pair(a,b,CommanderTrait.Cannibal,CommanderTrait.Neat)
                || Pair(a,b,CommanderTrait.Chef,CommanderTrait.CannotCook) || Pair(a,b,CommanderTrait.Gourmet,CommanderTrait.DullTaste)
                || Pair(a,b,CommanderTrait.Gourmet,CommanderTrait.CannotCook) || Pair(a,b,CommanderTrait.Gourmet,CommanderTrait.KitchenMenace)
                || Pair(a,b,CommanderTrait.KitchenMenace,CommanderTrait.Chef) || Pair(a,b,CommanderTrait.Pacifist,CommanderTrait.Bloodthirsty) || Pair(a,b,CommanderTrait.Pacifist,CommanderTrait.Reckless)
                || Pair(a,b,CommanderTrait.Loyal,CommanderTrait.Ambitious) || Pair(a,b,CommanderTrait.Loyal,CommanderTrait.Cunning)
                || Pair(a,b,CommanderTrait.Sociable,CommanderTrait.Loner) || Pair(a,b,CommanderTrait.Sociable,CommanderTrait.ColdBlooded)
                || Pair(a,b,CommanderTrait.Wanderer,CommanderTrait.Homebody) || Pair(a,b,CommanderTrait.Robust,CommanderTrait.Frail)
                || Pair(a,b,CommanderTrait.Muscular,CommanderTrait.Mighty) || Pair(a,b,CommanderTrait.Muscular,CommanderTrait.ThinLegs) || Pair(a,b,CommanderTrait.Mighty,CommanderTrait.ThinLegs);
        }
        private static bool Pair(CommanderTrait a, CommanderTrait b, CommanderTrait x, CommanderTrait y) => a == x && b == y || a == y && b == x;
        public static CommanderTraits Random() => Generate(null, null);
        public static CommanderTraits Inherit(CommanderTraits first, CommanderTraits second) => Generate(first, second);
        private static CommanderTraits Generate(CommanderTraits first, CommanderTraits second)
        {
            var result = new CommanderTraits();
            float roll = UnityEngine.Random.value;
            int count = roll < .3f ? 1 : roll < .8f ? 2 : 3;
            if (first != null && second != null)
                foreach (var parent in new[] { first, second })
                    foreach (CommanderTrait t in Enum.GetValues(typeof(CommanderTrait)))
                        if (result.values.Count < count && parent.Has(t) && UnityEngine.Random.value < .5f) result.TryAdd(t);
            while (result.values.Count < count) result.TryAdd((CommanderTrait)UnityEngine.Random.Range(0, Enum.GetValues(typeof(CommanderTrait)).Length));
            int passionCount = UnityEngine.Random.Range(1,5), major = 0;
            if (first != null && second != null && UnityEngine.Random.value < .5f)
            {
                var pool = new List<CommanderPassion>(first.passions); pool.AddRange(second.passions);
                if (pool.Count > 0) { var p = pool[UnityEngine.Random.Range(0,pool.Count)]; if (p.flame > 1) result.passions.Add(new CommanderPassion { activity = p.activity, flame = p.flame - 1 }); }
            }
            while (result.passions.Count < passionCount)
            {
                var activity = (CommanderActivity)UnityEngine.Random.Range(0,CommanderTalents.Count);
                if (result.Flame(activity) > 0) continue;
                var flame = major < 2 && UnityEngine.Random.value < .35f ? 2 : 1;
                if (flame == 2) major++;
                result.passions.Add(new CommanderPassion { activity = activity, flame = flame });
            }
            return result;
        }
    }
}
