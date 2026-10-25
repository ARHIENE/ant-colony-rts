using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ImportCritterModels
{
    public static async Task<string> Main(int index)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before import");
        var species = new[] { "Aphid", "Silkworm", "Spider", "Bee", "Wasp", "NightBeetle" }[index];
        var folder = "Assets/Resources/Critters/" + species;
        Directory.CreateDirectory(folder);
        var modelPath = folder + "/" + species + ".fbx";
        Copy("art/critter-kit/exports/" + species + ".fbx", modelPath);
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        Configure(modelImporter); modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        modelImporter.SaveAndReimport();
        var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().First(a => a.isValid);
        typeof(AntColony.EditorTools.KemonoImport).GetMethod("ConvertMaterials", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { modelPath, false });
        var controllerPath = folder + "/" + species + "Animator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        var count = 0;
        foreach (var source in Directory.GetFiles("art/critter-kit/exports", species + "@*.fbx").OrderBy(p => p))
        {
            var name = Path.GetFileNameWithoutExtension(source).Split('@')[1];
            var path = folder + "/" + Path.GetFileName(source); Copy(source, path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Configure(importer); importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; importer.sourceAvatar = avatar;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = name; clip.loopTime = new[] { "Idle", "Walk", "Eat", "Lie", "Bound", "Produce", "Fly" }.Contains(name);
                clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var motion = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
            state.motion = motion; if (name == "Idle") machine.defaultState = state;
            File.WriteAllText("Temp/critter-import-progress.txt", species + " " + name);
            count++; await Task.Yield();
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        return "Imported " + species + ": model + " + count + " clips; valid avatar";
    }

    static void Copy(string source, string target)
    {
        if (!File.Exists(target)) File.Copy(source, target);
        AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
    }
    static void Configure(ModelImporter importer)
    {
        importer.animationType = ModelImporterAnimationType.Generic; importer.importAnimation = true;
        importer.bakeAxisConversion = true; importer.optimizeGameObjects = false;
        importer.importCameras = importer.importLights = importer.addCollider = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }
}
