using System;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AntWorkVisualChecks
{
    private static async Task Wait(Func<bool> ready, string label)
    {
        var end = DateTime.UtcNow.AddSeconds(10);
        while (!ready() && DateTime.UtcNow < end) await Task.Delay(100);
        if (!ready()) throw new Exception("FAIL: " + label);
    }
    public static async Task<string> Main()
    {
        var original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "AntWorkVisual-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (!Application.isPlaying) throw new Exception("Play mode required.");
            await Wait(() => !SaveSystem.Busy, "previous load ready");
            SaveSystem.NewGame(new NewGameOptions { seed = 241024, mapSize = MapSize.Small });
            var until = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(100);
            if (SaveSystem.Busy) throw new Exception("New game load timed out.");
            await Task.Delay(300);
            return await Run();
        }
        finally { SaveStorage.RootOverride = original; }
    }

    static async Task<string> Run()
    {
        if (!Application.isPlaying || SaveSystem.Busy) throw new Exception("Start an initialized Play session.");
        Time.timeScale = 1;
        var commander = CommanderRoster.Instance.Commanders[0];
        commander.CommandStop();
        var siteObject = new GameObject("VisualCheckSite"); siteObject.transform.position = commander.Position;
        var nodeObject = new GameObject("VisualCheckSoil"); nodeObject.transform.position = commander.Position;
        var enemyObject = new GameObject("VisualCheckEnemy"); enemyObject.transform.position = commander.Position + Vector3.right * 20;
        try
        {
            var site = siteObject.AddComponent<BuildingConstructionSite>(); site.Initialize(null, 60);
            commander.CommandBuild(site);
            await Wait(() => commander.GetComponent<AntVisual>().StateName == "Attack" && commander.IsBuildingAnimation, "construction animation follows actual work");
            commander.CommandStop();
            var node = nodeObject.AddComponent<ResourceNode>(); node.ConfigureLoot(AntColony.Data.ResourceType.Soil, 100);
            commander.CommandGather(node);
            await Wait(() => commander.GetComponent<AntVisual>().StateName == "Eat" && commander.IsGatheringAnimation && commander.IsCarrying, "gathering animation and real cargo");
            var enemy = enemyObject.AddComponent<EnemyCommander>(); enemy.MakeRaider();
            await Task.Delay(100);
            var count = Object.FindObjectsByType<Animator>().Length;
            enemy.TakeDamage(float.MaxValue);
            await Task.Delay(200);
            var corpse = Array.Find(Object.FindObjectsByType<Animator>(), a => a.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"));
            if (corpse == null || enemy != null) throw new Exception("FAIL: visual death outlives removed combat unit");
            await Task.Delay(2700);
            // 시체는 즉시 사라지지 않고 치우기 작업 대상으로 남는다(CorpseChecks가 정리 담당).
            if (Object.FindObjectsByType<AntColony.World.Corpse>().Length == 0) throw new Exception("FAIL: corpse stays for cleaning");
            return "PASS: actual construction, gathering/cargo, enemy death animation and corpse kept for cleaning.";
        }
        finally
        {
            commander.CommandStop();
            if (siteObject != null) Object.Destroy(siteObject);
            if (nodeObject != null) Object.Destroy(nodeObject);
            if (enemyObject != null) Object.Destroy(enemyObject);
            Time.timeScale = 0;
        }
    }
}
