using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.Units
{
    // Equipment follows the authored hands. This component never replaces the arm trajectory.
    public sealed class MeshyNativeEquipment : MonoBehaviour
    {
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        Transform mount;
        GameObject item;
        Transform arrow;
        Quaternion arrowAxis;
        string held;
        float handSpan;
        MeshyNativeFingers fingers;
        public float FingerContactError {get;private set;}
        bool rebuild;
        public void SetHandSpan(float span)
        {
            if(Mathf.Abs(span-handSpan)<.0001f)return;
            handSpan=span;rebuild=true;
        }
        public float SupportShaftError { get; private set; }
        // Pickaxe geometry. World metres: lead palm to head centre, spare handle past the support hand, head centre to a point.
        // Item units (imported mesh along its shaft): origin to head centre, origin to handle end.
        public const float PickHeadDistance=.36f,PickEndMargin=.05f,PickTipHalf=.213f,PickHeadItem=.28f,PickEndItem=.13f;
        // The pointed tip that leads the downswing and the head centre, from the current authored hand pose (no state is changed).
        public (Vector3 tip,Vector3 head) PickStrikePoints()
        {
            var lead=Palm("Left");var shaft=(Palm("Right")-lead).normalized;
            var head=lead-shaft*PickHeadDistance;
            // In the swing plane, perpendicular to the shaft; the body-side point travels along its own direction on the way down.
            var across=Vector3.Cross(transform.right,shaft).normalized;
            return (head-across*PickTipHalf,head);
        }
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
                    if(source){arrow=Instantiate(source,mount,false).transform;arrowAxis=source.transform.localRotation;}
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
                    if(name.StartsWith("Tool_Pickaxe_"))
                    {
                        // Pickaxe: hold the handle near its end like a real pick, so the head swings out in front of the hands.
                        // Measured on the imported mesh (item units along the shaft): head centre at -0.28, handle end at +0.13.
                        // Only the shaft is lengthened; head width and thickness across the shaft keep their size.
                        float hands=Mathf.Max(handSpan,Vector3.Distance(Palm("Left"),Palm("Right")));
                        worldScale=(PickHeadDistance+hands+PickEndMargin)/(PickHeadItem+PickEndItem);
                        localScale=worldScale/Mathf.Max(.001f,mount.lossyScale.x);
                        item.transform.localScale=new Vector3(1f/Mathf.Max(.001f,mount.lossyScale.x),localScale,1f/Mathf.Max(.001f,mount.lossyScale.x));
                        item.transform.localPosition=Vector3.forward*(PickHeadItem*localScale-PickHeadDistance/Mathf.Max(.001f,mount.lossyScale.x));
                    }
                }
                foreach(var collider in item.GetComponentsInChildren<Collider>())Destroy(collider);
            }
            if(!mount||bones.Count==0)return;
            bool twoHand=(held.StartsWith("Tool_")||held.Contains("Quakehammer"))&&(motion=="Tool_Mine"||motion=="Tool_Chop"||motion=="Tool_Hammer"||motion=="SmithRepair");
            bool left=twoHand||held.Contains("Bow")||held.Contains("Shield");
            string side=left?"Left":"Right";var wrist=bones["mixamorig:"+side+"Hand"];
            var primary=Palm(side);mount.position=primary;
            if(twoHand)
            {
                // The authored heavy swing leads with the left hand toward the strike.
                // Imported work-tool heads are on -Z; +Z runs down toward the support hand.
                var support=Palm("Right");var shaft=support-primary;
                if(shaft.sqrMagnitude>.00001f)
                {
                    var facing=Vector3.ProjectOnPlane(transform.forward,shaft.normalized);
                    if(facing.sqrMagnitude<.00001f)facing=Vector3.ProjectOnPlane(transform.right,shaft.normalized);
                    // The pick head extends along X: keep its cutting plane in the swing plane.
                    mount.rotation=Quaternion.LookRotation(shaft.normalized,Vector3.Cross(shaft.normalized,facing.normalized));
                    if(held.StartsWith("Tool_Pickaxe_"))
                    {
                        // The forward projection above degenerates when the shaft lines up with the facing direction (mid-swing),
                        // which spun the pick about its shaft. The swing is sagittal, so roll against the body's side axis instead:
                        // both points stay in the swing plane for the whole cycle.
                        var bodySide=Vector3.ProjectOnPlane(transform.right,shaft.normalized);
                        if(bodySide.sqrMagnitude>.00001f)mount.rotation=Quaternion.LookRotation(shaft.normalized,bodySide.normalized);
                    }
                    SupportShaftError=Vector3.Cross(support-primary,mount.forward).magnitude;
                    FingerContactError=Mathf.Max(fingers.Wrap(true,primary,mount.forward,.012f),fingers.Wrap(false,support,mount.forward,.012f));
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
                arrow.gameObject.SetActive(motion=="BowShoot"&&phase>=.12f&&phase<.73f);
                if(arrow.gameObject.activeSelf)
                {
                    var nock=Palm("Right");var aim=primary-nock;float length=(aim.magnitude+.08f)/.31f;
                    if(aim.sqrMagnitude>.00001f)
                    {
                        arrow.rotation=Quaternion.LookRotation(-aim.normalized,transform.up)*arrowAxis;
                        arrow.localScale=new Vector3(1,length,1)/Mathf.Max(.001f,mount.lossyScale.x);
                        arrow.position=nock+aim.normalized*(.15f*length);
                    }
                }
            }
        }
    }
}
