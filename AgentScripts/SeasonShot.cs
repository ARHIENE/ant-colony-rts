using System;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;

// 계절 연출 캡처 준비(2026-10-06). 인자: 바이옴 이름, 계절 0~3. 새 게임을 그 바이옴으로 열고 계절 한가운데로 시간을 옮긴다.
public static class SeasonShot
{
    public static async Task<string> Main(string biome, int season)
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        var b = (MapBiome)Enum.Parse(typeof(MapBiome), biome);
        if (BiomeRules.Current != b)
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 261012, mapSize = MapSize.Small, biome = b });
            for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        }
        UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>()?.ClearSelection();
        GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, GameCalendar.SecondsPerDay * (season + .4f));
        await Task.Yield(); await Task.Yield();
        return $"biome={BiomeRules.Current} season={GameCalendar.CurrentSeason} winter={Shader.GetGlobalFloat("_SeasonWinter")}";
    }
}
