using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// Fresh Play mode. All saves use a disposable directory, never the player's slots.
public static class WeaponTalentChecks
{
    static int checks;
    static void Check(bool ok, string why) { if (!ok) throw new Exception("FAIL: " + why); checks++; }
    static void Near(float a, float b, string why) => Check(Mathf.Abs(a - b) < .01f, why + $" ({a}/{b})");
    static SaveFileV1 Clone(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(20);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static EquipmentItem Equip(CommanderAnt c, EquipmentSlot slot, WeaponKind weapon = WeaponKind.Mandible, ArmorKind armor = ArmorKind.Coating, TrinketEffect effect = TrinketEffect.Move, int quality = 1)
    {
        var item = new EquipmentItem { slot = slot, weapon = weapon, armor = armor, effect = effect, quality = quality };
        EquipmentInventory.Instance.Add(item);
        Check(EquipmentInventory.Instance.Equip(c, item), "equip " + item.Label); return item;
    }
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WeaponTalent-" + Guid.NewGuid().ToString("N"));
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 240924, commanderDeath = CommanderDeathMode.Gentle }); await Ready();
            var roster = CommanderRoster.Instance;
            foreach (var c in roster.Commanders) { c.CommandStop(); Check(c.Talents.levels.Sum() == 40, "starting skill budget"); }
            var a = roster.Commanders[0]; var b = roster.Commanders[1];
            a.ApplyTraits(new CommanderTraits()); b.ApplyTraits(new CommanderTraits());
            Array.Clear(a.Talents.levels, 0, CommanderTalents.Count); Array.Clear(a.Talents.experience, 0, CommanderTalents.Count);
            Check(a.CommandLimit == 10 && a.Weapon == null && a.Role == UnitRole.Melee, "bare mandibles and base capacity");
            Near(a.Talents.Multiplier(CommanderActivity.Gathering), .6f, "zero skill speed");
            a.GainExperience(CommanderActivity.Gathering, 350.5f);
            Check(a.Talents.Level(CommanderActivity.Gathering) == 2, "multiple skill levels");
            Near(a.Talents.Xp(CommanderActivity.Gathering), 50.5f, "fractional XP carry");
            a.Traits.passions.Add(new CommanderPassion { activity = CommanderActivity.Research, flame = 2 });
            a.Traits.TryAdd(CommanderTrait.Genius);
            a.GainExperience(CommanderActivity.Research, 10); Near(a.Talents.Xp(CommanderActivity.Research), 30, "passion and learning multiply");
            a.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Head, severity = InjurySeverity.Serious, remaining = 240 });
            a.GainExperience(CommanderActivity.Research, 10); Near(a.Talents.Xp(CommanderActivity.Research), 45, "head injury learning penalty");
            a.PersonalState.injuries.Clear(); a.ApplyTraits(new CommanderTraits());
            a.GainExperience(CommanderActivity.Fishing, float.NaN); a.GainExperience(CommanderActivity.Fishing, float.PositiveInfinity);
            Near(a.Talents.Xp(CommanderActivity.Fishing), 0, "invalid XP ignored");
            a.GainExperience(CommanderActivity.Command, float.MaxValue);
            Check(a.Talents.Level(CommanderActivity.Command) == 20 && a.Talents.Xp(CommanderActivity.Command) == 0 && a.CommandLimit == 50, "cap and command formula");

            var enemy = new GameObject("TalentCheckEnemy").AddComponent<WildMonster>();
            enemy.transform.position = a.Position + Vector3.right * 3;
            try
            {
                Check(a.CanAttackTarget(enemy), "unarmed can attack");
                foreach (WeaponKind weapon in Enum.GetValues(typeof(WeaponKind)))
                {
                    var hp = a.CurrentHealth; var troops = a.TroopCount;
                    Equip(a, EquipmentSlot.Weapon, weapon);
                    Check(a.Data.gatherRate > 0 && a.Data.carryCapacity > 0 && a.CanStartConstruction, weapon + " can work");
                    var node = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None).First(n => n.CanGather);
                    a.CommandGather(node); Check(a.CurrentResourceNode == node && a.IsWorking, weapon + " accepts gathering"); a.CommandStop();
                    Near(a.CurrentHealth, hp, "equipment preserves damage"); Check(a.TroopCount == troops, "equipment preserves troops");
                    if (weapon == WeaponKind.AcidSprayer) Near(a.Data.attackRange, 6, "acid range");
                }
                Equip(a, EquipmentSlot.Weapon, WeaponKind.Mandible);
                a.Talents.levels[(int)CommanderActivity.Melee] = 5;
                Check(a.TryPowerStrike(), "melee skill unlock");
                var cooldown = a.Skills.PowerStrikeCooldownLeft;
                Equip(a, EquipmentSlot.Weapon, WeaponKind.Shield);
                Check(!a.CanPowerStrike && a.CanDefensiveStance && a.Skills.PowerStrikeArmed, "weapon changes actions, preserves armed strike");
                Near(a.Skills.PowerStrikeCooldownLeft, cooldown, "cooldown preserved");
                Equip(a, EquipmentSlot.Armor, armor: ArmorKind.Wings);
                Check(a.IsFlying && !a.Agent.enabled && a.Weapon.weapon == WeaponKind.Shield, "wings independent of weapon");
                typeof(WildMonster).GetProperty("IsFlying").SetValue(enemy, true);
                Check(a.CanAttackTarget(enemy), "winged shield can attack air");
                Check(EquipmentInventory.Instance.Unequip(a, a.EquippedArmor), "landing removes wings");
                Check(!a.IsFlying && a.Agent.enabled && !a.CanAttackTarget(enemy), "ground shield cannot attack air");
                Equip(a, EquipmentSlot.Weapon, WeaponKind.AcidSprayer); Check(a.CanAttackTarget(enemy), "ground acid can attack air");
                a.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Wings, severity = InjurySeverity.Minor, remaining = 180 });
                var wings = new EquipmentItem { slot = EquipmentSlot.Armor, armor = ArmorKind.Wings };
                EquipmentInventory.Instance.Add(wings);
                Check(!EquipmentInventory.Instance.Equip(a, wings) && EquipmentInventory.Instance.Items.Contains(wings), "injured wing equip is atomic");
                a.PersonalState.injuries.Clear();
                a.CommandAttack(enemy);
                var combat = a.Talents.Xp(CommanderActivity.Ranged);
                a.TickPersonal(10); Near(a.Talents.Xp(CommanderActivity.Ranged), combat + 2, "combat time XP");
                typeof(WildMonster).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 1f);
                typeof(CommanderAnt).GetMethod("DealDamage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(a, new object[] { enemy });
                Near(a.Talents.Xp(CommanderActivity.Ranged), combat + 27, "last hit XP uses weapon skill");
            }
            finally { if (enemy != null) Object.Destroy(enemy.gameObject); a.CommandStop(); }
            await Task.Delay(50);

            Equip(b, EquipmentSlot.Weapon, WeaponKind.Pheromone, quality: 3);
            b.Talents.levels[(int)CommanderActivity.Command] = 20;
            Near(b.AuraRadius, 9, "masterwork support radius");
            Check(a.HasSupportAura, "support reaches nearby ally");
            var inv = EquipmentInventory.Instance;
            a.Talents.levels[(int)CommanderActivity.Command] = 0;
            var charm = Equip(a, EquipmentSlot.Trinket, effect: TrinketEffect.Command, quality: 3);
            AntPool.Instance.Breed(100); Check(a.TryAssign(a.FreeRanks), "fill capacity with charm");
            int assigned = a.TroopCount;
            Check(inv.Unequip(a, charm) && a.TroopCount == assigned && !a.TryAssign(1), "overcapacity keeps troops and blocks new allocation");
            a.TakeDamage(a.Armor + 1); var health = a.CurrentHealth;
            Equip(a, EquipmentSlot.Weapon, WeaponKind.Shield); Near(a.CurrentHealth, health, "damaged troop state preserved");
            var ship = WorldMapManager.Instance.CreateTransport(false, a.Position);
            a.ReturnTroops(a.TroopCount - 2);
            Check(ship.TryBoard(new[] { a }), "board equipped commander");
            var site = WorldMapManager.Instance.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
            Check(ship.TryDepart(site), "depart"); ship.Tick(ship.TravelSeconds + 1);
            Equip(a, EquipmentSlot.Weapon, WeaponKind.Mandible);
            Check(a.Transport == ship && !a.IsEmbarked, "field equipment change");
            Check(ship.TryReturn(), "return"); ship.Tick(ship.TravelSeconds + 1);
            foreach (var c in roster.Commanders) c.CommandStop();
            GameMenuController.Instance.Details(a); await Task.Delay(30);
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>().Count(t => Enum.GetNames(typeof(CommanderActivity)).Any(n => t.text.StartsWith(n + " "))) >= 9, "nine skills displayed");

            var original = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(original, out var error), "v3 validates: " + error);
            var bad = Clone(original); bad.commanders[0].talents.experience[0] = float.NaN;
            Check(!SaveValidator.Validate(bad, out _), "invalid skills rejected");
            bad = Clone(original); bad.commanders[0].personalState.equipment[0].weapon = (WeaponKind)99;
            Check(!SaveValidator.Validate(bad, out _), "unknown equipment rejected");
            var legacy = Clone(original); legacy.version = 2;
            for (int i = 0; i < legacy.commanders.Count; i++)
            {
                var c = legacy.commanders[i]; c.talents = null; c.level = 5; c.xp = 7; c.workLevel = 2; c.workProgress = 12.5f;
                c.role = i % 6; c.rank = 999; c.allowedRoles = new System.Collections.Generic.List<int> { c.role };
                c.traits.passions.Clear();
            }
            legacy.commanders[3].traits.passions.Add(new CommanderPassion { activity = (CommanderActivity)8, flame = 2 });
            var oldTroops = legacy.commanders.Sum(c => c.troopCount);
            Check(SaveValidator.Validate(legacy, out error), "v2 migrates, ignores ranks: " + error);
            Check(legacy.version == SaveFileV1.CurrentVersion && legacy.commanders.Sum(c => c.troopCount) == oldTroops, "migration preserves troops");
            Check(legacy.commanders[2].talents.Level(CommanderActivity.Ranged) == 5 && legacy.commanders[2].talents.Level(CommanderActivity.Gathering) == 8, "legacy skill mapping");
            Check(legacy.commanders[3].traits.passions[0].activity == CommanderActivity.Melee, "legacy passion mapping");
            Check(legacy.commanders[4].personalState.equipment.Any(e => e.slot == EquipmentSlot.Armor && e.armor == ArmorKind.Wings), "flying role maps to wings");
            var itemCount = legacy.equipmentInventory.Count + legacy.commanders.Sum(c => c.personalState.equipment.Count);
            Check(SaveValidator.Validate(legacy, out error) && itemCount == legacy.equipmentInventory.Count + legacy.commanders.Sum(c => c.personalState.equipment.Count), "migration is idempotent");
            var path = Path.Combine(SaveStorage.Root, "legacy.json"); SaveStorage.WriteAtomic(path, JsonUtility.ToJson(legacy));
            Check(SaveSystem.TryLoad(path, out error), "load migration: " + error); await Ready();
            var loaded = SaveSnapshot.Capture();
            Check(loaded.commanders.Sum(c => c.troopCount) == oldTroops, "loaded troop totals");
            Check(JsonUtility.ToJson(loaded.commanders[2].talents) == JsonUtility.ToJson(legacy.commanders[2].talents), "loaded talents");
            Check(SaveSystem.TrySave(false, 0, out error), "save migrated game: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "second load: " + error); await Ready();
            var again = SaveSnapshot.Capture();
            Check(again.equipmentInventory.Count == loaded.equipmentInventory.Count && again.commanders.Sum(c => c.troopCount) == oldTroops, "second cycle no duplication/loss");
            Check(again.commanders.Select(c => JsonUtility.ToJson(c.talents)).SequenceEqual(loaded.commanders.Select(c => JsonUtility.ToJson(c.talents))), "all nine skills survive second reload");
            return "PASS " + checks + " weapon/talent checks; combat, work, equipment, flight, UI, v2 migration, two reloads";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
