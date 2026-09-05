using System.IO;
using AntColony.Data;
using UnityEditor;
using UnityEngine;

// 세션 초기 셋업용 유틸리티. Assets/Data에 기본값이 채워진 UnitData/BuildingData 에셋을 만들어준다.
// 이미 존재하면 건드리지 않음(멱등) — 값은 인스펙터에서 자유롭게 바꿔도 이 메뉴를 다시 눌러도 안전.
public static class DataAssetBootstrapper
{
    private const string DataFolder = "Assets/Data";

    [MenuItem("Tools/Ant Colony/Create Default Data Assets")]
    public static void CreateDefaultDataAssets()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        CreateWorkerAntData();
        CreateSoldierAntData();
        CreateBuildingData("QueenChamberData", BuildingKind.QueenChamber, foodCost: 0, soilCost: 0, buildTime: 0f);
        CreateBuildingData("BarracksData", BuildingKind.Barracks, foodCost: 0, soilCost: 30, buildTime: 3f);
        CreateBuildingData("StorageData", BuildingKind.Storage, foodCost: 0, soilCost: 20, buildTime: 3f, foodCapBonus: 100, soilCapBonus: 100);
        CreateBuildingData("DigSiteData", BuildingKind.DigSite, foodCost: 0, soilCost: 40, buildTime: 0f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataAssetBootstrapper] Default data assets ready under " + DataFolder);
    }

    private static void CreateWorkerAntData()
    {
        var path = $"{DataFolder}/WorkerAntData.asset";
        if (AssetDatabase.LoadAssetAtPath<UnitData>(path) != null) return;

        var data = ScriptableObject.CreateInstance<UnitData>();
        data.displayName = "Worker Ant";
        data.role = UnitRole.Worker;
        data.foodCost = 10;
        data.buildTimeSeconds = 5f;
        data.maxHealth = 20f;
        data.moveSpeed = 3.5f;
        data.gatherRate = 5f;
        data.carryCapacity = 10;

        AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateSoldierAntData()
    {
        var path = $"{DataFolder}/SoldierAntData.asset";
        if (AssetDatabase.LoadAssetAtPath<UnitData>(path) != null) return;

        var data = ScriptableObject.CreateInstance<UnitData>();
        data.displayName = "Soldier Ant";
        data.role = UnitRole.Melee;
        data.foodCost = 20;
        data.buildTimeSeconds = 8f;
        data.requiredBarracksTier = 1;
        data.maxHealth = 40f;
        data.moveSpeed = 3f;
        data.attackDamage = 5f;
        data.attackRange = 1.5f;
        data.attackInterval = 1f;

        AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateBuildingData(
        string fileName,
        BuildingKind kind,
        int foodCost,
        int soilCost,
        float buildTime,
        int foodCapBonus = 0,
        int soilCapBonus = 0)
    {
        var path = $"{DataFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<BuildingData>(path) != null) return;

        var data = ScriptableObject.CreateInstance<BuildingData>();
        data.displayName = fileName.Replace("Data", "");
        data.kind = kind;
        data.foodCost = foodCost;
        data.soilCost = soilCost;
        data.buildTimeSeconds = buildTime;
        data.foodCapacityBonus = foodCapBonus;
        data.soilCapacityBonus = soilCapBonus;

        AssetDatabase.CreateAsset(data, path);
    }
}
