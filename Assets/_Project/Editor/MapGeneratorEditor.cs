using UnityEditor;
using UnityEngine;
using AntColony.Map;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var mapGenerator = (MapGenerator)target;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            mapGenerator.GenerateTerrain();
            SnapToTerrainMenu.SnapAll();
        }

        if (GUILayout.Button("Generate"))
        {
            mapGenerator.GenerateTerrain();
            SnapToTerrainMenu.SnapAll();
        }
    }

    [MenuItem("Tools/Ant Colony/Regenerate Map")]
    private static void RegenerateFromMenu()
    {
        var mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogWarning("No MapGenerator found in the open scene.");
            return;
        }
        mapGenerator.GenerateTerrain();
        SnapToTerrainMenu.SnapAll();
    }

    [MenuItem("Tools/Ant Colony/Setup Real Decoration Prefabs (SIMUL-TeaamProject)")]
    private static void SetupRealDecorationPrefabs()
    {
        var mapGenerator = Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogWarning("No MapGenerator found in the open scene.");
            return;
        }

        var tree01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TeamImport/Prefabs/SimpleNaturePack/Prefabs/Tree_01.prefab");
        var tree02 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TeamImport/Prefabs/SimpleNaturePack/Prefabs/Tree_02.prefab");
        var rock05 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_TeamImport/Prefabs/SimpleNaturePack/Prefabs/Rock_05.prefab");
        if (tree01 == null || tree02 == null || rock05 == null)
        {
            Debug.LogError("Could not load one or more decoration prefabs from Assets/_TeamImport/Prefabs/SimpleNaturePack/Prefabs/.");
            return;
        }

        var so = new SerializedObject(mapGenerator);
        var spawnObjects = so.FindProperty("spawnObjects");
        spawnObjects.arraySize = 3;

        void SetEntry(int index, GameObject prefab, float minHeight, float maxHeight, float spawnChance, float minScale, float maxScale, float minDistanceBetween)
        {
            var entry = spawnObjects.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("minHeight").floatValue = minHeight;
            entry.FindPropertyRelative("maxHeight").floatValue = maxHeight;
            entry.FindPropertyRelative("spawnChance").floatValue = spawnChance;
            entry.FindPropertyRelative("minScale").floatValue = minScale;
            entry.FindPropertyRelative("maxScale").floatValue = maxScale;
            entry.FindPropertyRelative("minDistanceBetween").floatValue = minDistanceBetween;
        }

        SetEntry(0, tree01, 0.2f, 0.9f, 0.05f, 0.8f, 1.2f, 2f);
        SetEntry(1, tree02, 0.2f, 0.9f, 0.05f, 0.8f, 1.2f, 2f);
        SetEntry(2, rock05, 0f, 1f, 0.03f, 0.6f, 1.1f, 2f);

        so.ApplyModifiedProperties();

        mapGenerator.GenerateTerrain();
        SnapToTerrainMenu.SnapAll();
        EditorUtility.SetDirty(mapGenerator);
        Debug.Log("[MapGeneratorEditor] Real decoration prefabs (Tree_01, Tree_02, Rock_05) wired into spawnObjects.");
    }
}
