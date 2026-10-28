using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
public static class CombatBudget1028Checks
{
    public static string Main()
    {
        Time.timeScale = 0; var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var c = CommanderRoster.Instance.Commanders.First(a => !a.IsDead && !a.IsChild);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var restore = typeof(CommanderAnt).GetMethod("RestoreTroops", flags);
        var originalTroops = c.TroopCount; var pending = typeof(CommanderAnt).GetField("pendingDamage", flags).GetValue(c);
        var text = new StringBuilder("Same commander/equipment/health, actual AttackDamage by troop count\n");
        try
        {
            restore.Invoke(c, new object[] { 1, 0f }); var one = c.AttackDamage;
            foreach (var troops in new[] { 0, 1, 5, 10, 20 })
            {
                restore.Invoke(c, new object[] { troops, 0f }); var damage = c.AttackDamage;
                Check(Mathf.Abs(damage - one * Mathf.Max(1, troops)) < .001f, "troop attack scale " + troops);
                text.AppendLine($"{troops} troops => attack {damage:0.###}");
            }
            foreach (var reduction in new[] { 0f, .25f, .75f, 1f, 2f })
                Check(FormationRules.ReceivedMultiplier(0, reduction) >= .25f, "reduction floor " + reduction);
            var circle = FormationRules.Modifiers(Formation.Circle); var fish = FormationRules.Modifiers(Formation.FishScale);
            Check(circle.Armor > fish.Armor && fish.Attack > circle.Attack, "circle/fish tradeoff");
            var arrow = FormationRules.Modifiers(Formation.Arrow); var legion = FormationRules.Modifiers(Formation.Arrow, true);
            Check(legion.Attack > arrow.Attack && legion.Armor == arrow.Armor, "legion preserves penalty");
            var forest = FormationRules.Modifiers(Formation.Snake, false, false, true); var open = FormationRules.Modifiers(Formation.Snake);
            Check(forest.Attack > 0 && open.Attack < 0, "terrain conditional formation");
            text.AppendLine("Troop losses20→10 halve attack; armor is subtracted per incoming hit with minimum min(1,raw), then received multiplier floor25%. Small weak hits can be suppressed heavily; no TTK balance proven by formula checks.");
            var result = "PASS " + count + " combat budget checks\n" + text; File.WriteAllText("Temp/combat-budget-result.txt", result); return result;
        }
        finally { restore.Invoke(c, new object[] { originalTroops, pending }); }
    }
}
