using AntColony.Buildings;
using AntColony.Data;
using AntColony.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 장수 획득 건물 3종(양육실·스카우트 파견소·수용소)의 BuildingData 자산과 씬 배치 템플릿을 만들고,
// 적 장수 템플릿을 만들어 씬의 모든 ColonyInvasion에 연결한다.
// 여러 번 실행해도 같은 결과가 되도록 이미 있는 자산·오브젝트는 다시 만들지 않고 값만 덮어쓴다.
public static class CommanderAcquisitionBootstrapper
{
    private const string EnemyCommanderTemplateName = "EnemyCommanderTemplate";

    [MenuItem("Tools/Ant Colony/Setup Commander Acquisition Buildings")]
    public static void Setup()
    {
        SetupBuilding<NurseryChamber>("Nursery", BuildingKind.Nursery,
            maxHealth: 200f, soilCost: 40, buildTime: 6f, constructionAnts: 5,
            position: new Vector3(245f, 10.6f, 200f));
        SetupBuilding<ScoutPost>("ScoutPost", BuildingKind.ScoutPost,
            maxHealth: 150f, soilCost: 30, buildTime: 5f, constructionAnts: 4,
            position: new Vector3(248f, 10.6f, 200f));
        SetupBuilding<PrisonerCamp>("PrisonerCamp", BuildingKind.PrisonerCamp,
            maxHealth: 250f, soilCost: 50, buildTime: 7f, constructionAnts: 6,
            position: new Vector3(251f, 10.6f, 200f));

        SetupEnemyCommander();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[CommanderAcquisitionBootstrapper] Nursery, scout post, prison and enemy commander are ready.");
    }

    private static void SetupBuilding<T>(string id, BuildingKind kind, float maxHealth, int soilCost,
        float buildTime, int constructionAnts, Vector3 position) where T : Component
    {
        var path = $"Assets/Data/{id}Data.asset";
        var data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.displayName = id;
        data.kind = kind;
        data.maxHealth = maxHealth;
        data.foodCost = 0;
        data.soilCost = soilCost;
        data.buildTimeSeconds = buildTime;
        data.constructionAnts = constructionAnts;
        EditorUtility.SetDirty(data);

        // 배치 컨트롤러는 이름이 Template으로 끝나는 씬 오브젝트를 템플릿으로 찾는다.
        var templateName = $"{id}Template";
        var template = FindSceneObject(templateName);
        if (template == null)
        {
            template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = templateName;
            Undo.RegisterCreatedObjectUndo(template, $"Create {templateName}");
        }

        template.transform.position = position;
        template.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);

        var building = template.GetComponent<BuildingBase>();
        if (building == null) building = template.AddComponent<BuildingBase>();
        var serialized = new SerializedObject(building);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (template.GetComponent<T>() == null) template.AddComponent<T>();

        // 템플릿은 반드시 비활성이어야 한다. 활성이면 수용소가 포로를 받고 양육실이 번식을 돌리기 시작한다.
        template.SetActive(false);
        EditorUtility.SetDirty(template);
    }

    // 적 장수 템플릿은 침공 부대 템플릿을 복제해 스크립트만 EnemyCommander로 바꾼다.
    // 컴포넌트를 지우고 다시 붙이면 체력·속도 등 인스펙터 값이 날아가므로 m_Script만 교체한다.
    private static void SetupEnemyCommander()
    {
        var invasions = Object.FindObjectsByType<ColonyInvasion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (invasions.Length == 0)
        {
            Debug.LogWarning("[CommanderAcquisitionBootstrapper] 씬에 ColonyInvasion이 없어 적 장수 연결을 건너뛴다.");
            return;
        }

        var commander = EnsureEnemyCommanderTemplate(invasions);
        if (commander == null) return;

        foreach (var invasion in invasions)
        {
            var serialized = new SerializedObject(invasion);
            serialized.FindProperty("commanderTemplate").objectReferenceValue = commander;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(invasion);
        }
    }

    private static EnemyCommander EnsureEnemyCommanderTemplate(ColonyInvasion[] invasions)
    {
        var existing = FindSceneObject(EnemyCommanderTemplateName);
        if (existing != null)
        {
            existing.SetActive(false);
            var found = existing.GetComponent<EnemyCommander>();
            if (found != null) return found;
            return ReplaceWithEnemyCommander(existing);
        }

        WildMonster raider = null;
        foreach (var invasion in invasions)
        {
            raider = new SerializedObject(invasion).FindProperty("raiderTemplate").objectReferenceValue as WildMonster;
            if (raider != null) break;
        }
        if (raider == null)
        {
            Debug.LogWarning("[CommanderAcquisitionBootstrapper] raiderTemplate이 비어 있어 적 장수 템플릿을 만들 수 없다.");
            return null;
        }

        var template = Object.Instantiate(raider.gameObject);
        template.name = EnemyCommanderTemplateName;
        template.transform.position = new Vector3(254f, 10.6f, 200f);
        template.SetActive(false);
        Undo.RegisterCreatedObjectUndo(template, $"Create {EnemyCommanderTemplateName}");
        return ReplaceWithEnemyCommander(template);
    }

    private static EnemyCommander ReplaceWithEnemyCommander(GameObject template)
    {
        var monster = template.GetComponent<WildMonster>();
        if (monster == null)
        {
            Debug.LogWarning($"[CommanderAcquisitionBootstrapper] {template.name}에 WildMonster가 없어 교체할 수 없다.");
            return null;
        }

        var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/World/EnemyCommander.cs");
        if (script == null)
        {
            Debug.LogWarning("[CommanderAcquisitionBootstrapper] EnemyCommander.cs를 찾지 못했다.");
            return null;
        }

        var serialized = new SerializedObject(monster);
        serialized.FindProperty("m_Script").objectReferenceValue = script;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(template);

        // m_Script 교체가 같은 틱에 반영되지 않으면 GetComponent가 null을 돌려준다.
        // 그 상태로 ColonyInvasion에 null을 꽂으면 조용히 적 장수가 사라지므로, 연결하지 않고 알린다.
        var commander = template.GetComponent<EnemyCommander>();
        if (commander == null)
            Debug.LogError("[CommanderAcquisitionBootstrapper] EnemyCommander 스크립트 교체가 아직 반영되지 않았다. "
                + "메뉴를 한 번 더 실행하면 연결된다.");
        return commander;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            if (transform.gameObject.scene.IsValid() && transform.gameObject.name == objectName)
                return transform.gameObject;
        return null;
    }
}
