using System.Collections.Generic;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Map
{
    // 2026-10-10 자원 분화: 본거지 맵의 원재료 노드. 같은 시드면 같은 자리에 같은 노드가 생겨(새 게임·불러오기 공통)
    // 저장은 "mat:순번" 키로 잔량만 덮어쓴다. 돌·광석은 고갈되고, 식물(목재·잎·섬유·수지·거미줄)은 다시 자란다.
    public sealed class MaterialDeposit : MonoBehaviour { }

    public static class MaterialDeposits
    {
        public static readonly List<ResourceNode> Spawned = new List<ResourceNode>();

        private readonly struct Spec
        {
            public readonly ResourceType type; public readonly int count; public readonly float amount, regrow; public readonly Color color;
            public Spec(ResourceType type, int count, float amount, float regrow, Color color) { this.type = type; this.count = count; this.amount = amount; this.regrow = regrow; this.color = color; }
        }
        // ponytail: 종류·개수·양·재성장 시간은 잠정(기획 미정). 맵 크기와 무관하게 같은 개수, 거리만 맵 크기에 비례.
        private static readonly Spec[] Specs =
        {
            new Spec(ResourceType.Stone, 3, 300, 0, new Color(.55f, .55f, .55f)),
            new Spec(ResourceType.Sand, 2, 200, 0, new Color(.86f, .78f, .55f)),
            new Spec(ResourceType.Clay, 2, 200, 0, new Color(.7f, .45f, .3f)),
            new Spec(ResourceType.Wood, 4, 120, 240, new Color(.45f, .3f, .15f)),
            new Spec(ResourceType.Leaf, 3, 80, 120, new Color(.3f, .65f, .25f)),
            new Spec(ResourceType.Fiber, 3, 80, 180, new Color(.6f, .7f, .35f)),
            new Spec(ResourceType.Resin, 2, 40, 300, new Color(.85f, .6f, .15f)),
            new Spec(ResourceType.Chitin, 1, 150, 0, new Color(.35f, .2f, .15f)),
            new Spec(ResourceType.Cobweb, 1, 30, 300, new Color(.92f, .92f, .95f)),
            new Spec(ResourceType.IronOre, 2, 150, 0, new Color(.6f, .35f, .3f)),
            new Spec(ResourceType.CopperOre, 1, 120, 0, new Color(.75f, .5f, .3f)),
            new Spec(ResourceType.Coal, 2, 200, 0, new Color(.12f, .12f, .12f)),
            new Spec(ResourceType.Sulfur, 1, 80, 0, new Color(.9f, .85f, .2f)),
        };

        // 지역별 편차(±60%): 기초 생활 재료는 어디에나 있고 산업 자원은 바이옴마다 많고 적다.
        public static float Multiplier(MapBiome biome, ResourceType type)
        {
            bool plant = type == ResourceType.Wood || type == ResourceType.Leaf || type == ResourceType.Fiber || type == ResourceType.Resin;
            bool ore = type == ResourceType.IronOre || type == ResourceType.CopperOre || type == ResourceType.Coal || type == ResourceType.Sulfur;
            float s = BiomeRules.Strong;
            return biome switch
            {
                MapBiome.Forest => plant ? 1 + s : type == ResourceType.Stone ? 1 - s : 1,
                MapBiome.Garden => plant ? 1 + s * .5f : 1,
                MapBiome.Waterside => type == ResourceType.Clay || type == ResourceType.Sand ? 1 + s : type == ResourceType.Stone ? 1 - s : 1,
                MapBiome.City => type == ResourceType.CopperOre || type == ResourceType.Sand ? 1 + s : plant ? 1 - s : 1,
                MapBiome.Desert => type == ResourceType.Sand || type == ResourceType.Stone ? 1 + s : plant ? 1 - s : 1,
                MapBiome.Cave => ore ? 1 + s : plant ? 1 - s : 1,
                _ => 1
            };
        }

        public static void Spawn(Vector3 home, float mapScale, int seed, MapBiome biome)
        {
            Spawned.Clear();
            var rng = new System.Random(seed ^ 0x5eed10);
            int index = 0;
            foreach (var spec in Specs)
                for (int k = 0; k < spec.count; k++, index++)
                {
                    ResourceNode node = null;
                    for (int attempt = 0; attempt < 12 && node == null; attempt++)
                    {
                        var angle = (float)rng.NextDouble() * Mathf.PI * 2; var radius = (HomeMapBuilder.HomeFlatRadius * .6f + (float)rng.NextDouble() * 40f) * Mathf.Max(.6f, mapScale);
                        var at = home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                        if (!NavMesh.SamplePosition(at, out var hit, 6f, NavMesh.AllAreas)) continue;
                        node = Create(spec, index, hit.position, spec.amount * Multiplier(biome, spec.type));
                    }
                    Spawned.Add(node); // 자리를 못 찾아도 순번을 유지한다(null).
                }
        }

        private static ResourceNode Create(Spec spec, int index, Vector3 at, float amount)
        {
            var go = GameObject.CreatePrimitive(spec.regrow > 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = $"Material {spec.type} {index:00}"; go.SetActive(false);
            go.transform.position = at + Vector3.up * .5f; go.transform.localScale = spec.regrow > 0 ? new Vector3(.8f, .6f, .8f) : Vector3.one * 1.1f;
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<Renderer>().material.color = spec.color;
            go.AddComponent<MaterialDeposit>();
            var node = go.AddComponent<ResourceNode>();
            node.ConfigureNatural(spec.type, amount, spec.regrow);
            go.SetActive(true);
            return node;
        }
    }
}
