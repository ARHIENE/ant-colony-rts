using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;

// Disposable fixture. Reload the untouched normal checkpoint before natural play.
public static class Provision1114Checks
{
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static (bool raw, bool cooked, bool herbs) Needs(CommanderAnt c) =>
        ((bool, bool, bool))typeof(CommanderAnt).GetMethod("ProvisioningNeeds", Hidden).Invoke(c, null);
    static int Priority(Component target, CommanderJobs job, bool raw, bool cooked, bool herbs) =>
        (int)typeof(CommanderAnt).GetMethod("WorkNeedPriority", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] {target, job, raw, cooked, herbs});
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0 || !(SaveStorage.RootOverride ?? "").Contains("Provision1114"))
            throw new Exception("Paused isolated Provision1114 fixture required");
        int count = 0;
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
        foreach (var c in team) { c.PersonalState.meal.diet = CommanderAnt.DietSimple; c.PersonalState.health = new HealthState { migrated = true }; }
        var c0 = team[0];
        var rm = ResourceManager.Instance;
        foreach (var t in CookedFoods.MealTypes.Concat(new[] {ResourceType.Herb}).Distinct()) rm.TrySpend(t, rm.GetAmount(t));
        var kitchens = SceneObjects.All<Kitchen>().Where(k => k.isActiveAndEnabled && !k.IsDead && !k.IsTable).ToArray();
        foreach (var k in kitchens) k.Paused = true;
        Check(!Needs(c0).raw, "Cooked-only diet with paused kitchens does not demand raw eating stock");
        var kitchen = kitchens.First();
        var meals = new Kitchen.State { configured = true };
        meals.orders.Add(new Kitchen.Order { recipe = 0, mode = Workshop.OrderMode.Count, amount = 1 });
        typeof(Kitchen).GetProperty("Meals").SetValue(kitchen, meals);
        Check(Needs(c0).raw, "An active meal order requires gathering ingredients even on cooked-only diet");
        kitchen.Paused = true;
        Check(!Needs(c0).raw, "Paused meal order does not demand ingredients");
        kitchen.Paused = false;
        var courier = new object(); int amount = Kitchen.Recipes[0].inputs.First(i => i.type == ResourceType.Food).amount;
        Check(kitchen.BeginPickup(courier, ResourceType.Food, amount), "Reserve exactly one batch");
        Check(kitchen.Deliver(courier, ResourceType.Food, amount) == amount, "Stage one batch");
        Check(!Needs(c0).raw, "Staged ingredients are not counted twice");
        var bench = SceneObjects.All<Processor>().First(p => p.IsMedical && p.isActiveAndEnabled);
        bench.Paused = false;
        Check(Needs(c0).herbs, "Preventive medicine order requests herbs without a current patient");
        bench.Paused = true;
        Check(!Needs(c0).herbs, "Paused medical order does not request herbs");
        bench.Paused = false;
        var herb = SceneObjects.All<AntColony.World.ResourceNode>().First(n => n.ResourceType == ResourceType.Herb);
        Check(Priority(herb, CommanderJobs.Gathering, false, false, true) == 2, "Preventive herbs have ordinary supply priority, below emergency care");
        rm.Add(ResourceType.Herb, 2);
        Check(!Needs(c0).herbs, "A stocked recipe stops requesting extra herbs");
        Check(Priority(bench, CommanderJobs.Hauling, false, false, false) == 2, "Ingredient delivery continues the ordered medicine supply chain");
        foreach (var order in bench.Orders.ToArray()) bench.RemoveOrder(order);
        Check(!Needs(c0).herbs, "Cancelled medical order does not request herbs");
        GameSession.Instance.MarkStarted(10, 30);
        c0.CommandStop(); c0.WorkState.health = 100;
        c0.PersonalState.sleep = new CommanderSleepState();
        c0.PersonalState.hygiene = new CommanderHygieneState();
        c0.PersonalState.joy = new CommanderJoyState();
        c0.PersonalState.mentalBreak = MentalBreak.None;
        c0.PersonalState.meal = new CommanderMealState { diet = CommanderAnt.DietRaw, satiety = 0 };
        var restoreCargo = typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden);
        restoreCargo.Invoke(c0, new object[] { 10f, ResourceType.Wood, null });
        c0.TickDuty(.1f);
        Check(c0.CarriedAmount == 10, "Hungry carrier keeps cargo when no edible meal exists");
        c0.PersonalState.meal.retrySeconds = 0;
        c0.TickDuty(.1f);
        Check(c0.CarriedAmount == 10, "Repeated hungry retry does not drop delivery");
        rm.Add(ResourceType.Food, c0.RationPortion);
        var woodBefore = SceneObjects.All<AntColony.World.ResourceNode>().Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Wood).Sum(n => n.AmountRemaining);
        c0.TickDuty(.1f);
        Check(c0.CarriedAmount == 0, "Available allowed food still interrupts cargo for eating");
        var woodAfter = SceneObjects.All<AntColony.World.ResourceNode>().Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Wood).Sum(n => n.AmountRemaining);
        Check(Math.Abs(woodAfter - woodBefore - 10) < .01f, "Meal interruption preserves all ten cargo on the floor");
        var result = "PASS " + count + " provisioning assertions; injected disposable fixture, not natural play.";
        File.WriteAllText("work/provision-1114/regression.txt", result); return result;
    }
}
