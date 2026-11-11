using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.EditorTools
{
    // Retarget to the existing modular character. Never replace its mesh or appearance parts.
    public static class MeshyMotionImport
    {
        const string Sources = "Assets/_Project/Art/MeshyAnimation1110";
        const string Output = "Assets/Resources/Kemono/MeshyMotions";
        static readonly Dictionary<string, string> Map = BoneMap();
        static Dictionary<string,string> BoneMap()
        {
            var map = new Dictionary<string,string> {{"pevis","Hips"},{"spine","Spine"},{"spine_01","Spine1"},{"spine_02","Spine2"},{"neck","Neck"},{"head","Head"}};
            foreach (var side in new[]{"L","R"})
            {
                var s = side == "L" ? "Left" : "Right";
                foreach(var p in new[]{("shoulder","Shoulder"),("upper_arm","Arm"),("forearm","ForeArm"),("hand","Hand"),("thigh","UpLeg"),("shin","Leg"),("foot","Foot")}) map[p.Item1+"."+side] = s+p.Item2;
                foreach(var f in new[]{"index","middle","ring","pinky","thumb"})
                    for(int j=1;j<=3;j++) map[(f=="thumb"?"thumb":"f_"+f)+".0"+j+"."+side] = s+"Hand"+char.ToUpper(f[0])+f.Substring(1)+j;
            }
            return map;
        }
        public static readonly Dictionary<string,string> Actions = new Dictionary<string,string> {
            {"Tool_Mine","Heavy_Hammer_Swing"},{"Tool_Chop","Charged_Axe_Chop"},
            {"Tool_Hammer","Heavy_Hammer_Swing"},{"SmithRepair","Heavy_Hammer_Swing"},
            {"Tool_Harvest","Pull_Radish"},{"GroundWork","Collect_Object"},
            {"Carry","Carry_Heavy_Object_Walk"},{"CarryPerson","Carry_Heavy_Object_Walk"},
            {"Walk","Walking"},{"Run","Running"},{"WalkInjured","Injured_Walk"},
            {"Sleep","Sleep_Normally"},{"Downed","Prone_Reach_Help"},
            {"Rest","Sit_Cross_Legged_on_Floor"},{"SkyGaze","Long_Breathe_and_Look_Around"},
            {"Inspect","Female_Bend_Over_Pick_Up_Inspect"},
            {"Hit","Hit_Reaction"},{"Death","Dead"},
            {"Attack","Right_Hand_Sword_Slash"},{"ShieldBlock","Shield_Push_Left"},
            {"BowShoot","Archery_Shot_2"},{"Celebrate","Victory_Fist_Pump"},
            {"IdleVariant0","Idle_3"},{"IdleVariant1","Idle_4"},{"IdleVariant2","Idle_5"},
            {"Talk","Talk_with_Hands_Open"},{"Listen","Listening_Gesture"}
        };
        [MenuItem("AntColony/Kemono/Retarget collected Meshy motions")]
        public static string Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before rebuilding clips.");
            Directory.CreateDirectory(Sources); Directory.CreateDirectory(Output);
            var files = Directory.GetFiles("art/meshy-animation-1110/fbx","*.fbx").ToDictionary(Path.GetFileNameWithoutExtension);
            foreach(var folder in new[]{"preset-bundle","life-bundle"})
                foreach(var path in Directory.GetFiles("art/meshy-animation-1106/"+folder,"*.fbx",SearchOption.AllDirectories))
                { var name=Path.GetFileNameWithoutExtension(path).Split(new[]{"animated_mixamo_"},StringSplitOptions.None).Last(); if(!files.ContainsKey(name)) files[name]=path; }
            var selected=Actions.Values.Distinct().ToArray();
            foreach(var name in selected) File.Copy(files[name],Sources+"/"+name+".fbx",true);
            AssetDatabase.Refresh();
            var report=new List<string>();
            foreach(var name in selected)
            {
                var path=Sources+"/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=true; importer.animationCompression=ModelImporterAnimationCompression.Off;
                importer.materialImportMode=ModelImporterMaterialImportMode.None;
                importer.importCameras=importer.importLights=false; importer.optimizeGameObjects=false;
                importer.SaveAndReimport();
                report.Add(Bake(path,name,name!="Dead"&&name!="Hit_Reaction"));
            }
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(KemonoImport.Controller);
            var sm=controller.layers[0].stateMachine;
            foreach(var action in Actions)
            {
                var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==action.Key)??sm.AddState(action.Key);
                state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Output+"/"+action.Value+".anim");
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
            File.WriteAllLines("work/animation-1110/import-report.txt",report);
            return "Retargeted "+selected.Length+" clips; connected "+Actions.Count+" states";
        }
        static void RestoreBind(GameObject go)
        {
            var poses=new Dictionary<Transform,Matrix4x4>();
            foreach(var s in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                for(int i=0;i<s.bones.Length;i++)if(s.bones[i]&&!poses.ContainsKey(s.bones[i]))poses[s.bones[i]]=s.transform.localToWorldMatrix*s.sharedMesh.bindposes[i].inverse;
            foreach(var t in go.GetComponentsInChildren<Transform>())if(poses.TryGetValue(t,out var m)){t.position=m.GetColumn(3);t.rotation=m.rotation;}
        }
        public static string Bake(string path,string name,bool loop)
        {
            var src=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var dst=Object.Instantiate(Resources.Load<GameObject>("Kemono/Kemono_Character"));
            var reference=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/MeshyAnimation1106/Rerig_All.fbx"));
            try
            {
                RestoreBind(reference);RestoreBind(dst);
                var a=src.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                var b=dst.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                var rest=reference.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                // Skeleton-only exports have no bindposes. Use the same character's skinned reference.
                if(src.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0)
                    foreach(var t in reference.GetComponentsInChildren<Transform>())if(a.TryGetValue(t.name,out var bone)){bone.localPosition=t.localPosition;bone.localRotation=t.localRotation;bone.localScale=t.localScale;}
                else RestoreBind(src);
                var rig=b["KemonoRig"];
                var pairs=dst.GetComponentsInChildren<Transform>().Where(t=>Map.ContainsKey(t.name)&&a.ContainsKey("mixamorig:"+Map[t.name])).Select(t=>(target:t,source:a["mixamorig:"+Map[t.name]])).ToArray();
                var correction=pairs.Select(p=>Quaternion.Inverse(rest[p.source.name].rotation)*p.target.rotation).ToArray();
                var pos=pairs.Select(p=>p.target.localPosition).ToArray();
                var srcHip=rest["mixamorig:Hips"].position;var dstHip=b["pevis"].position;
                float scale=dstHip.y/srcHip.y;
                var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
                var clip=new AnimationClip{name=name,frameRate=30};int frames=Mathf.CeilToInt(source.length*30);
                var curves=pairs.Select(p=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray()).ToArray();
                var last=new Quaternion[pairs.Length];Vector3 firstHip=Vector3.zero;
                for(int f=0;f<=frames;f++)
                {
                    float time=source.length*f/frames;source.SampleAnimation(src,time);
                    if(f==0)firstHip=a["mixamorig:Hips"].position;
                    for(int i=0;i<pairs.Length;i++)
                    {
                        var p=pairs[i];p.target.localPosition=pos[i];p.target.rotation=p.source.rotation*correction[i];
                        // Simulation owns travel. Retain vertical posture, discard horizontal root drift.
                        if(p.target.name=="pevis")p.target.position=dstHip+Vector3.up*(p.source.position.y-srcHip.y)*scale;
                        var q=p.target.localRotation;if(f>0&&Quaternion.Dot(q,last[i])<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);last[i]=q;
                        var v=p.target.localPosition;float[] values={q.x,q.y,q.z,q.w,v.x,v.y,v.z};
                        for(int j=0;j<7;j++)curves[i][j].AddKey(time,values[j]);
                    }
                }
                string[] props={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};
                for(int i=0;i<pairs.Length;i++)for(int j=0;j<7;j++)clip.SetCurve(AnimationUtility.CalculateTransformPath(pairs[i].target,rig),typeof(Transform),props[j],curves[i][j]);
                // Match the existing controller's centimetre conversion, including transitions from old clips.
                foreach(var axis in new[]{"x","y","z"})clip.SetCurve("",typeof(Transform),"m_LocalScale."+axis,AnimationCurve.Constant(0,source.length,100));
                var root=b["Root"];var rot=root.localRotation;
                for(int j=0;j<4;j++)clip.SetCurve("Root",typeof(Transform),props[j],AnimationCurve.Constant(0,source.length,new[]{rot.x,rot.y,rot.z,rot.w}[j]));
                clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
                var output=Output+"/"+name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
                if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,output);
                return name+" bones="+pairs.Length+" frames="+frames;
            }
            finally{Object.DestroyImmediate(src);Object.DestroyImmediate(dst);Object.DestroyImmediate(reference);}
        }
        public static string Rebake()
        {
            var report=new List<string>();
            foreach(var name in Actions.Values.Distinct())report.Add(Bake(Sources+"/"+name+".fbx",name,name!="Dead"&&name!="Hit_Reaction"));
            AssetDatabase.SaveAssets();File.WriteAllLines("work/animation-1110/import-report.txt",report);return "Rebaked "+report.Count;
        }
    }
}
