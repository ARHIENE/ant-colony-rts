using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MeshyAnimation1106RootAudit
{
 public static string Main(){var text=new System.Text.StringBuilder();foreach(var file in new[]{"Heavy_Hammer_Swing.fbx","Tool_Chop_SharedRig.fbx","Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.fbx"}){
 var path="Assets/_Project/Art/MeshyAnimation1106/"+file;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=Object.Instantiate(prefab);try{var anim=go.GetComponent<Animator>();text.AppendLine(file+" root="+go.transform.localEulerAngles+" scale="+go.transform.localScale+" avatar="+(anim?anim.avatar?.name:"NONE"));foreach(var t in go.GetComponentsInChildren<Transform>().Where(t=>t.parent==go.transform||t.name.EndsWith("Hips")))text.AppendLine("initial "+t.name+" rot="+t.localEulerAngles+" scale="+t.localScale);var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));clip.SampleAnimation(go,0);text.AppendLine("sample root="+go.transform.localEulerAngles);foreach(var t in go.GetComponentsInChildren<Transform>().Where(t=>t.parent==go.transform||t.name.EndsWith("Hips")))text.AppendLine("sample "+t.name+" rot="+t.localEulerAngles+" scale="+t.localScale);
 }finally{Object.DestroyImmediate(go);}}
 return text.ToString();}
}

