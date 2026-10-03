using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Map;
using UnityEngine;

namespace AntColony.Buildings
{
    // Phase 5(2026-10-01 확정, 10-02 구현): 1m 칸 격자. 벽·문·성벽·성문으로 둘러싸인 칸 묶음이 방이고,
    // 방 종류는 안에 놓인 가구로 자동 판정(산미포식). 방에는 지붕이 생기고 등급(인상도)이 붙는다. 수치는 잠정.
    public enum RoomKind { None, Empty, Mixed, Bedroom, Dining, Workroom, Hospital, Recreation, Storeroom, Nursery, Prison, WaitingRoom, Greenhouse }

    public sealed class Room
    {
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();
        public readonly List<BuildingBase> Furniture = new List<BuildingBase>();
        public RoomKind Kind;
        public int Decorations;
        public int Score => Cells.Count + Decorations * GameBalanceRooms.DecorationScore;
        // 0 초라함 / 1 보통 / 2 훌륭함
        public int Grade => Score < GameBalanceRooms.GradeNormal ? 0 : Score < GameBalanceRooms.GradeGreat ? 1 : 2;
        public string GradeName => new[] { "초라함", "보통", "훌륭함" }[Grade];
        public string KindName => RoomSystem.Name(Kind);
    }

    public static class GameBalanceRooms
    {
        public const int MaxRoomCells = 400, DecorationScore = 6, GradeNormal = 25, GradeGreat = 70;
        public const float MatchingWorkBonus = 1.15f;
        public static readonly int[] GradeMood = { 0, 2, 4 };
        public const float OutsideSleepMood = -5, RoomSleepMood = 1, RoomMealMood = 2;
        public const float CastleWallRadius = 3, CastleWallArmor = 2;
    }

    public static class RoomSystem
    {
        private static bool dirty = true;
        private static readonly List<Room> rooms = new List<Room>();
        private static readonly Dictionary<Vector2Int, Room> roomOf = new Dictionary<Vector2Int, Room>();
        private static readonly HashSet<Vector2Int> insideWalls = new HashSet<Vector2Int>();
        private static bool hasCastleWalls;

        public static void MarkDirty() => dirty = true;
        public static IReadOnlyList<Room> Rooms { get { Refresh(); return rooms; } }
        public static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
        public static Room RoomAt(Vector3 p) { Refresh(); return roomOf.TryGetValue(Cell(p), out var r) ? r : null; }
        public static bool IsIndoors(Vector3 p) => RoomAt(p) != null;
        // 성벽 안쪽 = 본거지 영역. 성벽이 하나도 없으면 판정하지 않는다(true).
        public static bool InsideCastle(Vector3 p) { Refresh(); return !hasCastleWalls || insideWalls.Contains(Cell(p)); }
        public static bool HasCastleWalls { get { Refresh(); return hasCastleWalls; } }

        public static bool IsBoundary(BuildingBase b) => b is SoilWall || b is Wall || b is Door || b is Gate;
        public static RoomKind KindOf(BuildingBase b)
        {
            if (b == null || b.IsDead || IsBoundary(b) || b is Decoration || b is Housing) return RoomKind.None;
            if (b.GetComponent<NurseryChamber>() != null) return RoomKind.Nursery;
            if (b.GetComponent<PrisonerCamp>() != null) return RoomKind.Prison;
            if (b.GetComponent<AntColony.World.ResourceNode>() != null) return RoomKind.Greenhouse;
            return b switch
            {
                Dormitory => RoomKind.Bedroom, Kitchen => RoomKind.Dining, Infirmary => RoomKind.Hospital,
                RestRoom or RecreationSpot => RoomKind.Recreation, Storage or Stockpile => RoomKind.Storeroom,
                ConscriptionPost => RoomKind.WaitingRoom,
                ScienceLab or ResearchLab or DefenseLab or Workshop or Barracks => RoomKind.Workroom,
                _ => RoomKind.None
            };
        }
        public static string Name(RoomKind k) => k switch
        {
            RoomKind.Empty => "빈 방", RoomKind.Mixed => "잡동사니 방", RoomKind.Bedroom => "숙소", RoomKind.Dining => "식당",
            RoomKind.Workroom => "작업실", RoomKind.Hospital => "의무실", RoomKind.Recreation => "휴게실", RoomKind.Storeroom => "창고",
            RoomKind.Nursery => "육아실", RoomKind.Prison => "감옥", RoomKind.WaitingRoom => "대기실", RoomKind.Greenhouse => "온실", _ => "바깥"
        };

        // 같은 종류의 방 안에 있는 가구는 작업 속도 +15%. 방 밖·다른 방은 그대로.
        public static float WorkBonus(Component target)
        {
            var b = target != null ? target.GetComponentInParent<BuildingBase>() : null;
            if (b == null) return 1f;
            var room = RoomAt(b.Position);
            return room != null && room.Kind != RoomKind.Mixed && room.Kind == KindOf(b) ? GameBalanceRooms.MatchingWorkBonus : 1f;
        }

        private static void Refresh()
        {
            if (!dirty) return;
            dirty = false; rooms.Clear(); roomOf.Clear(); insideWalls.Clear(); hasCastleWalls = false;
            var buildings = Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && !b.IsDead).ToArray();
            var blocked = new HashSet<Vector2Int>(); var castle = new HashSet<Vector2Int>();
            foreach (var b in buildings.Where(IsBoundary))
            {
                var cells = Footprint(b);
                blocked.UnionWith(cells);
                if (b is Gate || b is Wall w && w.IsCastle) { castle.UnionWith(cells); hasCastleWalls = true; }
            }
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            var min = Cell(bounds.min); var max = Cell(bounds.max);
            var seen = new HashSet<Vector2Int>();
            // 벽 칸에 붙은 빈칸에서만 시작한다(맵 전체를 훑지 않는다).
            foreach (var start in blocked.SelectMany(Neighbours).Where(c => !blocked.Contains(c)).ToArray())
            {
                if (seen.Contains(start)) continue;
                var region = Flood(start, blocked, seen, min, max, GameBalanceRooms.MaxRoomCells);
                if (region == null) continue;
                var room = new Room(); room.Cells.AddRange(region);
                foreach (var c in region) roomOf[c] = room;
                rooms.Add(room);
            }
            foreach (var b in buildings)
            {
                if (!roomOf.TryGetValue(Cell(b.Position), out var room)) continue;
                if (b is Decoration) room.Decorations++;
                else if (KindOf(b) != RoomKind.None) room.Furniture.Add(b);
            }
            foreach (var room in rooms)
            {
                var kinds = room.Furniture.Select(KindOf).Distinct().ToList();
                room.Kind = kinds.Count == 0 ? RoomKind.Empty : kinds.Count == 1 ? kinds[0] : RoomKind.Mixed;
            }
            if (hasCastleWalls)
            {
                // 성벽 안쪽: 성벽·성문만 경계로 보고, 맵 가장자리에 닿지 않는 영역(크기 제한 없음).
                var castleSeen = new HashSet<Vector2Int>();
                foreach (var start in castle.SelectMany(Neighbours).Where(c => !castle.Contains(c)).ToArray())
                {
                    if (castleSeen.Contains(start)) continue;
                    var region = Flood(start, castle, castleSeen, min, max, int.MaxValue);
                    if (region != null) insideWalls.UnionWith(region);
                }
            }
            RoomRoofs.Rebuild(rooms);
        }

        private static List<Vector2Int> Flood(Vector2Int start, HashSet<Vector2Int> blocked, HashSet<Vector2Int> seen, Vector2Int min, Vector2Int max, int limit)
        {
            var region = new List<Vector2Int>(); var queue = new Queue<Vector2Int>(); var open = false;
            if (start.x < min.x || start.y < min.y || start.x > max.x || start.y > max.y) return null;
            queue.Enqueue(start); seen.Add(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue(); region.Add(c);
                if (c.x <= min.x || c.y <= min.y || c.x >= max.x || c.y >= max.y || region.Count > limit) open = true;
                foreach (var n in Neighbours(c))
                    // 바깥/큰 영역도 끝까지 방문해야 다음 시작점에서 가짜 방으로 쪼개지지 않는다.
                    if (n.x >= min.x && n.y >= min.y && n.x <= max.x && n.y <= max.y
                        && !blocked.Contains(n) && seen.Add(n)) queue.Enqueue(n);
            }
            return open ? null : region;
        }

        private static IEnumerable<Vector2Int> Neighbours(Vector2Int c)
        {
            yield return c + Vector2Int.right; yield return c + Vector2Int.left; yield return c + Vector2Int.up; yield return c + Vector2Int.down;
        }

        public static List<Vector2Int> Footprint(BuildingBase b)
        {
            var r = b.GetComponent<Renderer>(); var cells = new List<Vector2Int>();
            var bounds = r != null ? r.bounds : new Bounds(b.Position, Vector3.one);
            var lo = Cell(bounds.min + new Vector3(.05f, 0, .05f)); var hi = Cell(bounds.max - new Vector3(.05f, 0, .05f));
            for (var x = lo.x; x <= hi.x; x++) for (var z = lo.y; z <= hi.y; z++) cells.Add(new Vector2Int(x, z));
            return cells;
        }

        public static bool NearCastleWall(Vector3 p) => Wall.CastleWalls
            .Any(w => w != null && !w.IsDead && (w.Position - p).sqrMagnitude <= GameBalanceRooms.CastleWallRadius * GameBalanceRooms.CastleWallRadius);
    }
}
