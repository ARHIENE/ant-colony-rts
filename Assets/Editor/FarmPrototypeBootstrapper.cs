using AntColony.Buildings;
using AntColony.Data;
using AntColony.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 밭(농사) 최소 프로토타입용 씬 템플릿과 BuildingData 자산을 만든다.
// 수치는 전부 임시값이며 인스펙터에서 조정 가능하다.
public static class FarmPrototypeBootstrapper
{
    private const string TemplateName = "FarmTemplate";
    private const string DataPath = "Assets/Data/FarmData.asset";

    [MenuItem("Tools/Ant Colony/Setup Farm Prototype")]
    public static void SetupFarm()
    {
        var data = AssetDatabase.LoadAssetAtPath<BuildingData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }

        data.displayName = "Farm";
        data.kind = BuildingKind.Farm;
        data.maxHealth = 150f;
        data.foodCost = 0;
        data.soilCost = 30;
        data.buildTimeSeconds = 4f;
        EditorUtility.SetDirty(data);

        var template = FindSceneObject(TemplateName);
        if (template == null)
        {
            template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = TemplateName;
            Undo.RegisterCreatedObjectUndo(template, "Create FarmTemplate");
        }

        template.transform.position = new Vector3(206f, 10.2f, 200f);
        template.transform.localScale = new Vector3(3f, 0.4f, 3f);

        var building = template.GetComponent<BuildingBase>();
        if (building == null) building = template.AddComponent<BuildingBase>();
        var buildingSerialized = new SerializedObject(building);
        buildingSerialized.FindProperty("data").objectReferenceValue = data;
        buildingSerialized.ApplyModifiedPropertiesWithoutUndo();

        var node = template.GetComponent<ResourceNode>();
        if (node == null) node = template.AddComponent<ResourceNode>();
        var nodeSerialized = new SerializedObject(node);
        nodeSerialized.FindProperty("resourceType").enumValueIndex = (int)ResourceType.Food;
        nodeSerialized.FindProperty("amountRemaining").floatValue = 0f;   // 성장 후 첫 수확
        nodeSerialized.FindProperty("regrowSeconds").floatValue = 20f;
        nodeSerialized.FindProperty("regrowAmount").floatValue = 100f;
        nodeSerialized.ApplyModifiedPropertiesWithoutUndo();

        template.SetActive(false);
        EditorUtility.SetDirty(template);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[FarmPrototypeBootstrapper] Farm prototype is ready.");
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            if (transform.gameObject.scene.IsValid() && transform.gameObject.name == objectName)
                return transform.gameObject;
        return null;
    }
}
