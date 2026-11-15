using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using AntColony.Data;

namespace AntColony.Buildings
{
    // Use the visible model's front edge, not the centre of an enlarged building.
    public sealed class FurnitureUseSurface : MonoBehaviour
    {
        static readonly float[] FrontOffsets={0f,.5f,-.5f,1f,-1f};
        Transform measuredModel;
        Vector3 localEdge, localSurface;
        float localHalfWidth;
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
            // 외형의 바닥과 NavMesh 높이는 경사지/기초 높이만큼 다를 수 있다.
            // 높이는 투영하되, 가구 반대편이나 막힌 옆 칸으로 작업 지점을 옮기지는 않는다.
            float sideRoom=Mathf.Min(.75f,Mathf.Max(0,building.transform.TransformVector(Vector3.right*cache.localHalfWidth).magnitude-.25f));
            foreach(float fraction in FrontOffsets)
            {
                var offset=building.transform.right*(sideRoom*fraction);
                var candidate=desired+offset;
                if(!NavMesh.SamplePosition(candidate,out var hit,2f,NavMesh.AllAreas)) continue;
                var horizontal=hit.position-candidate; horizontal.y=0;
                if(horizontal.sqrMagnitude>.35f*.35f) continue;
                standing=hit.position;
                surface+=offset;
                return true;
            }
            return false;
        }
        bool Measure(BuildingBase building, FacilityVisual visual)
        {
            var vertices=visual.Model.GetComponentsInChildren<MeshFilter>()
                .Where(f=>f.sharedMesh)
                .SelectMany(f=>(f.sharedMesh.isReadable ? f.sharedMesh.vertices : BoundsCorners(f.sharedMesh.bounds))
                    .Select(v=>building.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            if(vertices.Length==0) return false;
            float front=vertices.Min(v=>v.z), back=vertices.Max(v=>v.z);
            float x=(vertices.Min(v=>v.x)+vertices.Max(v=>v.x))*.5f;
            localHalfWidth=(vertices.Max(v=>v.x)-vertices.Min(v=>v.x))*.5f;
            float depth=back-front;
            frontSign=building.Data.kind==BuildingKind.Bookshelf?1:-1;
            var top=vertices.Where(v=>Mathf.Abs(v.x-x)<.3f && (frontSign<0?v.z<front+depth*.35f:v.z>back-depth*.35f)).ToArray();
            localSurface=new Vector3(x,(top.Length>0?top:vertices).Max(v=>v.y),frontSign<0?front+depth*.12f:back-depth*.12f);
            localEdge=new Vector3(x,vertices.Min(v=>v.y),frontSign<0?front:back);
            measuredModel=visual.Model;
            return true;
        }
        static Vector3[] BoundsCorners(Bounds bounds)
        {
            var points=new Vector3[8];
            for(int i=0;i<8;i++) points[i]=bounds.center+Vector3.Scale(bounds.extents,
                new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            return points;
        }
    }
}
