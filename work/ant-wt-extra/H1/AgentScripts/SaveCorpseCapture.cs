using System;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// SAVE 전용: 폐기할 Play 세션에서 시체 작업과 실제 HUD를 준비한다.
public static class SaveCorpseCapture
{
    static UserSettingsData settings;
    public static async Task<string> Prepare()
    {
        settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 260930, biome = MapBiome.Garden });
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("Scene not ready");
        GameMenuController.Instance.Resume(); Time.timeScale = 0; GameSession.Instance.MarkStarted(0, 20);
        foreach (var c in CommanderRoster.Instance.Commanders)
        {
            c.CommandStop(); c.WorkState.jobs = CommanderJobs.Cleaning;
            c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced));
        }
        var cleaner = CommanderRoster.Instance.Commanders[0]; var eater = CommanderRoster.Instance.Commanders[1];
        cleaner.Traits.TryAdd(CommanderTrait.Undertaker); cleaner.Traits.TryAdd(CommanderTrait.Neat);
        eater.Traits.TryAdd(CommanderTrait.Cannibal);
        var center = WorldMapManager.Instance.HomePosition + Vector3.right * 9;
        if (!NavMesh.SamplePosition(center, out var hit, 12, NavMesh.AllAreas)) throw new Exception("No corpse ground");
        center = hit.position; cleaner.Agent.Warp(center + Vector3.left); eater.Agent.Warp(center + Vector3.forward * 4);
        var corpse = Corpse.Spawn(new Corpse.State { name = "일반개미", position = center, count = 3, priority = true });
        var edible = Corpse.Spawn(new Corpse.State { name = "일반개미", position = eater.Position + Vector3.right, count = 2 });
        if (!cleaner.StartCorpseWork(corpse)) throw new Exception("Cleaner not ready"); cleaner.TickDuty(2);
        if (!eater.StartCorpseWork(edible, true)) throw new Exception("Eater not ready"); eater.TickDuty(2);
        WorkTargetPanel.Select(corpse);
        Object.FindAnyObjectByType<AntColony.Units.SelectionManager>().enabled = false;
        Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(center + Vector3.forward);
        Camera.main.orthographicSize = 12;
        return "Paused: 3-body cleaning pile, 2-body cannibal pile; target panel and HUD visible.";
    }
    public static string Finish() { UserSettings.Apply(settings, false); return "Capture settings restored."; }
}
