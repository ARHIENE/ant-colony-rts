using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using AntColony.Data;

namespace AntColony.Buildings
{
    // Use the visible model's front edge, not the centre of an enlarged building.
    public sealed class FurnitureUseSurface : MonoBehaviour
    {
        Transform measuredModel;
        Vector3 localEdge, localSurface;
        float frontSign=-1;
        public static bool Supported(BuildingBase building) => building && building.Data
            && building.Data.kind is BuildingKind.Bookshelf or BuildingKind.Carpentry;

        public static bool TryPoint(BuildingBase building, out Vector3 standing, out Vector3 surface)
        {
            standing=surface=default;
            if(!Supported(building)) return false;
            var visual=building.GetComponent<FacilityVisual>();
            if(!visual || !visual.Model) return false;
            var cache=building.GetComponent<FurnitureUseSurface>();
            if(!cache) cache=building.gameObject.AddComponent<FurnitureUseSurface>();
            if(cache.measuredModel!=visual.Model && !cache.Measure(building,visual)) return false;
            surface=building.transform.TransformPoint(cache.localSurface);
            var desired=building.transform.TransformPoint(cache.localEdge)+building.transform.forward*(cache.frontSign*.48f);
            if(!NavMesh.SamplePosition(desired,out var hit,.25f,NavMesh.AllAreas)) return false;
            standing=hit.position;
            return true;
        }
        bool Measure(BuildingBase building, FacilityVisual visual)
        {
            var vertices=visual.Model.GetComponentsInChildren<MeshFilter>()
                .Where(f=>f.sharedMesh && f.sharedMesh.isReadable)
                .SelectMany(f=>f.sharedMesh.vertices.Select(v=>building.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            if(vertices.Length==0) return false;
            float front=vertices.Min(v=>v.z), back=vertices.Max(v=>v.z);
            float x=(vertices.Min(v=>v.x)+vertices.Max(v=>v.x))*.5f;
            float depth=back-front;
            frontSign=building.Data.kind==BuildingKind.Bookshelf?1:-1;
            var top=vertices.Where(v=>Mathf.Abs(v.x-x)<.3f && (frontSign<0?v.z<front+depth*.35f:v.z>back-depth*.35f)).ToArray();
            if(top.Length==0) return false;
            localSurface=new Vector3(x,top.Max(v=>v.y),frontSign<0?front+depth*.12f:back-depth*.12f);
            localEdge=new Vector3(x,vertices.Min(v=>v.y),frontSign<0?front:back);
            measuredModel=visual.Model;
            return true;
        }
    }
}
