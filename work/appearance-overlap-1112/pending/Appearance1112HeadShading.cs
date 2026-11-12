using System;using System.IO;using UnityEngine;using UnityEditor;
public static class Appearance1112HeadShading {
 public static string Run(){if(Application.isPlaying)throw new Exception("Edit required");string report="";
 for(int rig=0;rig<2;rig++)foreach(var part in new[]{"Hair","Face","Ears"}){var path="Assets/Resources/MeshyNative/SourceParts/Rig"+rig+"/"+part+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)throw new Exception(path);m.SetFloat("_ReceiveShadows",0);m.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(m);report+=path+" keeps Lit lighting and shadow casting; disables head self-shadow reception\n";}AssetDatabase.SaveAssets();File.WriteAllText("work/appearance-overlap-1112/head-shading.txt",report);return report;
 }
}
