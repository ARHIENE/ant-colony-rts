using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;

namespace AntColony.World
{
    // 개발용 측정기: 실행 인자 -wavePerf 일 때만 동작. 새 게임 → 웨이브 20/25/28/30을 차례로 띄워 10초씩 프레임 시간을 기록하고 종료.
    // 결과: Application.persistentDataPath/waveperf.txt. 장수가 쓰러져도 측정이 이어지도록 웨이브 적의 공격력을 0으로 둔다.
    public sealed class WavePerfProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Environment.GetCommandLineArgs().Contains("-wavePerf")) new GameObject("WavePerfProbe").AddComponent<WavePerfProbe>();
        }

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            var log = new List<string> { $"{SystemInfo.processorType} / {SystemInfo.graphicsDeviceName} / {Screen.width}x{Screen.height}" };
            yield return new WaitForSecondsRealtime(3);
            while (SaveSystem.Busy) yield return null;
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3,
                startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            while (SaveSystem.Busy || WaveDefense.Instance == null) yield return null;
            AntColony.UI.GameMenuController.Instance?.Resume(); Time.timeScale = 1;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();
            var state = (WaveDefense.State)typeof(WaveDefense).GetField("s", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(w);
            yield return Measure(log, "적 0");
            foreach (var wave in new[] { 20, 25, 28, 30 })
            {
                state.wave = wave - 1; w.StartNow();
                var t = Time.realtimeSinceStartup;
                while (state.pending > 0 && Time.realtimeSinceStartup - t < 120)
                {
                    foreach (var v in local.Visitors) if (v != null) typeof(WildMonster).GetField("attackDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(v, 0f);
                    yield return null;
                }
                foreach (var v in local.Visitors) if (v != null) typeof(WildMonster).GetField("attackDamage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(v, 0f);
                yield return Measure(log, $"웨이브 {wave} 마릿수 {w.AliveEnemies}/{state.total} 유닛 {local.Visitors.Count(v => v != null && !v.IsDead)}");
                foreach (var v in local.Visitors.ToArray()) if (v != null && !v.IsDead) v.Wound(1e9f);
                t = Time.realtimeSinceStartup;
                while (w.InWave && Time.realtimeSinceStartup - t < 20) yield return null;
            }
            File.WriteAllLines(Path.Combine(Application.persistentDataPath, "waveperf.txt"), log);
            Application.Quit();
        }

        private static IEnumerator Measure(List<string> log, string label)
        {
            var samples = new List<float>(); var end = Time.realtimeSinceStartup + 10;
            while (Time.realtimeSinceStartup < end) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000); }
            samples.Sort();
            log.Add($"{label}: 평균 {samples.Average():0.0}ms · p95 {samples[(int)(samples.Count * .95f)]:0.0}ms · 최대 {samples[samples.Count - 1]:0.0}ms");
        }
    }
}
