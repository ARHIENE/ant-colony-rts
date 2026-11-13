using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
public static class Motion1113ToolSurface
{
 static Vector3[] Vertices(Mesh mesh){using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh)){using(var vertices=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp)){data[0].GetVertices(vertices);return vertices.ToArray();}}}
 public static async Task<string> Run()
 {
  var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsGatheringAnimation);var p=c.GetComponent<MeshyNativeMotionPlayer>();
  var target=c.CurrentResourceNode;var outward=Vector3.ProjectOnPlane(c.Position-target.transform.position,Vector3.up).normalized;
  var colliders=new List<MeshCollider>();var old=p.PreviewMotion;float oldPhase=p.PreviewPhase;
  try
  {
   foreach(var r in target.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.forceRenderingOff))
   {var collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;colliders.Add(collider);}
   p.PreviewMotion=p.PlayingMotion;p.PreviewPhase=0;await Task.Delay(100);
   var mount=p.GetComponentsInChildren<Transform>().First(t=>t.name=="Native equipment grip"&&t.gameObject.activeInHierarchy);var item=mount.GetChild(0);
   var vertices=item.GetComponentsInChildren<MeshFilter>().SelectMany(f=>Vertices(f.sharedMesh).Select(v=>mount.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
   float low=vertices.Min(v=>v.z),high=vertices.Max(v=>v.z);var head=vertices.Where(v=>v.z<low+(high-low)*.25f).ToArray();
   var tips=new[]{head.OrderBy(v=>v.x).First(),head.OrderByDescending(v=>v.x).First(),head.OrderBy(v=>v.y).First(),head.OrderByDescending(v=>v.y).First(),head.OrderBy(v=>v.z).First(),head.OrderByDescending(v=>v.z).First()};
   float nearest=999,phase=0;
   for(int i=0;i<72;i++)
   {
    p.PreviewPhase=i/72f;await Task.Delay(25);
    foreach(var v in tips)
    {
     var point=mount.TransformPoint(v);var ray=new Ray(point+outward*4,-outward);
     foreach(var collider in colliders)if(collider.Raycast(ray,out var hit,8))
     {float gap=Mathf.Abs(Vector3.Dot(point-hit.point,outward));if(gap<nearest){nearest=gap;phase=i/72f;}}
    }
   }
   p.PreviewPhase=phase;await Task.Delay(100);
   System.IO.Directory.CreateDirectory("work/motion-actions-1113/screens");ScreenCapture.CaptureScreenshot("work/motion-actions-1113/screens/farm-contact.png");await Task.Delay(250);
   string result="actual tool head to visible farm surface="+nearest+"m phase="+phase+" source="+p.PlayingMotion;
   System.IO.File.WriteAllText("work/motion-actions-1113/farm-surface.txt",result);return result;
  }
  finally{p.PreviewMotion=old;p.PreviewPhase=oldPhase;foreach(var col in colliders)UnityEngine.Object.Destroy(col);}
 }
}
