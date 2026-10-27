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
    // 공방과 같은 생산 목록(지정 수량 / 재고 유지 / 계속 생산)을 쓰고, 작업표 '제작'(약제대는 '간호(의료)')이 허용된 장수가 와서 작업한다.
    // 한 번에 한 묶음만 진행하며 재료는 묶음을 시작할 때 차감한다(목록 삭제는 진행 중인 묶음을 취소하지 않는다).
    public sealed partial class Processor : BuildingBase, IFreshMaterialSink
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
            BuildingKind.GasSmelter => IndustrialRecipes.GasSmelting(),
            BuildingKind.ChemistryBench => IndustrialRecipes.Chemistry(),
            BuildingKind.Carpentry => new[] { new Recipe("판재", ResourceType.Plank, 1, 8, CommanderActivity.Strength, (ResourceType.Wood, 2)) },
            BuildingKind.Stonecutter => new[] { new Recipe("석재 블록", ResourceType.StoneBlock, 1, 10, CommanderActivity.Strength, (ResourceType.Stone, 2)) },
            BuildingKind.Kiln => new[] { new Recipe("벽돌", ResourceType.Brick, 1, 8, null, (ResourceType.Clay, 2)),
                new Recipe("유리", ResourceType.Glass, 1, 12, null, (ResourceType.Sand, 2), (ResourceType.Coal, 1)) },
            BuildingKind.SpinningWheel => new[] { new Recipe("직물(섬유)", ResourceType.Cloth, 1, 10, null, (ResourceType.Fiber, 3)),
                new Recipe("직물(실)", ResourceType.Cloth, 1, 8, null, (ResourceType.Thread, 2)), new Recipe("직물(거미줄)", ResourceType.Cloth, 1, 8, null, (ResourceType.Cobweb, 2)) },
            BuildingKind.Smelter => new[] { new Recipe("철", ResourceType.Iron, 1, 14, CommanderActivity.Strength, (ResourceType.IronOre, 2), (ResourceType.Coal, 1)),
                new Recipe("구리", ResourceType.Copper, 1, 14, CommanderActivity.Strength, (ResourceType.CopperOre, 2), (ResourceType.Coal, 1)),
                new Recipe("강철", ResourceType.Steel, 1, 20, CommanderActivity.Strength, (ResourceType.Iron, 2), (ResourceType.Coal, 1)) }.Concat(SupplyRecipes.For(kind)).ToArray(),
            // 약제대(2026-10-11): 동물용 의약품·동물 수술 키트(장수용 약과 별도 자원, 재료는 잠정). 의료 능력만 사용(보조 기술 없음).
            BuildingKind.MedicineBench => new[] { new Recipe("동물용 의약품", ResourceType.AnimalMedicine, 1, 8, null, (ResourceType.Leaf, 2), (ResourceType.Honeydew, 1)),
                new Recipe("동물 수술 키트", ResourceType.SurgeryKit, 1, 14, null, (ResourceType.Cloth, 1), (ResourceType.Cobweb, 2), (ResourceType.Chitin, 1)),
                new Recipe("기초 약품", ResourceType.MedicineBasic, 1, 8, null, (ResourceType.Herb, 2)),
                new Recipe("일반 약품", ResourceType.MedicineNormal, 1, 12, null, (ResourceType.Herb, 2), (ResourceType.Cloth, 1)),
                new Recipe("고급 약품", ResourceType.MedicineAdvanced, 1, 20, null, (ResourceType.MedicineNormal, 1), (ResourceType.Herb, 2), (ResourceType.Component, 1)) },
            _ => Array.Empty<Recipe>()
        };
        public static bool IsKind(BuildingKind kind) => RecipesFor(kind).Length > 0;
        // 약제대는 간호(의료) 작업·의료 능력, 나머지 가공대는 제작 작업·제작 능력(2026-10-11 사용자 확정: 간호 작업 = 기획의 의료 작업).
        public bool IsMedical => Data != null && Data.kind == BuildingKind.MedicineBench;
        public CommanderJobs Job => IsMedical ? CommanderJobs.Nursing : CommanderJobs.Crafting;
        public CommanderActivity Skill => IsMedical ? CommanderActivity.Medicine : CommanderActivity.Crafting;

        [Serializable] public class Order { public int recipe; public Workshop.OrderMode mode; public int amount = 1; }
        [Serializable] public class State { public List<Order> orders = new List<Order>(); public int current = -1, pending = -1; public float progress; public string makerId = ""; public bool ruined, paused, gasPaid; public int bufferedOil; public List<ProductionInputs.Stock> staged = new List<ProductionInputs.Stock>(); }
        private State state = new State();
        private readonly ProductionInputs inputs = new ProductionInputs();
        public bool Paused { get => state.paused; set { state.paused = value; if (value) ReleaseWorkers(); } }
        private Order PendingOrder => Current != null ? null : state.orders.FirstOrDefault(o => o.recipe >= 0 && o.recipe < Recipes.Count
            && Wants(o, Recipes[o.recipe]) && (state.pending >= 0 ? o.recipe == state.pending : Affordable(Recipes[o.recipe])));
        private System.Collections.Generic.IEnumerable<(ResourceType type, int amount)> Required => PendingOrder is Order o ? Recipes[o.recipe].inputs : Array.Empty<(ResourceType, int)>();
        public bool NextPickup(out ResourceType type, out int amount) { type = default; amount = 0; return !Paused && !IsDead && isActiveAndEnabled && inputs.Next(Required, out type, out amount); }
        public bool BeginPickup(object courier, ResourceType type, int amount)
        {
            var order = PendingOrder;
            if (Paused || IsDead || order == null || !inputs.Reserve(courier, type, amount, Recipes[order.recipe].inputs)) return false;
            state.pending = order.recipe; return true;
        }
        public int Deliver(object courier, ResourceType type, int amount) => IsDead ? 0 : inputs.Deliver(courier, type, amount, Required);
        public int DeliverFood(object courier, ResourceType type, int amount, FoodLots lots) => IsDead ? 0 : inputs.Deliver(courier, type, amount, Required, lots);
        public void AgeIngredients(float seconds) => inputs.Age(seconds, Position);
        public void AbortPickup(object courier) => inputs.Abort(courier);
        public bool NeedsIngredients => NextPickup(out _, out _);
        public IReadOnlyList<Recipe> Recipes => RecipesFor(Data != null ? Data.kind : default);
        public List<Order> Orders => state.orders;
        public Recipe Current => state.current >= 0 && state.current < Recipes.Count ? Recipes[state.current] : null;
        public float Progress => Current == null ? 0 : state.progress / Current.work;
        public string MakerId => state.makerId;
        public bool CanContinue(CommanderAnt c) => c != null && (Current == null || string.IsNullOrEmpty(state.makerId) || state.makerId == c.PersonalState.id);
        public string WaitReason
        {
            get
            {
                if (state.ruined || !isActiveAndEnabled || IsDead) return "도구: 제작대 사용 불가 · 미완성품 보관/취소";
                if (Paused) return "일시중지";
                if (Industrial && !IndustrialReady) return IndustrialStatus;
                if (NeedsIngredients) return "재료 배송 대기";
                if (!NeedsWork) return "재료 또는 주문 대기";
                var crew = AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.CivilianWorkReady && CanContinue(c)).ToArray();
                if (crew.Any(c => c.ServiceTarget == this)) return crew.Any(c => c.ServiceTarget == this && (c.Position - Position).sqrMagnitude <= 49) ? "가공 중" : "작업 지점으로 이동";
                if (crew.Length == 0) return "담당 없음 · 원제작자 미복귀 시 보관/취소";
                var allowed = crew.Where(c => c.AllowsJob(Job)).ToArray();
                if (allowed.Length == 0) return "금지: " + Job;
                return allowed.Any(c => c.TryWorkApproach(Position, out _)) ? "담당 배정 대기" : "경로 없음";
            }
        }

        public bool AddOrder(int recipe, Workshop.OrderMode mode, int amount)
        {
            if (state.orders.Count >= Workshop.MaxOrders || recipe < 0 || recipe >= Recipes.Count || !MedicineResearch.Unlocked(Recipes[recipe].output)) return false;
            state.orders.Add(new Order { recipe = recipe, mode = mode, amount = Mathf.Clamp(amount, 1, Workshop.MaxOrderAmount) }); return true;
        }
        public void RemoveOrder(Order order)
        {
            if (!state.orders.Remove(order)) return;
            if (Current == null && state.pending == order.recipe && !state.orders.Any(o => o.recipe == state.pending))
            { inputs.Drop(Position); state.pending = -1; }
        }
        private Recipe PlannedOutput => Current ?? (state.pending >= 0 && state.pending < Recipes.Count ? Recipes[state.pending] : null);
        private bool Wants(Order o, Recipe r) => o.mode == Workshop.OrderMode.KeepStock ? Kitchen.Stock(r.output)
            + SceneObjects.All<Processor>().Where(p => p != this && p.PlannedOutput != null && p.PlannedOutput.output == r.output).Sum(p => p.PlannedOutput.amount) < o.amount : o.amount > 0;
        private bool Affordable(Recipe r) => MedicineResearch.Unlocked(r.output) && r.inputs.All(i => (ResourceManager.Instance?.GetAmount(i.type) ?? 0) + inputs.Amount(i.type) >= i.amount);
        public bool NeedsWork => !Paused && !state.ruined && !IsDead && isActiveAndEnabled && ResourceManager.Instance != null
            && IndustrialReady && (Current != null && MedicineResearch.Unlocked(Current.output) || PendingOrder != null && inputs.Ready(Required));

        // 목록 위에서부터 원하는 품목 중 재료가 있는 것 하나를 시작한다.
        private bool StartNext()
        {
            foreach (var o in state.orders.ToArray())
            {
                if (o.recipe >= Recipes.Count) continue;
                var r = Recipes[o.recipe];
                if (!Wants(o, r) || !Affordable(r)) continue;
                if (!inputs.Ready(r.inputs) || Industrial && !HasIndustrialSpace(r)) continue;
                if (!PayIndustrialFuel()) continue;
                if (!inputs.Consume(r.inputs)) continue;
                state.gasPaid = Industrial;
                state.current = o.recipe; state.pending = -1; state.progress = 0;
                if (o.mode == Workshop.OrderMode.Count && --o.amount <= 0) state.orders.Remove(o);
                return true;
            }
            return false;
        }

        public bool Work(CommanderAnt c, float seconds)
        {
            if (Chemical && (c == null || c.ServiceTarget != this || !c.AtDrill(this) || (c.Position - Position).sqrMagnitude > 49
                || c.IsEating || c.IsWashing || c.IsPlaying || c.IsInCombat)) return false;
            if (Industrial && UnityEngine.Time.timeScale <= 0 || !NeedsWork || !(seconds > 0) || float.IsInfinity(seconds) || !CanContinue(c) || !c.CivilianWorkReady || !c.AllowsJob(Job)
                || Current != null && !MedicineResearch.Unlocked(Current.output)) return false;
            if (Current == null && !StartNext()) return false;
            var r = Current;
            state.makerId = c.PersonalState.id;
            state.progress += c.WorkRate(Skill) * seconds; // 능력은 속도만 보정(산출·품질 동일)
            c.GainExperience(Skill, seconds, IsMedical ? Skill : r.support); // 약제대 생산은 의료만(보조 기술 연결 없음)
            if (state.progress < r.work) return true;
            state.bufferedOil = 0;
            state.current = -1; state.progress = 0; state.makerId = ""; state.gasPaid = false;
            AntColony.World.DiplomacyManager.DropFloor(r.output, r.amount, Position + ResourceManager.Scatter());
            return true;
        }

        // 미완성품 취소: 소비 전은 전량, 작업한 일반 재료는 70%를 실물로 반환한다.
        public void CancelCurrent()
        {
            if (Chemical && Current != null) state.bufferedOil = Mathf.FloorToInt(state.bufferedOil * (state.progress > 0 ? .7f : 1));
            inputs.Drop(Position); state.pending = -1;
            if (Current != null) foreach (var input in Current.inputs)
                AntColony.World.DiplomacyManager.DropFloor(input.type, input.type == AntColony.Data.ResourceType.Special || input.type == AntColony.Data.ResourceType.Component ? input.amount : Mathf.FloorToInt(input.amount * (state.progress > 0 ? .7f : 1)), Position + ResourceManager.Scatter());
            state.current = -1; state.progress = 0; state.makerId = ""; state.gasPaid = false;
            ReleaseWorkers();
        }

        private void ReleaseWorkers()
        {
            foreach (var c in AntUnitBase.Active.OfType<CommanderAnt>().Where(c => c.ServiceTarget == this).ToArray()) c.CommandStop();
        }
        protected override void OnDisable() { ReleaseWorkers(); PowerGrid.Unregister(this); Powered = false; base.OnDisable(); }
        protected override void Die()
        {
            // 공방처럼 미완성품을 잔해에 남겨 원제작자가 없어도 보관/취소할 수 있게 한다.
            state.ruined = true; enabled = false;
        }

        internal State CaptureState() { state.staged = inputs.staged; return JsonUtility.FromJson<State>(JsonUtility.ToJson(state)); }
        internal void RestoreState(State s)
        {
            state = s ?? new State();
            inputs.Restore(state.staged);
            state.orders ??= new List<Order>();
            state.orders.RemoveAll(o => o == null || o.recipe < 0 || o.recipe >= Recipes.Count || o.amount < 1 || o.amount > Workshop.MaxOrderAmount || !Enum.IsDefined(typeof(Workshop.OrderMode), o.mode));
            if (state.orders.Count > Workshop.MaxOrders) state.orders.RemoveRange(Workshop.MaxOrders, state.orders.Count - Workshop.MaxOrders);
            if (state.current >= Recipes.Count || !(state.progress >= 0) || float.IsInfinity(state.progress)) { state.current = -1; state.progress = 0; }
            enabled = !state.ruined;
        }
    }
}
