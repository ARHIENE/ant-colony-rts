using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class Animation1110Bake
{
 public static readonly Dictionary<string,string> Map=new Dictionary<string,string>{
 {"pevis","Hips"},{"spine","Spine"},{"spine_01","Spine1"},{"spine_02","Spine2"},{"neck","Neck"},{"head","Head"}};
 static void BindPose(GameObject go){var data=new Dictionary<Transform,Matrix4x4>();foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>())for(int i=0;i<s.bones.Length;i++)if(s.bones[i]&&!data.ContainsKey(s.bones[i]))data[s.bones[i]]=s.transform.localToWorldMatrix*s.sharedMesh.bindposes[i].inverse;foreach(var t in go.GetComponentsInChildren<Transform>())if(data.TryGetValue(t,out var m)){t.position=m.GetColumn(3);t.rotation=m.rotation;}}
 public static string Main(string sourcePath="Assets/_Project/Art/MeshyAnimation1106/Heavy_Hammer_Swing.fbx",string clipName="",string outputName="Tool_Mine",bool loop=true){
 foreach(var side in new[]{"L","R"}){string s=side=="L"?"Left":"Right";foreach(var p in new[]{("shoulder","Shoulder"),("upper_arm","Arm"),("forearm","ForeArm"),("hand","Hand"),("thigh","UpLeg"),("shin","Leg"),("foot","Foot")})Map[p.Item1+"."+side]=s+p.Item2;}
 var src=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));var dst=Object.Instantiate(Resources.Load<GameObject>("Kemono/Kemono_Character"));
 try{BindPose(src);BindPose(dst);var a=src.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var b=dst.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);var rig=b["KemonoRig"];var pairs=dst.GetComponentsInChildren<Transform>().Where(t=>Map.ContainsKey(t.name)&&a.ContainsKey("mixamorig:"+Map[t.name])).Select(t=>(target:t,source:a["mixamorig:"+Map[t.name]])).ToArray();var correction=pairs.Select(p=>Quaternion.Inverse(p.source.rotation)*p.target.rotation).ToArray();var pos=pairs.Select(p=>p.target.localPosition).ToArray();var srcHip=a["mixamorig:Hips"].position;var dstHip=b["pevis"].position;float scale=dstHip.y/srcHip.y;
 var sourceClip=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview")&&(clipName==""||c.name==clipName));var clip=new AnimationClip{name=outputName,frameRate=30};int frames=Mathf.CeilToInt(sourceClip.length*30);var curves=pairs.Select(p=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray()).ToArray();var last=new Quaternion[pairs.Length];
 for(int f=0;f<=frames;f++){float time=sourceClip.length*f/frames;sourceClip.SampleAnimation(src,time);for(int i=0;i<pairs.Length;i++){var p=pairs[i];p.target.localPosition=pos[i];p.target.rotation=p.source.rotation*correction[i];if(p.target.name=="pevis")p.target.position=dstHip+(p.source.position-srcHip)*scale;var q=p.target.localRotation;if(f>0&&Quaternion.Dot(q,last[i])<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);last[i]=q;var v=p.target.localPosition;float[] values={q.x,q.y,q.z,q.w,v.x,v.y,v.z};for(int j=0;j<7;j++)curves[i][j].AddKey(time,values[j]);}}
 for(int i=0;i<pairs.Length;i++){var path=AnimationUtility.CalculateTransformPath(pairs[i].target,rig);string[] props={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};for(int j=0;j<7;j++)clip.SetCurve(path,typeof(Transform),props[j],curves[i][j]);}clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);Directory.CreateDirectory("Assets/Resources/Kemono/MeshyMotions");var outPath="Assets/Resources/Kemono/MeshyMotions/"+outputName+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,outPath);AssetDatabase.SaveAssets();return outPath+" bones="+pairs.Length+" frames="+frames;
 }finally{Object.DestroyImmediate(src);Object.DestroyImmediate(dst);}
 }
}
