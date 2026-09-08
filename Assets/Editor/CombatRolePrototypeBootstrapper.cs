using AntColony.Buildings;
using AntColony.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CombatRolePrototypeBootstrapper
{
    [MenuItem("Tools/Ant Colony/Setup Ranged Role Prototype")]
    public static void SetupRanged()
    {
        SetupRole(
            UnitRole.Ranged,
            foodCost: 25,
            buildTime: 8f,
            maxHealth: 25f,
            moveSpeed: 3.2f,
            armor: 0f,
            attackDamage: 3f,
            attackRange: 4f,
            attackInterval: 1.2f,
            foodUpkeep: 1,
            unitScale: 0.9f,
            xOffset: 0f);
    }

    [MenuItem("Tools/Ant Colony/Setup Defense Role Prototype")]
    public static void SetupDefense()
    {
        SetupRole(
            UnitRole.Defense,
            foodCost: 30,
            buildTime: 10f,
            maxHealth: 60f,
            moveSpeed: 2.3f,
            armor: 2f,
            attackDamage: 4f,
            attackRange: 1.4f,
            attackInterval: 1.4f,
            foodUpkeep: 2,
            unitScale: 1.15f,
            xOffset: 9f);
    }

    [MenuItem("Tools/Ant Colony/Setup Flying Role Prototype")]
    public static void SetupFlying()
    {
        SetupRole(
            UnitRole.Flying,
            foodCost: 30,
            buildTime: 9f,
            maxHealth: 35f,
            moveSpeed: 3.5f,
            armor: 1f,
            attackDamage: 4f,
            attackRange: 1.6f,
            attackInterval: 1.2f,
            foodUpkeep: 2,
            unitScale: 1f,
            xOffset: 18f);
    }

    [MenuItem("Tools/Ant Colony/Setup Support Role Prototype")]
    public static void SetupSupport()
    {
        SetupRole(
            UnitRole.Support,
            foodCost: 25,
            buildTime: 8f,
            maxHealth: 30f,
            moveSpeed: 3.2f,
            armor: 0f,
            attackDamage: 2f,
            attackRange: 2.5f,
            attackInterval: 1.4f,
            foodUpkeep: 1,
            unitScale: 0.9f,
            xOffset: 27f);
    }

    private static void SetupRole(
        UnitRole role,
        int foodCost,
        float buildTime,
        float maxHealth,
        float moveSpeed,
        float armor,
        float attackDamage,
        float attackRange,
        float attackInterval,
        int foodUpkeep,
        float unitScale,
        float xOffset)
    {
        var unitData = GetOrCreateUnitData(
            role,
            foodCost,
            buildTime,
            maxHealth,
            moveSpeed,
            armor,
            attackDamage,
            attackRange,
            attackInterval,
            foodUpkeep);

        var meleeBarracks = FindSceneComponent<Barracks>("BarracksTemplate");
        if (meleeBarracks == null)
            throw new System.InvalidOperationException("Scene template not found: BarracksTemplate");

        var sourceUnit = new SerializedObject(meleeBarracks)
            .FindProperty("soldierAntPrefab")
            .objectReferenceValue as GameObject;
        if (sourceUnit == null)
            throw new System.InvalidOperationException("BarracksTemplate has no unit reference.");

        var unitTemplate = GetOrCreateTemplate(sourceUnit, $"{role}Ant");
        ConfigureUnitTemplate(unitTemplate, unitScale, xOffset);

        var barracksTemplate = GetOrCreateTemplate<Barracks>("BarracksTemplate", $"{role}BarracksTemplate");
        ConfigureBarracks(barracksTemplate, unitTemplate, unitData, role, xOffset);

        var labTemplate = GetOrCreateTemplate<ResearchLab>("MeleeResearchLabTemplate", $"{role}ResearchLabTemplate");
        ConfigureResearchLab(labTemplate, role, xOffset);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"[CombatRolePrototypeBootstrapper] {role} role prototype is ready.");
    }

    private static UnitData GetOrCreateUnitData(
        UnitRole role,
        int foodCost,
        float buildTime,
        float maxHealth,
        float moveSpeed,
        float armor,
        float attackDamage,
        float attackRange,
        float attackInterval,
        int foodUpkeep)
    {
        var path = $"Assets/Data/{role}AntData.asset";
        var data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<UnitData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.displayName = $"{role} Ant";
        data.role = role;
        data.foodCost = foodCost;
        data.buildTimeSeconds = buildTime;
        data.requiredBarracksTier = 1;
        data.maxHealth = maxHealth;
        data.moveSpeed = moveSpeed;
        data.armor = armor;
        data.attackDamage = attackDamage;
        data.attackRange = attackRange;
        data.attackInterval = attackInterval;
        data.foodUpkeep = foodUpkeep;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject GetOrCreateTemplate<T>(string sourceName, string targetName) where T : Component
    {
        var target = FindSceneObject(targetName);
        if (target != null) return target;

        var source = FindSceneComponent<T>(sourceName);
        if (source == null)
            throw new System.InvalidOperationException($"Scene template not found: {sourceName}");

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

    private static void ConfigureUnitTemplate(GameObject template, float unitScale, float xOffset)
    {
        template.transform.position = new Vector3(209f + xOffset, 10.1f, 200f);
        template.transform.localScale = Vector3.one * unitScale;
        template.SetActive(false);
        EditorUtility.SetDirty(template);
    }

    private static void ConfigureBarracks(
        GameObject template,
        GameObject unitTemplate,
        UnitData unitData,
        UnitRole role,
        float xOffset)
    {
        template.transform.position = new Vector3(212f + xOffset, 10.6f, 200f);
        var serialized = new SerializedObject(template.GetComponent<Barracks>());
        serialized.FindProperty("role").enumValueIndex = (int)role;
        serialized.FindProperty("soldierAntPrefab").objectReferenceValue = unitTemplate;
        serialized.FindProperty("soldierAntData").objectReferenceValue = unitData;
        serialized.FindProperty("data").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/BarracksData.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        template.SetActive(false);
    }

    private static void ConfigureResearchLab(GameObject template, UnitRole role, float xOffset)
    {
        template.transform.position = new Vector3(215f + xOffset, 10.6f, 200f);
        var serialized = new SerializedObject(template.GetComponent<ResearchLab>());
        serialized.FindProperty("role").enumValueIndex = (int)role;
        serialized.FindProperty("attackLevel").intValue = 0;
        serialized.FindProperty("armorLevel").intValue = 0;
        serialized.FindProperty("data").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/ResearchLabData.asset");
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
