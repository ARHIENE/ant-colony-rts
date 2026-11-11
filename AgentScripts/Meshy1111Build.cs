using System.Linq;
public static class Meshy1111Build
{
 public static string Run()=>AntColony.EditorTools.MeshyNativeLibraryBuild.Build();
 public static string Configure()
 {
  var library=UnityEngine.Resources.Load<AntColony.Units.MeshyNativeLibrary>("MeshyNative/Library");
  const string runtimePath="Assets/Resources/MeshyNative/Runtime.asset";
  var runtime=UnityEditor.AssetDatabase.LoadAssetAtPath<AntColony.Units.MeshyNativeLibrary>(runtimePath);
  if(!runtime){runtime=UnityEngine.ScriptableObject.CreateInstance<AntColony.Units.MeshyNativeLibrary>();UnityEditor.AssetDatabase.CreateAsset(runtime,runtimePath);}
  runtime.characters=library.characters;runtime.texture=library.texture;runtime.actions=library.actions;
  runtime.motions=library.motions.Where(m=>library.actions.Any(a=>a.motion==m.id)).ToArray();
  UnityEditor.EditorUtility.SetDirty(runtime);
  var path="Assets/Resources/MeshyNativeTrial.asset";
  var config=UnityEditor.AssetDatabase.LoadAssetAtPath<AntColony.Units.MeshyNativeTrial>(path);
  if(!config){config=UnityEngine.ScriptableObject.CreateInstance<AntColony.Units.MeshyNativeTrial>();UnityEditor.AssetDatabase.CreateAsset(config,path);}
  config.library=runtime;config.enabledForFirstCommander=true;
  UnityEditor.EditorUtility.SetDirty(config);UnityEditor.AssetDatabase.SaveAssets();
  return "First-commander native library trial persists across Play reload. Other modular appearances unchanged.";
 }
}
