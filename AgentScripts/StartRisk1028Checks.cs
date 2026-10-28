using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
public static class StartRisk1028Checks
{
    public static string Main()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var method = typeof(GameMenuController).GetMethod("StartRiskSummary", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (DifficultyLevel invasion in Enum.GetValues(typeof(DifficultyLevel)))
            foreach (CommanderDeathMode death in Enum.GetValues(typeof(CommanderDeathMode)))
                foreach (var number in new[] { 1, 2, 3 })
                {
                    var options = new NewGameOptions { difficulty = invasion, commanderDeath = death, startingCount = number };
                    var text = (string)method.Invoke(null, new object[] { options });
                    Check(text.Contains($"장수 {number}명") && text.Contains($"인원 ×{DifficultyProfile.StrengthScale(invasion):0.##}") && text.Contains($"간격 ×{DifficultyProfile.IntervalScale(invasion):0.##}"), "actual combination " + invasion + death + number);
                    Check(text.Contains("동료 구조 불가") == (number == 1) && text.Contains("즉사30%") == (death == CommanderDeathMode.Harsh), "conditional risk " + invasion + death + number);
                }
        var menu = GameMenuController.Instance; var field = typeof(GameMenuController).GetField("options", flags); var old = field.GetValue(menu);
        try
        {
            var options = new NewGameOptions { startingCount = 1, difficulty = DifficultyLevel.Harsh, commanderDeath = CommanderDeathMode.Harsh,
                startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() };
            field.SetValue(menu, options); var before = JsonUtility.ToJson(options);
            typeof(GameMenuController).GetMethod("NewGameScreen", flags).Invoke(menu, null); Canvas.ForceUpdateCanvases();
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("1명은 동료 구조 불가") && t.text.Contains("즉사30%")), "visible before start");
            Check(UnityEngine.Object.FindObjectsByType<MenuTooltip>().Any(t => t.Message != null && t.Message.Contains("마지막 장수 포로=패배")), "start tooltip repeats risk");
            Check(JsonUtility.ToJson(options) == before, "risk display cannot change chosen options");
            var result = "PASS " + count + " starting combination checks";
            System.IO.File.WriteAllText("Temp/start-risk-result.txt", result); return result;
        }
        finally { field.SetValue(menu, old); menu.Resume(); Time.timeScale = 0; }
    }
}
