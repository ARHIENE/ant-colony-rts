using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

// unity command run_script --file AgentScripts/CampaignChecks.cs (fresh Play mode).
// Uses disposable saves; never overwrites the player's slots.
public static class CampaignChecks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready");
        Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable test position");
        Check(c.Agent.Warp(hit.position), "commander warp");
    }
    static ScienceLab Lab(Vector3 p)
    {
        var template = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
        var lab = Object.Instantiate(template, p, Quaternion.identity); lab.name = "ScienceLab"; lab.gameObject.SetActive(true); return lab;
    }
    static void Complete(ScienceTechnology technology)
    {
        var r = CampaignResearch.Instance;
        Check(r.TryStart(technology), "start " + technology + ": " + r.BlockReason(technology));
        r.Tick(10000); Check(r.Has(technology), "complete " + technology);
    }
    static SaveFileV1 Clone(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "CampaignChecks-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 240924, commanderDeath = CommanderDeathMode.Gentle });
            await Ready();
            Object.FindFirstObjectByType<UpkeepManager>().enabled = false;
            Object.FindFirstObjectByType<LocalIncursions>().enabled = false;
            var rm = ResourceManager.Instance;
            foreach (ColonyResourceType type in Enum.GetValues(typeof(ColonyResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            var roster = CommanderRoster.Instance;
            foreach (var c in roster.Commanders) c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));
            var a = roster.Commanders[0]; var b = roster.Commanders[1];
            var inventory = EquipmentInventory.Instance;
            Check(CampaignResearch.Instance != null && inventory != null, "scene services bootstrapped");
            var food = rm.GetAmount(ColonyResourceType.Food); var soil = rm.GetAmount(ColonyResourceType.Soil);
            Check(!rm.TrySpend(1, 1, int.MaxValue) && rm.GetAmount(ColonyResourceType.Food) == food && rm.GetAmount(ColonyResourceType.Soil) == soil, "special spend atomic");
            Check(!rm.TrySpend(0, 0, -1), "negative special cost rejected");
            var charm = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Command };
            var replacement = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Move };
            inventory.Add(charm); inventory.Add(replacement);
            Check(inventory.Equip(a, charm), "command charm equipped");
            AntPool.Instance.Breed(100); Check(a.TryAssign(a.FreeRanks), "fill expanded command limit");
            var troopCount = a.TroopCount;
            Check(inventory.Equip(a, replacement) && a.TroopCount == troopCount && a.TroopCount > a.CommandLimit && !a.TryAssign(1), "capacity loss preserves troops, prevents assignment");
            Check(a.PersonalState.equipment.Count == 1 && inventory.Items.Contains(charm), "old charm returned once");
            var personal = a.CapturePersonalState();
            personal.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Minor, remaining = 5 });
            a.RestorePersonalState(personal); personal.injuries.Clear();
            Check(a.PersonalState.injuries.Count == 1, "personal restore deep copy");
            a.TickPersonal(6); Check(a.PersonalState.injuries.Count == 0, "minor injury heals");
            var loyalty = a.Traits.Loyalty;
            Check(a.TryReward() && a.Traits.Loyalty == loyalty + 8, "reward applies once after equipment events");
            Check(!a.TryReward(), "monthly reward cooldown");
            a.StartMentalBreak(MentalBreak.Idle); Check(!a.CanReceiveOrders && !a.TryAssign(1), "mental break blocks commands/allocation");
            a.TickPersonal(61); Check(a.CanReceiveOrders && a.PersonalState.moodFactors.Any(f => f.reason == "Catharsis"), "mental break recovery");
            b.TakeDamage(float.MaxValue); Check(!b.IsDead && b.TroopCount == 0 && b.PersonalState.injuries.Count > 0, "gentle downing applies injury without death");
            b.RestorePersonalState(new CommanderPersonalState()); Check(b.TryAssign(2), "downed commander replenished");
            a.ReturnTroops(a.TroopCount - 2);
            var lab = Lab(a.Position + Vector3.forward * 4);
            Check(lab.TryAssign(a) && a.ScienceAssignment == lab, "scientist ownership");
            Check(!a.CanChangeAllocation && !a.CanStartConstruction, "researcher cannot double-book");
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle), "tier wall");
            Check(lab.TryUpgrade(), "lab upgrade");
            var lab2 = Lab(b.Position + Vector3.forward * 4);
            Check(lab2.TryUpgrade() && lab2.TryAssign(b), "second researcher");
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle), "shared vehicle research");
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Resin), "one research at a time");
            var researchWork = 10 * 1.25f * (a.Talents.Multiplier(CommanderActivity.Research) + b.Talents.Multiplier(CommanderActivity.Research));
            CampaignResearch.Instance.Tick(10); Check(Mathf.Abs(CampaignResearch.Instance.Progress - researchWork) < .01f, "two labs contribute by skill and tier");
            lab2.gameObject.SetActive(false); Check(b.ScienceAssignment == null, "destroyed/disabled lab releases researcher");
            CampaignResearch.Instance.Tick(10000); Check(WorldMapManager.Instance.VehicleResearched, "vehicle unlock applied");
            Check(lab.TryUpgrade(), "tier 3"); Complete(ScienceTechnology.Gliding); Complete(ScienceTechnology.Aircraft); Complete(ScienceTechnology.MigrationTheory);
            Check(lab.TryUpgrade(), "tier 4"); Complete(ScienceTechnology.Hull); Complete(ScienceTechnology.Cocoons);
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Engine), "engine requires world blueprint");
            lab.ReleaseResearcher();
            var world = WorldMapManager.Instance;
            Check(world.CanCreateTransport(a.Position, out var shipPoint), "transport spawn point");
            var ship = world.CreateTransport(false, shipPoint);
            Move(a, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { a }), "board expedition");
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest);
            Check(ship.TryDepart(site), "depart boss expedition"); ship.Tick(ship.TravelSeconds + 1);
            Check(!ship.TryCollectRewards(), "live boss gives no rewards");
            site.Boss.TakeDamage(float.MaxValue); await Task.Delay(80);
            Check(site.Cleared && ship.TryCollectRewards(), "boss reward loaded");
            Check(ship.BlueprintCargo && !CampaignResearch.Instance.HasBlueprint && ship.EquipmentCargo.Count == 1, "loot remains local cargo");
            Check(!ship.TryCollectRewards(), "reward cannot duplicate");
            Check(ship.TryReturn(), "return boss cargo"); ship.Tick(ship.TravelSeconds + 1);
            Check(CampaignResearch.Instance.HasBlueprint && ship.EquipmentCargo.Count == 0 && inventory.Items.Count >= 2, "reward delivered only at home");
            Move(a, lab.Position + Vector3.right * 3); Check(lab.TryAssign(a), "researcher reassigned"); Complete(ScienceTechnology.Engine);
            lab.ReleaseResearcher();
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.AirshipYard, UnitRole.Worker });
            var yard = Object.Instantiate(template, world.HomePosition + Vector3.left * 12, Quaternion.identity).GetComponent<AirshipYard>();
            yard.name = "AirshipYard"; yard.gameObject.SetActive(true);
            Check(yard.TryBuild(AirshipPart.Hull), "hull construction starts"); yard.Tick(301);
            Check(!yard.TryDepart(), "engine required to depart");
            Check(yard.TryBuild(AirshipPart.Engine), "engine construction starts"); yard.Tick(301);
            Check(yard.Ready && yard.Cocoons == 0, "zero passenger departure allowed");
            Check(yard.TryBuild(AirshipPart.Cocoon), "cocoon construction starts"); yard.Tick(61);
            Move(b, yard.Position + Vector3.right * 4); Check(yard.TryBoard(b), "airship passenger boarded");
            Check(!b.CanChangeAllocation && !yard.TryBoard(b), "embarked passenger protected");
            Check(lab.TryAssign(a), "researcher retained for save");
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Herbs), "partial research saved"); CampaignResearch.Instance.Tick(7);
            foreach (var c in roster.Commanders) c.CommandStop();
            // 6단계: 비행선 건조 시작 시 과학자·적대 문명 침공이 올 수 있다. 저장 전 정리한다.
            foreach (var raider in Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(raider.DiplomaticFactionId)) raider.TakeDamage(float.MaxValue);
            await Task.Delay(100);
            var expected = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(expected, out var error), "valid capture: " + error);
            var broken = Clone(expected); broken.commanders[0].personalState.moodFactors.Add(new MoodFactor { value = float.NaN, reason = "invalid" });
            Check(!SaveValidator.Validate(broken, out _), "NaN mood rejected");
            broken = Clone(expected); broken.equipmentInventory.Add(broken.equipmentInventory[0]);
            Check(!SaveValidator.Validate(broken, out _), "duplicate equipment rejected");
            broken = Clone(expected); broken.buildings.First(x => x.kind == "AirshipYard").airship.passengers.Add(0);
            Check(!SaveValidator.Validate(broken, out _), "overcapacity/double assignment rejected");
            Check(SaveSystem.TrySave(false, 0, out error), "save accepted: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error); await Ready();
            var actual = SaveSnapshot.Capture();
            Check(actual.campaign.active == expected.campaign.active && Mathf.Abs(actual.campaign.progress - expected.campaign.progress) < 2, "research survives reload");
            Check(actual.campaign.blueprint && actual.campaign.completed.Count == expected.campaign.completed.Count, "research and blueprint retained");
            var restoredLab = Object.FindObjectsByType<ScienceLab>().First(x => x.Target != null);
            Check(restoredLab.Tier == 4 && restoredLab.Target.ScienceAssignment == restoredLab, "lab tier and assignment restored");
            var restoredYard = Object.FindFirstObjectByType<AirshipYard>();
            Check(restoredYard.Ready && restoredYard.Cocoons == 1 && restoredYard.Passengers.Count == 1 && restoredYard.Passengers[0].IsEmbarked, "airship state restored");
            Check(EquipmentInventory.Instance.Items.Count == expected.equipmentInventory.Count, "equipment retained");
            GameMenuController.Instance.Science();
            Check(GameMenuController.Instance.ScreenName == "Science / Airship", "science UI reachable");
            Check(restoredYard.TryDepart(), "departure succeeds");
            Check(CampaignResearch.Instance.Passengers.Count == 1 && CampaignResearch.Instance.LeftBehind.Count == CommanderRoster.Instance.Commanders.Count - 1, "ending manifests");
            Check(Time.timeScale == 0 && GameMenuController.Instance.ScreenName.Contains("VICTORY"), "ending pauses simulation");
            GameMenuController.Instance.Resume(); Check(Time.timeScale == 0, "cannot resume completed campaign");
            Check(!restoredYard.TryDepart(), "departure is one-shot");
            return "PASS " + checks + " campaign checks; research, rewards, equipment, injuries, save/reload, airship ending";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}
