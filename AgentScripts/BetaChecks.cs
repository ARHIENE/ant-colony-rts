using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BetaChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception("FAIL: " + message); checks++; }
    private static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
        Check(!SaveSystem.Busy, "scene ready"); await Task.Delay(200);
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play mode"); checks = 0;
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BetaChecks-" + Guid.NewGuid().ToString("N"));
        Encyclopedia.ResetCache();
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, difficulty = DifficultyLevel.Normal, seed = 23456 });
            await Ready();
            Check(GameSession.Instance.GameStarted && Time.timeScale > 0, "new game simulation running");
            var commanders = CommanderRoster.Instance.Commanders;
            Check(commanders.Count == 12 && commanders.All(c => c.GetComponent<AntVisual>() != null), "all generated commanders use Quirky");
            var commander = commanders[0]; var visual = commander.GetComponent<AntVisual>();
            var animator = commander.GetComponentInChildren<Animator>();
            Check(animator != null && animator.avatar != null && !animator.applyRootMotion, "avatar and no root motion");
            foreach (var state in new[] { "Idle_A", "Walk", "Run", "Eat", "Attack", "Hit", "Fly", "Sit", "Death" })
                Check(animator.HasState(0, Animator.StringToHash("Base Layer." + state)), "animation state " + state);
            Check(commander.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.sharedMesh != null && r.sharedMaterial.shader.isSupported), "meshes and URP shader resolve");
            Check(!commander.GetComponent<MeshRenderer>().enabled, "capsule hidden");
            Check(commander.GetComponentsInChildren<Collider>().Length == 1, "model does not add collision or ragdoll");
            Check(visual.StateName == "Idle_A", "idle animation");
            commander.CommandMove(commander.Position + Vector3.right * 3);
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (visual.StateName != "Run" && visual.StateName != "Walk" && DateTime.UtcNow < deadline) await Task.Delay(100);
            Check(visual.StateName == "Run" || visual.StateName == "Walk", "movement drives locomotion");
            commander.CommandStop();
            visual.Attack(commander.Position + Vector3.forward);
            await Task.Delay(180);
            Check(visual.StateName == "Attack" && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Attack"), "attack is actually evaluated");
            commander.TakeDamage(.1f); await Task.Delay(180);
            Check(visual.StateName == "Hit", "damage drives hit reaction");
            await Task.Delay(400);
            commander.ReturnTroops(commander.TroopCount);
            await Task.Delay(150);
            Check(visual.StateName == "Sit", "troopless commander remains alive and sits");
            commander.TryAssign(2);
            var flying = commanders.First(c => c.AllowedRoles.Contains(UnitRole.Flying));
            Check(flying.TrySetRole(UnitRole.Flying), "change to flying role");
            await Task.Delay(200);
            Check(flying.GetComponent<AntVisual>().StateName == "Fly", "flying animation");
            Check(flying.TrySetRole(UnitRole.Worker), "land again");
            var enemy = Object.FindObjectsByType<EnemyCommander>(FindObjectsSortMode.None).First();
            Check(enemy.GetComponent<AntVisual>() != null, "enemy commanders have Quirky visuals");
            foreach (var c in commanders) c.CommandStop();
            GameMenuController.Instance.Pause();
            var time = GameSession.Instance.PlaySeconds; await Task.Delay(200);
            Check(GameSession.Instance.PlaySeconds == time, "pause does not inflate play time");
            GameMenuController.Instance.Guide();
            Check(GameMenuController.Instance.ScreenName == "FIELD GUIDE" && GameMenuController.BlocksInput, "guide opens and blocks commands");
            Check(Object.FindFirstObjectByType<BetaProgress>() != null, "objectives installed");
            Check(SaveSystem.TrySave(false, 0, out var error), "save before reload: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error);
            await Ready();
            Check(CommanderRoster.Instance.Commanders.All(c => c.GetComponentInChildren<Animator>() != null), "loaded commanders keep models/animations");
            GameManager.Instance.ReportBossDefeated(); await Task.Delay(100);
            Check(GameMenuController.Instance.ScreenName == "VICTORY - BETA COMPLETE" && Time.timeScale == 0, "boss victory pauses on results");
            GameMenuController.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Continue Colony").onClick.Invoke();
            Check(Time.timeScale > 0 && GameMenuController.Instance.ScreenName == "Game", "continue after victory");
            foreach (var b in Object.FindObjectsByType<AntColony.Buildings.BuildingBase>(FindObjectsSortMode.None).Where(b => b.CountsTowardPlayerDefeat).ToArray())
                b.TakeDamage(float.MaxValue);
            await Task.Delay(100);
            Check(GameMenuController.Instance.ScreenName == "COLONY LOST" && Time.timeScale == 0, "defeat results");
            GameMenuController.Instance.Resume(); Check(Time.timeScale == 0, "cannot escape defeat into simulation");
            GameMenuController.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Restart Same Map").onClick.Invoke();
            GameMenuController.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Restart").onClick.Invoke();
            await Ready();
            Check(GameSession.Instance.Options.seed == 23456 && GameMenuController.Instance.ScreenName == "Game", "restart resets outcome and preserves map seed");
            return "PASS " + checks + " beta checks: Quirky animation, save/load, objectives, victory/defeat/restart.";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; Encyclopedia.ResetCache(); }
    }
}
