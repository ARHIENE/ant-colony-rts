using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Core
{
    public static class SpecialtyAcquisition
    {
        // 사용자 승인 잠정값. 현재 enum에 존재하는 비전투 전문성13계열만 연결한다.
        public const int GoldLevel = 10, DiamondLevel = 20;
        public const float GoldChance = .2f, DiamondChance = .05f;
        internal static Func<float> Roll = () => UnityEngine.Random.value;
        public static readonly CommanderActivity[] Activities = { CommanderActivity.Gathering, CommanderActivity.Building,
            CommanderActivity.Farming, CommanderActivity.Fishing, CommanderActivity.Crafting, CommanderActivity.Research,
            CommanderActivity.Medicine, CommanderActivity.Cooking, CommanderActivity.Strength, CommanderActivity.Art,
            CommanderActivity.Politics, CommanderActivity.Husbandry, CommanderActivity.Investigation };
        public static void Initialize(CommanderAnt c)
        {
            var s = c.Traits.specialties ??= new SpecialtyProgress();
            if (s.version != 0) return;
            s.innate = ((CommanderTrait[])Enum.GetValues(typeof(CommanderTrait))).Where(c.Traits.Has).ToList();
            for (var i = 0; i < Activities.Length; i++)
            {
                if (c.Talents.Level(Activities[i]) >= GoldLevel) s.results[i * 2] = 1;
                if (c.Talents.Level(Activities[i]) >= DiamondLevel) s.results[i * 2 + 1] = 1;
            }
            s.version = 1;
        }
        public static void Observe(CommanderAnt c)
        {
            var s = c.Traits.specialties;
            List<int> due = null;
            for (var i = 0; i < Activities.Length; i++)
                for (var grade = 0; grade < 2; grade++)
                    if (s.results[i * 2 + grade] == 0 && c.Talents.Level(Activities[i]) >= (grade == 0 ? GoldLevel : DiamondLevel))
                    { due ??= new List<int>(); due.Add(i * 2 + grade); }
            if (due == null) return;
            // 동시 후보는 기본 기술이 높은 순. 같은 계열은 상위부터, 다른 계열 동률만 무작위다.
            foreach (var slot in due.OrderByDescending(i => c.Talents.Level(Activities[i / 2])).ThenByDescending(i => i % 2).ThenBy(_ => UnityEngine.Random.value))
            {
                var family = slot / 2; var diamond = slot % 2 == 1;
                var trait = (CommanderTrait)((int)(diamond ? CommanderTrait.DiamondGathering : CommanderTrait.GoldGathering) + family);
                var oldIndex = c.Traits.values.FindIndex(t => TraitRules.Specialty(t) == family);
                var old = oldIndex >= 0 ? c.Traits.values[oldIndex] : (CommanderTrait?)null;
                int result;
                if (old.HasValue && TraitRules.Grade(old.Value) >= TraitRules.Grade(trait)) result = 5;
                else if (!old.HasValue && c.Traits.values.Count >= CommanderTraits.MaxTraits) result = 3;
                else if (Roll() >= (diamond ? DiamondChance : GoldChance)) result = 2;
                else
                {
                    if (old.HasValue) c.Traits.values.RemoveAt(oldIndex);
                    var added = c.Traits.TryAdd(trait);
                    if (!added && old.HasValue) c.Traits.values.Insert(oldIndex, old.Value);
                    result = added ? 4 : 3;
                }
                s.results[slot] = result;
                CampaignHistory.Record("전문성", c.CommanderName, TraitRules.Name(trait) + " · " + ResultName(result), true);
            }
        }
        private static string ResultName(int result) => result switch
        {
            1 => "시작/이전 기술 도달 · 소급 추첨 없음", 2 => "확률 실패 · 평생 기회 소모",
            3 => "특성 빈칸/조건 부족 · 평생 기회 소모", 4 => "획득 · 영구 유지",
            5 => "동급 이상 보유 · 추가 획득 없음", _ => "최초 도달 대기"
        };
        public static string Description(CommanderAnt c, CommanderActivity activity)
        {
            var family = Array.IndexOf(Activities, activity);
            if (family < 0) return "이 기술의 후천 전문성 계열은 아직 연결되지 않았습니다.";
            var state = c.Traits.specialties;
            string Status(int grade)
            {
                var level = grade == 0 ? GoldLevel : DiamondLevel;
                var result = state?.results[family * 2 + grade] ?? 0;
                if ((state == null || state.version == 0) && c.Talents.Level(activity) >= level) result = 1;
                return ResultName(result);
            }
            return $"후천 전문성 · 계열별 평생 1회(잠정)\n기술10 최초 골드20%: {Status(0)}\n기술20 최초 다이아5%: {Status(1)}\n4칸 만석이면 새 계열 기회도 소모합니다. 같은 계열 상위는 기존 슬롯에서 교체합니다.\n기술 하락/재도달·저장/불러오기로 재추첨하지 않습니다.";
        }
    }
}
