using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class MeshyLife1106Audit
{
 public static string Main(){
 const string dir="Assets/_Project/Art/MeshyAnimation1106/Life/";
 var files=Directory.GetFiles(dir,"*.fbx").OrderBy(x=>x).ToArray();var lines=new List<string>();
 var rt=new RenderTexture(320,320,24);var sheet=new Texture2D(960,1920,TextureFormat.RGB24,false);var previous=RenderTexture.active;
 try{for(int n=0;n<files.Length;n++){
 var path=files[n].Replace('\\','/');AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.animationType=ModelImporterAnimationType.Generic;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.SaveAndReimport();
 var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();var clip=clips.First();
 var root=new GameObject("Isolated life audit");root.transform.position=new Vector3(3000,3000,3000);var mesh=new Mesh();var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
 try{var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();model.transform.localScale*=1.6f/skin.bounds.size.y;skin.enabled=false;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/MeshyAnimation1106/Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png");var surface=new GameObject("Baked frame");surface.transform.SetParent(skin.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;var cg=new GameObject("Camera");cg.transform.SetParent(root.transform,false);var cam=cg.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=1;cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.24f,.23f);cam.cullingMask=1<<30;cam.targetTexture=rt;foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
 for(int k=0;k<3;k++){clip.SampleAnimation(model,clip.length*(k+.5f)/3f);skin.BakeMesh(mesh);mesh.RecalculateBounds();var center=renderer.bounds.center;cam.transform.position=center+new Vector3(-1,.25f,3);cam.transform.LookAt(center);cam.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,320,320),k*320,(5-n%6)*320);}
 lines.Add(Path.GetFileNameWithoutExtension(path)+" | "+clip.name+" | "+clip.length.ToString("F3")+"s | bones="+skin.bones.Length+" | curves="+AnimationUtility.GetCurveBindings(clip).Length);
 }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(mesh);Object.DestroyImmediate(mat);}
 if(n%6==5||n==files.Length-1){sheet.Apply();File.WriteAllBytes("art/meshy-animation-1106/life-bundle/unity-review-"+(n/6)+".png",sheet.EncodeToPNG());}
 }File.WriteAllLines("art/meshy-animation-1106/life-bundle/unity-clips.txt",lines);return string.Join("\n",lines);
 }finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);}
 }
}
