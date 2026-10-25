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
        public int pickedFood, pickedType; public bool served; // 집은 식품은 식탁 이동·저장을 넘어 보존한다.
        public int quality, cookSkill; // 먹는 중인 식사: 0 날것 / 1 간단 / 2 좋은 / 3 고급
        public bool rotten; // 정상 식품이 없어 썩은 식품을 먹는 중(2026-10-12)
        public int diet = -1; // 2026-10-15 장수별 식단 덮어쓰기(날것 1·간단 2·좋은 4). -1 = 공통 식단
        public bool Valid => Finite(satiety) && satiety >= 0 && satiety <= 100 && Finite(eatSeconds) && eatSeconds >= 0
            && Finite(poisonSeconds) && poisonSeconds >= 0 && Finite(retrySeconds) && retrySeconds >= 0
            && pickedFood >= 0 && pickedFood <= Mathf.CeilToInt(GameBalance.MealFood * 1.5f) && pickedType >= 0 && pickedType < StoreBin.TypeCount
            && (pickedFood == 0 || pickedType == (int)AntColony.Data.ResourceType.Food || pickedType == (int)AntColony.Data.ResourceType.RottenFood)
            && quality >= 0 && quality <= 3 && cookSkill >= 0 && cookSkill <= CommanderTalents.MaxLevel && diet >= -1 && diet <= CommanderAnt.AllDiet;
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    public partial class CommanderAnt
    {
        private static readonly string[] MealNames = { "날것", "간단한 식사", "좋은 식사", "고급 식사" };
        // 식단(2026-10-15): 공통 식단 + 장수별 덮어쓰기. 허용된 정상 식품만 먹고, 없으면 썩은 식품이 최후 선택이다.
        public const int DietRaw = 1, DietSimple = 2, DietFine = 4, AllDiet = 7;
        public static int CommonDiet = AllDiet;
        public int Diet => MealState.diet < 0 ? CommonDiet : MealState.diet;
        public bool DietAllows(int bit) => (Diet & bit) != 0;
        private CommanderMealState MealState => PersonalState.meal;
        public float Satiety => MealState.satiety;
        public bool IsEating => MealState.eatSeconds > 0;
        public bool IsFoodPoisoned => MealState.poisonSeconds > 0;
        public static float MealPoisonChance(int cookSkill) => Mathf.Max(0, 6 - cookSkill * .5f) / 100f;

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
            if (WorkPriorities.Red) { m.eatSeconds = 0; return false; } // 빨간 경보: 먹던 식사도 멈추고 작업한다.
            if (m.eatSeconds > 0)
            {
                m.eatSeconds = Mathf.Max(0, m.eatSeconds - seconds);
                if (m.eatSeconds == 0) FinishMeal();
                return true;
            }
            if (EatingCorpse) return false;
            if ((m.satiety > GameBalance.EatBelowSatiety && !m.served && m.pickedFood == 0) || IsCarrying || LabUpgradeBusy || IsPlaying) return false;
            if (m.retrySeconds > 0) { m.retrySeconds = Mathf.Max(0, m.retrySeconds - seconds); return false; }
            if (IsWorking || ServiceTarget != null || HuntTarget != null || CorpseTarget != null || ScienceAssignment != null || CraftingWorkshop != null) CommandStop();
            if (!CanReceiveOrders) return false;
            if (!m.served && m.pickedFood == 0 && traits.Has(CommanderTrait.Cannibal) && FindCorpseWork(true)) return true;

            if (!m.served && !FetchMeal()) return true;
            if (!m.served) return true;
            // 음식을 집은 뒤 접근 가능한 식탁으로 이동한다. 식탁이 없으면 그 자리에서 먹는다.
            var table = FindObjectsByType<Kitchen>(FindObjectsSortMode.None)
                .Where(k => !k.IsDead && k.isActiveAndEnabled && (k.IsTable || k.Data != null && k.Data.kind == AntColony.Data.BuildingKind.Kitchen)
                    && TryWorkApproach(k.Position, out _)).OrderBy(k => (k.Position - Position).sqrMagnitude).FirstOrDefault();
            if (table != null && MoveForMeal(table.Position)) return true;
            StopMoving(); m.served = false;
            m.eatSeconds = GameBalance.EatSeconds * (traits.Has(CommanderTrait.SlowEater) ? 2 : 1);
            return true;
        }

        private void FinishMeal()
        {
            var m = MealState;
            m.satiety = 100;
            int felt = m.quality == 0 ? 0 : Mathf.Clamp(m.quality + traits.TasteShift, 0, 3);
            var mood = GameBalance.MealMood[felt];
            if (mood < 0 && (traits.Has(CommanderTrait.IronStomach) || traits.Has(CommanderTrait.DullTaste) && m.quality == 0)) mood = 0;
            personalState.moodFactors.RemoveAll(f => f.reason.StartsWith("식사:"));
            if (mood != 0) personalState.AddMood("식사: " + MealNames[felt], mood, GameCalendar.SecondsPerDay / 2f);
            personalState.AddMood("배부름", traits.Has(CommanderTrait.Glutton) ? 10 : 5, GameCalendar.SecondsPerDay / 2f);
            // 식탁 없이 먹으면 기분 불이익(2026-10-12). ponytail: 식탁·식당 4m 안이면 식탁에서 먹은 것으로 본다.
            if (!FindObjectsByType<Kitchen>(FindObjectsSortMode.None).Any(k => !k.IsDead && k.isActiveAndEnabled && (k.IsTable || k.Data != null && k.Data.kind == AntColony.Data.BuildingKind.Kitchen) && (k.Position - Position).sqrMagnitude < 16))
                personalState.AddMood("식탁 없이 식사", GameBalance.NoTableMood, GameCalendar.SecondsPerDay / 2f);
            // Phase 5: 식당 방에서 먹으면 등급만큼 기분 +(식당·부엌 겸용은 절반).
            // 가구 6차: 연회장은 ×1.5.
            if (AntColony.Buildings.RoomSystem.RoomAt(Position) is AntColony.Buildings.Room hall && (hall.Kind == AntColony.Buildings.RoomKind.Dining || hall.Kind == AntColony.Buildings.RoomKind.DiningKitchen || hall.Kind == AntColony.Buildings.RoomKind.BanquetHall))
                personalState.AddMood("식당에서 식사", (AntColony.Buildings.GameBalanceRooms.RoomMealMood + AntColony.Buildings.GameBalanceRooms.GradeMood[hall.Grade])
                    * (hall.Kind == AntColony.Buildings.RoomKind.DiningKitchen ? .5f : hall.Kind == AntColony.Buildings.RoomKind.BanquetHall ? GameBalance.BanquetMealMultiplier : 1f), GameCalendar.SecondsPerDay / 2f);
            if (m.rotten)
            {
                personalState.moodFactors.RemoveAll(f => f.reason.StartsWith("식사:"));
                personalState.AddMood("식사: 썩은 식품", GameBalance.RottenMealMood, GameCalendar.SecondsPerDay / 2f);
                if (!traits.Has(CommanderTrait.IronStomach) && UnityEngine.Random.value < GameBalance.RottenPoisonChance) { m.poisonSeconds = GameBalance.FoodPoisonSeconds; personalState.AddMood("식중독", GameBalance.FoodPoisonMood, GameBalance.FoodPoisonSeconds); }
                m.rotten = false; return;
            }
            if (m.quality > 0 && !traits.Has(CommanderTrait.IronStomach)
                && UnityEngine.Random.value < MealPoisonChance(m.cookSkill))
            {
                m.poisonSeconds = GameBalance.FoodPoisonSeconds;
                personalState.AddMood("식중독", GameBalance.FoodPoisonMood, GameBalance.FoodPoisonSeconds);
            }
        }
    }
}
