using System.Linq;
using AntColony.Boss;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Units
{
    public partial class EquipmentItem
    {
        // G22가 종류만 확정했으므로 효과 수치는 잠정 밸런스다.
        public const float DurableCapacity = 200, HeatReduction = .5f, AreaRadius = 3;
        public float HeatResistance => bossEquipment == BossEquipmentKind.HeatShield ? HeatReduction : 0;
        public float AttackRadius => bossEquipment == BossEquipmentKind.AreaWeapon ? AreaRadius : 0;
        public string BossLabel => bossEquipment switch {
            BossEquipmentKind.Durable => "A 고유 내구 갑옷 (내구 " + Mathf.CeilToInt(durability) + "/" + DurableCapacity + ")",
            BossEquipmentKind.HeatShield => "B 방열 갑옷", BossEquipmentKind.AreaWeapon => "C 범위 분사기", _ => "" };
        public static EquipmentItem BossReward(BossId boss, string parcelId) => boss == BossId.D ? null : new EquipmentItem {
            id = "boss-equipment:" + parcelId, quality = 3,
            slot = boss == BossId.C ? EquipmentSlot.Weapon : EquipmentSlot.Armor,
            weapon = boss == BossId.C ? WeaponKind.AcidSprayer : WeaponKind.Mandible,
            bossEquipment = boss == BossId.A ? BossEquipmentKind.Durable : boss == BossId.B ? BossEquipmentKind.HeatShield : BossEquipmentKind.AreaWeapon,
            durability = boss == BossId.A ? DurableCapacity : 0 };
    }

    public partial class CommanderAnt
    {
        public float HeatResistance => EquippedArmor?.HeatResistance ?? 0;
        public void TakeHeatDamage(float amount) => TakeDamage(amount * (1 - HeatResistance));
        private float AbsorbDurableDamage(float amount)
        {
            var armor = EquippedArmor;
            if (armor?.bossEquipment != BossEquipmentKind.Durable) return amount;
            var absorbed = Mathf.Min(armor.durability, amount * .5f);
            armor.durability -= absorbed;
            return amount - absorbed;
        }
        private void StrikeWithEquipment(IDamageable target, float damage)
        {
            var radius = Weapon?.AttackRadius ?? 0;
            if (radius <= 0) { target.TakeDamage(damage); return; }
            // 기존 범위 스킬의 아군/중립/대공 판정과 처치 경험치를 그대로 쓴다.
            DamageArea(target.Position, radius, damage, Role);
        }
        public static bool DeliverBossEquipment(string parcelId, Vector3 home)
        {
            var research = CampaignResearch.Instance;
            var parcel = research?.BossProgress.Cargo.FirstOrDefault(p => p.id == parcelId);
            if (parcel == null || !research.DeliverBossReward(parcelId)) return false;
            var item = EquipmentItem.BossReward((BossId)parcel.boss, parcel.id);
            if (item != null && EquipmentInventory.Instance?.Add(item) != true) EquipmentLoot.Drop(home, new[] { item });
            return true;
        }
    }
}
