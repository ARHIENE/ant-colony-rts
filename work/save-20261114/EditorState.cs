using System.Linq;using UnityEditor;using UnityEngine.SceneManagement;
public static class EditorState {public static string Run()=>"playing="+EditorApplication.isPlaying+" compiling="+EditorApplication.isCompiling+" scenes="+string.Join(";",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var s=SceneManager.GetSceneAt(i);return s.path+" dirty="+s.isDirty;}));}
