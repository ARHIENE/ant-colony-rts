namespace AntColony.Setup
{
    using System;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.AI;
    using AntColony.World;
    public static class SetupInvasion
    {
        public static string Main()
        {
            if (Application.isPlaying) throw new Exception("Edit mode only.");
            var nest = GameObject.Find("EnemyNestPrototype");
            if (nest == null) throw new Exception("Missing raid nest.");
            if (nest.GetComponent<ColonyInvasion>() != null) return "Already configured.";
            if (!NavMesh.SamplePosition(new Vector3(225, 7, 218), out var hit, 5, NavMesh.AllAreas)) throw new Exception("No spawn NavMesh.");
            var spawn = new GameObject("InvasionSpawn");
            spawn.transform.SetParent(nest.transform);
            spawn.transform.position = hit.position;
            var template = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            template.name = "RaiderTemplate";
            template.SetActive(false);
            template.transform.SetParent(nest.transform);
            template.transform.position = hit.position;
            template.transform.localScale = new Vector3(.6f, .6f, .6f);
            var monster = template.AddComponent<WildMonster>();
            var settings = new SerializedObject(monster);
            settings.FindProperty("maxHealth").floatValue = 30;
            settings.FindProperty("attackDamage").floatValue = 4;
            settings.FindProperty("moveSpeed").floatValue = 3;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var invasion = nest.AddComponent<ColonyInvasion>();
            settings = new SerializedObject(invasion);
            settings.FindProperty("raiderTemplate").objectReferenceValue = monster;
            settings.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(spawn, "Configure invasion");
            Undo.RegisterCreatedObjectUndo(template, "Configure invasion");
            EditorSceneManager.MarkSceneDirty(nest.scene);
            EditorSceneManager.SaveScene(nest.scene);
            return "Invasion configured: first 90s, every 120s, 2 to 6 raiders, cap 12, HP30 damage4 speed3.";
        }
    }
}
