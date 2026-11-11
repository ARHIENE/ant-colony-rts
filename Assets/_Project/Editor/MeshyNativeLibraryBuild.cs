using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AntColony.Units;
using UnityEditor;
using UnityEngine;

namespace AntColony.EditorTools
{
    public static class MeshyNativeLibraryBuild
    {
        const string Output="Assets/Resources/MeshyNative";
        const string Source="Assets/_Project/Art/MeshyNativeSources";
        const string Original="Assets/_Project/Art/MeshyAnimation1106/";
        static string Id(string path)=>Path.GetFileNameWithoutExtension(path).Split(new[]{"animated_mixamo_"},StringSplitOptions.None).Last();
        public static string Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
            Directory.CreateDirectory(Source);Directory.CreateDirectory(Output);
            var files=new Dictionary<string,string>();
            foreach(var folder in new[]{"preset-bundle","life-bundle","run2","run3","heavy-hammer","archery3"})
            {
                var directory="art/meshy-animation-1106/"+folder;
                if(Directory.Exists(directory))foreach(var p in Directory.GetFiles(directory,"*.fbx",SearchOption.AllDirectories))files[Id(p)]=p;
            }
            foreach(var p in Directory.GetFiles("art/meshy-animation-1110/fbx","*.fbx"))files[Id(p)]=p;
            foreach(var p in Directory.GetFiles("Assets/_Project/Art/MeshyAnimation1110","*.fbx"))if(!files.ContainsKey(Id(p)))files[Id(p)]=p;
            foreach(var pair in files)if(!File.Exists(Source+"/"+pair.Key+".fbx"))File.Copy(pair.Value,Source+"/"+pair.Key+".fbx");
            AssetDatabase.Refresh();
            var characters=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(Original+"Heavy_Hammer_Swing.fbx"),AssetDatabase.LoadAssetAtPath<GameObject>(Original+"Rerig_All.fbx")};
            var entries=new List<MeshyNativeLibrary.Motion>();var report=new List<string>();
            foreach(var id in files.Keys.OrderBy(s=>s))
            {
                var path=Source+"/"+id+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                if(importer.animationType!=ModelImporterAnimationType.Generic||importer.animationCompression!=ModelImporterAnimationCompression.Off||importer.materialImportMode!=ModelImporterMaterialImportMode.None)
                {
                    importer.animationType=ModelImporterAnimationType.Generic;importer.animationCompression=ModelImporterAnimationCompression.Off;
                    importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.SaveAndReimport();
                }
                var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var bones=src.GetComponentsInChildren<Transform>();
                var hand=bones.First(t=>t.name=="mixamorig:RightHand");var elbow=bones.First(t=>t.name=="mixamorig:RightForeArm");
                int rig=Vector3.Distance(hand.position,elbow.position)>.162f?1:0;
                foreach(var input in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))
                {
                    var bindings=AnimationUtility.GetCurveBindings(input);
                    bool armatureRoot=bindings.Any(b=>b.path.StartsWith("mixamorig:"));
                    foreach(var binding in bindings)
                    {
                        var target=binding;
                        if(target.path.StartsWith("mixamorig:"))target.path="SmartRigArmature/"+target.path;
                        if(target.path!=""&&!characters[rig].transform.Find(target.path))throw new InvalidOperationException(id+" unmatched bone "+target.path);
                    }
                    bool loop=!new[]{"Arise","Dead","falling_down","Hit_Reaction","Shot_and_Blown_Back","Ladder_Climb_Finish","Ladder_Mount_Start","Sit_to_Stand_Transition_M","Stand_to_Sit_Transition_M","Walk_to_Sit","Stand_Clap_and_Sit_Down","Sit_Lie_Bed","sleep","Regular_Jump","Jump_Over_Obstacle_1","Roll_Dodge"}.Contains(id);
                    float span=0;
                    if(id=="Heavy_Hammer_Swing"||id=="Charged_Axe_Chop")
                    {
                        var model=UnityEngine.Object.Instantiate(characters[rig]);
                        try
                        {
                            var root=armatureRoot?model.transform.Find("SmartRigArmature").gameObject:model;
                            var pose=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                            Vector3 Palm(string side)=>Vector3.Lerp(pose["mixamorig:"+side+"Hand"].position,pose["mixamorig:"+side+"HandMiddle1"].position,.65f);
                            for(int frame=0;frame<=60;frame++){input.SampleAnimation(root,input.length*frame/60f);span=Mathf.Max(span,Vector3.Distance(Palm("Left"),Palm("Right")));}
                        }
                        finally{UnityEngine.Object.DestroyImmediate(model);}
                    }
                    entries.Add(new MeshyNativeLibrary.Motion{id=id,clip=input,rig=rig,loop=loop,armatureRoot=armatureRoot,maxHandSpan=span});
                    report.Add(id+" | native rig "+rig+" | "+bindings.Length+" original curves | "+input.length+"s");
                    break;
                }
            }
            var asset=Output+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<MeshyNativeLibrary>(asset);
            if(!library){library=ScriptableObject.CreateInstance<MeshyNativeLibrary>();AssetDatabase.CreateAsset(library,asset);}
            library.characters=characters;library.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Original+"Meshy_AI_Meadow_Sprout_20261106_animated_mixamo_All_animation.png");
            library.motions=entries.ToArray();library.actions=MeshyNativeActionBindings.All().Select(a=>new MeshyNativeLibrary.ActionBinding{action=a.Key,motion=a.Value}).ToArray();
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Directory.CreateDirectory("work/animation-1111");File.WriteAllLines("work/animation-1111/native-library.txt",report);
            return "Imported "+entries.Count+" native motions; bone paths validated; "+library.actions.Length+" action bindings. Rig 0="+entries.Count(e=>e.rig==0)+", rig 1="+entries.Count(e=>e.rig==1)+". Visual/gear validation pending.";
        }
    }
}
