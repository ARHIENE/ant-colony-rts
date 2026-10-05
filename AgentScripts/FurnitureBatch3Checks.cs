using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-05 가구 3차: 1인 침대·책장·목욕통·버섯밭·축사 + 휴게 시설 저장 종류 수정.
public static class FurnitureBatch3Checks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    static async Task Remove(Component c) { made.Remove(c.gameObject); Object.Destroy(c.gameObject); await Task.Yield(); await Task.Yield(); }

    public static async Task<string> Main()
    {
        checks = 0; made.Clear(); Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Furniture3-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261007, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }

            // 1. 템플릿·잠금
            foreach (var k in new[] { BuildingKind.SingleBed, BuildingKind.Bookshelf, BuildingKind.Bathtub })
                Check(Template(k) != null && Template(k).GetComponent<BuildingBase>().Data.kind == k, "template " + k);
            Check(Template(BuildingKind.MushroomFarm) == Template(BuildingKind.Farm) && Template(BuildingKind.AphidPen) == Template(BuildingKind.Farm), "farm variants use farm template");
            Check(BuildingPlacementController.LockReason(BuildingKind.Bookshelf) != null && BuildingPlacementController.LockReason(BuildingKind.Bathtub) != null, "bookshelf/bathtub need Recreation research");
            Check(BuildingPlacementController.LockReason(BuildingKind.AphidPen) != null && BuildingPlacementController.LockReason(BuildingKind.MushroomFarm) == null, "aphid pen needs honeydew ranch");

            // 2. 버섯밭 배치 = 균류 고정 밭
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var builder = CommanderRoster.Instance.Commanders.First(c => c.CanStartConstruction);
            Check(placement.BeginPlacement(BuildingKind.MushroomFarm, UnitRole.Worker, builder) && placement.PendingKind == BuildingKind.Farm
                && (FarmCrop?)typeof(BuildingPlacementController).GetField("pendingCrop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(placement) == FarmCrop.Fungus, "mushroom farm places a fungus farm");
            placement.CancelPlacement();

            // 3. 방: 책장 = 도서관, 목욕통 = 목욕탕, 1인 침대 1개 = 개인실 / 2개 = 숙소
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            var shelf = Put<RecreationSpot>(BuildingKind.Bookshelf, o + new Vector3(2.5f, 1, 4.2f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Library && shelf.Seats == 1, "bookshelf makes library");
            await Remove(shelf);
            var tub = Put<RecreationSpot>(BuildingKind.Bathtub, o + new Vector3(2.5f, .4f, 2.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Bathhouse, "bathtub makes bathhouse");
            await Remove(tub);
            var bed1 = Put<Dormitory>(BuildingKind.SingleBed, o + new Vector3(1.5f, .25f, 2)); await Task.Yield();
            Check(bed1.Beds == 1 && !bed1.IsMat && RoomSystem.RoomAt(inside).Kind == RoomKind.PrivateRoom, "one single bed = private room");
            var bed2 = Put<Dormitory>(BuildingKind.SingleBed, o + new Vector3(3.5f, .25f, 2)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Bedroom, "two single beds = bedroom");

            // 4. 이용 효과: 책장 = 연구 경험치, 목욕통 = 위생
            var c = CommanderRoster.Instance.Commanders.First(x => x.CivilianWorkReady && x.CanReceiveOrders && x != builder);
            foreach (var other in CommanderRoster.Instance.Commanders) if (other != c) other.gameObject.SetActive(false);
            c.CommandStop(); c.PersonalState.meal.satiety = 100; c.PersonalState.hygiene.hygiene = 50;
            var book = Put<RecreationSpot>(BuildingKind.Bookshelf, c.Position + Vector3.right * 2);
            c.PersonalState.joy.joy = 10; float Xp() => c.Talents.Level(CommanderActivity.Research) * 100000f + c.Talents.Xp(CommanderActivity.Research);
            var xp = Xp(); c.TickDuty(.1f);
            Check(c.IsPlaying && c.PlaySpot == book, "reads at bookshelf");
            c.TickDuty(GameBalance.PlaySeconds);
            Check(!c.IsPlaying && Xp() > xp, "bookshelf gives research xp");
            await Remove(book);
            var bath = Put<RecreationSpot>(BuildingKind.Bathtub, c.Position + Vector3.right * 2);
            c.PersonalState.joy.joy = 10; c.PersonalState.joy.retrySeconds = 0; var hygiene = c.Hygiene; c.TickDuty(.1f);
            Check(c.IsPlaying && c.PlaySpot == bath, "bathes in tub");
            c.TickDuty(GameBalance.PlaySeconds);
            Check(c.Hygiene > hygiene + 30, "bathtub restores hygiene");
            Put<RecreationSpot>(BuildingKind.Campfire, c.Position + Vector3.left * 6);

            // 5. 저장: 휴게 시설 종류 이름(예전 'RecreationSpot' 버그), 이전 저장의 질림 배열 2칸 → 4칸
            var file = SaveSnapshot.Capture();
            Check(file.buildings.Any(b => b.kind == "Bathtub") && file.buildings.Any(b => b.kind == "Campfire") && !file.buildings.Any(b => b.kind == "RecreationSpot"), "recreation spots saved by kind");
            foreach (var cd in file.commanders) cd.personalState.joy.boredom = new float[2];
            Check(SaveValidator.Validate(file, out var error) && file.commanders.All(cd => cd.personalState.joy.boredom.Length == RecreationSpot.KindCount), "old boredom arrays padded: " + error);
            Check(SaveSystem.TrySave(false, 0, out error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(RecreationSpot.All.Count(s => s.KindIndex == 3) == 1 && RecreationSpot.All.Count(s => s.KindIndex == 0) == 1, "bathtub and campfire restored");
            Check(Object.FindObjectsByType<Dormitory>().Count(d => d.Data.kind == BuildingKind.SingleBed) == 2, "single beds restored");
            made.Clear();
            return "PASS " + checks + " furniture batch 3 checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
