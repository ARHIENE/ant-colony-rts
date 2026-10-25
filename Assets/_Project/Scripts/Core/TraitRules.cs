using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using AntColony.World;

namespace AntColony.Core
{
    public enum TraitGrade { Red, Silver, Gold, Diamond }

    public static class TraitRules
    {
        // enum 끝의 골드·다이아 쌍은 기존 values 저장 경로를 그대로 쓴다(슬롯·유전·배타도 공통).
        private static readonly CommanderActivity[] activities = { CommanderActivity.Gathering, CommanderActivity.Building,
            CommanderActivity.Farming, CommanderActivity.Fishing, CommanderActivity.Crafting, CommanderActivity.Research,
            CommanderActivity.Medicine, CommanderActivity.Cooking, CommanderActivity.Strength, CommanderActivity.Art,
            CommanderActivity.Politics, CommanderActivity.Husbandry };
        private static readonly string[] diamonds = { "무중생유", "바벨", "낙토", "태공", "무라마사", "천기", "파나케이아", "식선", "역발산", "화룡", "여래장", "하멜른", "후긴" };
        public static int Specialty(CommanderTrait t) => t >= CommanderTrait.GoldGathering && t <= CommanderTrait.DiamondInvestigation
            ? ((int)t - (int)CommanderTrait.GoldGathering) % diamonds.Length : -1;
        public static TraitGrade Grade(CommanderTrait t)
        {
            if (t >= CommanderTrait.DiamondGathering) return TraitGrade.Diamond;
            if (t >= CommanderTrait.GoldGathering) return TraitGrade.Gold;
            return t switch {
                CommanderTrait.Lazy or CommanderTrait.Relaxed or CommanderTrait.Depressive or CommanderTrait.Pessimist
                or CommanderTrait.Fragile or CommanderTrait.Sensitive or CommanderTrait.Sluggish or CommanderTrait.Slow
                or CommanderTrait.SlowLearner or CommanderTrait.Glutton or CommanderTrait.Frail or CommanderTrait.Greedy
                or CommanderTrait.ThinLegs or CommanderTrait.CannotBuild or CommanderTrait.CannotGather or CommanderTrait.CannotResearch
                or CommanderTrait.CannotNurse or CommanderTrait.CannotCook or CommanderTrait.Uncultured or CommanderTrait.Pacifist
                or CommanderTrait.KitchenMenace or CommanderTrait.SlowEater or CommanderTrait.Senile => TraitGrade.Red,
                _ => TraitGrade.Silver };
        }
        public static string Name(CommanderTrait t) => Specialty(t) < 0 ? t.ToString()
            : Grade(t) == TraitGrade.Diamond ? diamonds[Specialty(t)] : "전문성: " + (Specialty(t) < activities.Length ? StartingCandidate.SkillName(activities[Specialty(t)]) : "조사");
        public static int Count(float roll) => roll < .95f ? 1 : roll < .995f ? 2 : 3;
        public static TraitGrade RollGrade(float roll, bool starting)
        {
            var value = roll * (starting ? 99f : 100f);
            return value < 25 ? TraitGrade.Red : value < 85 ? TraitGrade.Silver : starting || value < 99 ? TraitGrade.Gold : TraitGrade.Diamond;
        }
        public static List<CommanderTrait> Candidates(CommanderTraits traits, TraitGrade grade, bool starting) => ((CommanderTrait[])Enum.GetValues(typeof(CommanderTrait)))
            .Where(t => t != CommanderTrait.Senile && Grade(t) == grade && (!starting || grade != TraitGrade.Diamond)
                && new CommanderTraits { values = new List<CommanderTrait>(traits.values) }.TryAdd(t)).ToList();
        private static float SpecialtyBonus(CommanderTraits traits, int specialty) => specialty < 0 ? 0 : traits.values.Where(t => Specialty(t) == specialty)
            .Select(t => Grade(t) == TraitGrade.Diamond ? .3f : .2f).DefaultIfEmpty(0).Max();
        public static float Bonus(CommanderTraits traits, CommanderActivity activity) => SpecialtyBonus(traits, Array.IndexOf(activities, activity));
        public static float WorkBonus(CommanderTraits traits, CommanderActivity activity, bool equipment)
            => activity == CommanderActivity.Strength || activity == CommanderActivity.Husbandry || activity == CommanderActivity.Crafting && !equipment ? 0 : Bonus(traits, activity);
        public static float InvestigationMultiplier(CommanderTraits traits) => 1f - SpecialtyBonus(traits, 12);
        public static float FishingWorkFactor(CommanderAnt c) => (c.Traits.WorkMultiplier + Bonus(c.Traits, CommanderActivity.Fishing)) / c.Traits.WorkMultiplier;
        public static float YieldMultiplier(WorkerAnt worker, ResourceNode node)
        {
            if (!(worker is CommanderAnt c) || node.IsLooseCargo || node.RequiresResourceResearch && CampaignResearch.Instance?.ResourceResearched(node.ResourceType) != true) return 1;
            return 1f + Bonus(c.Traits, node.RequiresFishing ? CommanderActivity.Fishing : node.GetComponent<AntColony.Buildings.BuildingBase>() != null ? CommanderActivity.Farming : CommanderActivity.Gathering);
        }
    }
}
