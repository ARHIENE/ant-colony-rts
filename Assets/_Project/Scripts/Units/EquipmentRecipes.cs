using System;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public enum EquipmentRecipe { Mandible, AcidSprayer, Shield, Pheromone, Coating, Wings, Anklet, Antenna, Scent, Insignia, Sword, ShortBow, SingleRifle, BurstCarbine, Scattergun, Katana, GrandBow, SniperRifle }

    public static class EquipmentRecipes
    {
        private static readonly string[] Names = { "창", "산 분사기", "방패", "지원 신호기", "보호 갑옷", "비행 장비", "가벼운 발목 띠", "감각 보조 장신구", "향기 주머니", "구 지휘 표식(효과 없음)", "잎날검", "단궁", "사냥 소총", "연발 카빈", "산탄총", "월엽 카타나", "숲의 대궁", "별지기 저격총" };
        private static readonly Vector3Int[] Costs = {
            new Vector3Int(0,40,5), new Vector3Int(0,30,10), new Vector3Int(0,60,5), new Vector3Int(20,0,10),
            new Vector3Int(0,40,5), new Vector3Int(30,0,15), new Vector3Int(20,20,0), new Vector3Int(20,20,0),
            new Vector3Int(30,0,5), new Vector3Int(0,30,10),
            new Vector3Int(0,45,5), new Vector3Int(0,25,5), new Vector3Int(0,55,15), new Vector3Int(0,65,20), new Vector3Int(0,60,15),
            new Vector3Int(0,60,10), new Vector3Int(0,45,10), new Vector3Int(0,80,25) };
        public static string Name(EquipmentRecipe recipe) => Names[(int)recipe];
        // 제작 분류별 보조 능력(2026-10-07 기획 표, 품목 분류는 잠정): 중량 단조(큰턱 날·방패·코팅) = 근력, 정밀 기계(분사기·신호기·날개) = 연구, 장신구 = 예술.
        public static CommanderActivity Topic(EquipmentRecipe recipe) => recipe switch
        {
            EquipmentRecipe.Mandible or EquipmentRecipe.Shield or EquipmentRecipe.Coating or EquipmentRecipe.Sword or EquipmentRecipe.Katana => CommanderActivity.Strength,
            EquipmentRecipe.AcidSprayer or EquipmentRecipe.Pheromone or EquipmentRecipe.Wings or EquipmentRecipe.SingleRifle or EquipmentRecipe.BurstCarbine or EquipmentRecipe.Scattergun or EquipmentRecipe.SniperRifle => CommanderActivity.Research,
            _ => CommanderActivity.Art
        };
        public static Vector3Int Cost(EquipmentRecipe recipe) => Costs[(int)recipe];
        public static bool Unlocked(EquipmentRecipe recipe) => recipe != EquipmentRecipe.Insignia && Enum.IsDefined(typeof(EquipmentRecipe), recipe)
            && ((int)recipe >= 10 ? ScienceEffects.CanCraft(WeaponFor(recipe)) : (int)recipe < 4 ? ScienceEffects.CanCraft((WeaponKind)recipe)
                : (int)recipe < 6 ? ScienceEffects.CanCraft((ArmorKind)((int)recipe - 4)) : ScienceEffects.CanCraftTrinkets);
        public static EquipmentItem Create(EquipmentRecipe recipe, int quality) => new EquipmentItem {
            quality = Mathf.Clamp(quality, 0, 3), slot = (int)recipe >= 10 || (int)recipe < 4 ? EquipmentSlot.Weapon : (int)recipe < 6 ? EquipmentSlot.Armor : EquipmentSlot.Trinket,
            weapon = WeaponFor(recipe),
            armor = recipe == EquipmentRecipe.Wings ? ArmorKind.Wings : ArmorKind.Coating,
            effect = (int)recipe >= 6 && (int)recipe < 10 ? (TrinketEffect)((int)recipe - 6) : TrinketEffect.Move };
        public static WeaponKind WeaponFor(EquipmentRecipe recipe) => recipe switch
        {
            EquipmentRecipe.Sword => WeaponKind.Sword, EquipmentRecipe.ShortBow => WeaponKind.ShortBow,
            EquipmentRecipe.SingleRifle => WeaponKind.SingleRifle, EquipmentRecipe.BurstCarbine => WeaponKind.BurstCarbine,
            EquipmentRecipe.Scattergun => WeaponKind.Scattergun,
            EquipmentRecipe.Katana => WeaponKind.Katana, EquipmentRecipe.GrandBow => WeaponKind.GrandBow, EquipmentRecipe.SniperRifle => WeaponKind.SniperRifle,
            _ => (int)recipe < 4 ? (WeaponKind)recipe : WeaponKind.Mandible
        };
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
