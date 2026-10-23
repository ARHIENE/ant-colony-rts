using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.Save
{
    public static class SaveSnapshot
    {
        public static SaveFileV1 Capture()
        {
            if (!SaveCatalog.CanSave(out var error)) throw new InvalidOperationException(error);
            SaveReferences.Clear();
            var rm = ResourceManager.Instance;
            var pool = AntPool.Instance;
            var world = WorldMapManager.Instance;
            var options = GameSession.Instance.Options;
            var commanders = CommanderRoster.Instance.Commanders.ToList();
            var ships = world.Transports.Where(s => s != null).ToList();
            for (int i = 0; i < commanders.Count; i++) SaveReferences.Register("commander:" + i, commanders[i]);
            for (int i = 0; i < ships.Count; i++) SaveReferences.Register("ship:" + i, ships[i]);
            SaveReferences.Colonies(); SaveReferences.Actors();
            SaveRuntime.RegisterMonsters();
            var file = new SaveFileV1 { savedAtUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                label = options.mapSize + " / " + options.difficulty, playSeconds = GameSession.Instance.PlaySeconds, gameSeconds = GameSession.Instance.GameSeconds,
                options = new OptionsDto { mapSize = (int)options.mapSize, difficulty = (int)options.difficulty, commanderDeath = (int)options.commanderDeath, seed = options.seed, biome = (int)options.biome },
                randomState = JsonUtility.ToJson(UnityEngine.Random.state),
                upkeepTimer = Object.FindFirstObjectByType<UpkeepManager>()?.SavedTimer ?? 0,
                upkeepFailures = Object.FindFirstObjectByType<UpkeepManager>()?.ConsecutiveFailures ?? 0,
                campaign = CampaignResearch.Instance?.CaptureState() ?? new CampaignResearch.State(),
                history = CampaignHistory.Instance?.Capture() ?? new CampaignHistory.State(),
                events = ColonyEvents.Instance?.Capture() ?? new ColonyEvents.State(),
                memorial = Memorial.Capture(),
                weather = AntColony.Map.WeatherSystem.Instance?.CaptureState(),
                population = ColonyPopulation.Instance?.Capture() ?? new ColonyPopulation.State(),
                workPriorities = WorkPriorities.Capture(),
                diplomacy = DiplomacyManager.Instance?.Capture(),
                equipmentInventory = EquipmentInventory.Instance == null ? new List<EquipmentItem>() : EquipmentInventory.Instance.Items.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))).ToList(),
                corpses = Corpse.All.Where(c => c.Available).Select(c => c.Capture()).ToList(),
                construction = SaveConstruction.Capture(),
                critters = Critter.CaptureAll(),
                equipmentLoot = SaveCatalog.Ordered<EquipmentLoot>().Select(l => new EquipmentLootDto { position = new Vec3Dto(l.transform.position),
                    items = l.Items.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))).ToList() }).ToList(),
                incursionTimer = Object.FindFirstObjectByType<LocalIncursions>()?.SavedTimer ?? 0,
                loopCompleted = GameManager.Instance.SavedLoop, bossDefeated = GameManager.Instance.SavedBoss, defeated = GameManager.Instance.SavedDefeat,
                colony = new ColonyDto { food = rm.GetAmount(ResourceType.Food), soil = rm.GetAmount(ResourceType.Soil), special = rm.GetAmount(ResourceType.Special), materials = rm.CaptureMaterials(), baseBin = rm.BaseBin.Capture(),
                    foodCapacity = rm.GetCapacity(ResourceType.Food), soilCapacity = rm.GetCapacity(ResourceType.Soil), specialCapacity = rm.GetCapacity(ResourceType.Special),
                    storageResearchApplied = true,
                    antsFree = pool.Free, antsAssigned = pool.Assigned, antsReserved = pool.Reserved, fishingUnlocked = GameManager.Instance.FishingUnlocked, commonDiet = CommanderAnt.CommonDiet } };
            foreach (var c in commanders) file.commanders.Add(new CommanderDto { id = file.commanders.Count, name = c.CommanderName,
                troopCount = c.TroopCount, pendingDamage = c.PendingTroopDamage, carriedAmount = c.CarriedAmount, carriedType = (int)c.CarriedType, carriedLots = c.CaptureCargoLots(), fishingProgress = c.FishingProgress, talents = c.Talents.Copy(),
                traits = SaveCatalog.Traits(c.Traits), personalState = c.CapturePersonalState(),
                labAttackLevel = c.LabAttackLevel, labArmorLevel = c.LabArmorLevel, position = new Vec3Dto(c.Position),
                activeInScene = c.gameObject.activeSelf, location = c.IsCaptive ? 3 : c.Garrison != null ? 2 : c.Transport != null ? 1 : 0,
                siteIndex = SaveCatalog.SiteIndex(c.Captor != null ? c.Captor : c.Garrison?.Site), transportIndex = SaveCatalog.ShipIndex(c.Transport),
                strikeArmed = c.Skills.PowerStrikeArmed, strikeCooldown = c.Skills.PowerStrikeCooldownLeft,
                stanceCooldown = c.Skills.DefensiveStanceCooldownLeft, stanceTime = c.Skills.DefensiveStanceTimeLeft });
            for (var i = 0; i < SaveCatalog.Buildings.Length; i++) file.buildings.Add(SaveBuildings.Capture(SaveCatalog.Buildings[i], i.ToString(), false, commanders));
            foreach (var b in SaveCatalog.Ordered<BuildingBase>().Where(b => b.CountsTowardPlayerDefeat && !(b is ExpeditionTransport) && !SaveCatalog.Buildings.Contains(b)))
                file.buildings.Add(SaveBuildings.Capture(b, "new:" + file.buildings.Count, true, commanders));
            foreach (var w in Object.FindObjectsByType<Workshop>(FindObjectsInactive.Include).Where(w => !w.gameObject.activeInHierarchy && !w.name.EndsWith("Template") && !SaveCatalog.Buildings.Contains(w)))
                file.buildings.Add(SaveBuildings.Capture(w, "new:" + file.buildings.Count, true, commanders));
            for (var i = 0; i < SaveCatalog.Nodes.Length; i++) file.nodes.Add(Node(SaveCatalog.Nodes[i], i.ToString()));
            for (var i = 0; i < Map.MaterialDeposits.Spawned.Count; i++) file.nodes.Add(Node(Map.MaterialDeposits.Spawned[i], "mat:" + i));
            foreach (var n in SaveCatalog.Ordered<ResourceNode>().Where(n => n.GetComponent<Map.MaterialDeposit>() == null && !SaveCatalog.Nodes.Contains(n) && n.GetComponentInParent<BuildingBase>() == null && n.GetComponent<EventActor>() == null && n.GetComponent<Corpse>() == null))
                file.nodes.Add(Node(n, "new:" + file.nodes.Count));
            SaveRuntime.CaptureMonsters(file);
            for (var i = 0; i < SaveCatalog.Bosses.Length; i++)
            { var b = SaveCatalog.Bosses[i]; file.monsters.Add(new MonsterDto { key = "boss:" + i, health = b != null ? b.CurrentHp : 0,
                position = new Vec3Dto(b != null ? b.Position : Vector3.zero) }); }
            file.world.unlocked = world.Unlocked; file.world.vehicleResearched = world.VehicleResearched; file.world.aircraftResearched = world.AircraftResearched;
            file.world.notice = world.SettlementNotice;
            file.world.legacyLayout = world.LegacyLayout;
            foreach (var site in world.Sites) file.world.sites.Add(new SiteDto { index = file.world.sites.Count, cleared = site.Cleared,
                rewardsClaimed = site.RewardsClaimed,
                disposition = (int)site.Disposition, growthTimer = site.GrowthTimer, colony = Colony(site.Colony),
                settlementElapsed = site.Settlement?.Elapsed ?? 0, defenseRemaining = site.Defense?.Remaining ?? 0,
                defenseUnderAttack = site.Defense?.UnderAttack ?? false, defenseFaction = site.Defense?.CaptureFaction(),
                defenseCaptureProgress = site.Defense?.CaptureProgress ?? 0 });
            var home = Object.FindObjectsByType<EnemyColony>(FindObjectsSortMode.None).FirstOrDefault(c => c.GetComponentInParent<ExpeditionSite>() == null);
            file.world.hadHomeColony = home != null; file.world.homeColony = Colony(home);
            foreach (var s in world.Transports.Where(s => s != null)) file.world.transports.Add(new TransportDto { aircraft = s.Aircraft, state = (int)s.State,
                blueprintCargo = s.BlueprintCargo, equipmentCargo = s.EquipmentCargo.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))).ToList(),
                remaining = s.Remaining, siteIndex = SaveCatalog.SiteIndex(s.Site), position = new Vec3Dto(s.Position), homePosition = new Vec3Dto(s.HomePosition),
                cargoFood = s.GetCargo(ResourceType.Food), cargoSoil = s.GetCargo(ResourceType.Soil), cargoSpecial = s.GetCargo(ResourceType.Special),
                route = new RouteDto { running = s.Route.IsRunning, destinationIndex = SaveCatalog.SiteIndex(s.Route.Destination), waitSeconds = s.Route.WaitSeconds, status = s.Route.Status } });
            var camera = Object.FindFirstObjectByType<AntColony.Camera.IsometricCameraController>();
            file.camera = new CameraDto { focus = new Vec3Dto(camera.FocusPoint), yaw = camera.Yaw,
                orthoSize = camera.GetComponent<UnityEngine.Camera>().orthographicSize, viewedSite = SaveCatalog.SiteIndex(world.ViewedSite) };
            SaveReferences.Colonies();
            for (int i = 0; i < commanders.Count; i++) { file.commanders[i].order = commanders[i].CaptureOrder(); file.commanders[i].hasOrder = true; }
            for (int i = 0; i < file.world.transports.Count; i++) {
                var ship = ships[i]; ship.Route.CaptureProgress(file.world.transports[i].route);
                file.world.transports[i].route.crew = ship.Crew.Select(c => commanders.IndexOf(c)).ToList();
            }
            file.discoveries = Encyclopedia.Entries.ToList();
            return file;
        }

        private static ResourceNodeDto Node(ResourceNode n, string key)
        {
            SaveReferences.Register("node:" + key, n);
            return new ResourceNodeDto { key = key, exists = n != null,
            looseCargo = n != null && n.IsLooseCargo,
            amount = n != null ? n.AmountRemaining : 0, regrowTimer = n != null ? n.RegrowTimeRemaining : 0, sowRemaining = n != null ? n.SowRemaining : 0, bountifulHarvest = n != null && n.BountifulHarvest, gatheringForbidden = n != null && n.GatheringForbidden, fishMonth = n != null ? n.FishSeason : -1, lots = n != null && n.TryGetComponent<FloorFood>(out var food) ? food.Sync() : null,
            position = new Vec3Dto(n != null ? n.transform.position : Vector3.zero), type = n != null ? (int)n.ResourceType : 0 };
        }

        private static EnemyColonyDto Colony(EnemyColony c)
        {
            if (c == null) return null;
            var invasion = c.GetComponent<ColonyInvasion>();
            var site = SaveCatalog.SiteIndex(c.GetComponentInParent<ExpeditionSite>());
            return new EnemyColonyDto { nodes = c.GetComponentsInChildren<ResourceNode>(true).Select((n, i) => Node(n, "colony:" + site + ":" + i)).ToList(), rootPosition = new Vec3Dto(c.transform.position),
                buildingHealth = c.Buildings.Select(b => b != null ? b.CurrentHealth : 0).ToList(),
                extraBuildings = c.Buildings.Select(b => new Vec3Dto(b != null ? b.Position : Vector3.zero)).ToList(),
                waveTimer = invasion?.WaveTimer ?? 0, economyTimer = invasion?.EconomyTimer ?? 0, waveIndex = invasion?.WaveIndex ?? 0 };
        }

        private static void RestoreColony(EnemyColony c, EnemyColonyDto d)
        {
            if (c == null || d == null) return;
            c.SuppressRandomPlacement(); c.transform.position = d.rootPosition.ToVector3();
            while (c.Buildings.Length < d.buildingHealth.Count)
                if (c.RestoreExpansion(d.extraBuildings[c.Buildings.Length].ToVector3()) == null) throw new InvalidOperationException("Cannot restore colony expansion.");
            for (var i = 0; i < c.Buildings.Length; i++)
            {
                var b = c.Buildings[i];
                if (b == null) continue;
                if (d.buildingHealth[i] <= 0) { b.gameObject.SetActive(false); Object.Destroy(b.gameObject); }
                else { b.transform.position = d.extraBuildings[i].ToVector3(); b.RestoreHealth(d.buildingHealth[i]); }
            }
            var nodes = c.GetComponentsInChildren<ResourceNode>(true).ToList();
            for (int i = 0; i < d.nodes.Count; i++) {
                var n = d.nodes[i];
                var node = nodes.FirstOrDefault(x => (int)x.ResourceType == n.type && (x.transform.position - n.position.ToVector3()).sqrMagnitude < .09f);
                if (node == null) throw new InvalidOperationException("Missing colony resource node: " + n.key);
                nodes.Remove(node); node.transform.position = n.position.ToVector3();
                node.RestoreState(n.amount, n.regrowTimer, n.sowRemaining); node.FishSeason = n.fishMonth;
                node.IsLooseCargo = n.looseCargo; node.BountifulHarvest = n.bountifulHarvest; node.GatheringForbidden = n.gatheringForbidden;
                if (n.lots != null && node.TryGetComponent<FloorFood>(out var food)) food.Lots = n.lots;
                SaveReferences.Register("node:" + n.key, node);
            }
            c.GetComponent<ColonyInvasion>()?.RestoreTimers(d.waveTimer, d.economyTimer, d.waveIndex);
        }

        internal static IEnumerator Restore(SaveFileV1 file)
        {
            SaveReferences.Clear();
            var world = WorldMapManager.Instance;
            CommanderRoster.Instance.ClearAll();
            yield return null; // OnDestroy의 병력 반환을 끝낸 뒤 저장된 풀을 적용한다.
            foreach (var d in file.world.sites)
            {
                var s = world.Sites[d.index]; RestoreColony(s.Colony, d.colony);
                s.RestoreState(d.cleared, (ConquestDisposition)d.disposition); s.GrowthTimer = d.growthTimer;
                s.RewardsClaimed = d.rewardsClaimed;
                s.Settlement?.RestoreElapsed(d.settlementElapsed); s.Defense?.RestoreState(d.defenseRemaining, d.defenseCaptureProgress);
            }
            if (file.world.hadHomeColony) RestoreColony(Object.FindObjectsByType<EnemyColony>(FindObjectsSortMode.None)
                .First(c => c.GetComponentInParent<ExpeditionSite>() == null), file.world.homeColony);
            foreach (var d in file.nodes)
            {
                ResourceNode node;
                if (d.key.StartsWith("new:"))
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Saved Loot"; go.SetActive(false);
                    node = go.AddComponent<ResourceNode>(); node.ConfigureLoot((ResourceType)d.type, d.amount);
                }
                else if (d.key.StartsWith("mat:"))
                {
                    var m = int.Parse(d.key.Substring(4)); // 같은 시드로 다시 생긴 원재료 노드. 개수가 바뀐 빌드면 남는 기록은 버린다.
                    node = m < Map.MaterialDeposits.Spawned.Count ? Map.MaterialDeposits.Spawned[m] : null;
                }
                else node = SaveCatalog.Nodes[int.Parse(d.key)];
                SaveReferences.Register("node:" + d.key, node);
                if (node == null) continue;
                if (!d.exists) { Object.Destroy(node.gameObject); continue; }
                node.transform.position = d.position.ToVector3(); node.RestoreState(d.amount, d.regrowTimer, d.sowRemaining);
                node.IsLooseCargo = d.looseCargo;
                node.BountifulHarvest = d.bountifulHarvest; node.GatheringForbidden = d.gatheringForbidden; node.FishSeason = d.fishMonth;
                if (d.lots != null && d.lots.lots.Count > 0 && d.lots.Valid && node.TryGetComponent<FloorFood>(out var food)) food.Lots = d.lots;
            }
            foreach (var d in file.monsters)
            {
                if (d.hasRuntime) continue;
                var parts = d.key.Split(':'); var index = int.Parse(parts[1]);
                if (parts[0] == "boss")
                {
                    var b = SaveCatalog.Bosses[index]; SaveReferences.Register(d.key, b); if (b == null) continue;
                    if (d.health <= 0) b.RestoreDefeated(); else { b.transform.position = d.position.ToVector3(); b.RestoreHp(d.health); }
                    continue;
                }
                var m = parts[0] == "monster" ? SaveCatalog.Monsters[index]
                    : Object.Instantiate(world.Sites[index].GuardTemplate, d.position.ToVector3(), Quaternion.identity, world.Sites[index].transform);
                SaveReferences.Register(d.key, m);
                if (m == null) continue;
                m.DiplomaticFactionId = d.diplomaticFactionId; m.HuntDesignated = d.huntDesignated; m.Temperament = d.temperament;
                if (parts[0] == "occupier") m.RaidSettlement(world.Sites[index]);
                m.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                m.transform.position = d.position.ToVector3(); m.RestoreHealth(d.health);
                if (d.health > 0) m.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
                if (m is EnemyCommander ec && d.traits != null)
                { ec.ConfigureCommander(ec.CommanderName, ec.Rank, ec.Roles, SaveCatalog.Traits(d.traits)); ec.RestoreTalents(d.talents); }
            }
            world.VehicleResearched = file.world.vehicleResearched; world.AircraftResearched = file.world.aircraftResearched;
            world.RestoreUnlock(file.world.unlocked); world.SettlementNotice = file.world.notice;
            var ships = new List<ExpeditionTransport>();
            foreach (var d in file.world.transports)
            {
                var ship = world.CreateTransport(d.aircraft, d.homePosition.ToVector3()); SaveReferences.Register("ship:" + ships.Count, ship); ships.Add(ship);
                ship.BlueprintCargo = d.blueprintCargo; ship.EquipmentCargo = d.equipmentCargo.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))).ToList();
                ship.RestoreState((ExpeditionState)d.state, d.remaining, d.siteIndex < 0 ? null : world.Sites[d.siteIndex],
                    d.position.ToVector3(), d.homePosition.ToVector3(), d.cargoFood, d.cargoSoil, d.cargoSpecial);
                ship.Route.RestoreState(d.route.running, d.route.destinationIndex < 0 ? null : world.Sites[d.route.destinationIndex], d.route.waitSeconds, d.route.status);
            }
            var commanders = new List<CommanderAnt>();
            foreach (var d in file.commanders)
            {
                var c = CommanderRoster.Instance.CreateForLoad(d.name, CommanderRank.Sergeant, new[] { UnitRole.Worker },
                    UnitRole.Worker, SaveCatalog.Traits(d.traits), world.HomePosition + Vector3.right * 3);
                if (c == null) throw new InvalidOperationException("No navigable commander spawn.");
                SaveReferences.Register("commander:" + commanders.Count, c);
                commanders.Add(c); c.RestorePersonalState(d.personalState); c.RestoreTroops(d.troopCount, d.pendingDamage); c.RestoreCargo(d.carriedAmount, (ResourceType)d.carriedType, d.carriedLots); c.RestoreFishing(d.fishingProgress); c.RestoreLabLevels(d.labAttackLevel, d.labArmorLevel);
                c.RestoreTalents(d.talents);
                c.Skills.Restore(d.strikeArmed, d.strikeCooldown, d.stanceCooldown, d.stanceTime);
                c.SetEmbarked(false, d.position.ToVector3());
                c.RefreshDepartureVisual();
                if (d.location == 2) world.Sites[d.siteIndex].Settlement.Add(c);
                if (d.location == 3) world.Sites[d.siteIndex].Defense.RestorePrisoner(c);
            }
            for (var i = 0; i < ships.Count; i++)
            {
                ships[i].RestoreCrew(file.world.transports[i].route.crew.Count > 0
                    ? file.world.transports[i].route.crew.Select(id => commanders[id]).ToList()
                    : file.commanders.Where(d => d.location == 1 && d.transportIndex == i).Select(d => commanders[d.id]).ToList());
                if (ships[i].State == ExpeditionState.Deployed)
                    foreach (var d in file.commanders.Where(d => d.location == 1 && d.transportIndex == i)) commanders[d.id].SetEmbarked(false, d.position.ToVector3());
            }
            var restoredBuildings = new List<(BuildingBase building, BuildingDto dto)>();
            foreach (var d in file.buildings) { var b = SaveBuildings.Resolve(d); SaveBuildings.Restore(b, d, commanders); SaveReferences.Building(d.key, b); restoredBuildings.Add((b, d)); }
            yield return null; // 제거된 건물의 OnDisable/창고 상한 변경 후 확정값 복원.
            CampaignResearch.Instance?.RestoreState(file.campaign);
            // 감시탑 복원 후 감시 범위를 반영해 진행 중이던 사전 경보를 복구한다.
            foreach (var d in file.world.sites) world.Sites[d.index].Defense?.RestoreState(d.defenseRemaining, d.defenseCaptureProgress);
            // 방어시설 내구 단계가 복원된 뒤 체력을 다시 넣어야 상한에 잘리지 않는다.
            foreach (var (b, d) in restoredBuildings) if (b != null && d.kind != "Destroyed") b.RestoreHealth(d.health);
            // 건물·불 복원 뒤 장수 서비스 지시(소화 등)를 다시 건다. 경로는 새로 계산한다.
            foreach (var (b, d) in restoredBuildings) if (b != null && d.serviceCommander >= 0 && !file.commanders[d.serviceCommander].hasOrder) commanders[d.serviceCommander].StartService(b, (CommanderJobs)d.serviceJob);
            var p = file.colony;
            // Older saves include base warehouse capacity only, even if Fermentation was researched.
            var storageBonus = Vector3Int.zero;
            if (!p.storageResearchApplied)
                foreach (var storage in Object.FindObjectsByType<Storage>()) storageBonus += storage.ResearchBonus;
            ResourceManager.Instance.RestoreState(p.food, p.soil, p.special,
                p.foodCapacity + storageBonus.x, p.soilCapacity + storageBonus.y, p.specialCapacity + storageBonus.z);
            ResourceManager.Instance.RestoreMaterials(p.materials);
            ResourceManager.Instance.RestoreBins(p.baseBin, restoredBuildings.Where(x => x.building is Storage).Select(x => ((Storage)x.building, x.dto.storeBin)));
            AntPool.Instance.RestoreCounts(p.antsFree, p.antsAssigned, p.antsReserved);
            CommanderAnt.CommonDiet = p.commonDiet & CommanderAnt.AllDiet;
            GameManager.Instance.FishingUnlocked = p.fishingUnlocked
                || CampaignResearch.Instance != null && CampaignResearch.Instance.Has(ScienceTechnology.Fishing);
            GameManager.Instance.RestoreFlags(file.loopCompleted, file.bossDefeated, file.defeated);
            for (var i = 0; i < commanders.Count; i++)
                if (!file.commanders[i].activeInScene || commanders[i].IsDead) commanders[i].gameObject.SetActive(false);
            var upkeep = Object.FindFirstObjectByType<UpkeepManager>(); if (upkeep != null) upkeep.SavedTimer = file.upkeepTimer;
            if (upkeep != null) upkeep.RestoreFailures(file.upkeepFailures);
            if (EquipmentInventory.Instance != null) EquipmentInventory.Instance.Items = file.equipmentInventory.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))).ToList();
            foreach (var loot in file.equipmentLoot)
                EquipmentLoot.Drop(loot.position.ToVector3() - Vector3.up * .35f, loot.items.Select(e => JsonUtility.FromJson<EquipmentItem>(JsonUtility.ToJson(e))));
            foreach (var corpse in file.corpses) Corpse.Spawn(corpse);
            SaveConstruction.Restore(file.construction);
            Critter.RestoreAll(file.critters); // 2026-10-10 야생·탈출 사육 생물(옛 저장은 없음)
            var incursions = Object.FindFirstObjectByType<LocalIncursions>(); if (incursions != null) incursions.SavedTimer = file.incursionTimer;
            if (file.camera.viewedSite >= 0) world.ViewSite(world.Sites[file.camera.viewedSite]);
            Object.FindFirstObjectByType<AntColony.Camera.IsometricCameraController>().RestoreView(file.camera.focus.ToVector3(), Mathf.Round(file.camera.yaw / 90f) * 90f, file.camera.orthoSize); // 옛 쿼터뷰 45° 저장은 정면 시점으로
            Encyclopedia.Merge(file.discoveries); GameSession.Instance.MarkStarted(file.playSeconds, file.gameSeconds);
            foreach (var corpse in Corpse.All.ToArray())
                commanders.FirstOrDefault(c => c.PersonalState.id == corpse.Data.workerId)?.RestoreCorpseWork(corpse);
            Memorial.Restore(file.memorial); CampaignHistory.Instance.Restore(file.history); ColonyEvents.Instance.Restore(file.events); ColonyPopulation.Instance?.Restore(file.population); AntColony.Map.WeatherSystem.Instance?.RestoreState(file.weather); DiplomacyManager.Instance.RestoreRebels(); WorkPriorities.Restore(file.workPriorities);
            SaveReferences.Colonies(); SaveReferences.Actors();
            SaveRuntime.RestoreMonsters(file);
            for (int i = 0; i < commanders.Count; i++) if (file.commanders[i].hasOrder) commanders[i].RestoreOrder(file.commanders[i].order);
            // BeginPickup restores the courier ledger; restore the independent unpicked reservations afterwards.
            foreach (var d in file.construction ?? new List<ConstructionDto>()) {
                var site = SaveReferences.Get<BuildingConstructionSite>("construction:" + file.construction.IndexOf(d));
                if (site != null) for (int i = 0; i < d.needs.Count; i++) site.Needs[i].reserved = d.needs[i].reserved;
            }
            for (int i = 0; i < ships.Count; i++) ships[i].Route.RestoreProgress(file.world.transports[i].route);
            CommanderAnt.RefreshDepartureNotice();
            UnityEngine.Random.state = JsonUtility.FromJson<UnityEngine.Random.State>(file.randomState);
        }
    }
}

