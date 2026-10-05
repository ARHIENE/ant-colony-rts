using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Map;
using UnityEngine;

namespace AntColony.Buildings
{
    // Phase 5(2026-10-01 확정, 10-02 구현): 1m 칸 격자. 벽·문·성벽·성문으로 둘러싸인 칸 묶음이 방이고,
    // 방 종류는 안에 놓인 가구로 자동 판정(산미포식). 방에는 지붕이 생기고 등급(인상도)이 붙는다. 수치는 잠정.
    // 2026-10-05 방 종류 재정리. 방 판정에 쓰이지 않아 저장되지 않는다(번호 변경 무방).
    public enum RoomKind
    {
        None, Empty, Mixed,
        Bedroom, PrivateRoom, Dining, Kitchen, DiningKitchen, Bathroom, Bathhouse,
        Laboratory, Workshop, ProcessingRoom, Studio, TrainingRoom,
        Hospital, Recreation, Library, Storeroom, ColdStorage,
        Farm, Fishery, PowerPlant,
        Nursery, Prison, Barracks, Armory, WarRoom, BanquetHall, Graveyard, Temple
    }

    public sealed class Room
    {
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();
        public readonly List<BuildingBase> Furniture = new List<BuildingBase>();
        public RoomKind Kind;
        public int Decorations, Floors;
        public bool PrisonDoor; // 경계에 창살문·잠금문이 있음(감옥 조건)
        public int Score => Cells.Count + Decorations * GameBalanceRooms.DecorationScore + Floors / 2; // 바닥 칸당 +0.5
        // 인상도 단계: 점수가 넘은 기준선 수(지금 3단계 0 초라함 / 1 보통 / 2 훌륭함).
        public int Grade => GameBalanceRooms.GradeThresholds.Count(t => Score >= t);
        public string GradeName => GameBalanceRooms.GradeNames[Grade];
        public string KindName => RoomSystem.Name(Kind);
        // 산소미포함식 상위 방 이름(숙소 → 고급 숙소 → 귀빈실 등). 이름표가 없는 종류는 기본 이름.
        public string Title => RoomSystem.RankNames.TryGetValue(Kind, out var names) && names.Length > 0 ? names[Mathf.Min(Grade, names.Length - 1)] : KindName;
    }

    public static class GameBalanceRooms
    {
        public const int MaxRoomCells = 400, DecorationScore = 6;
        // 단계 수를 바꿀 때는 기준선·이름·기분 배열 길이를 함께 맞춘다(이름·기분 = 기준선 + 1).
        public static readonly int[] GradeThresholds = { 25, 70 };
        public static readonly string[] GradeNames = { "초라함", "보통", "훌륭함" };
        public const float MatchingWorkBonus = 1.15f;
        public const int MinBedsForDormRoom = 2, MinRecreationKinds = 2;
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
        // 가구가 요구하는 방 종류(필수 가구, 2026-10-05). 아직 없는 가구는 가장 가까운 기존 건물로 대신 잇고 '대용' 주석.
        // 가구가 없어 판정할 수 없는 방: 가공실(가공대·용광로), 화실(예술대),
        // 양식장·목장, 무기고, 연회장(긴 식탁 + 장식 3+), 묘지(관·묘비, 방 밖도 가능), 신전(제단). TODO 가구가 생기면 아래에 연결.
        // 방 밖 전용(None): 방어·마을 건물(산성탑·함정·주거 등), 풍차·물레방아·태양광판, 이동수단. 밭은 방 밖에서도 쓰고 방 안이면 농장.
        public static RoomKind KindOf(BuildingBase b)
        {
            if (b == null || b.IsDead || IsBoundary(b) || b is Decoration || b is Housing) return RoomKind.None;
            if (b.GetComponent<NurseryChamber>() != null) return RoomKind.Nursery; // 요람
            if (b.GetComponent<PrisonerCamp>() != null) return RoomKind.Prison; // 대용: 침대(포로 수용소) + 창살문·잠금문(Judge)
            if (b.GetComponent<AntColony.World.ResourceNode>() != null) return RoomKind.Farm; // 밭·버섯밭·축사 통합
            if (b.GetComponent<ScoutPost>() != null) return RoomKind.WarRoom; // 대용: 작전실 지도대·신호탑
            if (b is HygieneFixture) return RoomKind.Bathroom; // 화장실·세면대·샤워기
            if (b is PowerNode power) return power.IsGenerator ? RoomKind.PowerPlant : RoomKind.None; // 발전기류, 전선·배터리·전등은 아무 방에나
            return b switch
            {
                Dormitory => RoomKind.Bedroom, Infirmary => RoomKind.Hospital, // 침대류·병상
                Kitchen k => k.Data != null && k.Data.kind == BuildingKind.Hearth ? RoomKind.Kitchen : RoomKind.Dining, // 화덕(조리대)·식탁
                RecreationSpot spot when spot.KindIndex == 2 => RoomKind.Library, // 책장
                RecreationSpot spot when spot.KindIndex == 3 => RoomKind.Bathhouse, // 목욕통(온천은 아직 없음)
                RestRoom or RecreationSpot => RoomKind.Recreation, // 휴게 가구
                Storage s when s.Data != null && s.Data.kind == BuildingKind.FoodStore => RoomKind.ColdStorage, // 저장고
                Storage or Stockpile => RoomKind.Storeroom, // 수납장(기존 창고)·항아리
                Armory => RoomKind.Armory, // 무기고 가구
                ScienceLab or ResearchLab or DefenseLab => RoomKind.Laboratory, // 연구대
                Workshop => RoomKind.Workshop, Barracks => RoomKind.TrainingRoom, // 공방·훈련대
                ConscriptionPost => RoomKind.Barracks, // 대용: 막사 가구(대기실 역할 흡수)
                _ => RoomKind.None
            };
        }
        public static string Name(RoomKind k) => k switch
        {
            RoomKind.Empty => "빈 방", RoomKind.Mixed => "잡동사니 방", RoomKind.Bedroom => "숙소", RoomKind.PrivateRoom => "개인실",
            RoomKind.Dining => "식당", RoomKind.Kitchen => "부엌", RoomKind.DiningKitchen => "식당·부엌", RoomKind.Bathroom => "욕실", RoomKind.Bathhouse => "목욕탕",
            RoomKind.Laboratory => "연구실", RoomKind.Workshop => "공방", RoomKind.ProcessingRoom => "가공실", RoomKind.Studio => "화실", RoomKind.TrainingRoom => "훈련장",
            RoomKind.Hospital => "의무실", RoomKind.Recreation => "휴게실", RoomKind.Library => "도서관", RoomKind.Storeroom => "창고", RoomKind.ColdStorage => "냉장실",
            RoomKind.Farm => "농장", RoomKind.Fishery => "양식장·목장", RoomKind.PowerPlant => "발전소", RoomKind.Nursery => "육아실", RoomKind.Prison => "감옥",
            RoomKind.Barracks => "막사", RoomKind.Armory => "무기고", RoomKind.WarRoom => "작전실", RoomKind.BanquetHall => "연회장",
            RoomKind.Graveyard => "묘지", RoomKind.Temple => "신전", _ => "바깥"
        };
        // 상위 방 이름표(인상도 단계별). TODO 단계 수·이름이 정해지면 채운다. 예: [Bedroom] = { "숙소", "고급 숙소", "귀빈실" }
        public static readonly Dictionary<RoomKind, string[]> RankNames = new Dictionary<RoomKind, string[]>();

        // 방 안 가구들로 종류를 정한다. 숙소는 침대 2+(숙소 건물 1동 = 침대 4), 휴게실은 휴게 가구 2종+, 식탁 + 조리대는 겸용.
        private static RoomKind Judge(Room room)
        {
            var kinds = room.Furniture.Select(KindOf).Distinct().ToList();
            if (kinds.Count == 0) return RoomKind.Empty;
            if (kinds.Count == 2 && kinds.Contains(RoomKind.Dining) && kinds.Contains(RoomKind.Kitchen)) return RoomKind.DiningKitchen;
            if (kinds.Count > 1) return RoomKind.Mixed;
            var kind = kinds[0];
            if (kind == RoomKind.Bedroom)
                return room.Furniture.Sum(f => f is Dormitory d ? d.RoomBedCount : 1) >= GameBalanceRooms.MinBedsForDormRoom ? RoomKind.Bedroom : RoomKind.PrivateRoom;
            if (kind == RoomKind.Prison && !room.PrisonDoor) return RoomKind.Empty; // 감옥 = 침대 + 창살문·잠금문
            if (kind == RoomKind.Recreation && room.Furniture.Select(f => f.Data != null ? f.Data.kind : BuildingKind.RestRoom).Distinct().Count() < GameBalanceRooms.MinRecreationKinds) return RoomKind.Empty;
            return kind;
        }

        // 같은 종류의 방 안에 있는 가구는 작업 속도 +15%(식당·부엌 겸용은 절반). 방 밖·다른 방은 그대로.
        public static float WorkBonus(Component target)
        {
            var b = target != null ? target.GetComponentInParent<BuildingBase>() : null;
            if (b == null) return 1f;
            var room = RoomAt(b.Position);
            if (room == null) return 1f;
            var kind = KindOf(b);
            if (room.Kind == RoomKind.DiningKitchen && (kind == RoomKind.Dining || kind == RoomKind.Kitchen)) return 1f + (GameBalanceRooms.MatchingWorkBonus - 1f) * .5f;
            return room.Kind != RoomKind.Mixed && (room.Kind == kind || room.Kind == RoomKind.PrivateRoom && kind == RoomKind.Bedroom) ? GameBalanceRooms.MatchingWorkBonus : 1f;
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
                else if (b is FloorTile) room.Floors++;
                else if (KindOf(b) != RoomKind.None) room.Furniture.Add(b);
            }
            foreach (var door in buildings.OfType<Door>().Where(d => d.IsPrisonDoor))
                foreach (var c in Footprint(door).SelectMany(Neighbours))
                    if (roomOf.TryGetValue(c, out var room)) room.PrisonDoor = true;
            foreach (var room in rooms) room.Kind = Judge(room);
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
