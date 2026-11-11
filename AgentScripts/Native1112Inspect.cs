using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Native1112Inspect
{
 public static string Run()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  var paths=new[]{AssetDatabase.GetAssetPath(lib.characters[0]),AssetDatabase.GetAssetPath(lib.characters[1]),"Assets/Resources/Kemono/Kemono_Character.fbx"};
  var report=new System.Text.StringBuilder();
  foreach(var path in paths)
  {
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   try {
    report.AppendLine(path);
    foreach(var a in go.GetComponentsInChildren<Animator>())a.enabled=false;
    foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>()) {
     report.AppendLine($"mesh {s.name} vertices={s.sharedMesh.vertexCount} bounds={s.bounds} scale={s.transform.lossyScale} shapes={s.sharedMesh.blendShapeCount}");
     for(int i=0;i<s.bones.Length;i++)if(s.bones[i]) {
      var rest=s.transform.localToWorldMatrix*s.sharedMesh.bindposes[i].inverse;
      report.AppendLine($"bone {s.bones[i].name} rest={rest.GetColumn(3)}");
     }
    }
   } finally {UnityEngine.Object.DestroyImmediate(go);}
  }
  Directory.CreateDirectory("work/animation-1112");File.WriteAllText("work/animation-1112/native-rig-inspection.txt",report.ToString());return "Saved original/native modular rest inspection";
 }
}
