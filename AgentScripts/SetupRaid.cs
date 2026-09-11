namespace AntColony.Setup
{
    using System;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.AI;
    using AntColony.Buildings;
    using AntColony.Data;
    using AntColony.World;

    public static class SetupRaid
    {
        public static string Main()
        {
            if (Application.isPlaying) throw new Exception("Run in edit mode.");
            if (GameObject.Find("EnemyNestPrototype") != null) return "Nest already exists.";
            var positions = new Vector3[5];
            var offsets = new[] { Vector3.zero, new Vector3(3,0,0), new Vector3(0,0,3), new Vector3(2,0,3), new Vector3(4,0,3) };
            for (var i = 0; i < positions.Length; i++)
            {
                if (!NavMesh.SamplePosition(new Vector3(225, 10, 215) + offsets[i], out var hit, 8, NavMesh.AllAreas))
                    throw new Exception("No walkable nest position.");
                positions[i] = hit.position;
            }
            var root = new GameObject("EnemyNestPrototype");
            root.SetActive(false);
            var colony = root.AddComponent<EnemyColony>();
            var serializedColony = new SerializedObject(colony);
            var buildings = serializedColony.FindProperty("buildings");
            buildings.arraySize = 2;
            for (var i = 0; i < 2; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Enemy Nest Building " + (i + 1);
                go.transform.SetParent(root.transform);
                go.transform.position = positions[i] + Vector3.up * .4f;
                go.transform.localScale = new Vector3(1.2f, .8f, 1.2f);
                var building = go.AddComponent<BuildingBase>();
                var serialized = new SerializedObject(building);
                serialized.FindProperty("countsTowardPlayerDefeat").boolValue = false;
                serialized.FindProperty("fallbackMaxHealth").floatValue = 60;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                buildings.GetArrayElementAtIndex(i).objectReferenceValue = building;
            }
            serializedColony.ApplyModifiedPropertiesWithoutUndo();
            for (var i = 0; i < 3; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Nest Stock " + (ResourceType)i;
                go.transform.SetParent(root.transform);
                go.transform.position = positions[i + 2] + Vector3.up * .25f;
                go.transform.localScale = Vector3.one * .5f;
                var node = go.AddComponent<ResourceNode>();
                var serialized = new SerializedObject(node);
                serialized.FindProperty("ownerColony").objectReferenceValue = colony;
                serialized.FindProperty("resourceType").enumValueIndex = i;
                serialized.FindProperty("amountRemaining").floatValue = i == 2 ? 20 : 40;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            root.SetActive(true);
            Undo.RegisterCreatedObjectUndo(root, "Create raid prototype");
            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            return "Created two-building raid prototype with Food 40, Soil 40, Special 20 at " + positions[0];
        }
    }
}
