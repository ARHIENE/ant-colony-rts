using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MeshyAnimation1106Inspect
{
 public static string Main()
 {
  const string dir="Assets/_Project/Art/MeshyAnimation1106";
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var report=new System.Text.StringBuilder();
  foreach(var path in Directory.GetFiles(dir,"*.fbx")) {
   var p=path.Replace('\\','/');
   var importer=(ModelImporter)AssetImporter.GetAtPath(p);
   importer.animationType=ModelImporterAnimationType.Generic; importer.importAnimation=true; importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(p);
   report.AppendLine(p);
   foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))) report.AppendLine("CLIP "+clip.name+" duration="+clip.length);
   foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))report.AppendLine("SKIN "+r.name+" bones="+r.bones.Length+" shapes="+r.sharedMesh.blendShapeCount);
   foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name.Contains("Hand")||t.name.Contains("Finger")||t.name.Contains("Index")||t.name.Contains("Leg"))report.AppendLine("BONE "+t.name);
  }
  File.WriteAllText("art/meshy-animation-1106/unity-import-inspection.txt",report.ToString());
  return report.ToString();
 }
}
