using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    [Serializable]
    public sealed class StartingCandidate
    {
        public string name;
        public int ageYears, appearanceSeed;
        public CommanderTraits traits;
        public CommanderTalents talents;

        public static StartingCandidate Roll(int slot)
        {
            var t = CommanderTraits.Random(true);
            // 노망은 노화로 얻는 특성이다. 시작 후보는 다이아를 제외해 정규화한다.
            t.values.Remove(CommanderTrait.Senile);
            var skills = new CommanderTalents(); skills.Generate(t);
            return new StartingCandidate { name = "생존자 " + (slot + 1), ageYears = UnityEngine.Random.Range(18, 31),
                appearanceSeed = UnityEngine.Random.Range(1, int.MaxValue), traits = t, talents = skills };
        }

        public bool Valid => !string.IsNullOrWhiteSpace(name) && ageYears >= 18 && ageYears <= 30 && appearanceSeed > 0
            && traits != null && talents != null && talents.Validate() && traits.values.Count <= CommanderTraits.MaxTraits;

        public static readonly CommanderJobs[] Required = { CommanderJobs.Gathering, CommanderJobs.Hauling, CommanderJobs.Building,
            CommanderJobs.Crafting, CommanderJobs.Research, CommanderJobs.Nursing, CommanderJobs.Hunting };
        public static readonly string[] RequiredNames = { "채집", "운반", "건설", "제작", "연구", "의료", "전투" };
        public static string[] Missing(IEnumerable<StartingCandidate> team)
        {
            var all = (team ?? Array.Empty<StartingCandidate>()).Where(c => c != null && c.Valid).ToArray();
            return Required.Select((job, i) => all.Any(c => !c.traits.Blocks(job)) ? null : RequiredNames[i]).Where(n => n != null).ToArray();
        }
        public static string SkillName(CommanderActivity a) => a switch
        {
            CommanderActivity.Gathering => "채집", CommanderActivity.Building => "건설", CommanderActivity.Farming => "농사", CommanderActivity.Fishing => "낚시",
            CommanderActivity.Crafting => "제작", CommanderActivity.Research => "연구", CommanderActivity.Melee => "근접", CommanderActivity.Ranged => "원거리",
            CommanderActivity.Command => "지휘", CommanderActivity.Medicine => "의료", CommanderActivity.Cooking => "요리", CommanderActivity.Strength => "근력",
            CommanderActivity.Art => "예술", CommanderActivity.Politics => "정치", CommanderActivity.Husbandry => "사육", _ => "조사"
        };
        public string Blocked => string.Join(" · ", Required.Select((job, i) => traits.Blocks(job) ? RequiredNames[i] : null).Where(n => n != null));
        public static string Clothes(MapBiome biome) => Clothes(biome, GameSession.Exists ? GameSession.Instance.Options.climate : MapClimate.Temperate);
        public static string Clothes(MapBiome biome, MapClimate climate) => climate == MapClimate.Cold ? "평범한 보온 옷"
            : climate == MapClimate.TropicalWet ? "평범한 통풍 옷"
            : biome == MapBiome.Desert ? "평범한 통풍 옷" : biome == MapBiome.Cave ? "평범한 보온 옷" : "평범한 작업복";
        public static string AppearanceName(int seed) => new[] { "은빛 머리", "금빛 머리", "분홍 머리", "초록 머리", "연보라 머리" }[Math.Abs(seed % 5)]
            + " · " + new[] { "작은 체격", "보통 체격", "큰 체격" }[Math.Abs(seed % 3)];
        public static Color AppearanceColor(int seed) => new[] { new Color(.47f, .54f, .61f), new Color(.70f, .48f, .19f),
            new Color(.65f, .31f, .36f), new Color(.33f, .52f, .36f), new Color(.48f, .37f, .60f) }[Math.Abs(seed % 5)];
    }
}
