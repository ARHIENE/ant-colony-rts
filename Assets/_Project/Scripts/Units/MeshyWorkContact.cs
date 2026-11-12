using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    // Stand at the visible surface reached by the approved source swing, without changing its bones.
    public static class MeshyWorkContact
    {
        [Serializable] sealed class Profile { public string action; public Vector3[] points; }
        [Serializable] sealed class Profiles { public Profile[] profiles; }
        static Profiles profiles;
        static readonly Dictionary<Mesh, (Vector3[] vertices, int[] triangles)> meshes = new();
        static readonly Dictionary<(Component, string, int, int), float> radii = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            profiles = null; meshes.Clear(); radii.Clear();
        }

        public static bool TryRadius(Component target, string action, Vector3 outward, float floor, out float radius)
        {
            radius = 0;
            if (profiles == null)
            {
                var source = Resources.Load<TextAsset>("MeshyNative/WorkContactProfiles");
                if (!source) return false;
                profiles = JsonUtility.FromJson<Profiles>(source.text);
            }
            var profile = profiles?.profiles?.FirstOrDefault(p => p.action == action);
            if (profile == null) return false;
            var origin = target.transform.position;
            int angle = Mathf.RoundToInt(Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg * 10);
            var key = (target, action, angle, Mathf.RoundToInt((floor - origin.y) * 100));
            if (radii.TryGetValue(key, out radius)) return radius > 0;
            var right = Vector3.Cross(Vector3.up, -outward);
            float best = float.NegativeInfinity;
            foreach (var renderer in target.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || renderer.forceRenderingOff || !renderer.TryGetComponent<MeshFilter>(out var filter)) continue;
                var mesh = filter.sharedMesh;
                if (!mesh || !mesh.isReadable) continue;
                if (!meshes.TryGetValue(mesh, out var geometry))
                    meshes[mesh] = geometry = (mesh.vertices, mesh.triangles);
                var vertices = new Vector3[geometry.vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    var world = filter.transform.TransformPoint(geometry.vertices[i]);
                    var d = world - origin;
                    vertices[i] = new Vector3(Vector3.Dot(d, right), world.y - floor, Vector3.Dot(d, outward));
                }
                var triangles = geometry.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                    float denominator = (b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
                    if (Mathf.Abs(denominator) < .0000001f) continue;
                    float minX = Mathf.Min(a.x,b.x,c.x), maxX = Mathf.Max(a.x,b.x,c.x);
                    float minY = Mathf.Min(a.y,b.y,c.y), maxY = Mathf.Max(a.y,b.y,c.y);
                    foreach (var tip in profile.points)
                    {
                        if (tip.x < minX || tip.x > maxX || tip.y < minY || tip.y > maxY) continue;
                        float u = ((b.y-c.y)*(tip.x-c.x)+(c.x-b.x)*(tip.y-c.y))/denominator;
                        float v = ((c.y-a.y)*(tip.x-c.x)+(a.x-c.x)*(tip.y-c.y))/denominator;
                        if (u < 0 || v < 0 || u+v > 1) continue;
                        best = Mathf.Max(best, a.z*u+b.z*v+c.z*(1-u-v)+tip.z);
                    }
                }
            }
            // Small allowance for interpolation between the sampled poses, not a change in tool orientation.
            radius = float.IsNegativeInfinity(best) ? 0 : Mathf.Max(.4f, best + .015f);
            if (radii.Count > 512) radii.Clear();
            radii[key] = radius;
            return radius > 0;
        }
    }
}
