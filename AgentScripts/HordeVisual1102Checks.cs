using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

// 폐기할 게임에서만 실행한다. 생성한 습격대의 피해·포로·사체는 실제 게임 상태에 반영된다.
public static class HordeVisual1102Checks
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
        var total = 0;
        foreach (var monster in spawned)
            if (monster != null && monster.isActiveAndEnabled && !monster.IsDead) total++;
        return total;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException("무리 표시 검사 실패: " + message);
    }

    public static async Task<string> Main()
    {
        Check(Application.isPlaying && Time.timeScale > 0, "시작된 게임의 일시정지를 해제하세요.");
        var local = SceneObjects.Any<LocalIncursions>();
        Check(local != null, "LocalIncursions가 없습니다.");
        foreach (var monster in WildMonster.All)
            Check(monster == null || !monster.IsRaider || monster.IsDead, "기존 습격대가 없는 평화 구간에서 실행하세요.");
        var spawn = typeof(LocalIncursions).GetMethod("TrySpawnWaveGroup", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(int), typeof(bool) }, null);
        Check(spawn != null, "TrySpawnWaveGroup API가 없습니다.");
        var settings = UserSettings.Current.Clone();
        var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
        var oldScale = Time.timeScale;
        var spawned = new List<WildMonster>(80);
        var visuals = new List<HordeVisual>(80);
        var before = new HashSet<WildMonster>(local.Visitors);
        var perHost = 50;
        Func<int> count = () => perHost;
        try
        {
            UserSettings.Apply(quiet, false); Time.timeScale = 1;
            for (var group = 0; group < 4; group++)
            {
                var ok = (bool)spawn.Invoke(local, new object[] { 20, false });
                foreach (var visitor in local.Visitors)
                    if (visitor != null && before.Add(visitor))
                    {
                        spawned.Add(visitor);
                        visuals.Add(HordeVisual.Attach(visitor.gameObject, count));
                    }
                Check(ok, "습격대 생성 실패: " + spawned.Count);
            }
            Check(spawned.Count == 80 && Alive(spawned) == 80, "호스트 80개 생성 불충족");
            await NextFrame(); await NextFrame();
            foreach (var visual in visuals) Check(visual.ExtraBodies == 49, "활성 메시·인스턴싱 준비 또는 50마리 표시");
            Check(HordeVisual.Attach(spawned[0].gameObject, count) == visuals[0], "Attach 중복 컴포넌트");
            perHost = 25; await NextFrame(); await NextFrame();
            foreach (var visual in visuals) Check(visual.ExtraBodies == 24, "마릿수 감소");
            perHost = 1; await NextFrame(); await NextFrame();
            foreach (var visual in visuals) Check(visual.ExtraBodies == 0, "호스트 한 마리일 때 추가 몸체 제거");
            perHost = 50;
            spawned[0].gameObject.SetActive(false);
            Check(visuals[0].ExtraBodies == 0, "비활성 호스트의 추가 몸체 제거");
            spawned[0].gameObject.SetActive(true);

            var warmup = Time.realtimeSinceStartupAsDouble + 1;
            while (Time.realtimeSinceStartupAsDouble < warmup) await NextFrame();
            var samples = new List<float>(4096);
            var minimum = Alive(spawned); var maximum = minimum;
            var until = Time.realtimeSinceStartupAsDouble + 10;
            double sum = 0;
            while (Time.realtimeSinceStartupAsDouble < until)
            {
                await NextFrame();
                var ms = Time.unscaledDeltaTime * 1000f;
                samples.Add(ms); sum += ms;
                var alive = Alive(spawned); minimum = Math.Min(minimum, alive); maximum = Math.Max(maximum, alive);
            }
            Check(samples.Count > 0, "수집된 프레임 없음");
            samples.Sort();
            var p95 = samples[Math.Max(0, (int)Math.Ceiling(samples.Count * .95) - 1)];
            var report = new StringBuilder("HORDE-VIS-1102: 기본 표시 검사 통과\n");
            report.Append($"요청 80무리 × 50 = 4000몸체(추가 3920) | 생존 호스트 {minimum}~{maximum} | {samples.Count}프레임")
                .Append($" | 평균 {sum / samples.Count:0.00}ms | P95 {p95:0.00}ms");
            if (minimum != 80 || maximum != 80) report.Append(" | 고정 4000몸체 측정 불충족");
            Debug.Log(report.ToString());
            return report.ToString();
        }
        finally
        {
            // 시각 실험만 수행하며 실제 HordeCount·무리 체력은 변경하지 않는다.
            foreach (var monster in spawned)
                if (monster != null && !monster.IsDead) monster.Wound(1e9f);
            UserSettings.Apply(settings, false); Time.timeScale = oldScale;
        }
    }
}