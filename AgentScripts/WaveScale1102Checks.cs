using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// 2026-11-02 대규모 웨이브 실측(에디터): 새 게임 → 웨이브 N 실제 생성(개별 30 + 50마리 무리) → 10초 프레임 시간·캡처.
// 에디터 수치는 개발 Player보다 느리다. 참고값이며 최종 성능 판정은 Player 빌드로 한다.
public static class WaveScale1102Checks
{
    static async Task<(float avg, float p95)> Measure(float seconds)
    {
        var list = new System.Collections.Generic.List<float>(); var end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) { await Task.Yield(); list.Add(Time.unscaledDeltaTime * 1000); }
        list.Sort(); return (list.Average(), list[(int)(list.Count * .95f)]);
    }

    public static async Task<string> Main()
    {
        var log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WaveScale-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();
            var s = (WaveDefense.State)typeof(WaveDefense).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(w);
            var baseLine = await Measure(5); log += $"평화(적 0): 평균 {baseLine.avg:0.0}ms · p95 {baseLine.p95:0.0}ms\n";
            foreach (var wave in new[] { 20, 25, 28 })
            {
                s.wave = wave - 1; w.StartNow();
                var t = Time.realtimeSinceStartup;
                while (s.pending > 0 && Time.realtimeSinceStartup - t < 90) await Task.Delay(100);
                var hordes = local.Visitors.Where(v => v != null && !v.IsDead).ToArray();
                var center = hordes.Where(v => v.IsHorde).Select(v => v.transform.position).FirstOrDefault();
                SceneObjects.Any<AntColony.Camera.IsometricCameraController>()?.FocusOn(center);
                var m = await Measure(10);
                log += $"웨이브 {wave}: 마릿수 {w.AliveEnemies}/{s.total} · 유닛 {hordes.Length}(무리 {hordes.Count(v => v.IsHorde)}) · 평균 {m.avg:0.0}ms · p95 {m.p95:0.0}ms\n";
                ScreenCapture.CaptureScreenshot($"Temp/wave{wave}.png");
                await Task.Delay(500);
                foreach (var v in local.Visitors.ToArray()) if (v != null && !v.IsDead) v.Wound(1e9f);
                t = Time.realtimeSinceStartup;
                while (w.InWave && Time.realtimeSinceStartup - t < 20) await Task.Delay(100);
            }
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
