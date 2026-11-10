using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntColony.Units;
using Object=UnityEngine.Object;
public static class Animation1110Preview {
 public static string Batch(){foreach(var n in new[]{"Running","Walking","Sleep_Normally","Pull_Radish","Carry_Heavy_Object_Walk","Archery_Shot_2","Right_Hand_Sword_Slash","Heavy_Hammer_Swing"})Main(n,n=="Archery_Shot_2"?"Weapon_ShortBow":n=="Right_Hand_Sword_Slash"?"Weapon_Sword_Copper":n=="Heavy_Hammer_Swing"?"Tool_Pickaxe_Stone":null,true);return "8 clips, 48 poses";}
 public static string Main(string name="Tool_Mine",string tool="Tool_Pickaxe_Stone",bool grip=false){
 var owner=new GameObject("Animation audit");owner.transform.position=new Vector3(500,500,500);var model=KemonoModel.Create(owner.transform,42);var animator=RigAnimator.Find(model);animator.enabled=false;var clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/"+name);var g=model.GetComponent<MeshyEquipmentGrip>();g.enabled=false;g.Motion=name=="Heavy_Hammer_Swing"?"Tool_Mine":name=="Charged_Axe_Chop"?"Tool_Chop":name;KemonoModel.Hold(model,tool);
 var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();var meshes=skins.Select(s=>new Mesh()).ToArray();var renderers=skins.Select((s,i)=>{var go=new GameObject("Baked");go.transform.SetParent(s.transform,false);go.transform.localScale=new Vector3(1/s.transform.lossyScale.x,1/s.transform.lossyScale.y,1/s.transform.lossyScale.z);go.AddComponent<MeshFilter>().sharedMesh=meshes[i];var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=s.sharedMaterials;s.updateWhenOffscreen=true;s.forceMatrixRecalculationPerRender=true;s.enabled=false;return r;}).ToArray();var cg=new GameObject("Camera");cg.transform.SetParent(owner.transform,false);var cam=cg.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=1.05f;cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.22f,.24f);cam.cullingMask=1<<30;foreach(var t in owner.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
 var rt=new RenderTexture(500,500,24);cam.targetTexture=rt;var sheet=new Texture2D(1500,1000,TextureFormat.RGB24,false);var prev=RenderTexture.active;
 try{for(int f=0;f<6;f++){clip.SampleAnimation(animator.gameObject,clip.length*(f+.1f)/6);if(grip)g.Pose(false,(f+.1f)/6);for(int i=0;i<skins.Length;i++){skins[i].BakeMesh(meshes[i]);meshes[i].RecalculateBounds();}var bounds=renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});if(f==0)File.WriteAllLines("work/animation-1110/render-debug.txt",skins.Select((s,i)=>s.name+" scale="+s.transform.lossyScale+" meshBounds="+meshes[i].bounds+" world="+renderers[i].bounds));var center=bounds.center;cam.transform.position=center+new Vector3(-1,.15f,3);cam.transform.LookAt(center);cam.Render();RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,500,500),f%3*500,(1-f/3)*500);}sheet.Apply();var path="work/animation-1110/"+name+(grip?"-grip":"-raw")+".png";File.WriteAllBytes(path,sheet.EncodeToPNG());return path;}finally{RenderTexture.active=prev;Object.DestroyImmediate(owner);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(sheet);foreach(var m in meshes)Object.DestroyImmediate(m);}
 }
}




