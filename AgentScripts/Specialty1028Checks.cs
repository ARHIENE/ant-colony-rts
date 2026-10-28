using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class Specialty1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Specialty " + count + ": " + label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    static void Reset(CommanderAnt c, CommanderActivity activity, int level = 9, params CommanderTrait[] traits)
    {
        c.ApplyTraits(new CommanderTraits { values = traits.ToList() });
        Array.Clear(c.Talents.levels, 0, CommanderTalents.Count); Array.Clear(c.Talents.experience, 0, CommanderTalents.Count);
        Array.Clear(c.Talents.usage, 0, CommanderTalents.Count); c.Talents.recentWork.Clear(); c.Talents.potential = 200;
        c.Talents.levels[(int)activity] = level; c.Talents.experience[(int)activity] = CommanderTalents.Required(level) - 1;
    }
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Specialty-" + Guid.NewGuid().ToString("N"));
        var roll = typeof(SpecialtyAcquisition).GetField("Roll", BindingFlags.Static | BindingFlags.NonPublic); var oldRoll = roll.GetValue(null);
        int rolls = 0; float next = 0;
        roll.SetValue(null, (Func<float>)(() => { rolls++; return next; }));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0; var c = CommanderRoster.Instance.Commanders[0];
            var id = c.PersonalState.id;
            foreach (var activity in SpecialtyAcquisition.Activities)
            {
                Reset(c, activity); c.GainExperience(activity, 2);
                var i = Array.IndexOf(SpecialtyAcquisition.Activities, activity);
                var trait = (CommanderTrait)((int)CommanderTrait.GoldGathering + i);
                Check(c.Talents.Level(activity) == 10 && c.Traits.Has(trait) && c.Traits.specialties.results[i * 2] == 4, "real growth gold " + activity);
                Check(!c.Traits.CanInherit(trait), "earned expertise excluded from inherited pool " + activity);
            }
            Reset(c, CommanderActivity.Gathering); next = .2f; rolls = 0; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.specialties.results[0] == 2 && rolls == 1, "20 percent strict boundary failure");
            c.Talents.levels[0] = 9; c.Talents.experience[0] = 999; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(rolls == 1 && !c.Traits.Has(CommanderTrait.GoldGathering), "drop and recross cannot reroll");
            Reset(c, CommanderActivity.Gathering); next = .19999f; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.Has(CommanderTrait.GoldGathering), "below20 percent succeeds");
            var fixedTraits = new[] { CommanderTrait.Lazy, CommanderTrait.Fragile, CommanderTrait.Coward, CommanderTrait.LightEater };
            Reset(c, CommanderActivity.Gathering, 9, fixedTraits); next = 0; rolls = 0; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.specialties.results[0] == 3 && c.Traits.values.SequenceEqual(fixedTraits) && rolls == 0, "four slots consume opportunity without displacing personality");
            c.Traits.values.RemoveAt(3); c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.specialties.results[0] == 3 && rolls == 0, "later empty slot does not restore opportunity");
            Reset(c, CommanderActivity.Gathering, 19, CommanderTrait.GoldGathering, CommanderTrait.Lazy, CommanderTrait.Fragile, CommanderTrait.Coward);
            next = .05f; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.specialties.results[1] == 2 && c.Traits.Has(CommanderTrait.GoldGathering), "5 percent boundary and old gold preserved");
            Reset(c, CommanderActivity.Gathering, 19, CommanderTrait.GoldGathering, CommanderTrait.Lazy, CommanderTrait.Fragile, CommanderTrait.Coward);
            next = .04999f; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.values.Count == 4 && c.Traits.Has(CommanderTrait.DiamondGathering) && !c.Traits.Has(CommanderTrait.GoldGathering), "diamond upgrades own slot at full capacity");
            Check(c.Traits.CanInherit(CommanderTrait.GoldGathering) && !c.Traits.CanInherit(CommanderTrait.DiamondGathering), "innate gold remains heritable, earned diamond does not");
            var state = SaveSnapshot.Capture(); Check(SaveValidator.Validate(state, out var error), "four traits save validation " + error);
            var snapshotTraits = state.commanders.First(p => p.personalState.id == id).traits;
            snapshotTraits.specialties.results[0] = 99; Check(!SaveValidator.Validate(state, out error), "invalid result rejected"); snapshotTraits.specialties.results[0] = 1;
            snapshotTraits.specialties = null; Check(SaveValidator.Validate(state, out error), "old missing specialty state accepted");
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "actual save/load " + error); await Ready();
            c = CommanderRoster.Instance.Commanders.First(p => p.PersonalState.id == id); rolls = 0; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.Has(CommanderTrait.DiamondGathering) && c.Traits.specialties.results[1] == 4 && rolls == 0, "acquired result survives load without reroll");
            GameMenuController.Instance.Details(c); Canvas.ForceUpdateCanvases();
            var tip = UnityEngine.Object.FindObjectsByType<MenuTooltip>().First(t => t.name == "Skill Gathering");
            Check(tip.GetComponent<UnityEngine.UI.Image>().raycastTarget && tip.Message.Contains("획득 · 영구 유지"), "skill cell has actual hover target and outcome");
            tip.OnPointerEnter(null); Canvas.ForceUpdateCanvases();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var label = (UnityEngine.UI.Text)typeof(GameMenuController).GetField("tip", flags).GetValue(GameMenuController.Instance);
            var panel = (RectTransform)typeof(GameMenuController).GetField("tooltipPanel", flags).GetValue(GameMenuController.Instance);
            Check(panel.gameObject.activeInHierarchy && panel.rect.height >= label.preferredHeight + 16 - 1, "multiline explanation fits tooltip");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            Reset(c, CommanderActivity.Gathering, 10); rolls = 0; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(c.Traits.specialties.results[0] == 1 && rolls == 0, "existing or starting threshold not retroactively rolled");
            Reset(c, CommanderActivity.Gathering, 9, CommanderTrait.Lazy, CommanderTrait.Fragile, CommanderTrait.Coward);
            c.Talents.levels[(int)CommanderActivity.Strength] = 9; c.Talents.experience[(int)CommanderActivity.Strength] = 999;
            next = 0; c.GainExperience(CommanderActivity.Gathering, 10);
            Check(c.Traits.values.Count == 4 && c.Traits.specialties.results.Count(r => r == 4) == 1 && c.Traits.specialties.results.Count(r => r == 3) == 1, "simultaneous main/support candidates share final free slot");
            Check(CampaignHistory.Instance.Data.milestones.Any(e => e.kind == "전문성" && e.result.Contains("기회 소모")), "failure/blocked reason in history");
            c.ApplyTraits(new CommanderTraits(CommanderPersonality.Brave)); SpecialtyAcquisition.Initialize(c);
            Check(c.Traits.CanInherit(CommanderTrait.Brave), "legacy personality remains an innate inherited candidate");
            var legacyJson = JObject.Parse(File.ReadAllText(SaveSlots.PathFor(false, 0)));
            foreach (var token in legacyJson.SelectTokens("$..specialties").ToArray()) token.Parent.Remove();
            var legacyPath = Path.Combine(SaveStorage.Root, "legacy-without-specialties.json"); File.WriteAllText(legacyPath, legacyJson.ToString());
            Check(SaveSystem.TryLoad(legacyPath, out error), "load real legacy JSON with specialty fields absent " + error); await Ready();
            c = CommanderRoster.Instance.Commanders.First(p => p.PersonalState.id == id); rolls = 0; c.GainExperience(CommanderActivity.Gathering, 2);
            Check(rolls == 0 && c.Traits.specialties.results[0] == 1 && c.Traits.specialties.results[1] == 1, "legacy past thresholds are initialized without retroactive rolls");
            return "PASS " + count + " specialty acquisition checks";
        }
        finally { roll.SetValue(null, oldRoll); GameMenuController.Instance?.Resume(); Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
