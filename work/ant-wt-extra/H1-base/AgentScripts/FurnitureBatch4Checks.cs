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

// 2026-10-05 가구 4차: 저장고(식량)·항아리·무기고(장비 보관함 +10).
public static class FurnitureBatch4Checks
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
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Furniture4-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261008, mapSize = MapSize.Small }); await Ready();
            var rm = ResourceManager.Instance;

            // 1. 템플릿: 창고 템플릿이 변형으로 바뀌지 않는다.
            Check(Template(BuildingKind.Storage).GetComponent<Storage>().Data.kind == BuildingKind.Storage, "storage template kept");
            foreach (var k in new[] { BuildingKind.FoodStore, BuildingKind.Jar, BuildingKind.Armory })
                Check(Template(k) != null && Template(k).GetComponent<BuildingBase>().Data.kind == k, "template " + k);

            // 2. 한도: 저장고 식량 +150, 항아리 식량·재료 +50
            int Cap(AntColony.Data.ResourceType t) => rm.GetCapacity(t);
            var food = Cap(AntColony.Data.ResourceType.Food); var soil = Cap(AntColony.Data.ResourceType.Soil); var special = Cap(AntColony.Data.ResourceType.Special);
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var store = Put<Storage>(BuildingKind.FoodStore, home + new Vector3(12, .7f, 12));
            Check(Cap(AntColony.Data.ResourceType.Food) == food + 150 && Cap(AntColony.Data.ResourceType.Soil) == soil && Cap(AntColony.Data.ResourceType.Special) == special, "food store adds 150 food capacity");
            var jar = Put<Storage>(BuildingKind.Jar, home + new Vector3(15, .5f, 12));
            Check(Cap(AntColony.Data.ResourceType.Food) == food + 200 && Cap(AntColony.Data.ResourceType.Soil) == soil + 50, "jar adds 50 food and material");
            await Remove(jar);
            Check(Cap(AntColony.Data.ResourceType.Food) == food + 150 && Cap(AntColony.Data.ResourceType.Soil) == soil, "removing jar returns capacity");

            // 3. 무기고: 장비 보관함 +10
            Check(EquipmentInventory.Capacity == EquipmentInventory.BaseCapacity, "base equipment capacity");
            var armory = Put<Armory>(BuildingKind.Armory, home + new Vector3(18, .9f, 12));
            Check(EquipmentInventory.Capacity == EquipmentInventory.BaseCapacity + GameBalance.ArmorySlots, "armory adds 10 equipment slots");

            // 4. 방: 저장고 = 냉장실, 무기고 = 무기고, 항아리 = 창고
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            var s2 = Put<Storage>(BuildingKind.FoodStore, o + new Vector3(3, .7f, 3)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.ColdStorage, "food store makes cold storage");
            await Remove(s2);
            var a2 = Put<Armory>(BuildingKind.Armory, o + new Vector3(3, .9f, 3.25f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Armory, "armory room");
            await Remove(a2);
            var j2 = Put<Storage>(BuildingKind.Jar, o + new Vector3(2.5f, .5f, 2.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Storeroom, "jar makes storeroom");

            // 5. 저장: 무기고 덕분에 30개를 넘는 보관함도 통과, 변형 종류 복원
            var file = SaveSnapshot.Capture();
            Check(file.buildings.Count(b => b.kind == "Armory") == 1 && file.buildings.Any(b => b.kind == "FoodStore") && file.buildings.Any(b => b.kind == "Jar"), "variants saved by kind");
            Check(SaveSystem.TrySave(false, 0, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load: " + error); await Ready();
            Check(Armory.Count == 1 && EquipmentInventory.Capacity == EquipmentInventory.BaseCapacity + GameBalance.ArmorySlots, "armory restored");
            Check(Object.FindObjectsByType<Storage>().Count(s => s.Data.kind == BuildingKind.FoodStore) == 1 && Object.FindObjectsByType<Storage>().Count(s => s.Data.kind == BuildingKind.Jar) == 1, "food store and jar restored");
            made.Clear();
            return "PASS " + checks + " furniture batch 4 checks";
        }
        finally
        {
            foreach (var go in made) if (go != null) Object.Destroy(go);
            SaveStorage.RootOverride = original;
        }
    }
}
