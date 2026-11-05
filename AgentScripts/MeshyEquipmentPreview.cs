using System;
using System.IO;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
public static class MeshyEquipmentPreview
{
 public static string Main(string mode="weapons")
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
   var names=new[]{"Weapon_Sword_Copper","Weapon_ShortBow","Weapon_SingleRifle","Weapon_AcidSprayer"};
   if(mode=="new")names=new[]{"Weapon_Katana","Weapon_GrandBow","Weapon_SniperRifle","Tool_Axe_Stone"};
   for(int i=0;i<names.Length;i++){
    if(mode.StartsWith("hands")&&i!=2)continue;
    var h=new GameObject(names[i]);h.transform.SetParent(root.transform,false);h.transform.localPosition=new Vector3((i-1.5f)*1.05f,0,0);h.transform.localRotation=Quaternion.Euler(0,25,0);
    var model=KemonoModel.Create(h.transform,1105);var ap=model.GetComponent<KemonoAppearance>();ap.SetVariant("Hair","BOB");ap.SetVariant("Outfit","CITIZEN");ap.SetPaletteColor(new Color(.75f,.79f,.85f));
    var anim=model.GetComponentsInChildren<Animator>().First(a=>a.runtimeAnimatorController!=null);anim.Play("Base Layer.Idle_A",0,.2f);anim.Update(.01f);anim.enabled=false;
    if(mode=="wear"){
     var gear=model.AddComponent<MeshyEquipmentWearables>();
     gear.Apply(new EquipmentItem{slot=EquipmentSlot.Armor,armor=i==1?ArmorKind.Wings:ArmorKind.Coating,bossEquipment=i==2?BossEquipmentKind.Durable:i==3?BossEquipmentKind.HeatShield:BossEquipmentKind.None},new EquipmentItem{slot=EquipmentSlot.Trinket,effect=(TrinketEffect)i},null,null,false);
    }else{
     KemonoModel.Hold(model,names[i]);model.GetComponent<MeshyEquipmentGrip>().Pose(true);
     if(model.GetComponentsInChildren<MeshRenderer>().All(r=>!r.name.StartsWith(names[i])))throw new Exception("Missing held "+names[i]);
    }
   }
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
   cam.transform.localPosition=new Vector3(.3f,2.1f,7);cam.transform.LookAt(root.transform.position+Vector3.up*.8f);
   if(mode.StartsWith("hands")){
    cam.orthographicSize=.28f;
    var rifle=root.transform.Find("Weapon_SingleRifle");
    var hand=rifle.GetComponentsInChildren<Transform>().First(t=>t.name=="Socket.Tool.R");
    var focus=hand.position+rifle.forward*.14f;
    cam.transform.position=focus+(mode=="hands-side"?new Vector3(2,.3f,0):mode=="hands-top"?new Vector3(0,2,.2f):new Vector3(1,.6f,2));cam.transform.LookAt(focus);
   }
   cam.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
   var path="art/meshy-equipment-1105/Unity_Equipment_"+(mode=="wear"?"Wearables":mode.StartsWith("hands")?mode:mode=="new"?"NewWeapons":"Fitting")+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
   return "Actual Unity equipment fitting render: "+path;
  }finally{RenderTexture.active=previous;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;ShaderUtil.allowAsyncCompilation=asyncShaders;Object.DestroyImmediate(root);Object.DestroyImmediate(floorMat);rt.Release();Object.DestroyImmediate(rt);}
 }
}
