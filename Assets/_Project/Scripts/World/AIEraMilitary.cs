using System;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.World
{
    // 잠정 편성: 기존 장비 종류만 사용한다. 전쟁/출정 여부와 병력 수는 변경하지 않는다.
    public static class AIEraMilitary
    {
        private static readonly WeaponKind[][] Compositions = {
            new[] { WeaponKind.WoodenSword },
            new[] { WeaponKind.WoodenSword, WeaponKind.WoodenSword, WeaponKind.ShortBow },
            new[] { WeaponKind.Mandible, WeaponKind.Shield, WeaponKind.ShortBow },
            new[] { WeaponKind.Mandible, WeaponKind.Shield, WeaponKind.AcidSprayer },
            new[] { WeaponKind.Shield, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer },
            new[] { WeaponKind.Shield, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer, WeaponKind.AcidSprayer }
        };
        public static WeaponKind Weapon(int era, int member)
        {
            var composition = Compositions[Math.Clamp(era, 1, 6) - 1];
            return composition[Math.Max(0, member) % composition.Length];
        }
        public static UnitRole Role(WeaponKind weapon) => weapon == WeaponKind.ShortBow || weapon == WeaponKind.AcidSprayer
            ? UnitRole.Ranged : weapon == WeaponKind.Shield ? UnitRole.Defense : UnitRole.Melee;
        public static float Range(WeaponKind weapon) => weapon == WeaponKind.ShortBow ? 12 : weapon == WeaponKind.AcidSprayer ? 6 : 1.5f;
        public static float Interval(WeaponKind weapon) => weapon == WeaponKind.ShortBow ? 2 : weapon == WeaponKind.Shield ? 1.5f : 1.25f;
        public static float Health(WeaponKind weapon) => weapon == WeaponKind.Shield ? 180 : 150;
        public static int DefenseGrade(int era) => Math.Clamp(era, 1, 6);
        public static int DefenseArmor(int era) => 2 * (DefenseGrade(era) - 1);
        public static string Summary(int era) => Math.Clamp(era, 1, 6) switch {
            1 => "나무칼 근접",
            2 => "나무칼 2 : 단궁 1",
            3 => "근접 무기 1 : 방패 1 : 단궁 1",
            4 => "근접 무기 1 : 방패 1 : 산 분사기 1",
            5 => "방패 1 : 산 분사기 2",
            _ => "방패 1 : 산 분사기 3"
        };
        public static Civilization Faction(BuildingBase building)
        {
            var site = building.GetComponentInParent<ExpeditionSite>();
            return site == null || site.Disposition == ConquestDisposition.Annexed ? null : DiplomacyManager.Instance?.Faction(site);
        }
        public static float FortificationArmor(BuildingBase building) => building.GetComponentInParent<EnemyColony>() == null
            ? 0 : DefenseArmor(Faction(building)?.era ?? 1);
        public static void UpgradeGuards(ExpeditionSite site, Civilization faction)
        {
            if (site.Disposition == ConquestDisposition.Annexed) return;
            int member = 0;
            foreach (var guard in site.GetComponentsInChildren<WildMonster>(true))
                if (!guard.IsDead && !guard.Allied && guard.GuardBoss < 0
                    && (string.IsNullOrEmpty(guard.DiplomaticFactionId) || guard.DiplomaticFactionId == faction.id))
                    guard.ConfigureAIMilitary(faction.era, member++);
        }
    }
}
