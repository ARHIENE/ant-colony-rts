using System;
using System.IO;
using System.Reflection;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEngine;

public static class NaturalSource1115Checks
{
    const string Log = "work/colony-polish-1115/natural-source.txt";
    static void Check(bool ok, string message)
    {
        File.AppendAllText(Log, (ok ? "PASS " : "FAIL ") + message + "\n");
        if (!ok) throw new Exception(message);
    }
    public static string Main()
    {
        File.WriteAllText(Log, "Temporary isolated building fixtures; no campaign resources or progress changed.\n");
        foreach (var kind in new[] { BuildingKind.WaterSource, BuildingKind.OilDeposit, BuildingKind.ManualWell, BuildingKind.WaterPump, BuildingKind.OilDrill })
        {
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { kind, UnitRole.Worker });
            var instance = UnityEngine.Object.Instantiate(template);
            try
            {
                instance.transform.position = new Vector3(10000, 0, 10000);
                instance.SetActive(true);
                var b = instance.GetComponent<BuildingBase>();
                var natural = kind == BuildingKind.WaterSource || kind == BuildingKind.OilDeposit;
                Check(b.CountsTowardPlayerDefeat, kind + " remains in home save catalog");
                GameManager.Instance.RegisterBuilding(b);
                Check((GameManager.Instance.FindNearestPlayerBuilding(b.Position) == b) != natural, kind + " nearest hostile target");
                var ranked = GameManager.Instance.FindPlayerBuildingByRank(b.Position, target => target == b ? 0 : int.MaxValue, out _);
                Check((ranked == b) != natural, kind + " ranked hostile target");
                float before = b.CurrentHealth;
                b.TakeDamage(10);
                Check(natural ? b.CurrentHealth == before : b.CurrentHealth < before, kind + " damage policy");
                if (natural) { b.TakeDamage(float.MaxValue); Check(!b.IsDead && b.CurrentHealth == before, kind + " survives lethal area damage"); }
            }
            finally { instance.SetActive(false); UnityEngine.Object.Destroy(instance); }
        }
        return "PASS natural geology excluded; constructed extraction facilities remain vulnerable";
    }
}
