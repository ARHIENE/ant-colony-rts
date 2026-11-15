using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class AlienSiegeCore1115Import
{
 public static string Main(){
  const string folder="Assets/Resources/CampaignBoss1115";
  AssetDatabase.Refresh();
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/AlienSiegeCore_BaseColor.png");
  var path=folder+"/AlienSiegeCore.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
  if(!texture||importer==null)throw new Exception("Pack source missing");
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var names=model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct().ToArray();
  importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;importer.addCollider=false;
  var mp=folder+"/Materials/AlienSiegeCore.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
  if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,mp);}
  mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.15f);
  EditorUtility.SetDirty(mat);
  foreach(var name in names)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mat);
  importer.SaveAndReimport();AssetDatabase.SaveAssets();
  model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  if(!model.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap")==texture)))throw new Exception("Pack remap failed");
  return "PASS Meshy siege core, URP material and 2048 texture imported";
 }
}

