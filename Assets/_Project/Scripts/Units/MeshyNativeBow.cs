using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Units
{
    // Keep the authored pose except for the clearance needed by the character's large head.
    static class MeshyNativeBow
    {
        public static float ClearHead(Dictionary<string,Transform> bones,Vector3 up,Vector3 right)
        {
            Vector3 Palm(string side)=>Vector3.Lerp(bones["mixamorig:"+side+"Hand"].position,bones["mixamorig:"+side+"HandMiddle1"].position,.65f);
            var centre=bones["mixamorig:Head"].position+up*.10f;
            var a=Palm("Right");var b=Palm("Left");var line=b-a;
            var closest=a+line*Mathf.Clamp01(Vector3.Dot(centre-a,line)/Mathf.Max(line.sqrMagnitude,1e-8f));
            var away=closest-centre;
            var shift=away.normalized*Mathf.Max(0,.23f-away.magnitude);
            if(shift.sqrMagnitude>1e-8f)
                foreach(var side in new[]{"Right","Left"})
                {
                    var wrist=bones["mixamorig:"+side+"Hand"];
                    Solve(bones["mixamorig:"+side+"Arm"],bones["mixamorig:"+side+"ForeArm"],wrist,wrist.position+shift,side=="Right"?right:-right);
                }
            a=Palm("Right");b=Palm("Left");line=b-a;
            return Vector3.Distance(centre,a+line*Mathf.Clamp01(Vector3.Dot(centre-a,line)/Mathf.Max(line.sqrMagnitude,1e-8f)));
        }
        public static void Solve(Transform upper,Transform lower,Transform wrist,Vector3 target,Vector3 fallback)
        {
            var rotation=wrist.rotation;var a=upper.position;
            float l1=Vector3.Distance(a,lower.position),l2=Vector3.Distance(lower.position,wrist.position);
            var axis=(target-a).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(target,a),Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);
            target=a+axis*distance;
            var bend=Vector3.ProjectOnPlane(lower.position-a,axis);
            if(bend.sqrMagnitude<.00001f)bend=Vector3.ProjectOnPlane(fallback,axis);
            float along=(l1*l1+distance*distance-l2*l2)/(2*distance);
            var elbow=a+axis*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-a,elbow-a)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(wrist.position-lower.position,target-lower.position)*lower.rotation;
            wrist.rotation=rotation;
        }
    }
}
