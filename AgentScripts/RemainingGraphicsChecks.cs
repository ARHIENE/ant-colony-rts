using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object=UnityEngine.Object;
public static class RemainingGraphicsChecks
{
    public static async Task<string> Main()
    {
        int n=0;void Check(bool ok,string s){if(!ok)throw new Exception("FAIL "+s);n++;}
        var root=new GameObject("Remaining graphics checks");var time=Time.timeScale;Time.timeScale=0;
        try
        {
            var home=AntColony.World.WorldMapManager.Instance.HomePosition;
            var civilian=GameObject.CreatePrimitive(PrimitiveType.Capsule);civilian.transform.SetParent(root.transform);civilian.transform.position=home+Vector3.up*.6f;civilian.transform.localScale=Vector3.one*.6f;
            var visual=civilian.AddComponent<AntColony.Units.CivilianVisual>();await Task.Yield();
            var anim=AntColony.Units.RigAnimator.Find(civilian);Check(anim!=null&&anim.avatar.isValid,"resident rig connected");Check(civilian.GetComponent<Renderer>().forceRenderingOff,"civilian capsule hidden");
            var meshes=civilian.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=meshes.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            Check(Mathf.Abs(bounds.size.y-1.4f)<.02f,"citizen height");Check(Mathf.Abs(bounds.min.y-home.y)<.02f,"citizen stands on ground");
            anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;anim.Play("Base Layer.Walk",0,0);anim.Update(.15f);var bones=anim.GetComponentsInChildren<Transform>();var before=bones.Select(t=>t.localRotation).ToArray();anim.Update(.23f);Check(bones.Where((t,i)=>Quaternion.Angle(before[i],t.localRotation)>.1f).Any(),"resident walk animates");
            var loot=AntColony.World.EquipmentLoot.Drop(home,new[]{new AntColony.Units.EquipmentItem()});loot.transform.SetParent(root.transform);await Task.Yield();await Task.Yield();Check(loot.GetComponent<Renderer>().forceRenderingOff&&loot.transform.childCount>0,"equipment loot basket");Check(loot.Items.Count==1,"loot items unchanged");
            var corpse=AntColony.World.Corpse.Spawn(new AntColony.World.Corpse.State{kind=AntColony.World.CorpseKind.Wildlife,name="Probe",position=home});corpse.transform.SetParent(root.transform);await Task.Yield();await Task.Yield();Check(corpse.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.name.Contains("Prop_Remains")),"wildlife remains model even while paused");
            var cargo=GameObject.CreatePrimitive(PrimitiveType.Cube);cargo.SetActive(false);cargo.transform.SetParent(root.transform);var node=cargo.AddComponent<AntColony.World.ResourceNode>();node.ConfigureLoot(AntColony.Data.ResourceType.Special,5);cargo.SetActive(true);await Task.Yield();Check(cargo.GetComponent<Renderer>().forceRenderingOff,"special boss resource model");Check(node.AmountRemaining==5,"special amount unchanged");
            return "PASS "+n+" citizen/loot/carcass graphics checks";
        }
        finally{Time.timeScale=time;Object.Destroy(root);}
    }
}
