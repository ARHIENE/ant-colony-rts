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
        Wanderer, Homebody, Robust, Frail, Greedy
    }
    public enum CommanderActivity { Gathering, Building, Farming, Fishing, Crafting, Research, Melee, Ranged, Command }
    [Serializable] public class CommanderPassion { public CommanderActivity activity; public int flame; }
    [Serializable]
    public class CommanderTraits
    {
        public const int MinLoyalty = 0, MaxLoyalty = 100;
        [SerializeField] private CommanderPersonality personality;
        [SerializeField] private int loyalty = 50;
        public List<CommanderTrait> values = new List<CommanderTrait>();
        public List<CommanderPassion> passions = new List<CommanderPassion>();
        public List<string> loyaltyReasons = new List<string>();
        public CommanderPersonality Personality => Has(CommanderTrait.Brave) ? CommanderPersonality.Brave : Has(CommanderTrait.Cautious) ? CommanderPersonality.Cautious : Has(CommanderTrait.Loyal) ? CommanderPersonality.Devoted : CommanderPersonality.Balanced;
        public int Loyalty => loyalty;
        public bool Has(CommanderTrait trait) => values.Contains(trait) || (values.Count == 0 && ((trait == CommanderTrait.Brave && personality == CommanderPersonality.Brave) || (trait == CommanderTrait.Cautious && personality == CommanderPersonality.Cautious) || (trait == CommanderTrait.Loyal && personality == CommanderPersonality.Devoted)));
        public int AttackBonus => Has(CommanderTrait.Coward) ? -2 : Has(CommanderTrait.Cautious) ? -1 : Has(CommanderTrait.Brave) ? 2 : Has(CommanderTrait.Reckless) ? 3 : 0;
        public int ArmorBonus => Has(CommanderTrait.Coward) ? 1 : Has(CommanderTrait.Cautious) ? 2 : Has(CommanderTrait.Brave) ? -1 : Has(CommanderTrait.Reckless) ? -3 : 0;
        public float WorkMultiplier => Has(CommanderTrait.Lazy) ? .7f : Has(CommanderTrait.Relaxed) ? .85f : Has(CommanderTrait.Industrious) ? 1.15f : Has(CommanderTrait.Workaholic) ? 1.3f : 1f;
        public float MoveMultiplier => Has(CommanderTrait.Sluggish) ? .8f : Has(CommanderTrait.Slow) ? .9f : Has(CommanderTrait.Nimble) ? 1.1f : Has(CommanderTrait.Swift) ? 1.2f : 1f;
        public float LearningMultiplier => Has(CommanderTrait.SlowLearner) ? .5f : Has(CommanderTrait.Genius) ? 1.5f : 1f;
        public float FoodMultiplier => Has(CommanderTrait.LightEater) ? .7f : Has(CommanderTrait.Glutton) ? 1.5f : 1f;
        public float NegativeMoodMultiplier => Has(CommanderTrait.Sensitive) ? 1.5f : Has(CommanderTrait.Easygoing) ? .5f : 1f;
        public int BaseMood => Has(CommanderTrait.Depressive) ? -10 : Has(CommanderTrait.Pessimist) ? -5 : Has(CommanderTrait.Optimist) ? 5 : Has(CommanderTrait.Cheerful) ? 10 : 0;
        public int Flame(CommanderActivity activity) => passions.Find(p => p.activity == activity)?.flame ?? 0;
        public float GrowthMultiplier(CommanderActivity activity) => LearningMultiplier * (1f + .5f * Flame(activity));
        public CommanderTraits() { }
        public CommanderTraits(CommanderPersonality legacy, int value) { personality = legacy; SetLoyalty(value); }
        public void SetLoyalty(int value) => loyalty = Mathf.Clamp(value, 0, Has(CommanderTrait.Cunning) ? 70 : 100);
        public void AddLoyalty(int delta) => ChangeLoyalty(delta, "Event");
        public void ChangeLoyalty(int delta, string reason)
        {
            var old = loyalty; SetLoyalty(loyalty + delta);
            loyaltyReasons.Insert(0, reason + " " + (loyalty - old).ToString("+0;-0;0"));
            if (loyaltyReasons.Count > 3) loyaltyReasons.RemoveAt(3);
        }
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
            return Pair(a,b,CommanderTrait.Loyal,CommanderTrait.Ambitious) || Pair(a,b,CommanderTrait.Loyal,CommanderTrait.Cunning)
                || Pair(a,b,CommanderTrait.Sociable,CommanderTrait.Loner) || Pair(a,b,CommanderTrait.Sociable,CommanderTrait.ColdBlooded)
                || Pair(a,b,CommanderTrait.Wanderer,CommanderTrait.Homebody) || Pair(a,b,CommanderTrait.Robust,CommanderTrait.Frail);
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
            result.SetLoyalty((first != null && second != null ? Mathf.RoundToInt((first.Loyalty + second.Loyalty) * .5f) + UnityEngine.Random.Range(-10,11) : 50) + (result.Has(CommanderTrait.Loyal) ? 20 : 0));
            return result;
        }
    }
}
