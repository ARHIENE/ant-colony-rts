using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupQuirkyAnt
{
    public static string Main()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before installing art.");
        const string source = "Assets/_TeamImport/Prefabs/Quirky Series/Insect Bundle/Insect Vol.1";
        const string art = "Assets/Art/QuirkyAnt";
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(art)) AssetDatabase.CreateFolder("Assets/Art", "QuirkyAnt");
        foreach (var folder in new[] { "Animations", "Models", "Materials", "Textures" })
        {
            if (AssetDatabase.IsValidFolder(art + "/" + folder)) continue;
            var error = AssetDatabase.MoveAsset(source + "/" + folder, art + "/" + folder);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }
        var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quirky Series/Insect Bundle/Insect Vol.1/Prefabs/Ant_LODs.prefab");
        if (original == null) throw new InvalidOperationException("Ant_LODs prefab missing.");
        var root = Object.Instantiate(original);
        try
        {
            root.name = "QuirkyAnt";
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Animator, avatar or controller is missing.");
            animator.applyRootMotion = false;
            var material = AssetDatabase.LoadAssetAtPath<Material>(art + "/Materials/M_Ant.mat");
            var texture = material.GetTexture("_MainTex");
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .25f); EditorUtility.SetDirty(material);
            var meshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (meshes.Length == 0 || meshes.Any(m => m.sharedMesh == null)) throw new InvalidOperationException("Mesh dependency missing.");
            root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
            var bounds = meshes[0].bounds;
            var factor = 2.5f / Mathf.Max(bounds.size.x, bounds.size.z);
            root.transform.localScale = Vector3.one * factor;
            var wrapper = new GameObject("QuirkyAnt");
            try
            {
                root.transform.SetParent(wrapper.transform, true);
                root.transform.localPosition = -Vector3.up * meshes[0].bounds.min.y;
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                PrefabUtility.SaveAsPrefabAsset(wrapper, "Assets/Resources/QuirkyAnt.prefab");
            }
            finally { Object.DestroyImmediate(wrapper); }
            var ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/AntSelection.mat");
            if (ring == null)
            {
                ring = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                ring.color = Color.white; AssetDatabase.CreateAsset(ring, "Assets/Resources/AntSelection.mat");
            }
            AssetDatabase.SaveAssets();
            return "QuirkyAnt installed: LOD meshes, URP material, original avatar/controller; root motion and ragdoll collisions disabled.";
        }
        finally { if (root != null) Object.DestroyImmediate(root); }
    }
}
