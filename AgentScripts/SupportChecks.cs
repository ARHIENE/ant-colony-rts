using System;
using System.Linq;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SupportChecks
{
    static int checks;

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
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
    public static async System.Threading.Tasks.Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        await StartGame();
        checks = 0;

        var commanders = Object.FindObjectsByType<CommanderAnt>();
        var support = commanders.First(c => true);
        var target = commanders.First(c => c != support && true);
        var supportRole = support.Role;
        var targetRole = target.Role;
        var supportPosition = support.Position;
        var targetPosition = target.Position;
        var supportTroops = support.TroopCount;

        try
        {
            Check(Arm(support, UnitRole.Worker) && Arm(target, UnitRole.Ranged), "roles prepared");
            support.transform.position = target.Position;
            var attack = target.AttackDamage;
            var armor = target.Armor;

            Check(Arm(support, UnitRole.Support), "support role enabled");
            Check(target.HasSupportAura, "nearby ally receives aura");
            Check(target.AttackDamage == attack + CommanderAnt.SupportAttackBonus * target.TroopCount,
                "aura adds attack per troop");
            Check(target.Armor == armor + CommanderAnt.SupportArmorBonus, "aura adds armor");

            support.transform.position = target.Position + Vector3.right * (support.AuraRadius + 1f);
            Check(!target.HasSupportAura && target.AttackDamage == attack && target.Armor == armor,
                "aura ends outside radius");

            support.transform.position = target.Position;
            Check(support.ReturnTroops(support.TroopCount) == supportTroops, "support troops returned");
            Check(!target.HasSupportAura && target.AttackDamage == attack && target.Armor == armor,
                "empty support unit grants no aura");

            return "PASS: " + checks + " support aura range / attack / armor / empty-unit checks";
        }
        finally
        {
            support.transform.position = supportPosition;
            target.transform.position = targetPosition;
            Arm(support, supportRole);
            Arm(target, targetRole);
            if (support.TroopCount < supportTroops) support.TryAssign(supportTroops - support.TroopCount);
        }
    }
}
