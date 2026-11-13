using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;

// Run only after preserving the natural playthrough. This is a disposable save-compatibility fixture.
public static class EarlyBalance1113Checks
{
    public static string Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride))
            throw new Exception("Isolated Play session required");
        var kitchen = SceneObjects.All<Kitchen>().First(k => k.isActiveAndEnabled && !k.IsDead && !k.IsTable);
        var chef = CommanderRoster.Instance.Commanders.First(c => c != null && !c.IsDead);
        var state = new Kitchen.State { current = 0, progress = 9, configured = true };
        if (!Kitchen.Valid(state)) throw new Exception("Previous 9/10 cooking progress rejected");
        typeof(Kitchen).GetProperty("Meals").SetValue(kitchen, state);
        var before = ProductionStock.Count(ResourceType.SimpleMeal);
        if (!kitchen.Work(chef, .1f) || kitchen.Current != null || kitchen.Progress != 0)
            throw new Exception("Old cooking progress did not finish cleanly");
        if (ProductionStock.Count(ResourceType.SimpleMeal) != before + 1)
            throw new Exception("Old cooking progress must yield exactly one meal");
        // The next order still requires actual ingredients: completed progress cannot be reused.
        kitchen.Orders.Clear();
        if (kitchen.Work(chef, .1f) || ProductionStock.Count(ResourceType.SimpleMeal) != before + 1)
            throw new Exception("Duplicate meal after completion");
        if (!SaveSystem.TrySave(false, 1, out var error)) throw new Exception(error);
        var result = "PASS previous 9/10 cooking progress accepted; completes once; no duplicate output; normal fixture save succeeds. Discard this fixture.";
        File.WriteAllText("work/early-balance-1113/save-compatibility-checks.txt", result);
        return result;
    }
}
