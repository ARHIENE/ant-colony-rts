using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    // 기존 소굴의 실제 건물을 산업 슬롯으로 사용한다. 파괴된 슬롯을 다른 건물이 승계하지 않는다.
    public sealed class AIIndustry : MonoBehaviour
    {
        public int Slot { get; internal set; }
        public int Era { get; internal set; } = 1;
        public string Label => FacilityName(Era, Slot);
        public static string FacilityName(int era, int slot)
        {
            var prefix = new[] { "소굴", "농경", "증기", "석유", "전기", "미래" }[Math.Clamp(era, 1, 6) - 1];
            return prefix + " " + new[] { "식량·자재 본부", "채굴·정련소", "병기 공방", "연구소", "방어 지휘소" }[Math.Clamp(slot, 0, 4)];
        }
        public static void Refresh(EnemyColony colony, int era)
        {
            for (int i = 0; i < colony.Buildings.Length; i++)
            {
                var b = colony.Buildings[i]; if (b == null || b.IsDead) continue;
                var facility = b.GetComponent<AIIndustry>() ?? b.gameObject.AddComponent<AIIndustry>();
                facility.Slot = Math.Min(i, 4); facility.Era = era;
            }
        }
        public static bool Alive(EnemyColony colony, int slot) => colony != null && slot < colony.Buildings.Length
            && colony.Buildings[slot] != null && !colony.Buildings[slot].IsDead;
        public static bool CanResearch(EnemyColony colony, int era) => Alive(colony, era <= 2 ? 0 : 3);
        public static (int food, int soil) Income(EnemyColony colony, int era) =>
            ((Alive(colony, 0) ? 6 : 0) * era, ((Alive(colony, 0) ? 1 : 0) + (Alive(colony, 1) ? 3 : 0)) * era);
        public const int StockLimit = 12;
        public static void Produce(EnemyColony colony, Civilization faction)
        {
            Refresh(colony, faction.era);
            var income = Income(colony, faction.era);
            colony.AddResources(income.food, income.soil);
            // 현장 전리품과 외교 재고는 기존처럼 별도 생산분이다.
            faction.resources[0] = (int)Math.Min(int.MaxValue, (long)faction.resources[0] + income.food / 2);
            faction.resources[1] = (int)Math.Min(int.MaxValue, (long)faction.resources[1] + income.soil);
            if (!Alive(colony, 2)) return;
            var weapons = Enumerable.Range(0, 4).Select(i => AIEraMilitary.Weapon(faction.era, i)).Distinct().ToArray();
            var weapon = weapons.OrderBy(w => faction.equipment.Count(e => e.slot == EquipmentSlot.Weapon && e.weapon == w)).First();
            // 구식 무기로 보관함이 가득 차도 다음 시대 장비 생산이 막히지 않게 실물로 내놓는다.
            var surplus = faction.equipment.FirstOrDefault(e => e.slot == EquipmentSlot.Weapon && !e.unique
                && e.bossEquipment == BossEquipmentKind.None && (!weapons.Contains(e.weapon)
                    || faction.equipment.Count(other => other.slot == EquipmentSlot.Weapon && other.weapon == e.weapon) > 4));
            if (faction.equipment.Count >= StockLimit && (surplus == null
                || faction.equipment.Count(e => e.slot == EquipmentSlot.Weapon && e.weapon == weapon) >= 4)) return;
            if (!colony.TrySpendResources(4 * faction.era, 2 * faction.era)) return;
            if (faction.equipment.Count >= StockLimit)
            { faction.equipment.Remove(surplus); EquipmentLoot.Drop(colony.transform.position, new[] { surplus }); }
            faction.equipment.Add(new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = weapon,
                quality = Math.Min(3, (faction.era - 1) / 2 + (faction.era == 6 && Alive(colony, 3) ? 1 : 0)) });
        }
        internal static void Resupply(ExpeditionSite site, Civilization faction)
        {
            if (site.Colony == null || site.Disposition == ConquestDisposition.Annexed) return;
            int member = 0;
            foreach (var guard in site.GetComponentsInChildren<WildMonster>(true).Where(g => !g.IsDead && !g.Allied
                && g.GuardBoss < 0 && (string.IsNullOrEmpty(g.DiplomaticFactionId) || g.DiplomaticFactionId == faction.id)))
            {
                int index = guard.MilitaryEra > 0 ? guard.MilitaryMember : member; member++;
                if (guard.MilitaryEra >= faction.era) continue;
                var weapon = AIEraMilitary.Weapon(faction.era, index);
                var item = faction.equipment.FirstOrDefault(e => e.slot == EquipmentSlot.Weapon && e.weapon == weapon
                    && e.bossEquipment == BossEquipmentKind.None && !e.unique);
                if (item == null) continue;
                faction.equipment.Remove(item);
                // 교체품은 회수한다. 품질·ID를 보존하며 재고 상한은 생산 시 적용한다.
                if (guard.MilitaryEquipment != null) faction.equipment.Add(guard.MilitaryEquipment);
                guard.ConfigureAIMilitary(faction.era, index); guard.MilitaryEquipment = item;
            }
        }
        public static string Summary(EnemyColony colony, int era) => colony == null ? "소굴 없음" :
            string.Join(" / ", Enumerable.Range(0, Math.Min(5, colony.Buildings.Length))
                .Select(i => FacilityName(era, i) + (Alive(colony, i) ? " 가동" : " 파괴")));
    }
}
