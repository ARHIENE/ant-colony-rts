using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Map
{
    // NavRoute 경유지를 차례로 따라간다. 유닛 로직보다 먼저 돌아 경유지 도착을 목적지 도착으로 오인하지 않게
    // 경유지 2m 앞에서 다음 경유지로 바꾼다. 다른 명령(ResetPath·새 목적지)이 끼어들면 그만 따라간다.
    [DefaultExecutionOrder(-100)]
    public class NavRouteFollower : MonoBehaviour
    {
        private NavMeshAgent agent;
        private readonly Queue<Vector3> rest = new Queue<Vector3>();
        private Vector3 current, final;
        private bool active;

        public bool Following(Vector3 to) => active && Flat(to - final) < 3f;
        public void Clear() { active = false; rest.Clear(); }

        public void Follow(NavMeshAgent navAgent, List<Vector3> route, Vector3 to)
        {
            agent = navAgent; Clear();
            for (var i = 1; i < route.Count; i++) rest.Enqueue(route[i]);
            final = to; active = true;
            Go(route[0]);
        }

        private void Go(Vector3 point) { current = point; agent.SetDestination(point); }

        private void Update()
        {
            if (!active) return;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || !agent.pathPending && !agent.hasPath || Flat(agent.destination - current) > 1.5f)
            { Clear(); return; }
            if (agent.pathPending || agent.remainingDistance > 2f) return;
            if (rest.Count == 0) { active = false; return; }
            Go(rest.Dequeue());
        }

        private static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;
    }
}
