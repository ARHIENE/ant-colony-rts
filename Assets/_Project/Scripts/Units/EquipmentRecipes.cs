using System;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public enum EquipmentRecipe { Mandible, AcidSprayer, Shield, Pheromone, Coating, Wings, Anklet, Antenna, Scent, Insignia }

    public static class EquipmentRecipes
    {
        private static readonly string[] Names = { "큰턱 날", "산 분사기", "방패 갑각", "페로몬 신호기", "외골격 코팅", "비행 날개", "가벼운 발목 띠", "예민한 더듬이 고리", "향기 주머니", "지휘 표식" };
        private static readonly Vector3Int[] Costs = {
            new Vector3Int(0,40,5), new Vector3Int(0,30,10), new Vector3Int(0,60,5), new Vector3Int(20,0,10),
            new Vector3Int(0,40,5), new Vector3Int(30,0,15), new Vector3Int(20,20,0), new Vector3Int(20,20,0),
            new Vector3Int(30,0,5), new Vector3Int(0,30,10) };
        public static string Name(EquipmentRecipe recipe) => Names[(int)recipe];
        public static Vector3Int Cost(EquipmentRecipe recipe) => Costs[(int)recipe];
        public static bool Unlocked(EquipmentRecipe recipe) => Enum.IsDefined(typeof(EquipmentRecipe), recipe)
            && ((int)recipe < 4 ? ScienceEffects.CanCraft((WeaponKind)recipe)
                : (int)recipe < 6 ? ScienceEffects.CanCraft((ArmorKind)((int)recipe - 4)) : ScienceEffects.CanCraftTrinkets);
        public static EquipmentItem Create(EquipmentRecipe recipe, int quality) => new EquipmentItem {
            quality = Mathf.Clamp(quality, 0, 3), slot = (int)recipe < 4 ? EquipmentSlot.Weapon : (int)recipe < 6 ? EquipmentSlot.Armor : EquipmentSlot.Trinket,
            weapon = (int)recipe < 4 ? (WeaponKind)recipe : WeaponKind.Mandible,
            armor = recipe == EquipmentRecipe.Wings ? ArmorKind.Wings : ArmorKind.Coating,
            effect = (int)recipe >= 6 ? (TrinketEffect)((int)recipe - 6) : TrinketEffect.Move };
        public static int Quality(int skill, float roll, float upgradeRoll)
        {
            skill = Mathf.Clamp(skill, 0, 20);
            float crude = Mathf.Max(0, 40 - 2 * skill) / 100f, master = .0125f * skill;
            var normal = (1 - crude - master) * .6f;
            var quality = roll < crude ? 0 : roll < crude + normal ? 1 : roll < 1 - master ? 2 : 3;
            return Mathf.Min(3, quality + (upgradeRoll < ScienceEffects.QualityUpgradeChance ? 1 : 0));
        }
    }
}
