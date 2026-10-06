using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 메모리 전용 회귀 검사: 저장 슬롯·파일 I/O 없이 검증한다. Play Mode에서 실행.
public static class CommitRangeReviewChecks
{
    static readonly List<GameObject> made = new List<GameObject>();
    static readonly List<string> results = new List<string>();
    static void Check(bool value, string label) => results.Add((value ? "PASS " : "FAIL ") + label);
    static GameObject Template(BuildingKind kind) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { kind, UnitRole.Worker });
    static T Put<T>(BuildingKind kind, Vector3 position) where T : Component
    {
        var go = Object.Instantiate(Template(kind), position, Quaternion.identity);
        go.name = "Review " + kind; go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    static async Task Ready()
    {
        for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("scene not ready");
        Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play Mode required");
        results.Clear(); made.Clear();
        var oldScale = Time.timeScale;
        try
        {
            await Ready();
            SaveSystem.NewGame(new NewGameOptions { seed = 261006, mapSize = MapSize.Small }); await Ready();
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var armory = Put<Armory>(BuildingKind.Armory, home + new Vector3(15, 0, 15));
            var inventory = EquipmentInventory.Instance;
            var added = true;
            for (int i = inventory.Items.Count; i < 40; i++) added &= inventory.Add(new EquipmentItem());
            Check(added && inventory.Items.Count == 40, "fill all 40 equipment slots");
            var file = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(file, out var error), "40 items with armory: " + error);
            Object.DestroyImmediate(armory.gameObject);
            Check(inventory.Items.Count == 40 && inventory.Full, "armory loss preserves items and blocks additions");
            file = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(file, out error), "40 items after armory loss: " + error);
            Check(SaveValidator.TryParse(JsonUtility.ToJson(file), out var parsed, out error)
                && parsed.equipmentInventory.Count == 40, "overflow inventory JSON roundtrip: " + error);
            file.equipmentInventory.Add(file.equipmentInventory[0]);
            Check(!SaveValidator.Validate(file, out error), "overflow still rejects duplicate equipment IDs");
            inventory.Items.Clear();

            var battery = Put<PowerNode>(BuildingKind.Battery, home + new Vector3(20, 0, 20));
            file = SaveSnapshot.Capture();
            file.buildings.First(b => b.kind == "Battery").powerCharge = float.NaN;
            Check(!SaveValidator.Validate(file, out error), "reject NaN battery charge");
            foreach (var invalid in new[] { float.PositiveInfinity, -1f, GameBalance.BatteryCapacity + 1 })
            {
                file.buildings.First(b => b.kind == "Battery").powerCharge = invalid;
                Check(!SaveValidator.Validate(file, out error), "reject invalid battery charge " + invalid);
            }
            file.buildings.First(b => b.kind == "Battery").powerCharge = GameBalance.BatteryCapacity;
            Check(SaveValidator.Validate(file, out error), "accept full battery: " + error);
            Object.DestroyImmediate(battery.gameObject);

            // 전선 연결·절단·재연결과 두 배터리의 순차 방전. 화면/파일 캡처 없음.
            var origin = new Vector3(Mathf.Floor(home.x) + 40.5f, home.y, Mathf.Floor(home.z) + 40.5f);
            var b1 = Put<PowerNode>(BuildingKind.Battery, origin);
            var wire = Put<PowerNode>(BuildingKind.PowerWire, origin + Vector3.right);
            var lamp = Put<PowerNode>(BuildingKind.ElectricLamp, origin + Vector3.right * 2);
            var b2 = Put<PowerNode>(BuildingKind.Battery, origin + Vector3.left);
            var charge = typeof(PowerNode).GetProperty("Charge");
            charge.SetValue(b1, GameBalance.LampWatts / 2); charge.SetValue(b2, GameBalance.LampWatts);
            PowerGrid.Tick(1);
            Check(lamp.Powered && b1.Charge == 0 && b2.Charge == GameBalance.LampWatts / 2, "two batteries supply one demand without negative charge");
            wire.gameObject.SetActive(false); PowerGrid.Tick(.1f);
            Check(!lamp.Powered && !PowerGrid.NetworkOf(lamp).Contains(b1), "disabled wire splits network");
            wire.gameObject.SetActive(true); PowerGrid.Tick(.1f);
            Check(lamp.Powered && PowerGrid.NetworkOf(lamp).Contains(b1), "reenabled wire merges network");
            PowerGrid.Tick(1);
            Check(!lamp.Powered && b2.Charge >= 0, "insufficient charge turns consumers off");
            foreach (var node in new[] { b1, wire, lamp, b2 }) Object.DestroyImmediate(node.gameObject);

            var lab = Put<ScienceLab>(BuildingKind.ScienceLab, home + new Vector3(25, 0, 25));
            foreach (var tier in new[] { 3, 4 })
            {
                lab.RestoreAssignment(tier, null); file = SaveSnapshot.Capture(); file.version = 13;
                file.campaign.active = (int)ScienceTechnology.FungalFarming; file.campaign.progress = 250;
                foreach (var c in file.commanders) c.personalState.joy.boredom = new[] { .25f, .75f };
                Check(SaveValidator.Validate(file, out error), "v13 validation " + tier + ": " + error);
                Check(file.buildings.First(b => b.kind == "ScienceLab").scienceTier == (tier == 3 ? 4 : 6), "v13 lab tier " + tier);
                Check(file.campaign.progress > 199 && file.campaign.progress < 200, "research clamp " + tier);
                Check(file.commanders.All(c => c.personalState.joy.boredom.SequenceEqual(new[] { .25f, .75f }.Concat(new float[RecreationSpot.KindCount - 2]))), "boredom values preserved " + tier);
            }
            file = SaveSnapshot.Capture(); file.version = 13;
            var legacyJson = System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(file), ",\"hygiene\":\\{[^}]*\\}", "");
            Check(!legacyJson.Contains("\"hygiene\""), "legacy fixture omits hygiene field");
            Check(SaveValidator.TryParse(legacyJson, out parsed, out error)
                && parsed.commanders.All(c => c.personalState.hygiene.hygiene == 100), "v13 absent hygiene defaults to 100: " + error);
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var builder = CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction);
            Check(placement.BeginPlacement(BuildingKind.MushroomFarm, UnitRole.Worker, builder), "begin mushroom farm");
            placement.CancelPlacement();
            Check(placement.BeginPlacement(BuildingKind.Farm, UnitRole.Worker, builder), "begin regular farm");
            Check(typeof(BuildingPlacementController).GetField("pendingCrop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(placement) == null, "regular farm clears crop override");
            placement.CancelPlacement();

            var door = Put<Door>(BuildingKind.LockedDoor, home + new Vector3(30.5f, .75f, 30.5f));
            typeof(Door).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(door, null);
            var obstacle = door.GetComponent<NavMeshObstacle>(); var box = door.GetComponent<BoxCollider>();
            results.Add("INFO door obstacle " + obstacle.shape + " center=" + obstacle.center + " size=" + obstacle.size + "; collider center=" + box.center + " size=" + box.size);
            Check(obstacle.shape == NavMeshObstacleShape.Box && obstacle.center == box.center && obstacle.size == box.size, "lock obstacle matches door collider");

            var bed = Put<Dormitory>(BuildingKind.SingleBed, home + new Vector3(35, 0, 35));
            var room = new Room { PrisonDoor = true }; room.Furniture.Add(bed);
            var judge = typeof(RoomSystem).GetMethod("Judge", BindingFlags.Static | BindingFlags.NonPublic);
            Check((RoomKind)judge.Invoke(null, new object[] { room }) == RoomKind.PrivateRoom, "bed alone behind prison door stays private room");
            var camp = Put<PrisonerCamp>(BuildingKind.PrisonerCamp, home + new Vector3(38, 0, 35));
            room.Furniture.Add(camp.GetComponent<BuildingBase>());
            Check((RoomKind)judge.Invoke(null, new object[] { room }) == RoomKind.Prison, "bed and prisoner camp share prison room");
            room.Furniture.Remove(camp.GetComponent<BuildingBase>());
            room.PrisonDoor = false;
            Check((RoomKind)judge.Invoke(null, new object[] { room }) == RoomKind.PrivateRoom, "ordinary door keeps private room");
            var bare = new Room(); var floored = new Room { Floors = 1 };
            Check(Math.Abs((float)floored.Score - bare.Score - .5f) < .001f, "one floor adds half a room point");
            foreach (var kind in new[] { BuildingKind.Hearth, BuildingKind.SleepingMat, BuildingKind.DoubleBed, BuildingKind.SingleBed, BuildingKind.FoodStore, BuildingKind.Jar }) Template(kind);
            foreach (var kind in new[] { BuildingKind.Kitchen, BuildingKind.Dormitory, BuildingKind.Storage })
                Check(Template(kind).GetComponent<BuildingBase>().Data.kind == kind, "base template after variants " + kind);
            var yard = Put<AirshipYard>(BuildingKind.AirshipYard, home + new Vector3(45, 0, 45));
            Check(!yard.TryDepart(), "unfinished rocket cannot launch");
            yard.RestoreState(new AirshipYard.State { hull = true, engine = true }, CommanderRoster.Instance.Commanders.ToList());
            Check(yard.TryDepart() && CampaignResearch.Instance.Departed && Time.timeScale == 0, "completed rocket launches and ends simulation without passengers");
            Check(!yard.TryDepart(), "rocket launch is one shot");
            if (results.Any(r => r.StartsWith("FAIL "))) throw new Exception(string.Join("\n", results));
            return string.Join("\n", results);
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.DestroyImmediate(go);
            Time.timeScale = oldScale;
        }
    }
}
