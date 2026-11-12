using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntColony.Units;
public static class EquipmentFlip1112
{
 public static string Changed(){return Run("Axe")+"\n"+Run("Bow")+"\n"+Run("Pickaxe");}
 public static string Lifecycle()
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var pivot=new GameObject("Equipment lifecycle");
  var model=UnityEngine.Object.Instantiate(lib.characters[0],pivot.transform,false);
  try
  {
   var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);
   grip.SetHandSpan(.4f);grip.Pose("Tool_Pickaxe_Stone","Tool_Mine");
   grip.SetHandSpan(0);grip.Pose(null,"IdleVariant0");grip.Pose(null,"IdleVariant0");
   if(pivot.GetComponentsInChildren<Transform>().Any(t=>t.name=="Native equipment grip"&&t.gameObject.activeInHierarchy))throw new Exception("Unequipped tool still visible");
   grip.SetHandSpan(.5f);grip.Pose("Tool_Axe_Stone","Tool_Chop");grip.SetHandSpan(.3f);grip.Pose("Tool_Axe_Stone","Tool_Chop");
   if(pivot.GetComponentsInChildren<Transform>().Count(t=>t.name=="Native equipment grip"&&t.gameObject.activeInHierarchy)!=1)throw new Exception("Duplicate tool on hand-span change");
   grip.SetHandSpan(0);grip.Pose(null,"Sleep");
   return "PASS tool -> empty hand -> axe -> same tool/new span -> empty hand; no exceptions or active duplicates";
  }finally{UnityEngine.Object.DestroyImmediate(pivot);}
 }
 public static string Run(string filter="",float modelScale=1)
 {
  var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");var report=new List<string>();
  foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Resources/Kemono/Handheld"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>filter==""||Path.GetFileNameWithoutExtension(p).Contains(filter)))
  {
   var name=Path.GetFileNameWithoutExtension(path);string action=null,motion=null;
   bool two=name.StartsWith("Tool_Pickaxe_")||name.StartsWith("Tool_Axe_")||name.StartsWith("Tool_Hammer_")||name.StartsWith("Tool_Shovel_")||name.StartsWith("Tool_Hoe_")||name.Contains("Quakehammer");
   if(name.StartsWith("Tool_Pickaxe_")){action="Tool_Mine";motion="Heavy_Hammer_Swing";}
   else if(name.StartsWith("Tool_Shovel_")){action="Tool_Dig";motion="Heavy_Hammer_Swing";}
   else if(name.StartsWith("Tool_Hoe_")){action="Tool_Till";motion="Heavy_Hammer_Swing";}
   else if(name.StartsWith("Tool_Axe_")){action="Tool_Chop";motion=lib.actions.First(a=>a.action==action).motion;}
   else if(name.StartsWith("Tool_Hammer_")){action="Tool_Hammer";motion="Heavy_Hammer_Swing";}
   else if(name.Contains("Quakehammer")){action="Tool_Hammer";motion="Heavy_Hammer_Swing";}
   else if(name.Contains("Sword")||name.Contains("Katana")){action="Attack";motion="Right_Hand_Sword_Slash";}
   else if(name.Contains("Shield")){action="ShieldBlock";motion="Shield_Push_Left";}
   else if(name.Contains("Bow")){action="BowShoot";motion=lib.actions.First(a=>a.action=="BowShoot").motion;}
   if(motion==null){report.Add(name+" | LEGACY / source motion review required");continue;}
   var entry=lib.motions.First(m=>m.id==motion);var pivot=new GameObject("Equipment audit");
   pivot.transform.localScale=Vector3.one*modelScale;
   var model=UnityEngine.Object.Instantiate(lib.characters[entry.rig],pivot.transform,false);
   try
   {
    var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    Vector3 Palm(string s)=>Vector3.Lerp(bones["mixamorig:"+s+"Hand"].position,bones["mixamorig:"+s+"HandMiddle1"].position,.65f);
    var grip=pivot.AddComponent<MeshyNativeEquipment>();grip.Bind(model.transform);grip.SetHandSpan(entry.maxHandSpan*modelScale);
    float maxError=0,low=100,high=-100,forwardEnd=0,fingerError=0,armError=0;Transform item=null;
    for(int frame=0;frame<=30;frame++)
    {
     entry.clip.SampleAnimation(entry.armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model,entry.clip.length*frame/30f);
     var arm=bones["mixamorig:RightForeArm"].localRotation;grip.Pose(name,action);fingerError=Mathf.Max(fingerError,grip.FingerContactError);armError=Mathf.Max(armError,Quaternion.Angle(arm,bones["mixamorig:RightForeArm"].localRotation));
     if(!item)item=pivot.GetComponentsInChildren<Transform>().First(t=>t.name==name+"(Clone)");
     var mount=item.parent;bool left=name.Contains("Bow")||name.Contains("Shield");
     float lead=two?(name.Contains("Quakehammer")?.30f:name.StartsWith("Tool_Hammer_")?.12f:.20f):0;
     maxError=Mathf.Max(maxError,Vector3.Distance(item.position-mount.forward*lead*item.lossyScale.y,Palm(left?"Left":"Right")));
     if(two){float support=Vector3.Dot(Palm(left?"Right":"Left")-item.position,mount.forward)/item.lossyScale.y;low=Mathf.Min(low,support);high=Mathf.Max(high,support);}
     if(frame==30)forwardEnd=Vector3.Dot(-mount.forward,pivot.transform.forward);
    }
    bool pass=maxError<.002f&&(name.Contains("Bow")||armError<.01f)&&(!two||(low>=-(name.Contains("Quakehammer")?.30f:name.StartsWith("Tool_Hammer_")?.12f:.20f)&&high<=(name.Contains("Quakehammer")?.18f:name.StartsWith("Tool_Hammer_")?.09f:.13f)));
    report.Add(name+" scale="+modelScale+" | "+action+" | "+(pass?"PASS":"REVIEW")+" palm="+maxError.ToString("F6")+" support="+low.ToString("F3")+".."+high.ToString("F3")+" endHeadForward="+forwardEnd.ToString("F3")+" fingerTargetError="+fingerError.ToString("F4")+" armChanged="+armError.ToString("F3"));
   }finally{UnityEngine.Object.DestroyImmediate(pivot);}
  }
  File.WriteAllLines("work/equipment-flip-1112/audit"+(filter==""?"":"-"+filter)+".txt",report);return string.Join("\n",report);
 }
}

