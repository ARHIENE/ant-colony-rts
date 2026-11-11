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
        public void Pose(string name,string motion,float phase=-1)
        {
            if(rebuild||held!=name)
            {
                rebuild=false;held=name;if(mount){mount.gameObject.SetActive(false);if(Application.isPlaying)Destroy(mount.gameObject);else DestroyImmediate(mount.gameObject);}mount=null;item=null;arrow=null;
                if(string.IsNullOrEmpty(name))return;
                var prefab=MeshyEquipmentModel.Load(name);if(!prefab)return;
                mount=new GameObject("Native equipment grip").transform;mount.SetParent(transform,false);
                item=Instantiate(prefab,mount,false);
                item.transform.localScale/=Mathf.Max(.001f,mount.lossyScale.x);
                MeshyEquipmentModel.ApplyFinish(item,name);
                if(name.Contains("Bow"))
                {
                    var source=MeshyEquipmentModel.Load("Ammo_Arrow");
                    if(source){arrow=Instantiate(source,mount,false).transform;arrowAxis=source.transform.localRotation;arrowSourceScale=source.transform.localScale;}
                }
                if(name.StartsWith("Tool_Pickaxe_")||name.StartsWith("Tool_Axe_")||name.StartsWith("Tool_Hammer_")||name.Contains("Quakehammer"))
                {
                    // Imported handles span -0.20 (lead grip) to +0.10 (lower grip).
                    // Fit their actual usable span to the original animation's hand spacing.
                    float lead=name.Contains("Quakehammer")?.30f:name.StartsWith("Tool_Hammer_")?.12f:.20f;
                    float span=name.Contains("Quakehammer")?.46f:name.StartsWith("Tool_Hammer_")?.19f:.30f;
                    float worldScale=(Mathf.Max(handSpan,Vector3.Distance(Palm("Left"),Palm("Right")))+.035f)/span;
                    float localScale=worldScale/Mathf.Max(.001f,mount.lossyScale.x);
                    // Lengthen the shaft without enlarging its thickness or the head width.
                    item.transform.localScale=new Vector3(1f/Mathf.Max(.001f,mount.lossyScale.x),localScale,1f/Mathf.Max(.001f,mount.lossyScale.x));
                    item.transform.localPosition=Vector3.forward*(lead*localScale);
                }
                foreach(var collider in item.GetComponentsInChildren<Collider>())Destroy(collider);
            }
            if(!mount||bones.Count==0)return;
            if(held.Contains("Bow")&&motion=="BowShoot")
                BowHeadClearance=MeshyNativeBow.ClearHead(bones,transform.up,transform.right);
            bool twoHand=(held.StartsWith("Tool_")||held.Contains("Quakehammer"))&&(motion=="Tool_Mine"||motion=="Tool_Chop"||motion=="Tool_Hammer"||motion=="SmithRepair");
            bool left=held.Contains("Bow")||held.Contains("Shield");
            string side=left?"Left":"Right";var wrist=bones["mixamorig:"+side+"Hand"];
            var primary=Palm(side);mount.position=primary;
            if(twoHand)
            {
                // Work tools share the approved end-for-end orientation: right hand nearest the head.
                var support=Palm("Left");var shaft=support-primary;
                if(shaft.sqrMagnitude>.00001f)
                {
                    var facing=Vector3.ProjectOnPlane(transform.forward,shaft.normalized);
                    if(facing.sqrMagnitude<.00001f)facing=Vector3.ProjectOnPlane(transform.right,shaft.normalized);
                    // The pick head extends along X: keep its cutting plane in the swing plane.
                    mount.rotation=Quaternion.LookRotation(shaft.normalized,Vector3.Cross(shaft.normalized,facing.normalized));
                    SupportShaftError=Vector3.Cross(support-primary,mount.forward).magnitude;
                    FingerContactError=Mathf.Max(fingers.Wrap(left,primary,mount.forward,.012f),fingers.Wrap(!left,support,mount.forward,.012f));
                }
            }
            else
            {
                var across=bones["mixamorig:"+side+"HandIndex1"].position-bones["mixamorig:"+side+"HandPinky1"].position;
                var alongFingers=bones["mixamorig:"+side+"HandMiddle1"].position-wrist.position;
                mount.rotation=held.Contains("Bow")
                    ?Quaternion.LookRotation(across.normalized,Vector3.ProjectOnPlane(transform.forward,across.normalized).normalized)
                    :Quaternion.LookRotation(-across.normalized,Vector3.Cross(across,alongFingers).normalized);
                SupportShaftError=0;
                if(held.Contains("Bow"))FingerContactError=fingers.Wrap(true,primary,mount.forward,.009f);
                else if(held.Contains("Sword")||held.Contains("Katana")||held.Contains("Shield"))
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
