using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

// unity command run_script --file AgentScripts/Stage2Checks.cs --entry Stage2Checks.Main (fresh Play mode).
// 2026-09-25 작업 지시서 2단계: 과학 기술별 완료 전/후 효과, 새 시설 7종 건설·전투·저장 복원.
public static class Stage2Checks
{
    static int checks;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable test position");
        Check(c.Agent.Warp(hit.position), "commander warp");
    }
    static void Grant(ScienceTechnology t)
    {
        var r = CampaignResearch.Instance;
        var state = r.CaptureState();
        if (!state.completed.Contains((int)t)) state.completed.Add((int)t);
        r.RestoreState(state);
        Check(r.Has(t), "granted " + t);
    }
    static T Build<T>(BuildingKind kind, Vector3 p) where T : BuildingBase
    {
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any).Invoke(null, new object[] { kind, UnitRole.Worker });
        Check(template != null, "template " + kind);
        NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas);
        var go = Object.Instantiate(template, hit.position + Vector3.up * template.transform.localScale.y * .5f, Quaternion.identity);
        go.name = kind.ToString(); go.SetActive(true);
        return go.GetComponent<T>();
    }
    static readonly System.Collections.Generic.HashSet<WildMonster> used = new System.Collections.Generic.HashSet<WildMonster>();
    static WildMonster Monster(Vector3 p)
    {
        var incursions = Object.FindAnyObjectByType<LocalIncursions>();
        if (!incursions.Visitors.Any(v => v != null && !v.IsDead && !used.Contains(v)))
        {
            foreach (var v in incursions.Visitors.ToArray()) if (v != null && !v.IsDead) v.TakeDamage(float.MaxValue);
            Check(incursions.TrySpawn(), "spawn test monsters");
        }
        var m = incursions.Visitors.First(v => v != null && !v.IsDead && !used.Contains(v));
        used.Add(m);
        NavMesh.SamplePosition(p, out var hit, 5, NavMesh.AllAreas);
        m.GetComponent<NavMeshAgent>().Warp(hit.position);
        return m;
    }
    public static async Task<string> Main()
    {
        checks = 0; used.Clear(); Check(Application.isPlaying, "Play required");
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage2Checks-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 250926, commanderDeath = CommanderDeathMode.Gentle });
            await Ready();
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            var incursionsComponent = Object.FindAnyObjectByType<LocalIncursions>();
            var rm = ResourceManager.Instance;
            foreach (ColonyResourceType type in Enum.GetValues(typeof(ColonyResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            AntPool.Instance.Breed(100);
            var roster = CommanderRoster.Instance;
            foreach (var c in roster.Commanders) c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced, 50));
            var a = roster.Commanders[0];
            var home = a.Position;
            var world = WorldMapManager.Instance;

            // --- 농사: 균류 재배 / 감로 목장 / 고급 작물 ---
            var farm = Build<BuildingBase>(BuildingKind.Farm, home + Vector3.left * 14);
            var node = farm.GetComponent<ResourceNode>();
            var plot = farm.gameObject.AddComponent<FarmPlot>();
            Check(ScienceEffects.FarmYieldMultiplier == 1 && !ScienceEffects.WideFarms, "fungal farming off");
            Grant(ScienceTechnology.FungalFarming);
            var widthBefore = farm.transform.localScale.x;
            plot.Configure(FarmCrop.Fungus, ScienceEffects.WideFarms);
            Check(plot.Wide && Mathf.Approximately(farm.transform.localScale.x, 2 * widthBefore), "fungal farming: new farm is 2 cells wide");
            var baseAmount = node.RegrowAmount;
            typeof(ResourceNode).GetMethod("RestoreState", Any).Invoke(node, new object[] { 0f, .01f });
            Time.timeScale = 1; await Task.Delay(250); Time.timeScale = 0;
            Check(Mathf.Abs(node.AmountRemaining - baseAmount * 1.2f) < .01f, "fungal farming +20% harvest");
            Check(!ScienceEffects.CropUnlocked(FarmCrop.Honeydew), "honeydew locked");
            Grant(ScienceTechnology.HoneydewRanch);
            plot.Configure(FarmCrop.Honeydew, true);
            Check(ScienceEffects.CropUnlocked(FarmCrop.Honeydew) && node.RegrowSeconds == 300 && node.RegrowAmount == 80 && plot.DroughtPenalty == .2f, "honeydew crop");
            Check(!ScienceEffects.CropUnlocked(FarmCrop.AdvancedFungus), "advanced crop locked");
            Grant(ScienceTechnology.AdvancedCrops);
            plot.Configure(FarmCrop.AdvancedFungus, true);
            Check(node.RegrowSeconds == 360 && node.RegrowAmount == 150, "advanced fungus crop");
            UnityEngine.Random.InitState(7);
            var sp = rm.GetAmount(ColonyResourceType.Special);
            var harvest = typeof(FarmPlot).GetMethod("OnHarvested", Any);
            for (var i = 0; i < 400; i++) harvest.Invoke(plot, null);
            var bonus = (rm.GetAmount(ColonyResourceType.Special) - sp) / 2;
            Check(bonus > 20 && bonus < 60, "advanced fungus 10% Special 2 on harvest: " + bonus);

            // --- 의료: 약초 / 방역 / 부위 재생 ---
            Check(ScienceEffects.MinorHealRate == 1 && ScienceEffects.SeriousTreatRate == 1, "herbs off");
            a.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Legs, severity = InjurySeverity.Minor, remaining = 180 });
            Grant(ScienceTechnology.Herbs);
            a.TickPersonal(60);
            Check(Mathf.Abs(a.PersonalState.injuries[0].remaining - 90) < .01f, "herbs: minor injury heals in 2 min");
            Check(Mathf.Abs(ScienceEffects.SeriousTreatRate * 180 - 240) < .01f, "herbs: serious treatment 3 min");
            a.PersonalState.injuries.Clear();
            Check(ScienceEffects.MoldSpreadMultiplier == 1 && ScienceEffects.MoldTreatSeconds == 60, "sanitation off");
            Grant(ScienceTechnology.Sanitation);
            Check(ScienceEffects.MoldSpreadMultiplier == .5f && ScienceEffects.MoldTreatSeconds == 30, "sanitation halves mold");
            Grant(ScienceTechnology.Infirmary);
            var infirmary = Build<Infirmary>(BuildingKind.Infirmary, home + Vector3.back * 10);
            var patient = roster.Commanders[1];
            Move(patient, infirmary.Position + Vector3.right * 3);
            patient.PersonalState.injuries.Add(new CommanderInjury { part = InjuryPart.Wings, severity = InjurySeverity.Permanent, remaining = 240 });
            Check(!infirmary.CanRegenerate(patient), "regeneration locked");
            Grant(ScienceTechnology.Regeneration);
            sp = rm.GetAmount(ColonyResourceType.Special);
            Check(infirmary.TryRegenerate(patient) && rm.GetAmount(ColonyResourceType.Special) == sp - 20 && patient.PersonalState.treating
                && patient.PersonalState.injuries[0].severity == InjurySeverity.Serious && patient.PersonalState.injuries[0].remaining == 480, "regeneration admits permanent injury");
            patient.TickPersonal(240);
            Check(Mathf.Abs(patient.PersonalState.injuries[0].remaining - 240) < .01f, "regeneration takes 8 min (no herbs speedup)");
            patient.TickPersonal(241);
            Check(patient.PersonalState.injuries.Count == 0 && !patient.PersonalState.treating, "regeneration cures permanent injury");

            // --- 저장: 압축 저장 ---
            var storages = Object.FindObjectsByType<Storage>(FindObjectsSortMode.None).Count(s => s.isActiveAndEnabled && s.Data != null);
            var capacity = rm.GetCapacity(ColonyResourceType.Food);
            Grant(ScienceTechnology.Fermentation);
            Check(storages > 0 && rm.GetCapacity(ColonyResourceType.Food) == capacity + 50 * storages, "fermentation: storage +100 -> +150");

            // --- 재해 대응(4단계 이벤트 수치): 치수 / 방화대 / 보온 ---
            Check(!ScienceEffects.FloodImmune && ScienceEffects.DroughtGrowthPenalty == .5f, "drainage off");
            Grant(ScienceTechnology.Drainage);
            Check(ScienceEffects.FloodImmune && ScienceEffects.DroughtGrowthPenalty == .2f, "drainage");
            Check(ScienceEffects.WildfireDamageMultiplier == 1 && !ScienceEffects.FirebreaksBlockSpread, "firebreaks off");
            Grant(ScienceTechnology.Firebreaks);
            Check(ScienceEffects.WildfireDamageMultiplier == .5f && ScienceEffects.FirebreaksBlockSpread, "firebreaks");
            Check(ScienceEffects.ColdEffectMultiplier == 1, "insulation off");
            Grant(ScienceTechnology.Insulation);
            Check(ScienceEffects.ColdEffectMultiplier == .5f, "insulation halves cold");

            // --- 제작 해금(3단계 공방이 사용) ---
            Check(!ScienceEffects.CanCraft(WeaponKind.Mandible) && !ScienceEffects.CanCraft(ArmorKind.Coating) && !ScienceEffects.CanCraftTrinkets
                && !ScienceEffects.CanCraft(WeaponKind.Shield) && ScienceEffects.QualityUpgradeChance == 0, "crafting locked");
            Grant(ScienceTechnology.Blades); Check(ScienceEffects.CanCraft(WeaponKind.Mandible) && ScienceEffects.CanCraft(WeaponKind.AcidSprayer), "blades unlock");
            Grant(ScienceTechnology.ArmorPlates); Check(ScienceEffects.CanCraft(ArmorKind.Coating), "armor plates unlock");
            Grant(ScienceTechnology.Trinkets); Check(ScienceEffects.CanCraftTrinkets, "trinkets unlock");
            Grant(ScienceTechnology.AdvancedWeapons);
            Check(ScienceEffects.CanCraft(WeaponKind.Shield) && ScienceEffects.CanCraft(WeaponKind.Pheromone) && ScienceEffects.QualityUpgradeChance == .1f, "advanced weapons");
            Grant(ScienceTechnology.Gliding);
            var flyingBarracks = Object.FindObjectsByType<Barracks>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && b.Role == UnitRole.Flying).ToArray();
            foreach (var b in flyingBarracks) b.gameObject.SetActive(false);
            Check(!ScienceEffects.CanCraft(ArmorKind.Wings), "wings need a wing training ground");
            var wingYard = Object.Instantiate((GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any)
                .Invoke(null, new object[] { BuildingKind.Barracks, UnitRole.Flying }), home + Vector3.forward * 30, Quaternion.identity);
            wingYard.SetActive(true);
            Check(ScienceEffects.CanCraft(ArmorKind.Wings), "gliding + wing training ground unlock wings");

            // --- 수송: 수송 효율 / 대형 수송 ---
            Check(world.CanCreateTransport(home, out var shipPoint), "transport spawn");
            var vehicle = world.CreateTransport(false, shipPoint);
            Check(TransportRoute.IntervalSeconds == 60 && vehicle.CargoCapacity == 200, "efficient transport off");
            Grant(ScienceTechnology.EfficientTransport);
            Check(TransportRoute.IntervalSeconds == 30 && vehicle.CargoCapacity == 300, "efficient transport: 30s route, cargo +50%");
            Check(vehicle.Capacity == 40 && vehicle.CommanderCapacity == 4, "heavy transport off");
            Grant(ScienceTechnology.HeavyTransport);
            Check(vehicle.Capacity == 60 && vehicle.CommanderCapacity == 6, "heavy transport: troops +50%, commanders +2");

            // --- 흙벽 ---
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.SoilWall), "soil wall locked");
            Grant(ScienceTechnology.Resin);
            var wall = Build<SoilWall>(BuildingKind.SoilWall, home + Vector3.right * 12);
            Check(ScienceEffects.BuildingUnlocked(BuildingKind.SoilWall) && wall.Data.soilCost == 25 && wall.Data.constructionAnts == 3
                && wall.Data.buildTimeSeconds == 4 && wall.MaxHealth == 400 && wall.Armor == 2, "soil wall spec");
            wall.TakeDamage(10);
            Check(wall.CurrentHealth == 392, "wall armor 2");
            var obstacle = wall.GetComponent<NavMeshObstacle>();
            Check(obstacle != null && obstacle.carving, "wall blocks passage");

            // --- 함정 구덩이 ---
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.TrapPit), "trap locked");
            Grant(ScienceTechnology.Traps);
            var trap = Build<TrapPit>(BuildingKind.TrapPit, home + Vector3.right * 20);
            Check(trap.Data.foodCost == 10 && trap.Data.soilCost == 30 && trap.Data.buildTimeSeconds == 5 && trap.GetComponent<NavMeshObstacle>() == null, "trap spec, walkable");
            var victim = Monster(trap.Position);
            trap.Tick(.1f);
            Check(!trap.Armed && victim.RootRemaining == 4, "trap roots ground enemy 4s");
            var repairer = roster.Commanders[2];
            Move(repairer, trap.Position + Vector3.right * 2);
            var soil = rm.GetAmount(ColonyResourceType.Soil);
            Check(trap.TryRepair(repairer) && rm.GetAmount(ColonyResourceType.Soil) == soil - 10, "trap repair costs Soil 10");
            Check(trap.TryRepair(repairer) && rm.GetAmount(ColonyResourceType.Soil) == soil - 10, "repair is paid once");
            trap.Tick(3); Check(!trap.Armed, "repair takes 4s");
            trap.Tick(1.1f); Check(trap.Armed, "trap repaired");

            // --- 개미산 정제: 분사탑 +25%, 범위형 분사탑, 방어시설 연구소 ---
            var tower = Build<AcidTower>(BuildingKind.AcidTower, home + Vector3.right * 28);
            var baseDamage = tower.Damage;
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.AreaAcidTower) && !ScienceEffects.BuildingUnlocked(BuildingKind.DefenseLab), "acid refining locked");
            Grant(ScienceTechnology.AcidRefining);
            Check(Mathf.Abs(tower.Damage - baseDamage * 1.25f) < .01f, "acid refining +25% tower damage");
            var area = Build<AreaAcidTower>(BuildingKind.AreaAcidTower, home + Vector3.right * 36);
            Check(area.MaxHealth == 220 && area.Range == 9 && Mathf.Abs(area.Damage - 10) < .01f && area.Data.foodCost == 40 && area.Data.soilCost == 70, "area tower spec");
            var m1 = Monster(area.Position + Vector3.forward * 6);
            var m2 = Monster(m1.Position + Vector3.right * 1.5f);
            var h1 = m1.CurrentHealth; var h2 = m2.CurrentHealth;
            area.Tick(.3f);
            Check((m1.IsDead || m1.CurrentHealth < h1) && (m2.IsDead || m2.CurrentHealth < h2), "area tower hits clustered ground enemies");

            // --- 방어시설 연구소 ---
            Check(!DefenseUpgrades.TryUpgrade(DefenseLine.Firepower), "defense upgrades need a defense lab");
            var defenseLab = Build<DefenseLab>(BuildingKind.DefenseLab, home + Vector3.back * 20);
            Check(defenseLab.Data.foodCost == 60 && defenseLab.Data.soilCost == 80 && defenseLab.Data.buildTimeSeconds == 12, "defense lab spec");
            var food = rm.GetAmount(ColonyResourceType.Food); soil = rm.GetAmount(ColonyResourceType.Soil); sp = rm.GetAmount(ColonyResourceType.Special);
            foreach (DefenseLine line in Enum.GetValues(typeof(DefenseLine))) for (var i = 0; i < 3; i++) Check(DefenseUpgrades.TryUpgrade(line), "upgrade " + line);
            Check(rm.GetAmount(ColonyResourceType.Food) == food - 4 * 30 * 6 && rm.GetAmount(ColonyResourceType.Soil) == soil - 4 * 40 * 6
                && rm.GetAmount(ColonyResourceType.Special) == sp - 4 * 5, "upgrade costs level x F30/S40, Special 5 at level 3");
            Check(!DefenseUpgrades.TryUpgrade(DefenseLine.Range), "max level 3");
            Check(Mathf.Abs(tower.Damage - baseDamage * 1.25f * 1.45f) < .01f && area.Range == 13.5f, "firepower +15%/level, range +1.5m/level");
            Check(Mathf.Abs(wall.MaxHealth - 400 * 1.6f) < .01f, "durability +20%/level includes wall");
            var victim2 = Monster(trap.Position);
            trap.Tick(.1f); Check(!trap.Armed, "trap sprung again");
            trap.Tick(59); Check(!trap.Armed, "auto repair waits 60s");
            trap.Tick(1.1f); Check(trap.Armed, "trap line level 3 auto repairs");

            // --- 자폭개미 매설지 ---
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.MineField), "mines locked");
            Grant(ScienceTechnology.Mines);
            var mine = Build<MineField>(BuildingKind.MineField, home + Vector3.left * 30);
            Check(mine.Data.specialCost == 2 && mine.Data.foodCost == 20 && mine.Data.soilCost == 20 && mine.GetComponent<NavMeshObstacle>() == null, "mine spec");
            var target = Monster(mine.Position);
            var near = Monster(mine.Position + Vector3.right * 3);
            var hn = near.CurrentHealth;
            mine.Tick();
            await Task.Delay(50);
            Check(mine == null, "mine consumed once");
            Check(target.IsDead || target.CurrentHealth <= 0, "mine kills trigger enemy");
            Check(near.IsDead || Mathf.Abs(near.CurrentHealth - (hn - 60 * 1.45f)) < .01f, "mine radius 4 damage 60 x firepower");
            for (var i = 0; i < 8; i++) Build<MineField>(BuildingKind.MineField, home + Vector3.left * 30 + Vector3.forward * (i * 2 + 4));
            Check(MineField.Count == 8, "eight mines placed");
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>(FindObjectsInactive.Include);

            var builder = roster.Commanders[3];
            Move(builder, home + Vector3.back * 4);
            if (!builder.HasTroops) builder.TryAssign(3);
            selection.ClearSelection();
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", Any).Invoke(selection, new object[] { builder.GetComponent<AntColony.Units.SelectableObject>() });
            Check(!placement.BeginSciencePlacement(BuildingKind.MineField), "ninth mine blocked");
            Check(placement.BeginSciencePlacement(BuildingKind.SoilWall), "other science building placement allowed: canBuild=" + builder.CanStartConstruction + " troops=" + builder.TroopCount + " selected=" + selection.GetSelectedObjects().Count + " viewed=" + (world.ViewedSite != null) + " unlocked=" + ScienceEffects.BuildingUnlocked(BuildingKind.SoilWall));
            placement.CancelPlacement();

            // --- 감시탑 ---
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.Watchtower), "watchtower locked");
            Grant(ScienceTechnology.Watchtowers);
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
            var crew = roster.Commanders[4];
            Move(crew, vehicle.Position + Vector3.right * 3);
            if (!crew.HasTroops) crew.TryAssign(3);
            Check(vehicle.TryBoard(new[] { crew }) && vehicle.TryDepart(site), "depart to settlement");
            vehicle.Tick(vehicle.TravelSeconds);
            foreach (var b in site.Colony.GetComponentsInChildren<BuildingBase>()) b.TakeDamage(float.MaxValue);
            foreach (var e in site.GetComponentsInChildren<WildMonster>()) e.TakeDamage(float.MaxValue);
            await Task.Delay(100);
            Check(site.TryResolveConquest(ConquestDisposition.Annexed), "annex settlement");
            Check(!Watchtower.Watches(site), "no watchtower, no watch");
            var diplomacy = DiplomacyManager.Instance; var invader = diplomacy.Data.civilizations[0]; diplomacy.DeclareWar(invader); invader.nextRaid = diplomacy.Data.elapsed + 59; site.Defense.Tick(2);
            Check(!site.Defense.Warned, "unwatched site gets no warning");
            var watch = Build<Watchtower>(BuildingKind.Watchtower, home + Vector3.back * 28);
            Check(watch.MaxHealth == 150 && watch.Data.soilCost == 40 && Watchtower.Watches(site), "watchtower spec and coverage");
            // 경보 창을 지나쳤으므로 이번 습격을 끝내 타이머를 새로 시작한다.
            site.Defense.TryStartRaid();
            Check(site.Defense.UnderAttack, "raid starts");
            foreach (var attacker in site.Defense.Attackers.ToArray()) attacker.TakeDamage(float.MaxValue);
            invader.nextRaid = diplomacy.Data.elapsed + 900; site.Defense.Tick(.1f);
            Check(!site.Defense.UnderAttack && site.Defense.Remaining > 61, "raid repelled, timer reset");
            invader.nextRaid = diplomacy.Data.elapsed + 61; site.Defense.Tick(1);
            Check(!site.Defense.UnderAttack && !site.Defense.Warned, "before warning window");
            diplomacy.Data.elapsed += 2; site.Defense.Tick(2);
            Check(site.Defense.Warned && site.Defense.Status.Contains("WATCHTOWER"), "watchtower warns 60s before raid with route");
            Check(vehicle.TryReturn(), "return home"); vehicle.Tick(vehicle.TravelSeconds);

            // --- 휴게실 ---
            Check(!ScienceEffects.BuildingUnlocked(BuildingKind.RestRoom), "rest room locked");
            Grant(ScienceTechnology.Recreation);
            var rest = Build<RestRoom>(BuildingKind.RestRoom, home + Vector3.forward * 12);
            Check(rest.Data.foodCost == 40 && rest.Data.soilCost == 60 && rest.Data.constructionAnts == 4, "rest room spec");
            var guests = roster.Commanders.Where(c => !c.IsAwayFromHome && !c.PersonalState.treating).Take(5).ToArray();
            for (var i = 0; i < guests.Length; i++) Move(guests[i], rest.Position + new Vector3(i - 2, 0, 3));
            rest.RefreshSeats();
            Check(rest.Seats.Count == 4, "rest room seats four");
            var guest = rest.Seats[0];
            guest.PersonalState.AddMood("Fatigue", -10, 30);
            guest.TickPersonal(10);
            var fatigue = guest.PersonalState.moodFactors.Find(f => f.reason == "Fatigue");
            Check(fatigue != null && Mathf.Abs(fatigue.remaining - 10) < .01f && guest.PersonalState.moodFactors.Exists(f => f.reason == "Rest" && f.value == 5), "rest: fatigue x2 recovery, Rest +5");

            // --- 저장/불러오기 ---
            trap.Tick(.1f);
            var sprung = Monster(trap.Position); trap.Tick(.1f);
            Check(!trap.Armed, "trap broken before save");
            foreach (var m in WildMonster.All.ToArray()) if (m != null && !m.IsDead && Vector3.Distance(m.Position, home) < 200) m.TakeDamage(float.MaxValue);
            await Task.Delay(100);
            typeof(BuildingBase).GetMethod("RestoreHealth", Any).Invoke(wall, new object[] { 600f });
            wall.TakeDamage(-1); // 음수 피해는 무시된다
            var wallHealth = wall.CurrentHealth;
            Check(wallHealth > 400, "wall health above base via durability");
            incursionsComponent.enabled = false;
            foreach (var c in roster.Commanders) c.CommandStop();
            Check(SaveSystem.TrySave(false, 0, out var error), "save accepted: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error); await Ready();
            var rTrap = Object.FindAnyObjectByType<TrapPit>();
            Check(rTrap != null && !rTrap.Armed, "trap state restored");
            Check(DefenseUpgrades.Level(DefenseLine.Durability) == 3 && DefenseUpgrades.TrapAutoRepair, "defense levels restored");
            var rWall = Object.FindAnyObjectByType<SoilWall>();
            Check(rWall != null && Mathf.Abs(rWall.CurrentHealth - wallHealth) < .01f, "upgraded wall health survives reload");
            var rPlot = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).FirstOrDefault();
            Check(rPlot != null && rPlot.Crop == FarmCrop.AdvancedFungus && rPlot.Wide, "farm crop and width restored");
            Check(Object.FindAnyObjectByType<AreaAcidTower>() != null && Object.FindAnyObjectByType<DefenseLab>() != null
                && Object.FindAnyObjectByType<Watchtower>() != null && Object.FindAnyObjectByType<RestRoom>() != null
                && MineField.Count == 8, "new facilities restored");
            Check(CampaignResearch.Instance.Has(ScienceTechnology.HeavyTransport), "research restored");
            Check(WorldMapManager.Instance.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement).Defense.Warned, "watchtower warning restored after towers");
            var bossTrap = Build<TrapPit>(BuildingKind.TrapPit, home + Vector3.back * 35);
            var bossObject = new GameObject("Stage2 boss target"); bossObject.transform.position = bossTrap.Position;
            var boss = bossObject.AddComponent<AntColony.Boss.BossHealth>();
            bossTrap.Tick(.1f);
            Check(!bossTrap.Armed && boss.RootRemaining == 1, "boss triggers trap for one second");
            var bossMine = Build<MineField>(BuildingKind.MineField, home + Vector3.back * 40);
            bossObject.transform.position = bossMine.Position; var bossHp = boss.CurrentHp; bossMine.Tick();
            Check(!bossMine.gameObject.activeSelf && Mathf.Abs(boss.CurrentHp - (bossHp - 60 * 1.45f)) < .01f, "boss triggers mine and receives blast");
            var restoredArea = Object.FindAnyObjectByType<AreaAcidTower>();
            bossObject.transform.position = restoredArea.Position + Vector3.forward * 4;
            bossHp = boss.CurrentHp; restoredArea.Tick(10);
            Check(boss.CurrentHp < bossHp, "area tower acquires boss");
            Object.Destroy(bossObject);
            return "PASS " + checks + " stage 2 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}

