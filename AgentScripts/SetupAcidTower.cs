using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class SetupAcidTower
{
    public static string Main()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/AntColony.unity") throw new InvalidOperationException("Open AntColony scene first.");
        if (Object.FindObjectsByType<AcidTower>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(t => t.gameObject.scene == scene && t.name == "AcidTowerTemplate")) return "AcidTowerTemplate already exists.";

        var data = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/AcidTowerData.asset");
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            data.kind = BuildingKind.AcidTower; data.displayName = "Acid Tower";
            data.maxHealth = 250; data.foodCost = 30; data.soilCost = 60;
            data.constructionAnts = 5; data.buildTimeSeconds = 8;
            AssetDatabase.CreateAsset(data, "Assets/Data/AcidTowerData.asset");
        }
        Material Material(string path, string shader, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var found = Shader.Find(shader);
            if (found == null) throw new InvalidOperationException("Missing shader: " + shader);
            material = new Material(found); material.color = color;
            AssetDatabase.CreateAsset(material, path); return material;
        }
        var shell = Material("Assets/Materials/AcidTowerShell.mat", "Universal Render Pipeline/Lit", new Color(.18f, .27f, .19f));
        var acid = Material("Assets/Materials/AcidSpray.mat", "Universal Render Pipeline/Unlit", new Color(.55f, 1f, .12f));
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube); root.name = "AcidTowerTemplate"; root.SetActive(false);
        root.transform.position = new Vector3(255, 10, 200); root.transform.localScale = new Vector3(2.5f, 2, 2.5f);
        root.GetComponent<Renderer>().sharedMaterial = shell;
        var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder); nozzle.name = "AcidReservoir";
        nozzle.transform.SetParent(root.transform, false); nozzle.transform.localPosition = new Vector3(0, .7f, 0);
        nozzle.transform.localScale = new Vector3(.45f, .35f, .45f);
        nozzle.GetComponent<Renderer>().sharedMaterial = acid;
        Object.DestroyImmediate(nozzle.GetComponent<Collider>());
        var obstacle = root.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = Vector3.one; obstacle.carving = true;
        var sprayObject = new GameObject("AcidSpray"); sprayObject.transform.SetParent(root.transform, false);
        var spray = sprayObject.AddComponent<LineRenderer>(); spray.useWorldSpace = true;
        spray.positionCount = 2; spray.startWidth = .14f; spray.endWidth = .06f;
        spray.sharedMaterial = acid; spray.enabled = false;
        var tower = root.AddComponent<AcidTower>(); var serialized = new SerializedObject(tower);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.FindProperty("spray").objectReferenceValue = spray;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        return "Acid tower template installed: 30F / 60S / 5 ants, 8s construction, 250 HP, range 14, damage 18 every 1.5s.";
    }
}
