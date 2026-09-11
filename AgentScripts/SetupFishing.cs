namespace AntColony.Setup
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.AI;
    using AntColony.World;

    public static class SetupFishing
    {
        public static string Main()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Run in edit mode.");
            if (GameObject.Find("FishingSpot") != null) return "FishingSpot already exists.";
            if (!NavMesh.SamplePosition(new Vector3(190, 10, 210), out var bank, 5, NavMesh.AllAreas))
                throw new System.InvalidOperationException("No walkable bank near the colony.");
            var pondPosition = bank.position + new Vector3(-4, -.1f, 0);
            var pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pond.name = "FishingPond";
            pond.transform.position = pondPosition;
            pond.transform.localScale = new Vector3(6, .08f, 6);
            Object.DestroyImmediate(pond.GetComponent<Collider>());
            var obstacle = pond.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(1, 10, 1);
            obstacle.carving = true;
            var spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spot.name = "FishingSpot";
            spot.transform.position = bank.position + Vector3.up * .15f;
            spot.transform.localScale = new Vector3(1.2f, .3f, 1.2f);
            var node = spot.AddComponent<ResourceNode>();
            var serialized = new SerializedObject(node);
            serialized.FindProperty("requiresFishing").boolValue = true;
            serialized.FindProperty("amountRemaining").floatValue = 100;
            serialized.FindProperty("regrowSeconds").floatValue = 15;
            serialized.FindProperty("regrowAmount").floatValue = 100;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetMaterial(pond, "FishingWater", new Color(.12f, .42f, .7f));
            SetMaterial(spot, "FishingBank", new Color(.2f, .7f, .75f));
            Undo.RegisterCreatedObjectUndo(pond, "Create fishing pond");
            Undo.RegisterCreatedObjectUndo(spot, "Create fishing spot");
            EditorSceneManager.MarkSceneDirty(spot.scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(spot.scene);
            return "Created pond and walkable fishing bank at " + bank.position;
        }

        private static void SetMaterial(GameObject go, string name, Color color)
        {
            var path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
            }
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
