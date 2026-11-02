using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Boss;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using ColonyResourceType = AntColony.Data.ResourceType;

// unity command run_script --file AgentScripts/CampaignChecks.cs (fresh Play mode).
// Uses disposable saves; never overwrites the player's slots.
public static class CampaignChecks
{
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(60);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready");
        Time.timeScale = 0;
    }
    static void Move(CommanderAnt c, Vector3 p)
    {
        c.CommandStop();
        Check(NavMesh.SamplePosition(p, out var hit, 10, NavMesh.AllAreas), "walkable test position");
        Check(c.Agent.Warp(hit.position), "commander warp");
    }
    static ScienceLab Lab(Vector3 p)
    {
        var template = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
        var lab = Object.Instantiate(template, p, Quaternion.identity); lab.name = "ScienceLab"; lab.gameObject.SetActive(true); return lab;
    }
    static void Complete(ScienceTechnology technology)
    {
        var r = CampaignResearch.Instance;
        Check(r.TryStart(technology), "start " + technology + ": " + r.BlockReason(technology));
        r.Tick(10000); Check(r.Has(technology), "complete " + technology);
    }
    static void Upgrade(ScienceLab lab)
    {
        var promotion = ScienceLabUpgrades.RequiredResearch(lab.Tier);
        if (!CampaignResearch.Instance.Has(promotion)) Complete(promotion);
        var tier = lab.Tier;
        Check(lab.TryUpgrade() && lab.Tier == tier + 1, "upgrade after promotion and material payment");
    }
    static void ReturnHome(ExpeditionTransport ship)
    {
        foreach (var c in ship.Crew) Move(c, ship.Position);
        Check(ship.TryReturn(), "return boss cargo");
        for (var i = 0; i < 20 && ship.IsWithdrawing; i++) ship.Tick(ExpeditionWithdraw.BoardSeconds * 6);
        Check(ship.State == ExpeditionState.Returning, "physical boarding before return");
        ship.Tick(ship.TravelRemainingSeconds + .01f);
        Check(ship.State == ExpeditionState.Home, "boss cargo reaches home");
    }
    static SaveFileV1 Clone(SaveFileV1 f) => JsonUtility.FromJson<SaveFileV1>(JsonUtility.ToJson(f));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required");
        var oldRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "CampaignChecks-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone();
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 240924, commanderDeath = CommanderDeathMode.Gentle });
            await Ready();
            Object.FindFirstObjectByType<UpkeepManager>().enabled = false;
            Object.FindFirstObjectByType<LocalIncursions>().enabled = false;
            WaveDefense.Ensure().enabled = false;
            var rm = ResourceManager.Instance;
            foreach (ColonyResourceType type in Enum.GetValues(typeof(ColonyResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            var roster = CommanderRoster.Instance;
            foreach (var c in roster.Commanders) c.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced));
            foreach (var c in roster.Commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); c.SetJobEnabled(CommanderJobs.Research, true); }
            var a = roster.Commanders[0]; var b = roster.Commanders[1];
            var inventory = EquipmentInventory.Instance;
            Check(CampaignResearch.Instance != null && inventory != null, "scene services bootstrapped");
            var food = rm.GetAmount(ColonyResourceType.Food); var soil = rm.GetAmount(ColonyResourceType.Soil);
            Check(!rm.TrySpend(1, 1, int.MaxValue) && rm.GetAmount(ColonyResourceType.Food) == food && rm.GetAmount(ColonyResourceType.Soil) == soil, "special spend atomic");
            Check(!rm.TrySpend(0, 0, -1), "negative special cost rejected");
            var charm = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Command };
            var replacement = new EquipmentItem { slot = EquipmentSlot.Trinket, effect = TrinketEffect.Move };
            var otherEquipment = a.PersonalState.equipment.Where(e => e.slot != EquipmentSlot.Trinket).Select(e => e.id).OrderBy(id => id).ToArray();
            inventory.Add(charm); inventory.Add(replacement);
            Check(inventory.Equip(a, charm), "command charm equipped");
            a.WorkState.duty = CommanderDuty.Deployed;
            // G04: 모병제 5% 상한도 만족시켜 지휘 장신구 한도를 독립적으로 검사한다.
            AntPool.Instance.Breed(Mathf.Max(100, a.CommandLimit * 20));
            Check(ColonyPopulation.Instance.AvailableMilitary(false) >= a.FreeRanks, "지휘 한도 검사에 필요한 병역 인원 확보");
            Check(a.TryAssign(a.FreeRanks), "fill expanded command limit");
            var troopCount = a.TroopCount;
            Check(inventory.Equip(a, replacement) && a.TroopCount == troopCount && a.TroopCount > a.CommandLimit && !a.TryAssign(1), "capacity loss preserves troops, prevents assignment");
            Check(a.PersonalState.equipment.Count(e => e.slot == EquipmentSlot.Trinket) == 1
                && a.PersonalState.equipment.Contains(replacement) && inventory.Items.Count(e => e.id == charm.id) == 1
                && a.PersonalState.equipment.Where(e => e.slot != EquipmentSlot.Trinket).Select(e => e.id).OrderBy(id => id).SequenceEqual(otherEquipment),
                "old charm returned once while starting equipment remains");
            var personal = a.CapturePersonalState();
            MedicalRules.AddWound(personal.health, BodyPart.LegL, WoundKind.Bruise, 1); // 2026-10-15: 옛 injuries → 부위별 건강
            a.RestorePersonalState(personal); personal.health.wounds.Clear();
            Check(a.Health.wounds.Count == 1, "personal restore deep copy");
            a.Health.wounds.Clear();
            Check(a.TryReward() && a.PersonalState.moodFactors.Exists(f => f.reason == "포상" && f.value == 8), "reward mood applies once after equipment events");
            Check(!a.TryReward(), "monthly reward cooldown");
            a.StartMentalBreak(MentalBreak.Idle); Check(!a.CanReceiveOrders && !a.TryAssign(1), "mental break blocks commands/allocation");
            a.TickPersonal(61); Check(a.CanReceiveOrders && a.PersonalState.moodFactors.Any(f => f.reason == "Catharsis"), "mental break recovery");
            b.TakeDamage(float.MaxValue); Check(!b.IsDead && b.TroopCount == 0 && b.Health.Injured, "gentle downing applies injury without death");
            b.RestorePersonalState(new CommanderPersonalState()); Check(b.CanReceiveOrders, "downed commander recovered");
            a.ReturnTroops(a.TroopCount); a.WorkState.duty = CommanderDuty.Civilian;
            var lab = Lab(a.Position + Vector3.forward * 4);
            Check(lab.TryAssign(a) && a.ScienceAssignment == lab, "scientist ownership");
            Check(!a.CanChangeAllocation && !a.CanStartConstruction, "researcher cannot double-book");
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle), "tier wall");
            Check(!lab.TryUpgrade(), "lab upgrade needs promotion research");
            Upgrade(lab);
            var lab2 = Lab(b.Position + Vector3.forward * 4);
            Upgrade(lab2); Check(lab2.TryAssign(b), "second researcher");
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle), "shared vehicle research");
            // 2026-10-15 V5: 연구 변경 허용(진행 보존), 활성 연구는 항상 하나.
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Resin) && CampaignResearch.Instance.TryStart(ScienceTechnology.Vehicle)
                && CampaignResearch.Instance.Active.Technology == ScienceTechnology.Vehicle, "switch keeps one active research");
            var researchWork = 10 * (CampaignResearch.Instance.LabRate(lab) + CampaignResearch.Instance.LabRate(lab2));
            Check(CampaignResearch.Instance.LabRate(lab) > 0 && CampaignResearch.Instance.LabRate(lab2) > 0, "both researchers actively contribute");
            CampaignResearch.Instance.Tick(10); Check(Mathf.Abs(CampaignResearch.Instance.Progress - researchWork) < .01f, "two labs contribute by skill and tier");
            lab2.gameObject.SetActive(false); Check(b.ScienceAssignment == null, "destroyed/disabled lab releases researcher");
            CampaignResearch.Instance.Tick(10000); Check(WorldMapManager.Instance.VehicleResearched, "vehicle unlock applied");
            Upgrade(lab); Upgrade(lab); Check(lab.Tier == 4, "tier 4 (electric)"); Complete(ScienceTechnology.Gliding); Complete(ScienceTechnology.Aircraft);
            Upgrade(lab); Upgrade(lab); Check(lab.Tier == 6 && !lab.TryUpgrade(), "tier 6 (future) is max"); Complete(ScienceTechnology.MigrationTheory); Complete(ScienceTechnology.Hull); Complete(ScienceTechnology.Cocoons);
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Engine), "engine requires world blueprint");
            lab.ReleaseResearcher();
            var world = WorldMapManager.Instance;
            Check(world.CanCreateTransport(a.Position, out var shipPoint), "transport spawn point");
            var ship = world.CreateTransport(false, shipPoint);
            a.WorkState.duty = CommanderDuty.Deployed; Check(a.TryAssign(2), "expedition mobilization");
            Move(a, ship.Position + Vector3.right * 3); Check(ship.TryBoard(new[] { a }), "board expedition");
            var site = world.Sites.First(s => s.GetComponent<BossStronghold>()?.Id == BossId.A);
            Check(ship.TryDepart(site), "depart boss expedition"); ship.Tick(ship.TravelRemainingSeconds + 1);
            Move(a, ship.Position + Vector3.right * 3);
            Check(!ship.TryCollectRewards(), "live boss gives no rewards");
            site.Boss.TakeDamage(float.MaxValue); await Task.Delay(80);
            Check(site.Cleared && ship.TryCollectRewards(), "boss reward loaded");
            var parcels = CampaignResearch.Instance.BossProgress;
            Check(parcels.Obtained(BossId.A) && !parcels.Delivered(BossId.A)
                && BossRewardTransfer.CargoLoad(ship) == 2 && !CampaignResearch.Instance.HasBlueprint,
                "A core remains local cargo and is not a D blueprint");
            Check(!ship.TryCollectRewards(), "reward cannot duplicate");
            var inventoryBeforeDelivery = inventory.Items.Count;
            ReturnHome(ship);
            Check(parcels.Delivered(BossId.A) && parcels.Home(BossId.A) == 2 && BossRewardTransfer.CargoLoad(ship) == 0
                && inventory.Items.Count == inventoryBeforeDelivery + 1 && !CampaignResearch.Instance.HasBlueprint,
                "A cores and unique armor delivered once only at home");
            Check(!CampaignResearch.Instance.TryStart(ScienceTechnology.Engine), "A reward cannot unlock D engine research");
            // D 전투 자체는 이 검사의 범위 밖이다. 유효한 D 회수 화물을 만들어 실제 반입 API로 해금한다.
            var dFixture = CampaignResearch.Instance.CaptureState(); dFixture.bosses.revealed[(int)BossId.D] = true;
            CampaignResearch.Instance.RestoreState(dFixture); parcels = CampaignResearch.Instance.BossProgress;
            Check(parcels.RecordKill(BossId.D, GameCalendar.GameSeconds) && parcels.TryAcquire(BossId.D, "checks:D", out var dParcel), "D reward fixture acquired");
            var dCargo = parcels.Cargo.Single(p => p.boss == (int)BossId.D);
            Check(CommanderAnt.DeliverBossEquipment(dCargo.id, world.HomePosition) && CampaignResearch.Instance.HasBlueprint, "D core home delivery unlocks blueprint");
            a.ReturnTroops(a.TroopCount); a.WorkState.duty = CommanderDuty.Civilian;
            Move(a, lab.Position + Vector3.right * 3); Check(lab.TryAssign(a), "researcher reassigned"); Complete(ScienceTechnology.Engine);
            lab.ReleaseResearcher();
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.AirshipYard, UnitRole.Worker });
            var yard = Object.Instantiate(template, world.HomePosition + Vector3.left * 12, Quaternion.identity).GetComponent<AirshipYard>();
            yard.name = "AirshipYard"; yard.gameObject.SetActive(true);
            Check(yard.TryBuild(AirshipPart.Hull), "hull construction starts"); yard.Tick(301);
            Check(!yard.TryDepart(), "engine required to depart");
            Check(yard.TryBuild(AirshipPart.Engine), "engine construction starts"); yard.Tick(301);
            Check(yard.Ready && yard.Cocoons == 0 && !yard.TryDepart(), "assembled empty rocket cannot launch");
            Check(yard.TryBuild(AirshipPart.Cocoon), "cocoon construction starts"); yard.Tick(61);
            Check(!yard.TryBoard(b), "rocket boarding waits for preparation");
            Check(yard.TryLoadFuel(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay), "load activation fuel");
            var economy = RocketCampaign.Instance.Economy; economy.Observe(GameCalendar.GameSeconds, 1, 3);
            Check(yard.TryActivate(1, 3) && yard.Phase == RocketPhase.Preparing, "activate with delivered D core");
            // 15일 공세 실플레이 대신 준비 종료 직전의 저장 상태를 사용한다. 발사·승객 왕복 검사는 유지한다.
            var prepared = yard.CaptureState(roster.Commanders); prepared.preparationRemaining = .01f;
            yard.RestoreState(prepared, roster.Commanders); yard.Tick(.02f);
            Check(yard.Phase == RocketPhase.Boarding, "preparation completion enables boarding");
            Move(b, yard.Position + Vector3.right * 4); Check(yard.TryBoard(b), "airship passenger boarded");
            Check(!b.CanChangeAllocation && !yard.TryBoard(b), "embarked passenger protected");
            Check(lab.TryAssign(a), "researcher retained for save");
            Check(CampaignResearch.Instance.TryStart(ScienceTechnology.Herbs), "partial research saved"); CampaignResearch.Instance.Tick(7);
            foreach (var c in roster.Commanders) if (c != a) c.CommandStop(); // CommandStop은 연구 배정도 해제하므로 연구자는 제외
            // 6단계: 비행선 건조 시작 시 과학자·적대 문명 침공이 올 수 있다. 저장 전 정리한다.
            foreach (var raider in Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(raider.DiplomaticFactionId)) raider.TakeDamage(float.MaxValue);
            await Task.Delay(100);
            SimulationPause.UserSpeed(0);
            var expected = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(expected, out var error), "valid capture: " + error);
            var broken = Clone(expected); broken.commanders[0].personalState.moodFactors.Add(new MoodFactor { value = float.NaN, reason = "invalid" });
            Check(!SaveValidator.Validate(broken, out _), "NaN mood rejected");
            broken = Clone(expected); broken.equipmentInventory.Add(broken.equipmentInventory[0]);
            Check(!SaveValidator.Validate(broken, out _), "duplicate equipment rejected");
            broken = Clone(expected); broken.buildings.First(x => x.kind == "AirshipYard").airship.passengers.Add(0);
            Check(!SaveValidator.Validate(broken, out _), "overcapacity/double assignment rejected");
            Check(SaveSystem.TrySave(false, 0, out error), "save accepted: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load accepted: " + error); await Ready();
            WaveDefense.Ensure().enabled = false;
            var actual = SaveSnapshot.Capture();
            Check(actual.campaign.active == expected.campaign.active && Mathf.Abs(actual.campaign.progress - expected.campaign.progress) < 2, "research survives reload");
            Check(actual.campaign.blueprint && actual.campaign.completed.Count == expected.campaign.completed.Count, "research and blueprint retained");
            var restoredLab = Object.FindObjectsByType<ScienceLab>().First(x => x.Target != null);
            Check(restoredLab.Tier == 6 && restoredLab.Target.ScienceAssignment == restoredLab, "lab tier and assignment restored");
            var restoredYard = Object.FindFirstObjectByType<AirshipYard>();
            Check(restoredYard.Ready && restoredYard.Cocoons == 1 && restoredYard.Passengers.Count == 1 && restoredYard.Passengers[0].IsEmbarked, "airship state restored");
            Check(EquipmentInventory.Instance.Items.Count == expected.equipmentInventory.Count, "equipment retained");
            GameMenuController.Instance.Science(); GameMenuController.Instance.ScienceFacilities(); // 2026-10-15: 과학 = 연구 화면, 로켓은 시설 화면
            Check(GameMenuController.Instance.ScreenName == "로켓 · 과학 시설", "science UI reachable");
            Check(restoredYard.TryDepart(), "departure succeeds");
            var launchDeadline = DateTime.UtcNow.AddSeconds(5);
            while (!CampaignResearch.Instance.Departed && DateTime.UtcNow < launchDeadline) await Task.Delay(30);
            Check(CampaignResearch.Instance.Departed, "launch resolves at frame end");
            Check(CampaignResearch.Instance.Passengers.Count == 1 && CampaignResearch.Instance.LeftBehind.Count == CommanderRoster.Instance.Commanders.Count - 1, "ending manifests");
            Check(Time.timeScale == 0 && GameMenuController.Instance.ScreenName.Contains("VICTORY"), "ending pauses simulation");
            GameMenuController.Instance.Resume(); Check(Time.timeScale == 0, "cannot resume completed campaign");
            Check(!restoredYard.TryDepart(), "departure is one-shot");
            return "PASS " + checks + " campaign checks; research, rewards, equipment, injuries, save/reload, airship ending";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = oldRoot; UserSettings.Apply(settings, false); }
    }
}
