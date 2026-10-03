using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 로컬 맵 바이옴: 6종(Phase 6, 2026-10-01 확정). 시작할 때 고른다(= 난이도), 월드맵 거점마다도 다르다. None은 옛 저장·검사용(보정 없음).
    public enum MapBiome { None, Forest, Garden, Waterside, City, Desert, Cave }

    // 바이옴 차이는 강하게(±60%, 확정). 숲 바닥은 확정, 나머지 3종은 잠정안.
    // ponytail: 바이옴 전용 이벤트(발자국·물뿌리개·배수구 역류·쓰레기 더미)·물가 형태·숲 그늘은 다음 단계.
    public static class BiomeRules
    {
        public const float Strong = .6f;
        public static MapBiome Current => GameSession.Exists ? GameSession.Instance.Options.biome : MapBiome.None;
        public static readonly MapBiome[] All = { MapBiome.Garden, MapBiome.Forest, MapBiome.Waterside, MapBiome.City, MapBiome.Desert, MapBiome.Cave };
        public static MapBiome Random() => (MapBiome)UnityEngine.Random.Range(1, 7);
        // 시작 바이옴 = 난이도(잠정): 정원 쉬움 / 숲·물가 보통 / 도시·사막 어려움 / 동굴 매우 어려움.
        public static string Difficulty(MapBiome b) => b switch
        {
            MapBiome.Garden => "쉬움", MapBiome.Forest or MapBiome.Waterside => "보통", MapBiome.City or MapBiome.Desert => "어려움", MapBiome.Cave => "매우 어려움", _ => "—"
        };
        // 월드맵 거점 바이옴: 거점 이름에서 결정적으로 정한다(저장 불필요).
        public static MapBiome ForSite(string title)
        {
            var h = 17; foreach (var ch in title ?? "") h = unchecked(h * 31 + ch);
            return (MapBiome)(1 + (h & int.MaxValue) % 6);
        }

        public static string Name(MapBiome b) => b switch
        {
            MapBiome.Forest => "숲 바닥", MapBiome.Garden => "뒷마당 정원", MapBiome.Waterside => "물가 모래밭", MapBiome.City => "도시 구석", MapBiome.Desert => "사막", MapBiome.Cave => "동굴", _ => "기본"
        };

        // 맵 생성 때 로컬 자원 노드 양에 곱한다.
        public static float NodeMultiplier(ResourceType type) => NodeMultiplier(Current, type);
        public static float NodeMultiplier(MapBiome biome, ResourceType type) => (biome, type) switch
        {
            (MapBiome.Forest, ResourceType.Food) => 1 + Strong,
            (MapBiome.Forest, ResourceType.Special) => 1 - Strong,
            (MapBiome.Waterside, ResourceType.Soil) => 1 - Strong,
            (MapBiome.City, ResourceType.Special) => 1 + Strong,
            (MapBiome.Desert, ResourceType.Food) => 1 - Strong,   // 모래밭: 먹이 귀함, 모래·조약돌 많음
            (MapBiome.Desert, ResourceType.Soil) => 1 + Strong,
            (MapBiome.Cave, ResourceType.Food) => 1 - Strong,     // 동굴: 먹이 귀함, 박쥐 구아노·광물로 특수 많음
            (MapBiome.Cave, ResourceType.Special) => 1 + Strong,
            _ => 1f
        };
        public static float FarmGrowth => Current == MapBiome.Garden ? 1 + Strong : Current == MapBiome.Desert || Current == MapBiome.Cave ? 1 - Strong : 1f;
        // 사막 더위: 방 밖에서 일하면 피로 1.5배, 개미귀신 모래 구덩이로 방 밖 이동 -10%.
        public static float FatigueAt(bool indoors) => Current == MapBiome.Desert && !indoors ? 1.5f : 1f;
        public static float MoveAt(bool indoors) => Current == MapBiome.Desert && !indoors ? .9f : 1f;
        // 동굴 어둠: 방 밖 작업 -15%(방 = 불 밝힌 공간).
        public static float WorkAt(bool indoors) => Current == MapBiome.Cave && !indoors ? .85f : 1f;
        public static bool FarmAllowed => Current != MapBiome.City;
        public static float FishingYield => Current == MapBiome.Waterside ? 1 + Strong : 1f;
        public static float MoldSpread => Current == MapBiome.Forest ? 2f : 1f;
        public static float WildfireDamage => Current == MapBiome.Forest ? 2f : 1f;
        // 바닥 색조(지형 머티리얼 _BaseColor에 곱함).
        public static Color GroundTint(MapBiome b) => b switch
        {
            MapBiome.Forest => new Color(.72f, .82f, .62f), MapBiome.Garden => new Color(.85f, 1f, .75f),
            MapBiome.Waterside => new Color(1f, .95f, .78f), MapBiome.City => new Color(.72f, .72f, .74f),
            MapBiome.Desert => new Color(1f, .88f, .62f), MapBiome.Cave => new Color(.45f, .42f, .4f), _ => Color.white
        };
    }
}
