using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    // Equipment follows authored hands; bow clearance is a small length-preserving correction.
    public sealed class MeshyNativeEquipment : MonoBehaviour
    {
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        Transform mount;
        GameObject item;
        Transform arrow;
        Quaternion arrowAxis;
        Vector3 arrowSourceScale;
        string held;
        float handSpan;
        MeshyNativeFingers fingers;
        public float FingerContactError {get;private set;}
        public float BowHeadClearance {get;private set;}
        bool rebuild;
        Transform mealBowl;
        Transform readingBook;
        public void PoseBook(float seconds)
        {
            var centre=bones["mixamorig:Hips"].position+transform.up*.29f+transform.forward*.34f;
            if(!readingBook)
            {
                var source=Resources.Load<GameObject>("Furniture1113/Models/OpenBook/OpenBook");
                if(!source) return;
                readingBook=new GameObject("Reading book grip").transform;readingBook.SetParent(transform,false);
                readingBook.localScale=Vector3.one/Mathf.Max(.001f,transform.lossyScale.x);
                var model=Instantiate(source,readingBook,false).transform;
                var renders=model.GetComponentsInChildren<Renderer>();var bounds=renders[0].bounds;
                foreach(var r in renders)bounds.Encapsulate(r.bounds);
                model.localScale*=.36f/Mathf.Max(bounds.size.x,bounds.size.z);
                bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
                model.position+=readingBook.position-bounds.center;
                foreach(var collider in model.GetComponentsInChildren<Collider>())Destroy(collider);
            }
            readingBook.gameObject.SetActive(true);
            readingBook.SetPositionAndRotation(centre,transform.rotation*Quaternion.Euler(-18,0,0));
            PlacePalm("Left",centre-transform.right*.14f-transform.up*.025f,transform.forward,-transform.up);
            // Briefly move over the right page, then return to supporting its edge.
            float turn=Mathf.Pow(Mathf.Max(0,Mathf.Sin(seconds*1.3f)),8);
            PlacePalm("Right",centre+transform.right*(.14f-.09f*turn)+transform.up*(-.025f+.07f*turn),transform.forward,-transform.up);
        }
        public void PoseSaw(Vector3 surface,float seconds)
        {
            if(!mount || held!="Tool_Saw")return;
            var handle=surface-transform.forward*(.16f+.075f*Mathf.Sin(seconds*6))+transform.up*.065f;
            PlacePalm("Right",handle,transform.up,-transform.right);
            PlacePalm("Left",surface-transform.right*.22f+transform.up*.04f,transform.right,transform.up);
            mount.SetPositionAndRotation(Palm("Right"),Quaternion.LookRotation(transform.forward,transform.up));
            fingers.Wrap(false,Palm("Right"),transform.up,.017f);
        }
        public void PoseMeal(float seconds)
        {
            if(!bones.ContainsKey("mixamorig:Head")) return;
            var hip=bones["mixamorig:Hips"].position;
            var bowl=hip-transform.right*.14f+transform.forward*.22f+transform.up*.18f;
            var mouth=bones["mixamorig:Head"].position+transform.forward*.16f-transform.up*.07f;
            float lift=Mathf.SmoothStep(0,1,.5f-.5f*Mathf.Cos(seconds*Mathf.PI*2/3f));
            var right=Vector3.Lerp(bowl+transform.up*.10f+transform.right*.08f,mouth,lift);
            PlacePalm("Left",bowl,transform.forward,transform.up);
            PlacePalm("Right",right,transform.right,transform.up);
            fingers.Wrap(false,right,transform.right,.012f);
            if(!mealBowl)
            {
                var source=Resources.Load<GameObject>("Furniture1113/Models/Bowl/Bowl");
                if(!source) return;
                mealBowl=new GameObject("Meal bowl grip").transform;mealBowl.SetParent(transform,false);
                mealBowl.localScale=Vector3.one/Mathf.Max(.001f,transform.lossyScale.x);
                var model=Instantiate(source,mealBowl,false).transform;
                var renders=model.GetComponentsInChildren<Renderer>();
                var bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
                model.localScale*=.24f/Mathf.Max(bounds.size.x,bounds.size.z);
                bounds=renders[0].bounds;foreach(var r in renders)bounds.Encapsulate(r.bounds);
                model.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)-mealBowl.position;
                foreach(var c in model.GetComponentsInChildren<Collider>())Destroy(c);
            }
            mealBowl.gameObject.SetActive(true);
            mealBowl.SetPositionAndRotation(Palm("Left")+transform.up*.015f,Quaternion.LookRotation(transform.forward,Vector3.up));
        }
        public void SetHandSpan(float span)
        {
            if(Mathf.Abs(span-handSpan)<.0001f)return;
            handSpan=span;rebuild=true;
        }
        public float SupportShaftError { get; private set; }
        public void Bind(Transform model)
        {
            bones.Clear();foreach(var t in model.GetComponentsInChildren<Transform>())bones[t.name]=t;
            fingers=new MeshyNativeFingers(bones);
        }
        Vector3 Palm(string side)
        {
            var wrist=bones["mixamorig:"+side+"Hand"];
            return Vector3.Lerp(wrist.position,bones["mixamorig:"+side+"HandMiddle1"].position,.65f);
        }
        static bool IsGun(string name)=>name!=null&&(name.Contains("Rifle")||name.Contains("Carbine")||name.Contains("Scattergun")||name.Contains("AcidSprayer"));
        void PlacePalm(string side,Vector3 target,Vector3 axis,Vector3 normal)
        {
            var wrist=bones["mixamorig:"+side+"Hand"];
            var across=bones["mixamorig:"+side+"HandIndex1"].position-bones["mixamorig:"+side+"HandPinky1"].position;
            var along=bones["mixamorig:"+side+"HandMiddle1"].position-wrist.position;
            wrist.rotation=Quaternion.LookRotation(axis,normal)*Quaternion.Inverse(Quaternion.LookRotation(across,Vector3.Cross(across,along)))*wrist.rotation;
            for(int i=0;i<3;i++)MeshyNativeBow.Solve(bones["mixamorig:"+side+"Arm"],bones["mixamorig:"+side+"ForeArm"],wrist,wrist.position+target-Palm(side),-transform.up+(side=="Right"?transform.right:-transform.right));
        }
        public void Pose(string name,string motion,float phase=-1)
        {
            if(mealBowl)mealBowl.gameObject.SetActive(motion=="MealSeated");
            if(readingBook)readingBook.gameObject.SetActive(motion=="ReadBook");
            FingerContactError=0; SupportShaftError=0; BowHeadClearance=0;
            if(rebuild||held!=name)
            {
                rebuild=false;held=name;if(mount){mount.gameObject.SetActive(false);if(Application.isPlaying)Destroy(mount.gameObject);else DestroyImmediate(mount.gameObject);}mount=null;item=null;arrow=null;
                if(string.IsNullOrEmpty(name))return;
                var prefab=MeshyEquipmentModel.Load(name);if(!prefab)return;
                mount=new GameObject("Native equipment grip").transform;mount.SetParent(transform,false);
                item=Instantiate(prefab,mount,false);
                item.transform.localScale/=Mathf.Max(.001f,mount.lossyScale.x);
                MeshyEquipmentModel.ApplyFinish(item,name);
                if(IsGun(name)){item.transform.localRotation=Quaternion.identity;if(!name.Contains("AcidSprayer"))item.transform.localScale*=.7f;}
                if(name.Contains("Bow"))
                {
                    var source=MeshyEquipmentModel.Load("Ammo_Arrow");
                    if(source){arrow=Instantiate(source,mount,false).transform;arrowAxis=source.transform.localRotation;arrowSourceScale=source.transform.localScale;}
                }
                if(name.StartsWith("Tool_Pickaxe_")||name.StartsWith("Tool_Axe_")||name.StartsWith("Tool_Hammer_")||name.StartsWith("Tool_Shovel_")||name.StartsWith("Tool_Hoe_")||name.Contains("Quakehammer"))
                {
                    // Imported handles span -0.20 (lead grip) to +0.10 (lower grip).
                    // Fit their actual usable span to the original animation's hand spacing.
                    float lead=name.Contains("Quakehammer")?.30f:name.StartsWith("Tool_Hammer_")?.12f:.20f;
                    float span=name.Contains("Quakehammer")?.46f:name.StartsWith("Tool_Hammer_")?.19f:.30f;
                    float worldScale=(Mathf.Max(handSpan,Vector3.Distance(Palm("Left"),Palm("Right")))+.035f)/span;
                    float localScale=worldScale/Mathf.Max(.001f,mount.lossyScale.x);
                    // Lengthen the shaft without enlarging its thickness or the head width.
                    item.transform.localScale=new Vector3((name.Contains("Quakehammer")?.65f:1f)/Mathf.Max(.001f,mount.lossyScale.x),localScale,(name.Contains("Quakehammer")?.65f:1f)/Mathf.Max(.001f,mount.lossyScale.x));
                    item.transform.localPosition=Vector3.forward*(lead*localScale);
                }
                foreach(var collider in item.GetComponentsInChildren<Collider>())Destroy(collider);
            }
            if(!mount||bones.Count==0)return;
            if(held.Contains("Bow")&&motion=="BowShoot")
                BowHeadClearance=MeshyNativeBow.ClearHead(bones,transform.up,transform.right);
            if(IsGun(held))
            {
                var shoulder=bones["mixamorig:RightArm"].position;
                var target=shoulder-transform.right*.045f-transform.up*.075f+transform.forward*.20f;
                PlacePalm("Right",target,(transform.up+transform.forward*.4f).normalized,-transform.right);
                mount.SetPositionAndRotation(Palm("Right"),transform.rotation);
                FingerContactError=fingers.Wrap(false,mount.position,(transform.up+transform.forward*.4f).normalized,.012f);
                if(!held.Contains("AcidSprayer")){
                    var support=mount.position+transform.forward*(held.Contains("Sniper")?.19f:.18f)+transform.up*.014f;
                    PlacePalm("Left",support,transform.forward,transform.up);
                    SupportShaftError=Vector3.Distance(Palm("Left"),support);
                    FingerContactError=Mathf.Max(FingerContactError,fingers.Wrap(true,support,transform.forward,.019f));
                }
                return;
            }
            bool twoHand=(held.StartsWith("Tool_")||held.Contains("Quakehammer"))&&(motion=="Tool_Mine"||motion=="Tool_Chop"||motion=="Tool_Hammer"||motion=="Tool_Dig"||motion=="Tool_Till"||motion=="SmithRepair");
            bool left=held.Contains("Bow")||held.Contains("Shield");
            string side=left?"Left":"Right";var wrist=bones["mixamorig:"+side+"Hand"];
            var primary=Palm(side);mount.position=primary;
            if(twoHand)
            {
                // Keep the head and handle ends in the approved grip positions.
                var support=Palm("Left");var shaft=support-primary;
                if(shaft.sqrMagnitude>.00001f)
                {
                    var facing=Vector3.ProjectOnPlane(transform.forward,shaft.normalized);
                    if(facing.sqrMagnitude<.00001f)facing=Vector3.ProjectOnPlane(transform.right,shaft.normalized);
                    // The pick head extends along X: keep its cutting plane in the swing plane.
                    mount.rotation=Quaternion.LookRotation(shaft.normalized,Vector3.Cross(shaft.normalized,facing.normalized));
                    // Turn only the axe blade front-to-back around its shaft.
                    if(held.StartsWith("Tool_Axe_"))mount.rotation*=Quaternion.AngleAxis(180f,Vector3.forward);
                    SupportShaftError=Vector3.Cross(support-primary,mount.forward).magnitude;
                    FingerContactError=Mathf.Max(fingers.Wrap(left,primary,mount.forward,.012f),fingers.Wrap(!left,support,mount.forward,.012f));
                }
            }
            else
            {
                var across=bones["mixamorig:"+side+"HandIndex1"].position-bones["mixamorig:"+side+"HandPinky1"].position;
                var alongFingers=bones["mixamorig:"+side+"HandMiddle1"].position-wrist.position;
                mount.rotation=held.Contains("Bow")
                    ?Quaternion.LookRotation(across.normalized,-Vector3.ProjectOnPlane(transform.forward,across.normalized).normalized)
                    :Quaternion.LookRotation(-across.normalized,Vector3.Cross(across,alongFingers).normalized);
                if(held.Contains("Shield"))mount.rotation*=Quaternion.AngleAxis(180f,Vector3.forward);
                SupportShaftError=0;
                if(held.Contains("Bow"))FingerContactError=fingers.Wrap(true,primary,mount.forward,.009f);
                else if(held.StartsWith("Weapon_")||held.StartsWith("Tool_"))
                    FingerContactError=fingers.Wrap(left,primary,mount.forward,.012f);
            }
            if(arrow)
            {
                // Archery_Shot_3 starts drawn and releases at the beginning of the recoil.
                arrow.gameObject.SetActive(motion=="BowShoot"&&phase>=0&&phase<.16f);
                if(arrow.gameObject.activeSelf)
                {
                    var nock=Palm("Right");var aim=primary-nock;float length=(aim.magnitude+.08f)/.31f;
                    if(aim.sqrMagnitude>.00001f)
                    {
                        arrow.rotation=Quaternion.LookRotation(-aim.normalized,transform.up)*arrowAxis;
                        arrow.localScale=Vector3.Scale(arrowSourceScale,new Vector3(1,length,1))/Mathf.Max(.001f,mount.lossyScale.x);
                        arrow.position=nock+aim.normalized*(.15f*length);
                    }
                }
            }
        }
    }
}
