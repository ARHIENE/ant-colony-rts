using System;
using System.IO;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class HandGrip1106Preview
{
 public static string Batch()
 {
  var tools=new[]{"Axe","Pickaxe","Shovel","Hammer","Hoe","Sickle"};
  foreach(var n in tools)AssetDatabase.ImportAsset("Assets/Resources/Kemono/Handheld/Tool_"+n+"_Stone.fbx",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  foreach(var n in tools){Main("front","Tool_"+n+"_Stone");Main("side","Tool_"+n+"_Stone");}
  var sheet=new Texture2D(1800,1050,TextureFormat.RGB24,false);
  for(int i=0;i<tools.Length;i++){
   var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes("work/hand-grip-1106/Tool_"+tools[i]+"_Stone-front-Idle_A-0.2.png"));
   for(int y=0;y<525;y++)for(int x=0;x<600;x++)sheet.SetPixel((i%3)*600+x,(1-i/3)*525+y,tex.GetPixelBilinear((x+.5f)/600,(y+.5f)/525));
   Object.DestroyImmediate(tex);
  }
  sheet.Apply();File.WriteAllBytes("work/hand-grip-1106/tool-contact-sheet.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
  return "12 Unity close-up views and six-tool contact sheet rendered";
 }
 public static string Main(string mode="front", string tool="Tool_Axe_Stone", string clip="Idle_A", float phase=.2f, string isolation="none")
 {
  var root=new GameObject("Temporary equipment fitting stage");root.transform.position=new Vector3(500,500,500);
  var rt=new RenderTexture(1800,1050,24);var previous=RenderTexture.active;
  var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;var asyncShaders=ShaderUtil.allowAsyncCompilation;
  ShaderUtil.allowAsyncCompilation=false;
  var floorMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMat.color=new Color(.80f,.77f,.70f);
  try{
   var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.transform.SetParent(root.transform,false);ground.transform.localScale=Vector3.one*2;ground.GetComponent<Renderer>().sharedMaterial=floorMat;
   var lightObj=new GameObject("Key");lightObj.transform.SetParent(root.transform,false);lightObj.transform.rotation=Quaternion.Euler(40,145,0);
   var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.cullingMask=1<<31;light.shadows=LightShadows.Soft;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.57f,.59f,.61f);
   var co=new GameObject("Camera");co.transform.SetParent(root.transform,false);var cam=co.AddComponent<Camera>();cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=floorMat.color;cam.cullingMask=1<<31;cam.orthographic=true;cam.orthographicSize=1.35f;cam.nearClipPlane=.1f;cam.farClipPlane=30;cam.targetTexture=rt;
   var names=new[]{tool};
   if(mode=="new")names=new[]{"Weapon_Katana","Weapon_GrandBow","Weapon_SniperRifle","Tool_Axe_Stone"};
   for(int i=0;i<names.Length;i++){
    
    var h=new GameObject(names[i]);h.transform.SetParent(root.transform,false);h.transform.localPosition=Vector3.zero;h.transform.localRotation=Quaternion.identity;
    var model=KemonoModel.Create(h.transform,1105);var ap=model.GetComponent<KemonoAppearance>();ap.SetVariant("Hair","BOB");ap.SetVariant("Outfit","CITIZEN");ap.SetPaletteColor(new Color(.75f,.79f,.85f));
    var anim=model.GetComponentsInChildren<Animator>().First(a=>a.runtimeAnimatorController!=null);anim.Play("Base Layer."+clip,0,phase);anim.Update(.01f);anim.enabled=false;
    if(mode=="wear"){
     var gear=model.AddComponent<MeshyEquipmentWearables>();
     gear.Apply(new EquipmentItem{slot=EquipmentSlot.Armor,armor=i==1?ArmorKind.Wings:ArmorKind.Coating,bossEquipment=i==2?BossEquipmentKind.Durable:i==3?BossEquipmentKind.HeatShield:BossEquipmentKind.None},new EquipmentItem{slot=EquipmentSlot.Trinket,effect=(TrinketEffect)i},null,null,false);
    }else{
     KemonoModel.Hold(model,names[i]); var grip=model.GetComponent<MeshyEquipmentGrip>(); if(isolation=="open")grip.Held=null; grip.Pose(isolation!="open" && isolation!="noik" && clip=="Idle_A");
     if(model.GetComponentsInChildren<MeshRenderer>().All(r=>!r.name.StartsWith(names[i])))throw new Exception("Missing held "+names[i]);
    }
   }
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   if(isolation=="body"||isolation=="outfit")foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.enabled=renderer.enabled && renderer.name.StartsWith(isolation=="body"?"Part_Body":"Part_Outfit");
   cam.transform.localPosition=new Vector3(.3f,2.1f,7);cam.transform.LookAt(root.transform.position+Vector3.up*.8f);
   {
    cam.orthographicSize=mode=="full"?.9f:.14f;
    var rifle=root.transform.Find(tool);
    var hand=rifle.GetComponentsInChildren<Transform>().First(t=>t.name==(mode=="left"?"Socket.Tool.L":"Socket.Tool.R"));
    var focus=mode=="full"?root.transform.position+Vector3.up*.8f:hand.position;
    var view=mode=="side"?new Vector3(2,.3f,0):mode=="top"?new Vector3(0,2,.2f):new Vector3(1,.6f,2);
    if(mode=="grip"||mode=="grip-left"){
     if(mode=="grip-left")hand=rifle.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket.Tool.L");
     focus=hand.position;view=hand.TransformDirection(new Vector3(1,.3f,2));cam.orthographicSize=.08f;
    }
    cam.transform.position=focus+view;cam.transform.LookAt(focus,mode.StartsWith("grip")?hand.up:Vector3.up);
   }
   cam.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
   var path="work/hand-grip-1106/"+tool+"-"+mode+"-"+clip+"-"+phase+(isolation=="none"?"":"-"+isolation)+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
   return "Actual Unity equipment fitting render: "+path;
  }finally{RenderTexture.active=previous;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;ShaderUtil.allowAsyncCompilation=asyncShaders;Object.DestroyImmediate(root);Object.DestroyImmediate(floorMat);rt.Release();Object.DestroyImmediate(rt);}
 }
}


