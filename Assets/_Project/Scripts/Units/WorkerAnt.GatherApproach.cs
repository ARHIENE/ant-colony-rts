using AntColony.Data;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

namespace AntColony.Units
{
    public partial class WorkerAnt
    {
        // Resource centres are often walkable: the NavMesh does not describe the visible rock.
        // Pick a reachable edge instead of placing the worker inside its work target.
        public bool TryGatherApproach(ResourceNode node, out Vector3 destination)
        {
            destination=Position;
            if(node==null)return false;
            if(node.IsLooseCargo||node.GetComponent<AntColony.Buildings.BuildingBase>()!=null)
                return TryWorkApproach(node.transform.position,out destination);
            var bounds=new Bounds(node.transform.position,Vector3.zero);bool found=false;
            foreach(var r in node.GetComponentsInChildren<MeshRenderer>())
                if(r.enabled){if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
            bool tree=node.ResourceType==ResourceType.Wood||node.ResourceType==ResourceType.Resin;
            float radius=tree?.65f:Mathf.Clamp(Mathf.Max(bounds.extents.x,bounds.extents.z),.3f,3f);
            radius+=.35f;
            var centre=node.transform.position;
            var direction=Vector3.ProjectOnPlane(Position-centre,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=Vector3.forward;
            for(int i=0;i<12;i++)
            {
                int step=(i+1)/2*(i%2==0?1:-1);
                var candidate=centre+Quaternion.Euler(0,step*30,0)*direction*radius;
                if(IsFlying){destination=candidate;return true;}
                // Resource transforms can sit above the terrain (e.g. soil piles).
                // Sample the walking surface, not the elevated visual pivot.
                if(Physics.Raycast(candidate+Vector3.up*8,Vector3.down,out var ground,24,1<<8,QueryTriggerInteraction.Ignore))
                    candidate.y=ground.point.y;
                if(!NavMesh.SamplePosition(candidate,out var hit,.65f,NavMesh.AllAreas))continue;
                if(Vector3.ProjectOnPlane(hit.position-centre,Vector3.up).magnitude<radius-.2f)continue;
                if(!CanReach(hit.position))continue;
                destination=hit.position;return true;
            }
            return false;
        }
    }
}
