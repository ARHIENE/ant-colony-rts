using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

// Disposable transition fixture, not a claim of a naturally completed economy/campaign.
public static class CampaignComplete1115Checks
{
    const string Dir="work/campaign-complete-1115/";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] public class RestoreData { public UserSettingsData settings; public string root; public bool active; }
    static int count;
    static void Check(bool ok,string label) { File.AppendAllText(Dir+"checks.txt",(ok?"PASS ":"FAIL ")+label+"\n"); if(!ok) throw new Exception(label); count++; }
    static async Task Ready()
    {
        UnityEditor.EditorApplication.isPaused=false;
        var end=DateTime.UtcNow.AddSeconds(90);
        while((SaveSystem.Busy || !GameSession.Instance.GameStarted || CommanderRoster.Instance == null || CommanderRoster.Instance.Commanders.Count < 3) && DateTime.UtcNow<end) await Task.Delay(100);
        if(SaveSystem.Busy || CommanderRoster.Instance.Commanders.Count < 3) throw new Exception("Scene load timeout");
        SimulationPause.UserSpeed(0);
    }
    static async Task Capture(string name)
    { if(UnityEditor.EditorApplication.isPaused) UnityEditor.EditorApplication.Step(); await Task.Delay(300); UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Repaint(); ScreenCapture.CaptureScreenshot(Dir+name+".png"); await Task.Delay(500); }
    public static async Task<string> Screen() { await Capture("02b-mothership-live"); var cam=UnityEngine.Camera.main; return Inspect()+$" camera={cam.transform.position} near/far={cam.nearClipPlane}/{cam.farClipPlane} mask={cam.cullingMask} boss={RocketCampaign.Instance.MothershipBoss?.Position} crew={Crew.Position} decks="+string.Join(";",UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include).Where(r=>r.name.Contains("갑판")).Select(r=>$"{r.name} active={r.gameObject.activeInHierarchy} enabled={r.enabled} position={r.transform.position} layer={r.gameObject.layer} scale={r.transform.lossyScale}")); }
    static void Move(CommanderAnt c,Vector3 point)
    {
        c.CommandStop(); Check(NavMesh.SamplePosition(point,out var hit,5,NavMesh.AllAreas),"Fixture position on NavMesh");
        Check(c.Agent.Warp(hit.position),"Fixture checkpoint placement");
    }
    static CommanderAnt Crew => CommanderRoster.Instance.Commanders.First(c=>RocketCampaign.AwayOnMothership(c));
    public static string Inspect() => $"playing={Application.isPlaying} busy={SaveSystem.Busy} started={GameSession.Instance.GameStarted} roster={CommanderRoster.Instance?.Commanders.Count} scale={Time.timeScale} frame={Time.frameCount} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";
    public static async Task<string> LoadCheckpoint()
    {
        var initialDeadline=DateTime.UtcNow.AddSeconds(90);
        while(SaveSystem.Busy && DateTime.UtcNow<initialDeadline) await Task.Delay(100);
        if(SaveSystem.Busy) throw new Exception("Initial load still busy");
        var old=new RestoreData{settings=UserSettings.Current.Clone(),root=SaveStorage.RootOverride,active=true};
        File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(old,true));
        SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CampaignComplete1115-final-"+Guid.NewGuid().ToString("N"));
        var settings=old.settings.Clone();settings.autoSaveEnabled=settings.autoPauseThreat=settings.autoPauseDowned=false;UserSettings.Apply(settings,false);
        UnityEditor.EditorApplication.isPaused=false;
        Check(SaveSystem.TryLoad(Path.GetFullPath(Dir+"mothership-save.json"),out var error),"Final-code checkpoint load accepted: "+error);
        return "Load requested; run Reloaded after scene initialization";
    }
    public static string ReadyNow()
    {
        SimulationPause.UserSpeed(0);
        Check(!SaveSystem.Busy && GameSession.Instance.GameStarted,"New game initialization completed");
        Check(CommanderRoster.Instance.Commanders.Count==3,"Three starting commanders spawned");
        foreach(var c in CommanderRoster.Instance.Commanders) {c.CommandStop();c.SetJobEnabled(CommanderJobs.All,false);}
        Check(!RocketCampaign.Instance.Mothership.reached && !CampaignResearch.Instance.Departed,"Fresh campaign has not reached ending");
        return Inspect();
    }
    public static async Task<string> Prepare()
    {
        if(!Application.isPlaying) throw new Exception("Play required");
        UnityEditor.EditorApplication.isPaused=false;
        var initialDeadline=DateTime.UtcNow.AddSeconds(90);
        while(SaveSystem.Busy && DateTime.UtcNow<initialDeadline) await Task.Delay(100);
        if(SaveSystem.Busy) throw new Exception("Initial menu still loading");
        Directory.CreateDirectory(Dir);
        if(File.Exists(Dir+"restore.json") && JsonUtility.FromJson<RestoreData>(File.ReadAllText(Dir+"restore.json")).active) throw new Exception("Finish previous fixture first");
        var old=new RestoreData{settings=UserSettings.Current.Clone(),root=SaveStorage.RootOverride,active=true};
        File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(old,true));
        SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CampaignComplete1115-"+Guid.NewGuid().ToString("N"));
        var settings=old.settings.Clone(); settings.autoSaveEnabled=settings.autoPauseThreat=settings.autoPauseDowned=false; UserSettings.Apply(settings,false);
        File.WriteAllText(Dir+"checks.txt","TRANSITION FIXTURE: supplied late-game rocket state; not natural economy playthrough.\n");
        var team=Enumerable.Range(0,3).Select(StartingCandidate.Roll).ToArray();
        SaveSystem.NewGame(new NewGameOptions{mapSize=MapSize.Small,biome=MapBiome.Forest,seed=261114,startingCount=3,startingTeam=team});
        await Ready(); GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0);
        Check(!RocketCampaign.Instance.Mothership.reached && !CampaignResearch.Instance.Departed,"New game starts before mothership and victory");
        Check(!ExpeditionRules.Paused,"Expedition entry available");
        Check(CommanderRoster.Instance.Commanders.Count==3,"Three starting commanders spawned");
        foreach(var c in CommanderRoster.Instance.Commanders) {c.CommandStop(); c.SetJobEnabled(CommanderJobs.All,false);}
        await Capture("01-new-game");
        return "Isolated fresh game ready";
    }
    public static async Task<string> Launch()
    {
        Check((SaveStorage.RootOverride??"").Contains("CampaignComplete1115"),"Disposable fixture active");
        var c=CommanderRoster.Instance.Commanders[0];
        var template=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{BuildingKind.AirshipYard,UnitRole.Worker});
        var yard=UnityEngine.Object.Instantiate(template,c.Position+Vector3.left*10,Quaternion.identity).GetComponent<AirshipYard>();
        yard.name="AirshipYard"; yard.gameObject.SetActive(true);
        yard.RestoreState(new AirshipYard.State{hull=true,engine=true,phase=RocketPhase.Boarding,fuelType=(int)ResourceType.Petroleum,fuelUnits=15,snapshot=new RocketSnapshot{taken=true,currentPopulation=3,averagePopulation=3}},CommanderRoster.Instance.Commanders);
        Move(c,yard.Position+Vector3.right*4);
        Check(!yard.TryDepart(),"Empty rocket cannot launch");
        Check(yard.TryBoard(c),"Prepared rocket accepts nearby commander");
        Check(yard.TryDepart(),"Launch request accepted");
        typeof(AirshipYard).GetMethod("ResolveLaunch",Hidden).Invoke(yard,null);
        Check(yard.Phase==RocketPhase.Launched && RocketCampaign.AwayOnMothership(c),"Launch enters mothership");
        Check(!CampaignResearch.Instance.Departed,"Launch does not grant victory");
        Check(!c.IsEmbarked && c.Agent.isOnNavMesh,"Arriving commander can walk on deck");
        Check(!RocketCampaign.Instance.ReturnFromMothership(),"Cannot return before physical boarding");
        Check(RocketCampaign.Instance.MothershipBoss!=null && !RocketCampaign.Instance.MothershipBoss.IsDead,"Living final boss present");
        Check(!RocketCampaign.ShipBounds.Contains(RocketCampaign.Instance.Home),"Home and mothership remain separate");
        await Capture("02-mothership-arrival");
        return "Launch transition passed; ready for combat and return";
    }
    public static async Task<string> Gate()
    {
        var world=WorldMapManager.Instance; var c=CommanderRoster.Instance.Commanders[1];
        var origin=c.Position; var party=world.CreateWalkingParty(new[]{c});
        Check(party!=null,"Walking party has a reachable home exit");
        Check(Vector3.Distance(origin,party.Position)>3,"Party assembles at map edge, not current commander position");
        var escort=CommanderRoster.Instance.Commanders[2];
        Check(party.AssignEscorts(new[]{escort}),"Home commander accepts escort assignment");
        Check(!escort.IsEmbarked && !escort.IsAwayFromHome,"Escort stays physically on home map");
        ExpeditionTransport.CancelCrewOrdersFor(escort);
        Check(party.Escorts.Contains(escort),"Direct combat orders preserve escort membership");
        var destination=world.Sites.First(WorldMapManager.SiteRevealed);
        Check(!party.TryDepart(destination),"Cannot depart while walking to home exit");
        UnityEditor.EditorApplication.isPaused=false; SimulationPause.UserSpeed(3);
        var end=DateTime.UtcNow.AddSeconds(60);
        while(party.PendingCrewOrderCount>0 && DateTime.UtcNow<end) await Task.Delay(100);
        SimulationPause.UserSpeed(0);
        UnityEditor.EditorApplication.isPaused=true;
        Check(c.IsEmbarked && party.PendingCrewOrderCount==0,"Commander physically reached departure gate");
        Check(party.TryDepart(destination),"Gate arrival allows expedition departure");
        Check(party.EscortReturning,"Departure orders remaining escort to return");
        Check(SaveSystem.TrySave(false,2,out var escortError),"Save returning escort: "+escortError);
        party.Tick(party.TravelRemainingSeconds+.1f);
        Check(party.State==ExpeditionState.Deployed,"Travel connects to destination battlefield");
        await Capture("01b-expedition-gate");
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,2),out var loadError),"Reload departing party and returning escort: "+loadError);
        await Ready();
        Check(WorldMapManager.Instance.Transports.Any(t=>t.EscortReturning && t.Escorts.Any()),"Escort membership and return order survive full scene reload");
        return "Walking gate and expedition destination transition passed";
    }
    public static async Task<string> RoundTrip()
    {
        var campaign=RocketCampaign.Instance; var c=Crew; var boss=campaign.MothershipBoss;
        boss.TakeDamage(37); float hp=boss.CurrentHp, personal=c.PersonalHealth;
        Move(c,RocketCampaign.ShipLanding+Vector3.forward*9);
        Check(campaign.RequestMothershipBoarding(),"Return issues walking order");
        Check(!c.IsEmbarked && !campaign.ReturnFromMothership(),"Distant commander not instantly boarded");
        UnityEditor.EditorApplication.isPaused=false; SimulationPause.UserSpeed(1);
        var end=DateTime.UtcNow.AddSeconds(35);
        while(!c.IsEmbarked && DateTime.UtcNow<end) await Task.Delay(100);
        SimulationPause.UserSpeed(0); UnityEditor.EditorApplication.isPaused=true;
        Check(c.IsEmbarked,"Commander physically walked to landing craft");
        Check(campaign.ReturnFromMothership(),"Boarded crew return to Earth");
        Check(!RocketCampaign.AwayOnMothership(c) && !c.IsEmbarked,"Returned crew resume home simulation");
        Check(Mathf.Abs(c.PersonalHealth-personal)<.1f,"Return does not grant personal healing");
        var yard=SceneObjects.All<AirshipYard>().First(y=>y.Phase==RocketPhase.Boarding);
        Move(c,yard.Position+Vector3.right*4); Check(yard.TryBoard(c) && yard.TryDepart(),"Same rocket supports re-entry");
        typeof(AirshipYard).GetMethod("ResolveLaunch",Hidden).Invoke(yard,null);
        Check(RocketCampaign.AwayOnMothership(c) && Mathf.Abs(boss.CurrentHp-hp)<.1f,"Re-entry preserves boss damage");
        Check(!CampaignResearch.Instance.Departed,"Round trip still not victory");
        var saved=SaveSnapshot.Capture(); Check(SaveValidator.Validate(saved,out var error),"Mothership save validates: "+error);
        Check(SaveSystem.TrySave(false,0,out error),"Save mothership checkpoint: "+error);
        File.Copy(SaveSlots.PathFor(false,0),Dir+"mothership-save.json",true);
        Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload mothership checkpoint: "+error); await Ready();
        campaign=RocketCampaign.Instance; c=Crew;
        Check(Mathf.Abs(campaign.MothershipBoss.CurrentHp-hp)<.1f,"Boss damage survives full scene reload");
        Check(c.Agent.isOnNavMesh && RocketCampaign.ShipBounds.Contains(c.Position),"Reloaded commander stands on mothership navigation");
        await Capture("03-reloaded-mothership");
        return "Physical return, re-entry and save/reload passed";
    }
    public static async Task<string> Victory()
    {
        var campaign=RocketCampaign.Instance; var boss=campaign.MothershipBoss;
        Check(!CampaignResearch.Instance.Departed,"Living boss prevents victory");
        boss.TakeDamage(boss.CurrentHp); await Task.Delay(200);
        Check(campaign.MothershipVictory && CampaignResearch.Instance.Departed,"Final boss death grants victory");
        Check(Time.timeScale==0,"Victory pauses simulation");
        GameMenuController.Instance.Resume(); Check(Time.timeScale==0,"Completed campaign cannot resume");
        Check(SaveSystem.TrySave(false,1,out var error),"Victory save: "+error);
        File.Copy(SaveSlots.PathFor(false,1),Dir+"victory-save.json",true);
        await Capture("04-victory");
        return "PASS "+count+" transition checks (checkpoint fixture, not natural campaign balance)";
    }
    public static async Task<string> Reloaded()
    {
        SimulationPause.UserSpeed(0); UnityEditor.EditorApplication.isPaused=true;
        Check(!SaveSystem.Busy,"Reload completed");
        var saved=JsonUtility.FromJson<SaveFileV1>(File.ReadAllText(Dir+"mothership-save.json"));
        Check(Mathf.Abs(RocketCampaign.Instance.MothershipBoss.CurrentHp-saved.rocket.mothership.bossHp)<.1f,"Boss damage survives full scene reload");
        Check(Crew.Agent.isOnNavMesh && RocketCampaign.ShipBounds.Contains(Crew.Position),"Reloaded commander stands on mothership navigation");
        var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
        typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(camera,new object[]{new Vector3(-5000,0,5000),0f,24f});
        await Capture("03-reloaded-mothership"); return "Reload and temporary camera framing checked";
    }
    public static async Task<string> Combat()
    {
        var boss=RocketCampaign.Instance.MothershipBoss;var c=Crew;
        Check(boss.GetComponentsInChildren<Renderer>().Length>0,"Boss has visible body");
        var target=boss.GetComponentsInChildren<Collider>().First(x=>x.enabled && x.gameObject.activeInHierarchy);
        var cam=UnityEngine.Camera.main;var ray=cam.ScreenPointToRay(cam.WorldToScreenPoint(target.bounds.center));
        Check(Physics.RaycastAll(ray,1000).Any(h=>h.collider.GetComponentInParent<AntColony.Boss.BossHealth>()==boss),"Boss body is clickable through combat raycast");
        Move(c,boss.Position+Vector3.back*5);
        var view=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();
        view.ApplySettings(25,0); typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",Hidden).Invoke(view,new object[]{(boss.Position+c.Position)*.5f+Vector3.up*3,0f,14f});
        float before=boss.CurrentHp;
        c.CommandAttack(boss);
        UnityEditor.EditorApplication.isPaused=false;SimulationPause.UserSpeed(1);
        var end=DateTime.UtcNow.AddSeconds(20);
        while(boss.CurrentHp>=before && !c.IsDead && DateTime.UtcNow<end) await Task.Delay(100);
        SimulationPause.UserSpeed(0);UnityEditor.EditorApplication.isPaused=true;
        Check(boss.CurrentHp<before,"Normal commander attack damages final boss");
        var bossView=cam.WorldToViewportPoint(boss.Position+Vector3.up*2);
        var crewView=cam.WorldToViewportPoint(c.Position+Vector3.up);
        Check(bossView.z>0 && bossView.x>.05f && bossView.x<.95f && bossView.y>.05f && bossView.y<.85f,"Boss is visible below HUD inside mothership camera bounds");
        Check(crewView.z>0 && crewView.x>.05f && crewView.x<.95f && crewView.y>.05f && crewView.y<.85f,"Fighting commander is visible inside mothership camera bounds");
        await Capture("03b-live-combat");return $"Actual attack damage {before-boss.CurrentHp:0.0}";
    }

    public static string Main()
    {
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir+"run.txt","RUNNING\n");
        _ = RunAll(); return "Started; inspect work/campaign-complete-1115/run.txt";
    }
    public static string Regression()
    {
        File.AppendAllText(Dir+"run.txt","LATEST INDUSTRY/DISCOVERY REGRESSION\n");
        _=RunRegression();return "Started saved campaign regression; original settings restored in finally";
    }
    static async Task RunRegression()
    {
        try { await LoadCheckpoint(); await Ready(); await Reloaded(); await Combat(); await FinalChecks(); }
        catch(Exception e){File.AppendAllText(Dir+"run.txt","FAIL latest regression "+e+"\n");}
        finally {Finish();UnityEditor.EditorApplication.isPaused=false;File.AppendAllText(Dir+"run.txt","RESTORED latest regression\n");}
    }
    static async Task RunAll()
    {
        try
        {
            await Prepare(); await Launch(); await Gate(); await RoundTrip(); await Combat();
            await FinalChecks();
        }
        catch(Exception e) { File.AppendAllText(Dir+"run.txt","FAIL "+e+"\n"); }
        finally { Finish(); File.AppendAllText(Dir+"run.txt","RESTORED\n"); }
    }
    public static string ContinueFinal()
    {
        var old=new RestoreData{settings=UserSettings.Current.Clone(),root=SaveStorage.RootOverride,active=true};
        File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(old,true));
        SaveStorage.RootOverride=Path.Combine(Application.temporaryCachePath,"CampaignComplete1115-final-"+Guid.NewGuid().ToString("N"));
        _=FinishChecks(); return "Final checks resumed in isolated save root";
    }
    static async Task FinishChecks()
    {
        try { await FinalChecks(); }
        catch(Exception e) { File.AppendAllText(Dir+"run.txt","FAIL final "+e+"\n"); }
        finally { Finish(); File.AppendAllText(Dir+"run.txt","RESTORED final\n"); }
    }
    static async Task FinalChecks()
    {
            var boss=RocketCampaign.Instance.MothershipBoss;
            var encounter=boss.GetComponent<AntColony.Boss.MothershipEncounter>();
            Check(encounter!=null,"Dedicated final encounter attached");
            if(encounter.Capture().casting<0) encounter.Tick(10);
            var warning=encounter.Capture();
            Check(warning.casting>=0,"Final boss starts a telegraphed attack");
            Check(AntColony.Boss.MothershipEncounter.Contains(warning,warning.origin),"Warning center matches damage region");
            Check(!AntColony.Boss.MothershipEncounter.Contains(warning,warning.origin+Vector3.right*100),"Outside warning is safe");
            Check(SaveSystem.TrySave(false,0,out var error),"Save active final-boss warning: "+error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false,0),out error),"Reload active warning: "+error);
            await Ready();
            var restored=RocketCampaign.Instance.MothershipBoss.GetComponent<AntColony.Boss.MothershipEncounter>().Capture();
            Check(restored.casting==warning.casting && Mathf.Abs(restored.remaining-warning.remaining)<.01f,"Warning timing survives scene reload");
            await Capture("05-final-encounter");
            await Victory(); File.AppendAllText(Dir+"run.txt","PASS all stages\n");
    }
    public static string Finish()
    {
        if(!File.Exists(Dir+"restore.json")) return "No fixture";
        var old=JsonUtility.FromJson<RestoreData>(File.ReadAllText(Dir+"restore.json"));
        if(!old.active)return "Already restored";
        SimulationPause.UserSpeed(0); SaveStorage.RootOverride=old.root; UserSettings.Apply(old.settings,false);
        old.active=false;File.WriteAllText(Dir+"restore.json",JsonUtility.ToJson(old,true));return "User settings and save root restored";
    }
}

