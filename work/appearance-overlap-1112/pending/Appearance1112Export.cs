using System;using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEngine;using AntColony.Core;using AntColony.Units;
public static class Appearance1112Export {
 [Serializable] public class Dump{public Vector3[] source,baked;public Vector2[] uv;public int[] triangles;public BoneWeight[] weights;}
 public static async Task<string> Run(){var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);var p=c.GetComponent<MeshyNativeMotionPlayer>();c.transform.Find("Kemono").GetComponent<KemonoAppearance>().SetVariant("Hair","BOB");p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;await Task.Delay(120);string report="";
 foreach(var s in p.NativeModel.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&(s.name.StartsWith("Part_Hair_")||s.name.StartsWith("Part_Face_")))){var mesh=new Mesh();s.BakeMesh(mesh);File.WriteAllText("work/appearance-overlap-1112/"+s.name+"-live.json",JsonUtility.ToJson(new Dump{source=s.sharedMesh.vertices,baked=mesh.vertices,uv=mesh.uv,triangles=mesh.triangles,weights=s.sharedMesh.boneWeights}));var block=new MaterialPropertyBlock();s.GetPropertyBlock(block,0);report+=s.name+" materialTex="+s.sharedMaterial.GetTexture("_BaseMap")?.name+" blockTex="+block.GetTexture("_BaseMap")?.name+" verts="+mesh.vertexCount+" bones="+s.bones.Length+"\n";UnityEngine.Object.Destroy(mesh);}
 File.WriteAllText("work/appearance-overlap-1112/live-export.txt",report);return report;
 }
}
