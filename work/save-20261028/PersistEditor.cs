using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PersistEditor
{
 public static string Main()
 {
  if(Application.isPlaying)throw new Exception("Stop Play before preserving Editor scenes");var result=new List<string>();var folder=Path.GetFullPath("work/save-20261028/editor-scene-backup");Directory.CreateDirectory(folder);
  for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(!scene.isDirty){result.Add(scene.path+" clean");continue;}if(string.IsNullOrEmpty(scene.path))throw new Exception("Unsaved unnamed scene requires preservation before shutdown");var source=Path.GetFullPath(scene.path);if(File.Exists(source))File.Copy(source,Path.Combine(folder,i+"-"+Path.GetFileName(source)),true);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed "+scene.path);result.Add(scene.path+" saved; previous disk copy preserved outside Assets");}
  AssetDatabase.SaveAssets();var text=string.Join("\n",result);File.WriteAllText("work/save-20261028/editor-preservation.txt",text);return text;
 }
}
