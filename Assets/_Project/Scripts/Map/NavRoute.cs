using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Map
{
    // Unity 길찾기는 탐색 노드 한도가 있어 계단식 지형의 막다른 절벽이 많은 먼 길(보통·큰 맵 약 400m+)은
    // 실제로 이어져 있어도 PathPartial로 끝난다(2026-10-22). 그럴 때 지형 격자로 대략 경로를 구해 경유지마다 나눠 계산한다.
    public static class NavRoute
    {
        const int Stride = 2;           // 대략 경로 격자 간격(m). 경사로 안쪽 칸이 가운데 3줄이라 4로 두면 격자선이 비켜 가는 경사로가 생긴다
        const int WaypointEvery = 40;   // 경유지 간격(격자 칸 수, 약 80m)
        const float CheckDistance = 100f; // ponytail: 이보다 가까운 이동은 한도에 안 걸린다고 보고 바로 보낸다(실측 실패는 400m+). 한도 실패가 더 짧은 길에서 나오면 낮춘다.

        // from→to 걸어서 가는 길이. 못 가면 무한대. route를 주면 경유지(마지막 = to)를 채운다.
        public static float Length(Vector3 from, Vector3 to, List<Vector3> route = null)
        {
            route?.Clear();
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)) return float.PositiveInfinity;
            if (path.status == NavMeshPathStatus.PathComplete) { route?.Add(to); return Corners(path); }
            var map = MapGenerator.Active; var points = new List<Vector3>();
            if (path.status != NavMeshPathStatus.PathPartial || map == null || !map.CoarseRoute(from, to, Stride, WaypointEvery, points))
                return float.PositiveInfinity;
            points.Add(to);
            var total = 0f; var a = from;
            for (var i = 0; i < points.Count; i++)
            {
                var b = points[i];
                if (i < points.Count - 1) { if (!NavMesh.SamplePosition(b, out var hit, 2f, NavMesh.AllAreas)) return float.PositiveInfinity; b = hit.position; }
                if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return float.PositiveInfinity;
                total += Corners(path); route?.Add(b); a = b;
            }
            return total;
        }

        public static bool Reachable(Vector3 from, Vector3 to) => !float.IsPositiveInfinity(Length(from, to));

        // agent.SetDestination 대신 쓴다. 먼 길이 한 번에 안 찾아지면 경유지를 차례로 따라간다(NavRouteFollower).
        public static void SetDestination(NavMeshAgent agent, Vector3 to)
        {
            var follower = agent.GetComponent<NavRouteFollower>();
            if (follower != null && follower.Following(to)) return; // 매 프레임 같은 목적지를 다시 줘도 진행을 잃지 않게
            if (follower != null) follower.Clear();
            // 이미 같은 곳(3m 안)으로 가는 중이면 다시 확인하지 않는다(움직이는 대상을 매 프레임 쫓는 경우).
            var d = to - agent.destination; var same = (agent.hasPath || agent.pathPending) && new Vector2(d.x, d.z).sqrMagnitude < 9f;
            if (!same && (to - agent.transform.position).sqrMagnitude > CheckDistance * CheckDistance)
            {
                var route = new List<Vector3>();
                if (!float.IsPositiveInfinity(Length(agent.nextPosition, to, route)) && route.Count > 1)
                {
                    (follower != null ? follower : agent.gameObject.AddComponent<NavRouteFollower>()).Follow(agent, route, to);
                    return;
                }
            }
            agent.SetDestination(to);
        }

        static float Corners(NavMeshPath path)
        {
            var length = 0f; var c = path.corners;
            for (var i = 1; i < c.Length; i++) length += Vector3.Distance(c[i - 1], c[i]);
            return length;
        }
    }
}
