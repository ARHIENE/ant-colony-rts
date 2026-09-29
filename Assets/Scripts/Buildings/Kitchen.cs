using System;
using System.Collections.Generic;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 요리 작업으로 식사를 비축하고, 배고픈 장수가 와서 재고를 먹는다(CommanderMeal).
    public sealed class Kitchen : BuildingBase
    {
        [Serializable] public class Meal { public int quality, skill; }
        [Serializable] public class State { public List<Meal> meals = new List<Meal>(); public float progress; public bool paid; }
        public State Meals { get; internal set; } = new State();
        public const int Capacity = 30;
        // ponytail: 식당 비용·조리 10초는 잠정 밸런스. 확정 때 여기만 조정한다.
        public const float CookSeconds = 10;
        public bool NeedsCook => !IsDead && isActiveAndEnabled && Meals.meals.Count < Capacity
            && (Meals.paid || ResourceManager.Instance != null && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) >= GameBalance.MealFood);
        public bool Work(CommanderAnt c, float seconds)
        {
            if (!NeedsCook || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            if (!Meals.paid)
            {
                if (!ResourceManager.Instance.TrySpend((int)GameBalance.MealFood, 0, reason: ResourceReason.Crafting)) return false;
                Meals.paid = true;
            }
            var rate = c.WorkRate(CommanderActivity.Cooking);
            var elapsed = Mathf.Min(seconds, (CookSeconds - Meals.progress) / rate);
            Meals.progress += elapsed * rate; c.GainExperience(CommanderActivity.Cooking, elapsed);
            if (Meals.progress >= CookSeconds)
            {
                int skill = Mathf.Clamp(c.Talents.Level(CommanderActivity.Cooking) + c.Traits.CookingBonus, 0, CommanderTalents.MaxLevel);
                float roll = UnityEngine.Random.value;
                Meals.meals.Add(new Meal { skill = skill, quality = c.Traits.Has(CommanderTrait.Chef) || roll < skill * .015f ? 3 : roll < skill * .045f ? 2 : 1 });
                Meals.progress = 0; Meals.paid = false;
            }
            return true;
        }
        public bool HasMeal => !IsDead && isActiveAndEnabled && Meals.meals.Count > 0;
        // 가장 좋은 식사부터 꺼낸다.
        public Meal TakeMeal()
        {
            if (!HasMeal) return null;
            var best = Meals.meals[0];
            foreach (var m in Meals.meals) if (m.quality > best.quality) best = m;
            Meals.meals.Remove(best); return best;
        }
        public static bool Valid(State state) => state != null && state.meals != null && state.meals.Count <= Capacity
            && !float.IsNaN(state.progress) && state.progress >= 0 && state.progress < CookSeconds && (state.paid || state.progress == 0)
            && state.meals.TrueForAll(m => m != null && m.quality >= 1 && m.quality <= 3 && m.skill >= 0 && m.skill <= CommanderTalents.MaxLevel);
    }
}
