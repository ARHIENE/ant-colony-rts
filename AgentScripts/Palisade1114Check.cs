using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
using AntColony.Buildings;
using AntColony.UI;
public static class Palisade1114Check
{
    public static async Task<string> Run()
    {
        var c=CommanderRoster.Instance.Commanders.First(x=>x&&x.IsColonyMember);
        foreach(var n in UnityEngine.Object.FindObjectsByType<AntColony.World.ResourceNode>(FindObjectsInactive.Exclude)) if(n.WorkRequested)n.CancelWork();
        foreach(var hero in CommanderRoster.Instance.Commanders.Where(x=>x&&x.IsColonyMember)) hero.CommandStop();
        typeof(WorkerAnt).GetMethod("DropCargo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);
        c.PersonalState.toolTier=2;
        var pc=UnityEngine.Object.FindAnyObjectByType<BuildingPlacementController>();
        if(!pc.BeginPlacement(AntColony.Data.BuildingKind.Palisade,AntColony.Data.UnitRole.Worker,null))throw new Exception("Cannot begin fixture storage placement");
        var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{pc.PendingKind,AntColony.Data.UnitRole.Worker});
        bool placed=false;
        for(int radius=4;radius<20&&!placed;radius+=2)for(int i=0;i<16&&!placed;i++)
        {
            var desired=c.Position+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*radius;
            if(!UnityEngine.AI.NavMesh.SamplePosition(desired,out var hit,1,UnityEngine.AI.NavMesh.AllAreas))continue;
            var pos=(Vector3)typeof(BuildingPlacementController).GetMethod("GetPlacementPosition",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pc,new object[]{template,hit.position});
            if(!(bool)typeof(BuildingPlacementController).GetMethod("CellValid",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pc,new object[]{pos}))continue;
            placed=pc.PlaceLine(new System.Collections.Generic.List<Vector3>{pos},new System.Collections.Generic.List<bool>{true})>0;
        }
        if(!placed)throw new Exception("No valid fixture storage position");
        await Task.Delay(200);
        var site=UnityEngine.Object.FindObjectsByType<BuildingConstructionSite>(FindObjectsInactive.Exclude).OrderBy(s=>(s.Position-c.Position).sqrMagnitude).First();
        foreach(var n in site.Needs){n.delivered=n.need;n.reserved=0;}
        c.PersonalState.meal.satiety=100; c.PersonalState.sleep.fatigue=0; GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds,0);
        var sitePosition=site.Position;
        c.CommandBuild(site);
        SimulationPause.UserSpeed(1);
        var log=new StringBuilder(); bool started=false; int changes=0; Vector3 last=Vector3.positiveInfinity;
        for(int i=0;i<100;i++)
        {
            await Task.Delay(200);
            if(c.IsBuildingAnimation)
            {
                started=true;
                if(Vector3.Distance(last,c.Position)>.5f){changes++;last=c.Position;log.AppendLine("work point "+changes+" "+last+" source="+c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion+" remaining="+(site ? site.RemainingWork : 0));}
            }
        }
        SimulationPause.UserSpeed(0);
        log.AppendLine("started="+started+" distinctWorkPoints="+changes+" target="+c.ConstructionTarget+" state="+c.GetComponent<AntVisual>().StateName);
        var wall=UnityEngine.Object.FindObjectsByType<Wall>(FindObjectsInactive.Exclude).FirstOrDefault(w=>w.Data.kind==AntColony.Data.BuildingKind.Palisade);
        if(!wall)throw new Exception("Palisade construction not completed: "+log);
        log.AppendLine("PASS completed palisade; material="+wall.MainMaterial+" model="+wall.GetComponent<FacilityVisual>()?.Model?.name+" obstacle="+wall.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving);
        var ctl=UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();if(ctl)ctl.enabled=false;
        Camera.main.orthographicSize=3; Camera.main.transform.position=sitePosition+new Vector3(4,3,-5); Camera.main.transform.LookAt(sitePosition+Vector3.up);
        ScreenCapture.CaptureScreenshot("work/furniture-wall-1114/palisade.png"); await Task.Delay(300);
        System.IO.File.WriteAllText("work/furniture-wall-1114/palisade-check.txt",log.ToString());
        return log.ToString();
    }
}
