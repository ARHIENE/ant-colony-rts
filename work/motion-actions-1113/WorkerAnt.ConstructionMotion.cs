using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        private int constructionStep;
        private float? constructionStoppingDistance;
        public int ConstructionStep => constructionStep;
        // Keep the original Meshy hammer swing at every contact point.
        public string ConstructionClip => "Tool_Hammer";

        private void PreciseConstructionArrival()
        {
            if (constructionStoppingDistance.HasValue || IsFlying || !Agent || !Agent.enabled || !Agent.isOnNavMesh) return;
            constructionStoppingDistance = Agent.stoppingDistance;
            Agent.stoppingDistance = .025f;
        }

        private void RestoreConstructionArrival()
        {
            if (constructionStoppingDistance.HasValue && Agent) Agent.stoppingDistance = constructionStoppingDistance.Value;
            constructionStoppingDistance = null;
        }

        private bool ConstructionPoint(int step, out Vector3 point, bool reposition = false)
        {
            point = Position;
            if (!targetConstruction) return false;
            var collider = targetConstruction.GetComponent<Collider>();
            var bounds = collider ? collider.bounds : new Bounds(targetConstruction.Position, Vector3.one);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + .55f;
            var centre = targetConstruction.Position;
            // Start on the nearest side, then alternate reachable sides of the site.
            var outward = Vector3.ProjectOnPlane(Position - centre, Vector3.up).normalized;
            if (outward.sqrMagnitude < .01f) outward = Vector3.forward;
            if (reposition) outward = Quaternion.Euler(0, 90, 0) * outward;
            for (int i = 0; i < 12; i++)
            {
                int offset = (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
                var direction = Quaternion.Euler(0, offset * 30, 0) * outward;
                var desired = centre + direction * radius;
                if (Physics.Raycast(desired + Vector3.up * 8, Vector3.down, out var ground, 24, 1 << 8, QueryTriggerInteraction.Ignore))
                    desired.y = ground.point.y;
                if (GetComponent<MeshyNativeMotionPlayer>() && MeshyWorkContact.TryRadius(targetConstruction, ConstructionClip, direction, desired.y, out var fitted))
                    desired = new Vector3(centre.x + direction.x * fitted, desired.y, centre.z + direction.z * fitted);
                if (IsFlying) { point = desired; return true; }
                if (!NavMesh.SamplePosition(desired, out var hit, .3f, NavMesh.AllAreas)) continue;
                // A projection on the opposite side of a wall is not a valid work point.
                if (Vector3.ProjectOnPlane(hit.position - desired, Vector3.up).sqrMagnitude > .01f) continue;
                if (!CanReach(hit.position)) continue;
                if (!ConstructionPathClear(hit.position, bounds)) continue;
                if (reposition && Vector3.ProjectOnPlane(hit.position - Position, Vector3.up).sqrMagnitude < .36f) continue;
                point = hit.position;
                return true;
            }
            return false;
        }

        private bool ConstructionPathClear(Vector3 destination, Bounds footprint)
        {
            // Blueprints do not carve the NavMesh. Reject paths cutting through the site.
            var path = new NavMeshPath();
            if (!Agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete) return false;
            footprint.Expand(new Vector3(.1f, 0, .1f));
            var end = new Vector3(destination.x, footprint.center.y, destination.z);
            if (footprint.Contains(end)) return false;
            for (int i = 1; i < path.corners.Length; i++)
            {
                var a = new Vector3(path.corners[i-1].x, footprint.center.y, path.corners[i-1].z);
                var b = new Vector3(path.corners[i].x, footprint.center.y, path.corners[i].z);
                if (i == 1 && footprint.Contains(a)) continue; // Allow a worker already inside to leave.
                var delta = b - a;
                if (delta.sqrMagnitude > .0001f && footprint.IntersectRay(new Ray(a, delta.normalized), out float distance)
                    && distance < delta.magnitude - .02f) return false;
            }
            return true;
        }
    }
}
