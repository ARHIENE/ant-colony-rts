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
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

// Fresh Play session: unity command run_script --file AgentScripts/Stage3Checks.cs --entry Stage3Checks.Main --timeout_ms 180000
public static class Stage3Checks
{
    static int checks;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static void Near(float actual, float expected, string label) => Check(Mathf.Abs(actual - expected) < .01f, label + " actual=" + actual);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop(); Check(NavMesh.SamplePosition(p, out var hit, 15, NavMesh.AllAreas), "walkable point");
        Check(c.Agent.Warp(hit.position), "warp");
    }
    static T Build<T>(BuildingKind kind, Vector3 point) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any).Invoke(null, new object[] { kind, UnitRole.Flying });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template, point, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true); return go.GetComponent<T>();
    }
    static void Grant(params ScienceTechnology[] techs)
    {
        var state = CampaignResearch.Instance.CaptureState();
        foreach (var t in techs) if (!state.completed.Contains((int)t)) state.completed.Add((int)t);
        CampaignResearch.Instance.RestoreState(state);
    }
    static SaveFileV1 Copy(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage3-" + Guid.NewGuid().ToString("N"));
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 250927, mapSize = MapSize.Small, commanderDeath = CommanderDeathMode.Harsh }); await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            var rm = ResourceManager.Instance;
            foreach (Resource r in Enum.GetValues(typeof(Resource))) { rm.AddCapacity(r, 10000); rm.Add(r, 9000); }
            var roster = CommanderRoster.Instance; var c = roster.Commanders[0];
            foreach (var commander in roster.Commanders) commander.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));
            c.Talents.levels[(int)CommanderActivity.Crafting] = 0; c.Talents.experience[(int)CommanderActivity.Crafting] = 0;
            var w = Build<Workshop>(BuildingKind.Workshop, c.Position + Vector3.forward * 4);
            Move(c, w.Position + Vector3.right * 3);
            Check(w.Data.foodCost == 50 && w.Data.soilCost == 80 && w.Data.specialCost == 10 && w.Data.constructionAnts == 6 && w.Data.buildTimeSeconds == 10, "workshop construction cost");
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.Workshop) && !w.TryEnqueue(EquipmentRecipe.Mandible), "workshop research gate");
            Grant(ScienceTechnology.Blades);
            Check(ScienceEffects.BuildingUnlocked(BuildingKind.Workshop), "Blades unlocks workshop");
            var soil = rm.GetAmount(Resource.Soil); var special = rm.GetAmount(Resource.Special);
            Check(w.TryEnqueue(EquipmentRecipe.Mandible) && w.TryEnqueue(EquipmentRecipe.AcidSprayer) && w.TryEnqueue(EquipmentRecipe.Mandible), "three jobs accepted");
            Check(!w.TryEnqueue(EquipmentRecipe.Mandible), "fourth job rejected");
            Check(rm.GetAmount(Resource.Soil) == soil - 110 && rm.GetAmount(Resource.Special) == special - 20, "cost paid on queue");
            Check(w.Cancel(2) && rm.GetAmount(Resource.Soil) == soil - 70 && rm.GetAmount(Resource.Special) == special - 15, "waiting job refunds 100 percent");
            Check(w.TryAssign(c) && !c.CanReceiveOrders && !c.CanChangeEquipment, "crafter assignment locks commands");
            var position = c.Position; c.CommandMove(position + Vector3.right * 10);
            Check(!c.Agent.hasPath, "movement command blocked");
            var w2 = Build<Workshop>(BuildingKind.Workshop, w.Position + Vector3.back * 4);
            Check(w2.TryEnqueue(EquipmentRecipe.Mandible) && !w2.TryAssign(c), "one assignment only"); w2.Cancel(0);
            w.Tick(30); Near(w.Jobs[0].work, 18, "craft speed .6 at skill zero"); Near(c.Talents.Xp(CommanderActivity.Crafting), 30, "craft XP per second");
            soil = rm.GetAmount(Resource.Soil); special = rm.GetAmount(Resource.Special);
            Check(w.Cancel(0) && rm.GetAmount(Resource.Soil) == soil + 20 && rm.GetAmount(Resource.Special) == special + 2, "started job half refund rounded down");
            w.Tick(10); var progress = w.Jobs[0].work;
            w.enabled = false; Check(c.CanReceiveOrders && w.Crafter == null, "disable releases crafter");
            w.Tick(100); Near(w.Jobs[0].work, progress, "disabled progress preserved");
            w.enabled = true; Check(w.TryAssign(c), "resume assignment");
            var inventory = EquipmentInventory.Instance; inventory.Items.Clear();
            while (!inventory.Full) Check(inventory.Add(EquipmentRecipes.Create(EquipmentRecipe.Anklet, 1)), "fill inventory");
            Check(!inventory.Add(EquipmentRecipes.Create(EquipmentRecipe.Coating, 1)) && !w2.TryEnqueue(EquipmentRecipe.Mandible), "full blocks additions and queue");
            w.Tick(100); Near(w.Jobs[0].work, progress, "full pauses crafting");
            var other = roster.Commanders[1]; var oldWeapon = other.Weapon;
            var swap = inventory.Items[0]; swap.slot = EquipmentSlot.Weapon; swap.weapon = WeaponKind.Shield;
            Check(inventory.Equip(other, swap), "full inventory permits equip swap");
            if (oldWeapon == null) inventory.Add(EquipmentRecipes.Create(EquipmentRecipe.Anklet, 1));
            Check(inventory.Full && !inventory.Unequip(other, other.Weapon), "full inventory rejects unequip without loss");
            inventory.Items.RemoveAt(0);
            w.Tick(1000); Check(w.Jobs.Count == 0 && inventory.Full && c.CanReceiveOrders, "finish once, store result and release");
            Check(inventory.Items.Any(i => i.weapon == WeaponKind.AcidSprayer && i.slot == EquipmentSlot.Weapon), "crafted recipe retained");
            inventory.Items.Clear();
            Check(EquipmentRecipes.Quality(0, .399f, 1) == 0 && EquipmentRecipes.Quality(0, .401f, 1) == 1 && EquipmentRecipes.Quality(0, .761f, 1) == 2, "skill zero quality boundaries");
            Check(EquipmentRecipes.Quality(20, 0, 1) == 1 && EquipmentRecipes.Quality(20, .451f, 1) == 2 && EquipmentRecipes.Quality(20, .751f, 1) == 3, "skill twenty quality boundaries");
            Check(!EquipmentRecipes.Unlocked(EquipmentRecipe.Shield) && !EquipmentRecipes.Unlocked(EquipmentRecipe.Coating) && !EquipmentRecipes.Unlocked(EquipmentRecipe.Anklet), "advanced recipes locked");
            Grant(ScienceTechnology.ArmorPlates, ScienceTechnology.Trinkets, ScienceTechnology.AdvancedWeapons, ScienceTechnology.Gliding);
            Check(EquipmentRecipes.Quality(0, 0, .099f) == 1 && EquipmentRecipes.Quality(0, 0, .1f) == 0, "advanced weapons ten percentage point promotion");
            Build<Barracks>(BuildingKind.Barracks, w.Position + Vector3.forward * 20);
            foreach (EquipmentRecipe recipe in Enum.GetValues(typeof(EquipmentRecipe)))
            { Check(EquipmentRecipes.Unlocked(recipe) && EquipmentRecipes.Create(recipe, 3).IsValid, "recipe " + recipe); }

            // 1·2단계 회귀: 저장 검증도 장수 슬롯과 병력 정원을 별도로 계산해야 한다.
            Check(WorldMapManager.Instance.CanCreateTransport(c.Position, out var shipPoint), "transport point");
            WorldMapManager.Instance.CreateTransport(false, shipPoint);
            foreach (var commander in roster.Commanders) commander.CommandStop();
            var file = SaveSnapshot.Capture();
            for (int i = 0; i < 4; i++) { file.commanders[i].location = 1; file.commanders[i].transportIndex = 0; file.commanders[i].troopCount = 10; }
            file.colony.antsAssigned = file.commanders.Sum(d => d.troopCount);
            Check(SaveValidator.Validate(file, out var error), "4 commanders plus 40 troops can save: " + error);
            file.commanders[4].location = 1; file.commanders[4].transportIndex = 0; file.commanders[4].troopCount = 1; file.colony.antsAssigned = file.commanders.Sum(d => d.troopCount);
            Check(!SaveValidator.Validate(file, out error), "fifth commander rejected by save");
            file.campaign.completed.Add((int)ScienceTechnology.HeavyTransport);
            for (int i = 0; i < 6; i++) { file.commanders[i].location = 1; file.commanders[i].transportIndex = 0; file.commanders[i].troopCount = 10; }
            file.colony.antsAssigned = file.commanders.Sum(d => d.troopCount);
            Check(SaveValidator.Validate(file, out error), "heavy transport 6 plus 60 can save: " + error);

            Check(w.TryEnqueue(EquipmentRecipe.Shield) && w.TryAssign(c), "prepare saved work"); w.Tick(12);
            progress = w.Jobs[0].work;
            Check(w2.TryEnqueue(EquipmentRecipe.Mandible), "prepare ruined job"); w2.TakeDamage(float.MaxValue);
            Check(w2.Ruined && w2.Jobs.Count == 1, "destruction preserves queue");
            var w3 = Build<Workshop>(BuildingKind.Workshop, w.Position + Vector3.left * 10);
            Check(w3.TryEnqueue(EquipmentRecipe.Coating), "prepare inactive queue"); w3.gameObject.SetActive(false);
            var dropped = EquipmentRecipes.Create(EquipmentRecipe.Insignia, 3);
            EquipmentLoot.Drop(w.Position + Vector3.right * 7, new[] { dropped });
            var expectedLoot = dropped.id;
            file = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(file, out error), "workshop snapshot valid: " + error);
            var bad = Copy(file); bad.buildings.First(b => b.kind == "Workshop" && b.workshop.crafter >= 0).workshop.jobs[0].work = float.NaN;
            Check(!SaveValidator.Validate(bad, out error), "invalid progress rejected before scene change");
            bad = Copy(file); bad.equipmentInventory.Add(bad.equipmentLoot[0].items[0]);
            Check(!SaveValidator.Validate(bad, out error), "duplicate loot ownership rejected");
            var legacy = Copy(file); legacy.version = 3;
            legacy.buildings.RemoveAll(b => b.kind == "Workshop");
            for (int i = 0; i < 35; i++) legacy.equipmentInventory.Add(EquipmentRecipes.Create(EquipmentRecipe.Anklet, 1));
            Check(SaveValidator.Validate(legacy, out error) && legacy.version == SaveFileV1.CurrentVersion && legacy.equipmentInventory.Count == 30 && legacy.equipmentLoot.Sum(l => l.items.Count) == 5, "legacy overfull inventory migrated without loss: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            w = Object.FindObjectsByType<Workshop>().First(x => x.Crafter != null);
            Near(w.Jobs[0].work, progress, "craft progress restored"); Check(!w.Crafter.CanReceiveOrders, "restored crafter locked");
            Check(Object.FindObjectsByType<Workshop>().Any(x => x.Ruined && x.Jobs.Count == 1 && x.IsDead), "ruined workshop restored");
            Check(Object.FindObjectsByType<Workshop>(FindObjectsInactive.Include).Any(x => !x.name.EndsWith("Template") && !x.gameObject.activeSelf && x.Jobs.Count == 1), "inactive workshop restored");
            var loot = Object.FindObjectsByType<EquipmentLoot>().Single(x => x.Items.Any(e => e.id == expectedLoot));
            other = CommanderRoster.Instance.Commanders[1]; Move(other, loot.transform.position + Vector3.right);
            Check(loot.TryCollect(other) && EquipmentInventory.Instance.Items.Any(e => e.id == expectedLoot), "dropped equipment restored and recoverable");
            GameMenuControllerCheck(w);
            // 보관함이 가득 찬 원정 보상은 현장에 남고, 사망 장비도 공용 보관함으로 순간 이동하지 않는다.
            w.Release();
            inventory = EquipmentInventory.Instance;
            while (!inventory.Full) inventory.Add(EquipmentRecipes.Create(EquipmentRecipe.Anklet, 0));
            var world = WorldMapManager.Instance; var ship = world.Transports.First();
            c = CommanderRoster.Instance.Commanders[0]; Move(c, ship.Position + Vector3.right * 3);
            Check(ship.TryBoard(new[] { c }), "board overflow reward test");
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
            Check(ship.TryDepart(site), "depart overflow reward test"); ship.Tick(ship.TravelSeconds);
            typeof(ExpeditionSite).GetField("<Cleared>k__BackingField", Any).SetValue(site, true);
            Check(ship.TryCollectRewards() && ship.EquipmentCargo.Count == 0
                && Object.FindObjectsByType<EquipmentLoot>().Any(l => Vector3.Distance(l.transform.position, ship.Position) < 2 && l.Items.Count > 0), "full inventory leaves reward on site");
            Check(ship.TryReturn(), "return overflow reward test"); ship.Tick(ship.TravelSeconds);
            other = CommanderRoster.Instance.Commanders[1]; var deathIds = other.PersonalState.equipment.Select(e => e.id).ToArray();
            Check(deathIds.Length > 0, "death test has equipment");
            UnityEngine.Random.InitState(925);
            for (int attempt = 0; attempt < 100 && !other.IsDead; attempt++)
            {
                if (!other.HasTroops) other.TryAssign(1);
                other.LoseTroops(other.TroopCount);
            }
            Check(other.IsDead && other.PersonalState.equipment.Count == 0
                && Object.FindObjectsByType<EquipmentLoot>().Any(l => deathIds.All(id => l.Items.Any(e => e.id == id))), "fatality drops every equipped item");
            return "PASS " + checks + " stage 3 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
    static void GameMenuControllerCheck(Workshop w)
    {
        AntColony.UI.GameMenuController.Instance.ShowWorkshop(w);
        Check(AntColony.UI.GameMenuController.Instance.ScreenName == "공방 / 장비 보관함", "workshop UI opens");
        var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>();
        Check(buttons.Any(b => b.GetComponentInChildren<UnityEngine.UI.Text>()?.text.Contains("취소") == true), "queue cancel UI");
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
    }
}
