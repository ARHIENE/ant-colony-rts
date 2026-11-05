using System;
using System.Threading.Tasks;
using AntColony.Save;
using UnityEngine;
public static class AnimeSettlerRestore
{
    public static async Task<string> Main()
    {
        if (!SaveSystem.TryLoad("Temp/AnimeSettler/before-20261105-222652.json", out var error)) throw new Exception(error);
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(50);
        Time.timeScale = 0;
        if (SaveSystem.Busy) throw new Exception("Restore still loading; paused for inspection.");
        return "Original pre-work snapshot restored; simulation paused.";
    }
}

