using System.Linq;
using UnityEngine;
using UnityEditor;
public static class MeshyHandcart1115
{
 public static string Import()
 {
  const string folder="Assets/Resources/Transport1115";
  AssetDatabase.Refresh();
  foreach(var kind in new[]{"Handcart"})
  {
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+kind+"_Image_0.png");
   if(!texture)throw new System.Exception("Missing texture "+kind);
   var path=folder+"/"+kind+".fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
   var matPath=folder+"/Materials/Meshy1115_"+kind+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.2f);
   EditorUtility.SetDirty(mat);
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Meshy1115_"+kind),mat);
   importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   if(!model.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m&&m.GetTexture("_BaseMap")==texture)))throw new System.Exception("Texture remap failed "+kind);
  }
  AssetDatabase.SaveAssets();return "Textured Meshy handcart imported and material bindings verified.";
 }
}
