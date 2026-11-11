using System.Linq;
using UnityEngine;
using UnityEditor;
using AntColony.Units;
public static class Meshy1111BowBinding
{
 public static string Run()
 {
  var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Library");
  library.actions.First(a=>a.action=="BowShoot").motion="Archery_Shot_3";
  library.actions.First(a=>a.action=="Tool_Chop").motion="Heavy_Hammer_Swing";
  var runtime=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
  runtime.actions=library.actions;runtime.motions=library.motions.Where(m=>library.actions.Any(a=>a.motion==m.id)).ToArray();
  EditorUtility.SetDirty(library);EditorUtility.SetDirty(runtime);AssetDatabase.SaveAssets();
  return "Native BowShoot=Archery_Shot_3, Tool_Chop=Heavy_Hammer_Swing; runtime="+runtime.motions.Length+" original clips.";
 }
}
