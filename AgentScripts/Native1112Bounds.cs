using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class Native1112Bounds {
 public static string Run(){var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");return string.Join("\n",lib.characters.Select(c=>{var s=c.GetComponentInChildren<SkinnedMeshRenderer>();var b=new Bounds(s.transform.TransformPoint(s.sharedMesh.vertices[0]),Vector3.zero);foreach(var v in s.sharedMesh.vertices)b.Encapsulate(s.transform.TransformPoint(v));return c.name+" renderer="+s.bounds+" restGeometry="+b+" localMesh="+s.sharedMesh.bounds;}));}
}
