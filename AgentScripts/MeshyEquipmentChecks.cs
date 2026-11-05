using System;
using AntColony.Units;
using UnityEngine;

public static class MeshyEquipmentChecks
{
    public static string Main()
    {
        int count = 0;
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        Check((int)WeaponKind.ShortBow == 5 && (int)WeaponKind.Mandible == 0 && (int)EquipmentRecipe.Insignia == 9, "Saved IDs changed");
        foreach (EquipmentRecipe recipe in Enum.GetValues(typeof(EquipmentRecipe)))
        for (int quality = 0; quality < 4; quality++)
        {
            var item = EquipmentRecipes.Create(recipe, quality);
            Check(item.IsValid, "Invalid recipe " + recipe);
            Check(!string.IsNullOrEmpty(item.Label), "Missing label " + recipe);
            var restored = JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(item));
            Check(restored.IsValid && restored.weapon == item.weapon && restored.slot == item.slot && restored.Label == item.Label, "Save roundtrip " + recipe);
            foreach (var roll in new[] { 0f, 1f })
            {
                restored = JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(item)); restored.MakeUnique(roll);
                Check(restored.IsValid && restored.unique == (quality == 3), "Unique quality gate " + recipe);
                if (quality == 3) Check(restored.uniqueEffect != UniqueEquipmentEffect.None, "Missing unique effect");
            }
        }
        var old = JsonUtility.FromJson<EquipmentItem>("{\"id\":\"legacy\",\"slot\":0,\"quality\":3,\"weapon\":0,\"unique\":true}");
        Check(old.IsValid && old.Power == 5, "Legacy unique behavior changed");
        old.uniqueEffect = UniqueEquipmentEffect.Reinforced;
        Check(!old.IsValid, "Incompatible effect accepted");
        foreach (var kind in new[] { WeaponKind.SingleRifle, WeaponKind.BurstCarbine, WeaponKind.Scattergun })
            Check(MeshyEquipmentRules.IsRanged(kind) && MeshyEquipmentRules.Range(kind) > 0 && MeshyEquipmentRules.Interval(kind) > 0, "Invalid firearm " + kind);
        return count + " equipment recipe / JSON / unique compatibility checks passed";
    }
}
