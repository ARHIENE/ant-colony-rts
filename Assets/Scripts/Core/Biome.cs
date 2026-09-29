using AntColony.Data;
using UnityEngine;

namespace AntColony.Core
{
    // 로컬 맵 바이옴(2026-09-28): 새 게임마다 4종 중 하나. None은 옛 저장·검사용(보정 없음).
    public enum MapBiome { None, Forest, Garden, Waterside, City }

    // 바이옴 차이는 강하게(±60%, 확정). 숲 바닥은 확정, 나머지 3종은 잠정안.
    // ponytail: 바이옴 전용 이벤트(발자국·물뿌리개·배수구 역류·쓰레기 더미)·물가 형태·숲 그늘은 다음 단계.
    public static class BiomeRules
    {
        public const float Strong = .6f;
        public static MapBiome Current => GameSession.Exists ? GameSession.Instance.Options.biome : MapBiome.None;
        public static MapBiome Random() => (MapBiome)UnityEngine.Random.Range(1, 5);

        public static string Name(MapBiome b) => b switch
        {
            MapBiome.Forest => "숲 바닥", MapBiome.Garden => "뒷마당 정원", MapBiome.Waterside => "물가 모래밭", MapBiome.City => "도시 구석", _ => "기본"
        };

        // 맵 생성 때 로컬 자원 노드 양에 곱한다.
        public static float NodeMultiplier(ResourceType type) => (Current, type) switch
        {
            (MapBiome.Forest, ResourceType.Food) => 1 + Strong,
            (MapBiome.Forest, ResourceType.Special) => 1 - Strong,
            (MapBiome.Waterside, ResourceType.Soil) => 1 - Strong,
            (MapBiome.City, ResourceType.Special) => 1 + Strong,
            _ => 1f
        };
        public static float FarmGrowth => Current == MapBiome.Garden ? 1 + Strong : 1f;
        public static bool FarmAllowed => Current != MapBiome.City;
        public static float FishingYield => Current == MapBiome.Waterside ? 1 + Strong : 1f;
        public static float MoldSpread => Current == MapBiome.Forest ? 2f : 1f;
        public static float WildfireDamage => Current == MapBiome.Forest ? 2f : 1f;
        // 바닥 색조(지형 머티리얼 _BaseColor에 곱함).
        public static Color GroundTint(MapBiome b) => b switch
        {
            MapBiome.Forest => new Color(.72f, .82f, .62f), MapBiome.Garden => new Color(.85f, 1f, .75f),
            MapBiome.Waterside => new Color(1f, .95f, .78f), MapBiome.City => new Color(.72f, .72f, .74f), _ => Color.white
        };
    }
}
