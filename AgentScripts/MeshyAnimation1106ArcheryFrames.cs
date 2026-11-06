using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class MeshyAnimation1106ArcheryFrames
{
 public static string Main(){
 const string dir="Assets/_Project/Art/MeshyAnimation1106/";
 var root=new GameObject("Isolated archery frame audit");root.transform.position=new Vector3(2000,2000,2000);
 var rt=new RenderTexture(500,500,24);var sheet=new Texture2D(2000,1500,TextureFormat.RGB24,false);var mesh=new Mesh();var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));var previous=RenderTexture.active;
 try{var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(dir+"Archery_Shot_3.fbx"),root.transform);var clip=AssetDatabase.LoadAllAssetsAtPath(dir+"Archery_Shot_3.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();model.transform.localScale*=1.6f/skin.bounds.size.y;skin.enabled=false;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png");var surface=new GameObject("Baked frame");surface.transform.SetParent(skin.transform,false);surface.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;var cameraGo=new GameObject("Camera");cameraGo.transform.SetParent(root.transform,false);var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.85f;camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.24f,.23f);camera.cullingMask=1<<30;camera.targetTexture=rt;foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
 for(int i=0;i<12;i++){clip.SampleAnimation(model,clip.length*i/12f);skin.BakeMesh(mesh);var focus=renderer.bounds.center;camera.transform.position=focus+new Vector3(-1,.25f,3);camera.transform.LookAt(focus);camera.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,500,500),(i%4)*500,(2-i/4)*500);}
 sheet.Apply();var path="art/meshy-animation-1106/archery3-cycle.png";File.WriteAllBytes(path,sheet.EncodeToPNG());return path+" left to right, top to bottom, phases 0..11/12";
 }finally{RenderTexture.active=previous;Object.DestroyImmediate(root);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(mesh);Object.DestroyImmediate(mat);Object.DestroyImmediate(sheet);}
 }
}
