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

    public static string Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        checks = 0;

        var commanders = Object.FindObjectsByType<CommanderAnt>();
        var support = commanders.First(c => c.AllowedRoles.Contains(UnitRole.Support));
        var target = commanders.First(c => c != support && c.AllowedRoles.Contains(UnitRole.Ranged));
        var supportRole = support.Role;
        var targetRole = target.Role;
        var supportPosition = support.Position;
        var targetPosition = target.Position;
        var supportTroops = support.TroopCount;

        try
        {
            Check(support.TrySetRole(UnitRole.Worker) && target.TrySetRole(UnitRole.Ranged), "roles prepared");
            support.transform.position = target.Position;
            var attack = target.AttackDamage;
            var armor = target.Armor;

            Check(support.TrySetRole(UnitRole.Support), "support role enabled");
            Check(target.HasSupportAura, "nearby ally receives aura");
            Check(target.AttackDamage == attack + CommanderAnt.SupportAttackBonus * target.TroopCount,
                "aura adds attack per troop");
            Check(target.Armor == armor + CommanderAnt.SupportArmorBonus, "aura adds armor");

            support.transform.position = target.Position + Vector3.right * (CommanderAnt.SupportAuraRadius + 1f);
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
            support.TrySetRole(supportRole);
            target.TrySetRole(targetRole);
            if (support.TroopCount < supportTroops) support.TryAssign(supportTroops - support.TroopCount);
        }
    }
}
