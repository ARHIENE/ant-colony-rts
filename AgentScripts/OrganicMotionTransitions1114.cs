using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrganicMotionTransitions1114
{
    public static string Main()
    {
        var report=new StringBuilder();
        var scene=EditorSceneManager.NewPreviewScene();
        int total=0;
        try
        {
            foreach(var directory in Directory.GetDirectories("Assets/Resources/OrganicEnemies1114"))
            {
                string name=Path.GetFileName(directory), root=directory.Replace('\\','/');
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Idle.fbx");
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"/"+name+".controller");
                if(!source || !controller) throw new Exception("Missing model/controller: "+name);
                if(controller.layers.Length!=1) throw new Exception("Unexpected animation layers: "+name);
                var go=UnityEngine.Object.Instantiate(source); SceneManager.MoveGameObjectToScene(go,scene);
                try
                {
                    var animators=go.GetComponentsInChildren<Animator>(true);
                    if(animators.Length>1) throw new Exception("Multiple animators: "+name);
                    var animator=animators.Length==0?go.AddComponent<Animator>():animators[0];
                    animator.avatar=AssetDatabase.LoadAllAssetsAtPath(root+"/Idle.fbx").OfType<Avatar>().FirstOrDefault();
                    animator.runtimeAnimatorController=controller;
                    animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();
                    var bones=skin.bones; var before=new Quaternion[bones.Length];
                    var names=name=="BloomMender"?new[]{"Idle","Move","Attack","Hit","Death","Heal"}:new[]{"Idle","Move","Attack","Hit","Death"};
                    int pairs=0,maxClips=0; float maxAngle=0;
                    for(int from=0;from<names.Length;from++) for(int to=0;to<names.Length;to++)
                    {
                        animator.Rebind(); animator.SetInteger("Action",from);
                        animator.Play("Base Layer."+names[from],0,.35f); animator.Update(0);
                        for(int b=0;b<bones.Length;b++) before[b]=bones[b].localRotation;
                        animator.SetInteger("Action",to);
                        for(int step=0;step<24;step++)
                        {
                            animator.Update(1f/60);
                            int clips=animator.GetCurrentAnimatorClipInfo(0).Count(c=>c.weight>.001f)
                                +animator.GetNextAnimatorClipInfo(0).Count(c=>c.weight>.001f);
                            maxClips=Math.Max(maxClips,clips);
                            if(clips>2) throw new Exception("More than two blended clips: "+name);
                            for(int b=0;b<bones.Length;b++)
                            {
                                var q=bones[b].localRotation; var p=bones[b].localPosition;
                                if(float.IsNaN(p.sqrMagnitude)||float.IsInfinity(p.sqrMagnitude)||float.IsNaN(Quaternion.Dot(q,q)))
                                    throw new Exception("Nonfinite bone: "+name+"/"+names[to]);
                                maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(before[b],q)); before[b]=q;
                            }
                        }
                        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer."+names[to]))
                            throw new Exception("State stuck: "+name+" "+names[from]+" -> "+names[to]);
                        pairs++;
                    }
                    total+=pairs;
                    report.AppendLine(name+": PASS "+pairs+" pairs, layers=1, animators=1, maxActiveClips="+maxClips+", maxJointStep60fps="+maxAngle.ToString("F2"));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        report.AppendLine("Total transition pairs: "+total+". This checks blend ownership/state/finite transforms, not full mesh self-collision or live navigation foot contact.");
        File.WriteAllText("work/organic-combat-1114/motion-audit/unity-transitions.txt",report.ToString());
        return report.ToString();
    }
}
