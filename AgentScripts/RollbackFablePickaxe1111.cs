using UnityEditor;
public static class RollbackFablePickaxe1111
{
 public static string RestoreImporters()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop Play first");
  int changed=0;
  foreach(var path in new[]{"Assets/Resources/Environment1110/Models/Rock/Rock.fbx","Assets/Resources/Environment1110/Models/CopperDeposit/CopperDeposit.fbx","Assets/Resources/Environment1110/Models/IronDeposit/IronDeposit.fbx","Assets/Resources/ResourceModels/CC_OreOutcrop.fbx"})
  {
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);
   if(importer.isReadable){importer.isReadable=false;importer.SaveAndReimport();changed++;}
  }
  return "Restored pre-Fable Read/Write=false on "+changed+" mining models";
 }
}
