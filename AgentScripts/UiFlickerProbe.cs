using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 2026-10-24 5배속에서 깜빡이는 UI 요소 이름 찾기.
public static class UiFlickerProbe
{
    static HashSet<string> Set() => new HashSet<string>(Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None)
        .Where(g => g.isActiveAndEnabled && g.color.a > .01f).Select(g => { var t = g.transform; var s = t.name; if (t.parent) s = t.parent.name + "/" + s; return s + "#" + g.GetEntityId().ToString(); }));
    public static async Task<string> Main()
    {
        AntColony.UI.GameMenuController.Instance.SetSpeed(5);
        var counts = new Dictionary<string, int>(); var last = Set();
        for (var i = 0; i < 60; i++)
        {
            var f = Time.frameCount; while (Time.frameCount == f) await Task.Delay(5);
            var now = Set();
            foreach (var n in now.Except(last).Concat(last.Except(now))) counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1;
            last = now;
        }
        AntColony.UI.GameMenuController.Instance.SetSpeed(1);
        return "FLICKER " + string.Join(" | ", counts.OrderByDescending(k => k.Value).Take(15).Select(k => k.Key.Split('#')[0] + " x" + k.Value));
    }
}
