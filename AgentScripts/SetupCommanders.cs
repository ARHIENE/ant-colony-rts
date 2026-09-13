using System;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupCommanders
{
    public static string Main()
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        var queen = UnityEngine.Object.FindFirstObjectByType<QueenChamber>();
        if (queen == null) throw new Exception("Queen chamber missing");
        var pool = UnityEngine.Object.FindFirstObjectByType<AntPool>();
        if (pool == null) pool = new GameObject("AntPool").AddComponent<AntPool>();
        var roster = UnityEngine.Object.FindFirstObjectByType<CommanderRoster>();
        if (roster == null) roster = new GameObject("CommanderRoster").AddComponent<CommanderRoster>();
        var settings = new SerializedObject(roster);
        settings.FindProperty("spawnOrigin").objectReferenceValue = queen.transform;
        var names = new[] { "Worker", "Soldier", "Ranged", "Defense", "Flying", "Support" };
        var profiles = settings.FindProperty("roleProfiles");
        profiles.arraySize = names.Length;
        for (var i = 0; i < names.Length; i++)
        {
            var data = AssetDatabase.LoadAssetAtPath<UnitData>($"Assets/Data/{names[i]}AntData.asset");
            if (data == null) throw new Exception("Missing " + names[i]);
            profiles.GetArrayElementAtIndex(i).objectReferenceValue = data;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(queen.gameObject.scene);
        EditorSceneManager.SaveScene(queen.gameObject.scene);
        return "Configured pool and 12 commanders; 2 troops each, per-commander Worker + one combat role.";
    }
}
