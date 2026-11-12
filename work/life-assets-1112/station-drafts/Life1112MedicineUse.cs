using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// Isolated-game fixture: ingredients are supplied by the fixture; walking and production run normally.
public static class Life1112MedicineUse
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying || UserSettings.Current.autoSaveEnabled || string.IsNullOrEmpty(AntColony.Save.SaveStorage.RootOverride))
            throw new Exception("Prepare an isolated game with autosave disabled first");
        var c = CommanderRoster.Instance.Commanders.First(x => x && x.IsColonyMember && x.CanDoJob(CommanderJobs.Nursing));
        foreach (var unit in CommanderRoster.Instance.Commanders) { unit.CommandStop(); unit.SetJobEnabled(CommanderJobs.All, false); }
        AntColony.UI.SimulationPause.UserSpeed(0);
        var oldJobs = c.WorkState.jobs;
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { BuildingKind.MedicineBench, UnitRole.Worker });
        var go = Object.Instantiate(template, c.Position + Vector3.right * 6, Quaternion.identity);
        go.SetActive(true);
        var bench = go.GetComponent<Processor>();
        try
        {
            FacilityVisual.Attach(bench);
            await Task.Delay(150);
            var model = go.GetComponent<FacilityVisual>().Model;
            if (!model || !model.name.StartsWith("MedicineBench")) throw new Exception("Meshy medicine bench not shown");
            int recipe = bench.Recipes.ToList().FindIndex(r => r.output == ResourceType.MedicineBasic);
            if (!bench.AddOrder(recipe, Workshop.OrderMode.Count, 1)) throw new Exception("Medicine order rejected");
            var courier = new object(); var resources = ResourceManager.Instance;
            if (resources.AddTo(resources.BaseBin, ResourceType.Herb, 2) != 2 || !bench.BeginPickup(courier, ResourceType.Herb, 2))
                throw new Exception("Fixture ingredients could not be stocked/reserved");
            resources.TakeFrom(resources.BaseBin, ResourceType.Herb, 2);
            if (bench.Deliver(courier, ResourceType.Herb, 2) != 2 || !bench.NeedsWork) throw new Exception("Fixture ingredients not ready");
            c.SetJobEnabled(CommanderJobs.Nursing, true);
            if (!c.StartService(bench, CommanderJobs.Nursing)) throw new Exception("Cannot start medicine work");
            var reached = typeof(SoldierAnt).GetMethod("HasReachedDestination", BindingFlags.Instance | BindingFlags.NonPublic);
            bool sawWork = false;
            float workDistance = -1;
            var deadline = DateTime.UtcNow.AddSeconds(45);
            AntColony.UI.SimulationPause.UserSpeed(1);
            // Count orders are removed when a batch STARTS; Current remains until completion.
            while ((bench.Orders.Count > 0 || bench.Current != null) && DateTime.UtcNow < deadline)
            {
                if (bench.Progress > 0 && !sawWork)
                {
                    if (!(bool)reached.Invoke(c, null)) throw new Exception("Production started before arrival");
                    sawWork = true;
                    workDistance = Vector3.Distance(c.Position, bench.Position);
                    File.WriteAllText("work/life-assets-1112/medicine-working.txt", "Reached station; distance=" + workDistance + "; motion=" + c.GetComponent<AntVisual>().MotionName);
                }
                await Task.Delay(40);
            }
            if (!sawWork || bench.Orders.Count != 0 || bench.Current != null) throw new Exception("Medicine arrival/production not observed; remaining=" + bench.Orders.Count + ", current=" + (bench.Current != null) + ", distance=" + Vector3.Distance(c.Position, bench.Position));
            if (!ResourceNode.Available.Any(n => n.IsLooseCargo && n.ResourceType == ResourceType.MedicineBasic && Vector3.Distance(n.transform.position, bench.Position) < 5))
                throw new Exception("No actual medicine output near station");
            var result = "Meshy MedicineBench: reached workstation, real-frame Nursing production, one order finished, medicine output exists; work distance=" + workDistance;
            File.WriteAllText("work/life-assets-1112/medicine-use.txt", result);
            return result;
        }
        finally
        {
            AntColony.UI.SimulationPause.UserSpeed(0);
            c.CommandStop(); c.WorkState.jobs = oldJobs;
            Object.Destroy(go);
        }
    }
}
