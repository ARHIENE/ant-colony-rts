using UnityEngine;

namespace AntColony.Units
{
    public partial class EquipmentItem
    {
        // Initial clothing balance; old armor durability is unchanged.
        public const float ClothingCapacity = 100;
        public bool IsClothing => slot == EquipmentSlot.Armor && armor == ArmorKind.ClothClothes && bossEquipment == BossEquipmentKind.None;
    }
}
