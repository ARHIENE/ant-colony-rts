using System;
using UnityEngine;

namespace AntColony.Units
{
    // Proposed effect pool: Notion defines the selection rules, but leaves effects and values open.
    public enum UniqueEquipmentEffect { None, Keen, Light, Reinforced, Resonant }
    public partial class EquipmentItem
    {
        public bool UniqueEffectValid => Enum.IsDefined(typeof(UniqueEquipmentEffect), uniqueEffect)
            && (uniqueEffect == UniqueEquipmentEffect.None || unique && (uniqueEffect == UniqueEquipmentEffect.Light
                || uniqueEffect == UniqueEquipmentEffect.Keen && slot == EquipmentSlot.Weapon && MeshyEquipmentRules.HasAttackBonus(weapon)
                || uniqueEffect == UniqueEquipmentEffect.Reinforced && slot == EquipmentSlot.Armor
                || uniqueEffect == UniqueEquipmentEffect.Resonant && slot == EquipmentSlot.Trinket));
        public string UniqueEffectLabel => uniqueEffect switch
        {
            UniqueEquipmentEffect.Keen => "[예리함] ", UniqueEquipmentEffect.Light => "[경량] ",
            UniqueEquipmentEffect.Reinforced => "[견고함] ", UniqueEquipmentEffect.Resonant => "[공명] ", _ => ""
        };
        public float UniqueMoveBonus => unique && uniqueEffect == UniqueEquipmentEffect.Light ? .08f : 0;
        public void MakeUnique(float roll)
        {
            if (quality != 3) return;
            unique = true;
            uniqueEffect = Mathf.Clamp01(roll) < .5f || slot == EquipmentSlot.Weapon && !MeshyEquipmentRules.HasAttackBonus(weapon) ? UniqueEquipmentEffect.Light
                : slot == EquipmentSlot.Weapon ? UniqueEquipmentEffect.Keen
                : slot == EquipmentSlot.Armor ? UniqueEquipmentEffect.Reinforced : UniqueEquipmentEffect.Resonant;
        }
    }
}
