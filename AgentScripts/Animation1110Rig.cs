using System.Linq;
using UnityEngine;
using UnityEditor;
public static class Animation1110Rig {
 public static string Main(){var lines=new System.Collections.Generic.List<string>();foreach(var path in new[]{"Assets/Resources/Kemono/Kemono_Character.fbx","Assets/_Project/Art/MeshyAnimation1106/Rerig_All.fbx"}){var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));try{lines.Add(path);foreach(var t in go.GetComponentsInChildren<Transform>())if(t.childCount>0&&!t.name.StartsWith("Part"))lines.Add(t.name+" parent="+t.parent?.name+" localScale="+t.localScale+" localPos="+t.localPosition+" pos="+t.position.ToString("F3")+" rot="+t.rotation.eulerAngles.ToString("F1"));}finally{Object.DestroyImmediate(go);}}System.IO.File.WriteAllLines("work/animation-1110/rig.txt",lines);return string.Join("\n",lines);}
}

