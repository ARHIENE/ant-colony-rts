using System.Linq;using UnityEngine;using UnityEditor;
public static class CropAsset1115
{
 public static string Import()
 {
  AssetDatabase.Refresh();
  const string folder="Assets/Resources/CropStages1115";
  if(!AssetDatabase.IsValidFolder(folder+"/Materials"))AssetDatabase.CreateFolder(folder,"Materials");
  foreach(var file in System.IO.Directory.GetFiles(folder,"*.fbx"))
  {
   var path=file.Replace('\\','/');var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
   foreach(var source in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
   {
    string target=folder+"/Materials/"+source.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(target);
    if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=source.color;AssetDatabase.CreateAsset(mat,target);}
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),mat);
   }
   importer.SaveAndReimport();
  }
  AssetDatabase.SaveAssets();return "Imported six existing crop stages with URP materials";
 }
 public static string Main()=>string.Join("\n",new[]{"Facilities/CC_MushroomFarm","Facilities/CC_PlanterBed","Facilities/CC_FarmTile"}.Select(path=>{
 var p=Resources.Load<GameObject>(path);return path+"\n"+string.Join("\n",p.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+" | "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"null"))));}));
}
