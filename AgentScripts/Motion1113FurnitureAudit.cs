using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using AntColony.Buildings;
using AntColony.Data;
public static class Motion1113FurnitureAudit
{
    public static string Run()
    {
        var report = new StringBuilder();
        var get = typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var kind in new[] { BuildingKind.SingleBed, BuildingKind.DoubleBed, BuildingKind.BunkBed, BuildingKind.SleepingMat, BuildingKind.Hammock, BuildingKind.Dormitory, BuildingKind.BigTable, BuildingKind.Bookshelf })
        {
            var template = (GameObject)get.Invoke(null, new object[] { kind, UnitRole.Worker });
            var go = UnityEngine.Object.Instantiate(template, new Vector3(0, -100, 0), Quaternion.identity);
            try
            {
                go.name = "Furniture audit " + kind;
                go.SetActive(true);
                FacilityVisual.Attach(go.GetComponent<BuildingBase>());
                var visual = go.GetComponent<FacilityVisual>();
                if (visual && !visual.Model) typeof(FacilityVisual).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
                report.AppendLine(kind + " model=" + (visual && visual.Model ? visual.Model.name : "none"));
                if (visual && visual.Model)
                    foreach (var r in visual.Model.GetComponentsInChildren<Renderer>())
                        report.AppendLine(r.name + " center=" + (r.bounds.center-go.transform.position).ToString("F4") + " size=" + r.bounds.size.ToString("F4"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        System.IO.File.WriteAllText("work/motion-actions-1113/furniture-bounds.txt", report.ToString());
        return report.ToString();
    }
}
