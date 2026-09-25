using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// Run in Play mode after starting a game. Uses isolated files, never player saves.
public static class SaveRoundtripChecks
{
    private static int checks;
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        checks++;
    }
    private static void Near(float actual, float expected, string label) =>
        Check(Mathf.Abs(actual - expected) < .5f, label + ": " + actual + " / " + expected);

    private static async Task<SaveFileV1> Load(string path)
    {
        Check(SaveSystem.TryLoad(path, out var error), "load accepted: " + error);
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(10);
        Check(!SaveSystem.Busy && GameSession.Instance.GameStarted, "load finished");
        GameMenuController.Instance.Pause();
        foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
        return SaveSnapshot.Capture();
    }

    public static async Task<string> Main()
    {
        checks = 0;
        if (Application.isPlaying && !GameSession.Instance.GameStarted)
        {
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250925 });
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(30);
        }
        Check(Application.isPlaying && GameSession.Instance.GameStarted, "playable session");
        var previousRoot = SaveStorage.RootOverride;
        var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "SaveRoundtrip-" + Guid.NewGuid().ToString("N"));
        Encyclopedia.ResetCache();
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false;
            UserSettings.Apply(isolated, false);
            GameMenuController.Instance.Pause();
            foreach (var c in CommanderRoster.Instance.Commanders) c.CommandStop();
            var expected = SaveSnapshot.Capture();
            expected.colony.special = 23;
            expected.commanders[0].talents.levels[(int)AntColony.Units.CommanderActivity.Melee] = 3;
            expected.commanders[0].talents.experience[(int)AntColony.Units.CommanderActivity.Melee] = 7;
            expected.commanders[0].labAttackLevel = 2;
            expected.commanders[0].labArmorLevel = 1;
            expected.commanders[0].strikeArmed = true;
            expected.commanders[0].strikeCooldown = 9;
            expected.commanders[0].stanceCooldown = 12;
            expected.commanders[0].stanceTime = 3;
            var queen = expected.buildings.First(b => b.kind == "QueenChamber");
            queen.queenProductionRemaining = 8;
            queen.queenFishingRemaining = 11;
            var barracks = new BuildingDto { key = "new:" + expected.buildings.Count, kind = "Barracks", runtimeBuilt = true,
                health = 100, role = (int)AntColony.Data.UnitRole.Melee, barracksTier = 2, barracksUpgradeRemaining = 6,
                position = new Vec3Dto(WorldMapManager.Instance.HomePosition + Vector3.right * 12) };
            expected.buildings.Add(barracks);
            var destroyed = expected.buildings.First(b => b.kind == "Storage");
            destroyed.kind = "Destroyed"; destroyed.health = 0; destroyed.nodes.Clear();
            expected.nodes[0].amount = 0;
            expected.nodes[0].regrowTimer = 0;
            expected.monsters.First(m => m.key.StartsWith("monster:")).health = 0;
            expected.nodes.Add(new ResourceNodeDto { key = "new:" + expected.nodes.Count, type = 0,
                amount = 37, position = new Vec3Dto(WorldMapManager.Instance.HomePosition + Vector3.right * 5) });
            var site = expected.world.sites.First(s => s.colony != null && s.colony.buildingHealth.Count > 0);
            var shipIndex = expected.world.transports.Count;
            expected.world.transports.Add(new TransportDto { state = (int)ExpeditionState.Outbound,
                aircraft = true, remaining = 7, siteIndex = site.index,
                position = new Vec3Dto(WorldMapManager.Instance.HomePosition),
                homePosition = new Vec3Dto(WorldMapManager.Instance.HomePosition), cargoFood = 13 });
            expected.commanders[1].location = 1;
            expected.commanders[1].transportIndex = shipIndex;
            var path = Path.Combine(SaveStorage.Root, "fixture.json");
            SaveStorage.WriteAtomic(path, JsonUtility.ToJson(expected));
            var actual = await Load(path);
            Verify(expected, actual, queen.key, barracks.key, destroyed.key, shipIndex);
            Check(SaveSystem.TrySave(false, 1, out var error), "save restored state: " + error);
            var again = await Load(SaveSlots.PathFor(false, 1));
            Verify(actual, again, queen.key, barracks.key, destroyed.key, shipIndex);
            return "PASS " + checks + " checks; two scene reloads, progression, timers, destroyed building/enemy, depleted node, new loot, outbound crew/cargo";
        }
        finally
        {
            Time.timeScale = 0;
            SaveStorage.RootOverride = previousRoot;
            Encyclopedia.ResetCache();
            UserSettings.Apply(settings, false);
        }
    }

    private static void Verify(SaveFileV1 expected, SaveFileV1 actual, string queen, string barracks, string destroyed, int ship)
    {
        Check(actual.colony.special == expected.colony.special, "special resources");
        Check(actual.colony.antsAssigned == expected.colony.antsAssigned, "assigned population");
        var c = actual.commanders[0]; var e = expected.commanders[0];
        Check(c.talents.levels.SequenceEqual(e.talents.levels) && c.talents.experience.SequenceEqual(e.talents.experience)
            && c.labAttackLevel == e.labAttackLevel && c.labArmorLevel == e.labArmorLevel, "talents/upgrades");
        Check(c.strikeArmed == e.strikeArmed, "armed strike");
        Near(c.strikeCooldown, e.strikeCooldown, "strike cooldown");
        Near(c.stanceTime, e.stanceTime, "stance duration");
        var q = actual.buildings.Single(b => b.key == queen); var eq = expected.buildings.Single(b => b.key == queen);
        Near(q.queenProductionRemaining, eq.queenProductionRemaining, "queen production");
        Near(q.queenFishingRemaining, eq.queenFishingRemaining, "fishing research");
        var b = actual.buildings.Single(x => x.key == barracks); var eb = expected.buildings.Single(x => x.key == barracks);
        Check(b.barracksTier == eb.barracksTier, "barracks tier");
        Near(b.barracksUpgradeRemaining, eb.barracksUpgradeRemaining, "barracks upgrade");
        Check(actual.buildings.Single(x => x.key == destroyed).kind == "Destroyed", "destroyed building stays destroyed");
        Near(actual.nodes[0].amount, expected.nodes[0].amount, "depleted node");
        Check(actual.nodes.Count == expected.nodes.Count && actual.nodes.Last().amount == 37, "new loot preserved");
        Check(actual.monsters.First(m => m.key.StartsWith("monster:")).health == 0, "defeated enemy stays dead");
        Check(actual.commanders[1].location == 1 && actual.commanders[1].transportIndex == ship, "crew ownership");
        Check(actual.world.transports[ship].state == (int)ExpeditionState.Outbound && actual.world.transports[ship].cargoFood == 13, "outbound state/cargo");
        Near(actual.world.transports[ship].remaining, expected.world.transports[ship].remaining, "travel timer");
        Check(CommanderRoster.Instance.Commanders[1].IsEmbarked, "travelling crew remains embarked");
    }
}
