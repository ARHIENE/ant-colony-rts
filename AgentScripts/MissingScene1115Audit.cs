using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using UnityEditor.SceneManagement;
public static class MissingScene1115Audit
{
    public static string Fix()
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        const string path = "Assets/_Project/Scenes/AntColony.unity";
        var scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Expected scene must already be loaded");
        Directory.CreateDirectory("work/scene-orphans-1115");
        var backup = "work/scene-orphans-1115/AntColony-before.unity";
        if (!File.Exists(backup)) File.Copy(path, backup);
        var roots = scene.GetRootGameObjects();
        var targets = roots.Where(g => g.name == "TerrainGenerator" || g.name == "TerrainGenerator (1)").ToArray();
        if (targets.Length != 2 || targets.Any(g => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g) != 1))
            throw new Exception("Expected exactly the two audited missing references");
        var transforms = roots.Sum(g => g.GetComponentsInChildren<Transform>(true).Length);
        var valid = roots.Sum(g => g.GetComponentsInChildren<Component>(true).Count(c => c != null));
        foreach (var g in targets) { Undo.RegisterCompleteObjectUndo(g, "Remove orphaned terrain script"); GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g); }
        if (transforms != roots.Sum(g => g.GetComponentsInChildren<Transform>(true).Length)
            || valid != roots.Sum(g => g.GetComponentsInChildren<Component>(true).Count(c => c != null)))
            throw new Exception("Valid scene structure changed");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene write failed");
        var report = $"Removed exactly two missing script slots; preserved {transforms} transforms and {valid} valid components. Scene written; backup retained.\n" + Main();
        File.WriteAllText("work/scene-orphans-1115/checks.txt", report);
        return report;
    }
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    public static string Main() => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount)
        .Select(SceneManager.GetSceneAt).SelectMany(s => s.GetRootGameObjects())
        .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
        .Where(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
        .Select(t => $"{t.gameObject.scene.path}: {Path(t)} count={GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)} source={AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject))}"));
}
