using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 장수 전투 경험치/레벨 검사. 보스/건물 처치를 실제로 발생시키므로(전역 bossDefeated 갱신)
// 이 스크립트 이후에는 새 Play 세션에서 다른 검사를 돌리는 것이 안전하다.
public static class CommanderProgressionChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }

    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    static MethodInfo dealDamage;
    static void Attack(CommanderAnt commander, AntColony.Core.IDamageable target)
    {
        dealDamage.Invoke(commander, new object[] { target });
    }

    static void SetPrivate(object instance, string field, object value)
    {
        // private 필드는 상속되지 않으므로 선언 타입을 찾을 때까지 올라간다.
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var info = type.GetField(field, Flags);
            if (info == null) continue;
            info.SetValue(instance, value);
            return;
        }
        throw new Exception("missing field: " + field);
    }

    static WildMonster SpawnRaider(Vector3 position, float health)
    {
        var go = new GameObject("XpCheckMonster");
        go.SetActive(false);
        go.transform.position = position;
        var monster = go.AddComponent<WildMonster>();
        SetPrivate(monster, "maxHealth", health);
        SetPrivate(monster, "attackDamage", 0f); // 검사 중 장수 병력이 줄지 않도록 무해한 개체로 만든다.
        monster.MakeRaider();                    // 처치 시 루프 승리 보고 없이 Destroy된다.
        go.SetActive(true);
        return monster;
    }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        checks = 0;

        // 1) 순수 경험치 계산 (씬 상태와 무관).
        var math = new CommanderProgression();
        Check(math.Level == 1 && math.Xp == 0 && math.XpToNext == 100 && math.AttackBonus == 0 && math.ArmorBonus == 0,
            "starts at level 1 with no xp and no bonus");
        Check(math.AddXp(-25) == 0 && math.AddXp(0) == 0 && math.Xp == 0, "negative and zero grants rejected");
        Check(math.AddXp(25) == 0 && math.Level == 1 && math.Xp == 25, "sub-level grant accumulates");
        Check(math.AddXp(75) == 1 && math.Level == 2 && math.Xp == 0 && math.XpToNext == 200, "exact requirement levels up");
        Check(math.AttackBonus == 1 && math.ArmorBonus == 1, "level 2 grants +1 attack and +1 armor");

        var carry = new CommanderProgression();
        Check(carry.AddXp(250) == 1 && carry.Level == 2 && carry.Xp == 150, "surplus carries into the next level");

        var multi = new CommanderProgression();
        // 100+200+300+400 = 1000 → 5레벨 정확히 도달.
        Check(multi.AddXp(1000) == 4 && multi.Level == 5 && multi.Xp == 0, "one grant can span multiple levels");

        var capped = new CommanderProgression();
        Check(capped.AddXp(int.MaxValue) == CommanderProgression.MaxLevel - 1, "int.MaxValue stops at the cap without overflow");
        Check(capped.Level == CommanderProgression.MaxLevel && capped.Xp == 0 && capped.XpToNext == 0, "capped xp is cleared cleanly");
        Check(capped.AddXp(int.MaxValue) == 0 && capped.Level == CommanderProgression.MaxLevel, "no progress past the cap");

        // 2) 실제 전투 경험치.
        var commander = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None)
            .First(c => c.AllowedRoles.Contains(UnitRole.Ranged));
        var other = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None).First(c => c != commander);
        var progression = commander.Progression;
        var levelField = typeof(CommanderProgression).GetField("level", Flags);
        var xpField = typeof(CommanderProgression).GetField("xp", Flags);
        var originalLevel = levelField.GetValue(progression);
        var originalXp = xpField.GetValue(progression);
        dealDamage = typeof(SoldierAnt).GetMethod("DealDamage", Flags);

        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var upkeepEnabled = upkeep.enabled;
        upkeep.enabled = false;
        var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
            && (m is WildMonster || m is ColonyInvasion)).ToArray();
        foreach (var threat in threats) threat.enabled = false;

        var gameManager = GameManager.Instance;
        var bossField = typeof(GameManager).GetField("bossDefeated", Flags);
        var bossDefeated = bossField.GetValue(gameManager);

        var startRole = commander.Role;
        var startRank = commander.Rank;
        GameObject monsterObject = null, targetMonsterObject = null, buildingObject = null, bossObject = null;
        try
        {
            levelField.SetValue(progression, 1);
            xpField.SetValue(progression, 0);
            commander.CommandStop();
            Check(commander.TrySetRole(UnitRole.Ranged), "commander takes a combat role");
            if (!commander.HasTroops)
            {
                AntPool.Instance.Breed(2);
                Check(commander.TryAssign(1), "commander has at least one troop");
            }

            // 비살상 타격은 경험치를 주지 않는다.
            var tough = SpawnRaider(commander.transform.position, 100000f);
            monsterObject = tough.gameObject;
            Attack(commander, tough);
            Check(tough.CurrentHealth < 100000f && progression.Xp == 0 && progression.Level == 1, "nonlethal hit grants no xp");

            // 아군(다른 장수)에게 가한 피해도 경험치 대상이 아니다. 검사로 잃은 병력은 바로 복구한다.
            var friendlyTroops = other.TroopCount;
            Attack(commander, other);
            Check(progression.Xp == 0 && CommanderProgression.KillXp(other) == 0, "friendly damage grants no xp");
            var lost = friendlyTroops - other.TroopCount;
            if (lost > 0)
            {
                AntPool.Instance.Breed(lost);
                other.TryAssign(lost);
            }

            // 다른 주체가 죽인 대상은 경험치가 없고, 이미 죽은 대상 재타격도 중복 지급되지 않는다.
            tough.TakeDamage(100000f);
            Check(tough == null || tough.IsDead, "external kill removes the target");
            if (tough != null) Attack(commander, tough);
            Check(progression.Xp == 0, "external kill and repeat hits on a dead target grant no xp");

            // 실제 전투 루프(TickCombat)로 마지막 일격을 넣어 처치한다.
            var target = SpawnRaider(commander.transform.position, 1f);
            targetMonsterObject = target.gameObject;
            var troops = commander.TroopCount;
            var health = commander.CurrentHealth;
            var limit = commander.CommandLimit;
            commander.CommandAttack(target);
            SetPrivate(commander, "attackTimer", 0f);
            typeof(SoldierAnt).GetMethod("TickCombat", Flags).Invoke(commander, null);
            typeof(SoldierAnt).GetMethod("TickCombat", Flags).Invoke(commander, null);
            Check(progression.Xp == CommanderProgression.MonsterKillXp && progression.Level == 1, "monster last hit grants 25 xp");
            Check(commander.TroopCount == troops && commander.CurrentHealth == health && commander.CommandLimit == limit,
                "xp gain never changes troops, health or command limit");
            commander.CommandStop();

            // 적 건물 처치 = 50.
            buildingObject = new GameObject("XpCheckBuilding");
            buildingObject.SetActive(false);
            var enemyBuilding = buildingObject.AddComponent<BuildingBase>();
            SetPrivate(enemyBuilding, "countsTowardPlayerDefeat", false);
            SetPrivate(enemyBuilding, "fallbackMaxHealth", 1f);
            buildingObject.SetActive(true);
            Attack(commander, enemyBuilding);
            Check(progression.Xp == CommanderProgression.MonsterKillXp + CommanderProgression.BuildingKillXp,
                "enemy building kill grants 50 xp");

            // 보스 처치 = 100. 누적 175 → 2레벨 + 75 이월.
            var attackBefore = commander.AttackDamage;
            var armorBefore = commander.Armor;
            bossObject = new GameObject("XpCheckBoss");
            bossObject.SetActive(false);
            var boss = bossObject.AddComponent<AntColony.Boss.BossHealth>();
            SetPrivate(boss, "maxHp", 1f);
            bossObject.SetActive(true);
            Attack(commander, boss);
            Check(progression.Level == 2 && progression.Xp == 75 && progression.XpToNext == 200,
                "boss kill grants 100 xp and carries the surplus into level 2");
            Check(commander.AttackDamage == attackBefore + commander.TroopCount && commander.Armor == armorBefore + 1f,
                "level bonus adds +1 attack per troop and +1 armor");
            Check(commander.TroopCount == troops && commander.CurrentHealth == health, "level up never heals or creates troops");

            // 3) 보직/관직/배정/비활성화가 경험치를 건드리지 않는다.
            Check(commander.TrySetRole(UnitRole.Worker) && progression.Level == 2 && progression.Xp == 75, "role change keeps xp");
            Check(commander.TrySetRank(CommanderRank.General) && progression.Level == 2 && progression.Xp == 75, "rank change keeps xp");
            commander.TryAssign(1);
            commander.ReturnTroops(commander.TroopCount);
            Check(progression.Level == 2 && progression.Xp == 75, "assignment changes and zero troops keep xp");
            commander.gameObject.SetActive(false);
            commander.gameObject.SetActive(true);
            Check(commander.Progression.Level == 2 && commander.Progression.Xp == 75, "disable and re-enable keep xp");

            // 4) 선택 UI에 레벨/경험치가 표시된다.
            Check(commander.TrySetRole(UnitRole.Ranged), "back to a combat role for the ui check");
            AntPool.Instance.Breed(1);
            commander.TryAssign(1);
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            selection.ClearSelection();
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", Flags)
                .Invoke(selection, new object[] { commander.GetComponent<AntColony.Units.SelectableObject>() });
            await Task.Delay(100);
            var panel = Object.FindAnyObjectByType<AntColony.UI.SelectedUnitPanel>();
            var statsText = (UnityEngine.UI.Text)typeof(AntColony.UI.SelectedUnitPanel)
                .GetField("combatStatsText", Flags).GetValue(panel);
            Check(statsText.text.Contains("Lv 2") && statsText.text.Contains("75/200"), "selected commander panel shows level and xp");
            selection.ClearSelection();

            return "PASS: " + checks + " commander xp math / last-hit kill / retention / bonus / ui checks";
        }
        finally
        {
            commander.CommandStop();
            if (monsterObject != null) Object.Destroy(monsterObject);
            if (targetMonsterObject != null) Object.Destroy(targetMonsterObject);
            if (buildingObject != null) Object.Destroy(buildingObject);
            if (bossObject != null) Object.Destroy(bossObject);
            levelField.SetValue(progression, originalLevel);
            xpField.SetValue(progression, originalXp);
            commander.TrySetRank(startRank);
            commander.TrySetRole(startRole);
            bossField.SetValue(gameManager, bossDefeated);
            upkeep.enabled = upkeepEnabled;
            foreach (var threat in threats) if (threat != null) threat.enabled = true;
        }
    }
}
