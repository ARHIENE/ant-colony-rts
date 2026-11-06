using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class MeshyAnimation1106Select
{
 public static string Main(int index=1,float phase=0,bool play=true){var w=Resources.FindObjectsOfTypeAll<EditorWindow>().First(x=>x.GetType().Name=="MeshyAnimation1106Live");var type=w.GetType();var flags=BindingFlags.Instance|BindingFlags.NonPublic;type.GetField("selection",flags).SetValue(w,index);type.GetMethod("Build",flags).Invoke(w,null);type.GetField("phase",flags).SetValue(w,phase);type.GetField("running",flags).SetValue(w,play);type.GetMethod("Tick",flags).Invoke(w,null);return "Selected clip index="+index+" phase="+phase+" playing="+play;}
}
