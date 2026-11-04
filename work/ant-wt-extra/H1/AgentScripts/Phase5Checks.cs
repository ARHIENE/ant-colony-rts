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
using Object = UnityEngine.Object;

// Phase 5: 1m 격자·벽·문·지붕·방 판정·방 보너스·성벽/성문·주거는 방 밖.
public static class Phase5Checks
{
    static int checks;
    static readonly List<GameObject> made = new List<GameObject>();
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { k, UnitRole.Worker });
    static T Put<T>(BuildingKind k, Vector3 p) where T : Component
    {
        var go = Object.Instantiate(Template(k), p, Quaternion.identity); go.SetActive(true); made.Add(go); return go.GetComponent<T>();
    }
    public static async Task<string> Main()
    {
        checks = 0; made.Clear();
        if (!Application.isPlaying) throw new Exception("Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261004, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
        try
        {
            var home = Object.FindAnyObjectByType<Stockpile>().Position;
            var o = new Vector3(Mathf.Floor(home.x) + 20, home.y, Mathf.Floor(home.z) + 20);
            // 6×6 테두리(안쪽 4×4)를 나뭇잎 벽으로 두르고 한 칸은 문.
            for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++)
            {
                if (x != 0 && x != 5 && z != 0 && z != 5) continue;
                var p = o + new Vector3(x + .5f, .75f, z + .5f);
                if (x == 0 && z == 2) Put<Door>(BuildingKind.Door, p); else Put<Wall>(BuildingKind.LeafWall, p);
            }
            await Task.Yield();
            var inside = o + new Vector3(2.5f, .5f, 2.5f);
            var room = RoomSystem.RoomAt(inside);
            Check(room != null && room.Cells.Count == 16 && room.Kind == RoomKind.Empty, "enclosed 4x4 room detected");
            Check(!RoomSystem.IsIndoors(o + new Vector3(10, 0, 10)), "outside is not a room");
            Check(RoomRoofs.Count >= 1, "roof generated for room");
            Check(Put<Wall>(BuildingKind.CapWall, o + new Vector3(30.5f, .8f, .5f)).Armor > Put<Wall>(BuildingKind.LeafWall, o + new Vector3(32.5f, .75f, .5f)).Armor
                && Put<Wall>(BuildingKind.LeafWall, o + new Vector3(34.5f, .75f, .5f)).Flammable, "wall materials differ");

            // 가구로 방 종류 판정: 침대(숙소) → 숙소, 식탁 추가 → 잡동사니.
            var bed = Put<Dormitory>(BuildingKind.Dormitory, o + new Vector3(3f, .7f, 3f));
            await Task.Yield();
            room = RoomSystem.RoomAt(inside);
            Check(room != null && room.Kind == RoomKind.Bedroom && room.Furniture.Contains(bed), "bed makes bedroom");
            Check(Mathf.Approximately(RoomSystem.WorkBonus(bed), GameBalanceRooms.MatchingWorkBonus), "matching room work bonus");
            var deco = Put<Decoration>(BuildingKind.FlowerPot, o + new Vector3(1.5f, .5f, 1.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Decorations == 1 && RoomSystem.RoomAt(inside).Score == 16 + GameBalanceRooms.DecorationScore, "decoration raises room score");
            var table = Put<Kitchen>(BuildingKind.Kitchen, o + new Vector3(4.5f, .75f, 4.5f)); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Mixed && Mathf.Approximately(RoomSystem.WorkBonus(bed), 1f), "mixed furniture gives no bonus");
            made.Remove(table.gameObject); Object.Destroy(table.gameObject); await Task.Yield(); await Task.Yield();
            Check(RoomSystem.RoomAt(inside).Kind == RoomKind.Bedroom, "removing furniture re-judges room");

            // 2026-10-05 방 종류 재정리: 필수 가구 → 방 이름, 휴게실은 2종 이상, 상위 방 이름표 구조.
            made.Remove(bed.gameObject); Object.Destroy(bed.gameObject); await Task.Yield(); await Task.Yield();
            async Task<RoomKind> Furnish(params BuildingKind[] kinds)
            {
                var placed = kinds.Select((k, i) => Put<BuildingBase>(k, o + new Vector3(1.5f + i, .75f, 3.5f))).ToList();
                await Task.Yield(); var kind = RoomSystem.RoomAt(inside).Kind;
                foreach (var p in placed) { made.Remove(p.gameObject); Object.Destroy(p.gameObject); }
                await Task.Yield(); await Task.Yield();
                return kind;
            }
            Check(await Furnish(BuildingKind.ScienceLab) == RoomKind.Laboratory, "research bench makes laboratory");
            Check(await Furnish(BuildingKind.Workshop) == RoomKind.Workshop, "workshop room");
            Check(await Furnish(BuildingKind.Infirmary) == RoomKind.Hospital, "sickbed makes hospital");
            Check(await Furnish(BuildingKind.Campfire) == RoomKind.Empty, "one recreation kind is not a lounge");
            Check(await Furnish(BuildingKind.Campfire, BuildingKind.GamblingDen) == RoomKind.Recreation, "two recreation kinds make a lounge");
            Check(await Furnish(BuildingKind.ConscriptionPost) == RoomKind.Barracks, "conscription stands in for barracks furniture");
            Check(await Furnish(BuildingKind.Storage) == RoomKind.Storeroom, "storage makes storeroom");
            Check(RoomSystem.Name(RoomKind.DiningKitchen) == "식당·부엌" && RoomSystem.Name(RoomKind.Temple) == "신전", "new room names");
            var empty = RoomSystem.RoomAt(inside);
            Check(empty.Title == empty.KindName && GameBalanceRooms.GradeNames.Length == GameBalanceRooms.GradeThresholds.Length + 1
                && GameBalanceRooms.GradeMood.Length == GameBalanceRooms.GradeNames.Length, "grade tiers consistent");
            RoomSystem.RankNames[RoomKind.Empty] = new[] { "헛간", "창고방", "넓은 방" };
            Check(empty.Title == RoomSystem.RankNames[RoomKind.Empty][empty.Grade], "rank names follow grade");
            RoomSystem.RankNames.Remove(RoomKind.Empty);

            // 벽을 부수면 방이 사라진다.
            var broken = made.Select(g => g.GetComponent<Wall>()).First(w => w != null && w.Position.x < o.x + 1 && w.Position.z > o.z + 3 && w.Position.z < o.z + 4);
            var brokenAt = broken.Position; broken.TakeDamage(float.MaxValue); await Task.Yield();
            Check(RoomSystem.RoomAt(inside) == null, "breaking a wall opens the room");
            Put<Wall>(BuildingKind.LeafWall, brokenAt); await Task.Yield();
            Check(RoomSystem.RoomAt(inside) != null, "rebuilt wall closes room");

            // 방 밖 침대: '바깥에서 잠', 주거 건물은 방 안 배치 불가.
            Check(Housing.IsKind(BuildingKind.Hut) && RoomSystem.IsIndoors(inside), "housing placement rejects room cells");

            // 격자 맞춤: 1칸 벽은 칸 가운데, 2칸 성벽은 칸 경계.
            var placement = Object.FindAnyObjectByType<BuildingPlacementController>();
            var snap = typeof(BuildingPlacementController).GetMethod("GetPlacementPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            typeof(BuildingPlacementController).GetField("pendingKind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(placement, BuildingKind.LeafWall);
            var p1 = (Vector3)snap.Invoke(placement, new object[] { Template(BuildingKind.LeafWall), new Vector3(10.2f, 0, 7.9f) });
            Check(Mathf.Approximately(p1.x, 10.5f) && Mathf.Approximately(p1.z, 7.5f), "one-cell snap to center " + p1);
            typeof(BuildingPlacementController).GetField("pendingKind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(placement, BuildingKind.CastleWall);
            var p2 = (Vector3)snap.Invoke(placement, new object[] { Template(BuildingKind.CastleWall), new Vector3(10.4f, 0, 7.7f) });
            Check(Mathf.Approximately(p2.x, 10f) && Mathf.Approximately(p2.z, 8f), "two-cell snap to edge " + p2);

            // 성벽·성문으로 둘러싼 안쪽 = 본거지 영역. 성문 열고 닫기 + 저장.
            var c0 = o + new Vector3(-40, 0, 0);
            Gate gate = null;
            for (var i = 0; i < 6; i++) for (var j = 0; j < 6; j++)
            {
                if (i != 0 && i != 5 && j != 0 && j != 5) continue;
                var p = c0 + new Vector3(i * 2 + 1, 1.3f, j * 2 + 1);
                if (i == 0 && j == 2) gate = Put<Gate>(BuildingKind.Gate, p); else Put<Wall>(BuildingKind.CastleWall, p);
            }
            await Task.Yield();
            var court = c0 + new Vector3(6, 0, 6);
            Check(RoomSystem.HasCastleWalls && RoomSystem.InsideCastle(court) && !RoomSystem.InsideCastle(c0 + new Vector3(-10, 0, -10)), "castle encloses home area");
            Check(RoomSystem.NearCastleWall(c0 + new Vector3(1, 0, 3)) && !RoomSystem.NearCastleWall(court), "castle wall bonus zone");
            Check(gate.Open && !gate.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled, "gate starts open");
            gate.SetOpen(false); Check(!gate.Open && gate.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled, "closed gate blocks");
            var file = SaveSnapshot.Capture();
            Check(SaveValidator.Validate(file, out var error), "save with walls: " + error);
            var dto = file.buildings.FirstOrDefault(b => b.kind == "Gate");
            Check(dto != null && !dto.gateOpen && file.buildings.Count(b => b.kind == "LeafWall") >= 19 && file.buildings.Any(b => b.kind == "Door"), "walls, door and gate state saved");

            // 성벽 밖 주거는 안전 수요를 깎는다.
            var pop = ColonyPopulation.Instance; var before = pop.SafetyDemand;
            Put<Housing>(BuildingKind.Hut, c0 + new Vector3(-10, .6f, -10)); await Task.Yield();
            Check(pop.SafetyDemand < before, "housing outside walls lowers safety");
            return "PASS " + checks + " Phase 5 checks";
        }
        finally
        {
            foreach (var g in made) if (g != null) Object.Destroy(g);
        }
    }
}
