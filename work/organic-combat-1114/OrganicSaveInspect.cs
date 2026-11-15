using System.IO;
using System.Linq;
using AntColony.Save;
using UnityEngine;
public static class OrganicSaveInspect
{
    public static string Main()
    {
        var directory = Directory.GetDirectories(Application.temporaryCachePath, "OrganicCombat-*").OrderByDescending(Directory.GetLastWriteTimeUtc).First();
        var path = Directory.GetFiles(Path.Combine(directory, "saves"), "*.json").First();
        bool ok = SaveValidator.TryParse(File.ReadAllText(path), out _, out var error);
        return path + "\n" + ok + "\n" + error;
    }
}
