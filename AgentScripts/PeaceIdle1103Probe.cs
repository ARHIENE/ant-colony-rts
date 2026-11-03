using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

// 2026-11-03 임시 측정: 첫 평화 구간 무조작 진행 시 식량·기분 원인·잔해·장수 활동을 반나절마다 기록(Temp/peace-log.txt). 웨이브 시작에서 종료.
public static class PeaceIdle1103Probe
{
    static string log = "";
    static void Note(string s) { log += $"[{GameCalendar.Day}일 {GameCalendar.GameSeconds:0}s] {s}\n"; File.WriteAllText("Temp/peace-log.txt", log); }

    public static async Task<string> Main()
    {
        log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PeaceIdle-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 5;
            var w = WaveDefense.Instance; var next = 0f;
            while (!w.InWave && GameCalendar.GameSeconds < 1200)
            {
                if (GameCalendar.GameSeconds >= next)
                {
                    next += 75;
                    var food = ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food);
                    var wrecks = SceneObjects.All<CrashWreck>().Count(x => x != null && x.isActiveAndEnabled && !x.IsDead);
                    Note($"식량 {food:0} · 잔해 {wrecks} · 시민 {AntPool.Instance?.Total} · 목표 {GoalChain.CurrentText.Split('\n')[0]}");
                    foreach (var c in CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead))
                    {
                        var ps = c.CapturePersonalState();
                        var factors = string.Join(", ", ps.moodFactors.OrderBy(f => f.value).Take(4).Select(f => $"{f.reason}{f.value:+0;-0}"));
                        Note($"  {c.name} 기분 {c.Mood:0} 체력 {c.PersonalHealth:0} 쓰러짐 {c.Health.Downed} 잠 {c.IsAsleep} 활동 {c.CurrentActivity} 작업 {c.IsWorking} 이탈 {c.IsDeparting} | {factors}");
                    }
                }
                await Task.Delay(200);
            }
            Note($"웨이브 {w.Wave} 시작 · 적 {w.RemainingEnemies}");
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
