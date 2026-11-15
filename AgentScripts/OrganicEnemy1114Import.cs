using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class OrganicEnemy1114Import
{
    public static string RefreshAnimations(string creature)
    {
        string root="Assets/Resources/OrganicEnemies1114/"+creature;
        if(!File.Exists(root+"/"+creature+".controller")) throw new Exception("Import model first: "+creature);
        var names=creature=="BloomMender"?new[]{"Idle","Move","Attack","Hit","Death","Heal"}:new[]{"Idle","Move","Attack","Hit","Death"};
        foreach(var name in names)
        {
            string target=root+"/"+name+".fbx";
            File.Copy("art/enemy-concepts-1114/"+creature+"/"+name+".fbx",target,true);
            AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);
            var clip=AssetDatabase.LoadAllAssetsAtPath(target).OfType<AnimationClip>().First(c=>c.name==name);
            if(clip.length<=0 || AnimationUtility.GetAnimationClipSettings(clip).loopTime!=(name=="Idle"||name=="Move"))
                throw new Exception("Invalid clip after refresh: "+creature+"/"+name);
        }
        AssetDatabase.SaveAssets();
        return creature+": refreshed "+names.Length+" clips; existing material/controller metadata preserved.";
    }

    public static string RefreshDeaths()
    {
        var report = new StringBuilder();
        var sources = Directory.GetDirectories("art/enemy-concepts-1114")
            .Where(d => File.Exists(d + "/death-refined.txt")).ToArray();
        if (sources.Length != 17) throw new Exception("Expected 17 refined models, got " + sources.Length);
        foreach (var source in sources)
        {
            string name = Path.GetFileName(source);
            string target = "Assets/Resources/OrganicEnemies1114/" + name + "/Death.fbx";
            if (!File.Exists(target)) throw new Exception("Import model first: " + name);
            File.Copy(source + "/Death.fbx", target, true);
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAllAssetsAtPath(target).OfType<AnimationClip>().First(c => c.name == "Death");
            if (clip.length <= 0 || AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new Exception("Invalid Death clip: " + name);
            report.AppendLine(name + ": refreshed Death, " + clip.length + "s");
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("art/enemy-concepts-1114/unity-death-refresh.txt", report.ToString());
        return report.ToString();
    }

    public static string Main(string creature)
    {
        string root = "Assets/Resources/OrganicEnemies1114/" + creature;
        Directory.CreateDirectory(root);
        var names = creature=="BloomMender" ? new[] { "Idle", "Move", "Attack", "Hit", "Death", "Heal" } : new[] { "Idle", "Move", "Attack", "Hit", "Death" };
        var clips = new AnimationClip[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = root + "/" + names[i] + ".fbx";
            File.Copy("art/enemy-concepts-1114/" + creature + "/" + names[i] + ".fbx", path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.isReadable = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            var settings = importer.defaultClipAnimations;
            if (settings.Length != 1) throw new Exception(names[i] + " expected one clip, got " + settings.Length);
            settings[0].name = names[i];
            settings[0].loopTime = i < 2;
            settings[0].loopPose = i < 2;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
            if (clips[i].length <= 0 || AnimationUtility.GetCurveBindings(clips[i]).Length == 0) throw new Exception("Empty clip " + names[i]);
        }
        string textureDir = root + "/Textures";
        Directory.CreateDirectory(textureDir);
        foreach(var source in Directory.GetFiles("art/enemy-concepts-1114/" + creature + "/Textures", "*.png"))
            File.Copy(source,textureDir+"/"+Path.GetFileName(source),true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var materialPath = root + "/"+creature+".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
        var textures = AssetDatabase.FindAssets("t:Texture2D", new[]{textureDir}).Select(g=>AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var baseMap = textures.FirstOrDefault(t=>t.name.ToLowerInvariant().Contains("base") || t.name.ToLowerInvariant().Contains("color"));
        if (!baseMap) throw new Exception("Missing base-color texture");
        material.SetTexture("_BaseMap",baseMap);
        var normal=textures.FirstOrDefault(t=>t.name=="Normal");
        if(normal)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
            ti.textureType=TextureImporterType.NormalMap; ti.SaveAndReimport();
            material.SetTexture("_BumpMap",normal); material.EnableKeyword("_NORMALMAP");
        }
        material.SetFloat("_Smoothness",.25f);
        foreach(var name in names)
        {
            var path=root+"/"+name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            foreach(var mat in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(mat),material);
            importer.SaveAndReimport();
        }
        string controllerPath=root+"/"+creature+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(!controller)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Action",AnimatorControllerParameterType.Int);
            var sm=controller.layers[0].stateMachine;
            for(int i=0;i<names.Length;i++)
            {
                var state=sm.AddState(names[i]); state.motion=clips[i];
                if(i==0) sm.defaultState=state;
                var transition=sm.AddAnyStateTransition(state);
                transition.hasExitTime=false; transition.duration=.12f; transition.canTransitionToSelf=false;
                transition.AddCondition(AnimatorConditionMode.Equals,i,"Action");
            }
        }
        var report=new StringBuilder();
        foreach(var clip in clips) report.AppendLine(clip.name+": "+clip.length+"s, "+AnimationUtility.GetCurveBindings(clip).Length+" curves");
        report.AppendLine("Material textures: "+string.Join(",",textures.Select(t=>t.name)));
        report.AppendLine("Controller Action: 0 Idle / 1 Move / 2 Attack / 3 Hit / 4 Death"+(creature=="BloomMender"?" / 5 Heal":"")+". Gameplay spawning not connected.");
        AssetDatabase.SaveAssets();
        File.WriteAllText("art/enemy-concepts-1114/"+creature+"/unity-import.txt",report.ToString());
        return report.ToString();
    }
}

