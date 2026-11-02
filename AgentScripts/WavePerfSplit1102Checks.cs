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

// 웨이브 28 성능 절벽 원인 분리(에디터): 전체 → 떼 표시 끔 → 적 AI 끔(Update 비활성) 순으로 프레임 비교.
public static class WavePerfSplit1102Checks
{
    static async Task<string> Measure(string label, float seconds = 6)
    {
        var list = new System.Collections.Generic.List<float>(); var end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) { await Task.Yield(); list.Add(Time.unscaledDeltaTime * 1000); }
        list.Sort(); return $"{label}: 평균 {list.Average():0.0}ms · p95 {list[(int)(list.Count * .95f)]:0.0}ms\n";
    }

    public static async Task<string> Main()
    {
        var log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WaveSplit-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
            GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();
            var s = (WaveDefense.State)typeof(WaveDefense).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(w);
            var dmg = typeof(WildMonster).GetField("attackDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            log += await Measure("적 0");
            s.wave = 27; w.StartNow();
            var t = Time.realtimeSinceStartup;
            while (s.pending > 0 && Time.realtimeSinceStartup - t < 90) { foreach (var v in local.Visitors) if (v != null) dmg.SetValue(v, 0f); await Task.Delay(100); }
            foreach (var v in local.Visitors) if (v != null) dmg.SetValue(v, 0f);
            var alive = local.Visitors.Where(v => v != null && !v.IsDead).ToArray();
            log += $"웨이브 28: 마릿수 {w.AliveEnemies} 유닛 {alive.Length}\n";
            log += await Measure("전체");
            var hv = alive.Select(v => v.GetComponent<AntColony.Units.HordeVisual>()).Where(h => h != null).ToArray();
            foreach (var h in hv) h.enabled = false;
            log += await Measure($"떼 표시 끔({hv.Length})");
            foreach (var h in hv) h.enabled = true;
            foreach (var v in alive) v.enabled = false;
            log += await Measure("적 AI 끔(떼 표시 켬)");
            foreach (var v in alive) { var a = v.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (a != null) a.enabled = false; }
            log += await Measure("적 AI+NavMeshAgent 끔");
            foreach (var v in alive) if (v != null) { v.enabled = true; v.Wound(1e9f); }
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
