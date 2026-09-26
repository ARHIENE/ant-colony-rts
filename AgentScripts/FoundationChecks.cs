using System;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

// Run in Play mode with an idle started game; restores session options and clocks.
public static class FoundationChecks
{
    public static async System.Threading.Tasks.Task<string> Main()
    {
        // 새 Play 세션은 메인 메뉴로 시작하므로 필요하면 게임을 직접 시작한다.
        if (Application.isPlaying && !GameSession.Instance.GameStarted)
        {
            while (SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
            SaveSystem.NewGame(new NewGameOptions());
            while (SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
            GameMenuController.Instance.Resume();
        }
        var checks = 0;
        void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
        var session = GameSession.Instance;
        Check(Application.isPlaying && session.GameStarted && !SaveSystem.Busy, "started session");
        var options = session.Options.Clone(); var real = session.PlaySeconds; var game = session.GameSeconds;
        var scale = Time.timeScale;
        try
        {
            GameMenuController.Instance.Resume();
            session.MarkStarted(10, 299.99f);
            Check(GameCalendar.Month == 1 && GameCalendar.Year == 1, "month boundary before");
            session.MarkStarted(10, 300);
            Check(GameCalendar.Month == 2 && GameCalendar.TotalMonths == 1, "month boundary");
            session.MarkStarted(10, 900);
            Check(GameCalendar.CurrentSeason == Season.Summer && GameCalendar.Month == 4, "summer boundary");
            session.MarkStarted(10, 3600);
            Check(GameCalendar.Year == 2 && GameCalendar.Month == 1 && GameCalendar.MonthProgress == 0, "year boundary");
            GameMenuController.Instance.SetSpeed(3);
            Check(Time.timeScale == 3, "three times speed");
            GameMenuController.Instance.ToggleSimulation();
            Check(Time.timeScale == 0, "simulation pause");
            GameMenuController.Instance.ToggleSimulation();
            Check(Time.timeScale == 3, "resume selected speed");
            GameMenuController.Instance.Pause(); GameMenuController.Instance.Resume();
            Check(Time.timeScale == 3, "menu preserves speed");
            var selected = options.Clone(); selected.commanderDeath = CommanderDeathMode.Harsh;
            session.SetOptions(selected);
            var file = SaveSnapshot.Capture();
            Check(file.gameSeconds == 3600 && file.playSeconds == 10 && file.options.commanderDeath == 2, "capture distinct clocks and death option");
            Check(SaveValidator.TryParse(JsonUtility.ToJson(file), out var parsed, out var error), "v2 roundtrip: " + error);
            Check(parsed.gameSeconds == file.gameSeconds && parsed.options.commanderDeath == 2, "v2 values preserved");
            file.version = 1; foreach (var c in file.commanders) { c.level = 1; c.xp = 0; } file.playSeconds = 730; file.gameSeconds = 0; file.options.commanderDeath = 0;
            Check(SaveValidator.TryParse(JsonUtility.ToJson(file), out parsed, out error), "legacy migration: " + error);
            Check(parsed.version == SaveFileV1.CurrentVersion && parsed.gameSeconds == 730 && parsed.options.commanderDeath == 1, "legacy defaults preserve calendar and normal death mode");
            parsed.gameSeconds = float.NaN;
            Check(!SaveValidator.Validate(parsed, out _), "reject nonfinite calendar");
            parsed.gameSeconds = 0; parsed.options.commanderDeath = 99;
            Check(!SaveValidator.Validate(parsed, out _), "reject unknown death option");
            return "PASS " + checks + " foundation checks";
        }
        finally { session.SetOptions(options); session.MarkStarted(real, game); Time.timeScale = scale; }
    }
}
