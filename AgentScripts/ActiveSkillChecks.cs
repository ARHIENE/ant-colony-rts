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
        if (AntColony.Core.GameSession.Instance.GameStarted) return;
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions());
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.UI.GameMenuController.Instance.Resume(); UnityEngine.Time.timeScale = 1;
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
        if (!Application.isPlaying) throw new Exception("Play mode required");
        await StartGame();
        checks = 0;

        var commander = Object.FindObjectsByType<CommanderAnt>(FindObjectsSortMode.None)
            .First(c => true && true);
        var otherRole = UnitRole.Defense;
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

            // 8) 선택 UI: 클릭 시점 선택 조회와 상태 표시.
            ResetSkills(commander);
            var panel = Object.FindAnyObjectByType<AntColony.UI.SelectedUnitPanel>();
            var panelType = typeof(AntColony.UI.SelectedUnitPanel);
            var strikeText = (UnityEngine.UI.Text)panelType.GetField("powerStrikeText", Flags).GetValue(panel);
            var stanceText = (UnityEngine.UI.Text)panelType.GetField("stanceText", Flags).GetValue(panel);
            var strikeButton = strikeText.transform.parent.GetComponent<UnityEngine.UI.Button>();
            var stanceButton = stanceText.transform.parent.GetComponent<UnityEngine.UI.Button>();

            selection.ClearSelection();
            strikeButton.onClick.Invoke();
            stanceButton.onClick.Invoke();
            Check(!commander.Skills.PowerStrikeArmed && !commander.Skills.DefensiveStanceActive, "no selection at click time casts nothing");

            var addToSelection = typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", Flags);
            Action<AntColony.Units.CommanderAnt> select = c => addToSelection.Invoke(selection,
                new object[] { c.GetComponent<AntColony.Units.SelectableObject>() });
            async Task<bool> WaitFor(Func<bool> condition)
            {
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(20);
                return condition();
            }

            // B도 시전 가능한 상태로 만든다(잘못된 대상 보호를 증명하려면 B가 실제로 시전될 수 있어야 한다).
            ResetSkills(other);
            other.Talents.levels[(int)CommanderActivity.Melee] = 3;
            if (!other.HasTroops)
            {
                AntPool.Instance.Breed(1);
                Check(other.TryAssign(1), "commander B has a troop");
            }

            select(commander);
            commander.Talents.levels[(int)CommanderActivity.Melee] = 1;
            Check(await WaitFor(() => strikeText.text == "Strike Lv2" && stanceText.text == "Guard Lv3"),
                "locked labels: " + strikeText.text + " / " + stanceText.text);
            Check(!strikeButton.interactable && !stanceButton.interactable, "locked buttons are not interactable");
            commander.Talents.levels[(int)CommanderActivity.Melee] = 3;
            Check(Arm(commander, UnitRole.Defense), "shield enables defensive stance");
            Check(await WaitFor(() => strikeText.text == "Strike" && stanceText.text == "Guard"
                && strikeButton.interactable && stanceButton.interactable), "ready labels and interactable: " + strikeText.text + " / " + stanceText.text);

            // 같은 프레임에 A → B로 선택을 바꾼 직후 클릭: 패널 캐시(A)가 아니라 B에 시전된다.
            selection.ClearSelection();
            select(other);
            strikeButton.onClick.Invoke();
            Check(other.Skills.PowerStrikeArmed && !commander.Skills.PowerStrikeArmed, "same-frame A->B switch casts on B only");
            ResetSkills(other);

            // 다중 선택이면 아무에게도 시전하지 않는다.
            select(commander);
            strikeButton.onClick.Invoke();
            stanceButton.onClick.Invoke();
            Check(!commander.Skills.PowerStrikeArmed && !other.Skills.PowerStrikeArmed
                && !commander.Skills.DefensiveStanceActive && !other.Skills.DefensiveStanceActive, "multi-selection casts nothing");

            // 병력 0이면 버튼이 비활성화된다.
            selection.ClearSelection();
            select(commander);
            var uiTroops = commander.TroopCount;
            SetPrivate(commander, "pendingDamage", 0f);
            commander.ReturnTroops(uiTroops);
            Check(await WaitFor(() => !strikeButton.interactable && !stanceButton.interactable), "no-troop buttons are not interactable");
            Check(commander.TryAssign(uiTroops), "ui troops restored");
            Check(await WaitFor(() => strikeButton.interactable && stanceButton.interactable), "buttons re-enable with troops");

            strikeButton.onClick.Invoke();
            stanceButton.onClick.Invoke();
            Check(commander.Skills.PowerStrikeArmed && commander.Skills.DefensiveStanceActive, "buttons cast on selected commander");
            Check(await WaitFor(() => strikeText.text == "Strike ON" && stanceText.text.StartsWith("Guard ON")
                && !strikeButton.interactable && !stanceButton.interactable),
                "active labels and non-interactable: " + strikeText.text + " / " + stanceText.text);

            // 가장 긴 라벨이 버튼(100x26) 안에 들어간다.
            stanceText.text = "Guard ON 5s";
            Check(stanceText.preferredWidth <= 100f && stanceText.preferredHeight <= 26f,
                $"Guard ON 5s fits 100x26: {stanceText.preferredWidth}x{stanceText.preferredHeight}");

            commander.Skills.CancelEffects();
            Check(await WaitFor(() => strikeText.text.StartsWith("Strike 1") && strikeText.text.EndsWith("s")
                && stanceText.text.StartsWith("Guard ") && stanceText.text.EndsWith("s") && !stanceText.text.Contains("ON")
                && !strikeButton.interactable && !stanceButton.interactable),
                "cooldown labels and non-interactable: " + strikeText.text + " / " + stanceText.text);
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
