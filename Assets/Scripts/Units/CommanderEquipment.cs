using System;
using System.Collections.Generic;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Units
{
    public enum EquipmentSlot { Weapon, Armor, Trinket }
    public enum TrinketEffect { Move, Gather, Mood, Command }
    public enum WeaponKind { Mandible, AcidSprayer, Shield, Pheromone }
    public enum ArmorKind { Coating, Wings }
    [Serializable]
    public class EquipmentItem
    {
        public string id = Guid.NewGuid().ToString("N");
        public EquipmentSlot slot;
        public int quality;
        public TrinketEffect effect;
        public WeaponKind weapon;
        public ArmorKind armor;
        public bool IsValid => !string.IsNullOrEmpty(id) && Enum.IsDefined(typeof(EquipmentSlot), slot) && quality >= 0 && quality <= 3
            && Enum.IsDefined(typeof(TrinketEffect), effect) && Enum.IsDefined(typeof(WeaponKind), weapon) && Enum.IsDefined(typeof(ArmorKind), armor);
        public string Label => new[] { "조잡", "보통", "정교", "걸작" }[Mathf.Clamp(quality,0,3)] + " "
            + EquipmentRecipes.Name((EquipmentRecipe)(slot == EquipmentSlot.Weapon ? (int)weapon : slot == EquipmentSlot.Armor ? 4 + (int)armor : 6 + (int)effect));
        public static EquipmentItem Random(int quality) => new EquipmentItem { slot = (EquipmentSlot)UnityEngine.Random.Range(0,3), quality = Mathf.Clamp(quality,0,3),
            effect = (TrinketEffect)UnityEngine.Random.Range(0,4), weapon = (WeaponKind)UnityEngine.Random.Range(0,4), armor = (ArmorKind)UnityEngine.Random.Range(0,2) };
    }
    public class EquipmentInventory : MonoBehaviour
    {
        public static EquipmentInventory Instance { get; private set; }
        public List<EquipmentItem> Items = new List<EquipmentItem>();
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public const int Capacity = 30;
        public bool Full => Items.Count >= Capacity;
        public bool Add(EquipmentItem item)
        {
            if (Full || item == null || !item.IsValid || Items.Exists(e => e.id == item.id)) return false;
            Items.Add(item); return true;
        }
        public bool Equip(CommanderAnt commander, EquipmentItem item)
        {
            if (commander == null || item == null || !item.IsValid || !commander.CanChangeEquipment || !Items.Contains(item)) return false;
            var old = commander.PersonalState.equipment.Find(e => e.slot == item.slot);
            if (!commander.CanReplaceEquipment(old, item)) return false;
            Items.Remove(item);
            if (old != null) { commander.PersonalState.equipment.Remove(old); Items.Add(old); }
            commander.PersonalState.equipment.Add(item);
            commander.RefreshEquipment();
            commander.Traits.ChangeLoyalty(new[] { 2,4,6,10 }[item.quality] * (commander.Traits.Has(CommanderTrait.Greedy) ? 2 : 1), "Equipment gift");
            return true;
        }
        public bool Unequip(CommanderAnt commander, EquipmentItem item)
        {
            if (Full || commander == null || !commander.CanChangeEquipment || !commander.PersonalState.equipment.Contains(item) || !commander.CanReplaceEquipment(item, null)) return false;
            commander.PersonalState.equipment.Remove(item); Add(item);
            commander.RefreshEquipment();
            commander.Traits.ChangeLoyalty(commander.Traits.Has(CommanderTrait.Greedy) ? -6 : -3, "Equipment removed"); return true;
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
            switch (effect)
            {
                case TrinketEffect.Move: return .05f * (item.quality + 1);
                case TrinketEffect.Gather: return new[] { .08f, .15f, .22f, .30f }[item.quality];
                case TrinketEffect.Mood: return new[] { 3f, 5f, 7f, 10f }[item.quality];
                default: return new[] { 3f, 5f, 7f, 10f }[item.quality];
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
