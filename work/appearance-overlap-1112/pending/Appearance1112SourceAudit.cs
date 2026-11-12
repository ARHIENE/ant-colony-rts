using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using AntColony.Units;
public static class Appearance1112SourceAudit {
 public static string Run(){
 var lib=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");string report="";
 foreach(var m in lib.motions.Where(m=>m.id=="Idle_3"||m.id=="Heavy_Hammer_Swing"))report+=m.id+" bindings="+string.Join(";",AnimationUtility.GetCurveBindings(m.clip).Where(b=>!b.propertyName.StartsWith("m_Local")).Select(b=>b.path+":"+b.type.Name+":"+b.propertyName))+"\n";
 for(int i=0;i<lib.characters.Length;i++){var obj=lib.characters[i];report+="Rig"+i+" renderers="+string.Join(";",obj.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+":"+r.GetType().Name+":"+r.enabled))+"\n";}
 File.WriteAllText("work/appearance-overlap-1112/source-renderer-audit.txt",report);return report;
 }
}
