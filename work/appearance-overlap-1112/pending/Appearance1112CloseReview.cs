using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Appearance1112CloseReview
{
 public static string All(){for(int i=0;i<119;i+=6)Run(i,6);return "119 source-part poses rendered";}
 public static string Run(int start=0,int count=1)
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  var root=new GameObject("Temporary source motion review");root.transform.position=new Vector3(700,700,700);
  var rt=new RenderTexture(320,320,24);var tile=new Texture2D(320,320,TextureFormat.RGB24,false);
  int rows=Math.Min(count,lib.motions.Length-start);var sheet=new Texture2D(1600,rows*320,TextureFormat.RGB24,false);
  var oldRT=RenderTexture.active;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.mainTexture=lib.texture;
  var oldAsync=ShaderUtil.allowAsyncCompilation;
  try
  {
   ShaderUtil.allowAsyncCompilation=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
   var camObj=new GameObject("Review camera");camObj.transform.SetParent(root.transform,false);var cam=camObj.AddComponent<Camera>();cam.enabled=false;cam.cullingMask=1<<31;cam.targetTexture=rt;cam.orthographic=true;cam.orthographicSize=.52f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.22f,.25f);cam.nearClipPlane=.01f;
   cam.transform.localPosition=new Vector3(0,1.35f,3);cam.transform.LookAt(root.transform.position+Vector3.up*1.34f);
   var lightObj=new GameObject("Review light");lightObj.transform.SetParent(root.transform,false);lightObj.transform.rotation=Quaternion.Euler(35,-35,0);var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<31;
   for(int row=0;row<rows;row++)
   {
    var m=lib.motions.First(x=>x.id=="Idle_3");var pivot=new GameObject(m.id);pivot.transform.SetParent(root.transform,false);var model=UnityEngine.Object.Instantiate(lib.characters[m.rig],pivot.transform,false);
    foreach(var a in model.GetComponentsInChildren<Animator>())a.enabled=false;
    var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=MeshyNativeBounds.Rest(skins);pivot.transform.localScale=Vector3.one*(1.6f/bounds.size.y);
    var original=KemonoModel.Create(pivot.transform,42); original.GetComponent<KemonoAppearance>().SetVariant("Hair","BOB"); original.GetComponent<KemonoAppearance>().SetPaletteColor(new Color(.65f,.31f,.36f));foreach(var r in original.GetComponentsInChildren<Renderer>())r.forceRenderingOff=true;
    if(!MeshyNativeAppearance.Attach(model.transform,original.transform,m.rig))throw new Exception("Appearance attachment failed");
    skins=model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();
    var baked=skins.Select(s=>new Mesh()).ToArray();
    for(int si=0;si<skins.Length;si++)
    {
     var s=skins[si];s.enabled=false;var visible=new GameObject("Baked pose");visible.transform.SetParent(s.transform,false);visible.AddComponent<MeshFilter>().sharedMesh=baked[si];var renderer=visible.AddComponent<MeshRenderer>();renderer.sharedMaterials=s.sharedMaterials.Select(source=>{var unlit=new Material(Shader.Find("Universal Render Pipeline/Unlit"));unlit.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));unlit.SetColor("_BaseColor",source.GetColor("_BaseColor"));return unlit;}).ToArray(); renderer.receiveShadows=false;for(int mi=0;mi<s.sharedMaterials.Length;mi++){var block=new MaterialPropertyBlock();s.GetPropertyBlock(block,mi);renderer.SetPropertyBlock(block,mi);}
    }
    foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
    var hips=model.GetComponentsInChildren<Transform>().First(t=>t.name=="mixamorig:Hips");var bind=hips.localPosition;
    for(int f=0;f<5;f++)
    {
     m.clip.SampleAnimation(m.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,m.clip.length*(.05f+.2f*f));
     hips.localPosition=new Vector3(bind.x,hips.localPosition.y,bind.z);
     for(int si=0;si<skins.Length;si++)skins[si].BakeMesh(baked[si]);
     cam.Render();RenderTexture.active=rt;tile.ReadPixels(new Rect(0,0,320,320),0,0);tile.Apply();sheet.SetPixels(f*320,(rows-1-row)*320,320,320,tile.GetPixels());
    }
    UnityEngine.Object.DestroyImmediate(pivot);foreach(var mesh in baked)UnityEngine.Object.DestroyImmediate(mesh);
   }
   sheet.Apply();Directory.CreateDirectory("work/appearance-overlap-1112/unlit-review");var path="work/appearance-overlap-1112/unlit-review/"+start.ToString("D3")+".png";File.WriteAllBytes(path,sheet.EncodeToPNG());File.WriteAllLines(Path.ChangeExtension(path,"txt"),lib.motions.Skip(start).Take(rows).Select(m=>m.id));return path;
  }
  finally{RenderTexture.active=oldRT;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;ShaderUtil.allowAsyncCompilation=oldAsync;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mat);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tile);UnityEngine.Object.DestroyImmediate(sheet);}
 }
}
