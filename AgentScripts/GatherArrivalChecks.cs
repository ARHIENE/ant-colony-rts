using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object=UnityEngine.Object;
public static class GatherArrivalChecks
{
 static int count;static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);count++;}
 public static async Task<string> Main()
 {
  count=0;var root=AntColony.Save.SaveStorage.RootOverride;var old=AntColony.Core.UserSettings.Current.Clone();GameObject test=null;
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"GatherArrival-"+Guid.NewGuid().ToString("N"));
  try
  {
   var settings=old.Clone();settings.autoSaveEnabled=false;settings.autoPauseThreat=settings.autoPauseDowned=settings.autoPauseRocket=false;AntColony.Core.UserSettings.Apply(settings,false);
   while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   AntColony.Core.StartingCandidate[] team;do{team=Enumerable.Range(0,2).Select(AntColony.Core.StartingCandidate.Roll).ToArray();}while(AntColony.Core.StartingCandidate.Missing(team).Length>0);
   AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions{seed=261025,mapSize=AntColony.Core.MapSize.Small,startingCount=2,startingTeam=team});while(AntColony.Save.SaveSystem.Busy)await Task.Delay(50);
   var menu=AntColony.UI.GameMenuController.Instance;menu.Resume();Time.timeScale=1;
   var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(c=>c.IsColonyMember&&c.CanDoJob(AntColony.Units.CommanderJobs.Gathering));c.CommandStop();
   Check(UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.right*5,out var hit,3,UnityEngine.AI.NavMesh.AllAreas),"nearby work ground");
   test=GameObject.CreatePrimitive(PrimitiveType.Cylinder);test.SetActive(false);test.name="Gather arrival wood";test.transform.position=hit.position+Vector3.up*.5f;var node=test.AddComponent<AntColony.World.ResourceNode>();node.ConfigureNatural(AntColony.Data.ResourceType.Wood,100,0);test.SetActive(true);await Task.Yield();await Task.Yield();
   var cam=UnityEngine.Camera.main;cam.GetComponent<AntColony.Camera.IsometricCameraController>().FocusOn(test.transform.position);
   var selector=Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();selector.ClearSelection();typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(selector,new object[]{c.GetComponent<AntColony.Units.SelectableObject>()});Physics.SyncTransforms();
   var motionAnimator=AntColony.Units.RigAnimator.Find(c.gameObject);var skin=c.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.enabled&&r.name.StartsWith("Part_Body_"));var mesh=new Mesh();skin.BakeMesh(mesh);var pose=mesh.vertices;
   var beforePos=c.Position;var click=(Vector2)cam.WorldToScreenPoint(test.GetComponent<Collider>().bounds.center);
   typeof(AntColony.Units.UnitSelectionController).GetMethod("IssueCommand",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Object.FindAnyObjectByType<AntColony.Units.UnitSelectionController>(),new object[]{click});
   Check(c.CurrentResourceNode==node,"right click accepted");var feedback=node.GetComponent<AntColony.Units.GatherOrderFeedback>();Check(feedback!=null&&feedback.OrderText.Contains("이동 중"),"gather order feedback distinct from move");Check(node.GetComponent<AntColony.World.ResourceNodeStatus>().StatusText.Contains("이동 중"),"resource label confirms order");
   await Task.Delay(300);skin.BakeMesh(mesh);Check(mesh.vertices.Where((v,i)=>(v-pose[i]).sqrMagnitude>.000001f).Any(),"visible mesh deforms while walking without forced Animator.Update");Object.Destroy(mesh);
   var deadline=Time.realtimeSinceStartup+18;while(!c.IsGatheringAnimation&&Time.realtimeSinceStartup<deadline)await Task.Delay(100);
   Check(c.IsGatheringAnimation,"walk from five meters reaches gathering despite stoppingDistance zero");Check(Vector3.Distance(beforePos,c.Position)>1,"actual approach without Warp");await Task.Delay(250);
   Check(!c.Agent.hasPath,"path cleared at work position");Check(feedback.OrderText.Contains("채집 중"),"target shows actively gathering");
   var anim=AntColony.Units.RigAnimator.Find(c.gameObject);Check(anim.GetCurrentAnimatorClipInfo(0).Any(x=>x.clip.name=="Tool_Chop"&&x.weight>.8f),"actual chop clip selected instead of Walk");
   var bones=anim.GetComponentsInChildren<Transform>();var rotations=bones.Select(t=>t.localRotation).ToArray();var at=c.Position;var amount=node.AmountRemaining;await Task.Delay(700);
   Check(bones.Where((t,i)=>Quaternion.Angle(rotations[i],t.localRotation)>.2f).Any(),"chop bones animate in real frames");Check(Vector3.Distance(at,c.Position)<.12f,"no rubbing against resource while working");Check(node.AmountRemaining<amount&&c.CarriedAmount>0,"gather amount decreases and cargo increases");
   c.CommandStop();await Task.Yield();await Task.Yield();Check(feedback.OrderText==null&&!feedback.GetComponentInChildren<LineRenderer>().enabled,"cancel clears target indicator");
   var result="PASS "+count+" gather arrival, distinct feedback and real-frame chop checks";System.IO.File.WriteAllText("Temp/gather-arrival-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/gather-arrival-result.txt",e.ToString());throw;}
  finally{if(test!=null)Object.Destroy(test);Time.timeScale=0;AntColony.Save.SaveStorage.RootOverride=root;AntColony.Core.UserSettings.Apply(old,false);}
 }
}
