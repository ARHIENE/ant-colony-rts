using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// 2026-11-02 첫 평화 → 첫 웨이브 → 다음 평화 실진행 관찰(자동 작업만, 플레이어 조작 없음). 5배속, 장면 캡처 Temp/play-*.png.
public static class PlayFirstWave1102Checks
{
    static string log = "";
    static void Note(string s) { log += $"[{GameCalendar.Day}일 {GameCalendar.GameSeconds:0}s] {s}\n"; File.WriteAllText("Temp/play-log.txt", log); }
    static async Task Shot(string name) { ScreenCapture.CaptureScreenshot($"Temp/play-{name}.png"); await Task.Delay(400); }

    public static async Task<string> Main()
    {
        log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PlayWave-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 5;
            var w = WaveDefense.Instance; var cam = SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            var home = CommanderRoster.Instance.Commanders[0].Position; cam?.FocusOn(home);
            Note($"시작 · 평화 {w.PeaceLeft:0}s"); await Shot("0-start");
            var lastDay = -1;
            while (!w.InWave && GameCalendar.GameSeconds < 1200)
            {
                if (GameCalendar.Day != lastDay)
                {
                    lastDay = GameCalendar.Day;
                    var cs = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead).ToArray();
                    Note($"식량 {ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food):0} · 건물 {GameManager.Instance?.FindNearestPlayerBuilding(home) != null} · 장수 {cs.Length} 부상/쓰러짐 {cs.Count(c => c.Health.Downed)} · 목표 {GoalChain.CurrentText.Split('\n')[0]}");
                    cam?.FocusOn(home); await Shot($"1-day{lastDay}");
                }
                if (w.PeaceLeft < 60 && !log.Contains("직전 HUD")) { Note("웨이브 직전 HUD"); await Shot("2-before"); }
                await Task.Delay(200);
            }
            Note($"웨이브 {w.Wave} 시작 · 적 {w.RemainingEnemies}");
            Time.timeScale = 1;
            await Task.Delay(3000); var enemy = SceneObjects.Any<LocalIncursions>().Visitors.FirstOrDefault(v => v != null && !v.IsDead);
            if (enemy != null) cam?.FocusOn(enemy.transform.position);
            await Shot("3-wave");
            var t = Time.realtimeSinceStartup; var shots = 0;
            while (w.InWave && Time.realtimeSinceStartup - t < 240)
            {
                if (Time.realtimeSinceStartup - t > shots * 15) { enemy = SceneObjects.Any<LocalIncursions>().Visitors.FirstOrDefault(v => v != null && !v.IsDead); if (enemy != null) cam?.FocusOn(enemy.transform.position); await Shot($"4-fight{shots++}"); Note($"전투 중 남은 적 {w.RemainingEnemies}"); }
                await Task.Delay(200);
            }
            Note(w.InWave ? "4분 안에 웨이브 미종료(장수가 싸우러 가지 않음?)" : $"웨이브 종료 · 평화 {w.PeaceLeft:0}s · 강조 {GoalChain.RecentAchievement}");
            cam?.FocusOn(CommanderRoster.Instance.Commanders[0].Position); await Shot("5-after");
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
