using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using AntColony.Buildings;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class OrganicCombat1114Checks
{
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static string log;
    static int passed;
    static readonly List<WildMonster> fixtures = new List<WildMonster>();
    static Vector3 origin;
    static void Check(bool ok, string name)
    {
        log += (ok ? "PASS " : "FAIL ") + name + "\n";
        File.WriteAllText("work/organic-combat-1114/combat-checks.txt", log);
        if (!ok) throw new Exception(name);
        passed++;
    }
    static object Call(object host, string method, params object[] args) => typeof(WildMonster).GetMethod(method, Flags).Invoke(host, args);
    static void Set(WildMonster host, string field, object value) => typeof(WildMonster).GetField(field, Flags).SetValue(host, value);
    static OrganicCombatState State(WildMonster host) => (OrganicCombatState)typeof(WildMonster).GetField("organic", Flags).GetValue(host);
    static void Ally(WildMonster host, bool value) => typeof(WildMonster).GetProperty("Allied").SetValue(host, value);
    static async Task Wait(Func<bool> condition, int seconds = 60)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < until) await Task.Delay(100);
        if (!condition()) throw new Exception("Wait timed out");
    }
    static WildMonster Spawn(OrganicEnemyKind kind, Vector3 position, bool allied = false, int horde = 0)
    {
        var local = SceneObjects.Any<LocalIncursions>();
        NavMesh.SamplePosition(position, out var ground, 10, NavMesh.AllAreas);
        var monster = (WildMonster)typeof(LocalIncursions).GetMethod("RestoreSpawn", Flags).Invoke(local, new object[] { false, ground.position });
        monster.name = "OrganicCheck_" + kind;
        monster.ConfigureWeakIntruder(); monster.ConfigureOrganic(kind);
        if (horde > 0) Call(monster, "ConfigureHorde", horde);
        monster.MakeRaider(); Ally(monster, allied); monster.gameObject.SetActive(true);
        typeof(LocalIncursions).GetMethod("RestoreVisitor", Flags).Invoke(local, new object[] { monster });
        fixtures.Add(monster);
        return monster;
    }
    static void Clear()
    {
        foreach (var monster in fixtures) if (monster != null) Object.DestroyImmediate(monster.gameObject);
        fixtures.Clear(); OrganicCombatEffects.Restore(null);
        foreach (var corpse in Corpse.All.ToArray()) if (corpse != null && corpse.Data.organicKind > 0) Object.DestroyImmediate(corpse.gameObject);
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        log = ""; passed = 0;
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "OrganicCombat-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 261114, mapSize = MapSize.Small, startingCount = 3 });
            await Wait(() => !SaveSystem.Busy && GameSession.Instance.GameStarted && SaveCatalog.Ready);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            Check(NavMesh.SamplePosition(HomeReturnPoint.Anchor + Vector3.right * 10, out var ground, 15, NavMesh.AllAreas), "fixture NavMesh"); origin = ground.position;
            for (int i = 1; i <= 17; i++)
            {
                var monster = Spawn((OrganicEnemyKind)i, origin);
                Check(monster.GetComponent<AntVisual>() == null && monster.GetComponent<OrganicEnemyVisual>() != null
                    && monster.GetComponentsInChildren<Animator>().Length == 1, "single organic visual " + (OrganicEnemyKind)i);
                var animator = monster.GetComponent<OrganicEnemyVisual>().Animator;
                var skin = monster.GetComponentInChildren<SkinnedMeshRenderer>(); var mesh = new Mesh();
                foreach (var state in new[] { "Idle", "Move", "Attack", "Hit", "Death" })
                {
                    animator.Play("Base Layer." + state, 0, .5f); animator.Update(0); skin.BakeMesh(mesh, true);
                    var vertices = mesh.vertices; var bounds = new Bounds(skin.transform.TransformPoint(vertices[0]), Vector3.zero);
                    foreach (var vertex in vertices) bounds.Encapsulate(skin.transform.TransformPoint(vertex));
                    if (bounds.size.magnitude > 8 || bounds.size.magnitude < .1f) throw new Exception("Invalid world model size " + (OrganicEnemyKind)i + "/" + state + " " + bounds.size);
                }
                Object.DestroyImmediate(mesh);
                Check(true, "world size stable across clips " + (OrganicEnemyKind)i);
            }
            Check(Enumerable.Range(0, 40).Select(i => OrganicEnemyRules.Select(8, i, EnemyAim.General)).Distinct().Count() == 17, "all 17 species selectable");
            Check(Enumerable.Range(1, 3).All(w => Enumerable.Range(0, 30).All(i => !OrganicEnemyRules.Ranged(OrganicEnemyRules.Select(w, i, EnemyAim.General)))), "early three waves melee preserved");
            Clear();

            var melee = Spawn(OrganicEnemyKind.ClawHunter, origin);
            var victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward, true);
            Set(melee, "currentTarget", victim); Set(melee, "attackTimer", 0f);
            float health = victim.CurrentHealth;
            Call(melee, "BeginOrganicStrike"); Call(melee, "TickOrganic", .4f);
            Check(victim.CurrentHealth == health && melee.OrganicPhase == 1, "windup has no premature damage");
            Call(melee, "TickOrganic", .4f);
            Check(victim.CurrentHealth < health && melee.OrganicPhase == 0, "melee impact after windup");
            Set(melee, "attackTimer", 0f); health = victim.CurrentHealth;
            Call(melee, "BeginOrganicStrike"); Call(melee, "OrganicHurt"); Call(melee, "TickOrganic", .8f);
            Check(victim.CurrentHealth == health, "hit interrupts prepared strike");
            Set(melee, "attackTimer", 0f); Call(melee, "BeginOrganicStrike"); victim.transform.position += Vector3.forward * 10;
            Call(melee, "TickOrganic", .8f);
            Check(victim.CurrentHealth == health && melee.OrganicPhase == 0, "out of range cancels prepared strike");
            Clear();

            var shooter = Spawn(OrganicEnemyKind.NeedleStalker, origin);
            victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward * 3, true);
            health = victim.CurrentHealth;
            OrganicCombatEffects.Fire(shooter, victim, victim.Position, 2);
            Check(victim.CurrentHealth == health && OrganicCombatEffects.Ensure().PendingCount == 1, "projectile travels before impact");
            Object.DestroyImmediate(shooter.gameObject); OrganicCombatEffects.Ensure().Tick(1);
            Check(victim.CurrentHealth == health - 2, "launched projectile survives attacker death");
            shooter = Spawn(OrganicEnemyKind.NeedleStalker, origin);
            health = victim.CurrentHealth; OrganicCombatEffects.Fire(shooter, victim, victim.Position, 2);
            victim.transform.position += Vector3.right * 2; OrganicCombatEffects.Ensure().Tick(1);
            Check(victim.CurrentHealth == health, "aimed projectile can be dodged");
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.SoilWall, UnitRole.Worker });
            var wallObject = Object.Instantiate(template, origin + Vector3.forward * 1.5f, Quaternion.identity); wallObject.SetActive(true);
            var wall = wallObject.GetComponent<BuildingBase>();
            victim.transform.position = origin + Vector3.forward * 3; Physics.SyncTransforms();
            Check(OrganicCombatEffects.BlockingBarrier(shooter.Position, victim.Position, victim) == wall, "wall blocks shot line");
            health = victim.CurrentHealth; OrganicCombatEffects.Fire(shooter, victim, victim.Position, 2); OrganicCombatEffects.Ensure().Tick(1);
            Check(victim.CurrentHealth == health, "projectile does not damage through wall");
            float wallHealth = wall.CurrentHealth;
            OrganicCombatEffects.Corrode(shooter, wall, 4); OrganicCombatEffects.Ensure().Tick(.5f);
            Check(wall.CurrentHealth == wallHealth, "corrosion waits for first tick");
            OrganicCombatEffects.Ensure().Tick(.6f);
            Check(wall.CurrentHealth < wallHealth && OrganicCombatEffects.Ensure().PendingCount == 1, "corrosion has bounded repeated ticks");
            OrganicCombatEffects.Ensure().Tick(4);
            Check(OrganicCombatEffects.Ensure().PendingCount == 0, "corrosion expires");
            Object.DestroyImmediate(wallObject);
            Clear();

            var artillery = Spawn(OrganicEnemyKind.SiegeBrooder, origin);
            var friendly = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward * 3);
            victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward * 3 + Vector3.right * .6f, true);
            Physics.SyncTransforms(); health = victim.CurrentHealth;
            OrganicCombatEffects.Fire(artillery, victim, victim.Position, 2); OrganicCombatEffects.Ensure().Tick(2);
            Check(victim.CurrentHealth < health && friendly.CurrentHealth == 15, "splash excludes friendly aliens");
            Clear();

            var commander = SceneObjects.All<CommanderAnt>().First(c => c.isActiveAndEnabled);
            var commanderAgent = commander.GetComponent<NavMeshAgent>();
            commander.CommandStop();
            NavMesh.SamplePosition(origin + Vector3.forward * 4, out var pullStart, 2, NavMesh.AllAreas);
            commanderAgent.Warp(pullStart.position); Vector3 previousPosition = commander.Position;
            typeof(OrganicCombatEffects).GetMethod("Pull", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { commander, origin });
            float pulled = Vector3.Distance(previousPosition, commander.Position);
            Check(pulled > .1f && pulled <= 2.1f, "hook pull is bounded and stays on NavMesh");

            var shield = Spawn(OrganicEnemyKind.Bulwark, origin); shield.transform.rotation = Quaternion.identity;
            Check(shield.OrganicDamageMultiplier(origin + Vector3.forward * 4, true, false) < 1, "frontal carapace protection");
            Check(shield.OrganicDamageMultiplier(origin - Vector3.forward * 4, true, false) == 1
                && shield.OrganicDamageMultiplier(origin + Vector3.forward * 4, true, true) == 1, "rear and splash bypass shield");
            var protectedAlly = Spawn(OrganicEnemyKind.ClawHunter, origin - Vector3.forward);
            Check(protectedAlly.OrganicDamageMultiplier(origin + Vector3.forward * 4, true, false) < 1, "carapace protects ally behind");
            Clear();
            var veil = Spawn(OrganicEnemyKind.VeilWeaver, origin); State(veil).mistLeft = 5;
            protectedAlly = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.right);
            Check(protectedAlly.OrganicDamageMultiplier(origin + Vector3.forward * 4, true, false) == .75f
                && protectedAlly.OrganicDamageMultiplier(origin + Vector3.forward * 4, false, false) == 1, "mist reduces ranged only without immunity");
            Clear();

            var healer = Spawn(OrganicEnemyKind.BloomMender, origin);
            var horde = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.right, horde: 3);
            Set(horde, "currentHealth", 29f); int bodies = horde.HordeCount;
            Call(healer, "MendNearby");
            Check(horde.CurrentHealth == 30 && horde.HordeCount == bodies, "healing cannot resurrect lost horde bodies");
            Set(horde, "currentHealth", 25f); Call(healer, "MendNearby");
            Check(horde.CurrentHealth == 25, "healing does not stack during target lock");
            var secondHealer = Spawn(OrganicEnemyKind.BloomMender, origin + Vector3.forward);
            Set(secondHealer, "currentHealth", 5f); Call(healer, "MendNearby");
            Check(secondHealer.CurrentHealth == 5, "healers cannot sustain each other indefinitely");
            Clear();

            var reclaimer = Spawn(OrganicEnemyKind.CarrionReclaimer, origin);
            victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.right);
            var organicCorpse = Corpse.Drop(victim, CorpseKind.Wildlife, "회수 검수 사체");
            Set(reclaimer, "currentHealth", 5f); State(reclaimer).corpsePosition = organicCorpse.Position;
            int corpseCount = Corpse.All.Count;
            Call(reclaimer, "ConsumeCorpse");
            Check(reclaimer.CurrentHealth == 10 && Corpse.All.Count == corpseCount - 1, "reclaimer consumes corpse without resurrection");
            Clear();

            var carrier = Spawn(OrganicEnemyKind.BroodCarrier, origin);
            int before = SceneObjects.Any<LocalIncursions>().Visitors.Count(v => v != null && !v.IsDead);
            Call(carrier, "ReleaseYoung"); Call(carrier, "ReleaseYoung");
            int after = SceneObjects.Any<LocalIncursions>().Visitors.Count(v => v != null && !v.IsDead);
            Call(carrier, "ReleaseYoung");
            Check(carrier.OrganicSpawnCharges == 0 && after > before && after <= before + 4
                && SceneObjects.Any<LocalIncursions>().Visitors.Count(v => v != null && !v.IsDead) == after, "finite brood tracked as wave visitors");

            // 실제 저장: 소모한 방출 횟수, 공격 준비, 비행 중 투사체, 사체를 함께 왕복한다.
            shooter = Spawn(OrganicEnemyKind.NeedleStalker, origin + Vector3.left * 2);
            victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward * 3, true);
            Set(shooter, "currentTarget", victim); Call(shooter, "BeginOrganicStrike"); Call(shooter, "TickOrganic", .2f);
            OrganicCombatEffects.Fire(shooter, victim, victim.Position, 1);
            Corpse.Drop(carrier, CorpseKind.Wildlife, "검수 생물 사체");
            Check(SaveSystem.TrySave(false, 0, out var error), "actual save: " + error);
            string path = SaveSlots.PathFor(false, 0);
            File.WriteAllText("work/organic-combat-1114/test-save-path.txt", path);
            Check(SaveValidator.TryParse(File.ReadAllText(path), out var saved, out error), "new save passes parser: " + error);
            var savedOrganic = saved.monsters.First(m => m.runtime?.organic != null).runtime.organic;
            int savedCharges = savedOrganic.spawnCharges; savedOrganic.spawnCharges = 3;
            Check(!SavePreflight.Validate(saved, out _), "invalid spawn charges rejected before restore"); savedOrganic.spawnCharges = savedCharges;
            Check(SaveSystem.TryLoad(path, out error), "actual load: " + error);
            await Wait(() => !SaveSystem.Busy); Time.timeScale = 0;
            carrier = WildMonster.All.First(m => m.name == "OrganicCheck_BroodCarrier");
            shooter = WildMonster.All.First(m => m.name == "OrganicCheck_NeedleStalker");
            Check(carrier.OrganicSpawnCharges == 0, "saved brood charges preserved");
            Check(shooter.OrganicPhase == 1 && Math.Abs(State(shooter).phaseLeft - .54f) < .03f, "saved windup preserved");
            Check(OrganicCombatEffects.Ensure().PendingCount == 1, "saved projectile preserved");
            Check(Corpse.All.Any(c => c.Data.organicKind == (int)OrganicEnemyKind.BroodCarrier && c.GetComponentInChildren<Animator>() != null), "saved organic corpse model preserved");
            Check(shooter.GetComponent<AntVisual>() == null && shooter.GetComponentsInChildren<Animator>().Length == 1, "load has no duplicate human visual");

            foreach (var monster in WildMonster.All.ToArray()) if (monster.IsOrganic) Object.DestroyImmediate(monster.gameObject);
            OrganicCombatEffects.Restore(null);
            foreach (var unit in SceneObjects.All<CommanderAnt>()) unit.enabled = false;
            var runner = Spawn(OrganicEnemyKind.ClawHunter, origin);
            victim = Spawn(OrganicEnemyKind.ClawHunter, origin + Vector3.forward * 6, true);
            typeof(WildMonster).GetProperty("Docile").SetValue(victim, true);
            Set(runner, "currentTarget", victim); Set(runner, "targetSearchTimer", 100f);
            previousPosition = runner.Position; health = victim.CurrentHealth;
            Time.timeScale = 1;
            await Wait(() => runner != null && Vector3.Distance(previousPosition, runner.Position) > .25f, 20);
            Check(runner.GetComponent<OrganicEnemyVisual>().Animator.GetInteger("Action") == 1, "live navigation drives Move animation");
            await Wait(() => victim.CurrentHealth < health, 20); Time.timeScale = 0;
            Check(victim.CurrentHealth < health, "live Update approaches and resolves organic attack");
            Clear();
            return "PASS " + passed + " checks\n" + log;
        }
        catch (Exception error) { log += "ERROR " + error + "\n"; File.WriteAllText("work/organic-combat-1114/combat-checks.txt", log); throw; }
        finally { Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; fixtures.Clear(); }
    }
}
