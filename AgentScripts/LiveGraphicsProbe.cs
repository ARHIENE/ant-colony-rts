using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
public static class LiveGraphicsProbe
{
 public static async Task<string> Main()
 {
  var root=AntColony.Save.SaveStorage.RootOverride;var settings=AntColony.Core.UserSettings.Current.Clone();
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"LiveProbe-"+Guid.NewGuid().ToString("N"));
  try {
   var s=settings.Clone();s.autoSaveEnabled=false;s.autoPauseThreat=s.autoPauseDowned=s.autoPauseRocket=false;AntColony.Core.UserSettings.Apply(s,false);
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   AntColony.Core.StartingCandidate[] team;do{team=Enumerable.Range(0,2).Select(AntColony.Core.StartingCandidate.Roll).ToArray();}while(AntColony.Core.StartingCandidate.Missing(team).Length>0);
   AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions{seed=261025,mapSize=AntColony.Core.MapSize.Small,startingCount=2,startingTeam=team});
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;
   var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(x=>x.IsColonyMember);
   var a=AntColony.Units.RigAnimator.Find(c.gameObject);
   string Sample(string label){var ts=c.GetComponentsInChildren<Transform>();var head=ts.First(t=>t.name=="head");var hip=ts.First(t=>t.name=="pevis");var b=c.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((x,y)=>{x.Encapsulate(y);return x;});return label+" time="+a.GetCurrentAnimatorStateInfo(0).normalizedTime+" state="+c.GetComponent<AntColony.Units.AntVisual>().StateName+" up="+(head.position-hip.position).normalized+" bounds="+b.size+" rig="+a.transform.localEulerAngles+" scale="+a.transform.lossyScale+" timescale="+Time.timeScale;}
   await Task.Delay(500);var first=Sample("idle");var pos=c.Position;c.CommandMove(pos+Vector3.right*8);await Task.Delay(700);var second=Sample("move1");await Task.Delay(700);var third=Sample("move2");
   var cam=UnityEngine.Camera.main.GetComponent<AntColony.Camera.IsometricCameraController>();
   return first+"\n"+second+"\n"+third+"\nmove="+Vector3.Distance(pos,c.Position)+" cameraEnabled="+cam.enabled+" edge="+AntColony.Core.UserSettings.Current.edgeScrollThickness+" queriesTriggers="+Physics.queriesHitTriggers;
  } finally{Time.timeScale=0;AntColony.Save.SaveStorage.RootOverride=root;AntColony.Core.UserSettings.Apply(settings,false);}
 }
}
