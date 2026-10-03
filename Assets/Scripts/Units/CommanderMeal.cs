using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    // 식사(2026-09-28): 포만이 떨어지면 일을 멈추고 식당 재고를 먹는다. 식사가 없으면 창고에서 날로 먹는다.
    [Serializable] public class CommanderMealState
    {
        public float satiety = 100, eatSeconds, poisonSeconds, retrySeconds;
        public int quality, cookSkill; // 먹는 중인 식사: 0 날것 / 1 간단 / 2 좋은 / 3 고급
        public bool Valid => Finite(satiety) && satiety >= 0 && satiety <= 100 && Finite(eatSeconds) && eatSeconds >= 0
            && Finite(poisonSeconds) && poisonSeconds >= 0 && Finite(retrySeconds) && retrySeconds >= 0
            && quality >= 0 && quality <= 3 && cookSkill >= 0 && cookSkill <= CommanderTalents.MaxLevel;
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public partial class CommanderAnt
    {
        private static readonly string[] MealNames = { "날것", "간단한 식사", "좋은 식사", "고급 식사" };
        private CommanderMealState MealState => PersonalState.meal;
        public float Satiety => MealState.satiety;
        public bool IsEating => MealState.eatSeconds > 0;
        public bool IsFoodPoisoned => MealState.poisonSeconds > 0;

        // TickDuty에서 수면 다음에 부른다. true면 이번 틱은 식사(또는 식중독)로 끝낸다.
        private bool TickMeal(float seconds)
        {
            var m = MealState;
            m.satiety = Mathf.Max(0, m.satiety - GameBalance.SatietyPerSecond * seconds);
            if (m.poisonSeconds > 0) m.poisonSeconds = Mathf.Max(0, m.poisonSeconds - seconds);
            if (IsDeployed || IsAwayFromHome || IsEmbarked || IsCaptive) return false;
            if (m.poisonSeconds > 0)
            {
                if (IsWorking || ServiceTarget != null || HuntTarget != null || CorpseTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
                return true;
            }
            if (m.eatSeconds > 0)
            {
                m.eatSeconds = Mathf.Max(0, m.eatSeconds - seconds);
                if (m.eatSeconds == 0) FinishMeal();
                return true;
            }
            if (EatingCorpse) return false;
            if (m.satiety > GameBalance.EatBelowSatiety || IsCarrying || LabUpgradeBusy || IsPlaying) return false;
            if (m.retrySeconds > 0) { m.retrySeconds = Mathf.Max(0, m.retrySeconds - seconds); return false; }
            if (IsWorking || ServiceTarget != null || HuntTarget != null || CorpseTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
            if (!CanReceiveOrders) return false;
            if (traits.Has(CommanderTrait.Cannibal) && FindCorpseWork(true)) return true;

            // ponytail: 배고픈 동안 매 틱 식당을 다시 찾는다. 장수 수가 많아져 느려지면 목표를 캐시한다.
            var kitchen = FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Where(k => k.HasMeal)
                .OrderBy(k => (k.Position - Position).sqrMagnitude).FirstOrDefault();
            var place = kitchen != null ? kitchen : BuildingBase.FindNearestDepositPoint(Position);
            if (place != null && (place.Position - Position).sqrMagnitude > 49)
            {
                var moving = IsFlying ? !HasReachedDestination() : Agent.pathPending || Agent.hasPath;
                if (!moving && UnityEngine.AI.NavMesh.SamplePosition(place.Position, out var hit, 7, UnityEngine.AI.NavMesh.AllAreas) && CanReach(hit.position))
                { base.CommandMove(hit.position); return true; }
                if (moving) return true;
                if (kitchen != null) kitchen = null; // 닿지 못하면 그 자리에서 날로 먹는다.
            }
            StartMeal(kitchen);
            return true;
        }

        private void StartMeal(Kitchen kitchen)
        {
            var m = MealState;
            var meal = kitchen != null ? kitchen.TakeMeal() : null;
            var rm = ResourceManager.Instance;
            if (meal != null)
            {
                m.quality = meal.quality; m.cookSkill = meal.skill;
                // 대식은 한 끼 양 ×1.5: 조리된 몫 외에 모자란 Food를 더 쓴다(없으면 그냥 먹는다).
                var extra = Mathf.CeilToInt(GameBalance.MealFood * traits.FoodMultiplier - GameBalance.MealFood);
                if (extra > 0) rm?.TrySpend(extra, 0, reason: ResourceReason.Upkeep);
            }
            else if (rm != null && rm.TrySpend(Mathf.CeilToInt(GameBalance.MealFood * traits.FoodMultiplier), 0, reason: ResourceReason.Upkeep))
            { m.quality = 0; m.cookSkill = 0; }
            else
            {
                // 먹을 것이 없다: 굶주림. 잠시 뒤 다시 먹으러 간다.
                personalState.AddMood("Hunger", traits.Has(CommanderTrait.Ascetic) ? 5 : -15, GameBalance.StarveRetrySeconds + 1);
                OnHunger();
                m.retrySeconds = GameBalance.StarveRetrySeconds;
                return;
            }
            m.eatSeconds = GameBalance.EatSeconds * (traits.Has(CommanderTrait.SlowEater) ? 2 : 1);
        }

        private void FinishMeal()
        {
            var m = MealState;
            m.satiety = 100;
            int felt = m.quality == 0 ? 0 : Mathf.Clamp(m.quality + traits.TasteShift, 0, 3);
            var mood = GameBalance.MealMood[felt];
            if (mood < 0 && (traits.Has(CommanderTrait.IronStomach) || traits.Has(CommanderTrait.DullTaste) && m.quality == 0)) mood = 0;
            personalState.moodFactors.RemoveAll(f => f.reason.StartsWith("식사:"));
            if (mood != 0) personalState.AddMood("식사: " + MealNames[felt], mood, GameCalendar.SecondsPerDay / 3f);
            personalState.AddMood("배부름", traits.Has(CommanderTrait.Glutton) ? 10 : 5, GameCalendar.SecondsPerDay / 3f);
            // Phase 5: 식당 방에서 먹으면 등급만큼 기분 +.
            if (AntColony.Buildings.RoomSystem.RoomAt(Position) is AntColony.Buildings.Room hall && hall.Kind == AntColony.Buildings.RoomKind.Dining)
                personalState.AddMood("식당에서 식사", AntColony.Buildings.GameBalanceRooms.RoomMealMood + AntColony.Buildings.GameBalanceRooms.GradeMood[hall.Grade], GameCalendar.SecondsPerDay / 3f);
            if (m.quality > 0 && !traits.Has(CommanderTrait.IronStomach)
                && UnityEngine.Random.value * 100 < Mathf.Max(0, 6 - m.cookSkill * .5f))
            {
                m.poisonSeconds = GameBalance.FoodPoisonSeconds;
                personalState.AddMood("식중독", GameBalance.FoodPoisonMood, GameBalance.FoodPoisonSeconds);
            }
        }
    }
}
