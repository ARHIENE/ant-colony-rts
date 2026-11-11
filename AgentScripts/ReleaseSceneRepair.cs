using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ReleaseSceneRepair
{
 public static string Run()
 {
  if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Edit and ready required");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/_Project/Scenes/AntColony.unity")throw new Exception("Unexpected scene");
  var roots=scene.GetRootGameObjects();
  var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
  var missing=all.Where(t=>PrefabUtility.GetPrefabInstanceStatus(t.gameObject)==PrefabInstanceStatus.MissingAsset).ToArray();
  var trees=missing.Where(t=>(t.name.StartsWith("Tree_01 (")||t.name.StartsWith("Tree_02 (")||t.name.StartsWith("Rock_05 ("))&& t.GetComponents<Component>().Length==1 && t.childCount==0 && t.parent!=null && t.parent.name.StartsWith("TerrainGenerator")).ToArray();
  var bosses=missing.Where(t=>t.name.StartsWith("MiniBirdBoss (")).ToArray();
  if(trees.Length!=15390 || bosses.Length!=1 || missing.Length!=15391)throw new Exception("Unexpected missing asset inventory");
  var boss=bosses[0].gameObject;
  var components=boss.GetComponents<Component>();
  var componentState=components.Select(c=>c is MonoBehaviour ? JsonUtility.ToJson(c) : null).ToArray();
  // Unpacking retains all added gameplay components; do not delete the missing boss.
  PrefabUtility.UnpackPrefabInstance(boss,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  if(!components.SequenceEqual(boss.GetComponents<Component>()))throw new Exception("Boss components changed");
  // Transform serialization contains prefab linkage; compare gameplay component values only.
  for(int i=1;i<components.Length;i++)if(componentState[i]!=JsonUtility.ToJson(components[i]))throw new Exception("Boss gameplay data changed");
  boss.name="MiniBirdBoss";
  foreach(var t in trees)UnityEngine.Object.DestroyImmediate(t.gameObject);
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
  var report="Removed 15390 empty broken landscape instances; preserved boss component identities and serialized gameplay values; scene saved";
  File.WriteAllText("work/release-visual-pass/scene-repair.txt",report);return report;
 }
}
