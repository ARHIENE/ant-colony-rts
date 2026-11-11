using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Units
{
    [DefaultExecutionOrder(150)]
    public sealed class MeshyEquipmentGrip : MonoBehaviour
    {
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly Dictionary<Transform,Quaternion> relaxedFingers=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> fingerPositions=new Dictionary<Transform,Vector3>();
        public string Held;
        public string Motion;
        readonly Dictionary<Transform,Quaternion> socketRest=new Dictionary<Transform,Quaternion>();
        float attackStart=-100,attackDuration=.45f;
        public void CaptureRestPose(){if(bones.Count==0)Awake();}
        public void BeginAttack(float duration){attackStart=Time.time;attackDuration=Mathf.Max(.1f,duration);}
        void Awake(){foreach(var t in GetComponentsInChildren<Transform>(true))if(!bones.ContainsKey(t.name)){
            bones.Add(t.name,t);
            if(t.name.StartsWith("Socket.Tool."))socketRest[t]=t.localRotation;
            if(t.name.StartsWith("f_")||t.name.StartsWith("thumb.")){relaxedFingers[t]=t.localRotation;fingerPositions[t]=t.localPosition;}
            if(t.name.StartsWith("forearm.")||t.name.StartsWith("hand."))fingerPositions[t]=t.localPosition;
        }}
        void LateUpdate(){Pose(false);}
        static bool IsGun(string name)=>name!=null&&(name.Contains("Rifle")||name.Contains("Carbine")||name.Contains("Scattergun")||name.Contains("AcidSprayer"));
        public void Pose(bool presentation, float samplePhase=-1)
        {
            if(bones.Count==0)Awake();
            bool working=Held!=null&&Held.StartsWith("Tool_")&&(Motion=="Tool_Mine"||Motion=="Tool_Chop"||Motion=="Tool_Dig"||Motion=="Tool_Till"||Motion=="Tool_Hammer"||Motion=="SmithRepair");
            foreach(var pair in fingerPositions)if(pair.Key!=null)pair.Key.localPosition=pair.Value;
            // The old rig socket sat at the fingertips. Seat the handle inside the palm.
            foreach(var side in new[]{"L","R"})if(bones.TryGetValue("Socket.Tool."+side,out var palm))
            {
                palm.localPosition=new Vector3(side=="L"?-.003f:.003f,.019f,.012f);
                if(socketRest.TryGetValue(palm,out var socketRotation))palm.localRotation=socketRotation;
                if(working&&bones.TryGetValue("f_index.01."+side,out var index)&&bones.TryGetValue("f_pinky.01."+side,out var pinky)&&bones.TryGetValue("f_middle.01."+side,out var middle))
                {
                    // Run the shaft across the knuckles, not along the fingers.
                    var across=(index.localPosition-pinky.localPosition).normalized;
                    var along=relaxedFingers[middle]*Vector3.up;
                    var normal=Vector3.Cross(across,along).normalized;
                    palm.localPosition=(index.localPosition+pinky.localPosition)*.5f+along*.012f;
                    palm.localRotation=Quaternion.LookRotation(across,normal);
                }
                if(IsGun(Held))palm.localRotation=Quaternion.Inverse(side=="R"
                    ?Quaternion.LookRotation(-Vector3.right,-Vector3.forward)
                    :Quaternion.LookRotation(Vector3.up,Vector3.right));
            }
            if(string.IsNullOrEmpty(Held)){
                return;
            }
            // Release the previously used hand when switching between bow, shield and right-hand tools.
            foreach(var pair in relaxedFingers)if(pair.Key!=null)pair.Key.localRotation=pair.Value;
            var up=transform.up;var forward=transform.forward;
            if(working && bones.TryGetValue("upper_arm.R",out var armR)&&bones.TryGetValue("upper_arm.L",out var armL))
            {
                // Keep the authored wind-up, impact and recovery in phase with the torso.
                // The former sine-wave target erased the original arm animation.
                var span=bones["hand.L"].position-bones["hand.R"].position;
                var axis=span.sqrMagnitude>.000001f?span.normalized:forward;
                var grip=bones["Socket.Tool.R"].position;
                if(bones.TryGetValue("head",out var head))
                {
                    // Clear the shaft and tool head, not just the wrist. The face sits
                    // above the head joint on this short, large-headed character.
                    var centre=head.position+up*.16f;
                    for(int pass=0;pass<3;pass++)
                    {
                        float along=Mathf.Clamp(Vector3.Dot(centre-grip,axis),0,.45f);
                        float intrusion=Mathf.Max(0,.30f-Vector3.Distance(grip+axis*along,centre));
                        grip+=forward*intrusion*.4f;
                        axis=(axis*.45f+forward*intrusion).normalized;
                    }
                }
                var rotation=Quaternion.LookRotation(-axis,Vector3.Cross(-axis,transform.right));
                Place("R",grip,rotation,true);
                if(bones.TryGetValue("Socket.Tool.R",out var shaft)&&bones.TryGetValue("Socket.Tool.L",out var support))
                    for(int attempt=0;attempt<8;attempt++)
                    {
                        var contact=shaft.position+shaft.rotation*Vector3.back*Mathf.Clamp(span.magnitude,.12f,.18f);
                        Place("L",contact,rotation,true);
                        var error=support.position-contact;
                        if(error.sqrMagnitude<.000004f)break;
                        // Project the common shaft into both arms' reachable space.
                        Place("R",shaft.position+error,rotation,true);
                    }
            }
            else if(IsGun(Held))
            {
                if(!bones.TryGetValue("upper_arm.R",out var shoulder))return;
                float elapsed=Time.time-attackStart;
                float recoil=elapsed>=0&&elapsed<attackDuration?Mathf.Exp(-elapsed*20)*.035f:0;
                if(Held.Contains("Carbine")&&elapsed<.28f)recoil=Mathf.Exp(-(elapsed%.09f)*30)*.022f;
                // Bring the rear grip toward the chest centre; the previous forward offset
                // put the support point outside this character's short arm reach.
                float shoulderSide=Mathf.Sign(Vector3.Dot(shoulder.position-transform.position,transform.right));
                var grip=shoulder.position-transform.right*shoulderSide*.035f-up*.065f+forward*(.19f-recoil);
                var aim=transform.rotation*Quaternion.Euler(-recoil*120,0,0);
                Place("R",grip,aim);
                // Anchor the support hand to the solved main hand, including recoil and reach limits.
                if(!Held.Contains("AcidSprayer")&&bones.TryGetValue("Socket.Tool.R",out var right))
                    Place("L",right.position+right.rotation*new Vector3(0,.014f,Held.Contains("Sniper")?.19f:.18f),aim);
            }
            else if(presentation)
            {
                bool bow=Held.Contains("Bow");var side=bow?"L":"R";
                if(!bones.TryGetValue("upper_arm."+side,out var shoulder))return;
                var sign=Mathf.Sign(Vector3.Dot(shoulder.position-transform.position,transform.right));
                var grip=shoulder.position-up*.34f+forward*.15f+transform.right*sign*.10f;
                Place(side,grip,transform.rotation*Quaternion.Euler(0,bow?90:0,bow?0:-sign*18));
            }
            if(!working&&(Held.Contains("Katana")||Held.Contains("Quakehammer")||Held.Contains("Pickaxe")||Held.Contains("Shovel")||Held.Contains("Hoe"))
                &&bones.TryGetValue("Socket.Tool.R",out var main))
            {
                // Keep the existing whole-body swing; the second hand follows the handle, never world space.
                float separation=Held.Contains("Katana")?-.075f:Held.Contains("Quakehammer")?-.11f:.13f;
                Place("L",main.position+main.rotation*Vector3.back*separation,main.rotation);
            }
            bool bowHeld=Held.Contains("Bow"),shieldHeld=Held.Contains("Shield");
            CloseHand(shieldHeld||bowHeld?"L":"R",bowHeld?.30f:1f,IsGun(Held));
            if(IsGun(Held)&&!Held.Contains("AcidSprayer")||Held.Contains("Katana")||Held.Contains("Quakehammer")||Held.Contains("Pickaxe")||Held.Contains("Shovel")||Held.Contains("Hoe"))CloseHand("L",.8f,false);
            if(IsGun(Held))
            {
                WrapHand("R",new Vector3(0,1,.4f),.012f,true);
                if(!Held.Contains("AcidSprayer"))WrapHand("L",Vector3.forward,.019f,false);
            }
            else if(working)
            {
                WrapHand("R",Vector3.forward,.009f,false);
                WrapHand("L",Vector3.forward,.009f,false);
            }
        }
        void WrapHand(string side,Vector3 handleAxis,float radius,bool trigger)
        {
            if(!bones.TryGetValue("Socket.Tool."+side,out var socket))return;
            var axis=socket.TransformDirection(handleAxis).normalized;
            foreach(var finger in new[]{"index","middle","ring","pinky"})
            {
                var chain=new Transform[3];bool valid=true;
                for(int j=0;j<3;j++)if(!bones.TryGetValue("f_"+finger+".0"+(j+1)+"."+side,out chain[j]))valid=false;
                if(!valid)continue;
                var knuckle=chain[0].position;
                var centre=socket.position+axis*Vector3.Dot(knuckle-socket.position,axis);
                var radial=Vector3.ProjectOnPlane(knuckle-centre,axis).normalized;
                var target=centre-radial*radius;
                if(trigger&&finger=="index")target=socket.TransformPoint(new Vector3(0,.012f,.028f));
                float tipLength=chain[2].localPosition.magnitude;
                var signs=new float[3];
                var hand=bones["hand."+side];
                for(int j=0;j<3;j++)signs[j]=Mathf.Sign(Vector3.Dot(chain[j].parent.rotation*relaxedFingers[chain[j]]*Vector3.forward,hand.forward));
                for(int iteration=0;iteration<8;iteration++)for(int j=2;j>=0;j--)
                {
                    var tip=chain[2].TransformPoint(Vector3.up*tipLength);
                    var hinge=chain[j].right;
                    var from=Vector3.ProjectOnPlane(tip-chain[j].position,hinge);
                    var to=Vector3.ProjectOnPlane(target-chain[j].position,hinge);
                    if(from.sqrMagnitude<1e-10f||to.sqrMagnitude<1e-10f)continue;
                    float angle=Mathf.Clamp(Vector3.SignedAngle(from,to,hinge),-12,12);
                    var rest=relaxedFingers[chain[j]];
                    float current=Mathf.DeltaAngle(0,(Quaternion.Inverse(rest)*chain[j].localRotation).eulerAngles.x);
                    float limit=j==0?65:j==1?85:45;
                    float bend=Mathf.Clamp((current+angle)*signs[j],0,limit)*signs[j];
                    chain[j].localRotation=rest*Quaternion.Euler(bend,0,0);
                }
            }
            WrapThumb(side,axis,socket.position,radius);
        }
        void WrapThumb(string side,Vector3 axis,Vector3 centre,float radius)
        {
            var chain=new Transform[3];
            for(int j=0;j<3;j++)if(!bones.TryGetValue("thumb.0"+(j+1)+"."+side,out chain[j]))return;
            var basePoint=chain[0].position;
            centre+=axis*Vector3.Dot(basePoint-centre,axis);
            var radial=Vector3.ProjectOnPlane(basePoint-centre,axis).normalized;
            var target=centre-radial*radius;
            float length=chain[2].localPosition.magnitude;
            for(int iteration=0;iteration<12;iteration++)for(int j=2;j>=0;j--)
            {
                var tip=chain[2].TransformPoint(Vector3.up*length);
                var delta=Quaternion.FromToRotation(tip-chain[j].position,target-chain[j].position);
                var desired=Quaternion.Inverse(chain[j].parent.rotation)*delta*chain[j].rotation;
                chain[j].localRotation=Quaternion.RotateTowards(relaxedFingers[chain[j]],desired,j==0?75:85);
            }
        }
        void CloseHand(string side,float amount,bool trigger)
        {
            // Blender finger X axes are mirrored. Assign from the neutral pose, avoiding per-frame accumulation.
            float sign=side=="R"?-1:1;
            foreach(var finger in new[]{"index","middle","ring","pinky"})for(int joint=1;joint<=3;joint++)
            {
                if(!bones.TryGetValue("f_"+finger+".0"+joint+"."+side,out var bone)||!relaxedFingers.TryGetValue(bone,out var neutral))continue;
                float bend=(joint==1?48:joint==2?65:35)*amount;
                if(trigger&&finger=="index")bend*=.45f;
                float curl=Mathf.Sign(Vector3.Dot(bone.parent.rotation*neutral*Vector3.forward,bones["hand."+side].forward));
                bone.localRotation=neutral*Quaternion.Euler(curl*bend,0,0);
            }
            for(int joint=1;joint<=3;joint++)if(bones.TryGetValue("thumb.0"+joint+"."+side,out var thumb)&&relaxedFingers.TryGetValue(thumb,out var rest))
                thumb.localRotation=rest*Quaternion.Euler(sign*(joint==1?15:25)*amount,0,0);
        }
        void Place(string side,Vector3 grip,Quaternion socketRotation,bool preserveAnimatedElbow=false)
        {
            if(!bones.TryGetValue("upper_arm."+side,out var upper)||!bones.TryGetValue("forearm."+side,out var lower)
                ||!bones.TryGetValue("hand."+side,out var hand)||!bones.TryGetValue("Socket.Tool."+side,out var socket))return;
            var handRotation=socketRotation*Quaternion.Inverse(Quaternion.Inverse(hand.rotation)*socket.rotation);
            var offset=Quaternion.Inverse(hand.rotation)*(socket.position-hand.position);
            var target=grip-handRotation*offset;
            var a=upper.position;float l1=Vector3.Distance(a,lower.position),l2=Vector3.Distance(lower.position,hand.position);
            var delta=target-a;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);var axis=delta.sqrMagnitude>.000001f?delta.normalized:transform.forward;
            target=a+axis*distance;
            float shoulderSide=Mathf.Sign(Vector3.Dot(upper.position-transform.position,transform.right));
            var bend=Vector3.ProjectOnPlane(-transform.up+transform.right*shoulderSide*.4f,axis).normalized;
            if(preserveAnimatedElbow)
            {
                var authored=Vector3.ProjectOnPlane(lower.position-a,axis);
                if(authored.sqrMagnitude>.00001f)bend=authored.normalized;
            }
            float along=(l1*l1+distance*distance-l2*l2)/(2*distance);
            var elbow=a+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-a,elbow-a)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
            hand.rotation=handRotation;
        }
    }
}
