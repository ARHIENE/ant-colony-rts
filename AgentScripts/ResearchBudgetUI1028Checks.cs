using System;
using System.Linq;
using System.Reflection;
using AntColony.Core;
using AntColony.UI;
using UnityEngine;
public static class ResearchBudgetUI1028Checks
{
    public static string Main()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var r = CampaignResearch.Instance; Check(r != null, "started game required");
        Time.timeScale = 0;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var field = typeof(CampaignResearch).GetField("state", flags); var old = field.GetValue(r);
        var method = typeof(GameMenuController).GetMethod("ResearchBudgetText", BindingFlags.NonPublic | BindingFlags.Static);
        var menu = GameMenuController.Instance; var pick = typeof(GameMenuController).GetField("researchPick", flags); var era = typeof(GameMenuController).GetField("researchEra", flags);
        var oldPick = pick.GetValue(menu); var oldEra = era.GetValue(menu);
        string Text() => (string)method.Invoke(null, new object[] { r, CampaignResearch.Technologies[(int)ScienceTechnology.Engine] });
        try
        {
            var s = new CampaignResearch.State(); field.SetValue(r, s);
            Check(Text().Contains("포함 5개") && Text().Contains("식량 1300") && Text().Contains("연구량 13400"), "closure includes prerequisite budget");
            s.completed.Add((int)ScienceTechnology.Vehicle);
            Check(Text().Contains("포함 4개") && Text().Contains("식량 1200") && Text().Contains("연구량 13000"), "completed prerequisite excluded");
            s.active = (int)ScienceTechnology.Engine; s.progress = 1000;
            Check(Text().Contains("식량 800") && Text().Contains("연구량 12000"), "active cost already paid and progress deducted");
            s.partialIds.Add((int)ScienceTechnology.Gliding); s.partialProgress.Add(500);
            Check(Text().Contains("식량 600") && Text().Contains("연구량 11500"), "paused topic cost not duplicated");
            var before = JsonUtility.ToJson(s); Text(); Check(before == JsonUtility.ToJson(s), "read only");
            pick.SetValue(menu, (ScienceTechnology?)ScienceTechnology.Engine); era.SetValue(menu, 6); menu.Science(); Canvas.ForceUpdateCanvases();
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("미완 선행 포함 4개")), "actual science UI budget");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("승급·건설·표본/핵심물 확보 비용은 별도")), "budget boundary visible");
            return "PASS " + count + " research budget UI checks";
        }
        finally { field.SetValue(r, old); pick.SetValue(menu, oldPick); era.SetValue(menu, oldEra); menu.Resume(); Time.timeScale = 0; }
    }
}
