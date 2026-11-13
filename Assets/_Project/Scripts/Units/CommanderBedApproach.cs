using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        Dormitory approachingBed;
        Vector3 bedApproach;
        float? bedStoppingDistance;
        bool bedArrived;
        bool? bedNavigationUpdates;
        public bool AtSleepingPlace => IsAsleep && (SleepsRough || bedArrived);

        void ResetBedApproach()
        {
            if (bedNavigationUpdates.HasValue && Agent)
            {
                if (Agent.enabled && Agent.isOnNavMesh) Agent.Warp(Agent.nextPosition);
                Agent.updatePosition=bedNavigationUpdates.Value;
            }
            bedNavigationUpdates=null;
            if (bedStoppingDistance.HasValue && Agent) Agent.stoppingDistance = bedStoppingDistance.Value;
            bedStoppingDistance = null; approachingBed = null; bedArrived = false;
        }

        // Reach a free side of the allocated bed before showing its rest pose.
        bool ReachBed(Dormitory bed, out bool reachable)
        {
            reachable = true;
            if (bedArrived && approachingBed == bed)
            {
                if (TrySleepingPose(out var resting, out _))
                    transform.position=new Vector3(resting.x, bed.GetComponent<Renderer>().bounds.min.y, resting.z);
                return true;
            }
            if (approachingBed != bed)
            {
                ResetBedApproach();
                var col = bed.GetComponent<Collider>();
                var bounds = col ? col.bounds : new Bounds(bed.Position, Vector3.one);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + .35f;
                var outward = Vector3.ProjectOnPlane(Position-bed.Position, Vector3.up).normalized;
                if (outward.sqrMagnitude < .01f) outward = bed.transform.right;
                bool found = false;
                for (int i=0; i<12; i++)
                {
                    var desired = bed.Position + Quaternion.Euler(0,i*30,0)*outward*radius;
                    desired.y = bounds.min.y;
                    if (!NavMesh.SamplePosition(desired,out var hit,.4f,NavMesh.AllAreas) || !CanReach(hit.position)) continue;
                    bedApproach=hit.position; found=true; break;
                }
                if (!found) { reachable=false; return false; }
                approachingBed=bed;
            }
            // A newly placed bed can carve the NavMesh after the initial route was chosen.
            if (!IsFlying && NavMesh.SamplePosition(bedApproach,out var currentApproach,.75f,NavMesh.AllAreas))
                bedApproach=currentApproach.position;
            if (Vector3.ProjectOnPlane(Position-bedApproach,Vector3.up).sqrMagnitude <= .04f)
            {
                StopMoving(); bedArrived=true;
                if (bedStoppingDistance.HasValue && Agent) Agent.stoppingDistance=bedStoppingDistance.Value;
                bedStoppingDistance=null;
                if (TrySleepingPose(out var resting,out _) && Agent)
                {
                    bedNavigationUpdates=Agent.updatePosition; Agent.updatePosition=false;
                    transform.position=new Vector3(resting.x,bed.GetComponent<Renderer>().bounds.min.y,resting.z);
                }
                return true;
            }
            bedArrived=false;
            if (!IsFlying && Agent && !bedStoppingDistance.HasValue)
            { bedStoppingDistance=Agent.stoppingDistance; Agent.stoppingDistance=.05f; }
            bool walking=IsFlying ? !HasReachedDestination() : Agent && Agent.enabled && Agent.isOnNavMesh && (Agent.pathPending || Agent.hasPath);
            if (!walking) SetMoveDestination(bedApproach);
            return false;
        }

        public bool TrySleepingPose(out Vector3 support, out Vector3 headDirection)
        {
            support=default; headDirection=default;
            if (!IsAsleep || !bedArrived || !approachingBed || Dormitory.Of(this)!=approachingBed) return false;
            var visual=approachingBed.GetComponent<FacilityVisual>();
            if (!visual || !visual.Model) return false;
            var authoredSurface = visual.Model.Find("SleepingSurface");
            if (authoredSurface)
            {
                support = authoredSurface.position;
                headDirection = -approachingBed.transform.forward;
                return true;
            }
            var surfaces=visual.Model.GetComponentsInChildren<Renderer>()
                .Where(r=>r.name.IndexOf("mattress",System.StringComparison.OrdinalIgnoreCase)>=0 || r.name.StartsWith("Woven strip"))
                .OrderBy(r=>r.bounds.center.y).ToArray();
            if (surfaces.Length==0) return false;
            int slot=System.Array.IndexOf(approachingBed.Residents.ToArray(),this);
            var kind=approachingBed.Data.kind;
            var surface=surfaces[kind==BuildingKind.BunkBed ? Mathf.Clamp(slot,0,surfaces.Length-1) : 0].bounds;
            if (kind==BuildingKind.SleepingMat) foreach(var r in surfaces) surface.Encapsulate(r.bounds);
            support=new Vector3(surface.center.x,surface.max.y+.015f,surface.center.z);
            headDirection=-approachingBed.transform.forward;
            if (kind==BuildingKind.DoubleBed) support+=approachingBed.transform.right*(slot==0 ? -.25f : .25f)*surface.size.x;
            return true;
        }
    }
}
