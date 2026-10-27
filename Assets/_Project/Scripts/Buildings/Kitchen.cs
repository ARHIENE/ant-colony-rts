using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    // 요리(2026-10-12 기획): 생산 목록(지정 횟수·재고 유지·계속)으로 간단한/좋은 요리를 만든다. 등급은 레시피로만 정한다(기술에 따른 무작위 상승 없음).
    // 재료는 실제 조리 시작 때 소비하며 창고·운반·조리대 대기 중 신선도를 유지한다. 완성 요리는 시설 주변 바닥에서 운반된다.
    // 요리사가 떠나도 진행도는 남아 다른 요리사가 잇는다. 시작 전 취소는 손실 없음, 시작 후 취소는 소비한 재료를 돌려주지 않는다.
    public sealed partial class Kitchen : BuildingBase, IFreshMaterialSink
    {
        [Serializable] public class Meal { public int quality, skill; }
        [Serializable] public class Order { public int recipe; public Workshop.OrderMode mode; public int amount = 1; }
        // meals·paid는 옛 저장 호환(이전 비축 식사). 새 요리는 바닥 자원으로 나온다.
        [Serializable] public class State
        {
            public List<Meal> meals = new List<Meal>(); public float progress; public bool paid;
            public List<Order> orders = new List<Order>(); public int current = -1; public bool paused, configured;
            public int staged; // 2026-10-15 요리사가 가져다 놓은 재료(식량). 조리 시작 때 소비, 철거 시 바닥으로
            public FoodLots stagedLots;
        }
        public sealed class Recipe
        {
            public readonly string name; public readonly ResourceType output; public readonly int food; public readonly float seconds;
            public Recipe(string name, ResourceType output, int food, float seconds) { this.name = name; this.output = output; this.food = food; this.seconds = seconds; }
        }
        // ponytail: 레시피·재료·시간은 잠정(기획 미정). 좋은 요리는 재료·시간을 두 배로 쓴다. 썩은 식품은 쓰지 않는다.
        public static readonly Recipe[] Recipes =
        {
            new Recipe("간단한 요리", ResourceType.SimpleMeal, (int)GameBalance.MealFood, 10),
            new Recipe("좋은 요리", ResourceType.FineMeal, (int)GameBalance.MealFood * 2, 20),
        };
        public const int Capacity = 30;
        public const float CookSeconds = 20; // 가장 긴 레시피(저장 검증용)
        private State meals = new State();
        public State Meals { get => meals; internal set { meals = value ?? new State(); Normalize(); } }
        // 큰 식탁(가구 5차): 먹기 전용. 요리·비축은 하지 않고 장수가 와서 먹는 자리만 된다(CommanderMeal).
        public bool IsTable => Data != null && (Data.kind == BuildingKind.BigTable || Data.kind == BuildingKind.BanquetTable); // 연회용 긴 식탁도 먹기 전용
        public Recipe Current => meals.current >= 0 && meals.current < Recipes.Length ? Recipes[meals.current] : null;
        public float Progress => Current == null ? 0 : meals.progress / Current.seconds;
        public bool Paused { get => meals.paused; set => meals.paused = value; }
        public List<Order> Orders => meals.orders;

        protected override void OnEnable() { base.OnEnable(); Normalize(); }
        // 새 시설·옛 저장: 간단한 요리 재고 유지 10을 기본 주문으로 둔다(예전 자동 조리와 같은 흐름).
        private void Normalize()
        {
            meals.orders ??= new List<Order>(); meals.meals ??= new List<Meal>();
            if (!meals.configured && !IsTable) { meals.configured = true; if (meals.orders.Count == 0) meals.orders.Add(new Order { recipe = 0, mode = Workshop.OrderMode.KeepStock, amount = 10 }); }
            if (meals.paid && meals.current < 0) meals.current = 0; // 옛 저장: 값 치른 조리 = 간단한 요리 진행 중
            meals.paid = false;
            if (meals.current >= Recipes.Length || !(meals.progress >= 0)) { meals.current = -1; meals.progress = 0; }
        }
        public bool AddOrder(int recipe, Workshop.OrderMode mode, int amount)
        {
            if (IsTable || meals.orders.Count >= Workshop.MaxOrders || recipe < 0 || recipe >= Recipes.Length) return false;
            meals.orders.Add(new Order { recipe = recipe, mode = mode, amount = Mathf.Clamp(amount, 1, Workshop.MaxOrderAmount) }); return true;
        }
        // 창고 재고 + 아직 운반되지 않은 바닥 요리(재고 유지가 운반 대기 중에 과잉 생산하지 않게).
        public static int Stock(ResourceType t) => (ResourceManager.Instance?.GetAmount(t) ?? 0)
            + Mathf.FloorToInt(AntColony.World.ResourceNode.Available.Where(n => n != null && n.IsLooseCargo && n.ResourceType == t).Sum(n => n.AmountRemaining));
        private bool Wants(Order o) => o.mode == Workshop.OrderMode.KeepStock ? Stock(Recipes[o.recipe].output)
            + SceneObjects.All<Kitchen>().Count(k => k != this && k.PlannedOutput == Recipes[o.recipe].output) < o.amount : o.amount > 0;
        private bool Usable => !IsTable && !IsDead && isActiveAndEnabled;
        // 다음에 만들 주문(조리 중이 아닐 때). 재료는 조리대에 실제로 놓인 양(staged)으로만 시작한다.
        private Order NextOrder => meals.orders.FirstOrDefault(o => o.recipe < Recipes.Length && Wants(o));
        public bool NeedsCook => Usable && !meals.paused && (Current != null || NextOrder is Order o && meals.staged >= Recipes[o.recipe].food);
        // 아직 가져와야 할 재료(운반 중인 몫 제외). 창고에 없으면 0.
        private readonly Dictionary<object, int> inTransit = new Dictionary<object, int>();
        private int Missing => !Usable || meals.paused || Current != null || !(NextOrder is Order o) ? 0
            : Mathf.Max(0, Recipes[o.recipe].food - meals.staged - inTransit.Values.Sum());
        public bool NeedsIngredients => Missing > 0 && (ResourceManager.Instance?.GetAmount(ResourceType.Food) ?? 0) > 0;
        public int Staged => meals.staged;
        public bool NextPickup(out ResourceType type, out int amount) { type = ResourceType.Food; amount = NeedsIngredients ? Missing : 0; return amount > 0; }
        public bool BeginPickup(object courier, ResourceType type, int amount) { if (type != ResourceType.Food || amount <= 0 || !Usable) return false; inTransit[courier] = amount; return true; }
        public int Deliver(object courier, ResourceType type, int amount)
            => DeliverFood(courier, type, amount, null);
        public void AbortPickup(object courier) => inTransit.Remove(courier);
        // 철거·이동: 놓아둔 재료는 그대로 바닥에(손실 없음).
        internal void EjectStaged()
        {
            if (meals.staged > 0) AntColony.World.DiplomacyManager.DropFloor(ResourceType.Food, meals.staged, Position + Vector3.right * 1.5f, meals.stagedLots);
            meals.staged = 0; meals.stagedLots = null; inTransit.Clear();
        }
        // 기다리는 이유(작업 UI): 정지 / 재료 부족 / 재료 운반 대기 / 목표 달성.
        public string WaitReason => !Usable ? null : meals.paused ? "일시정지" : Current != null || NeedsCook ? null
            : meals.orders.Count == 0 ? "생산 목록 없음" : NextOrder == null ? "목표 달성" : NeedsIngredients || inTransit.Count > 0 ? "재료 운반 대기" : "재료 부족";

        private bool StartNext()
        {
            foreach (var o in meals.orders.ToArray())
            {
                if (o.recipe >= Recipes.Length) continue;
                var r = Recipes[o.recipe];
                if (!Wants(o)) continue;
                if (meals.staged < r.food) return false; // 첫 주문의 재료가 모일 때까지 기다린다
                meals.stagedLots?.Take(r.food); meals.staged -= r.food; CampaignHistory.Resource(ResourceType.Food, r.food, true, ResourceReason.Crafting);
                meals.current = o.recipe; meals.progress = 0;
                if (o.mode == Workshop.OrderMode.Count && --o.amount <= 0) meals.orders.Remove(o);
                return true;
            }
            return false;
        }
        public bool Work(CommanderAnt c, float seconds)
        {
            if (!NeedsCook || !(seconds > 0) || float.IsInfinity(seconds)) return false;
            if (Current == null && !StartNext()) return false;
            var r = Current; var rate = c.WorkRate(CommanderActivity.Cooking); // 요리 기술은 속도만(등급은 레시피)
            var elapsed = Mathf.Min(seconds, (r.seconds - meals.progress) / rate);
            meals.progress += elapsed * rate; c.GainExperience(CommanderActivity.Cooking, elapsed);
            if (meals.progress < r.seconds) return true;
            int skill = Mathf.Clamp(c.Talents.Level(CommanderActivity.Cooking) + c.Traits.CookingBonus, 0, CommanderTalents.MaxLevel);
            RecordSkill(r.output, skill);
            AntColony.World.DiplomacyManager.DropFloor(r.output, 1, Position + Vector3.right * 1.5f); // 새 보존 기간으로 바닥에(운반은 따로)
            meals.current = -1; meals.progress = 0;
            return true;
        }
        // 진행 중 조리 취소: 이미 소비한 재료는 돌려주지 않는다. 철거·이동 시작 때도 쓴다.
        public void CancelCurrent() { meals.current = -1; meals.progress = 0; }

        // 식중독 확률용 요리 기술. ponytail: 요리 종류별 최근 요리사 기술의 이동 평균. 개별 요리마다 기술을 들고 다니게 하려면 FoodLots에 붙인다.
        private static readonly float[] cookedSkill = { 5, 5 };
        private static void RecordSkill(ResourceType t, int skill) { var i = t == ResourceType.FineMeal ? 1 : 0; cookedSkill[i] = Mathf.Lerp(cookedSkill[i], skill, .3f); }
        public static int SkillOf(ResourceType t) => Mathf.RoundToInt(cookedSkill[t == ResourceType.FineMeal ? 1 : 0]);

        // 옛 저장의 비축 식사(새 요리는 창고 자원).
        public bool HasMeal => !IsDead && isActiveAndEnabled && meals.meals.Count > 0;
        public Meal BestMeal(int diet) => !HasMeal ? null : meals.meals.Where(m => (diet & (m.quality >= 2 ? CommanderAnt.DietFine : CommanderAnt.DietSimple)) != 0).OrderByDescending(m => m.quality).FirstOrDefault();
        public Meal TakeMeal(Meal choice) => HasMeal && meals.meals.Remove(choice) ? choice : null;
        public Meal TakeMeal()
        {
            if (!HasMeal) return null;
            var best = meals.meals[0];
            foreach (var m in meals.meals) if (m.quality > best.quality) best = m;
            meals.meals.Remove(best); return best;
        }
        public static bool Valid(State state) => state != null && state.meals != null && state.meals.Count <= Capacity
            && state.staged >= 0 && (state.stagedLots == null || state.stagedLots.Valid && state.stagedLots.Total <= state.staged) && !float.IsNaN(state.progress) && state.progress >= 0 && state.progress < CookSeconds && (state.paid || state.current >= 0 || state.progress == 0)
            && state.current >= -1 && state.current < Recipes.Length && (state.orders == null || state.orders.Count <= Workshop.MaxOrders && state.orders.TrueForAll(o => o != null
                && o.recipe >= 0 && o.recipe < Recipes.Length && o.amount >= 1 && o.amount <= Workshop.MaxOrderAmount && Enum.IsDefined(typeof(Workshop.OrderMode), o.mode)))
            && state.meals.TrueForAll(m => m != null && m.quality >= 1 && m.quality <= 3 && m.skill >= 0 && m.skill <= CommanderTalents.MaxLevel);
    }
}
