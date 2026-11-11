using System;
using System.Linq;
using System.Text;
using UnityEngine;
using AntColony.Units;

public static class Animation1111WorkChecks
{
    public static string Main()
    {
        var report=new StringBuilder();
        foreach(var seed in new[]{42,1111,87})
        {
            var owner=new GameObject("Work motion check");
            try
            {
                var model=KemonoModel.Create(owner.transform,seed);
                var animator=RigAnimator.Find(model);animator.enabled=false;
                var grip=model.GetComponent<MeshyEquipmentGrip>();grip.enabled=false;
                var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
                foreach(var setup in new[]{("Tool_Mine","Heavy_Hammer_Swing","Tool_Pickaxe_Stone"),("Tool_Chop","Charged_Axe_Chop","Tool_Axe_Stone")})
                {
                    KemonoModel.Hold(model,setup.Item3);grip.Motion=setup.Item1;
                    var clip=Resources.Load<AnimationClip>("Kemono/MeshyMotions/"+setup.Item2);
                    float gap=0,footDrift=0;Vector3[] startFeet=null;
                    for(int frame=0;frame<=120;frame++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/120);
                        grip.Pose(false);
                        var shaft=bones["Socket.Tool.R"];var support=bones["Socket.Tool.L"];
                        var offset=support.position-shaft.position;
                        gap=Mathf.Max(gap,Vector3.ProjectOnPlane(offset,shaft.forward).magnitude);
                        if(startFeet==null)startFeet=new[]{bones["foot.L"].position,bones["foot.R"].position};
                        footDrift=Mathf.Max(footDrift,Vector3.Distance(startFeet[0],bones["foot.L"].position),Vector3.Distance(startFeet[1],bones["foot.R"].position));
                    }
                    report.AppendLine($"seed={seed} {setup.Item1} supportShaftGap={gap:F5}m footDrift={footDrift:F5}m");
                    if(float.IsNaN(gap)||gap>.025f||footDrift>.04f)throw new Exception(report.ToString());
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }
        return report.ToString();
    }
}
