using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class AssessmentClarity1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "AssessmentClarity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var people = CommanderRoster.Instance.Commanders; var a = people[0];
            foreach (var c in people) { Array.Fill(c.Talents.levels, 2); Array.Clear(c.Talents.experience, 0, c.Talents.experience.Length); c.Talents.potential = 200; }
            var assessment = WorldMapManager.Instance.Assessment;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var evaluate = typeof(TalentAssessment).GetMethod("Evaluate", flags); var publish = typeof(TalentAssessment).GetMethod("Publish", flags);
            void Update() => publish.Invoke(assessment, new object[] { evaluate.Invoke(assessment, new object[] { a, a.PersonalState.id, a.CommanderName, a.Talents }) });
            Update(); var old = assessment.Rating(a.PersonalState.id).Value.Current;
            foreach (var c in people.Skip(1)) Array.Fill(c.Talents.levels, 5);
            Check(assessment.Rating(a.PersonalState.id).Value.Current == old, "old report preserves old comparison basis");
            Update(); Check(assessment.Rating(a.PersonalState.id).Value.Current < old && a.Talents.Level(CommanderActivity.Research) == 2, "relative score drops without personal skill loss");
            var before = a.Talents.Copy(); GameMenuController.Instance.ShowAssessment(a.PersonalState.id); Canvas.ForceUpdateCanvases();
            var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Select(t => t.text).ToArray();
            Check(texts.Any(t => t.Contains("개인 기술이 감소한 것은 아닙니다")), "relative versus personal explanation");
            Check(texts.Any(t => t.Contains("종합 별점이 모든 직무의 적합성을 뜻하지는 않습니다")), "role distinction");
            Check(texts.Any(t => t.Contains("잠재력도 도달 보장이 아닙니다")), "potential limit explanation");
            Check(before.Current == a.Talents.Current && before.potential == a.Talents.potential, "UI cannot change growth");
            return "PASS " + count + " assessment clarity checks";
        }
        finally { GameMenuController.Instance?.Resume(); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
