using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AntColony.EditorTools
{
    public static class StorageBootstrapper
    {
        [MenuItem("Tools/Ant Colony/Setup Storage Construction")]
        public static void Setup()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
            var template = Object.FindObjectsByType<Storage>(FindObjectsInactive.Include)
                .FirstOrDefault(s => s.gameObject.scene.IsValid() && s.name == "StorageTemplate");
            if (template != null) return;
            var source = Object.FindObjectsByType<Storage>()
                .First(s => s.gameObject.scene.IsValid() && s.CountsTowardPlayerDefeat);
            var data = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Data/StorageData.asset");
            if (data == null) throw new InvalidOperationException("StorageData is missing.");
            var go = Object.Instantiate(source.gameObject);
            go.name = "StorageTemplate";
            go.SetActive(false);
            go.transform.SetParent(null);
            var serialized = new SerializedObject(go.GetComponent<Storage>());
            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(go.scene);
            EditorSceneManager.SaveScene(go.scene);
        }
    }
}
