using UnityEditor;
public static class HandGrip1106Import
{
 public static string Main(string path="Assets/Resources/Kemono/Kemono_Character.fbx"){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);return "Imported "+path;}
}
