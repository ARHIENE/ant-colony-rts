using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using AntColony.Units;
public static class Meshy1111LegacyAudit
{
 public static string Run()
 {
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Kemono/KemonoAnimator.controller");
  var model=KemonoModel.Create(null,123);var animator=RigAnimator.Find(model);var report=new List<string>();
  try
  {
   var bones=model.GetComponentsInChildren<Transform>().Where(t=>new[]{"upper_arm.L","upper_arm.R","forearm.L","forearm.R","hand.L","hand.R","shin.L","shin.R","foot.L","foot.R"}.Contains(t.name)).ToArray();
   var rest=bones.Select(t=>t.localPosition).ToArray();
   foreach(var state in controller.layers[0].stateMachine.states)
   {
    if(!(state.state.motion is AnimationClip clip))continue;
    float delta=0,movement=0;Quaternion[] rotations=null;
    for(int f=0;f<=10;f++)
    {
     clip.SampleAnimation(animator.gameObject,clip.length*f/10f);
     if(f==0)rotations=bones.Select(t=>t.localRotation).ToArray();
     for(int i=0;i<bones.Length;i++){delta=Mathf.Max(delta,Vector3.Distance(rest[i],bones[i].localPosition));movement=Mathf.Max(movement,Quaternion.Angle(rotations[i],bones[i].localRotation));}
    }
    report.Add(state.state.name+" clip="+clip.name+" jointMotion="+movement.ToString("F2")+" restTranslationDelta="+delta.ToString("F4")+(delta>.005?" REVIEW":" PASS"));
   }
  }finally{UnityEngine.Object.DestroyImmediate(model);}
  System.IO.File.WriteAllLines("work/animation-1111/legacy-audit.txt",report);return string.Join("\n",report);
 }
}
