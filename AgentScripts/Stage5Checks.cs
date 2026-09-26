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
using Random = UnityEngine.Random;
using Resource = AntColony.Data.ResourceType;
using PlayerSelectable = AntColony.Units.SelectableObject;

// Fresh Play: unity command run_script --file AgentScripts/Stage5Checks.cs --entry Stage5Checks.Main --timeout_ms 240000 --timeout 250
public static class Stage5Checks
{
    static int checks;
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + checks + ": " + label); checks++; }
    static void Near(float actual, float expected, string label) => Check(Mathf.Abs(actual - expected) < .02f, label + " actual=" + actual + " expected=" + expected);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Any).Invoke(o, args);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Any).SetValue(o, value);
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(75);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable commander position");
        c.Agent.enabled = false; c.transform.position = hit.position; c.ApplyMovementMode();
    }
    static void Traits(CommanderAnt c, params CommanderTrait[] values)
    {
        var t = new CommanderTraits(); foreach (var v in values) Check(t.TryAdd(v), "trait accepted " + v);
        t.SetLoyalty(80); c.ApplyTraits(t);
    }
    static void Relations(CommanderAnt a, CommanderAnt b, float value)
    { a.PersonalState.Relation(b.PersonalState.id).value = value; b.PersonalState.Relation(a.PersonalState.id).value = value; }
    static int Seed(float max)
    { for (int i = 0; i < 10000; i++) { Random.InitState(i); if (Random.value < max) return i; } throw new Exception("seed"); }
    static SaveFileV1 Copy(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage5-" + Guid.NewGuid().ToString("N"));
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 250928, mapSize = MapSize.Small, commanderDeath = CommanderDeathMode.Gentle }); await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            Object.FindAnyObjectByType<LocalIncursions>().enabled = false;
            var roster = CommanderRoster.Instance; var all = roster.Commanders.ToArray();
            var a = all[0]; var b = all[1]; var c = all[2]; var home = WorldMapManager.Instance.HomePosition;
            AntPool.Instance.Breed(300);
            foreach (var r in Enum.GetValues(typeof(Resource)).Cast<Resource>()) { ResourceManager.Instance.AddCapacity(r, 10000); ResourceManager.Instance.Add(r, 9000); }
            foreach (var unit in all) { Traits(unit); Move(unit, home + new Vector3(15, 0, 15)); unit.PersonalState.relations.Clear(); }
            Check(a.TryAssign(8), "workforce assigned");
            var loyalty = a.Traits.Loyalty; Check(a.ReturnTroops(4) == 4, "small recall accepted"); Near(a.Traits.Loyalty, loyalty, "less than half no penalty");
            Check(a.ReturnTroops(3) == 3, "half recall accepted"); Near(a.Traits.Loyalty, loyalty - 5, "half recall loyalty -5");
            Traits(a, CommanderTrait.Ambitious); a.OnTroopsRecalled(5, 10); Near(a.Traits.Loyalty, 70, "ambitious recall -10");
            Check(a.TryReward(), "reward available"); Near(a.Traits.Loyalty, 85, "ambitious reward +15"); Check(!a.TryReward(), "reward monthly limit");
            Traits(a, CommanderTrait.Loyal); a.OnTroopsRecalled(5, 10); Near(a.Traits.Loyalty, 80, "loyal recall zero");
            Traits(a, CommanderTrait.Glutton); a.OnHunger(); Near(a.Traits.Loyalty, 76, "glutton hunger -4");
            Traits(a, CommanderTrait.Ascetic); a.OnHunger(); Near(a.Traits.Loyalty, 80, "ascetic hunger zero");
            Traits(a); a.OnHunger(); Near(a.Traits.Loyalty, 78, "hunger -2");
            a.OnExpeditionVictory(); Near(a.Traits.Loyalty, 81, "victory +3");
            Traits(a, CommanderTrait.Wanderer); a.OnExpeditionVictory(); Near(a.Traits.Loyalty, 86, "wanderer victory +6");
            Traits(a, CommanderTrait.Homebody); a.OnExpeditionVictory(); Near(a.Traits.Loyalty, 81, "homebody victory +1");
            a.Social.expeditions = 9; a.OnExpeditionStarted(); Check(!a.Traits.Has(CommanderTrait.Homebody), "10 expeditions remove homebody");

            // Execution priorities, friendship death and captivity debt.
            Traits(a, CommanderTrait.Sociable); Traits(b, CommanderTrait.ColdBlooded); Traits(c, CommanderTrait.Loyal);
            a.PersonalState.originFaction = "same";
            CommanderAnt.OnPrisonerExecuted("unknown", "same", "test");
            Near(a.Traits.Loyalty, 65, "same faction execution -15 overrides personality"); Near(b.Traits.Loyalty, 83, "cold execution +3"); Near(c.Traits.Loyalty, 80, "loyal execution zero");
            a.PersonalState.originFaction = ""; Traits(a); Traits(b, CommanderTrait.Sociable); Relations(a, b, 50);
            var site = WorldMapManager.Instance.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
            typeof(CommanderAnt).GetProperty("Captor").SetValue(a, site);
            a.OnCaptured(); Check(b.PersonalState.rageRemaining == 60 && !b.CanReceiveOrders, "friend capture starts revenge and locks orders");
            b.PersonalState.rageRemaining = 0;
            a.TickCaptivity(600); Near(a.Traits.Loyalty, 80, "captive debt deferred"); Near(b.Traits.Loyalty, 60, "social friend abandonment -10 monthly");
            Check(a.Social.captiveDebt == 20, "two months debt saved");
            typeof(CommanderAnt).GetProperty("Captor").SetValue(a, null);
            Random.InitState(Seed(.3f)); a.OnRescued(); Near(a.Traits.Loyalty, 80, "debt -20 and rescue +20"); Near(b.Traits.Loyalty, 70, "social rescue +10");
            Check(a.Traits.Has(CommanderTrait.ColdBlooded), "captivity changes trait");
            Traits(b); Relations(a, b, 80); b.PersonalState.Relation(a.PersonalState.id).spouse = true;
            a.NotifyDowned(true, "전투"); Near(b.Traits.Loyalty, 70, "spouse death -10"); b.PersonalState.rageRemaining = 0;
            Traits(b, CommanderTrait.Loyal); a.NotifyDowned(true, "처형"); Near(b.Traits.Loyalty, 70, "preventable spouse death loyal half"); b.PersonalState.rageRemaining = 0;
            Traits(a, CommanderTrait.Robust); a.Social.seriousInjuries = 3; Call(a, "TickSocial", .01f); Check(!a.Traits.Has(CommanderTrait.Robust), "three serious injuries remove robust");
            Traits(c); c.Social.seriousInjuries = 3; Call(c, "TickSocial", .01f); Check(c.Traits.Has(CommanderTrait.Frail), "three serious injuries add frail");
            Traits(c, CommanderTrait.IronWill); c.Social.breakdowns = 4; c.StartMentalBreak(MentalBreak.Idle); Check(c.Traits.Has(CommanderTrait.Easygoing), "fifth breakdown lowers mental trait");
            c.PersonalState.mentalBreak = MentalBreak.None; c.PersonalState.breakRemaining = 0;

            foreach (var unit in all) { Traits(unit); unit.PersonalState.relations.Clear(); unit.PersonalState.rageRemaining = 0; unit.PersonalState.mentalBreak = MentalBreak.None; }
            Move(a, home + Vector3.right * 5); Move(b, a.Position + Vector3.forward * 2); Move(c, a.Position + Vector3.left * 2);
            // Social timers use a pair once; work compatibility and idle separation.
            var first = string.CompareOrdinal(a.PersonalState.id, b.PersonalState.id) < 0 ? a : b;
            var second = first == a ? b : a;
            Call(first, "TickRelations", 60f); Near(first.PersonalState.Relation(second.PersonalState.id).value, 0, "idle proximity is not shared combat");
            first.Social.combatSeen = second.Social.combatSeen = true; first.PersonalState.lastCombatSeconds = second.PersonalState.lastCombatSeconds = 0;
            Traits(first, CommanderTrait.Sociable); Call(first, "TickRelations", 60f); Near(first.PersonalState.Relation(second.PersonalState.id).value, 3, "social shared combat +3");
            first.Social.combatSeen = second.Social.combatSeen = false;
            Traits(first, CommanderTrait.Brave); Traits(second, CommanderTrait.Cautious); Relations(first, second, 0);
            Random.InitState(Seed(.1f)); Call(first, "TickRelations", 300f); Near(first.PersonalState.Relation(second.PersonalState.id).value, -10, "opposed courage quarrel");
            Traits(first); Traits(second); Relations(first, second, -70);
            first.TryAssign(first.CommandLimit - first.TroopCount); second.TryAssign(second.CommandLimit - second.TroopCount);
            int n1 = first.TroopCount, n2 = second.TroopCount; Random.InitState(Seed(.1f)); Call(first, "TickRelations", 60f);
            Check(first.TroopCount == n1 - Mathf.CeilToInt(n1 * .2f) && second.TroopCount == n2 - Mathf.CeilToInt(n2 * .2f), "duel costs 20 percent each");
            Check(first.PersonalState.injuries.Any(i => i.severity == InjurySeverity.Minor) || second.PersonalState.injuries.Any(i => i.severity == InjurySeverity.Minor), "duel loser minor injury");
            Relations(a, b, 50); Relations(a, c, 50); Relations(b, c, 50); Check(a.Faction().Count == 3, "mutual friends form faction");

            // Active skills: valid targets, cooldowns, delayed area damage, rally and real landing.
            foreach (var unit in all) { unit.PersonalState.relations.Clear(); unit.PersonalState.injuries.Clear(); unit.PersonalState.rageRemaining = 0; }
            a.PersonalState.equipment.Clear(); a.PersonalState.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.AcidSprayer, quality = 1 }); a.RefreshEquipment();
            a.Talents.levels[(int)CommanderActivity.Ranged] = 4; Check(!a.CanAcidRain, "acid level gate"); a.Talents.levels[(int)CommanderActivity.Ranged] = 5;
            var enemyGo = new GameObject("Stage5 target"); enemyGo.transform.position = a.Position + Vector3.forward * 4; var enemy = enemyGo.AddComponent<WildMonster>();
            Set(enemy, "currentHealth", 10000f);
            Check(!a.TryAcidRain(new Vector3(float.NaN, 0, 0)) && a.Social.acidCooldown == 0, "invalid acid target no cooldown");
            float damage = a.AttackDamage * .25f;
            Check(a.TryAcidRain(enemy.Position), "acid casts"); Check(!a.TryAcidRain(enemy.Position), "acid cooldown enforced");
            a.TickAdvancedSkills(.5f); Near(enemy.CurrentHealth, 10000, "acid waits one second");
            a.TickAdvancedSkills(3.5f); Near(enemy.CurrentHealth, 10000 - Mathf.Max(1, damage) * 4, "four acid ticks"); Near(a.Social.acidCooldown, 21, "acid cooldown elapsed");
            a.TickAdvancedSkills(21); Check(a.CanAcidRain, "acid ready again");
            a.PersonalState.equipment[0].weapon = WeaponKind.Pheromone; a.RefreshEquipment(); a.Talents.levels[(int)CommanderActivity.Command] = 5;
            Move(b, a.Position + Vector3.right); Check(a.TryRally(), "rally casts"); Near(b.Social.rallyRemaining, 8, "rally nearby ally"); Check(!a.TryRally(), "rally cooldown enforced");
            b.TickAdvancedSkills(8); Near(b.Social.rallyRemaining, 0, "rally expires");
            a.PersonalState.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Armor, armor = ArmorKind.Wings, quality = 1 }); a.RefreshEquipment();
            Check(a.IsFlying && !a.Agent.enabled, "wings flight");
            Check(!a.TryDive(new Vector3(float.PositiveInfinity, 0, 0)), "invalid dive denied");
            float beforeDive = enemy.CurrentHealth; float diveDamage = a.AttackDamage * 1.5f;
            Check(a.TryDive(enemy.Position), "dive casts"); Check(!a.CanReceiveOrders, "dive locks commands");
            a.TickAdvancedSkills(1); Check(!a.Social.diving && !a.IsFlying && a.Agent.enabled, "dive lands on navmesh"); Near(enemy.CurrentHealth, beforeDive - diveDamage, "dive area damage");
            a.TickAdvancedSkills(3); Check(a.IsFlying && !a.Agent.enabled && a.Social.diveCooldown > 0, "flight returns without cooldown refund");
            Object.Destroy(enemyGo); await Task.Delay(30);

            // Faction rebellion, own troop accounting, targeting and captive identity.
            a.PersonalState.equipment.Clear(); a.RefreshEquipment();
            foreach (var unit in new[] { a, b, c }) { Traits(unit); unit.PersonalState.relations.Clear(); unit.PersonalState.rageRemaining = 0; unit.PersonalState.mentalBreak = MentalBreak.None; }
            Relations(a, b, 50); Relations(a, c, 50); Relations(b, c, 50);
            a.Traits.SetLoyalty(10); b.Traits.SetLoyalty(30); c.Traits.SetLoyalty(35);
            int assigned = AntPool.Instance.Assigned, leaving = a.TroopCount + b.TroopCount + c.TroopCount;
            Check(a.TryDeparture(), "loyalty departure begins");
            Check(new[] { a, b, c }.All(u => u.Social.departure == DepartureState.Rebellion), "low loyalty faction all rebels");
            Check(AntPool.Instance.Assigned == assigned - leaving, "rebel troops removed once from friendly pool");
            Check(!a.CanReceiveOrders && !a.GetComponent<PlayerSelectable>().enabled, "rebel unselectable and orders blocked");
            Check(CombatTargeting.CanAttack(UnitRole.Melee, a), "friendly combat can target rebel");
            Check(ReferenceEquals(CombatTargeting.FindNearestEnemy(a.Position, 1, UnitRole.Melee), a), "auto targeting finds rebel");
            a.TickDeparture(60); Check(a.Social.departure == DepartureState.Retreating, "rebel retreats at sixty seconds");
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any).Invoke(null, new object[] { BuildingKind.PrisonerCamp, UnitRole.Worker });
            Check(template != null, "prison template");
            var campGo = Object.Instantiate(template, home + Vector3.left * 10, Quaternion.identity); campGo.SetActive(true); var camp = campGo.GetComponent<PrisonerCamp>();
            string oldId = a.PersonalState.id;
            a.PersonalState.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Trinket, quality = 2 }); string gearId = a.PersonalState.equipment[0].id;
            a.TakeDamage(float.MaxValue); await Task.Delay(30);
            var prisoner = camp.Prisoners.FirstOrDefault(p => p.PersonalState.id == oldId);
            Check(prisoner != null && prisoner.PersonalState.equipment[0].id == gearId, "captured rebel preserves identity and equipment");
            Check(AntPool.Instance.Assigned == assigned - leaving, "defeated rebel cannot remove more friendly ants");
            Check(!roster.Commanders.Any(u => u.PersonalState.id == oldId), "no duplicate commander identity after capture");

            // Save while retreating and with a rebel prisoner; migrate v5 and reject malformed state.
            foreach (var unit in roster.Commanders) unit.CommandStop();
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out var error), "v6 snapshot: " + error);
            var invalid = Copy(file); invalid.commanders[0].personalState.social.acidCooldown = float.NaN; Check(!SaveValidator.Validate(invalid, out error), "NaN skill cooldown rejected");
            invalid = Copy(file); invalid.commanders[0].personalState.social.departure = (DepartureState)99; Check(!SaveValidator.Validate(invalid, out error), "unknown departure rejected");
            invalid = Copy(file); invalid.commanders[0].personalState.social = null; Check(!SaveValidator.Validate(invalid, out error), "missing v6 social state rejected");
            var legacy = Copy(file); legacy.version = 5;
            // Legacy had no rebels or captured player gear; normalize the synthetic fixture's population and equipment.
            legacy.colony.antsAssigned = legacy.commanders.Sum(u => u.troopCount) + legacy.buildings.Sum(v => v.scoutDispatchedAnts);
            Check(SaveValidator.Validate(legacy, out error) && legacy.version == SaveFileV1.CurrentVersion && legacy.commanders.All(u => u.personalState.social.departure == DepartureState.None), "v5 defaults migrate: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save rebels and captive: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load rebels and captive: " + error); await Ready();
            camp = Object.FindObjectsByType<PrisonerCamp>().First(p => p.Prisoners.Any(v => v.PersonalState.id == oldId)); prisoner = camp.Prisoners.First(p => p.PersonalState.id == oldId);
            Check(prisoner.PersonalState.equipment[0].id == gearId, "prison gear survives reload");
            Check(CommanderRoster.Instance.Commanders.Count(u => u.IsHostile) == 2, "two rebels restored once");
            Set(camp, "basePersuadeChance", 1f); Set(camp, "loyaltyPenalty", 0f);
            Check(camp.TryPersuade(prisoner), "recaptured rebel persuaded");
            var recruit = CommanderRoster.Instance.Commanders.First(u => u.PersonalState.id == oldId);
            Check(recruit.IsColonyMember && recruit.CanReceiveOrders && recruit.Traits.Loyalty == 30, "recruit allegiance and loyalty restored");
            Check(recruit.PersonalState.equipment[0].id == gearId, "recruit owns original gear");
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "second save/load: " + error); await Ready();
            Check(CommanderRoster.Instance.Commanders.Count(u => u.PersonalState.id == oldId) == 1, "no duplicates second reload");
            GameMenuController.Instance.Roster(); Canvas.ForceUpdateCanvases();
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("충성")), "loyalty visible in roster");
            recruit = CommanderRoster.Instance.Commanders.First(u => u.PersonalState.id == oldId);
            GameMenuController.Instance.Details(recruit); Canvas.ForceUpdateCanvases();
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("파벌:")), "relations and faction detail visible");
            Check(GameMenuController.LoyaltyColor(15) != GameMenuController.LoyaltyColor(30) && GameMenuController.LoyaltyColor(30) != Color.white, "loyalty danger colors");
            return "PASS " + checks + " stage 5 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}

