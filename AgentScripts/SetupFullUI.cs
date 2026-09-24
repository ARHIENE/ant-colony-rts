using System.Linq;
using AntColony.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupFullUI
{
    public static string Main()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/AntColony.unity")
            throw new System.InvalidOperationException("Open AntColony first.");
        var imported = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(x => x != null && x.GetType().Name == "TerrainGenerator");
        if (imported != null)
        {
            var ported = imported.GetComponent<MapGenerator>();
            if (ported == null)
            {
                ported = imported.gameObject.AddComponent<MapGenerator>();
                var source = new SerializedObject(imported);
                var target = new SerializedObject(ported);
                foreach (var name in new[] { "xSize", "zSize", "xOffset", "zOffset", "noiseScale", "heightMultiplier",
                    "octavesCount", "lacunarity", "persistance", "terrainLayers", "mat", "generateWater", "waterMat",
                    "waterHeight", "spawnObjects" })
                {
                    var property = source.FindProperty(name);
                    if (property != null && target.FindProperty(name) != null) target.CopyFromSerializedProperty(property);
                }
                target.ApplyModifiedPropertiesWithoutUndo();
            }
            imported.enabled = false;
            EditorUtility.SetDirty(imported);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "AntColony startup scene and tracked MapGenerator configured.";
    }
}
