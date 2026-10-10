using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 가공대(2026-10-10 자원 분화): 원재료 → 가공품. 기본 2단계(원재료→가공품), 고급 3단계(광석→금속→강철).
    // 공방과 같은 생산 목록(지정 수량 / 재고 유지 / 계속 생산)을 쓰고, 작업표 '제작'이 허용된 장수가 와서 작업한다.
    // 한 번에 한 묶음만 진행하며 재료는 묶음을 시작할 때 차감한다(목록 삭제는 진행 중인 묶음을 취소하지 않는다).
    public sealed class Processor : BuildingBase
    {
        public sealed class Recipe
        {
            public readonly string name; public readonly (ResourceType type, int amount)[] inputs; public readonly ResourceType output; public readonly int amount; public readonly float work;
            public readonly CommanderActivity? support;
            public Recipe(string name, ResourceType output, int amount, float work, CommanderActivity? support, params (ResourceType, int)[] inputs)
            { this.name = name; this.output = output; this.amount = amount; this.work = work; this.support = support; this.inputs = inputs; }
            public string Cost => string.Join(" + ", inputs.Select(i => $"{i.type.DisplayName()} {i.amount}"));
        }

        // ponytail: 레시피·수량·작업량은 잠정(기획 미정). 중량 가공은 근력, 정밀 가공(유리·직물)은 보조 없음.
        public static Recipe[] RecipesFor(BuildingKind kind) => kind switch
        {
            BuildingKind.Carpentry => new[] { new Recipe("판재", ResourceType.Plank, 1, 8, CommanderActivity.Strength, (ResourceType.Wood, 2)) },
            BuildingKind.Stonecutter => new[] { new Recipe("석재 블록", ResourceType.StoneBlock, 1, 10, CommanderActivity.Strength, (ResourceType.Stone, 2)) },
            BuildingKind.Kiln => new[] { new Recipe("벽돌", ResourceType.Brick, 1, 8, null, (ResourceType.Clay, 2)),
                new Recipe("유리", ResourceType.Glass, 1, 12, null, (ResourceType.Sand, 2), (ResourceType.Coal, 1)) },
            BuildingKind.SpinningWheel => new[] { new Recipe("직물(섬유)", ResourceType.Cloth, 1, 10, null, (ResourceType.Fiber, 3)),
                new Recipe("직물(실)", ResourceType.Cloth, 1, 8, null, (ResourceType.Thread, 2)), new Recipe("직물(거미줄)", ResourceType.Cloth, 1, 8, null, (ResourceType.Cobweb, 2)) },
            BuildingKind.Smelter => new[] { new Recipe("철", ResourceType.Iron, 1, 14, CommanderActivity.Strength, (ResourceType.IronOre, 2), (ResourceType.Coal, 1)),
                new Recipe("구리", ResourceType.Copper, 1, 14, CommanderActivity.Strength, (ResourceType.CopperOre, 2), (ResourceType.Coal, 1)),
                new Recipe("강철", ResourceType.Steel, 1, 20, CommanderActivity.Strength, (ResourceType.Iron, 2), (ResourceType.Coal, 1)) },
            // 약제대(2026-10-11): 동물용 의약품·동물 수술 키트(장수용 약과 별도 자원, 재료는 잠정)
            BuildingKind.MedicineBench => new[] { new Recipe("동물용 의약품", ResourceType.AnimalMedicine, 1, 8, CommanderActivity.Medicine, (ResourceType.Leaf, 2), (ResourceType.Honeydew, 1)),
                new Recipe("동물 수술 키트", ResourceType.SurgeryKit, 1, 14, CommanderActivity.Medicine, (ResourceType.Cloth, 1), (ResourceType.Cobweb, 2), (ResourceType.Chitin, 1)) },
            _ => Array.Empty<Recipe>()
        };
        public static bool IsKind(BuildingKind kind) => RecipesFor(kind).Length > 0;

        [Serializable] public class Order { public int recipe; public Workshop.OrderMode mode; public int amount = 1; }
        [Serializable] public class State { public List<Order> orders = new List<Order>(); public int current = -1; public float progress; }
        private State state = new State();
        public IReadOnlyList<Recipe> Recipes => RecipesFor(Data != null ? Data.kind : default);
        public List<Order> Orders => state.orders;
        public Recipe Current => state.current >= 0 && state.current < Recipes.Count ? Recipes[state.current] : null;
        public float Progress => Current == null ? 0 : state.progress / Current.work;

        public bool AddOrder(int recipe, Workshop.OrderMode mode, int amount)
        {
            if (state.orders.Count >= Workshop.MaxOrders || recipe < 0 || recipe >= Recipes.Count) return false;
            state.orders.Add(new Order { recipe = recipe, mode = mode, amount = Mathf.Clamp(amount, 1, Workshop.MaxOrderAmount) }); return true;
        }
        private static bool Wants(Order o, Recipe r) => o.mode == Workshop.OrderMode.KeepStock ? ResourceManager.Instance.GetAmount(r.output) < o.amount : o.amount > 0;
        private static bool Affordable(Recipe r) => r.inputs.All(i => ResourceManager.Instance.GetAmount(i.type) >= i.amount);
        public bool NeedsWork => !IsDead && isActiveAndEnabled && ResourceManager.Instance != null
            && (Current != null || state.orders.Any(o => o.recipe < Recipes.Count && Wants(o, Recipes[o.recipe]) && Affordable(Recipes[o.recipe])));

        // 목록 위에서부터 원하는 품목 중 재료가 있는 것 하나를 시작한다.
        private bool StartNext()
        {
            foreach (var o in state.orders.ToArray())
            {
                if (o.recipe >= Recipes.Count) continue;
                var r = Recipes[o.recipe];
                if (!Wants(o, r) || !Affordable(r)) continue;
                foreach (var i in r.inputs) ResourceManager.Instance.TrySpend(i.type, i.amount, ResourceReason.Production);
                state.current = o.recipe; state.progress = 0;
                if (o.mode == Workshop.OrderMode.Count && --o.amount <= 0) state.orders.Remove(o);
                return true;
            }
            return false;
        }

        public bool Work(CommanderAnt c, float seconds)
        {
            if (!NeedsWork || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            if (Current == null && !StartNext()) return false;
            var r = Current;
            state.progress += c.WorkRate(CommanderActivity.Crafting) * seconds;
            c.GainExperience(CommanderActivity.Crafting, seconds, r.support);
            if (state.progress < r.work) return true;
            state.current = -1; state.progress = 0;
            AntColony.World.DiplomacyManager.StoreResource(r.output, r.amount, ResourceReason.Production); // 창고가 차면 바닥 더미
            return true;
        }

        // 철거 시 진행 중 묶음의 재료는 돌려준다(가공 전 재료라 손실 없음).
        internal void RefundCurrent()
        {
            var r = Current; if (r == null) return;
            foreach (var i in r.inputs) AntColony.World.DiplomacyManager.StoreResource(i.type, i.amount, ResourceReason.Refund);
            state.current = -1; state.progress = 0;
        }

        internal State CaptureState() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        internal void RestoreState(State s)
        {
            state = s ?? new State();
            state.orders ??= new List<Order>();
            state.orders.RemoveAll(o => o == null || o.recipe < 0 || o.recipe >= Recipes.Count || o.amount < 1 || o.amount > Workshop.MaxOrderAmount || !Enum.IsDefined(typeof(Workshop.OrderMode), o.mode));
            if (state.orders.Count > Workshop.MaxOrders) state.orders.RemoveRange(Workshop.MaxOrders, state.orders.Count - Workshop.MaxOrders);
            if (state.current >= Recipes.Count || !(state.progress >= 0) || float.IsInfinity(state.progress)) { state.current = -1; state.progress = 0; }
        }
    }
}
