using System;
using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public enum EquipmentSlot { Weapon, Armor, Trinket }
    public enum TrinketEffect { Move, Gather, Mood, Command }
    public enum WeaponKind { Mandible, AcidSprayer, Shield, Pheromone, WoodenSword, ShortBow }
    public enum ArmorKind { Coating, Wings }
    public enum BossEquipmentKind { None, Durable, HeatShield, AreaWeapon }
    [Serializable]
    public partial class EquipmentItem
    {
        public string id = Guid.NewGuid().ToString("N");
        public EquipmentSlot slot;
        public int quality;
        public TrinketEffect effect;
        public WeaponKind weapon;
        public ArmorKind armor;
        public BossEquipmentKind bossEquipment;
        public float durability;
        public bool unique; // 다이아 무라마사 유일작: 효과 단계 +1(장신구 ×1.5), 이름 앞 "유일작"
        public int Power => quality + 1 + (unique ? 1 : 0);
        public bool IsValid => !string.IsNullOrEmpty(id) && Enum.IsDefined(typeof(EquipmentSlot), slot) && quality >= 0 && quality <= 3
            && Enum.IsDefined(typeof(TrinketEffect), effect) && Enum.IsDefined(typeof(WeaponKind), weapon) && Enum.IsDefined(typeof(ArmorKind), armor)
            && Enum.IsDefined(typeof(BossEquipmentKind), bossEquipment) && !float.IsNaN(durability) && !float.IsInfinity(durability)
            && durability >= 0 && durability <= DurableCapacity
            && (bossEquipment == BossEquipmentKind.None || bossEquipment == BossEquipmentKind.AreaWeapon && slot == EquipmentSlot.Weapon
                || (bossEquipment == BossEquipmentKind.Durable || bossEquipment == BossEquipmentKind.HeatShield) && slot == EquipmentSlot.Armor);
        public string Label => (unique ? "유일작 " : "") + new[] { "조잡", "보통", "정교", "걸작" }[Mathf.Clamp(quality,0,3)] + " "
            + (bossEquipment != BossEquipmentKind.None ? BossLabel : slot == EquipmentSlot.Weapon && weapon == WeaponKind.WoodenSword ? "나무칼"
                : slot == EquipmentSlot.Weapon && weapon == WeaponKind.ShortBow ? "단궁"
                : EquipmentRecipes.Name((EquipmentRecipe)(slot == EquipmentSlot.Weapon ? (int)weapon : slot == EquipmentSlot.Armor ? 4 + (int)armor : 6 + (int)effect)));
        public static EquipmentItem Random(int quality) => new EquipmentItem { slot = (EquipmentSlot)UnityEngine.Random.Range(0,3), quality = Mathf.Clamp(quality,0,3),
            effect = (TrinketEffect)UnityEngine.Random.Range(0,4), weapon = (WeaponKind)UnityEngine.Random.Range(0,4), armor = (ArmorKind)UnityEngine.Random.Range(0,2) };
    }
    public class EquipmentInventory : MonoBehaviour
    {
        public static EquipmentInventory Instance { get; private set; }
        public List<EquipmentItem> Items = new List<EquipmentItem>();
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public const int BaseCapacity = 30;
        public static int Capacity => BaseCapacity + AntColony.Buildings.Armory.Count * GameBalance.ArmorySlots; // 무기고 1동마다 +10(2026-10-05)
        public bool Full => Items.Count >= Capacity;
        public bool Add(EquipmentItem item)
        {
            if (Full || item == null || !item.IsValid || Items.Exists(e => e.id == item.id)) return false;
            Items.Add(item); return true;
        }
        // 장착·교체·해제가 막힌 이유(빈 문자열 = 가능). 보관함은 본거지에 있으므로 본거지 밖 장수는 원격으로 바꾸지 못한다(2026-10-15).
        public string ChangeBlock(CommanderAnt commander, EquipmentItem next, EquipmentItem old)
        {
            if (commander == null || commander.IsDead) return "대상 장수 없음";
            if (commander.IsAwayFromHome) return "본거지 밖 — 보관함에 접근할 수 없음";
            if (!commander.CanReceiveOrders) return "명령을 받을 수 없는 상태";
            if (commander.IsConstructing || commander.LabUpgradeBusy) return "건설·연구 중";
            if (next != null && !Items.Contains(next)) return "보관함에 없는 장비";
            if (old != null && !commander.PersonalState.equipment.Contains(old)) return "장착하지 않은 장비";
            if (next == null && old != null && Full) return "보관함이 가득 참";
            if (next?.slot == EquipmentSlot.Armor && next.armor == ArmorKind.Wings && commander.PersonalState.Severity(InjuryPart.Wings) > 0) return "비행 불가 부상";
            if (!commander.CanReplaceEquipment(old, next)) return "비행 중 — 착륙 지점 없음";
            return "";
        }
        public bool Equip(CommanderAnt commander, EquipmentItem item)
        {
            if (commander == null || item == null || !item.IsValid || !Items.Contains(item)) return false;
            var old = commander.PersonalState.equipment.Find(e => e.slot == item.slot);
            if (ChangeBlock(commander, item, old) != "") return false;
            Items.Remove(item);
            if (old != null) { commander.PersonalState.equipment.Remove(old); Items.Add(old); }
            commander.PersonalState.equipment.Add(item);
            commander.RefreshEquipment();
            commander.MoodEvent("장비 선물", new[] { 2,4,6,10 }[item.quality] * (commander.Traits.Has(CommanderTrait.Greedy) ? 2 : 1));
            return true;
        }
        public bool Unequip(CommanderAnt commander, EquipmentItem item)
        {
            if (item == null || ChangeBlock(commander, null, item) != "") return false;
            commander.PersonalState.equipment.Remove(item); Add(item);
            commander.RefreshEquipment();
            commander.MoodEvent("장비 회수", commander.Traits.Has(CommanderTrait.Greedy) ? -6 : -3); return true;
        }
    }

    public partial class CommanderAnt
    {
        public EquipmentItem Weapon => personalState.equipment.Find(e => e.slot == EquipmentSlot.Weapon);
        public EquipmentItem EquippedArmor => personalState.equipment.Find(e => e.slot == EquipmentSlot.Armor);
        public string WeaponLabel => Weapon?.weapon.ToString() ?? "Bare mandibles";
        public bool CanChangeEquipment => CanReceiveOrders && !IsConstructing && !LabUpgradeBusy;
        public float AuraRadius => SupportAuraRadius + (Weapon?.quality ?? 0);
        public CommanderActivity CombatActivity => Role == AntColony.Data.UnitRole.Ranged ? CommanderActivity.Ranged
            : Role == AntColony.Data.UnitRole.Support ? CommanderActivity.Command : CommanderActivity.Melee;
        public float TrinketBonus(TrinketEffect effect)
        {
            var item = personalState.equipment.Find(e => e.slot == EquipmentSlot.Trinket && e.effect == effect);
            if (item == null) return 0;
            var unique = item.unique ? 1.5f : 1f;
            switch (effect)
            {
                case TrinketEffect.Move: return .05f * (item.quality + 1) * unique;
                case TrinketEffect.Gather: return new[] { .08f, .15f, .22f, .30f }[item.quality] * unique;
                case TrinketEffect.Mood: return new[] { 3f, 5f, 7f, 10f }[item.quality] * unique;
                default: return new[] { 3f, 5f, 7f, 10f }[item.quality] * unique;
            }
        }
        public bool CanReplaceEquipment(EquipmentItem old, EquipmentItem next)
        {
            if (next?.slot == EquipmentSlot.Armor && next.armor == ArmorKind.Wings && personalState.Severity(InjuryPart.Wings) > 0) return false;
            if (IsFlying && old?.slot == EquipmentSlot.Armor && (next == null || next.armor != ArmorKind.Wings)
                && !UnityEngine.AI.NavMesh.SamplePosition(Position, out _, LandingSampleRadius, UnityEngine.AI.NavMesh.AllAreas)) return false;
            return true;
        }
        public bool CycleWeapon()
        {
            var inventory = EquipmentInventory.Instance;
            if (inventory == null || !CanChangeEquipment) return false;
            var item = inventory.Items.Find(e => e.slot == EquipmentSlot.Weapon);
            return item != null ? inventory.Equip(this, item) : Weapon != null && inventory.Unequip(this, Weapon);
        }
        public void RefreshEquipment()
        {
            // 화물·피해·스킬 재사용 대기는 보존하며 기존 작업만 멈춘다.
            CommandStop(); ApplyRoleProfile();
            if (!IsEmbarked) ApplyMovementMode();
        }
    }
}
