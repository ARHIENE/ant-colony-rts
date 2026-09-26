using System;
using UnityEngine;

namespace AntColony.Save
{
    // 파괴적인 씬 전환을 하기 전에 저장 파일을 먼저 검증한다.
    // 여기서 false가 나오면 현재 게임은 건드리지 않고 오류만 알린다.
    public static class SaveValidator
    {
        public static bool TryParse(string json, out SaveFileV1 file, out string error)
        {
            file = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json)) { error = "Empty file."; return false; }
            SaveFileV1 parsed;
            try
            {
                parsed = JsonUtility.FromJson<SaveFileV1>(json);
            }
            catch (Exception e)
            {
                error = "Not valid JSON (" + e.GetType().Name + ").";
                return false;
            }
            if (parsed == null) { error = "Not valid JSON."; return false; }
            if (!Validate(parsed, out error)) return false;
            file = parsed;
            return true;
        }

        public static bool Validate(SaveFileV1 file, out string error)
        {
            error = null;
            if (file == null) { error = "No data."; return false; }
            if (file.gameId != "AntColony") { error = "Not an Ant Colony save."; return false; }
            if (file.version <= 5 && file.commanders != null)
                foreach (var c in file.commanders) if (c?.personalState != null) c.personalState.social = new Units.CommanderSocialState();
            if (file.version == 1)
            {
                // Version 1 tracked scaled play time; preserve it as the simulation calendar.
                file.gameSeconds = file.playSeconds;
                if (file.options != null) file.options.commanderDeath = (int)Core.CommanderDeathMode.Normal;
                file.equipmentInventory = new System.Collections.Generic.List<Units.EquipmentItem>();
                file.campaign = new Core.CampaignResearch.State();
                if (file.world != null)
                {
                    if (file.world.transports != null) foreach (var transport in file.world.transports)
                        if (transport != null) transport.equipmentCargo = new System.Collections.Generic.List<Units.EquipmentItem>();
                    if (file.world.vehicleResearched) file.campaign.completed.Add((int)Core.ScienceTechnology.Vehicle);
                    if (file.world.aircraftResearched) { file.campaign.completed.Add((int)Core.ScienceTechnology.Gliding); file.campaign.completed.Add((int)Core.ScienceTechnology.Aircraft); }
                }
                if (file.commanders != null) foreach (var c in file.commanders)
                    if (c != null) { c.personalState = new Units.CommanderPersonalState(); MigrateTraits(c.traits); }
                if (file.buildings != null) foreach (var b in file.buildings)
                    if (b != null) { b.scienceTier = 1; b.scientist = -1;
                        if (b.prisoners != null) foreach (var p in b.prisoners) if (p != null) MigrateTraits(p.traits); }
                if (file.monsters != null) foreach (var m in file.monsters) if (m != null) MigrateTraits(m.traits);
                file.version = 2;
            }
            if (file.version == 2 && !CommanderMigration.Upgrade(file, out error)) return false;
            if (file.version == 3 && !WorkshopMigration.Upgrade(file, out error)) return false;
            if (file.version == 4)
            {
                file.history = new Core.CampaignHistory.State();
                file.events = new World.ColonyEvents.State();
                if (file.commanders != null) foreach (var c in file.commanders)
                    if (c?.personalState != null) { c.personalState.infected = false; c.personalState.moldLoss = c.personalState.moldSpread = c.personalState.moldTreatment = 0; }
                file.version = 5;
            }
            if (file.version == 5)
            {
                if (file.commanders != null) foreach (var c in file.commanders)
                    if (c?.personalState != null) c.personalState.social = new Units.CommanderSocialState();
                if (file.buildings != null) foreach (var b in file.buildings)
                    if (b?.prisoners != null) foreach (var p in b.prisoners)
                        if (p != null) { p.personalState = new Units.CommanderPersonalState(); p.labAttack = p.labArmor = 0; p.strikeCooldown = p.stanceCooldown = 0; }
                file.version = 6;
            }
            if (file.version == 6)
            {
                if (file.world != null) file.world.legacyLayout = file.world.sites?.Count == 30;
                file.diplomacy = World.DiplomacyManager.InitialState(file.options?.seed ?? 0, file.world?.legacyLayout == true);
                file.version = 7;
            }
            if (file.version != SaveFileV1.CurrentVersion)
            {
                error = $"Save version {file.version} cannot be read by this build (expects {SaveFileV1.CurrentVersion}).";
                return false;
            }
            if (file.options == null || file.colony == null || file.world == null
                || file.commanders == null || file.buildings == null)
            {
                error = "Missing required sections.";
                return false;
            }
            if (!Enum.IsDefined(typeof(Core.MapSize), file.options.mapSize)) { error = "Unknown map size."; return false; }
            if (!Enum.IsDefined(typeof(Core.DifficultyLevel), file.options.difficulty)) { error = "Unknown difficulty."; return false; }
            if (!Enum.IsDefined(typeof(Core.CommanderDeathMode), file.options.commanderDeath)) { error = "Unknown commander death mode."; return false; }
            if (file.colony.antsFree < 0 || file.colony.antsAssigned < 0 || file.colony.antsReserved < 0)
            { error = "Negative ant counts."; return false; }
            foreach (var commander in file.commanders)
            {
                if (commander == null) { error = "Null commander entry."; return false; }
                if (commander.talents == null || !commander.talents.Validate()) { error = "Invalid commander talents."; return false; }
                if (commander.troopCount < 0) { error = "Negative troop count."; return false; }
                if (commander.position == null || !commander.position.IsFinite()) { error = "Invalid commander position."; return false; }
                if (commander.location < 0 || commander.location > 3) { error = "Unknown commander location."; return false; }
            }
            foreach (var building in file.buildings)
            {
                if (building == null || string.IsNullOrEmpty(building.kind)) { error = "Invalid building entry."; return false; }
                if (building.position == null || !building.position.IsFinite()) { error = "Invalid building position."; return false; }
            }
            if (file.world.sites == null || file.world.transports == null) { error = "Missing world sections."; return false; }
            return SavePreflight.Validate(file, out error);
        }
        private static void MigrateTraits(TraitsDto traits)
        {
            if (traits == null) return;
            traits.values = new System.Collections.Generic.List<Units.CommanderTrait>();
            traits.passions = new System.Collections.Generic.List<Units.CommanderPassion>();
            traits.loyaltyReasons = new System.Collections.Generic.List<string>();
        }
    }
}
