using System;using System.Linq;using UnityEngine;using UnityEditor;
public static class AcidTower1115Import {
 public static string Main(){const string folder="Assets/Resources/AcidTower1115";AssetDatabase.Refresh();
 foreach(var kind in new[]{"AcidTower"}){
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+kind+"_BaseColor.png");if(!texture)throw new Exception("Missing texture "+kind);
 var path=folder+"/"+kind+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
 var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var names=model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct().ToArray();
 importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;importer.addCollider=false;
 var mp=folder+"/Materials/"+kind+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
 if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,mp);}
 mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.15f);
 var normalPath=folder+"/"+kind+"_Normal.png";var ti=(TextureImporter)AssetImporter.GetAtPath(normalPath);if(ti){ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");}
 if(kind=="ThatchRoof"){
 mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_ZWrite",0);mat.SetInt("_SrcBlend",5);mat.SetInt("_DstBlend",10);mat.SetInt("_SrcBlendAlpha",1);mat.SetInt("_DstBlendAlpha",10);
 mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
 }
 EditorUtility.SetDirty(mat);foreach(var name in names)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);importer.SaveAndReimport();
 model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap")==texture)))throw new Exception("Material remap failed "+kind);
 if(kind=="PalisadeGate"&&model.GetComponentsInChildren<Transform>().Count(t=>t.name=="GateLeafLeft"||t.name=="GateLeafRight")!=2)throw new Exception("Missing gate hinges");
 }AssetDatabase.SaveAssets();return "PASS Meshy acid tower and textures imported";}
}

