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

// 2026-10-05 가구 2차: 화덕(조리대)·자리·큰침대·바닥·잠금문·창살문.
public static class FurnitureBatch2Checks
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
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Furniture2-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261006, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }

            // 1. 템플릿: 같은 컴포넌트 변형이 섞이지 않는다.
            foreach (var k in new[] { BuildingKind.Hearth, BuildingKind.SleepingMat, BuildingKind.DoubleBed, BuildingKind.Floor, BuildingKind.LockedDoor, BuildingKind.BarredDoor })
                Check(Template(k) != null && Template(k).GetComponent<BuildingBase>().Data.kind == k, "template " + k);
            Check(Template(BuildingKind.Kitchen).GetComponent<BuildingBase>().Data.kind == BuildingKind.Kitchen
                && Template(BuildingKind.Dormitory).GetComponent<BuildingBase>().Data.kind == BuildingKind.Dormitory, "dining/dorm templates not replaced by variants");

            // 2. 방: 창살문으로 막은 4×4 방
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.BarredDoor, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            Check(RoomSystem.RoomAt(inside) != null && RoomSystem.RoomAt(inside).PrisonDoor, "barred door marks prison door");
            var hearth = Put<Kitchen>(BuildingKind.Hearth, o + new Vector3(2.5f, .6f, 3.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Kitchen, "hearth makes kitchen");
            var table = Put<Kitchen>(BuildingKind.Kitchen, o + new Vector3(3.5f, .75f, 2.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.DiningKitchen, "hearth + table = dining kitchen");
            await Remove(table);
            var cook = CommanderRoster.Instance.Commanders.First();
            Check(hearth.NeedsCook && hearth.Work(cook, Kitchen.CookSeconds * 10) && hearth.Meals.meals.Count == 1, "hearth cooks and stores meals");
            await Remove(hearth);
            var bigBed = Put<Dormitory>(BuildingKind.DoubleBed, o + new Vector3(2, .3f, 2)); await Task.Yield();
            Check(bigBed.Beds == 2 && RoomSystem.RoomAt(inside).Kind == RoomKind.PrivateRoom, "double bed alone = private room");
            var mat = Put<Dormitory>(BuildingKind.SleepingMat, o + new Vector3(4.5f, .05f, 4)); await Task.Yield();
            Check(mat.Beds == 1 && mat.IsMat && RoomSystem.RoomAt(inside).Kind == RoomKind.Bedroom, "two beds = bedroom");
            await Remove(bigBed); await Remove(mat);
            var camp = Put<PrisonerCamp>(BuildingKind.PrisonerCamp, o + new Vector3(2.5f, .5f, 2.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Prison, "prisoner camp + barred door = prison");
            await Remove(camp);

            // 3. 바닥: 이동·방 점수·청결
            var before = RoomSystem.RoomAt(inside).Score;
            for (var i = 1; i <= 4; i++) Put<FloorTile>(BuildingKind.Floor, o + new Vector3(i + .5f, .02f, 1.5f));
            await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Floors == 4 && RoomSystem.RoomAt(inside).Score == before + 2, "floors raise room score");
            Check(FloorTile.At(o + new Vector3(2.5f, 0, 1.5f)) && !FloorTile.At(o + new Vector3(2.5f, 0, 3.5f)), "floor cells tracked");

            // 4. 잠금문: 밤에만 잠긴다.
            var locked = Put<Door>(BuildingKind.LockedDoor, home + new Vector3(30.5f, .75f, 30.5f));
            var session = GameSession.Instance; var seconds = session.GameSeconds; var play = session.PlaySeconds;
            session.MarkStarted(play, Mathf.Floor(seconds / GameCalendar.SecondsPerDay) * GameCalendar.SecondsPerDay + GameCalendar.DaySeconds + 1);
            typeof(Door).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(locked, null);
            Check(GameCalendar.IsNight && locked.Locked, "locked door closes at night");
            session.MarkStarted(play, Mathf.Floor(seconds / GameCalendar.SecondsPerDay) * GameCalendar.SecondsPerDay + 1);
            typeof(Door).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(locked, null);
            Check(!GameCalendar.IsNight && !locked.Locked, "locked door opens by day");

            // 5. 배정: 미혼 장수는 큰침대보다 다른 침대를 먼저
            var single = CommanderRoster.Instance.Commanders.First(c => !c.PersonalState.relations.Exists(r => r.spouse) && Dormitory.Of(c) == null);
            var farBed = Put<Dormitory>(BuildingKind.DoubleBed, single.Position + Vector3.right * 3);
            var farMat = Put<Dormitory>(BuildingKind.SleepingMat, single.Position + Vector3.right * 9);
            Check(Dormitory.Assign(single) == farMat, "unmarried commander skips double bed");

            // 6. 저장: 변형 종류가 그대로 돌아온다.
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(Object.FindObjectsByType<Dormitory>().Count(d => d.IsMat) == 1 && Object.FindObjectsByType<Dormitory>().Count(d => d.Beds == 2) == 1, "mat and double bed saved");
            Check(Object.FindObjectsByType<FloorTile>().Length == 4, "floors saved");
            Check(Object.FindObjectsByType<Door>().Count(d => d.Data.kind == BuildingKind.LockedDoor) == 1 && Object.FindObjectsByType<Door>().Count(d => d.IsPrisonDoor) == 2, "locked/barred doors saved");
            made.Clear();
            return "PASS " + checks + " furniture batch 2 checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
