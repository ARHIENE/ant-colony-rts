using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AnimationUnits1028Checks
{
    public static string FixAll()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before import");
        var folder = "Temp/performance1028/animation-units"; Directory.CreateDirectory(folder);
        var paths = Directory.GetFiles("Assets/_Project/Art/Kemono/Anim", "*.fbx");
        var changed = 0;
        foreach (var path in paths)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            if (importer.globalScale == 100f) continue;
            var backup = Path.Combine(folder, Path.GetFileName(path) + ".meta");
            if (!File.Exists(backup)) File.Copy(path + ".meta", backup);
            importer.globalScale = 100f; importer.SaveAndReimport(); changed++;
            File.WriteAllText("Temp/animation-units1028-progress.txt", changed + "/" + paths.Length + " " + path);
        }
        Audit();
        var controller = Resources.Load<RuntimeAnimatorController>("Kemono/KemonoAnimator");
        var count = 0;
        foreach (var clip in controller.animationClips.Distinct())
        {
            var head = AnimationUtility.GetCurveBindings(clip).First(b => b.path.EndsWith("head") && b.propertyName == "m_LocalPosition.y");
            var value = AnimationUtility.GetEditorCurve(clip, head).keys[0].value;
            if (Mathf.Abs(value - .0399691f) > .005f) throw new Exception("Animation units mismatch " + clip.name + " " + value);
            count++;
        }
        var result = "PASS " + count + " character clips use avatar-sized translations; " + changed + " animation FBX imports repaired";
        File.WriteAllText("Temp/animation-units1028-result.txt", result); return result;
    }
    public static string Audit()
    {
        var controller = Resources.Load<RuntimeAnimatorController>("Kemono/KemonoAnimator");
        var report = controller.animationClips.GroupBy(AssetDatabase.GetAssetPath).Select(g =>
        {
            var clip = g.First(); var importer = (ModelImporter)AssetImporter.GetAtPath(g.Key);
            var head = AnimationUtility.GetCurveBindings(clip).FirstOrDefault(b => b.path.EndsWith("head") && b.propertyName == "m_LocalPosition.y");
            var y = head.path != null ? AnimationUtility.GetEditorCurve(clip, head).keys[0].value : -1;
            return Path.GetFileName(g.Key) + " scale=" + importer.globalScale + " headY=" + y;
        }).ToArray();
        File.WriteAllLines("Temp/animation-units1028-audit.txt", report);
        return string.Join("\n", report);
    }
    public static string Probe()
    {
        if (Application.isPlaying) throw new Exception("Stop Play before import");
        const string path = "Assets/_Project/Art/Kemono/Anim/Kemono@Idle.fbx";
        Directory.CreateDirectory("Temp/performance1028/animation-units");
        File.Copy(path + ".meta", "Temp/performance1028/animation-units/Idle-before.meta", false);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        var before = importer.globalScale;
        importer.globalScale = 100;
        importer.SaveAndReimport();
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        var values = AnimationUtility.GetCurveBindings(clip).Where(b => b.path.EndsWith("pevis") && b.propertyName == "m_LocalPosition.y")
            .Select(b => AnimationUtility.GetEditorCurve(clip, b).keys[0].value).ToArray();
        return "Idle import globalScale " + before + " -> " + importer.globalScale + "; pelvis Y=" + string.Join(",", values);
    }
}
