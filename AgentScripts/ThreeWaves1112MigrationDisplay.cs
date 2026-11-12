using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Units;
using AntColony.UI;
using AntColony.World;
using AntColony.Save;
using UnityEngine;
using UnityEngine.UI;

public static class ThreeWaves1112MigrationDisplay
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(SaveStorage.RootOverride) || Time.timeScale != 0) throw new Exception("Paused disposable game required");
        var events = ColonyEvents.Instance;
        typeof(ColonyEvents).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(events, new ColonyEvents.State());
        if (!events.TryTrigger(ColonyEvent.Migration)) throw new Exception("Migration event could not start");
        await Task.Delay(1100);
        var panel = GameObject.Find("MigrationOffer");
        if (panel == null) throw new Exception("Migration panel invisible");
        var text = string.Join("\n", panel.GetComponentsInChildren<Text>().Select(t => t.text));
        var accept = panel.GetComponentsInChildren<Button>().First(b => b.name == "AcceptMigration");
        if (!text.Contains("장수 1명") || text.Contains("시민") || accept.GetComponent<MenuTooltip>().Message.Contains("시민")) throw new Exception("Retired civilian offer text remains");
        var entry = CampaignHistory.Instance.Data.recent.Last(e => e.kind == "이벤트" && e.name == "이주민 무리");
        if (entry.result.Contains("시민") || !entry.result.Contains("장수 1명")) throw new Exception("Event history still promises civilians");
        ScreenCapture.CaptureScreenshot("work/three-waves-1112/screens/migration-display.png"); await Task.Delay(300);
        var before = CommanderRoster.Instance.Count; accept.onClick.Invoke();
        if (CommanderRoster.Instance.Count != before + 1 || events.MigrationPending) throw new Exception("Normal accept did not recruit exactly one commander");
        var result = "PASS live offer, accept tooltip and event history say one commander; normal accept recruits exactly one. Disposable event fixture, not natural run.";
        File.WriteAllText("work/three-waves-1112/migration-display-checks.txt", result); return result;
    }
}
