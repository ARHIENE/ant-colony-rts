using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Native1112Portraits
{
 public static async Task<string> Run(){
  var p=UnityEngine.Object.FindAnyObjectByType<MeshyNativeMotionPlayer>();var model=p.transform.Find("Kemono");var appearance=model.GetComponent<KemonoAppearance>();
  var source=model.GetComponentsInChildren<SkinnedMeshRenderer>();var face=source.First(s=>s.name.StartsWith("Part_Face_"));
  string hair=source.First(s=>s.enabled&&s.name.StartsWith("Part_Hair_")).name.Split('_')[2];
  var cam=Camera.main;var cc=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(cc)cc.enabled=false;
  AntColony.UI.SimulationPause.UserSpeed(0);EditorApplication.isPaused=false;p.PreviewMotion="Idle_3";p.PreviewPhase=.3f;await Task.Delay(100);
  var head=p.NativeModel.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Head");var focus=head.position+Vector3.up*.1f;cam.orthographicSize=.42f;cam.nearClipPlane=.02f;cam.transform.position=focus+model.forward*2;cam.transform.LookAt(focus);
  string path="work/animation-1112/portraits";Directory.CreateDirectory(path);var rt=new RenderTexture(480,480,24);var tex=new Texture2D(480,480,TextureFormat.RGB24,false);var prior=cam.targetTexture;var active=RenderTexture.active;
  Action<string> capture=name=>{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,480,480),0,0);tex.Apply();File.WriteAllBytes(path+"/"+name+".png",tex.EncodeToPNG());};
  try{foreach(var option in appearance.GetVariants("Hair")){appearance.SetVariant("Hair",option);await Task.Delay(40);capture("hair-"+option);}appearance.SetVariant("Hair","CROP");await Task.Delay(40);
   for(int i=0;i<face.sharedMesh.blendShapeCount;i++){for(int j=0;j<face.sharedMesh.blendShapeCount;j++)face.SetBlendShapeWeight(j,i==j?100:0);p.NativeModel.GetComponent<MeshyNativeAppearance>().Sync();await Task.Delay(60);capture("face-"+face.sharedMesh.GetBlendShapeName(i).Replace('.','_'));}
   return "6 hair variants and 8 expression closeups captured";
  }finally{appearance.SetVariant("Hair",hair);for(int i=0;i<face.sharedMesh.blendShapeCount;i++)face.SetBlendShapeWeight(i,0);p.PreviewMotion=null;p.PreviewPhase=-1;cam.targetTexture=prior;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
