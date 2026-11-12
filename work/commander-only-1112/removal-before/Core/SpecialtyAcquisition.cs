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
            ObserveCombat(c, s);
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
        // 전투 다이아(D21~D24·D04): 근접20 → 무간/참경/역린 중 하나, 사격20 → 엑스텐, 지휘20 → 레기온. 확률·평생1회·만석 규칙은 비전투와 같다.
        public static readonly CommanderActivity[] CombatActivities = { CommanderActivity.Melee, CommanderActivity.Ranged, CommanderActivity.Command };
        private static readonly CommanderTrait[] Melee = { CommanderTrait.DiamondDoubleStrike, CommanderTrait.DiamondArmorBreak, CommanderTrait.DiamondWrath };
        private static void ObserveCombat(CommanderAnt c, SpecialtyProgress s)
        {
            if (s.combat == null || s.combat.Length != CombatActivities.Length)
                s.combat = CombatActivities.Select(a => c.Talents.Level(a) >= DiamondLevel ? 1 : 0).ToArray();
            for (var i = 0; i < CombatActivities.Length; i++)
            {
                if (s.combat[i] != 0 || c.Talents.Level(CombatActivities[i]) < DiamondLevel) continue;
                int result;
                var trait = i == 1 ? CommanderTrait.DiamondPierce : i == 2 ? CommanderTrait.Legion : CommanderTrait.DiamondDoubleStrike;
                if (i == 0 && Melee.Any(c.Traits.Has)) result = 5;
                else if (c.Traits.Has(trait)) result = 5;
                else if (c.Traits.values.Count >= CommanderTraits.MaxTraits) result = 3;
                else if (Roll() >= DiamondChance) result = 2;
                else
                {
                    if (i == 0) trait = Melee[Math.Min(Melee.Length - 1, (int)(Roll() * Melee.Length))];
                    result = c.Traits.TryAdd(trait) ? 4 : 3;
                }
                s.combat[i] = result;
                CampaignHistory.Record("전문성", c.CommanderName, (i == 0 && result != 4 ? "무간/참경/역린" : TraitRules.Name(trait)) + " · " + ResultName(result), true);
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
            var combat = Array.IndexOf(CombatActivities, activity);
            if (combat >= 0)
            {
                var r = c.Traits.specialties?.combat?.Length == CombatActivities.Length ? c.Traits.specialties.combat[combat] : 0;
                return $"전투 다이아 · 평생 1회(잠정)\n기술20 최초 다이아5% ({(combat == 0 ? "무간·참경·역린 중 하나" : combat == 1 ? "엑스텐" : "레기온")}): {ResultName(r)}\n4칸 만석이면 기회도 소모합니다. 저장/불러오기로 재추첨하지 않습니다.";
            }
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
