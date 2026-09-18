namespace AntColony.EditorTools
{
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldMapBootstrapper
{
    [MenuItem("Tools/Ant Colony/Setup World Map")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
        var game = Object.FindFirstObjectByType<GameManager>().gameObject;
        var world = game.GetComponent<WorldMapManager>() ?? game.AddComponent<WorldMapManager>();
        var incursions = game.GetComponent<LocalIncursions>() ?? game.AddComponent<LocalIncursions>();
        var colony = Object.FindObjectsByType<EnemyColony>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        var boss = Object.FindObjectsByType<BossHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        var old = new SerializedObject(colony.GetComponent<ColonyInvasion>());
        var raider = old.FindProperty("raiderTemplate").objectReferenceValue;
        var commander = old.FindProperty("commanderTemplate").objectReferenceValue;
        if (raider == null || commander == null) throw new System.InvalidOperationException("Missing invasion templates.");
        colony.gameObject.SetActive(false);
        boss.gameObject.SetActive(false);
        var data = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/ScienceLabData.asset");
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, "Assets/Data/ScienceLabData.asset");
        }
        data.kind = BuildingKind.ScienceLab;
        data.displayName = "Science Lab";
        data.foodCost = 100;
        data.soilCost = 100;
        data.constructionAnts = 8;
        data.buildTimeSeconds = 10;
        EditorUtility.SetDirty(data);
        var labObject = Template("ScienceLabTemplate", new Vector3(3, 2, 3));
        var lab = labObject.GetComponent<ScienceLab>() ?? labObject.AddComponent<ScienceLab>();
        Set(lab, "data", data);
        var shipObject = Template("ExpeditionTransportTemplate", new Vector3(3, 1.2f, 4));
        var ship = shipObject.GetComponent<ExpeditionTransport>() ?? shipObject.AddComponent<ExpeditionTransport>();
        var shipData = new SerializedObject(ship);
        shipData.FindProperty("countsTowardPlayerDefeat").boolValue = false;
        shipData.ApplyModifiedPropertiesWithoutUndo();
        Set(world, "colonyTemplate", colony);
        Set(world, "bossTemplate", boss);
        Set(world, "commanderTemplate", commander);
        Set(world, "transportTemplate", ship);
        Set(incursions, "raiderTemplate", raider);
        Set(incursions, "commanderTemplate", commander);
        EditorSceneManager.MarkSceneDirty(game.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(game.scene);
    }

    private static GameObject Template(string name, Vector3 scale)
    {
        var go = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name)?.gameObject;
        if (go == null) { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; }
        go.SetActive(false);
        go.transform.position = new Vector3(255, 10, 200);
        go.transform.localScale = scale;
        return go;
    }
    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
}
