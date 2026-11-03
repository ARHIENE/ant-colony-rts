using AntColony.Map;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.World
{
    public partial class WildMonster
    {
        public const float RetreatTimeout = 30f;
        public bool Retreating { get; private set; }
        private float retreatLeft;
        private Vector3 retreatExit;

        internal void BeginRetreat()
        {
            if (IsDead || Retreating) return;
            Retreating = true; retreatLeft = RetreatTimeout;
            currentTarget = null; StopMoving();
            var map = MapGenerator.Active;
            retreatExit = Position;
            if (map != null)
            {
                var p = map.transform.InverseTransformPoint(Position);
                var edge = Mathf.Min(p.x, map.BaseXSize - p.x, p.z, map.BaseZSize - p.z);
                if (edge == p.x) p.x = .5f;
                else if (edge == map.BaseXSize - p.x) p.x = map.BaseXSize - .5f;
                else if (edge == p.z) p.z = .5f;
                else p.z = map.BaseZSize - .5f;
                retreatExit = map.transform.TransformPoint(p);
            }
            else retreatExit += (Position.sqrMagnitude > 1 ? Position.normalized : Vector3.forward) * 50;
            if (!IsFlying && NavMesh.SamplePosition(retreatExit, out var hit, 4, NavMesh.AllAreas)) retreatExit = hit.position;
        }

        private void TickRetreat(float dt)
        {
            currentTarget = null;
            retreatLeft -= dt;
            var delta = retreatExit - Position; delta.y = 0;
            if (retreatLeft <= 0 || delta.sqrMagnitude <= 1)
            {
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            RootRemaining = Mathf.Max(0, RootRemaining - dt);
            if (IsFlying) transform.position = Vector3.MoveTowards(Position, retreatExit, moveSpeed * dt);
            else if (CanMove())
            {
                agent.stoppingDistance = 0; agent.speed = moveSpeed;
                RequestDestination(retreatExit);
            }
        }
    }
}
