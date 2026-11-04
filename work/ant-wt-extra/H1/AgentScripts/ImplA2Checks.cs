using UnityEngine.AI;
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
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ImplA2Checks
{
    static int count;
    static void Check(bool value, string text) { if (!value) throw new Exception("A2 FAIL " + count + ": " + text); count++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
    }
    static NewGameOptions Options(int n)
    {
        var o = new NewGameOptions { startingCount = n, seed = 261023, mapSize = MapSize.Small, biome = MapBiome.Forest,
            startingTeam = Enumerable.Range(0, n).Select(StartingCandidate.Roll).ToArray() };
        foreach (var candidate in o.startingTeam) candidate.traits.values.Clear();
        return o;
    }
    static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name);

    public static async Task<string> Main()
    {
        var start = await StartAndSupplies();
        var craft = await ImplA2CraftChecks.Main();
        var fire = await ImplA2FireJoyChecks.Main();
        return start + "\n" + craft + "\n" + fire;
    }

    public static async Task<string> StartAndSupplies()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplA2-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            for (var i = 0; i < 100; i++) { var c = StartingCandidate.Roll(0); Check(c.Valid && c.ageYears >= 18 && c.ageYears <= 30, "young full candidate"); }
            var invalid = Options(1); invalid.startingTeam[0].traits.TryAdd(CommanderTrait.CannotBuild);
            Check(StartingCandidate.Missing(invalid.startingTeam).Contains("건설"), "team construction gap");
            bool blocked = false; try { CrashLandingStart.Prepare(invalid); } catch (InvalidOperationException) { blocked = true; }
            Check(blocked, "G03 API blocks incapable team");

            var opts = Options(3); opts.startingTeam[0].traits.TryAdd(CommanderTrait.Glutton); opts.startingTeam[0].traits.TryAdd(CommanderTrait.ThinLegs);
            CrashLandingStart.Begin(opts); await Ready();
            var all = CommanderRoster.Instance.Commanders.ToArray();
            Check(all.Length == 3, "three survivors");
            Check(AntPool.Instance.Total == 0 && ColonyPopulation.Instance.Total == 0 && ColonyPopulation.Instance.HousingCapacity == 0, "zero citizens and housing");
            Check(ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) == 0 && ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Soil) == 0, "no starting warehouse bundle");
            for (var i = 0; i < all.Length; i++)
            {
                var c = all[i];
                Check(c.PersonalState.ageMonths == opts.startingTeam[i].ageYears * 12, "selected age transferred");
                Check(c.Talents.levels.SequenceEqual(opts.startingTeam[i].talents.levels), "selected skills transferred");
                Check(c.PersonalState.relations.Count == 0 && c.PersonalState.appearanceSeed == opts.startingTeam[i].appearanceSeed, "appearance and no prior relationships");
                Check(c.Weapon?.weapon == WeaponKind.WoodenSword && c.PersonalState.toolTier == 1 && !string.IsNullOrEmpty(c.PersonalState.ordinaryClothes), "starter clothes weapon tools");
                Check(c.CarriedRationMeals == 14 && c.StartingLoadWeight <= c.TotalLoadCapacity && c.PersonalState.carriedRations.Stalest >= 24 * 150 - 1, "G12 fourteen meals and load limit");
            }
            Check(Object.FindObjectsByType<CrashWreck>(FindObjectsSortMode.None).Length == 1, "single salvageable wreck");
            Check(ResourceNode.Available.Where(n => n.IsLooseCargo && n.ResourceType == AntColony.Data.ResourceType.Stone).Sum(n => n.AmountRemaining) >= 20, "twenty hand-carried surface stones");
            Check(!ResourceNode.Available.Any(n => n.IsLooseCargo && n.ResourceType.IsFood()
                && AntColony.Map.HomeMapBuilder.CurrentWorldBounds.Contains(new Vector3(n.transform.position.x, 0, n.transform.position.z))), "no starting floor food"); // 원정 지역 중립 자원지는 제외
            var who = all[0]; foreach (var c in all) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            who.PersonalState.meal.satiety = 20; var before = who.PersonalState.carriedRations.Total;
            who.TickDuty(.1f);
            Check(who.IsEating && who.PersonalState.carriedRations.Total == before - who.RationPortion, "eat from own supplies without storage");
            who.TickDuty(GameBalance.EatSeconds * 2);
            Check(who.Satiety > 90 && who.PersonalState.carriedRations.Total == before - who.RationPortion, "meal consumes once");
            Check(who.DropCarriedRations(1) && who.CarriedRationMeals == 12, "drop one meal");
            Check(ResourceManager.Instance.GetAmount(AntColony.Data.ResourceType.Food) == 0, "personal and floor stock not double-counted");

            GameMenuController.Instance.CarriedSupplies(); Check(Button("Drop Rations " + who.CommanderName).interactable, "supplies drop UI");
            var menu = GameMenuController.Instance;
            typeof(GameMenuController).GetField("options", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, invalid);
            // UI keeps three preview slots while only the selected prefix is used.
            invalid.startingTeam = new[] { invalid.startingTeam[0], StartingCandidate.Roll(1), StartingCandidate.Roll(2) };
            typeof(GameMenuController).GetMethod("NewGameScreen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
            Check(!Button("Start Game").interactable, "G03 start button disabled");
            Check(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("수행자 부족: 건설")), "G03 missing work shown");
            var old = invalid.startingTeam[0]; Button("Reroll Candidate 0").onClick.Invoke();
            Check(!ReferenceEquals(old, invalid.startingTeam[0]) && invalid.startingTeam[0].Valid, "whole slot reroll");
            Check(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("잠재력 미평가")), "G24 no potential disclosure");
            menu.Resume(); Time.timeScale = 0;

            who.CommandStop();
            Check(SaveSystem.TrySave(false, 0, out var error), "save A2 state: " + error);
            var id = who.PersonalState.id; var rationCount = who.PersonalState.carriedRations.Total;
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load A2 state: " + error); await Ready();
            who = CommanderRoster.Instance.Commanders.First(c => c.PersonalState.id == id);
            Check(who.PersonalState.carriedRations.Total == rationCount && who.PersonalState.toolTier == 1 && who.Weapon.weapon == WeaponKind.WoodenSword, "supplies survive save/load without duplication");
            Check(Object.FindObjectsByType<CrashWreck>(FindObjectsSortMode.None).Length == 1, "wreck survives save/load once");

            CrashLandingStart.Begin(Options(1)); await Ready();
            Check(CommanderRoster.Instance.Count == 1 && CommanderRoster.Instance.Commanders[0].CarriedRationMeals == 14, "one survivor start scales food");
            Check(Object.FindObjectsByType<CrashWreck>(FindObjectsSortMode.None).Length == 1, "one-survivor same wreck");
            return "PASS A2 start/supplies/UI " + count;
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}

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
            foreach (var t in new[] { AntColony.Data.ResourceType.Wood, AntColony.Data.ResourceType.Stone, AntColony.Data.ResourceType.Fiber, AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.Soil }) { rm.AddCapacity(t, 1000); rm.Add(t, 200); }
            var p = Object.FindFirstObjectByType<BuildingPlacementController>();
            var carpentry = Place(p, c, BuildingKind.Carpentry);
            Check(carpentry != null && carpentry.Needs.Count == 1 && carpentry.Needs[0].type == AntColony.Data.ResourceType.Wood && carpentry.Needs[0].need == 10, "carpentry exact wood ten");
            Check(carpentry.Building.GetComponent<BuildingBase>().Data.constructionAnts == 0, "carpentry no citizens");
            var desk = Place(p, c, BuildingKind.ScienceLab);
            Check(desk != null && desk.Needs.Count == 2 && desk.Needs.Any(n => n.type == AntColony.Data.ResourceType.Wood && n.need == 20) && desk.Needs.Any(n => n.type == AntColony.Data.ResourceType.Stone && n.need == 10), "research desk exact materials");
            Check(ScienceLab.PrerequisitesMet && desk.Building.GetComponent<BuildingBase>().Data.constructionAnts == 0, "research no population barracks power gate");
            p.CancelPlacement();

            var rock = new GameObject("A2 test vein").AddComponent<ResourceNode>(); rock.ConfigureNatural(AntColony.Data.ResourceType.Stone, 20, 0);
            c.PersonalState.toolTier = 1; Check(rock.RequiresMiningTool && !rock.CanGatherWith(c), "wood tools cannot mine vein");
            c.PersonalState.toolTier = 2; Check(rock.CanGatherWith(c), "stone tools permit vein mining"); c.PersonalState.toolTier = 1;
            var loose = new GameObject("A2 loose stone").AddComponent<ResourceNode>(); loose.ConfigureLoot(AntColony.Data.ResourceType.Stone, 20);
            Check(loose.CanGatherWith(c), "loose stone hand hauling");
            var beforeWood = rm.GetAmount(AntColony.Data.ResourceType.Wood); var beforeStone = rm.GetAmount(AntColony.Data.ResourceType.Stone);
            Check(c.StartGroundCraft(GroundRecipe.StoneTools), "stone tools no bench or research");
            Check(rm.GetAmount(AntColony.Data.ResourceType.Wood) == beforeWood - 2 && rm.GetAmount(AntColony.Data.ResourceType.Stone) == beforeStone - 3, "stone tools exact cost once");
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
            var bow = EquipmentInventory.Instance.Items.Last();
            Check(EquipmentInventory.Instance.Equip(c, bow) && c.Role == UnitRole.Ranged
                && Mathf.Approximately(c.Data.attackRange, 12) && Mathf.Approximately(c.Data.attackInterval, 2), "short bow actual ranged role and 12m/2s profile (Claude combat connection)");
            var sword = EquipmentInventory.Instance.Items.First(e => e.slot == EquipmentSlot.Weapon && e.weapon == WeaponKind.WoodenSword);
            Check(EquipmentInventory.Instance.Equip(c, sword) && c.Role == UnitRole.Melee && c.Data.role == UnitRole.Melee
                && EquipmentInventory.Instance.Equip(c, bow) && c.Role == UnitRole.Ranged && c.Data.role == UnitRole.Ranged
                && Mathf.Approximately(c.Data.attackRange, 12) && Mathf.Approximately(c.Data.attackInterval, 2), "wooden sword melee and short bow 12m/2s after re-equipping");

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

public static class ImplA2FireJoyChecks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL A2 fire/joy: " + message); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static T Build<T>(BuildingKind kind, Vector3 position) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        var go = Object.Instantiate(template, position, Quaternion.identity); go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static void JoyTick(CommanderAnt c, float seconds) => typeof(CommanderAnt).GetMethod("TickJoy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, new object[] { seconds });
    static void Bored(CommanderAnt c)
    {
        c.CommandStop(); c.PersonalState.joy = new CommanderJoyState { joy = 20 };
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplA2FireJoy-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var team = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray();
            foreach (var candidate in team) candidate.traits.values.Clear();
            CrashLandingStart.Begin(new NewGameOptions { startingCount = 3, startingTeam = team, seed = 261023, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0; GameSession.Instance.MarkStarted(0, 10);
            var cs = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember && !c.IsDead).ToArray();
            Check(cs.Length >= 2, "two commanders for minimum users test"); var c = cs[0]; var d = cs[1];
            foreach (var commander in cs) { commander.CommandStop(); commander.SetJobEnabled(CommanderJobs.All, false); commander.Traits.values.Clear(); }

            var old = new CommanderWorkState { jobs = (CommanderJobs)32767, priorities = Enumerable.Range(0, 15).Select(i => i % 9 + 1).ToArray() };
            var previous = old.priorities.ToArray(); Check(old.Valid, "old 15 jobs validate and migrate"); old.MigrateLegacyJobs();
            Check(old.priorities.Length == 16 && old.priorities.Take(15).SequenceEqual(previous) && old.Priority(CommanderJobs.Firefighting) == 9, "15 priorities preserved, firefighting 9");
            Check(new CommanderWorkState().Priority(CommanderJobs.Firefighting) == 9 && CommanderWorkState.JobCount == 16, "new job defaults");
            var disabled = new CommanderWorkState { jobs = CommanderJobs.All & ~CommanderJobs.Firefighting };
            disabled = JsonUtility.FromJson<CommanderWorkState>(JsonUtility.ToJson(disabled));
            Check(disabled.Valid && disabled.Priority(CommanderJobs.Firefighting) == 0, "explicit firefighting disabled survives roundtrip");
            GameMenuController.Instance.WorkSchedule();
            Check(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Any(b => b.name == "Priority Firefighting"), "firefighting UI column");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;

            foreach (var s in RecreationSpot.All.ToArray()) s.gameObject.SetActive(false);
            Bored(c); JoyTick(c, .01f); Check(c.IsPlaying && c.PersonalState.joy.freeWalk, "free walking without facility");
            var joy = c.Joy; JoyTick(c, GameBalance.PlaySeconds);
            Check(Mathf.Abs(c.Joy - (joy - GameBalance.JoyPerSecond * GameBalance.PlaySeconds + GameBalance.PlayJoy * .5f)) < .02f, "free walking recovers 50 percent");

            var camp = Build<RecreationSpot>(BuildingKind.Campfire, c.Position + Vector3.right * 2);
            Check(camp.MinimumUsers == 1 && camp.Seats == 6, "campfire 1 / 6");
            Bored(c); JoyTick(c, .01f); Check(c.PlaySpot == camp, "solo campfire starts"); joy = c.Joy; JoyTick(c, GameBalance.PlaySeconds);
            Check(Mathf.Abs(c.Joy - (joy - GameBalance.JoyPerSecond * GameBalance.PlaySeconds + GameBalance.PlayJoy)) < .02f, "solo campfire full recovery");
            camp.gameObject.SetActive(false);
            var gamble = Build<RecreationSpot>(BuildingKind.GamblingDen, c.Position + Vector3.right * 2);
            Check(gamble.MinimumUsers == 2 && gamble.Seats == 4, "gambling 2 / 4");
            Bored(c); JoyTick(c, .01f); float remain = c.PersonalState.joy.playSeconds; JoyTick(c, 2);
            Check(c.PersonalState.joy.playSeconds == remain && !c.PersonalState.joy.groupStarted, "solo gambling waits");
            Check(NavMesh.SamplePosition(c.Position, out var near, 4, NavMesh.AllAreas) && d.Agent.Warp(near.position), "partner nearby");
            Bored(d); JoyTick(d, .01f); JoyTick(c, .01f); JoyTick(d, .01f);
            Check(c.PersonalState.joy.groupStarted && d.PersonalState.joy.groupStarted, "two start gambling");
            var savedJoy = JsonUtility.FromJson<CommanderJoyState>(JsonUtility.ToJson(c.PersonalState.joy));
            Check(savedJoy.Valid && savedJoy.groupStarted, "joy state roundtrip");
            JoyTick(c, GameBalance.PlaySeconds); JoyTick(d, GameBalance.PlaySeconds); gamble.gameObject.SetActive(false);

            c.PersonalState.joy.joy = 100; c.WorkState.duty = CommanderDuty.Civilian; c.CommandStop();
            var wall = Build<Wall>(BuildingKind.LeafWall, c.Position + Vector3.forward * 2);
            Check(WallFire.Ignite(wall), "ignite"); var fire = wall.GetComponent<WallFire>();
            Check(fire.Managed, "home fire managed"); c.SetJobPriority(CommanderJobs.Firefighting, 9); c.TickDuty(2);
            Check(c.ServiceJob == CommanderJobs.Firefighting && c.ServiceTarget == wall, "autonomous firefighting");
            c.TickDuty(2.5f); Check(fire.Burning && Mathf.Abs(fire.ExtinguishProgress - .5f) < .01f, "half after 2.5s");
            var snapshot = fire.CaptureState(); fire.RestoreState(JsonUtility.FromJson<WallFire.State>(JsonUtility.ToJson(snapshot)));
            Check(Mathf.Abs(fire.ExtinguishProgress - .5f) < .01f, "fire state roundtrip");
            var fireWorkerId = c.PersonalState.id;
            Check(SaveSystem.TrySave(false, 0, out var error), "save active fire and service order: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load active fire and service order: " + error); await Ready();
            c = CommanderRoster.Instance.Commanders.First(x => x.PersonalState.id == fireWorkerId);
            fire = Object.FindObjectsByType<WallFire>(FindObjectsSortMode.None).Single(f => f.Burning); wall = fire.GetComponent<Wall>();
            Check(Mathf.Abs(fire.ExtinguishProgress - .5f) < .01f && c.ServiceTarget == wall && c.ServiceJob == CommanderJobs.Firefighting,
                "fire progress and order restored (Claude save connection)");
            c.TickDuty(2.5f); Check(!fire.Burning, "extinguished after 5s without water or tools");
            Check(WallFire.Ignite(wall), "can reignite"); c.CommandStop(); c.WorkState.duty = CommanderDuty.Deployed; c.TickDuty(2);
            Check(c.ServiceTarget == null && fire.ExtinguishProgress == 0, "deployed does not automatically extinguish");
            Check(c.CommandExtinguish(fire), "explicit deployed firefighting"); c.TickDuty(5);
            Check(!fire.Burning, "explicit deployed finishes"); c.WorkState.duty = CommanderDuty.Civilian;
            Object.Destroy(wall.gameObject); if (camp) Object.Destroy(camp.gameObject); if (gamble) Object.Destroy(gamble.gameObject); // 불러오기로 씬이 바뀌면 이미 없다
            return "PASS ImplA2FireJoyChecks " + checks;
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
