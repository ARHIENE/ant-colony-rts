using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        private int constructionStep;
        public int ConstructionStep => constructionStep;
        public string ConstructionClip => constructionStep == 1 ? "Saw" : "Tool_Hammer";
        private bool ConstructionPoint(int step, out Vector3 point)
        {
            point = Position;
            if (targetConstruction == null) return false;
            var collider = targetConstruction.GetComponent<Collider>();
            var bounds = collider != null ? collider.bounds : new Bounds(targetConstruction.Position, Vector3.one);
            var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) + .55f, .9f, 3.5f);
            var angle = step * Mathf.PI * 2 / 3;
            var desired = targetConstruction.Position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            if (IsFlying) { point = desired; return true; }
            if (!NavMesh.SamplePosition(desired, out var hit, 1.2f, NavMesh.AllAreas) || !CanReach(hit.position)) return false;
            point = hit.position; return true;
        }
    }
}
