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
        // 일반 특성 한글 이름(화면에 영어 enum 이름이 그대로 나오던 문제, 2026-10-25).
        private static readonly System.Collections.Generic.Dictionary<CommanderTrait, string> Korean = new System.Collections.Generic.Dictionary<CommanderTrait, string> {
            [CommanderTrait.Lazy] = "게으름", [CommanderTrait.Relaxed] = "느긋함", [CommanderTrait.Industrious] = "부지런함", [CommanderTrait.Workaholic] = "일중독",
            [CommanderTrait.Depressive] = "우울", [CommanderTrait.Pessimist] = "비관", [CommanderTrait.Optimist] = "낙관", [CommanderTrait.Cheerful] = "명랑",
            [CommanderTrait.Fragile] = "유리 멘탈", [CommanderTrait.Sensitive] = "예민", [CommanderTrait.Easygoing] = "태평", [CommanderTrait.IronWill] = "강철 의지",
            [CommanderTrait.Coward] = "겁쟁이", [CommanderTrait.Cautious] = "신중", [CommanderTrait.Brave] = "용감", [CommanderTrait.Reckless] = "무모",
            [CommanderTrait.Sluggish] = "굼뜸", [CommanderTrait.Slow] = "느림", [CommanderTrait.Nimble] = "날렵", [CommanderTrait.Swift] = "신속",
            [CommanderTrait.SlowLearner] = "느린 학습", [CommanderTrait.Genius] = "천재", [CommanderTrait.LightEater] = "소식가", [CommanderTrait.Glutton] = "대식가",
            [CommanderTrait.Loyal] = "충성", [CommanderTrait.Ambitious] = "야심", [CommanderTrait.Cunning] = "교활", [CommanderTrait.Sociable] = "사교적",
            [CommanderTrait.Loner] = "외톨이", [CommanderTrait.ColdBlooded] = "냉혈", [CommanderTrait.Bloodthirsty] = "피에 굶주림", [CommanderTrait.Ascetic] = "금욕",
            [CommanderTrait.Wanderer] = "방랑자", [CommanderTrait.Homebody] = "집순이", [CommanderTrait.Robust] = "튼튼함", [CommanderTrait.Frail] = "허약", [CommanderTrait.Greedy] = "탐욕",
            [CommanderTrait.Nocturnal] = "야행성", [CommanderTrait.Muscular] = "왕근육", [CommanderTrait.Mighty] = "괴력", [CommanderTrait.ThinLegs] = "가는 다리",
            [CommanderTrait.CannotBuild] = "건설 불가", [CommanderTrait.CannotGather] = "채집 불가", [CommanderTrait.CannotResearch] = "연구 불가", [CommanderTrait.CannotNurse] = "간호 불가",
            [CommanderTrait.CannotCook] = "요리 불가", [CommanderTrait.Uncultured] = "무교양", [CommanderTrait.Pacifist] = "평화주의", [CommanderTrait.Chef] = "요리사",
            [CommanderTrait.Gourmet] = "미식가", [CommanderTrait.DullTaste] = "무딘 미각", [CommanderTrait.KitchenMenace] = "주방 테러범", [CommanderTrait.IronStomach] = "강철 위장", [CommanderTrait.SlowEater] = "느긋한 식사",
        };
        public static string Name(CommanderTrait t) => Specialty(t) < 0 ? t == CommanderTrait.Legion ? "레기온" : Korean.TryGetValue(t, out var ko) ? ko : t.ToString()
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
