using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class OrganicEnemy1114Checks
{
    public static string Main()
    {
        var report=new StringBuilder();
        foreach(var name in new[]{"WallRam","NeedleStalker","BloomMender","ClawHunter","BroodAlien","BreachBeast","AcidSpitter","HuntRider","SiegeRider","WallMelter","SiegeBrooder","BroodCarrier","HookMaw","Bulwark","BurrowLurker","CarrionReclaimer","VeilWeaver"})
        {
            string root="Assets/Resources/OrganicEnemies1114/"+name;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Idle.fbx");
            if(!source) throw new Exception("Missing model: "+name);
            var go=UnityEngine.Object.Instantiate(source);
            go.hideFlags=HideFlags.HideAndDontSave;
            var baked=new Mesh();
            try
            {
                var smr=go.GetComponentInChildren<SkinnedMeshRenderer>();
                if(!smr || smr.bones.Length<2) throw new Exception("Missing skin: "+name);
                if(!smr.sharedMaterial || !smr.sharedMaterial.GetTexture("_BaseMap")) throw new Exception("Missing texture: "+name);
                var clipNames=name=="BloomMender"?new[]{"Idle","Move","Attack","Hit","Death","Heal"}:new[]{"Idle","Move","Attack","Hit","Death"};
                foreach(var cn in clipNames)
                {
                    var clip=AssetDatabase.LoadAllAssetsAtPath(root+"/"+cn+".fbx").OfType<AnimationClip>().First(c=>c.name==cn);
                    clip.SampleAnimation(go,0); smr.BakeMesh(baked); var first=baked.vertices;
                    float maximum=0;
                    foreach(float fraction in new[]{.2f,.45f,.7f,.99f})
                    {
                        clip.SampleAnimation(go,clip.length*fraction); smr.BakeMesh(baked); var current=baked.vertices;
                        for(int i=0;i<first.Length;i++) maximum=Mathf.Max(maximum,Vector3.Distance(first[i],current[i]));
                    }
                    if(maximum<.001f) throw new Exception("Static clip: "+name+"/"+cn);
                    bool loop=AnimationUtility.GetAnimationClipSettings(clip).loopTime;
                    if(loop!=(cn=="Idle" || cn=="Move")) throw new Exception("Wrong loop: "+cn);
                    report.AppendLine(name+"/"+cn+": PASS, skin delta "+maximum.ToString("F4")+", loop="+loop);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); UnityEngine.Object.DestroyImmediate(go); }
        }
        File.WriteAllText("art/enemy-concepts-1114/unity-motion-checks.txt",report.ToString());
        return report.ToString();
    }
}
