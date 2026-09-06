using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RangedRoleBootstrapper
{
    private const string UnitDataPath = "Assets/Data/RangedAntData.asset";

    [MenuItem("Tools/Ant Colony/Setup Ranged Role Prototype")]
    public static void Setup()
    {
        var unitData = GetOrCreateUnitData();
        var meleeBarracks = FindSceneComponent<Barracks>("BarracksTemplate");
        if (meleeBarracks == null) throw new System.InvalidOperationException("Scene template not found: BarracksTemplate");
        var meleeBarracksData = new SerializedObject(meleeBarracks);
        var sourceUnit = meleeBarracksData.FindProperty("soldierAntPrefab").objectReferenceValue as GameObject;
        if (sourceUnit == null) throw new System.InvalidOperationException("BarracksTemplate has no unit prefab reference.");

        var unitTemplate = GetOrCreateTemplate(sourceUnit, "RangedAnt");
        ConfigureUnitTemplate(unitTemplate);

        var barracksTemplate = GetOrCreateTemplate<Barracks>("BarracksTemplate", "RangedBarracksTemplate");
        ConfigureBarracks(barracksTemplate, unitTemplate, unitData);

        var labTemplate = GetOrCreateTemplate<ResearchLab>("MeleeResearchLabTemplate", "RangedResearchLabTemplate");
        ConfigureResearchLab(labTemplate);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[RangedRoleBootstrapper] Ranged role prototype is ready.");
    }

    private static UnitData GetOrCreateUnitData()
    {
        var data = AssetDatabase.LoadAssetAtPath<UnitData>(UnitDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<UnitData>();
            AssetDatabase.CreateAsset(data, UnitDataPath);
        }

        data.displayName = "Ranged Ant";
        data.role = UnitRole.Ranged;
        data.foodCost = 25;
        data.buildTimeSeconds = 8f;
        data.requiredBarracksTier = 1;
        data.maxHealth = 25f;
        data.moveSpeed = 3.2f;
        data.armor = 0f;
        data.attackDamage = 3f;
        data.attackRange = 4f;
        data.attackInterval = 1.2f;
        data.foodUpkeep = 1;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject GetOrCreateTemplate<T>(string sourceName, string targetName) where T : Component
    {
        var target = FindSceneObject(targetName);
        if (target != null) return target;

        var source = FindSceneComponent<T>(sourceName);
        if (source == null) throw new System.InvalidOperationException($"Scene template not found: {sourceName}");

        target = Object.Instantiate(source.gameObject);
        target.name = targetName;
        target.SetActive(false);
        Undo.RegisterCreatedObjectUndo(target, $"Create {targetName}");
        return target;
    }

    private static GameObject GetOrCreateTemplate(GameObject source, string targetName)
    {
        var target = FindSceneObject(targetName);
        if (target != null) return target;

        target = Object.Instantiate(source);
        target.name = targetName;
        target.SetActive(false);
        Undo.RegisterCreatedObjectUndo(target, $"Create {targetName}");
        return target;
    }

    private static void ConfigureUnitTemplate(GameObject template)
    {
        template.transform.position = new Vector3(209f, 10.1f, 200f);
        template.transform.localScale = Vector3.one * 0.9f;
        template.SetActive(false);
        EditorUtility.SetDirty(template);
    }

    private static void ConfigureBarracks(GameObject template, GameObject unitTemplate, UnitData unitData)
    {
        template.transform.position = new Vector3(212f, 10.6f, 200f);
        var serialized = new SerializedObject(template.GetComponent<Barracks>());
        serialized.FindProperty("role").enumValueIndex = (int)UnitRole.Ranged;
        serialized.FindProperty("soldierAntPrefab").objectReferenceValue = unitTemplate;
        serialized.FindProperty("soldierAntData").objectReferenceValue = unitData;
        serialized.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/BarracksData.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        template.SetActive(false);
    }

    private static void ConfigureResearchLab(GameObject template)
    {
        template.transform.position = new Vector3(215f, 10.6f, 200f);
        var serialized = new SerializedObject(template.GetComponent<ResearchLab>());
        serialized.FindProperty("role").enumValueIndex = (int)UnitRole.Ranged;
        serialized.FindProperty("attackLevel").intValue = 0;
        serialized.FindProperty("armorLevel").intValue = 0;
        serialized.FindProperty("data").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/ResearchLabData.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        template.SetActive(false);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            if (transform.gameObject.scene.IsValid() && transform.gameObject.name == objectName)
                return transform.gameObject;
        return null;
    }

    private static T FindSceneComponent<T>(string objectName) where T : Component
    {
        foreach (var component in Resources.FindObjectsOfTypeAll<T>())
            if (component.gameObject.scene.IsValid() && component.gameObject.name == objectName)
                return component;
        return null;
    }
}
