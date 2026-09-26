using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Save
{
    public static class SavePreflight
    {
        private static void Check(bool valid, string field) { if (!valid) throw new FormatException("Invalid " + field + "."); }
        private static bool N(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0 && n <= 100000000;
        private static bool V(Vec3Dto v) => v != null && v.IsFinite() && Mathf.Abs(v.x) < 100000 && Mathf.Abs(v.y) < 100000 && Mathf.Abs(v.z) < 100000;
        private static bool R(int r) => Enum.IsDefined(typeof(UnitRole), r);
        private static void Traits(TraitsDto t)
        {
            Check(t != null && Enum.IsDefined(typeof(CommanderPersonality), t.personality) && t.loyalty >= 0 && t.loyalty <= 100, "traits");
            L(t.values, 3, "trait list"); L(t.passions, 4, "passions"); L(t.loyaltyReasons, 3, "loyalty history");
            var traits = new CommanderTraits();
            foreach (var value in t.values) Check(Enum.IsDefined(typeof(CommanderTrait), value) && traits.TryAdd(value), "trait conflicts");
            var activities = new HashSet<CommanderActivity>();
            foreach (var passion in t.passions) Check(passion != null && Enum.IsDefined(typeof(CommanderActivity), passion.activity)
                && passion.flame >= 1 && passion.flame <= 2 && activities.Add(passion.activity), "passion");
            Check(t.passions.Count(p => p.flame == 2) <= 2 && t.loyaltyReasons.All(r => r != null && r.Length < 1000), "trait history");
        }
        private static void L<T>(List<T> list, int max, string field) => Check(list != null && list.Count <= max, field);
        private static void Colony(EnemyColonyDto d)
        {
            if (d == null) return;
            Check(V(d.rootPosition) && N(d.waveTimer) && N(d.economyTimer) && d.waveIndex >= 0, "enemy colony");
            L(d.buildingHealth, 10000, "enemy buildings"); L(d.extraBuildings, 10000, "building positions");
            Check(d.buildingHealth.Count == d.extraBuildings.Count && d.buildingHealth.All(N) && d.extraBuildings.All(V), "enemy building data");
        }
        public static bool Validate(SaveFileV1 f, out string error)
        {
            error = null;
            try
            {
                Check(DateTime.TryParse(f.savedAtUtc, out _) && f.camera != null, "save header");
                Check(N(f.playSeconds) && N(f.gameSeconds) && N(f.upkeepTimer) && N(f.incursionTimer), "timers");
                Check(f.upkeepFailures >= 0, "upkeep failures");
                Check(Core.CampaignResearch.Validate(f.campaign, out _), "campaign research");
                Check(Core.CampaignHistory.Validate(f.history) && World.ColonyEvents.Validate(f.events), "events/history");
                L(f.equipmentInventory, EquipmentInventory.Capacity, "equipment inventory");
                var equipmentIds = new HashSet<string>();
                foreach (var item in f.equipmentInventory) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "inventory item");
                L(f.equipmentLoot, 10000, "equipment loot");
                foreach (var loot in f.equipmentLoot)
                {
                    Check(loot != null && V(loot.position), "equipment loot position");
                    L(loot.items, 10000, "equipment loot items");
                    foreach (var item in loot.items) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "equipment loot ownership");
                }
                var personalIds = new HashSet<string>();
                Check(World.DiplomacyManager.Validate(f.diplomacy, f.world.sites.Count), "diplomacy");
                foreach (var faction in f.diplomacy.civilizations.Concat(f.diplomacy.markets))
                {
                    foreach (var item in faction.equipment) Check(equipmentIds.Add(item.id), "diplomatic equipment ownership");
                    foreach (var prisoner in faction.prisoners.Concat(faction.rebels))
                    {
                        Check(prisoner != null && prisoner.PersonalState != null && prisoner.PersonalState.Validate(out _) && personalIds.Add(prisoner.PersonalState.id), "diplomatic prisoner");
                        Traits(SaveCatalog.Traits(prisoner.Traits));
                        Check(prisoner.Talents != null && prisoner.Talents.Validate() && prisoner.Roles != null && prisoner.Roles.Length > 0 && prisoner.Roles.All(r => R((int)r)), "diplomatic prisoner talents");
                        foreach (var item in prisoner.PersonalState.equipment) Check(equipmentIds.Add(item.id), "diplomatic prisoner equipment");
                    }
                }
                Check(!string.IsNullOrEmpty(f.randomState) && f.randomState.Length < 1000, "random state");
                JsonUtility.FromJson<UnityEngine.Random.State>(f.randomState);
                L(f.commanders, 1000, "commanders"); L(f.buildings, 10000, "buildings"); L(f.nodes, 100000, "nodes"); L(f.monsters, 10000, "monsters");
                L(f.world.sites, 133, "sites"); Check(f.world.sites.Count == (f.world.legacyLayout ? 30 : 33) + (f.diplomacy?.extraSites ?? 0), "site count"); Check(World.DiplomacyManager.Validate(f.diplomacy, f.world.sites.Count), "diplomacy"); L(f.world.transports, 1000, "transports");
                L(f.discoveries, 1000, "discoveries"); Check(f.discoveries.All(d => d != null && !string.IsNullOrEmpty(d.key) && d.key.Length < 200 && d.body != null && d.body.Length < 10000), "discovery entry");
                var p = f.colony;
                Check(new[] { p.food, p.soil, p.special, p.foodCapacity, p.soilCapacity, p.specialCapacity, p.antsFree, p.antsAssigned, p.antsReserved }.All(v => v >= 0 && v <= 100000000), "colony amounts");
                Check(p.food <= p.foodCapacity && p.soil <= p.soilCapacity && p.special <= p.specialCapacity && p.antsReserved == 0, "capacity/construction");
                Check(V(f.camera.focus) && N(f.camera.orthoSize) && f.camera.orthoSize >= 8 && f.camera.orthoSize <= 35
                    && !float.IsNaN(f.camera.yaw) && !float.IsInfinity(f.camera.yaw) && f.camera.viewedSite >= -1 && f.camera.viewedSite < f.world.sites.Count, "camera");
                for (var i = 0; i < f.commanders.Count; i++)
                {
                    var c = f.commanders[i]; Check(c.id == i && !string.IsNullOrEmpty(c.name) && c.name.Length <= 200, "commander ID/name");
                    Traits(c.traits);
                    Check(c.personalState != null && c.personalState.Validate(out _) && personalIds.Add(c.personalState.id), "commander personal state");
                    foreach (var item in c.personalState.equipment) Check(equipmentIds.Add(item.id), "duplicate equipment ownership");
                    Check(!c.personalState.dead || c.troopCount == 0 && !c.activeInScene, "dead commander");
                    // 부상·장비 해제·구 저장 이관으로 한도 초과가 되어도 기존 병력은 보존한다.
                    Check(V(c.position) && c.troopCount <= 100000000 && N(c.pendingDamage) && c.pendingDamage < 1, "troops");
                    Check(c.talents != null && c.talents.Validate(), "talents");
                    Check(c.labAttackLevel >= 0 && c.labAttackLevel <= 3 && c.labArmorLevel >= 0 && c.labArmorLevel <= 3, "upgrades");
                    Check(N(c.strikeCooldown) && N(c.stanceCooldown) && N(c.stanceTime), "skills");
                    Check(c.transportIndex >= -1 && c.transportIndex < f.world.transports.Count && c.siteIndex >= -1 && c.siteIndex < f.world.sites.Count, "commander location");
                    Check(c.location != 1 || c.transportIndex >= 0, "crew reference"); Check(c.location < 2 || c.siteIndex >= 0, "settlement reference");
                    if (c.location == 2) Check(f.world.sites[c.siteIndex]?.disposition == 1, "garrison disposition");
                    if (c.location == 3) Check(f.world.sites[c.siteIndex]?.disposition == 3 && c.troopCount == 0, "captivity");
                }
                Check(f.buildings.Select(b => b.key).Distinct().Count() == f.buildings.Count, "building IDs");
                var targets = new HashSet<int>();
                foreach (var b in f.buildings)
                {
                    Check(b.key != null && V(b.position) && N(b.health) && !float.IsNaN(b.rotationY) && !float.IsInfinity(b.rotationY), "building");
                    Check(new[] { b.barracksUpgradeRemaining, b.labResearchRemaining, b.queenProductionRemaining, b.queenFishingRemaining, b.scienceRemaining,
                        b.scoutRemaining, b.prisonEscapeTimer, b.towerCooldown }.All(N) && V(b.scienceSpawn), "building timers");
                    Check(b.barracksTier >= 1 && b.barracksTier <= 3 && R(b.role), "building tier/role");
                    Check(b.labResearchCommanderId >= -1 && b.labResearchCommanderId < f.commanders.Count && (b.labResearchRemaining <= 0 || b.labResearchCommanderId >= 0 && targets.Add(b.labResearchCommanderId)), "research target");
                    Check(b.scienceTier >= 1 && b.scienceTier <= 4 && b.scientist >= -1 && b.scientist < f.commanders.Count, "scientist/tier");
                    if (b.scientist >= 0) Check(b.kind == "ScienceLab" && targets.Add(b.scientist)
                        && f.commanders[b.scientist].location == 0 && !f.commanders[b.scientist].personalState.dead, "scientist ownership");
                    Check(AirshipYard.Validate(b.airship, f.commanders.Count, out _), "airship");
                    var w = b.workshop;
                    Check(w != null, "workshop state"); L(w.jobs, Core.GameBalance.CraftQueueCapacity, "craft queue");
                    Check(w.crafter >= -1 && w.crafter < f.commanders.Count
                        && (b.kind == "Workshop" || w.jobs.Count == 0 && w.crafter == -1 && !w.ruined && !w.paused && !w.inactive), "workshop kind");
                    for (int j = 0; j < w.jobs.Count; j++)
                        Check(w.jobs[j] != null && Enum.IsDefined(typeof(EquipmentRecipe), w.jobs[j].recipe)
                            && N(w.jobs[j].work) && w.jobs[j].work <= Core.GameBalance.CraftWork && (j == 0 || w.jobs[j].work == 0), "craft job");
                    if (w.crafter >= 0) Check(!w.ruined && !w.paused && !w.inactive && w.jobs.Count > 0 && b.health > 0 && targets.Add(w.crafter)
                        && f.commanders[w.crafter].location == 0 && f.commanders[w.crafter].activeInScene
                        && !f.commanders[w.crafter].personalState.dead && f.commanders[w.crafter].personalState.mentalBreak == MentalBreak.None, "crafter ownership");
                    Check(N(b.trapBroken) && N(b.trapRepair) && b.trapRepair <= Core.GameBalance.TrapRepairSeconds
                        && Enum.IsDefined(typeof(FarmCrop), b.crop), "trap/farm state");
                    L(b.patients, Infirmary.Capacity, "infirmary patients");
                    foreach (var id in b.patients) Check(b.kind == "Infirmary" && b.health > 0 && id >= 0 && id < f.commanders.Count
                        && targets.Add(id) && f.commanders[id].location == 0 && f.commanders[id].activeInScene
                        && !f.commanders[id].personalState.dead && f.commanders[id].personalState.treating
                        && f.commanders[id].personalState.NeedsTreatment
                        && f.commanders[id].personalState.mentalBreak == MentalBreak.None, "patient ownership");
                    if (b.airship != null && b.airship.passengers != null)
                        foreach (var id in b.airship.passengers) Check(b.kind == "AirshipYard" && targets.Add(id)
                            && f.commanders[id].location == 0 && !f.commanders[id].personalState.dead, "airship passenger ownership");
                    Check(new[] { b.scoutDispatchedAnts, b.scoutSuccess, b.scoutFailure, b.prisonRecruited, b.prisonExecuted, b.prisonEscaped, b.nurseryBirths }.All(v => v >= 0), "building counters");
                    Check(b.scoutDispatched == (b.scoutRemaining > 0), "scout state");
                    L(b.prisoners, 5, "prisoners"); L(b.nurseryAffinity, 500000, "affinity"); L(b.nodes, 1000, "building nodes");
                    foreach (var prisoner in b.prisoners) { Check(prisoner != null && Enum.IsDefined(typeof(CommanderRank), prisoner.rank) && prisoner.persuadeAttempts >= 0, "prisoner");
                        Traits(prisoner.traits); Check(prisoner.talents != null && prisoner.talents.Validate(), "prisoner talents");
                        Check(prisoner.personalState != null && prisoner.personalState.Validate(out _) && personalIds.Add(prisoner.personalState.id), "prisoner personal state");
                        foreach (var item in prisoner.personalState.equipment) Check(equipmentIds.Add(item.id), "prisoner equipment ownership");
                        Check(prisoner.labAttack >= 0 && prisoner.labAttack <= 3 && prisoner.labArmor >= 0 && prisoner.labArmor <= 3 && N(prisoner.strikeCooldown) && N(prisoner.stanceCooldown), "prisoner upgrades/skills");
                        L(prisoner.roles, 6, "prisoner roles"); Check(prisoner.roles.Count > 0 && prisoner.roles.All(R), "prisoner roles"); }
                    foreach (var a in b.nurseryAffinity) Check(a != null && a.firstCommanderId >= 0 && a.firstCommanderId < f.commanders.Count && a.secondCommanderId >= 0 && a.secondCommanderId < f.commanders.Count && a.firstCommanderId != a.secondCommanderId && N(a.value), "affinity");
                    for (var i = 0; i < b.nodes.Count; i++) Check(b.nodes[i] != null && b.nodes[i].index == i && N(b.nodes[i].amount) && N(b.nodes[i].regrowTimer), "building node");
                }
                Check((long)p.antsAssigned == f.commanders.Where(c => c.personalState.social.departure == DepartureState.None).Sum(c => (long)c.troopCount) + f.buildings.Sum(b => (long)b.scoutDispatchedAnts), "assigned population");
                for (var i = 0; i < f.world.sites.Count; i++) { var s = f.world.sites[i]; Check(s != null && s.index == i && s.disposition >= 0 && s.disposition <= 3
                    && N(s.growthTimer) && N(s.settlementElapsed) && N(s.defenseRemaining) && N(s.defenseCaptureProgress), "site state"); Colony(s.colony); }
                Colony(f.world.homeColony);
                var occupied = new HashSet<int>();
                for (var i = 0; i < f.world.transports.Count; i++)
                {
                    var s = f.world.transports[i]; Check(s != null && s.state >= 0 && s.state <= 3 && N(s.remaining) && s.siteIndex >= -1 && s.siteIndex < f.world.sites.Count
                        && V(s.position) && V(s.homePosition) && s.cargoFood >= 0 && s.cargoSoil >= 0 && s.cargoSpecial >= 0, "transport");
                    Check(s.state == 0 || s.siteIndex >= 0, "transport destination");
                    L(s.equipmentCargo, 10000, "equipment cargo");
                    foreach (var item in s.equipmentCargo) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "equipment cargo ownership");
                    if (s.state == 1 || s.state == 2) Check(occupied.Add(s.siteIndex), "duplicate visitor");
                    Check(s.route != null && N(s.route.waitSeconds) && s.route.destinationIndex >= -1 && s.route.destinationIndex < f.world.sites.Count
                        && (!s.route.running || s.route.destinationIndex >= 0 && f.world.sites[s.route.destinationIndex].disposition == 1 && s.state != 2), "route");
                    var crew = f.commanders.Where(c => c.location == 1 && c.transportIndex == i).ToArray();
                    var heavy = f.campaign.completed.Contains((int)Core.ScienceTechnology.HeavyTransport);
                    Check(crew.Sum(c => (long)c.troopCount) <= (s.aircraft ? Core.GameBalance.AircraftTroops : Core.GameBalance.VehicleTroops) * (heavy ? 1.5f : 1f)
                        && crew.Length <= (s.aircraft ? Core.GameBalance.AircraftCommanders : Core.GameBalance.VehicleCommanders) + (heavy ? 2 : 0), "transport capacity");
                }
                Check(f.nodes.All(n => n != null && n.key != null && N(n.amount) && N(n.regrowTimer) && V(n.position) && Enum.IsDefined(typeof(ResourceType), n.type))
                    && f.nodes.Select(n => n.key).Distinct().Count() == f.nodes.Count, "resource nodes");
                Check(f.monsters.All(m => m != null && m.key != null && N(m.health) && V(m.position)) && f.monsters.Select(m => m.key).Distinct().Count() == f.monsters.Count, "monsters");
                foreach (var m in f.monsters) if (m.traits != null)
                { Traits(m.traits); Check(m.talents != null && m.talents.Validate(), "enemy talents"); }
                Check(f.monsters.All(m => string.IsNullOrEmpty(m.diplomaticFactionId) || f.diplomacy.civilizations.Any(c => c.id == m.diplomaticFactionId)), "enemy faction");
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
        public static bool ForScene(SaveFileV1 f, out string error)
        {
            if (!SaveValidator.Validate(f, out error)) return false;
            try
            {
                Check(SaveCatalog.Ready, "scene readiness");
                Check(f.buildings.Count(b => !b.runtimeBuilt) == SaveCatalog.Buildings.Length, "scene buildings");
                foreach (var b in f.buildings)
                {
                    BuildingBase template;
                    if (b.runtimeBuilt) { Check(Enum.TryParse<BuildingKind>(b.kind, out var kind), "building type"); template = BuildingPlacementController.GetTemplate(kind, (UnitRole)b.role)?.GetComponent<BuildingBase>(); Check(template != null, "building template"); }
                    else { Check(int.TryParse(b.key, out var i) && i >= 0 && i < SaveCatalog.Buildings.Length, "building index"); template = SaveCatalog.Buildings[i]; }
                    if (template != null && b.kind != "Destroyed") Check(SaveCatalog.Kind(template) == b.kind && b.health <= template.MaxPossibleHealth
                        && b.nodes.Count == template.GetComponentsInChildren<World.ResourceNode>(true).Length, "building schema");
                }
                Check(f.nodes.Count(n => !n.key.StartsWith("new:")) == SaveCatalog.Nodes.Length, "scene nodes");
                foreach (var n in f.nodes) Check(n.key.StartsWith("new:") || int.TryParse(n.key, out var i) && i >= 0 && i < SaveCatalog.Nodes.Length, "node index");
                Check(f.monsters.Count(m => m.key.StartsWith("monster:")) == SaveCatalog.Monsters.Length && f.monsters.Count(m => m.key.StartsWith("boss:")) == SaveCatalog.Bosses.Length, "scene enemies");
                foreach (var m in f.monsters) { var key = m.key.Split(':'); Check(key.Length >= 2 && int.TryParse(key[1], out _), "enemy key"); var i = int.Parse(key[1]);
                    Check(i >= 0 && (key[0] == "monster" && i < SaveCatalog.Monsters.Length || key[0] == "boss" && i < SaveCatalog.Bosses.Length
                        || key[0] == "occupier" && i < f.world.sites.Count && f.world.sites[i].disposition == 3), "enemy index"); }
                // JsonUtility writes null inline classes as empty objects.
                for (var i = 0; i < f.world.sites.Count; i++) Check((i >= SaveCatalog.ColonySizes.Length || SaveCatalog.ColonySizes[i] == 0) ? f.world.sites[i].colony == null || f.world.sites[i].colony.buildingHealth.Count == 0
                    : f.world.sites[i].colony != null && f.world.sites[i].colony.buildingHealth.Count >= SaveCatalog.ColonySizes[i], "colony schema");
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}



