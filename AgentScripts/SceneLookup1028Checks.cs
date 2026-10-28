using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

public static class SceneLookup1028Checks
{
    private static UnityEngine.Object sink;
    public static string Main()
    {
        var count = 0;
        void Compare<T>() where T : UnityEngine.Object
        {
            if (!ReferenceEquals(SceneObjects.Any<T>(), SceneObjects.All<T>().FirstOrDefault())) throw new Exception("Scene lookup mismatch " + typeof(T).Name);
            count++;
        }
        Compare<BuildingBase>(); Compare<Stockpile>(); Compare<ScienceLab>(); Compare<DemolitionSite>(); Compare<BuildingConstructionSite>(); Compare<EquipmentLoot>();
        void Query() { sink = SceneObjects.Any<Stockpile>(); sink = SceneObjects.Any<ScienceLab>(); sink = SceneObjects.Any<DemolitionSite>(); sink = SceneObjects.Any<EquipmentLoot>(); }
        for (var i = 0; i < 10; i++) Query();
        var controlStart = GC.GetAllocatedBytesForCurrentThread(); var control = new byte[4096];
        var available = GC.GetAllocatedBytesForCurrentThread() > controlStart; GC.KeepAlive(control);
        var start = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) Query();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        return "PASS " + count + " same first-active lookup results; allocation="
            + (available ? allocated + " bytes/4000 queries" : "unavailable (Mono counter does not register control allocation)");
    }
}
