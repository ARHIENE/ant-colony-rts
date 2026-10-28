using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AntColony.Core;
using AntColony.Buildings;
public static class ProgressionBudget1028Checks
{
    public static string Main()
    {
        var definitions = CampaignResearch.Technologies; var seen = new HashSet<ScienceTechnology>(); var visiting = new HashSet<ScienceTechnology>();
        void Visit(ScienceTechnology t)
        {
            if (seen.Contains(t)) return;
            if (!visiting.Add(t)) throw new Exception("Research cycle " + t);
            var d = definitions[(int)t]; if (d.Technology != t) throw new Exception("Registry index " + t);
            foreach (var p in d.Prerequisites) Visit(p);
            visiting.Remove(t); seen.Add(t);
        }
        foreach (var t in Enum.GetValues(typeof(ScienceTechnology)).Cast<ScienceTechnology>()) Visit(t);
        var all = seen.Count; seen.Clear();
        foreach (var t in new[] { ScienceTechnology.Hull, ScienceTechnology.Cocoons, ScienceTechnology.Engine }) Visit(t);
        var route = definitions.Where(d => seen.Contains(d.Technology)).ToArray();
        var report = new StringBuilder("PASS registry index/acyclic prerequisite graph " + all + " technologies\nMinimum rocket research closure, excludes bosses/blueprints/construction/life interruptions\n");
        foreach (var d in route) report.AppendLine($"{d.Name},era={d.Tier},work={d.Work},food={d.Food},material={d.Soil},special={d.Special}");
        report.AppendLine($"Total: {route.Length} topics, work={route.Sum(d => d.Work)}, food={route.Sum(d => d.Food)}, material={route.Sum(d => d.Soil)}, special={route.Sum(d => d.Special)}");
        report.AppendLine("One worker at research rate1 using minimum required lab tier for each topic, pure hours=" + route.Sum(d => d.Work / (1 + .25f * (d.Tier - 1))) / 3600);
        report.AppendLine("At50% research duty=" + route.Sum(d => d.Work / (1 + .25f * (d.Tier - 1))) / 1800 + " hours, before travel/bosses/construction.");
        report.AppendLine("Current prototype lab upgrades1to6 total food=" + Enumerable.Range(1, 5).Sum(i => i * 60) + ",materials=" + Enumerable.Range(1, 5).Sum(i => i * 80) + "; planned metal/electronics tier gates not implemented.");
        var result = report.ToString(); File.WriteAllText("Temp/progression-budget-result.txt", result); return result;
    }
}
