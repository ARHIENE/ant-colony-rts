using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class ChemicalImport1028
{
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before asset import");
  AssetDatabase.Refresh();var import=typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp",BindingFlags.Static|BindingFlags.NonPublic);var checks=0;
  foreach(var path in new[]{"Assets/Resources/Facilities/CC_ChemistryBench.fbx","Assets/Resources/ResourceModels/Proc_Plastic.fbx"})
  {
   import.Invoke(null,new object[]{path,true});var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var renderers=model.GetComponentsInChildren<Renderer>();
   if(renderers.Length==0||renderers.Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||!m.shader.name.StartsWith("Universal Render Pipeline/"))))throw new Exception("Missing URP model/material: "+path);
   checks+=renderers.Length;
  }
  AssetDatabase.SaveAssets();var result="PASS 2 chemical/plastic FBX imports, "+checks+" renderers/material sets";File.WriteAllText("Temp/chemical-import1028-result.txt",result);return result;
 }
}
