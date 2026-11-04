namespace AntColony.Regression
{
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BiomeEventChecks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        checks = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BiomeEvents-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 261004, biome = MapBiome.Garden }); await Ready();
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(AntColony.Units.CommanderJobs.All, false); c.CommandStop(); }
            var events = ColonyEvents.Instance;
            Check(EventRules.IsCrisis(ColonyEvent.Footprint) && !EventRules.IsCrisis(ColonyEvent.TrashPile), "new crisis and fortune classification");
            Check(events.Eligible(ColonyEvent.Footprint) && !events.Eligible(ColonyEvent.WateringCan) && !events.Eligible(ColonyEvent.TrashPile), "garden spring eligibility");
            Check(events.TryTrigger(ColonyEvent.Footprint) && !events.TryTrigger(ColonyEvent.Footprint), "footprint trigger and cooldown");
            var point = HomeMapBuilder.CurrentWorldBounds.center;
            var near = new GameObject("Footprint near").AddComponent<BuildingBase>(); near.transform.position = point;
            var far = new GameObject("Footprint far").AddComponent<BuildingBase>(); far.transform.position = point + Vector3.right * 7;
            typeof(ColonyEvents).GetMethod("ApplyFootprint", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { point });
            Check(Mathf.Approximately(near.CurrentHealth, near.MaxHealth * .6f), "footprint removes 40 percent max health");
            Check(far.CurrentHealth == far.MaxHealth, "outside six metres unharmed");
            Object.Destroy(near.gameObject); Object.Destroy(far.gameObject); await Task.Yield();

            GameSession.Instance.MarkStarted(0, GameCalendar.SecondsPerDay + 10);
            events.Restore(new ColonyEvents.State { drought = EventRules.DroughtSeconds });
            Check(events.Eligible(ColonyEvent.WateringCan) && events.TryTrigger(ColonyEvent.WateringCan), "summer watering event");
            var state = events.Capture();
            Check(events.DroughtRemaining == 0 && state.watering == 30, "watering immediately ends drought for thirty seconds flooding");
            var wet = new GameObject("Watering near").AddComponent<ResourceNode>(); wet.transform.position = state.wateringPosition.ToVector3();
            var dry = new GameObject("Watering far").AddComponent<ResourceNode>(); dry.transform.position = wet.transform.position + Vector3.right * 16;
            Check(ColonyEvents.Flooded(wet) && !wet.CanGather && !ColonyEvents.Flooded(dry), "watering only blocks nodes within fifteen metres");
            events.Tick(29); Check(ColonyEvents.Flooded(wet), "flood lasts through second 29");
            events.Tick(1); Check(!ColonyEvents.Flooded(wet), "flood expires at thirty seconds");
            Object.Destroy(wet.gameObject); Object.Destroy(dry.gameObject); await Task.Yield();
            Check(ColonyEvents.Validate(state), "watering state validates");
            state.watering = float.NaN; Check(!ColonyEvents.Validate(state), "invalid watering timer rejected");
            state.watering = 30; state.wateringPosition.x = float.PositiveInfinity; Check(!ColonyEvents.Validate(state), "invalid watering point rejected");
            var legacy = new ColonyEvents.State { cooldowns = new float[11] }; legacy.cooldowns[2] = 27;
            Check(ColonyEvents.Validate(legacy), "old eleven-event save accepted"); events.Restore(legacy);
            Check(events.Capture().cooldowns.Length == EventRules.Names.Length && events.Capture().cooldowns[2] == 27, "old cooldowns extended without loss");
            var history = new CampaignHistory.State { events = new long[11] }; history.events[2] = 8;
            Check(CampaignHistory.Validate(history), "old event history accepted"); CampaignHistory.Instance.Restore(history);
            Check(CampaignHistory.Instance.Data.events.Length == EventRules.Names.Length && CampaignHistory.Instance.Data.events[2] == 8, "old history extended without loss");

            GameSession.Instance.Options.biome = MapBiome.City; GameSession.Instance.MarkStarted(0, 10); events.Restore(new ColonyEvents.State());
            Check(!events.Eligible(ColonyEvent.Footprint) && !events.Eligible(ColonyEvent.WateringCan), "garden events excluded in city");
            Check(EventRules.Name(ColonyEvent.Flood) == "배수구 역류" && events.TryTrigger(ColonyEvent.Flood), "city flood renamed with same effect");
            Check(events.FloodRemaining == EventRules.FloodSeconds && CampaignHistory.Instance.Data.recent.Last().name == "배수구 역류", "city flood duration and notice");
            events.Restore(new ColonyEvents.State()); Check(events.TryTrigger(ColonyEvent.TrashPile), "city trash event triggers");
            var actor = Object.FindObjectsByType<EventActor>().Single(a => a.Kind == EventActorKind.TrashFood || a.Kind == EventActorKind.TrashSpecial);
            Check(actor.GetComponent<ResourceNode>() != null && actor.Remaining > 0, "trash creates timed gatherable resource");
            foreach (var kind in new[] { EventActorKind.TrashFood, EventActorKind.TrashSpecial })
            {
                events.Restore(new ColonyEvents.State());
                var item = EventActor.Spawn(new EventActor.State { kind = kind, amount = 10, remaining = 100, position = new Vec3Dto(point) });
                var type = kind == EventActorKind.TrashFood ? AntColony.Data.ResourceType.Food : AntColony.Data.ResourceType.Special;
                Check(item.GetComponent<ResourceNode>().ResourceType == type, "trash resource type " + kind);
                Check(item.GetComponent<ResourceNode>().Extract(3) == 3, "trash can be gathered " + kind);
                var saved = events.Capture(); Check(ColonyEvents.Validate(saved), "trash snapshot valid " + kind); events.Restore(saved);
                var restored = Object.FindObjectsByType<EventActor>().Single(a => a.Kind == kind);
                Check(restored.GetComponent<ResourceNode>().AmountRemaining == 7 && restored.Remaining == 100, "trash stock and timer restored " + kind);
                restored.Tick(100); Check(!restored.gameObject.activeSelf, "trash expires " + kind);
            }
            events.Restore(new ColonyEvents.State()); GameSession.Instance.Options.biome = MapBiome.Forest;
            Check(!events.Eligible(ColonyEvent.TrashPile), "trash excluded outside city");
            var visual = Object.FindFirstObjectByType<SeasonVisuals>();
            typeof(SeasonVisuals).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
            Check(Mathf.Approximately(DayNightLighting.SeasonIntensity, .8f), "forest shade multiplier");
            GameSession.Instance.Options.biome = MapBiome.Cave;
            typeof(SeasonVisuals).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
            Check(Mathf.Approximately(DayNightLighting.SeasonIntensity, .6f), "cave lighting preserved");
            return "PASS " + checks + " biome event checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}

}
