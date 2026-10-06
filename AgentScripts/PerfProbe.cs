using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

// 성능 조사(2026-10-07). 인자: 맵 크기, 추가 번식 수. 새 게임 → 프로파일러로 몇 프레임 기록 → 메인 스레드 자기 시간 상위 항목.
public static class PerfProbe
{
    public static async Task<string> Main(string size, int breed)
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261015, mapSize = (MapSize)Enum.Parse(typeof(MapSize), size), biome = MapBiome.Forest });
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        if (breed > 0) AntColony.Core.AntPool.Instance.Breed(breed);
        Time.timeScale = 1;
        for (var i = 0; i < 10; i++) await Task.Yield();
        ProfilerDriver.ClearAllFrames();
        ProfilerDriver.enabled = true; Profiler.enabled = true;
        var start = Time.frameCount;
        while (Time.frameCount < start + 8) await Task.Yield();
        ProfilerDriver.enabled = false;
        var sb = new StringBuilder($"size={size} breed={breed} dt={Time.unscaledDeltaTime * 1000:0}ms\n");
        var totals = new System.Collections.Generic.Dictionary<string, float>();
        var frames = 0;
        for (var f = ProfilerDriver.firstFrameIndex; f <= ProfilerDriver.lastFrameIndex; f++)
        {
            using var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.InvertHierarchy, HierarchyFrameDataView.columnSelfTime, false);
            if (view == null || !view.valid) continue;
            frames++;
            var kids = new System.Collections.Generic.List<int>();
            view.GetItemChildren(view.GetRootItemID(), kids);
            foreach (var id in kids)
            {
                var name = view.GetItemName(id);
                totals[name] = (totals.TryGetValue(name, out var t) ? t : 0) + view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
            }
        }
        foreach (var pair in totals.OrderByDescending(p => p.Value).Take(15)) sb.AppendLine($"{pair.Value / Math.Max(1, frames):0.0}ms  {pair.Key}");
        return sb.ToString();
    }
}
