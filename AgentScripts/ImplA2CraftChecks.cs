using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// Fresh Play session. Mutates an isolated new game and never writes player save slots.
public static class ImplA2CraftChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL A2 craft: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static GameObject Template(BuildingKind kind) => (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Static)
        .Invoke(null, new object[] { kind, UnitRole.Worker });
    static BuildingConstructionSite Place(BuildingPlacementController p, CommanderAnt c, BuildingKind kind)
    {
        Check(p.BeginPlacement(kind, UnitRole.Worker, c), kind + " unlocked at population zero");
        return (BuildingConstructionSite)typeof(BuildingPlacementController).GetMethod("PlaceOne", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(p, new object[] { c.Position + Vector3.forward * 6, c.Position + Vector3.forward * 6, false, null });
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplA2Craft-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone(); var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            var options = new NewGameOptions { mapSize = MapSize.Small, seed = 231029, startingCount = 3,
                startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() };
            foreach (var candidate in options.startingTeam) candidate.traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var c = CommanderRoster.Instance.Commanders[0]; c.CommandStop(); c.ApplyTraits(new CommanderTraits()); c.SetJobPriority(CommanderJobs.Crafting, 5);
            var rm = ResourceManager.Instance;
            foreach (var t in new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Fiber, ResourceType.Food, ResourceType.Soil }) { rm.AddCapacity(t, 1000); rm.Add(t, 200); }
            var p = Object.FindFirstObjectByType<BuildingPlacementController>();
            var carpentry = Place(p, c, BuildingKind.Carpentry);
            Check(carpentry != null && carpentry.Needs.Count == 1 && carpentry.Needs[0].type == ResourceType.Wood && carpentry.Needs[0].need == 10, "carpentry exact wood ten");
            Check(carpentry.Building.GetComponent<BuildingBase>().Data.constructionAnts == 0, "carpentry no citizens");
            var desk = Place(p, c, BuildingKind.ScienceLab);
            Check(desk != null && desk.Needs.Count == 2 && desk.Needs.Any(n => n.type == ResourceType.Wood && n.need == 20) && desk.Needs.Any(n => n.type == ResourceType.Stone && n.need == 10), "research desk exact materials");
            Check(ScienceLab.PrerequisitesMet && desk.Building.GetComponent<BuildingBase>().Data.constructionAnts == 0, "research no population barracks power gate");
            p.CancelPlacement();

            var rock = new GameObject("A2 test vein").AddComponent<ResourceNode>(); rock.ConfigureNatural(ResourceType.Stone, 20, 0);
            c.PersonalState.toolTier = 1; Check(rock.RequiresMiningTool && !rock.CanGatherWith(c), "wood tools cannot mine vein");
            c.PersonalState.toolTier = 2; Check(rock.CanGatherWith(c), "stone tools permit vein mining"); c.PersonalState.toolTier = 1;
            var loose = new GameObject("A2 loose stone").AddComponent<ResourceNode>(); loose.ConfigureLoot(ResourceType.Stone, 20);
            Check(loose.CanGatherWith(c), "loose stone hand hauling");
            var beforeWood = rm.GetAmount(ResourceType.Wood); var beforeStone = rm.GetAmount(ResourceType.Stone);
            Check(c.StartGroundCraft(GroundRecipe.StoneTools), "stone tools no bench or research");
            Check(rm.GetAmount(ResourceType.Wood) == beforeWood - 2 && rm.GetAmount(ResourceType.Stone) == beforeStone - 3, "stone tools exact cost once");
            c.TickGroundCraft(2); Check(c.PersonalState.groundCraft.work > 0 && c.IsGroundCrafting, "ground crafting progresses");
            var id = c.PersonalState.id; var progress = c.PersonalState.groundCraft.work;
            Check(SaveSystem.TrySave(false, 0, out var error), "save crafting: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load crafting: " + error); await Ready();
            c = CommanderRoster.Instance.Commanders.Single(x => x.PersonalState.id == id);
            Check(Mathf.Approximately(c.PersonalState.groundCraft.work, progress), "craft progress roundtrip");
            c.TickGroundCraft(1000); Check(c.PersonalState.toolTier == 2 && !c.IsGroundCrafting, "stone tool completed");
            var count = EquipmentInventory.Instance.Items.Count;
            Check(c.StartGroundCraft(GroundRecipe.ShortBow), "short bow no bench or research"); c.TickGroundCraft(1000);
            Check(EquipmentInventory.Instance.Items.Count == count + 1 && EquipmentInventory.Instance.Items.Last().weapon == WeaponKind.ShortBow, "short bow output exactly once");
            c.TickGroundCraft(1000); Check(EquipmentInventory.Instance.Items.Count == count + 1, "no duplicate bow on idle tick");

            var labObject = Object.Instantiate(Template(BuildingKind.ScienceLab), c.Position + Vector3.forward * 3, Quaternion.identity);
            labObject.name = "A2 research desk"; labObject.SetActive(true); var lab = labObject.GetComponent<ScienceLab>();
            c.CommandStop(); c.SetJobPriority(CommanderJobs.Research, 5);
            Check(lab.Tier == 1 && lab.TryAssign(c), "one researcher at tier one");
            Check(!lab.TryAssign(c), "research desk has one seat");
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Fishing), "tier one fishing starts");
            CampaignResearch.Instance.Tick(10000); Check(CampaignResearch.Instance.Has(ScienceTechnology.Fishing), "research desk completes first era research");
            if (rock != null) Object.Destroy(rock.gameObject);
            if (loose != null) Object.Destroy(loose.gameObject);
            return "PASS ImplA2CraftChecks " + checks;
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0; }
    }
}
