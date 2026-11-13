using AntColony.Buildings;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        ScienceLab researchApproachLab;
        Vector3 researchApproachPoint;
        float? researchStoppingDistance;

        public bool AtResearchDesk => ScienceAssignment && researchApproachLab == ScienceAssignment
            && Vector3.ProjectOnPlane(Position - researchApproachPoint, Vector3.up).sqrMagnitude <= .04f;

        void ResetResearchApproach()
        {
            if (researchStoppingDistance.HasValue && Agent) Agent.stoppingDistance = researchStoppingDistance.Value;
            researchStoppingDistance = null;
            researchApproachLab = null;
        }

        bool ReachResearchDesk(ScienceLab lab)
        {
            if (!lab || lab.IsDead) { ResetResearchApproach(); return false; }
            if (researchApproachLab != lab)
            {
                ResetResearchApproach();
                var col = lab.GetComponent<Collider>();
                var bounds = col ? col.bounds : new Bounds(lab.Position, Vector3.one);
                var visual = lab.GetComponent<FacilityVisual>();
                if (visual && visual.Model)
                {
                    var renderers = visual.Model.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    }
                }
                var outward = Vector3.ProjectOnPlane(Position-bounds.center, Vector3.up).normalized;
                if (outward.sqrMagnitude < .01f) outward = lab.transform.forward;
                bool found = false;
                for (int i=0; i<12; i++)
                {
                    var direction = Quaternion.Euler(0,i*30,0)*outward;
                    float edge = Mathf.Min(bounds.extents.x / Mathf.Max(.001f,Mathf.Abs(direction.x)),
                        bounds.extents.z / Mathf.Max(.001f,Mathf.Abs(direction.z)));
                    var desired = bounds.center + direction*(edge+.35f);
                    desired.y = bounds.min.y;
                    if (!NavMesh.SamplePosition(desired,out var hit,.4f,NavMesh.AllAreas) || !CanReach(hit.position)) continue;
                    researchApproachPoint=hit.position; found=true; break;
                }
                if (!found) return false;
                researchApproachLab=lab;
            }
            if (!IsFlying && NavMesh.SamplePosition(researchApproachPoint,out var currentApproach,.75f,NavMesh.AllAreas))
                researchApproachPoint=currentApproach.position;
            if (Vector3.ProjectOnPlane(Position-researchApproachPoint,Vector3.up).sqrMagnitude <= .04f)
            {
                StopMoving();
                if (researchStoppingDistance.HasValue && Agent) Agent.stoppingDistance=researchStoppingDistance.Value;
                researchStoppingDistance=null;
                return true;
            }
            if (!IsFlying && Agent && !researchStoppingDistance.HasValue)
            { researchStoppingDistance=Agent.stoppingDistance; Agent.stoppingDistance=.05f; }
            bool walking=IsFlying ? !HasReachedDestination() : Agent && Agent.enabled && Agent.isOnNavMesh && (Agent.pathPending || Agent.hasPath);
            if (!walking) SetMoveDestination(researchApproachPoint);
            return false;
        }
    }
}
