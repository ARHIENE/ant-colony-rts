using System;
using System.IO;
using System.Reflection;
using AntColony.Save;
using UnityEngine;
public static class AnimeSettlerBackup
{
    public static string Main()
    {
        if (!Application.isPlaying || SaveSystem.Busy) throw new Exception("Need stable running game.");
        var snapshot = SaveSnapshot.Capture();
        var type = typeof(SaveSystem).Assembly.GetType("AntColony.Save.SaveOptionalStates");
        var method = type.GetMethod("ToJson", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var text = (string)method.Invoke(null, new object[] { snapshot, true });
        Directory.CreateDirectory("Temp/AnimeSettler");
        var path = "Temp/AnimeSettler/before-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
        File.WriteAllText(path, text);
        Time.timeScale = 0;
        return path;
    }
}
