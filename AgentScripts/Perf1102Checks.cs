using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

// 폐기 가능한 시작된 게임에서 실행한다. 생성한 적의 전투·사망은 정착지 상태에 실제로 반영된다.
public static class Perf1102Checks
{
    private static async Task NextFrame()
    {
        var frame = Time.frameCount;
        do
        {
            await Task.Yield();
            if (!Application.isPlaying) throw new InvalidOperationException("측정 중 Play가 종료됐습니다.");
        } while (Time.frameCount == frame);
    }

    private static int Alive(List<WildMonster> spawned)
    {
        var count = 0;
        foreach (var monster in spawned)
            if (monster != null && monster.isActiveAndEnabled && !monster.IsDead) count++;
        return count;
    }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying || Time.timeScale <= 0)
            throw new InvalidOperationException("게임을 시작하고 일시정지를 해제한 Play 상태에서 실행하세요.");
        var local = SceneObjects.Any<LocalIncursions>();
        if (local == null) throw new InvalidOperationException("시작된 게임의 LocalIncursions가 없습니다.");
        foreach (var monster in WildMonster.All)
            if (monster != null && monster.IsRaider && !monster.IsDead)
                throw new InvalidOperationException("기존 습격대가 없는 평화 구간에서 실행하세요.");

        // AgentScripts가 별도 어셈블리로 실행돼도 internal API를 호출할 수 있게 한다.
        var spawn = typeof(LocalIncursions).GetMethod("TrySpawnWaveGroup", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(int), typeof(bool) }, null);
        if (spawn == null) throw new InvalidOperationException("TrySpawnWaveGroup API가 없습니다.");
        var settings = UserSettings.Current.Clone();
        var quiet = settings.Clone();
        quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
        var oldScale = Time.timeScale;
        var spawned = new List<WildMonster>(200);
        var report = new StringBuilder("PERF-1102 (1배속, 생성 후 안정화 1초 + 측정 10초)\n");
        try
        {
            UserSettings.Apply(quiet, false);
            Time.timeScale = 1;
            foreach (var target in new[] { 50, 100, 200 })
            {
                spawned.Clear();
                var before = new HashSet<WildMonster>(local.Visitors);
                try
                {
                    for (var count = 0; count < target;)
                    {
                        var group = Math.Min(20, target - count);
                        var ok = (bool)spawn.Invoke(local, new object[] { group, count == 0 });
                        foreach (var visitor in local.Visitors)
                            if (visitor != null && before.Add(visitor)) spawned.Add(visitor);
                        if (!ok) throw new InvalidOperationException("습격대 생성 실패: 누적 " + spawned.Count);
                        count += group;
                    }
                    if (spawned.Count != target || Alive(spawned) != target)
                        throw new InvalidOperationException("생성 수 불일치: 요청 " + target + ", 생존 " + Alive(spawned));
                    var warmup = Time.realtimeSinceStartupAsDouble + 1;
                    while (Time.realtimeSinceStartupAsDouble < warmup) await NextFrame();
                    var samples = new List<float>(4096);
                    var until = Time.realtimeSinceStartupAsDouble + 10;
                    var minimum = Alive(spawned);
                    var maximum = minimum;
                    double sum = 0;
                    while (Time.realtimeSinceStartupAsDouble < until)
                    {
                        await NextFrame();
                        var ms = Time.unscaledDeltaTime * 1000f;
                        samples.Add(ms); sum += ms;
                        var alive = Alive(spawned);
                        minimum = Math.Min(minimum, alive); maximum = Math.Max(maximum, alive);
                    }
                    if (samples.Count == 0) throw new InvalidOperationException("측정 프레임이 없습니다.");
                    samples.Sort();
                    var p95 = samples[Math.Max(0, (int)Math.Ceiling(samples.Count * .95) - 1)];
                    var line = $"요청 {target}마리 | 생존 {minimum}~{maximum} | {samples.Count}프레임 | 평균 {sum / samples.Count:0.00}ms | P95 {p95:0.00}ms"
                        + (minimum == target && maximum == target ? "" : " | 고정 N 측정 불충족");
                    Debug.Log(line); report.AppendLine(line);
                }
                finally
                {
                    foreach (var monster in spawned)
                        if (monster != null && !monster.IsDead) monster.Wound(1e9f);
                }
                // 사체·드롭·포로 처리까지 실행된 상태로 다음 구간에 진입한다.
                await NextFrame();
            }
            return report.ToString();
        }
        finally
        {
            UserSettings.Apply(settings, false);
            Time.timeScale = oldScale;
        }
    }
}