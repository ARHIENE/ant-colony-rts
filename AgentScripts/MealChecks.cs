using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 4단계 식사 (2026-09-28): 포만 감소, 식당 재고 식사·품질 기분, 날것, 식중독, 식사 특성, 굶주림, 장수 유지비 삭제, 저장.
public static class MealChecks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(NavMesh.SamplePosition(position, out var hit, 10, NavMesh.AllAreas), "building position");
        var go = Object.Instantiate(template, hit.position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static void Warp(CommanderAnt c, Vector3 p) { c.CommandStop(); Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable"); Check(c.Agent.Warp(hit.position), "warp"); }
    static void At(float timeOfDay) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, timeOfDay);
    static float Mood(CommanderAnt c, string reason) => c.PersonalState.moodFactors.Find(f => f.reason == reason)?.value ?? 0;
    static void Hungry(CommanderAnt c) { c.PersonalState.meal = new CommanderMealState { satiety = 20 }; c.PersonalState.moodFactors.Clear(); }
    // 먹기 시작 → 식사 시간만큼 흘려 끝낸다.
    static void Eat(CommanderAnt c)
    {
        c.TickDuty(1); Check(c.IsEating, "starts eating");
        for (var i = 0; i < 30 && c.IsEating; i++) c.TickDuty(1);
        Check(!c.IsEating && c.Satiety == 100, "meal restores satiety");
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Meal-" + Guid.NewGuid().ToString("N"));
        GameObject kitchenObject = null;
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260929, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var all = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            foreach (var x in all) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); x.Traits.values.Clear(); }
            var c = all[0]; var rm = ResourceManager.Instance;
            rm.Add(AntColony.Data.ResourceType.Food, 200);
            At(10);

            // 장수 자동 유지비 삭제: 일반개미만 청구.
            var upkeep = Object.FindFirstObjectByType<UpkeepManager>();
            Check(upkeep.FoodDue == AntPool.Instance.Total, "upkeep bills ants only");

            // 포만은 깨어 있는 동안 줄고, 30 이하가 되면 먹으러 간다.
            c.PersonalState.meal = new CommanderMealState();
            c.TickDuty(10);
            Check(Mathf.Approximately(c.Satiety, 100 - GameBalance.SatietyPerSecond * 10), "satiety decays " + c.Satiety);
            Check(!c.IsEating, "not hungry yet");

            // 식당이 없으면 창고에서 날로: Food 2, 기분 -8.
            var store = BuildingBase.FindNearestDepositPoint(c.Position);
            Warp(c, store.Position + Vector3.forward * 3);
            Check((store.Position - c.Position).sqrMagnitude <= 49, "near storage");
            Hungry(c); var food = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Eat(c);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == food - 2, "raw meal costs 2 Food");
            Check(Mood(c, "식사: 날것") == -8 && Mood(c, "배부름") == 5, "raw meal mood -8, fed +5");

            // 대식: 한 끼 ×1.5, 배부름 +10. 강철 위장: 날것 페널티 없음.
            c.Traits.values.Add(CommanderTrait.Glutton); c.Traits.values.Add(CommanderTrait.IronStomach);
            Hungry(c); food = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Eat(c);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == food - 3 && Mood(c, "배부름") == 10, "glutton eats 3 Food, fed +10");
            Check(Mood(c, "식사: 날것") == 0, "iron stomach ignores raw penalty");
            c.Traits.values.Clear();

            // 식당 재고: 가장 좋은 식사부터, 고급 +10.
            var kitchen = Build<Kitchen>(BuildingKind.Kitchen, c.Position + Vector3.right * 8); kitchenObject = kitchen.gameObject;
            kitchen.Meals.meals.Add(new Kitchen.Meal { quality = 1, skill = 20 });
            kitchen.Meals.meals.Add(new Kitchen.Meal { quality = 3, skill = 20 });
            Warp(c, kitchen.Position + Vector3.forward * 3);
            Check((kitchen.Position - c.Position).sqrMagnitude <= 49, "near kitchen");
            Hungry(c); food = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Eat(c);
            Check(kitchen.Meals.meals.Count == 1 && kitchen.Meals.meals[0].quality == 1, "takes best meal");
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == food, "cooked meal already paid");
            Check(Mood(c, "식사: 고급 식사") == 10 && !c.IsFoodPoisoned, "fine meal +10, skill 20 never poisons");

            // 미식가는 한 칸 낮게: 간단한 식사 → 날것 기분.
            c.Traits.values.Add(CommanderTrait.Gourmet);
            Hungry(c); Eat(c);
            Check(Mood(c, "식사: 날것") == -8 && kitchen.Meals.meals.Count == 0, "gourmet feels simple meal as raw");
            c.Traits.values.Clear();
            var taste = new CommanderTraits(); Check(taste.TryAdd(CommanderTrait.Gourmet) && !taste.TryAdd(CommanderTrait.DullTaste), "gourmet excludes dull taste");

            // 식당이 멀면 걸어가서 먹는다.
            kitchen.Meals.meals.Add(new Kitchen.Meal { quality = 2, skill = 20 });
            Warp(c, kitchen.Position + Vector3.forward * 20);
            Hungry(c); c.TickDuty(1);
            Check(!c.IsEating && (c.Agent.hasPath || c.Agent.pathPending), "walks to kitchen");
            Warp(c, kitchen.Position + Vector3.forward * 3); Eat(c);
            Check(Mood(c, "식사: 좋은 식사") == 5, "good meal +5");

            // 식중독: 요리 기술 0 → 6%. 걸리면 60초 작업 불가·기분 -10. 강철 위장은 면역.
            var poisoned = false;
            for (var i = 0; i < 300 && !poisoned; i++)
            {
                c.PersonalState.meal = new CommanderMealState { satiety = 20, eatSeconds = .5f, quality = 1, cookSkill = 0 };
                c.TickDuty(1); poisoned = c.IsFoodPoisoned;
            }
            Check(poisoned && CommanderOverhead.Activity(c) == "식중독" && Mood(c, "식중독") == GameBalance.FoodPoisonMood, "food poisoning at skill 0");
            c.TickDuty(GameBalance.FoodPoisonSeconds);
            Check(!c.IsFoodPoisoned, "poisoning wears off");
            c.Traits.values.Add(CommanderTrait.IronStomach);
            for (var i = 0; i < 300; i++)
            {
                c.PersonalState.meal = new CommanderMealState { satiety = 20, eatSeconds = .5f, quality = 1, cookSkill = 0 };
                c.TickDuty(1); Check(!c.IsFoodPoisoned, "iron stomach immune");
            }
            c.Traits.values.Clear();

            // Food가 없으면 굶주림 -15, 30초 뒤 재시도.
            rm.TrySpend(rm.GetAmount(AntColony.Data.ResourceType.Food), 0);
            Warp(c, store.Position + Vector3.forward * 3);
            Hungry(c); c.TickDuty(1);
            Check(!c.IsEating && Mood(c, "Hunger") == -15 && c.PersonalState.meal.retrySeconds == GameBalance.StarveRetrySeconds, "starving without food");

            // 출전·수면 중에는 먹으러 가지 않는다(수면 중엔 포만도 그대로).
            rm.Add(AntColony.Data.ResourceType.Food, 50);
            Hungry(c); At(GameCalendar.DaySeconds + 1); c.TickDuty(1);
            Check(c.IsAsleep && !c.IsEating && c.Satiety == 20, "asleep: no meal, no hunger decay");
            At(10); c.TickDuty(1);

            // 저장: 식사 상태 왕복·검증.
            c.PersonalState.meal = new CommanderMealState { satiety = 42, poisonSeconds = 5 };
            var copy = c.CapturePersonalState();
            Check(copy.meal.satiety == 42 && copy.meal.poisonSeconds == 5 && copy.Validate(out _), "meal state roundtrip");
            copy.meal.satiety = float.NaN;
            Check(!copy.Validate(out _), "invalid satiety rejected");
            var legacy = JsonUtility.FromJson<CommanderPersonalState>("{\"id\":\"x\"}");
            Check(legacy.meal != null && legacy.meal.satiety == 100, "old save gets full satiety");
            return $"MealChecks passed: {checks}";
        }
        finally
        {
            if (kitchenObject != null) Object.Destroy(kitchenObject);
            SaveStorage.RootOverride = root; Time.timeScale = 1;
        }
    }
}
