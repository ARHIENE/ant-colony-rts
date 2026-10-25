using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class ImportResidentModel
{
    static void Copy(string source,string target){if(!File.Exists(target))File.Copy(source,target);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);}
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var folder="Assets/Resources/Residents";Directory.CreateDirectory(folder);
        var path=folder+"/Resident_Combo_A.fbx";Copy("art/resident-kit/exports/character/Resident_Combo_A.fbx",path);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);Configure(importer);importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();
        var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First(a=>a.isValid);
        typeof(AntColony.EditorTools.KemonoImport).GetMethod("ConvertMaterials",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{path,false});
        var controllerPath=folder+"/ResidentAnimator.controller";var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(ctrl==null)ctrl=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach(var clipName in new[]{"Idle","Walk"})
        {
            path=folder+"/Resident@"+clipName+".fbx";Copy("art/resident-kit/exports/anim/Resident@"+clipName+".fbx",path);
            importer=(ModelImporter)AssetImporter.GetAtPath(path);Configure(importer);importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=avatar;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var clips=importer.defaultClipAnimations;foreach(var c in clips){c.name=clipName;c.loopTime=true;c.lockRootRotation=c.lockRootHeightY=c.lockRootPositionXZ=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
            var machine=ctrl.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==clipName);if(state==null)state=machine.AddState(clipName);state.motion=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));if(clipName=="Idle")machine.defaultState=state;
        }
        var basket="Assets/Resources/ResourceModels/Prop_Basket.fbx";Copy("art/resident-kit/exports/tools/Prop_Basket.fbx",basket);typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{basket,false});
        EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssets();return "Imported resident + Idle/Walk + loot basket";
    }
    static void Configure(ModelImporter m){m.animationType=ModelImporterAnimationType.Generic;m.importAnimation=true;m.bakeAxisConversion=true;m.optimizeGameObjects=false;m.importCameras=m.importLights=m.addCollider=false;m.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;m.materialLocation=ModelImporterMaterialLocation.InPrefab;}
}
