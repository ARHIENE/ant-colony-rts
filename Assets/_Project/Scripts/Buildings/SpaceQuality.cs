using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;

namespace AntColony.Buildings
{
    public enum TemperatureBand { Cold, Comfortable, Hot }

    // 미관·온도(2026-10-10 확정안). 열전도·비열 시뮬레이션 없이 공간 단위로 판정한다.
    // 미관: 방(방 밖이면 주변 6m)의 마감 재료 미관 평균 + 장식 − 시체·파손 시설·생활 공간에 방치된 물자. 별도 스트레스 수치는 없다.
    // 온도: 추움·쾌적·더움. 바깥 온도(계절·날씨·바이옴)를 방의 단열(벽 재료)과 난방 가구(연료 소비)가 쾌적 쪽으로 당긴다.
    public static class SpaceQuality
    {
        // ponytail: 수치는 잠정(미관 상하한 ±10, 단열 기준 0.6, 장식 가산 상한 6).
        public const float BeautyCap = 10f, InsulationComfort = .6f, DecorationCap = 6f, NearRadius = 6f;

        private static BuildingBase[] cache = new BuildingBase[0]; private static float cacheTime = -10;
        public static void Invalidate() => cacheTime = -10; // 건물이 생기거나 사라지면 다시 모은다
        private static IEnumerable<BuildingBase> Buildings()
        {
            if (Time.unscaledTime - cacheTime > 1f || cache.Any(b => b == null)) { cache = Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None); cacheTime = Time.unscaledTime; }
            return cache.Where(b => b != null && b.isActiveAndEnabled && !b.IsDead && b.CountsTowardPlayerDefeat);
        }

        // 벽·문은 종류가 곧 재료다. 나머지는 고른 주재료.
        public static ResourceType MaterialOf(BuildingBase b) => b.Data == null ? ResourceType.Soil : b.Data.kind switch
        {
            BuildingKind.SoilWall => ResourceType.Soil, BuildingKind.LeafWall => ResourceType.Leaf, BuildingKind.CapWall => ResourceType.Iron,
            BuildingKind.CastleWall => ResourceType.Stone, BuildingKind.Door or BuildingKind.LockedDoor or BuildingKind.Gate => ResourceType.Wood,
            BuildingKind.BarredDoor => ResourceType.Iron, _ => b.MainMaterial
        };

        public static float BeautyAt(Vector3 p)
        {
            var room = RoomSystem.RoomAt(p);
            var items = room != null ? room.Walls.Concat(room.Contents).Distinct().ToList()
                : Buildings().Where(b => (b.Position - p).sqrMagnitude <= NearRadius * NearRadius).ToList();
            bool Near(Vector3 q) => room != null ? RoomSystem.RoomAt(q) == room : (q - p).sqrMagnitude <= NearRadius * NearRadius;
            float finish = items.Count == 0 ? 0 : items.Average(b => MaterialInfo.For(MaterialOf(b)).beauty);
            float decor = Mathf.Min(DecorationCap, items.OfType<Decoration>().GroupBy(d => d.Data.kind).Sum(g => g.Max(d => d.MoodBonus)) * .5f);
            float bad = Corpse.All.Count(c => c != null && c.Available && Near(c.Position)) * 2f
                + items.Count(b => b.CurrentHealth < b.MaxHealth * .5f)
                // 생활 공간에 방치된 물자(바닥 더미). 창고·작업장 보관은 감점하지 않는다.
                + (room != null && (room.Kind == RoomKind.Storeroom || room.Kind == RoomKind.ColdStorage || room.Kind == RoomKind.Workshop || room.Kind == RoomKind.ProcessingRoom) ? 0
                    : ResourceNode.Available.Count(n => n != null && n.IsLooseCargo && Near(n.transform.position)));
            return Mathf.Clamp(finish + decor - bad, -BeautyCap, BeautyCap);
        }
        public static string BeautyName(float v) => v >= 2 ? "좋음" : v <= -2 ? "흉함" : "평범";

        // 바깥: 겨울·눈 = 추움, 여름 = 더움(사막은 겨울 외 더움), 동굴은 늘 쾌적.
        public static TemperatureBand Outdoor
        {
            get
            {
                if (BiomeRules.Current == MapBiome.Cave) return TemperatureBand.Comfortable;
                var w = AntColony.Map.WeatherSystem.Current; var season = GameCalendar.CurrentSeason;
                if (w == AntColony.Map.WeatherKind.Snow || w == AntColony.Map.WeatherKind.Blizzard || season == Season.Winter) return TemperatureBand.Cold;
                if (season == Season.Summer || BiomeRules.Current == MapBiome.Desert) return TemperatureBand.Hot;
                return TemperatureBand.Comfortable;
            }
        }
        public static float Insulation(Room room) => room == null || room.Walls.Count == 0 ? 0 : room.Walls.Average(w => MaterialInfo.For(MaterialOf(w)).insulation);
        public static bool IsHeater(BuildingBase b) => b.Data != null && (b.Data.kind == BuildingKind.Brazier || b.Data.kind == BuildingKind.Hearth || b.Data.kind == BuildingKind.Campfire);
        public static TemperatureBand At(Vector3 p)
        {
            var outdoor = Outdoor; var room = RoomSystem.RoomAt(p);
            if (room == null || outdoor == TemperatureBand.Comfortable) return outdoor;
            if (outdoor == TemperatureBand.Cold && Heating.Heated(room)) return TemperatureBand.Comfortable;
            return Insulation(room) >= InsulationComfort ? TemperatureBand.Comfortable : outdoor; // 단열이 좋으면 실내 온도를 유지한다
        }
        public static string Name(TemperatureBand t) => t == TemperatureBand.Cold ? "추움" : t == TemperatureBand.Hot ? "더움" : "쾌적";
        public static string Describe(Vector3 p)
        {
            var room = RoomSystem.RoomAt(p); var beauty = BeautyAt(p);
            return $"미관 {beauty:+0.#;-0.#;0}({BeautyName(beauty)}) · 온도 {Name(At(p))}" + (room != null ? $" · 단열 {Insulation(room):0.##}" + (Heating.Heated(room) ? " · 난방 중" : "") : "");
        }
    }

    // 난방(2026-10-10): 추운 날 방 안 난방 가구(화롯불·화덕·모닥불)가 목재를 태워 방을 쾌적하게 한다.
    // 단열이 낮을수록 연료를 많이 쓴다(냉난방 소비). 연료가 없으면 난방되지 않는다.
    public sealed class Heating : MonoBehaviour
    {
        // 방은 건물이 바뀔 때마다 다시 만들어지므로 가장 작은 칸으로 구분한다(연료 누적이 끊기지 않게).
        private static Vector2Int Key(Room r) => r.Cells.Aggregate((a, b) => a.x < b.x || a.x == b.x && a.y < b.y ? a : b);
        private static readonly HashSet<Vector2Int> heated = new HashSet<Vector2Int>();
        private static readonly Dictionary<Vector2Int, float> burn = new Dictionary<Vector2Int, float>();
        public static bool Heated(Room r) => r != null && r.Cells.Count > 0 && heated.Contains(Key(r));
        private float timer;
        private void Update()
        {
            if (Time.deltaTime <= 0 || (timer += Time.deltaTime) < 1f) return;
            Tick(timer); timer = 0;
        }
        public static void Tick(float seconds)
        {
            heated.Clear();
            var rm = ResourceManager.Instance; if (rm == null || SpaceQuality.Outdoor != TemperatureBand.Cold) return;
            foreach (var room in RoomSystem.Rooms)
            {
                if (!room.Contents.Any(SpaceQuality.IsHeater)) continue;
                var key = Key(room); burn.TryGetValue(key, out var due);
                due += seconds / GameCalendar.SecondsPerMonth * GameBalance.HeatingWoodPerMonth * (1f - SpaceQuality.Insulation(room) * .8f);
                var whole = Mathf.FloorToInt(due);
                if (whole > 0) { if (!rm.TrySpend(ResourceType.Wood, whole, ResourceReason.Upkeep)) { burn[key] = 0; continue; } due -= whole; }
                burn[key] = due; heated.Add(key);
            }
            var live = new HashSet<Vector2Int>(RoomSystem.Rooms.Where(r => r.Cells.Count > 0).Select(Key));
            foreach (var stale in burn.Keys.Where(k => !live.Contains(k)).ToArray()) burn.Remove(stale);
        }
    }
}
