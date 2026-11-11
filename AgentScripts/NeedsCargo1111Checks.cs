using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Units;
using UnityEngine;

public static class NeedsCargo1111Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<string> Main(string label)
    {
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        string log = "";
        void Check(bool ok, string name) { log += (ok ? "PASS " : "FAIL ") + name + "\n"; }
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "NeedsCargo1111-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            UnityEngine.Random.InitState(261106);
            StartingCandidate candidate;
            do { candidate = StartingCandidate.Roll(0); } while (StartingCandidate.Missing(new[] { candidate }).Length > 0);
            SaveSystem.NewGame(new NewGameOptions { seed = 261106, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 1, startingTeam = new[] { candidate } });
            var end = DateTime.UtcNow.AddSeconds(60);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
            if (SaveSystem.Busy) throw new Exception("New game timeout");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var c = CommanderRoster.Instance.Commanders.First();
            c.SetJobEnabled(CommanderJobs.All, false); c.PersonalState.carriedRations = new FoodLots();
            Check(!c.IsSleepTime, "fixture is awake daytime");
            var state = typeof(WorkerAnt).GetField("state", Hidden);
            void Load() { c.CommandStop(); typeof(WorkerAnt).GetMethod("RestoreCargo", Hidden).Invoke(c, new object[] { 5f, ResourceType.Soil, null }); state.SetValue(c, Enum.Parse(state.FieldType, "DeliveringMaterial")); }
            c.PersonalState.hygiene.hygiene = 29; c.PersonalState.hygiene.retrySeconds = 10;
            c.PersonalState.meal.satiety = 100;
            Load(); c.TickDuty(.1f);
            Check(c.CarriedAmount == 5 && c.IsWorking, "temporary hygiene retry preserves ongoing delivery");
            c.PersonalState.hygiene.hygiene = 100; c.PersonalState.hygiene.retrySeconds = 0;
            c.PersonalState.meal.satiety = 0; c.PersonalState.meal.retrySeconds = 10;
            Load(); c.TickDuty(.1f);
            Check(c.CarriedAmount == 5 && c.IsWorking, "unavailable meal retry preserves ongoing delivery");
            c.PersonalState.meal.satiety = 100; c.PersonalState.meal.retrySeconds = 0;
            c.PersonalState.hygiene.hygiene = 29; c.PersonalState.hygiene.retrySeconds = 0;
            Load(); var before = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Soil).Sum(n => n.AmountRemaining) + c.CarriedAmount;
            c.TickDuty(.1f);
            var after = ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == ResourceType.Soil).Sum(n => n.AmountRemaining) + c.CarriedAmount;
            Check(!c.IsCarrying, "expired retry interrupts cargo for needs"); log += $"INFO washing={c.IsWashing} retry={c.PersonalState.hygiene.retrySeconds} canOrder={c.CanReceiveOrders}\n";
            Check(Mathf.Abs(before - after) < .01f, "needs interruption conserves cargo");
            File.WriteAllText("Temp/needs-cargo-1111-" + label + ".txt", log); return log;
        }
        finally { Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
