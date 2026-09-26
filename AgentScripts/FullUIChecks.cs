using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

public static class FullUIChecks
{
    private static int checks;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("FAIL: " + message); checks++; }
    private static void Click(string label)
    {
        var button = GameMenuController.Instance.GetComponentsInChildren<Button>()
            .SingleOrDefault(b => b.name == label && b.interactable);
        Check(button != null, "active button: " + label);
        button.onClick.Invoke();
    }
    private static async Task Ready()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(100);
        Check(!SaveSystem.Busy, "scene finishes loading");
    }
    public static async Task<string> Main()
    {
        Check(Application.isPlaying, "Play mode");
        var oldRoot = SaveStorage.RootOverride;
        var settings = UserSettings.Current.Clone();
        var root = Path.Combine(Application.temporaryCachePath, "FullUIChecks-" + Guid.NewGuid().ToString("N"));
        SaveStorage.RootOverride = root; Encyclopedia.ResetCache(); UserSettings.ResetCacheForReload();
        try
        {
            await Ready();
            Check(GameMenuController.Instance.ScreenName == "ANT COLONY" && Time.timeScale == 0, "starts in paused main menu");
            Check(GameMenuController.BlocksInput, "menu input gate");
            var before = AntPool.Instance.Total;
            await Task.Delay(200);
            Check(AntPool.Instance.Total == before, "no hidden population simulation");
            Click("Settings");
            Check(GameMenuController.Instance.ScreenName == "Settings", "settings button opens settings");
            Click("Autosave: On");
            UserSettings.ResetCacheForReload();
            Check(!UserSettings.Current.autoSaveEnabled, "settings survive disk reload");
            Click("Back");
            Click("Encyclopedia");
            Check(GameMenuController.Instance.ScreenName == "Encyclopedia", "encyclopedia button");
            Click("Back");
            Click("New Game");
            Click("Map: Medium (base size)"); Click("Map: Large (160% of base)");
            Click("Difficulty: Normal"); Click("Difficulty: Harsh");
            GameMenuController.Instance.GetComponentInChildren<InputField>().text = "76543";
            Click("Start Game");
            await Ready(); GameMenuController.Instance.Pause();
            Check(GameSession.Instance.GameStarted, "new game starts");
            Check(GameSession.Instance.Options.seed == 76543 && GameSession.Instance.Options.mapSize == MapSize.Small, "real options applied");
            var map = Object.FindFirstObjectByType<AntColony.Map.HomeMapBuilder>();
            Check(map != null && map.Rebuilt && Mathf.Abs(map.WorldBounds.size.x - 240) < .1f, "small terrain and navmesh rebuilt");
            Check(CommanderRoster.Instance.Count == 12, "commanders spawned on new navmesh");
            Check(Object.FindObjectsByType<GameMenuController>(FindObjectsSortMode.None).Length == 1, "one persistent menu after reload");
            Check(Time.timeScale == 0, "pause menu freezes");
            GameMenuController.Instance.Resume();
            Check(Time.timeScale == 1, "resume restores scale");
            Time.timeScale = .5f; GameMenuController.Instance.Pause(); GameMenuController.Instance.Resume();
            Check(Time.timeScale == .5f, "custom scale restored");
            GameMenuController.Instance.Pause();
            var s = UserSettings.Current.Clone(); s.toastSeconds = 2; UserSettings.Apply(s);
            ToastManager.Show("Paused toast check");
            Check(ToastManager.Count > 0 && Time.timeScale == 0, "toast does not change pause");
            await Task.Delay(2300);
            Check(ToastManager.Count == 0, "toast expires while paused");
            GameMenuController.Instance.Roster();
            var sorted = GameMenuController.SortedCommanders();
            Check(sorted.Length == 12 && sorted.Zip(sorted.Skip(1), (a,b) => StringComparer.Ordinal.Compare(a.CommanderName, b.CommanderName) <= 0).All(x => x), "name sorting");
            Check(Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Any(x => x.content != null && x.viewport != null), "scroll hierarchy");
            foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
            var commander = CommanderRoster.Instance.Commanders[0]; commander.GainExperience(CommanderActivity.Melee, 130); commander.GainExperience(CommanderActivity.Gathering, 123);
            ResourceManager.Instance.Add(ColonyResourceType.Special, 17);
            var world = WorldMapManager.Instance;
            var ship = world.CreateTransport(false, world.HomePosition + Vector3.right * 8);
            var expected = SaveSnapshot.Capture();
            Check(SaveSystem.TrySave(false, 0, out var error), "save succeeds: " + error);
            Check(SaveSlots.Inspect(false, 0).Valid, "slot reports valid");
            Check(SaveSystem.TrySave(false, 0, out error) && File.Exists(SaveSlots.PathFor(false, 0) + ".bak"), "atomic replacement retains backup");
            var json = File.ReadAllText(SaveSlots.PathFor(false, 0));
            Check(SaveValidator.TryParse(json, out var parsed, out error), "JSON validated: " + error);
            Check(SavePreflight.ForScene(parsed, out error), "serialized save matches scene: " + error);
            var emptyColony = parsed.world.sites.First(s => s.colony != null && s.colony.buildingHealth.Count == 0).colony;
            emptyColony.buildingHealth.Add(1); emptyColony.extraBuildings.Add(new Vec3Dto(Vector3.zero));
            Check(!SavePreflight.ForScene(parsed, out error), "unexpected colony rejected");
            Check(SaveValidator.TryParse(json, out parsed, out error), "reparse after colony schema check");
            parsed.commanders[0].transportIndex = 99999;
            Check(!SaveValidator.Validate(parsed, out error), "bad commander references rejected");
            Check(SaveValidator.TryParse(json, out parsed, out error), "reparse clean save"); parsed.nodes[0].amount = float.NaN;
            Check(!SaveValidator.Validate(parsed, out error), "NaN rejected");
            var corrupt = Path.Combine(root, "corrupt.json"); File.WriteAllText(corrupt, "{broken");
            var unchanged = ResourceManager.Instance.GetAmount(ColonyResourceType.Special);
            Check(!SaveSystem.TryLoad(corrupt, out error) && !SaveSystem.Busy && unchanged == ResourceManager.Instance.GetAmount(ColonyResourceType.Special), "corrupt load preserves game");
            ResourceManager.Instance.Add(ColonyResourceType.Special, 11);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error);
            await Ready(); GameMenuController.Instance.Pause();
            var actual = SaveSnapshot.Capture();
            Check(actual.colony.special == expected.colony.special && actual.colony.antsAssigned == expected.colony.antsAssigned, "resources and ants roundtrip");
            Check(actual.commanders[0].talents.levels.SequenceEqual(expected.commanders[0].talents.levels)
                && actual.commanders[0].talents.experience.SequenceEqual(expected.commanders[0].talents.experience), "commander skills roundtrip");
            Check(actual.world.sites.Count == expected.world.sites.Count && actual.world.transports.Count == expected.world.transports.Count, "world and transport roundtrip");
            Check(actual.buildings.Count == expected.buildings.Count && actual.nodes.Count == expected.nodes.Count && actual.monsters.Count == expected.monsters.Count, "all entity counts roundtrip");
            Check(GameMenuController.Instance.ScreenName == "Paused" && Object.FindObjectsByType<GameMenuController>(FindObjectsSortMode.None).Length == 1, "load lifecycle has one menu");
            return "PASS " + checks + " checks; isolated save directory: " + root;
        }
        finally
        {
            Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; Encyclopedia.ResetCache(); UserSettings.ResetCacheForReload(); UserSettings.Apply(settings, false);
        }
    }
}

