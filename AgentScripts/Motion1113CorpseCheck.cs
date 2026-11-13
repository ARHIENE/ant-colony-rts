using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.World;
public static class Motion1113CorpseCheck
{
 public static async Task<string> Run()
 {
  AntColony.UI.SimulationPause.UserSpeed(0);
  var c=Corpse.Spawn(new Corpse.State{kind=CorpseKind.EnemyCommander,name="Paused corpse test",position=new Vector3(0,0,0),count=1});
  try
  {
   await Task.Delay(200);
   var bones=c.GetComponentsInChildren<Transform>();var head=bones.First(t=>t.name=="mixamorig:Head");var foot=bones.First(t=>t.name=="mixamorig:LeftFoot");
   float before=Mathf.Abs(head.position.y-foot.position.y);
   var native=c.GetComponentInChildren<MeshyNativeCorpse>();
   var clip=(AnimationClip)typeof(MeshyNativeCorpse).GetField("clip",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(native);
   var animator=(Animator)typeof(MeshyNativeCorpse).GetField("animator",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(native);
   clip.SampleAnimation(animator.gameObject,clip.length-.001f);
   float sampled=Mathf.Abs(head.position.y-foot.position.y);
   string result="paused live head-foot height="+before+" source-end height="+sampled+" clip="+clip.name+" loop="+clip.isLooping;
   System.IO.File.WriteAllText("work/motion-actions-1113/corpse-check.txt",result);return result;
  }finally{UnityEngine.Object.Destroy(c.gameObject);}
 }
}
