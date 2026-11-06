using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class MeshyAnimation1106PlaybackAudit
{
 public static string Main(){var w=Resources.FindObjectsOfTypeAll<EditorWindow>().First(x=>x.GetType().Name=="MeshyAnimation1106Live");var type=w.GetType();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var model=(GameObject)type.GetField("model",flags).GetValue(w);var clip=(AnimationClip)type.GetField("clip",flags).GetValue(w);var animator=model.GetComponent<Animator>();var text=new System.Text.StringBuilder();text.AppendLine("clip="+clip.name+" legacy="+clip.legacy+" animator enabled="+animator.enabled+" initialized="+animator.isInitialized+" avatar="+(animator.avatar?animator.avatar.name:"NONE"));var bones=model.GetComponentsInChildren<Transform>().Where(t=>new[]{"mixamorig:RightArm","mixamorig:RightForeArm","mixamorig:LeftArm","mixamorig:LeftForeArm"}.Contains(t.name)).ToArray();type.GetField("running",flags).SetValue(w,false);foreach(float phase in new[]{0f,.25f,.5f,.75f}){type.GetField("phase",flags).SetValue(w,phase);type.GetMethod("Tick",flags).Invoke(w,null);text.AppendLine("phase="+phase);foreach(var b in bones)text.AppendLine(b.name+" local="+b.localEulerAngles.ToString("F2")+" usedBySkin="+model.GetComponentInChildren<SkinnedMeshRenderer>().bones.Contains(b));}type.GetField("phase",flags).SetValue(w,.55f);type.GetMethod("Tick",flags).Invoke(w,null);return text.ToString();}
}


