using System;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

// 건설 화면의 특정 탭을 연다(캡처용). 인자: 탭 이름(예: "휴게").
public static class BuildTabShot
{
    public static async Task<string> Main(string tabName)
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>()?.ClearSelection();
        BuildScreen.Open();
        var t = typeof(BuildScreen);
        var inst = t.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var names = (string[])t.GetField("TabNames", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetValue(null);
        t.GetField("tab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(inst, Array.IndexOf(names, tabName));
        await Task.Yield(); await Task.Yield();
        return "opened " + tabName;
    }
}
