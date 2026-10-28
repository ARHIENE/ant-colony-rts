using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;

public static class BuildScreenPerformance1028Checks
{
    public static string Main()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<BuildScreen>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var update = typeof(BuildScreen).GetMethod("Update", flags);
        var panel = (RectTransform)typeof(BuildScreen).GetField("buildPanel", flags).GetValue(screen);
        var count = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Build screen FAIL " + label); count++; }
        while (BuildScreen.IsOpen) BuildScreen.Back();
        for (var i = 0; i < 5; i++) update.Invoke(screen, null);
        var controlStart = GC.GetAllocatedBytesForCurrentThread(); var control = new byte[4096];
        var counterAvailable = GC.GetAllocatedBytesForCurrentThread() > controlStart; GC.KeepAlive(control);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) update.Invoke(screen, null);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (counterAvailable) Check(allocated < 8192, "closed UI 100 updates allocation " + allocated);
        var original = panel.sizeDelta;
        try
        {
            BuildScreen.Open(); Canvas.ForceUpdateCanvases(); update.Invoke(screen, null);
            Check(BuildScreen.IsOpen, "opens normally");
            var tabs = (System.Collections.Generic.List<Button>)typeof(BuildScreen).GetField("tabButtons", flags).GetValue(screen);
            var width = ((RectTransform)tabs[0].transform).rect.width;
            panel.sizeDelta = original + Vector2.right * 80; update.Invoke(screen, null);
            Check(((RectTransform)tabs[0].transform).rect.width > width, "resize relayout preserved");
            var tab = typeof(BuildScreen).GetField("tab", flags);
            for (var i = 0; i < tabs.Count; i++)
            {
                tab.SetValue(screen, i); update.Invoke(screen, null);
                Check(tabs[i].gameObject.activeInHierarchy, "tab remains available " + i);
            }
            var result = "PASS " + count + " construction UI open/resize/all tabs; closed allocation="
                + (counterAvailable ? allocated + " bytes/100 updates" : "unavailable (Mono counter does not register control allocation)");
            File.WriteAllText("Temp/build-screen-performance1028-result.txt", result); return result;
        }
        finally { panel.sizeDelta = original; while (BuildScreen.IsOpen) BuildScreen.Back(); }
    }
}
