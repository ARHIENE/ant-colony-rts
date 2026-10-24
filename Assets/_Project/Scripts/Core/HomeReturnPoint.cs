using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Map;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Core
{
    public static class HomeReturnPoint
    {
        public static Vector3 Anchor => RocketCampaign.Instance != null && RocketCampaign.Instance.HasHome
            ? RocketCampaign.Instance.Home : AntColony.World.WorldMapManager.Instance?.HomePosition ?? Vector3.zero;

        // 본거지 지도 안의 NavMesh 경계 중 귀환 위치에 가장 가까운 통행 가능 점.
        public static bool TryGround(Vector3 from, out Vector3 point)
        {
            point = Anchor;
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            if (bounds.size.x <= 0 || bounds.size.z <= 0) return false;
            var mesh = NavMesh.CalculateTriangulation();
            var edges = new Dictionary<(int, int), int>();
            void Edge(int a, int b) { var key = a < b ? (a, b) : (b, a); edges.TryGetValue(key, out int count); edges[key] = count + 1; }
            for (int i = 0; i < mesh.indices.Length; i += 3)
            { Edge(mesh.indices[i], mesh.indices[i + 1]); Edge(mesh.indices[i + 1], mesh.indices[i + 2]); Edge(mesh.indices[i + 2], mesh.indices[i]); }
            float best = float.MaxValue;
            foreach (var edge in edges.Where(e => e.Value == 1))
            {
                var a = mesh.vertices[edge.Key.Item1]; var b = mesh.vertices[edge.Key.Item2];
                var delta = b - a;
                var near = a + delta * Mathf.Clamp01(Vector3.Dot(from - a, delta) / Mathf.Max(.0001f, delta.sqrMagnitude));
                if (near.x < bounds.min.x || near.z < bounds.min.z || near.x > bounds.max.x || near.z > bounds.max.z
                    || near.x > bounds.min.x + 2 && near.z > bounds.min.z + 2 && near.x < bounds.max.x - 2 && near.z < bounds.max.z - 2
                    || !NavMesh.SamplePosition(near, out var hit, 1, NavMesh.AllAreas)) continue;
                float distance = (from - hit.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance; point = hit.position;
            }
            return best < float.MaxValue;
        }
        public static bool TryAir(out Vector3 point)
        {
            point = Anchor;
            var bounds = HomeMapBuilder.CurrentWorldBounds;
            if (bounds.size.x <= 0 || bounds.size.z <= 0) return false;
            // 가장 가까운 빈 착륙 지점부터 탐색한다. 창고·징집소 존재와 무관하다.
            for (int radius = 0; radius <= 20; radius += 2)
                for (int i = 0; i < (radius == 0 ? 1 : 16); i++)
                {
                    float angle = i * Mathf.PI / 8;
                    var candidate = Anchor + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    if (!NavMesh.SamplePosition(candidate, out var hit, 1, NavMesh.AllAreas)) continue;
                    if (hit.position.x < bounds.min.x || hit.position.x > bounds.max.x
                        || hit.position.z < bounds.min.z || hit.position.z > bounds.max.z) continue;
                    if (Physics.OverlapSphere(hit.position + Vector3.up, 1).Any(c => c.GetComponentInParent<BuildingBase>() != null)) continue;
                    point = hit.position; return true;
                }
            return false;
        }
    }
}
