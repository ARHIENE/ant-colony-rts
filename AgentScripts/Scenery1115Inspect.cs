using System.Linq;
using AntColony.World;
using UnityEngine;
public static class Scenery1115Inspect
{
    public static string Main() => string.Join("\n", Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude)
        .Where(r => r.enabled && !r.forceRenderingOff && r.bounds.size.magnitude > 4
            && (r.bounds.center - WorldMapManager.Instance.HomePosition).sqrMagnitude < 120 * 120)
        .Select(r => new { renderer = r, mesh = r.GetComponent<MeshFilter>()?.sharedMesh })
        .Where(p => p.mesh && new[] { "Cube", "Cylinder", "Sphere", "Capsule" }.Contains(p.mesh.name))
        .Select(p => p.renderer.name + " parent=" + p.renderer.transform.parent?.name + " root=" + p.renderer.transform.root.name
            + " center=" + p.renderer.bounds.center + " size=" + p.renderer.bounds.size
            + " scripts=" + string.Join(",", p.renderer.GetComponentsInParent<MonoBehaviour>().Where(c=>c).Select(c=>c.GetType().Name))));
}
