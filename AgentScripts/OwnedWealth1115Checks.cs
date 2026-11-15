using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;
using AntColony.Core;
using AntColony.World;
public static class OwnedWealth1115Checks
{
    static void Check(bool ok, string text)
    {
        File.AppendAllText("work/industry-bootstrap-1115/checks.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
        if (!ok) throw new Exception(text);
    }
    public static string Main()
    {
        if (!Application.isPlaying || Time.timeScale != 0) throw new Exception("Paused isolated Play required");
        Directory.CreateDirectory("work/industry-bootstrap-1115");
        File.WriteAllText("work/industry-bootstrap-1115/checks.txt", "Isolated ownership/UI regression; temporary fixtures removed, not campaign progress.\n");
        var source = ResourceNode.Available.First(n => n.ResourceType == ResourceType.Soil && n.AmountRemaining > 10
            && n.GetComponentInParent<ExpeditionSite>() is ExpeditionSite s && s.Kind == ExpeditionSiteKind.ResourceSite && s.Disposition == ConquestDisposition.Undecided);
        var site = source.GetComponentInParent<ExpeditionSite>();
        var amount = source.AmountRemaining; var disposition = site.Disposition;
        var amountField = typeof(ResourceNode).GetField("amountRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
        var dispositionProperty = typeof(ExpeditionSite).GetProperty("Disposition");
        var originals = (ResourceNode[])typeof(ExpeditionSite).GetProperty("SiteResourceNodes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(site);
        GameObject cargo = null;
        try
        {
            var before = RocketEconomy.CaptureOwned().wealth;
            Check(source.Extract(1) == 1, "Fixture removes one unit of unclaimed natural soil");
            Check(Math.Abs(RocketEconomy.CaptureOwned().wealth - before) < .02f, "Unclaimed natural quantity does not affect owned wealth");
            amountField.SetValue(source, amount);
            cargo = new GameObject("OwnedWealthFixture"); cargo.SetActive(false); cargo.transform.SetParent(site.transform, false);
            cargo.AddComponent<ResourceNode>().ConfigureLoot(ResourceType.Soil, 7); cargo.SetActive(true);
            Check(Math.Abs(RocketEconomy.CaptureOwned().wealth - before - 7) < .02f, "Player floor cargo at a neutral site still contributes exactly seven");
            dispositionProperty.SetValue(site, ConquestDisposition.Annexed);
            float expected = originals.Where(n => n != null && n.isActiveAndEnabled).Sum(n => RocketEconomy.ResourceValue(n.ResourceType, n.AmountRemaining));
            Check(Math.Abs(RocketEconomy.CaptureOwned().wealth - before - 7 - expected) < .02f, "Annexed resource reserves count as owned stock");
        }
        finally
        {
            amountField.SetValue(source, amount); dispositionProperty.SetValue(site, disposition);
            if (cargo != null) UnityEngine.Object.DestroyImmediate(cargo);
        }
        var p = SceneObjects.All<Processor>().First(p => p.isActiveAndEnabled && p.Data.kind == BuildingKind.Carpentry);
        Check(p.Current == null && p.Orders.Count == 0 && p.WaitReason == "생산 주문 없음", "No-order message is explicit");
        Check(p.AddOrder(0, Workshop.OrderMode.KeepStock, 1), "Add ordinary stock target below existing plank stock");
        var order = p.Orders.Last();
        try { Check(p.WaitReason.StartsWith("재고 목표 충족"), "Satisfied stock target is not reported as missing materials"); }
        finally { p.RemoveOrder(order); }
        var tech = CampaignResearch.Technologies[(int)ScienceTechnology.ManagedResources];
        Check(tech.Soil == 0 && tech.Food == 25 && tech.Work == 200, "Bootstrap research preserves food/work and removes circular soil cost");
        Check(CampaignResearch.Technologies[(int)ScienceTechnology.FungalFarming].Soil == GameBalance.ScienceSoil[0], "Other research retains original soil cost");
        var quarry = SceneObjects.All<Processor>().First(p => p.isActiveAndEnabled && p.Data.kind == BuildingKind.Quarry);
        Check(quarry.Current?.output == ResourceType.Soil && quarry.Progress > .4f && quarry.Progress < .5f,
            "Natural partial soil batch preserved through full scene reload");
        return File.ReadAllText("work/industry-bootstrap-1115/checks.txt");
    }
    public static string Audit()
    {
        var nodes = SceneObjects.All<ResourceNode>().Where(n => n.IsLooseCargo && !n.IsRaidLocked
            && n.GetComponentInParent<ExpeditionSite>() is ExpeditionSite s
            && s.Kind == ExpeditionSiteKind.ResourceSite && s.Disposition != ConquestDisposition.Annexed).ToArray();
        var total = RocketEconomy.CaptureOwned().wealth;
        var natural = nodes.Sum(n => RocketEconomy.ResourceValue(n.ResourceType, n.AmountRemaining));
        var report = $"Owned reported={total}; neutral resource sites={natural}; excluding neutral={total-natural}; expectation={GameBalanceNeeds.ExpectationTier(total)} -> {GameBalanceNeeds.ExpectationTier(total-natural)}\n"
            + string.Join("\n", nodes.Select(n=>$"{n.name}: {n.AmountRemaining} {n.ResourceType}"));
        Directory.CreateDirectory("work/industry-bootstrap-1115");
        File.WriteAllText("work/industry-bootstrap-1115/wealth-before.txt", report);
        return report;
    }
}
