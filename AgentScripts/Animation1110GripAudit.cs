using System;
using System.Linq;
using UnityEngine;
using AntColony.Units;
public static class Animation1110GripAudit {
 public static string Main(){var owner=new GameObject("Grip audit");var model=KemonoModel.Create(owner.transform,42);var a=RigAnimator.Find(model);a.enabled=false;var g=model.GetComponent<MeshyEquipmentGrip>();g.enabled=false;g.Motion="Tool_Mine";KemonoModel.Hold(model,"Tool_Pickaxe_Stone");var clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/Heavy_Hammer_Swing");var ts=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);float max=0,minHead=99;for(int i=0;i<60;i++){clip.SampleAnimation(a.gameObject,clip.length*i/60);g.Pose(false,i/60f);var main=ts["Socket.Tool.R"];var support=ts["Socket.Tool.L"];max=Mathf.Max(max,Vector3.Distance(main.position-main.forward*.095f,support.position));minHead=Mathf.Min(minHead,Vector3.Distance(main.position,ts["head"].position));}UnityEngine.Object.DestroyImmediate(owner);return "support error max="+max+" hand/head min="+minHead;}
}
