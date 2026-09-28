using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 장수 액티브 스킬(강타·방어 태세) 검사. 재사용 대기·지속 시간 경과는 내부 시각 필드를 과거로 돌려 흉내 낸다.
public static class ActiveSkillChecks
{
    static int checks;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }

    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    static void SetPrivate(object instance, string field, object value)
    {
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
        var go = new GameObject("SkillCheckMonster");
        go.SetActive(false);
        go.transform.position = position;
        var monster = go.AddComponent<WildMonster>();
        SetPrivate(monster, "maxHealth", health);
        SetPrivate(monster, "attackDamage", 0f);
        monster.MakeRaider();
        go.SetActive(true);
        return monster;
    }

    static void ResetSkills(CommanderAnt commander)
    {
        commander.Skills.CancelEffects();
        SetPrivate(commander.Skills, "powerStrikeReadyTime", float.NegativeInfinity);
        SetPrivate(commander.Skills, "stanceReadyTime", float.NegativeInfinity);
    }

    // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
    static async System.Threading.Tasks.Task StartGame()
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(50);
        Check(!AntColony.Save.SaveSystem.Busy, "initial load completes");
        if (!GameSession.Instance.GameStarted)
        {
            AntColony.Save.SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small });
            deadline = DateTime.UtcNow.AddSeconds(90);
            while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(50);
            Check(!AntColony.Save.SaveSystem.Busy, "new game loads");
        }
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
    }
    // 무기=역할 개편: 예전 보직 변경을 해당 무기(날개) 장착으로 대신한다.
    static bool Arm(AntColony.Units.CommanderAnt c, AntColony.Data.UnitRole role)
    {
        var inv = AntColony.Units.EquipmentInventory.Instance;
        var item = role == AntColony.Data.UnitRole.Flying
            ? new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Armor, armor = AntColony.Units.ArmorKind.Wings, quality = 1 }
            : new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Weapon, quality = 1,
                weapon = role == AntColony.Data.UnitRole.Ranged ? AntColony.Units.WeaponKind.AcidSprayer : role == AntColony.Data.UnitRole.Defense ? AntColony.Units.WeaponKind.Shield
                    : role == AntColony.Data.UnitRole.Support ? AntColony.Units.WeaponKind.Pheromone : AntColony.Units.WeaponKind.Mandible };
        if (inv.Full) inv.Items.RemoveAt(0);
        if (!inv.Add(item) || !inv.Equip(c, item)) return false;
        inv.Items.RemoveAll(e => e.slot == item.slot && e.quality == 1 && e != item);
        return role == AntColony.Data.UnitRole.Flying ? c.IsFlying : c.Role == (role == AntColony.Data.UnitRole.Worker ? AntColony.Data.UnitRole.Melee : role);
    }
    public static async Task<string> Main()
    {
        var root = AntColony.Save.SaveStorage.RootOverride;
        var timeScale = Time.timeScale;
        AntColony.Save.SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "ActiveSkill-" + Guid.NewGuid().ToString("N"));
        try { return await Run(); }
        finally { AntColony.Save.SaveStorage.RootOverride = root; Time.timeScale = timeScale; }
    }
    static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        checks = 0;
        await StartGame();

        var commander = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None)
            .First(c => true && true);
        var originalTalents = commander.Talents.Copy();
        var dealDamage = typeof(SoldierAnt).GetMethod("DealDamage", Flags);
        Action<AntColony.Core.IDamageable> attack = target => dealDamage.Invoke(commander, new object[] { target });

        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var upkeepEnabled = upkeep.enabled;
        upkeep.enabled = false;
        var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
            && (m is WildMonster || m is ColonyInvasion)).ToArray();
        foreach (var threat in threats) threat.enabled = false;

        var other = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None).First(c => c != commander);
        var otherTalents = other.Talents.Copy();
        var startRole = commander.Role;
        var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
        GameObject toughObject = null, killObject = null;
        try
        {
            commander.CommandStop();
            commander.WorkState.duty = CommanderDuty.Deployed;
            other.CommandStop();
            other.WorkState.duty = CommanderDuty.Deployed;
            ResetSkills(commander);
            Check(Arm(commander, UnitRole.Melee), "commander takes melee weapon for power strike");
            if (!commander.HasTroops)
            {
                AntPool.Instance.Breed(2);
                Check(commander.TryAssign(1), "commander has at least one troop");
            }

            // 1) 레벨 잠금.
            commander.Talents.levels[(int)CommanderActivity.Melee] = 1;
            Check(!commander.TryPowerStrike() && !commander.TryDefensiveStance(), "level 1 locks both skills");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 2;
            Check(!commander.TryDefensiveStance() && !commander.Skills.DefensiveStanceActive, "level 2 still locks stance");

            // 2) 병력 0 / 비활성 거부.
            var troops = commander.TroopCount;
            SetPrivate(commander, "pendingDamage", 0f); // 부상 1마리 회수 제한 없이 전원 회수하기 위함
            Check(commander.ReturnTroops(troops) == troops && !commander.TryPowerStrike()
                && !commander.Skills.PowerStrikeArmed, "no troops rejects cast");
            Check(commander.TryAssign(troops), "troops restored");
            commander.enabled = false;
            Check(!commander.TryPowerStrike() && !commander.Skills.PowerStrikeArmed, "disabled commander rejects cast");
            commander.enabled = true;
            if (commander.TroopCount < troops) commander.TryAssign(troops - commander.TroopCount);

            // 3) 강타: 장전·중복 거부·다음 타격 한 번만 2배.
            Check(commander.TryPowerStrike() && commander.Skills.PowerStrikeArmed, "level 2 arms power strike");
            Check(!commander.TryPowerStrike(), "duplicate arm rejected");
            Check(commander.Skills.PowerStrikeCooldownLeft > AntColony.Units.CommanderSkills.PowerStrikeCooldown - 1f, "cooldown starts on arm");

            var tough = SpawnRaider(commander.transform.position, 100000f);
            toughObject = tough.gameObject;
            var damage = commander.AttackDamage;
            var hp = tough.CurrentHealth;
            attack(tough);
            var first = hp - tough.CurrentHealth;
            hp = tough.CurrentHealth;
            attack(tough);
            var second = hp - tough.CurrentHealth;
            Check(Mathf.Approximately(first, damage * AntColony.Units.CommanderSkills.PowerStrikeMultiplier), "armed hit deals 2x: " + first);
            Check(Mathf.Approximately(second, damage) && !commander.Skills.PowerStrikeArmed, "next hit is normal and strike consumed");
            Check(!commander.TryPowerStrike(), "cooldown blocks re-arm after use");

            // 4) 강화 타격으로 처치해도 경험치가 지급된다.
            SetPrivate(commander.Skills, "powerStrikeReadyTime", Time.time - 1f);
            Check(commander.TryPowerStrike(), "re-arm after cooldown expires");
            var kill = SpawnRaider(commander.transform.position, damage * 1.5f);
            killObject = kill.gameObject;
            attack(kill);
            Check(kill == null || kill.IsDead,
                "empowered hit defeats target");

            // 5) 방어 태세: +5 방어, 중복 거부, 만료, 재사용 대기 유지.
            commander.Talents.levels[(int)CommanderActivity.Melee] = 3;
            Check(Arm(commander, UnitRole.Defense), "shield enables defensive stance");
            var armor = commander.Armor; // 레벨 3 보너스 포함 기준값
            Check(commander.TryDefensiveStance() && commander.Armor == armor + AntColony.Units.CommanderSkills.DefensiveStanceArmor,
                "stance adds +5 armor");
            Check(!commander.TryDefensiveStance(), "duplicate stance rejected");

            // 6) 보직 변경은 장전·태세·대기를 유지한다.
            SetPrivate(commander.Skills, "powerStrikeReadyTime", Time.time - 1f);
            Check(Arm(commander, UnitRole.Melee) && commander.TryPowerStrike(), "arm strike with melee weapon");
            Check(Arm(commander, UnitRole.Defense) && commander.Skills.PowerStrikeArmed && commander.Skills.DefensiveStanceActive
                && commander.Skills.DefensiveStanceCooldownLeft > 0f, "weapon change keeps skill state");

            SetPrivate(commander.Skills, "stanceEndTime", Time.time - 0.01f);
            Check(!commander.Skills.DefensiveStanceActive && commander.Armor == armor, "stance expires and armor returns");
            Check(!commander.TryDefensiveStance(), "stance cooldown outlasts duration");
            SetPrivate(commander.Skills, "stanceReadyTime", Time.time - 1f);
            Check(commander.TryDefensiveStance(), "stance recast after cooldown");

            // 7) 비활성화는 장전·태세를 끊고 대기는 환급하지 않는다.
            var assigned = commander.TroopCount;
            commander.gameObject.SetActive(false);
            commander.gameObject.SetActive(true);
            Check(!commander.Skills.PowerStrikeArmed && !commander.Skills.DefensiveStanceActive
                && commander.Skills.PowerStrikeCooldownLeft > 0f && commander.Skills.DefensiveStanceCooldownLeft > 0f,
                "disable cancels effects and keeps cooldowns");
            if (commander.TroopCount < assigned) commander.TryAssign(assigned - commander.TroopCount);

            // 8) 현재 Q 버튼: 무기에 따라 강타/방어 태세를 표시하고 클릭 시점 선택을 조회한다.
            ResetSkills(commander);
            ResetSkills(other);
            other.Talents.levels[(int)CommanderActivity.Melee] = 3;
            Check(Arm(other, UnitRole.Melee), "commander B uses melee weapon");
            if (!other.HasTroops)
            {
                AntPool.Instance.Breed(1);
                Check(other.TryAssign(1), "commander B has a troop");
            }
            var card = Object.FindAnyObjectByType<AntColony.UI.CommandCard>();
            var button = card.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Skill");
            var label = button.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t => t.alignment == TextAnchor.LowerCenter);
            var refresh = typeof(AntColony.UI.CommandCard).GetMethod("LateUpdate", Flags);
            Action update = () => refresh.Invoke(card, null);
            var add = typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", Flags);
            Action<CommanderAnt> select = c => add.Invoke(selection, new object[] { c.GetComponent<AntColony.Units.SelectableObject>() });

            selection.ClearSelection(); update(); button.onClick.Invoke();
            Check(!button.gameObject.activeInHierarchy && !commander.Skills.PowerStrikeArmed && !other.Skills.PowerStrikeArmed,
                "no selection hides skill and casts nothing");
            select(commander);
            Check(Arm(commander, UnitRole.Melee), "melee Q selected");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 1; update();
            Check(label.text == "강타\nLv2" && !button.interactable, "strike level lock");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 3; update();
            Check(label.text == "강타" && button.interactable, "strike ready");

            selection.ClearSelection(); select(other); button.onClick.Invoke();
            Check(other.Skills.PowerStrikeArmed && !commander.Skills.PowerStrikeArmed, "same-frame A->B casts on B only");
            ResetSkills(other);
            select(commander); update(); button.onClick.Invoke();
            Check(!button.gameObject.activeInHierarchy && !commander.Skills.PowerStrikeArmed && !other.Skills.PowerStrikeArmed,
                "multi-selection hides skill and casts nothing");

            selection.ClearSelection(); select(commander);
            var uiTroops = commander.TroopCount;
            SetPrivate(commander, "pendingDamage", 0f);
            commander.ReturnTroops(uiTroops); update();
            Check(!button.interactable, "no troops disables skill");
            Check(commander.TryAssign(uiTroops), "ui troops restored");
            update(); Check(button.interactable, "troops re-enable skill");
            button.onClick.Invoke(); update();
            Check(commander.Skills.PowerStrikeArmed && label.text == "강타 ON" && !button.interactable, "Q arms strike");
            commander.Skills.CancelEffects(); update();
            Check(label.text == "강타\n15s" && !button.interactable, "strike cooldown label");

            Check(Arm(commander, UnitRole.Defense), "shield Q selected");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 2; update();
            Check(label.text == "방어 태세\nLv3" && !button.interactable, "stance level lock");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 3; update();
            Check(label.text == "방어 태세" && button.interactable, "stance ready");
            button.onClick.Invoke(); update();
            Check(commander.Skills.DefensiveStanceActive && label.text == "방어 태세\n5s" && !button.interactable, "Q activates stance");
            commander.Skills.CancelEffects(); update();
            Check(label.text == "방어 태세\n20s" && !button.interactable, "stance cooldown label");
            commander.ReturnTroops(commander.TroopCount);
            commander.WorkState.duty = CommanderDuty.Civilian; update();
            Check(!button.interactable, "civilian locks combat skill"); // HUD v2: 평시에는 잠긴 채 보인다.
            selection.ClearSelection();
            return "PASS: " + checks + " active skill gating / cooldown / one-shot damage / stance / role / disable / ui checks";
        }
        finally
        {
            selection.ClearSelection();
            commander.CommandStop();
            if (toughObject != null) Object.Destroy(toughObject);
            if (killObject != null) Object.Destroy(killObject);
            ResetSkills(commander);
            ResetSkills(other);
            other.Talents.levels = otherTalents.levels; other.Talents.experience = otherTalents.experience;
            commander.Talents.levels = originalTalents.levels; commander.Talents.experience = originalTalents.experience;
            Arm(commander, startRole);
            upkeep.enabled = upkeepEnabled;
            foreach (var threat in threats) if (threat != null) threat.enabled = true;
        }
    }
}
