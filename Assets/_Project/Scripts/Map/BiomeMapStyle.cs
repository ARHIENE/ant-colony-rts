using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Map
{
    // 바이옴별 맵 모습(2026-10-03): 높이별 바닥 텍스처, 장식물(나무·돌·풀·소품), 물.
    // Resources/Biomes/{바이옴}.asset 에서 읽는다. 에셋 팩이 없는 PC에서는 빈 칸을 건너뛰고 씬 기본값을 쓴다.
    [CreateAssetMenu(menuName = "Ant Colony/Biome Map Style")]
    public sealed class BiomeMapStyle : ScriptableObject
    {
        public List<MapGenerator.Layer> layers = new List<MapGenerator.Layer>();
        public List<MapGenerator.SpawnObject> spawns = new List<MapGenerator.SpawnObject>();
        public bool water;
        [Range(0, 1)] public float waterHeight = .2f;
        public Material waterMaterial;

        public static BiomeMapStyle For(AntColony.Core.MapBiome biome) => Resources.Load<BiomeMapStyle>("Biomes/" + biome);
    }
}
