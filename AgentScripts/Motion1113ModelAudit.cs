using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class Motion1113ModelAudit
{
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Edit mode required");
        var report = new StringBuilder();
        var paths = Directory.GetFiles("Assets/Resources/Furniture1113/Models", "*.fbx", SearchOption.AllDirectories);
        foreach (var path in paths)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            if (!model) throw new Exception("Missing imported model " + path);
            var go = UnityEngine.Object.Instantiate(model);
            try
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("No renderer " + path);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    foreach (var material in renderer.sharedMaterials)
                        if (!material || !material.shader || !material.shader.isSupported || !material.GetTexture("_BaseMap"))
                            throw new Exception("Missing supported textured material " + path);
                }
                report.AppendLine(model.name + " size=" + bounds.size.ToString("F4") + " center=" + bounds.center.ToString("F4"));
                if (model.name == "SingleBed")
                {
                    // Measure the authored mattress at the central sleeping area instead of the tall headboard.
                    var vertices = go.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v)));
                    var central = vertices.Where(v => Math.Abs(v.x-bounds.center.x) < bounds.size.x*.2f && Math.Abs(v.z-bounds.center.z) < bounds.size.z*.2f).ToArray();
                    if (central.Length == 0) throw new Exception("No central bed geometry");
                    report.AppendLine("SingleBed centralSupportY=" + central.Max(v=>v.y) + " normalized=" + ((central.Max(v=>v.y)-bounds.min.y)/bounds.size.y));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        File.WriteAllText("work/motion-actions-1113/imported-model-audit.txt", report.ToString());
        return report.ToString();
    }
}
