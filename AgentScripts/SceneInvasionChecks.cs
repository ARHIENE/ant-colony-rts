namespace AntColony.Regression
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using AntColony.World;
    using AntColony.Buildings;
    using AntColony.Core;
    using UnityEngine;
    public static class SceneInvasionChecks
    {
        public static async Task<string> Main()
        {
            if(!Application.isPlaying) throw new Exception("Play mode required; restart Play after this destructive test.");
            var waves=GameObject.Find("EnemyNestPrototype").GetComponent<ColonyInvasion>();
            var field=typeof(ColonyInvasion).GetField("timer",BindingFlags.Instance|BindingFlags.NonPublic);
            var buildings=UnityEngine.Object.FindObjectsByType<BuildingBase>().Where(b=>b.CountsTowardPlayerDefeat).ToDictionary(b=>b,b=>b.CurrentHealth);
            var upkeep=UnityEngine.Object.FindAnyObjectByType<UpkeepManager>();
            var enabled=upkeep.enabled; upkeep.enabled=false;
            try
            {
                field.SetValue(waves,0f);
                var deadline=DateTime.UtcNow.AddSeconds(45);
                while(!buildings.Any(pair=>pair.Key==null||pair.Key.CurrentHealth<pair.Value))
                {
                    if(DateTime.UtcNow>deadline) throw new Exception("Actual scene invasion did not reach and damage a base building.");
                    await Task.Delay(100);
                }
                return "PASS: configured scene raiders spawned, navigated real terrain, and damaged player base with default movement/combat stats.";
            }
            finally { upkeep.enabled=enabled; }
        }
    }
}
