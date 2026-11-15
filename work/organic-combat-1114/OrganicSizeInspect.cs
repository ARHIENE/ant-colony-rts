using System.Linq;
using System.Text;
using AntColony.World;
using UnityEngine;
public static class OrganicSizeInspect
{
    public static string Main()
    {
        var monster = WildMonster.All.First(m => m.IsOrganic);
        var result = new StringBuilder();
        foreach (var t in monster.GetComponentsInChildren<Transform>().Take(7)) result.AppendLine(t.name + " local=" + t.localScale + " world=" + t.lossyScale + " pos=" + t.localPosition);
        var skin = monster.GetComponentInChildren<SkinnedMeshRenderer>();
        result.AppendLine("renderer="+skin.bounds+" localBounds="+skin.localBounds+" mesh="+skin.sharedMesh.bounds);
        foreach (bool scale in new[]{false,true})
        {
            var mesh = new Mesh(); skin.BakeMesh(mesh,scale); mesh.RecalculateBounds();
            var v=mesh.vertices; var b=new Bounds(skin.transform.TransformPoint(v[0]),Vector3.zero);
            foreach(var p in v) b.Encapsulate(skin.transform.TransformPoint(p));
            result.AppendLine("bakeScale="+scale+" local="+mesh.bounds+" world="+b);
            Object.DestroyImmediate(mesh);
        }
        return result.ToString();
    }
}
