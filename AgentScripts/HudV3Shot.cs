using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using Object = UnityEngine.Object;

// HUD v3 화면 확인용: 새 게임 → 장수 1명 선택 → 상세 창 열기.
public static class HudV3Shot
{
    public static async Task<string> Main()
    {
        if (!GameSession.Instance.GameStarted) { SaveSystem.NewGame(new NewGameOptions { seed = 261001, mapSize = MapSize.Small }); await Task.Delay(4000); }
        GameMenuController.Instance.Resume(); Time.timeScale = 1;
        var c = RosterBar.Commanders.First();
        Object.FindAnyObjectByType<RosterBar>().Click(0);
        await Task.Delay(300);
        Object.FindAnyObjectByType<DetailTabs>().Toggle(c);
        Time.timeScale = 0;
        return "selected " + c.CommanderName;
    }
}
