using System;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// SAVE 캡처용(2026-10-07): 숲 맵 본거지 옆에 가구 5·6차 일부를 늘어놓고 카메라를 맞춘다. 저장하지 않는다.
public static class FurnitureShot
{
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static void Put(BuildingKind k, Vector3 p) { var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261016, mapSize = MapSize.Small, biome = MapBiome.Garden });
        for (var i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50);
        Object.FindAnyObjectByType<SelectionManager>()?.ClearSelection();
        var home = Object.FindAnyObjectByType<Stockpile>().Position;
        var o = new Vector3(Mathf.Floor(home.x) + 8, home.y, Mathf.Floor(home.z) - 4);
        // 연회장: 긴 식탁 + 장식 3개, 옆에 휴게 가구·온천·침대
        for (var x = 0; x < 9; x++) for (var z = 0; z < 8; z++)
        {
            if (x != 0 && x != 8 && z != 0 && z != 7) continue;
            var p = o + new Vector3(x + .5f, .75f, z + .5f);
            if (x == 0 && z == 3) Put(BuildingKind.Door, p); else Put(BuildingKind.LeafWall, p);
        }
        Put(BuildingKind.BanquetTable, o + new Vector3(4.5f, .4f, 3.5f));
        Put(BuildingKind.Carpet, o + new Vector3(2f, .02f, 3.5f));
        Put(BuildingKind.Tapestry, o + new Vector3(4.5f, .8f, 6.8f));
        Put(BuildingKind.Brazier, o + new Vector3(7.2f, .4f, 1.2f));
        Put(BuildingKind.BronzeStatue, o + new Vector3(7.2f, 1.2f, 6f));
        var r = o + new Vector3(-12, 0, 0);
        Put(BuildingKind.HotSpring, r + new Vector3(1.5f, .25f, 1.5f));
        Put(BuildingKind.Janggi, r + new Vector3(5, .3f, 1));
        Put(BuildingKind.WrestlingRing, r + new Vector3(5.5f, .08f, 5));
        Put(BuildingKind.Instrument, r + new Vector3(1, .5f, 5));
        Put(BuildingKind.BunkBed, r + new Vector3(1, .8f, 9));
        Put(BuildingKind.Hammock, r + new Vector3(3, .3f, 9));
        Put(BuildingKind.Monument, r + new Vector3(6, 1.25f, 9));
        Put(BuildingKind.Lantern, r + new Vector3(8, .6f, 3));
        Put(BuildingKind.Torch, r + new Vector3(-1, .75f, 3));
        await Task.Yield(); await Task.Yield();
        Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>()?.FocusOn(o + new Vector3(-2, 0, 4));
        await Task.Yield();
        return "room=" + RoomSystem.Name(RoomSystem.RoomAt(o + new Vector3(4.5f, .5f, 2)).Kind);
    }
}
