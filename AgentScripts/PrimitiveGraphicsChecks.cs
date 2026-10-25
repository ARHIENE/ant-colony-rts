using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
public static class PrimitiveGraphicsChecks
{
    static int n;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); n++; }
    public static async Task<string> Main()
    {
        n = 0; var root = new GameObject("Primitive graphics checks"); var scale=Time.timeScale; Time.timeScale=0;
        try
        {
            var home=AntColony.World.WorldMapManager.Instance.HomePosition;
            foreach(var kind in new[]{AntColony.World.EventActorKind.Wasp,AntColony.World.EventActorKind.NightPredator,AntColony.World.EventActorKind.Wanderer})
            {
                var actor=AntColony.World.EventActor.Spawn(new AntColony.World.EventActor.State{kind=kind,position=new AntColony.Save.Vec3Dto(home),remaining=100,amount=100});
                actor.transform.SetParent(root.transform,true); await Task.Yield(); await Task.Yield();
                var anim=AntColony.Units.RigAnimator.Find(actor.gameObject);
                Check(anim!=null && anim.avatar.isValid,"event model rig "+kind);
                Check(!actor.GetComponent<Renderer>().enabled || actor.GetComponent<Renderer>().forceRenderingOff,"event primitive hidden "+kind);
                anim.cullingMode=AnimatorCullingMode.AlwaysAnimate; var clip=kind==AntColony.World.EventActorKind.Wasp?"Fly":"Walk";
                anim.Play("Base Layer."+clip,0,0); anim.Update(.1f);
                var bones=anim.GetComponentsInChildren<Transform>(); var before=bones.Select(b=>b.localRotation).ToArray(); anim.Update(.55f);
                Check(bones.Where((b,i)=>Quaternion.Angle(before[i],b.localRotation)>.1f).Any(),"event bones animate "+kind);
            }
            var site=GameObject.CreatePrimitive(PrimitiveType.Cube); site.transform.SetParent(root.transform); site.transform.position=home; site.transform.localScale=new Vector3(2.5f,.2f,2.5f);
            site.AddComponent<AntColony.Buildings.BuildingConstructionSite>().Initialize(null,30); await Task.Yield(); await Task.Yield();
            Check(site.GetComponent<Renderer>().forceRenderingOff && site.transform.childCount>0,"construction plan model");
            var c=AntColony.World.Corpse.Spawn(new AntColony.World.Corpse.State{kind=AntColony.World.CorpseKind.ColonyCommander,name="Probe",position=home}); c.transform.SetParent(root.transform);
            Check(AntColony.Units.RigAnimator.Find(c.gameObject)!=null,"restored commander corpse model");
            foreach(var id in new[]{"CC_ConstructionPlan","CC_DemolitionRubble","CC_OreOutcrop"})
                Check(Resources.Load<GameObject>("ResourceModels/"+id)!=null,"authored prop imported "+id);
            return "PASS "+n+" event/construction/corpse graphics checks";
        }
        finally { Time.timeScale=scale; Object.Destroy(root); }
    }
}
