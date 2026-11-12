using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using AntColony.Units;
using AntColony.World;
public static class Corpse1112PausedChecks
{
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale != 0) throw new Exception("Paused isolated Play required");
        int count = 0;
        foreach (var kind in new[] { CorpseKind.ColonyCommander, CorpseKind.EnemyCommander, CorpseKind.Ant })
        {
            var corpse = Corpse.Spawn(new Corpse.State { kind=kind, name="Paused fixture", position=new Vector3(700,0,700) });
            try
            {
                var native=corpse.GetComponentInChildren<MeshyNativeCorpse>();
                if (kind == CorpseKind.Ant)
                {
                    if (!(bool)typeof(CorpseProp).GetField("shown",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(corpse.GetComponent<CorpseProp>()))
                        throw new Exception("Restored ordinary corpse needs simulation time to show its bundle");
                }
                else
                {
                    if(!native)throw new Exception("Restored commander has no native corpse");
                    var clip=(AnimationClip)typeof(MeshyNativeCorpse).GetField("clip",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(native);
                    float elapsed=(float)typeof(MeshyNativeCorpse).GetField("elapsed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(native);
                    if(elapsed<clip.length)throw new Exception("Restored body starts from standing/death start");
                    float bottom=float.PositiveInfinity,top=float.NegativeInfinity;var mesh=new Mesh();
                    try{foreach(var skin in native.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled))
                    {skin.BakeMesh(mesh);foreach(var v in mesh.vertices){float y=skin.transform.TransformPoint(v).y;bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}}}
                    finally{UnityEngine.Object.DestroyImmediate(mesh);}
                    if(Mathf.Abs(bottom-corpse.Position.y)>.03f || top-bottom>1.2f)throw new Exception("Restored body standing or floating: "+bottom+".."+top);
                }
                count++;
            }
            finally{UnityEngine.Object.DestroyImmediate(corpse.gameObject);}
        }
        var library=Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
        var fresh=MeshyNativeCorpse.Create(library,library.motions.First(m=>m.id=="Dead"),new Vector3(700,0,700),Quaternion.identity,true);
        try
        {
            if((float)typeof(MeshyNativeCorpse).GetField("elapsed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fresh.GetComponent<MeshyNativeCorpse>())!=0)
                throw new Exception("Fresh death was forced to final frame");
        }
        finally{UnityEngine.Object.DestroyImmediate(fresh);}
        var result="PASS "+count+" paused restore types: commander native end pose/floor, ordinary bundle immediately shown; fresh death starts at frame zero";
        File.WriteAllText("work/appearance-overlap-1112/corpse-drafts/paused-check.txt",result);return result;
    }
}
