using System;
using AntColony.Core;
using AntColony.Buildings;
using UnityEngine;
public static class CropRoles1028Checks
{
    public static string Main()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        foreach (FarmCrop crop in Enum.GetValues(typeof(FarmCrop)))
        {
            var go = new GameObject("Crop role check"); go.SetActive(false);
            try
            {
                var p = go.AddComponent<FarmPlot>(); p.Configure(crop, false);
                Check(p.Node.RegrowSeconds == FarmCropRules.GrowSeconds(crop) && p.Node.RegrowAmount == FarmCropRules.Yield(crop), "actual configuration " + crop);
                Check(FarmCropRules.Description(crop).Contains("겨울 성장 정지") && !string.IsNullOrEmpty(FarmCropRules.Role(crop)), "meaning and environment " + crop);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        Check(FarmCropRules.GrowSeconds(FarmCrop.Fungus) == GameBalance.FungusSeconds && FarmCropRules.Yield(FarmCrop.AdvancedFungus) == GameBalance.AdvancedFungusFood, "original crops preserved");
        Check(FarmCropRules.GrowSeconds(FarmCrop.Barley) < FarmCropRules.GrowSeconds(FarmCrop.Rice) && FarmCropRules.Yield(FarmCrop.Rice) > FarmCropRules.Yield(FarmCrop.Wheat), "short cycle versus bulk rice");
        Check(FarmCropRules.Description(FarmCrop.Grapes).Contains("6일") && FarmCropRules.Description(FarmCrop.Wheat).Contains("24일"), "correct shelf life by actual output");
        return "PASS " + count + " crop role checks";
    }
}
