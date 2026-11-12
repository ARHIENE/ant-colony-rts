using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using AntColony.Units;

// Run after rebuilding and again after reimport; visible counts alone missed corrupt indices.
public static class Appearance1112AssetAudit
{
    public static string Run()
    {
        var rows = new List<string>();
        for (int rig = 0; rig < 2; rig++)
        {
            var kit = Resources.Load<MeshyNativeAppearanceSet>("MeshyNative/SourceParts/Kit" + rig);
            if (!kit || kit.parts == null) throw new Exception("Missing kit " + rig);
            foreach (var part in kit.parts)
            {
                var mesh = part.mesh;
                if (!mesh) throw new Exception("Missing mesh " + part.name);
                var vertices = mesh.vertices;
                var indices = mesh.triangles;
                if (indices.Any(i => i < 0 || i >= vertices.Length)) throw new Exception(part.name + " invalid index");
                if (mesh.normals.Length != vertices.Length || mesh.uv.Length != vertices.Length || mesh.boneWeights.Length != vertices.Length)
                    throw new Exception(part.name + " stream length mismatch");
                if (vertices.Any(v => !Finite(v.x) || !Finite(v.y) || !Finite(v.z))) throw new Exception(part.name + " nonfinite vertex");
                if (part.name.StartsWith("Part_Hair_") && (indices.Length / 3 != 39103 || mesh.blendShapeCount != 0))
                    throw new Exception(part.name + " wrong hair partition or facial shapes");
                if (part.name.StartsWith("Part_Face_") && (indices.Length / 3 != 4741 || mesh.blendShapeCount != 8))
                    throw new Exception(part.name + " wrong face partition or shapes");
                var delta = new Vector3[vertices.Length];
                for (int shape = 0; shape < mesh.blendShapeCount; shape++)
                    for (int frame = 0; frame < mesh.GetBlendShapeFrameCount(shape); frame++)
                    {
                        mesh.GetBlendShapeFrameVertices(shape, frame, delta, null, null);
                        if (delta.Any(v => !Finite(v.x) || !Finite(v.y) || !Finite(v.z)))
                            throw new Exception(part.name + " nonfinite facial shape");
                    }
                rows.Add($"Rig{rig} {part.name}: vertices={vertices.Length}, triangles={indices.Length / 3}, shapes={mesh.blendShapeCount}");
            }
        }
        File.WriteAllLines("work/appearance-overlap-1112/rebuilt-asset-audit.txt", rows);
        return rows.Count + " meshes: index/stream/shape integrity passed; visual acceptance separate";
    }
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
