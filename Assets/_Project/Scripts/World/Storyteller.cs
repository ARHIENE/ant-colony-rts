using System.Linq;
using AntColony.Core;
using UnityEngine;

namespace AntColony.World
{
    // 사건 감독(2026-11-01, RimWorld 카산드라식 긴장 곡선). 새 사건은 만들지 않고 기존 습격·사건의 시기와 위기/호재 비율만 조절한다.
    // 위태로움(부상자 절반 이상·식량 바닥) → 위기를 미루고 호재 쪽으로. 번성(식량 넉넉·부상 없음·4일 이후) → 위기를 앞당김.
    // 큰 위기(습격·재해) 직후 RestSeconds 동안은 새 위기를 내지 않는다(숨 쉴 틈). 상태는 저장하지 않는다(불러오면 휴식 없이 시작).
    // ponytail: 판정은 장수 체력·식량만 본다. 재산 등급·방어 시설까지 반영하려면 PlayerPerformanceProbe를 입력으로 확장.
    public static class Storyteller
    {
        public const float RestSeconds = 180, DistressRaidScale = 1.6f, ThrivingRaidScale = .8f, DistressCrisisScale = .35f, ThrivingCrisisBonus = .15f;
        public const int GraceDays = 1, ThrivingDay = 4, ThrivingFoodPerCommander = 30, DistressFoodPerCommander = 5;
        private static float lastCrisisAt = -9999;

        public static float SinceCrisis => GameCalendar.GameSeconds - lastCrisisAt;
        public static bool Resting => SinceCrisis < RestSeconds;
        public static void MarkCrisis() => lastCrisisAt = GameCalendar.GameSeconds;
        public static void Reset() => lastCrisisAt = -9999;

        public enum Mood { Normal, Distressed, Thriving }
        public static Mood Current
        {
            get
            {
                var home = CommanderRoster.Instance == null ? null : CommanderRoster.Instance.Commanders
                    .Where(c => c != null && !c.IsDead && c.IsColonyMember && !c.IsAwayFromHome && !c.IsCaptive).ToArray();
                if (home == null || home.Length == 0) return Mood.Normal;
                var hurt = home.Count(c => c.PersonalHealth <= 0 || c.Health.Downed || c.PersonalHealth < GameBalance.CommanderHealth * .5f);
                var food = ResourceManager.Instance != null ? ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) : 0;
                return Classify(home.Length, hurt, food, GameCalendar.Day);
            }
        }
        // 순수 판정(검사용).
        public static Mood Classify(int commanders, int hurt, float food, int day)
        {
            if (commanders <= 0) return Mood.Normal;
            if (hurt * 2 >= commanders || food < commanders * DistressFoodPerCommander && day >= 1) return Mood.Distressed;
            if (hurt == 0 && food >= commanders * ThrivingFoodPerCommander && day >= ThrivingDay) return Mood.Thriving;
            return Mood.Normal;
        }

        public static float RaidDelayScale(Mood mood) => mood == Mood.Distressed ? DistressRaidScale : mood == Mood.Thriving ? ThrivingRaidScale : 1f;
        public static float CrisisChance(float baseChance, Mood mood) => mood == Mood.Distressed ? baseChance * DistressCrisisScale
            : mood == Mood.Thriving ? Mathf.Min(1, baseChance + ThrivingCrisisBonus) : baseChance;
    }
}
