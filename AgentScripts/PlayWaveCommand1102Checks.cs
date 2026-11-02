using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// 2026-11-02 플레이어 지휘 재현: 웨이브 1·3·5·8을 '지금 시작'으로 열고, 플레이어처럼 장수 전원에게 가장 가까운 적 공격(A+클릭)을 반복 지시.
// 웨이브 종료까지 걸린 시간, 장수 개인 체력·병력 변화, 쓰러짐을 기록. 캡처 Temp/cmd-*.png.
public static class PlayWaveCommand1102Checks
{
    static string log = "";
    static void Note(string s) { log += s + "\n"; File.WriteAllText("Temp/cmd-log.txt", log); }

    public static async Task<string> Main()
    {
        log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PlayCmd-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>(); var cam = SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
            var s = (WaveDefense.State)typeof(WaveDefense).GetField("s", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(w);
            foreach (var wave in new[] { 1, 3, 5, 8, 12 })
            {
                var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead && c.IsColonyMember).ToArray();
                if (team.All(c => c.Health.Downed)) { Note("장수 전원 쓰러짐 — 중단"); break; }
                var hp0 = team.Sum(c => c.PersonalHealth); var troops0 = team.Sum(c => c.TroopCount);
                s.wave = wave - 1; w.StartNow();
                var t = Time.realtimeSinceStartup; var shot = false;
                while (w.InWave && Time.realtimeSinceStartup - t < 180)
                {
                    foreach (var c in team)
                    {
                        if (c == null || c.IsDead || c.Health.Downed) continue;
                        var enemy = local.Visitors.Where(v => v != null && !v.IsDead).OrderBy(v => (v.Position - c.Position).sqrMagnitude).FirstOrDefault();
                        if (enemy != null) c.CommandAttack(enemy);
                    }
                    if (!shot && Time.realtimeSinceStartup - t > 12)
                    {
                        var e = local.Visitors.FirstOrDefault(v => v != null && !v.IsDead); if (e != null) cam?.FocusOn(e.Position);
                        ScreenCapture.CaptureScreenshot($"Temp/cmd-wave{wave}.png"); shot = true;
                    }
                    await Task.Delay(1000);
                }
                Note($"웨이브 {wave}(적 {s.total}): {(w.InWave ? "3분 내 미종료 · 남은 적 " + w.RemainingEnemies : $"종료 {Time.realtimeSinceStartup - t:0}초")} · 장수 체력 {hp0:0}→{team.Sum(c => c.PersonalHealth):0} · 병력 {troops0}→{team.Sum(c => c.TroopCount)} · 쓰러짐 {team.Count(c => c.Health.Downed)}");
                if (w.InWave) { foreach (var v in local.Visitors.ToArray()) if (v != null && !v.IsDead) v.Wound(1e9f); var t2 = Time.realtimeSinceStartup; while (w.InWave && Time.realtimeSinceStartup - t2 < 15) await Task.Delay(100); }
            }
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
