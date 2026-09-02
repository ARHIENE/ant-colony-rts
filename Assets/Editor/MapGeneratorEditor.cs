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
}
