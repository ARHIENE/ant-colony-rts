using System;
using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Save
{
    internal static class CommanderMigration
    {
        internal static bool Upgrade(SaveFileV1 file, out string error)
        {
            error = "Invalid legacy commander data.";
            if (file.commanders == null || file.equipmentInventory == null) return false;
            foreach (var c in file.commanders)
            {
                if (c == null || !Enum.IsDefined(typeof(UnitRole), c.role) || c.level < 1 || c.level > 20 || c.xp < 0
                    || (c.level == 20 ? c.xp != 0 : c.xp >= c.level * 100)
                    || c.workLevel < 0 || c.workLevel > 5 || float.IsNaN(c.workProgress) || c.workProgress < 0 || c.workProgress >= 100
                    || c.personalState == null || !c.personalState.Validate(out _) || c.traits == null || !Passions(c.traits)) return false;
                c.talents = new CommanderTalents();
                var combat = c.role == (int)UnitRole.Ranged ? CommanderActivity.Ranged
                    : c.role == (int)UnitRole.Support ? CommanderActivity.Command : CommanderActivity.Melee;
                c.talents.levels[(int)combat] = c.level;
                c.talents.experience[(int)combat] = c.xp;
                c.talents.levels[(int)CommanderActivity.Gathering] = c.workLevel * 4;
                c.talents.experience[(int)CommanderActivity.Gathering] = c.workLevel == 5 ? 0 : c.workProgress;
                c.talents.levels[(int)CommanderActivity.Crafting] = c.personalState.craftLevel * 4;
                c.talents.levels[(int)CommanderActivity.Research] = c.personalState.researchLevel * 4;
                c.talents.Add(CommanderActivity.Crafting, c.personalState.craftProgress);
                c.talents.Add(CommanderActivity.Research, c.personalState.researchProgress);
                EquipmentItem item = null;
                if (c.role == (int)UnitRole.Flying) item = new EquipmentItem { slot = EquipmentSlot.Armor, armor = ArmorKind.Wings, quality = 1 };
                else if (c.role != (int)UnitRole.Worker) item = new EquipmentItem { slot = EquipmentSlot.Weapon, quality = 1,
                    weapon = c.role == (int)UnitRole.Ranged ? WeaponKind.AcidSprayer : c.role == (int)UnitRole.Defense ? WeaponKind.Shield
                        : c.role == (int)UnitRole.Support ? WeaponKind.Pheromone : WeaponKind.Mandible };
                // 이관 장비와 슬롯이 겹쳐도 기존 장비를 잃지 않는다.
                if (item != null)
                {
                    var old = c.personalState.equipment.Find(e => e.slot == item.slot);
                    if (old != null) { c.personalState.equipment.Remove(old); file.equipmentInventory.Add(old); }
                    c.personalState.equipment.Add(item);
                }
            }
            if (file.buildings != null) foreach (var b in file.buildings)
                if (b?.prisoners != null) foreach (var p in b.prisoners)
                    if (p != null) { if (!Passions(p.traits)) return false; p.talents = new CommanderTalents(); }
            if (file.monsters != null) foreach (var m in file.monsters)
                if (m?.traits != null) { if (!Passions(m.traits)) return false; m.talents = new CommanderTalents(); }
            file.version = SaveFileV1.CurrentVersion;
            error = null; return true;
        }
        private static bool Passions(TraitsDto traits)
        {
            if (traits?.passions == null) return false;
            var result = new List<CommanderPassion>();
            foreach (var p in traits.passions)
            {
                if (p == null || (int)p.activity < 0 || (int)p.activity > 10 || p.flame < 1 || p.flame > 2) return false;
                var skill = (int)p.activity == 10 ? CommanderActivity.Command : (int)p.activity >= 8 ? CommanderActivity.Melee : p.activity;
                var existing = result.Find(v => v.activity == skill);
                if (existing == null) result.Add(new CommanderPassion { activity = skill, flame = p.flame });
                else existing.flame = Math.Max(existing.flame, p.flame);
            }
            traits.passions = result; return true;
        }
    }
}
