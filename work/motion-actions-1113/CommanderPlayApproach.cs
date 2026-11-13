using AntColony.Buildings;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        RecreationSpot approachingPlaySpot;
        Vector3 playApproach;
        float? playStoppingDistance;
        void ResetPlayApproach()
        {
            if (playStoppingDistance.HasValue && Agent) Agent.stoppingDistance = playStoppingDistance.Value;
            playStoppingDistance = null;
            approachingPlaySpot = null;
        }
        public bool AtPlaySpot => PlaySpot && approachingPlaySpot == PlaySpot
            && Vector3.ProjectOnPlane(Position - playApproach, Vector3.up).sqrMagnitude <= .04f;

        bool ReachPlaySpot(RecreationSpot spot)
        {
            if (!spot || spot.IsDead) return false;
            if (approachingPlaySpot != spot)
            {
                if (!TryPlayApproach(spot, out playApproach)) return false;
                approachingPlaySpot = spot;
            }
            if (Vector3.ProjectOnPlane(Position - playApproach, Vector3.up).sqrMagnitude <= .04f)
            { StopMoving(); if (playStoppingDistance.HasValue && Agent) Agent.stoppingDistance = playStoppingDistance.Value; playStoppingDistance = null; return true; }
            if (!IsFlying && Agent && !playStoppingDistance.HasValue)
            { playStoppingDistance = Agent.stoppingDistance; Agent.stoppingDistance = .05f; }
            bool walking = IsFlying ? !HasReachedDestination() : Agent.enabled && Agent.isOnNavMesh && (Agent.pathPending || Agent.hasPath);
            if (!walking || !IsFlying && !Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete) SetMoveDestination(playApproach);
            return false;
        }

        bool TryPlayApproach(RecreationSpot spot, out Vector3 point)
        {
            point = Position;
            var collider = spot.GetComponent<Collider>();
            var bounds = collider ? collider.bounds : new Bounds(spot.Position, Vector3.one);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + .4f;
            Vector3 outward = Vector3.ProjectOnPlane(Position - spot.Position, Vector3.up).normalized;
            if (outward.sqrMagnitude < .01f) outward = Vector3.forward;
            for (int i = 0; i < 12; i++)
            {
                Vector3 desired = spot.Position + Quaternion.Euler(0, i * 30, 0) * outward * radius;
                if (!NavMesh.SamplePosition(desired, out var hit, .4f, NavMesh.AllAreas) || !CanReach(hit.position)) continue;
                bool occupied = false;
                foreach (var other in spot.Users)
                    if (other && other != this && Vector3.ProjectOnPlane(other.Position-hit.position, Vector3.up).sqrMagnitude < .64f) { occupied = true; break; }
                if (occupied) continue;
                point = hit.position; return true;
            }
            return false;
        }
    }
}
