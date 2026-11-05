using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
using System.Collections.Generic;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

public static class FunPriority1105Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static string log; static int pass, fail;
    static void Check(bool ok, string label) { if(ok) pass++; else fail++; log += (ok ? "PASS " : "FAIL ") + label + "\n"; }
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target,args);
    static async Task CheckCliff(LocalIncursions local)
    {
        var buildings=SceneObjects.All<BuildingBase>().Where(b=>b!=null && b.gameObject.activeInHierarchy).ToArray();
        var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.Hut,UnitRole.Worker});
        var sources=new List<NavMeshBuildSource>();
        void Ground(float x,float width) => sources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Box, size=new Vector3(width,1,20), transform=Matrix4x4.TRS(new Vector3(x,-.5f,1000),Quaternion.identity,Vector3.one), area=0 });
        Ground(1000,20); Ground(1037.5f,35);
        var data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),sources,new Bounds(new Vector3(1020,0,1000),new Vector3(100,20,100)),Vector3.zero,Quaternion.identity);
        var handle=NavMesh.AddNavMeshData(data); GameObject home=null;
        try
        {
            foreach(var b in buildings) b.gameObject.SetActive(false);
            home=UnityEngine.Object.Instantiate(template,new Vector3(1000,0,1000),Quaternion.identity); home.SetActive(true);
            await Task.Delay(200);
            NavMesh.SamplePosition(new Vector3(1045,0,1000),out var from,8,NavMesh.AllAreas); NavMesh.SamplePosition(new Vector3(1000,0,1000),out var to,10,NavMesh.AllAreas);
            var path=new NavMeshPath(); NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path); log += $"TERRAIN {path.status} from={from.position} to={to.position} corners={string.Join(";",path.corners.Select(p=>p.ToString()))}\n";
            Check(path.status==NavMeshPathStatus.PathPartial && path.corners.Length>0 && Vector3.Distance(path.corners.Last(),new Vector3(1000,0,1000))<30,"reproduced disconnected terrain ending within old 30m allowance");
            var spawned=false;
            for(int i=0;i<15 && !spawned;i++) spawned=local.TrySpawnWaveGroup(1,false);
            Check(!spawned,"cliff with no destructible barrier cannot accept a stranded raider");
            foreach(var v in local.Visitors.ToArray()) if(v!=null) v.Wound(1e9f);
        }
        finally
        {
            if(home!=null) UnityEngine.Object.Destroy(home);
            foreach(var b in buildings) if(b!=null) b.gameObject.SetActive(true);
            handle.Remove(); UnityEngine.Object.Destroy(data);
        }
    }
    public static async Task<string> Main()
    {
        if(!Application.isPlaying) throw new Exception("Play required");
        log=""; pass=fail=0;
        var settings=UserSettings.Current.Clone(); var root=SaveStorage.RootOverride;
        SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"FunPriority1105-"+Guid.NewGuid().ToString("N"));
        try
        {
            var quiet=settings.Clone(); quiet.autoSaveEnabled=quiet.autoPauseThreat=quiet.autoPauseDowned=false; UserSettings.Apply(quiet,false);
            SaveSystem.NewGame(new NewGameOptions { seed=261102, mapSize=MapSize.Small, startingCount=3,
                startingTeam=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray() });
            var deadline=DateTime.UtcNow.AddSeconds(60);
            while(SaveSystem.Busy && DateTime.UtcNow<deadline) await Task.Delay(50);
            if(SaveSystem.Busy) throw new Exception("NewGame did not complete");
            GameMenuController.Instance.Resume(); Time.timeScale=0;
            var w=WaveDefense.Instance; var local=SceneObjects.Any<LocalIncursions>();
            Check(w!=null && local!=null,"wave components present");
            var frame=Time.frameCount; await Task.Delay(150); Check(Time.frameCount>frame,"editor frames advance");
            Check(local.TrySpawnWaveGroup(1,false),"first wave can spawn before player builds a completed building");
            foreach(var v in local.Visitors.ToArray()) if(v!=null) v.Wound(1e9f);
            w.StartNow();
            var state=(WaveDefense.State)typeof(WaveDefense).GetField("s",Hidden).GetValue(w);
            var initial=state.pending;
            var field=typeof(LocalIncursions).GetField("raiderTemplate",Hidden); var template=field.GetValue(local);
            try { field.SetValue(local,null); for(int i=0;i<35;i++) Call(w,"SpawnGroup"); }
            finally { field.SetValue(local,template); }
            Check(state.pending==initial,"failed spawns preserve pending enemies");
            state.sinceStart=6; typeof(WaveDefense).GetField("groupTimer",Hidden).SetValue(w,10f); Call(w,"Update");
            Check(w.InWave,"missing spawns cannot grant victory");
            var json=JsonUtility.ToJson(w.Capture()); w.Restore(JsonUtility.FromJson<WaveDefense.State>(json));
            state=(WaveDefense.State)typeof(WaveDefense).GetField("s",Hidden).GetValue(w);
            Check(w.InWave && state.pending==initial,"JSON roundtrip preserves unfinished spawn state");
            Call(w,"SpawnGroup");
            Check(state.pending<initial && state.failures==0,"spawning resumes when obstruction is resolved");
            foreach(var v in local.Visitors.ToArray()) if(v!=null) v.Wound(1e9f);
            state.pending=0; state.sinceStart=6; Call(w,"Update");
            Check(!w.InWave && Mathf.Approximately(w.PeaceLeft,WaveDefense.Peace),"normal completion still starts four-day peace");
            await CheckCliff(local);
            var result=$"PASS {pass} FAIL {fail}\n"+log;
            File.WriteAllText("Temp/fun-priority-checks.txt",result); return result;
        }
        finally { Time.timeScale=0; UserSettings.Apply(settings,false); SaveStorage.RootOverride=root; }
    }
}
