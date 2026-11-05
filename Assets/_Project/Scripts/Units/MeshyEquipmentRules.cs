namespace AntColony.Units
{
    // New firearm values are provisional game balance, not specified by the Notion design.
    public static class MeshyEquipmentRules
    {
        public static bool IsFirearm(WeaponKind kind) => kind == WeaponKind.SingleRifle || kind == WeaponKind.BurstCarbine || kind == WeaponKind.Scattergun || kind == WeaponKind.SniperRifle;
        public static bool IsBow(WeaponKind kind) => kind == WeaponKind.ShortBow || kind == WeaponKind.GrandBow;
        public static bool IsRanged(WeaponKind kind) => kind == WeaponKind.AcidSprayer || IsBow(kind) || IsFirearm(kind);
        public static bool HasAttackBonus(WeaponKind kind) => kind == WeaponKind.Mandible || kind == WeaponKind.AcidSprayer || kind == WeaponKind.Sword || kind == WeaponKind.Katana || kind == WeaponKind.GrandBow || IsFirearm(kind);
        public static float Range(WeaponKind kind) => kind == WeaponKind.SniperRifle ? 24 : kind == WeaponKind.SingleRifle ? 16 : kind == WeaponKind.BurstCarbine ? 11 : kind == WeaponKind.Scattergun ? 7 : 0;
        public static float Interval(WeaponKind kind) => kind == WeaponKind.SniperRifle ? 3.4f : kind == WeaponKind.SingleRifle ? 2.4f : kind == WeaponKind.BurstCarbine ? 1.8f : kind == WeaponKind.Scattergun ? 2.2f : 0;
        public static string HeldModel(EquipmentItem item)
        {
            if (item == null) return null;
            if (item.bossEquipment == BossEquipmentKind.AreaWeapon) return "Weapon_BossC_Quakehammer";
            return item.weapon switch
            {
                WeaponKind.Mandible => "Weapon_Spear_Copper", WeaponKind.Shield => "Weapon_Shield_Copper",
                WeaponKind.ShortBow => "Weapon_ShortBow", WeaponKind.WoodenSword => "Weapon_Sword_Wood",
                WeaponKind.AcidSprayer => "Weapon_AcidSprayer", WeaponKind.Pheromone => "Weapon_CommandStaff",
                WeaponKind.SingleRifle => "Weapon_SingleRifle", WeaponKind.BurstCarbine => "Weapon_BurstCarbine",
                WeaponKind.Scattergun => "Weapon_Scattergun", WeaponKind.Katana => "Weapon_Katana",
                WeaponKind.GrandBow => "Weapon_GrandBow", WeaponKind.SniperRifle => "Weapon_SniperRifle",
                WeaponKind.QuakeMaul => "Weapon_BossC_Quakehammer", _ => "Weapon_Sword_Copper"
            };
        }
    }
}
