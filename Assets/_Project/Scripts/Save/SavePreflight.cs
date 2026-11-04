using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Save
{
    public static partial class SavePreflight
    {
        private static void Check(bool valid, string field) { if (!valid) throw new FormatException("저장 정보 오류: " + field + "."); }
        private static bool N(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0 && n <= 100000000;
        private static bool V(Vec3Dto v) => v != null && v.IsFinite() && Mathf.Abs(v.x) < 100000 && Mathf.Abs(v.y) < 100000 && Mathf.Abs(v.z) < 100000;
        private static bool R(int r) => Enum.IsDefined(typeof(UnitRole), r);
        private static void Traits(TraitsDto t)
        {
            Check(t != null && Enum.IsDefined(typeof(CommanderPersonality), t.personality), "특성");
            L(t.values, CommanderTraits.MaxTraits, "특성 목록"); L(t.passions, 4, "열정 목록");
            Check(SpecialtyProgress.Valid(t.specialties), "전문성 진행");
            var traits = new CommanderTraits();
            foreach (var value in t.values) Check(Enum.IsDefined(typeof(CommanderTrait), value) && traits.TryAdd(value), "상충 특성");
            var activities = new HashSet<CommanderActivity>();
            foreach (var passion in t.passions) Check(passion != null && Enum.IsDefined(typeof(CommanderActivity), passion.activity)
                && passion.flame >= 1 && passion.flame <= 2 && activities.Add(passion.activity), "열정");
            Check(t.passions.Count(p => p.flame == 2) <= 2, "특성 이력");
        }
        private static void L<T>(List<T> list, int max, string field) => Check(list != null && list.Count <= max, field);
        private static void Colony(EnemyColonyDto d)
        {
            if (d == null) return;
            Check(V(d.rootPosition) && N(d.waveTimer) && N(d.economyTimer) && d.waveIndex >= 0, "적 소굴");
            L(d.nodes, 10000, "적 소굴 자원 목록");
            Check(d.nodes.All(n => n != null && n.key != null && N(n.amount) && N(n.regrowTimer) && N(n.sowRemaining) && N(n.rebirthTimer) && n.rebirthTimer <= AntColony.World.ResourceNode.RebirthSeconds && N(n.rebirthPeak) && ValidNaturalWork(n) && V(n.position)
                && Enum.IsDefined(typeof(ResourceType), n.type)), "적 소굴 자원 정보");
            L(d.buildingHealth, 10000, "적 건물"); L(d.extraBuildings, 10000, "건물 위치");
            Check(d.buildingHealth.Count == d.extraBuildings.Count && d.buildingHealth.All(N) && d.extraBuildings.All(V), "적 건물 정보");
        }
        public static bool Validate(SaveFileV1 f, out string error)
        {
            error = null;
            try
            {
                Check(DateTime.TryParse(f.savedAtUtc, out _) && f.camera != null, "저장 기본 정보");
                Check(Core.ColonyPopulation.ValidNaturalDeaths(f.population), "자연사 소수 이월");
                ValidateIntegration(f);
                Check(Core.TalentAssessment.Validate(f.assessment, f.world.transports.Count, f.world.sites.Count), "인재 평가");
                Check(SaveUnderground.Valid(f), "지하 탐사");
                Check(AntColony.Core.RocketCampaign.Validate(f.rocket), "로켓 진행");
                Check(f.rocket == null || f.rocket.economy.samples.All(s => s.at <= f.gameSeconds), "로켓 경제 집계 날짜");
                Check(N(f.playSeconds) && N(f.gameSeconds) && N(f.upkeepTimer) && N(f.incursionTimer), "진행 시간");
                Check(f.upkeepFailures >= 0, "유지비 부족 횟수");
                Check(Core.CampaignResearch.Validate(f.campaign, out _), "과학 연구");
                Check(Core.CampaignHistory.Validate(f.history) && World.ColonyEvents.Validate(f.events), "사건·기록");
                // 무기고 파괴 후 초과 장비도 보존한다. 수용 한도는 새 장비 입고에만 적용한다.
                L(f.equipmentInventory, 10000, "장비 재고");
                var equipmentIds = new HashSet<string>();
                foreach (var item in f.equipmentInventory) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "재고 장비");
                L(f.equipmentLoot, 10000, "전리품 장비");
                L(f.corpses, 10000, "시체 목록");
                Check(SaveAttribution.Valid(f.constructionAttribution, f.construction), "시공 기여");
                Check(SaveConstruction.Valid(f.construction), "건설 예정지");
                var corpseWorkers = new HashSet<string>();
                foreach (var corpse in f.corpses)
                {
                    Check(corpse != null && !string.IsNullOrEmpty(corpse.name) && corpse.name.Length < 1000 && V(new Vec3Dto(corpse.position))
                        && Enum.IsDefined(typeof(World.CorpseKind), corpse.kind) && corpse.count > 0 && corpse.count <= 100000
                        && N(corpse.remaining) && (corpse.remaining > 0 || corpse.kind == World.CorpseKind.ColonyCommander && !string.IsNullOrEmpty(corpse.commanderId)) && corpse.remaining >= 0 && corpse.remaining <= World.Corpse.Lifetime // 장수 유해는 0
                        && N(corpse.food) && corpse.food <= 20 && (corpse.food == 0 || corpse.kind == World.CorpseKind.Wildlife)
                        && corpse.workforce >= 0
                        && N(corpse.progress) && corpse.progress < World.Corpse.WorkSeconds, "시체 상태");
                    if (!string.IsNullOrEmpty(corpse.workerId))
                        Check(corpseWorkers.Add(corpse.workerId) && f.commanders.Any(c => c?.personalState?.id == corpse.workerId
                            && c.activeInScene && !c.personalState.dead && c.location == 0 && c.personalState.work.duty == CommanderDuty.Civilian
                            && (!corpse.eating || corpse.kind != World.CorpseKind.Wildlife && c.traits.values.Contains(CommanderTrait.Cannibal))), "시체 담당자");
                    else Check(corpse.progress == 0 && !corpse.eating, "담당 없는 시체");
                }
                foreach (var loot in f.equipmentLoot)
                {
                    Check(loot != null && V(loot.position), "전리품 장비 위치");
                    L(loot.items, 10000, "전리품 장비 목록");
                    foreach (var item in loot.items) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "전리품 장비 소유권");
                }
                var personalIds = new HashSet<string>();
                Check(World.DiplomacyManager.Validate(f.diplomacy, f.world.sites.Count), "외교");
                Check(f.campaign?.politicalPresence == null || f.campaign.politicalPresence.All(p => p.site < f.world.sites.Count), "정치 담당 거점");
                foreach (var faction in f.diplomacy.civilizations.Concat(f.diplomacy.markets))
                {
                    foreach (var item in faction.equipment) Check(equipmentIds.Add(item.id), "외교 장비 소유권");
                    foreach (var prisoner in faction.prisoners.Concat(faction.rebels))
                    {
                        Check(prisoner != null && prisoner.PersonalState != null && prisoner.PersonalState.Validate(out _) && personalIds.Add(prisoner.PersonalState.id), "외교 포로");
                        Traits(SaveCatalog.Traits(prisoner.Traits));
                        Check(prisoner.Talents != null && prisoner.Talents.Validate() && prisoner.Roles != null && prisoner.Roles.Length > 0 && prisoner.Roles.All(r => R((int)r)), "외교 포로 재능");
                        foreach (var item in prisoner.PersonalState.equipment) Check(equipmentIds.Add(item.id), "외교 포로 장비");
                    }
                }
                Check(!string.IsNullOrEmpty(f.randomState) && f.randomState.Length < 1000, "난수 상태");
                JsonUtility.FromJson<UnityEngine.Random.State>(f.randomState);
                L(f.commanders, 1000, "장수 목록"); L(f.buildings, 10000, "건물 목록"); L(f.nodes, 100000, "자원 목록"); L(f.monsters, 10000, "적 목록");
                L(f.world.sites, 133, "거점 목록"); Check(f.world.sites.Count == (f.world.legacyLayout ? 30 : 33) + (f.diplomacy?.extraSites ?? 0), "거점 수"); Check(World.DiplomacyManager.Validate(f.diplomacy, f.world.sites.Count), "외교"); L(f.world.transports, 1000, "수송 수단 목록");
                L(f.discoveries, 1000, "도감 목록"); Check(f.discoveries.All(d => d != null && !string.IsNullOrEmpty(d.key) && d.key.Length < 200 && d.body != null && d.body.Length < 10000), "도감 항목");
                foreach (var n in f.nodes.Concat(f.buildings.SelectMany(b => b.nodes))) Check(n != null && n.workforce >= 0, "자원 작업 인력");
                foreach (var m in f.monsters) Check(m != null && Enum.IsDefined(typeof(World.WildlifeTemperament), m.temperament), "야생 동물 성향");
                var p = f.colony;
                Check(new[] { p.food, p.soil, p.special, p.foodCapacity, p.soilCapacity, p.specialCapacity, p.antsFree, p.antsAssigned, p.antsReserved }.All(v => v >= 0 && v <= 100000000), "정착지 보유량");
                Check(p.materials == null || p.materials.Length <= AntColony.Core.ResourceManager.OtherMaterials.Length && p.materials.All(v => v >= 0 && v <= p.soilCapacity), "재료");
                Check(p.food <= p.foodCapacity && p.soil <= p.soilCapacity && p.special <= p.specialCapacity && p.antsReserved == 0, "보관 한도·건설");
                Check(V(f.camera.focus) && N(f.camera.orthoSize) && f.camera.orthoSize >= 8 && f.camera.orthoSize <= 35
                    && !float.IsNaN(f.camera.yaw) && !float.IsInfinity(f.camera.yaw) && f.camera.viewedSite >= -1 && f.camera.viewedSite < f.world.sites.Count, "카메라");
                for (var i = 0; i < f.commanders.Count; i++)
                {
                    var c = f.commanders[i]; Check(c.id == i && !string.IsNullOrEmpty(c.name) && c.name.Length <= 200, "장수 식별자·이름");
                    Traits(c.traits);
                    Check(c.personalState != null && c.personalState.Validate(out _) && personalIds.Add(c.personalState.id), "장수 개인 상태");
                    foreach (var item in c.personalState.equipment) Check(equipmentIds.Add(item.id), "장비 중복 소유");
                    Check(!c.personalState.dead || c.troopCount == 0 && !c.activeInScene, "사망 장수");
                    // 부상·장비 해제·구 저장 이관으로 한도 초과가 되어도 기존 병력은 보존한다.
                    Check(V(c.position) && c.troopCount <= 100000000 && N(c.pendingDamage) && c.pendingDamage < 1, "병력");
                    Check(N(c.carriedAmount) && c.carriedAmount <= 100000000 && N(c.fishingProgress) && c.fishingProgress >= 0 && c.fishingProgress <= Core.GameBalance.FishingCatchSeconds && System.Enum.IsDefined(typeof(ResourceType), c.carriedType), "장수 운반 물자");
                    Check(c.carriedLots == null || c.carriedLots.Valid, "장수 운반 식품 신선도");
                    Check(c.talents != null && c.talents.Validate(), "재능");
                    Check(c.labAttackLevel >= 0 && c.labAttackLevel <= 3 && c.labArmorLevel >= 0 && c.labArmorLevel <= 3, "강화 단계");
                    Check(N(c.strikeCooldown) && N(c.stanceCooldown) && N(c.stanceTime), "기술");
                    Check(c.transportIndex >= -1 && c.transportIndex < f.world.transports.Count && c.siteIndex >= -1 && c.siteIndex < f.world.sites.Count, "장수 소속 지역");
                    Check(c.location != 1 || c.transportIndex >= 0, "승무원 연결"); Check(c.location < 2 || c.siteIndex >= 0, "거점 연결");
                    if (c.location == 2) Check(f.world.sites[c.siteIndex]?.disposition == 1, "주둔 거점 상태");
                    if (c.location == 3) Check((f.world.sites[c.siteIndex]?.disposition == 3 || f.diplomacy?.seizures?.Any(s => s.owner == c.personalState.id && s.item == "" && s.holder != "player") == true) && c.troopCount == 0, "포로 상태");
                }
                Check(f.buildings.Select(b => b.key).Distinct().Count() == f.buildings.Count, "건물 식별자");
                var targets = new HashSet<int>();
                foreach (var b in f.buildings)
                {
                    Check(b.workforce >= 0 && N(b.repairCredit) && b.repairCredit <= 100000000
                        && BabelState.Valid(b.babel) && (b.artistId == null || b.artistId.Length <= 1000)
                        && b.decorationQuality >= 0 && b.decorationQuality <= 3 && Kitchen.Valid(b.kitchen), "인력·수리·요리·장식");
                    Check(b.key != null && V(b.position) && N(b.health) && !float.IsNaN(b.rotationY) && !float.IsInfinity(b.rotationY), "건물");
                    Check(N(b.powerFuelSeconds) && b.powerFuelSeconds <= Core.GameBalance.WoodBurnSeconds, "발전 연료");
                    Check(!Enum.TryParse<BuildingKind>(b.kind, out var supplyKind) || !Buildings.SupplyFacility.IsKind(supplyKind)
                        || b.supply == null || Buildings.SupplyFacility.ValidForKind(supplyKind, b.supply), "공급 시설");
                    Check(b.kind != "PlanterBed" && b.kind != "StarterMushroomBed" || b.starterCrop == null || Buildings.StarterCropBed.Valid(b.starterCrop), "기초 작물");
                    Check(b.powerPriority >= 0 && b.powerPriority <= 9 && N(b.powerCharge) && b.powerCharge <= Core.GameBalance.BatteryCapacity
                        && (b.kind == "Battery" || b.powerCharge == 0) && b.materialTier >= 0 && Enum.IsDefined(typeof(ResourceType), b.mainMaterial) && ((ResourceType)b.mainMaterial).IsMaterial(), "건물 전력·재료");
                    Check(new[] { b.barracksUpgradeRemaining, b.labResearchRemaining, b.queenProductionRemaining, b.queenFishingRemaining, b.scienceRemaining,
                        b.scoutRemaining, b.prisonEscapeTimer, b.towerCooldown }.All(N) && V(b.scienceSpawn), "건물 진행 시간");
                    Check(b.barracksTier >= 1 && b.barracksTier <= 3 && R(b.role), "건물 등급·보직");
                    Check(b.serviceCommander >= -1 && b.serviceCommander < f.commanders.Count, "시설 담당 장수");
                    Check(b.labResearchCommanderId >= -1 && b.labResearchCommanderId < f.commanders.Count && (b.labResearchRemaining <= 0 || b.labResearchCommanderId >= 0 && targets.Add(b.labResearchCommanderId)), "연구 대상");
                    Check(b.scienceTier >= 1 && b.scienceTier <= Buildings.ScienceLab.MaxTier && b.scientist >= -1 && b.scientist < f.commanders.Count, "연구 담당·등급");
                    if (b.scientist >= 0) Check(b.kind == "ScienceLab" && targets.Add(b.scientist)
                        && f.commanders[b.scientist].location == 0 && !f.commanders[b.scientist].personalState.dead, "연구 담당 소속");
                    Check(b.kind != "AirshipYard" || AirshipYard.Validate(b.airship, f.commanders.Count, out _), "로켓");
                    if (b.kind == "AirshipYard" && b.airship != null)
                    {
                        Check(!Core.RocketSnapshot.Present(b.airship.snapshot) || b.airship.snapshot.at <= f.gameSeconds, "로켓 집계 날짜");
                        Check(b.airship.passengers.Sum(id => 1L + f.commanders[id].troopCount) + b.airship.civilianPassengers
                            <= AirshipYard.BaseSeats + AirshipYard.SeatsPerModule * b.airship.cocoons, "로켓 탑승 좌석");
                    }
                    var w = b.workshop;
                    Check(w != null, "공방 상태"); L(w.jobs, Core.GameBalance.CraftQueueCapacity, "제작 대기열");
                    if (w.orders != null)
                        Check(w.orders.Count <= Buildings.Workshop.MaxOrders && (b.kind == "Workshop" || w.orders.Count == 0) && w.orders.All(o => o != null && Enum.IsDefined(typeof(EquipmentRecipe), o.recipe)
                            && Enum.IsDefined(typeof(Buildings.Workshop.OrderMode), o.mode) && o.amount >= 1 && o.amount <= Buildings.Workshop.MaxOrderAmount), "생산 지시");
                    Check(w.crafter >= -1 && w.crafter < f.commanders.Count
                        && (b.kind == "Workshop" || w.jobs.Count == 0 && w.crafter == -1 && !w.ruined && !w.paused && !w.inactive), "제작 시설 종류");
                    for (int j = 0; j < w.jobs.Count; j++)
                        Check(w.jobs[j] != null && Enum.IsDefined(typeof(EquipmentRecipe), w.jobs[j].recipe)
                            && N(w.jobs[j].work) && w.jobs[j].work <= w.jobs[j].WorkRequired && (j == 0 || w.jobs[j].work == 0), "제작 작업");
                    if (w.crafter >= 0) Check(!w.ruined && !w.paused && !w.inactive && w.jobs.Count > 0 && b.health > 0 && targets.Add(w.crafter)
                        && f.commanders[w.crafter].location == 0 && f.commanders[w.crafter].activeInScene
                        && !f.commanders[w.crafter].personalState.dead && f.commanders[w.crafter].personalState.mentalBreak == MentalBreak.None, "제작자 소속");
                    Check(N(b.trapBroken) && N(b.trapRepair) && b.trapRepair <= Core.GameBalance.TrapRepairSeconds
                        && Enum.IsDefined(typeof(FarmCrop), b.crop) && (b.farm?.diamondCycle == null || b.farm.diamondCycle.Valid)
                        && (b.farm?.diamondReason == null || b.farm.diamondReason.Length <= 1000), "함정·농장 상태");
                    L(b.patients, Infirmary.Capacity, "의무실 환자");
                    foreach (var id in b.patients) Check(b.kind == "Infirmary" && b.health > 0 && id >= 0 && id < f.commanders.Count
                        && targets.Add(id) && f.commanders[id].location == 0 && f.commanders[id].activeInScene
                        && !f.commanders[id].personalState.dead && f.commanders[id].personalState.treating
                        && f.commanders[id].personalState.mentalBreak == MentalBreak.None, "환자 소속");
                    if (b.kind == "AirshipYard" && b.airship != null && b.airship.passengers != null)
                        foreach (var id in b.airship.passengers) Check(b.kind == "AirshipYard" && targets.Add(id)
                            && f.commanders[id].location == 0 && !f.commanders[id].personalState.dead, "로켓 승객 소속");
                    Check(new[] { b.scoutDispatchedAnts, b.scoutSuccess, b.scoutFailure, b.prisonRecruited, b.prisonExecuted, b.prisonEscaped, b.nurseryBirths }.All(v => v >= 0), "건물 누적 수치");
                    Check(b.scoutDispatched == (b.scoutRemaining > 0), "정찰 상태");
                    Check(b.scoutCompanion == -1 || b.scoutDispatched && b.scoutCompanion >= 0 && b.scoutCompanion < f.commanders.Count
                        && f.commanders[b.scoutCompanion].location == 0 && !f.commanders[b.scoutCompanion].personalState.dead, "정찰 동행자");
                    L(b.prisoners, 5, "포로 목록"); L(b.nurseryAffinity, 500000, "관계 수치"); L(b.nodes, 1000, "건물 자원 목록");
                    foreach (var prisoner in b.prisoners) { Check(prisoner != null && Enum.IsDefined(typeof(CommanderRank), prisoner.rank) && prisoner.persuadeAttempts >= 0, "포로");
                        Traits(prisoner.traits); Check(prisoner.talents != null && prisoner.talents.Validate(), "포로 재능");
                        Check(prisoner.personalState != null && prisoner.personalState.Validate(out _) && personalIds.Add(prisoner.personalState.id), "포로 개인 상태");
                        foreach (var item in prisoner.personalState.equipment) Check(equipmentIds.Add(item.id), "포로 장비 소유권");
                        Check(prisoner.labAttack >= 0 && prisoner.labAttack <= 3 && prisoner.labArmor >= 0 && prisoner.labArmor <= 3 && N(prisoner.strikeCooldown) && N(prisoner.stanceCooldown), "포로 강화·기술");
                        L(prisoner.roles, 6, "포로 보직"); Check(prisoner.roles.Count > 0 && prisoner.roles.All(R), "포로 보직"); }
                    foreach (var a in b.nurseryAffinity) Check(a != null && a.firstCommanderId >= 0 && a.firstCommanderId < f.commanders.Count && a.secondCommanderId >= 0 && a.secondCommanderId < f.commanders.Count && a.firstCommanderId != a.secondCommanderId && N(a.value), "관계 수치");
                    for (var i = 0; i < b.nodes.Count; i++) Check(b.nodes[i] != null && b.nodes[i].index == i && N(b.nodes[i].amount) && N(b.nodes[i].regrowTimer) && N(b.nodes[i].sowRemaining) && ValidNaturalWork(b.nodes[i]), "건물 자원");
                }
                Check((long)p.antsAssigned == f.commanders.Where(c => c.personalState.social.departure == DepartureState.None).Sum(c => (long)c.troopCount) + f.buildings.Sum(b => (long)b.scoutDispatchedAnts) + f.world.transports.Sum(t => (long)(t.withdrawal?.troops ?? 0)), "배정 시민");
                Check(f.weather == null || f.weather.kind >= 0 && f.weather.kind <= 6 && N(f.weather.remaining), "날씨");
                for (var i = 0; i < f.world.sites.Count; i++) { var s = f.world.sites[i]; Check(s != null && s.index == i && s.disposition >= 0 && s.disposition <= 3
                    && N(s.growthTimer) && N(s.settlementElapsed) && N(s.defenseRemaining) && N(s.defenseCaptureProgress), "거점 상태"); Colony(s.colony); }
                Colony(f.world.homeColony);
                Check(UI.SimulationPause.Valid(f.world.pause), "일시정지 상태");
                Check(f.world.defenseReturn == null || f.world.defenseReturn.disabled != null && f.world.defenseReturn.disabled.Count <= 10000 && f.world.defenseReturn.disabled.All(id => !string.IsNullOrEmpty(id)), "방어 귀환");
                for (var i = 0; i < f.world.transports.Count; i++)
                {
                    var s = f.world.transports[i]; Check(s != null && World.ExpeditionTransport.ValidFoodAmounts(s), "화물 신선도"); Check(s != null && s.state >= 0 && s.state <= 3 && N(s.remaining) && s.siteIndex >= -1 && s.siteIndex < f.world.sites.Count
                        && V(s.position) && V(s.homePosition) && s.cargoFood >= 0 && s.cargoSoil >= 0 && s.cargoSpecial >= 0, "수송 수단");
                    Check(s.state == 0 || s.siteIndex >= 0, "수송 목적지");
                    L(s.equipmentCargo, 10000, "장비 화물");
                    foreach (var item in s.equipmentCargo) Check(item != null && item.IsValid && equipmentIds.Add(item.id), "장비 화물 소유권");
                    Check(N(s.foodDebt) && s.foodDebt <= 100000000 && N(s.dockingRemaining) && s.dockingRemaining <= World.ExpeditionRules.DockSeconds && !(s.walking && s.aircraft), "원정 상태");
                    Check(s.route != null && N(s.route.waitSeconds) && s.route.destinationIndex >= -1 && s.route.destinationIndex < f.world.sites.Count
                        && (!s.route.running || s.route.destinationIndex >= 0 && f.world.sites[s.route.destinationIndex].disposition == 1 ), "수송 경로");
                    var crew = f.commanders.Where(c => c.location == 1 && c.transportIndex == i).ToArray();
                    var heavy = f.campaign.completed.Contains((int)Core.ScienceTechnology.HeavyTransport);
                    Check(s.walking || crew.Sum(c => (long)c.troopCount) + (s.withdrawal?.troops ?? 0) <= (s.aircraft ? Core.GameBalance.AircraftTroops : Core.GameBalance.VehicleTroops) * (heavy ? 1.5f : 1f)
                        && crew.Length <= (s.aircraft ? Core.GameBalance.AircraftCommanders : Core.GameBalance.VehicleCommanders) + (heavy ? 2 : 0), "수송 적재 한도");
                }
                Check(f.nodes.All(n => n != null && n.key != null && N(n.amount) && N(n.regrowTimer) && N(n.sowRemaining) && ValidNaturalWork(n) && n.fishMonth >= -1 && V(n.position) && Enum.IsDefined(typeof(ResourceType), n.type))
                    && f.nodes.Select(n => n.key).Distinct().Count() == f.nodes.Count, "자원 노드");
                Check(f.critters == null || f.critters.Count <= 1000 && f.critters.All(c => c != null && Enum.IsDefined(typeof(Buildings.Species), c.species) && N(c.health) && c.health > 0 && V(c.position) && (c.tamer == null || c.tamer.Valid) && (c.disease == null || c.disease.Valid)), "동물 목록");
                Check(f.monsters.All(m => m != null && m.key != null && N(m.health) && V(m.position)
                    && AntColony.Boss.AoE.BossAoEState.Valid(m.circleAoE) && AntColony.Boss.AoE.BossAoEState.Valid(m.coneAoE)
                    && AntColony.Boss.AoE.BossAoEState.Valid(m.lineAoE)
                    && (!AntColony.Boss.AoE.BossAoEState.Present(m.circleAoE) && !AntColony.Boss.AoE.BossAoEState.Present(m.coneAoE) && !AntColony.Boss.AoE.BossAoEState.Present(m.lineAoE) || m.key.StartsWith("boss:") && m.health > 0)
                    && AntColony.Boss.BossEncounter.Validate(m.bossEncounter) && (!AntColony.Boss.BossEncounter.Present(m.bossEncounter) || m.key.StartsWith("boss:")))
                    && f.monsters.Select(m => m.key).Distinct().Count() == f.monsters.Count, "적 목록");
                foreach (var m in f.monsters) if (m.traits != null)
                { Traits(m.traits); Check(m.talents != null && m.talents.Validate(), "적 재능"); }
                Check(f.monsters.All(m => string.IsNullOrEmpty(m.diplomaticFactionId) || f.diplomacy.civilizations.Any(c => c.id == m.diplomaticFactionId)), "적 세력");
                Check(Core.TalentAssessment.ValidateReferences(f), "평가 대상 연결");
                SaveOrderValidation.Validate(f);
                Check(SaveL2.Valid(f), "제작·예약 소유권");
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
        public static bool ForScene(SaveFileV1 f, out string error)
        {
            if (!SaveValidator.Validate(f, out error)) return false;
            try
            {
                Check(SaveCatalog.Ready, "장면 준비 상태");
                Check(f.buildings.Count(b => !b.runtimeBuilt) == SaveCatalog.Buildings.Length, "장면 건물");
                foreach (var b in f.buildings)
                {
                    BuildingBase template;
                    if (b.runtimeBuilt) { Check(Enum.TryParse<BuildingKind>(b.kind, out var kind), "건물 종류"); template = BuildingPlacementController.GetTemplate(kind, (UnitRole)b.role)?.GetComponent<BuildingBase>(); Check(template != null, "건설 정보"); }
                    else { Check(int.TryParse(b.key, out var i) && i >= 0 && i < SaveCatalog.Buildings.Length, "건물 번호"); template = SaveCatalog.Buildings[i]; }
                    if (template != null && b.kind != "Destroyed") Check(SaveCatalog.Kind(template) == b.kind && b.health <= template.MaxPossibleHealth
                        && b.nodes.Count == template.GetComponentsInChildren<World.ResourceNode>(true).Length, "건물 구성");
                }
                Check(f.nodes.Count(n => !n.key.StartsWith("new:") && !n.key.StartsWith("mat:")) == SaveCatalog.Nodes.Length, "장면 자원");
                foreach (var n in f.nodes) Check(n.key.StartsWith("new:") || n.key.StartsWith("mat:") && int.TryParse(n.key.Substring(4), out var m) && m >= 0 || int.TryParse(n.key, out var i) && i >= 0 && i < SaveCatalog.Nodes.Length, "자원 번호");
                Check(f.monsters.Count(m => m.key.StartsWith("monster:")) == SaveCatalog.Monsters.Length && f.monsters.Count(m => m.key.StartsWith("boss:")) == SaveCatalog.Bosses.Length, "장면 적");
                foreach (var m in f.monsters) { if (m.hasRuntime && !m.key.StartsWith("monster:")) continue; var key = m.key.Split(':'); Check(key.Length >= 2 && int.TryParse(key[1], out _), "적 식별자"); var i = int.Parse(key[1]);
                    Check(i >= 0 && (key[0] == "monster" && i < SaveCatalog.Monsters.Length || key[0] == "boss" && i < SaveCatalog.Bosses.Length
                        || key[0] == "occupier" && i < f.world.sites.Count && f.world.sites[i].disposition == 3), "적 번호"); }
                // JsonUtility writes null inline classes as empty objects.
                for (var i = 0; i < f.world.sites.Count; i++) Check((i >= SaveCatalog.ColonySizes.Length || SaveCatalog.ColonySizes[i] == 0) ? f.world.sites[i].colony == null || f.world.sites[i].colony.buildingHealth.Count == 0
                    : f.world.sites[i].colony != null && f.world.sites[i].colony.buildingHealth.Count >= SaveCatalog.ColonySizes[i], "소굴 구성");
                Check(Core.TalentAssessment.ValidateReferences(f), "평가 대상 연결");
                SaveOrderValidation.Validate(f);
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}



