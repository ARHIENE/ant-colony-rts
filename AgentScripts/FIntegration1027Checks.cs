using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class FIntegration1027Checks
{
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("F integration FAIL "+checks+" "+label);checks++;System.IO.File.WriteAllText("Temp/FIntegration1027-progress.txt",checks+" "+label);}
 static async Task Wait(Func<bool> ready,int seconds=20){var end=DateTime.UtcNow.AddSeconds(seconds);while(!ready()&&DateTime.UtcNow<end)await Task.Delay(80);}
 static Button Button(string text)=>AntColony.UI.GameMenuController.Instance.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text==text);
 public static async Task<string> Main()
 {
  checks=0;GameObject go=null;Time.timeScale=0;var settings=UserSettings.Current.Clone();var root=AntColony.Save.SaveStorage.RootOverride;
  AntColony.Save.SaveStorage.RootOverride=System.IO.Path.Combine(Application.temporaryCachePath,"FIntegration1027-"+Guid.NewGuid().ToString("N"));
  try
  {
   var config=settings.Clone();config.autoSaveEnabled=false;config.autoPauseThreat=config.autoPauseDowned=config.autoPauseRocket=false;UserSettings.Apply(config,false);
   var day=Mathf.Floor(GameCalendar.GameSeconds/150)*150;if(GameCalendar.TimeOfDay>20)day+=150;GameSession.Instance.MarkStarted(gameSeconds:day+20);
   foreach(var person in AntColony.Core.CommanderRoster.Instance.Commanders){person.CommandStop();person.SetJobEnabled(CommanderJobs.All,false);person.Traits.values.Clear();person.PersonalState.meal=new CommanderMealState();person.PersonalState.sleep=new CommanderSleepState();person.PersonalState.hygiene=new CommanderHygieneState();person.PersonalState.joy=new CommanderJoyState();}
   var c=AntColony.Core.CommanderRoster.Instance.Commanders.First(c=>c.CivilianWorkReady&&c.CanChangeAllocation);
   c.SetJobEnabled(CommanderJobs.Research,true);var state=CampaignResearch.Instance.CaptureState();if(!state.completed.Contains((int)ScienceTechnology.UndergroundSurvey))state.completed.Add((int)ScienceTechnology.UndergroundSurvey);CampaignResearch.Instance.RestoreState(state);
   Check(UnityEngine.AI.NavMesh.SamplePosition(c.Position+Vector3.right*12,out var ground,4,UnityEngine.AI.NavMesh.AllAreas),"reachable distant scanner site");
   var template=(GameObject)typeof(UndergroundScanner).GetMethod("Template",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);go=Object.Instantiate(template,ground.position,Quaternion.identity);go.name="UndergroundScanner";go.SetActive(true);var scanner=go.GetComponent<UndergroundScanner>();
   Check(Vector3.Distance(c.Position,scanner.Position)>8,"scanner starts beyond direct assignment range");
   Check(scanner.RequestAssignment(c)&&scanner.Target==c,"menu assignment reserves distant scanner without teleport");
   var before=c.Position;AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=1;await Wait(()=>Vector3.Distance(c.Position,scanner.Position)<7.5f,20);Time.timeScale=0;
   Check(Vector3.Distance(c.Position,before)>1&&Vector3.Distance(c.Position,scanner.Position)<8,"researcher walks to assigned scanner");
   var progress=scanner.Progress;GameSession.Instance.MarkStarted(gameSeconds:day+110);c.TickDuty(.1f);
   Check(c.IsAsleep&&scanner.Target==c&&c.SurveyAssignment==scanner,"night sleep preserves scanner ownership");
   GameSession.Instance.MarkStarted(gameSeconds:day+170);c.TickDuty(.1f);Check(!c.IsAsleep&&scanner.Target==c,"scanner assignment resumes after needs");
   Check(scanner.Progress==progress,"outage and sleep add no survey work");
   var menu=AntColony.UI.GameMenuController.Instance;menu.Management("생산·자원","매장지·배관");await Task.Delay(100);
   Check(menu.ScreenName=="지하 탐사","production tab opens functional survey menu");
   Check(menu.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()?.text=="담당 해제 · 후보 진행 보존"),"survey release control available");
   var survey=UndergroundSurvey.Instance;Check(!menu.GetComponentsInChildren<Text>().Any(t=>survey.Layers.Where(l=>!l.discovered&&l.type>=0).Any(l=>t.text.Contains($"남은 총량 {l.remaining:0}"))),"undiscovered deposit amounts not leaked");
   Button("담당 해제 · 후보 진행 보존").onClick.Invoke();Check(scanner.Target==null&&c.SurveyAssignment==null,"release button updates real assignment");
   menu.Resume();var assessment=WorldMapManager.Instance.Assessment;assessment.AssignEvaluator(c);assessment.Tick(10);menu.Management("인재·편성","평가");await Task.Delay(80);
   Check(menu.ScreenName=="인재 평가"&&assessment.Rating(c.PersonalState.id).HasValue,"assessment tab and report connected");
   Check(menu.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("종합 현재 능력")&&t.text.Contains("추정")),"relative current stars and uncertain potential shown");
   Button("관심 등록").onClick.Invoke();Check(assessment.Reports.Any(r=>r.target==c.PersonalState.id&&r.interested),"interest control updates saved report");
   menu.Details(c);await Task.Delay(80);Check(!menu.GetComponentsInChildren<Text>().Any(t=>System.Text.RegularExpressions.Regex.IsMatch(t.text,@"\b(CA|PA)\s+\d")),"actual CA and PA totals hidden in roster");
   var site=WorldMapManager.Instance.Sites.First(s=>DiplomacyManager.Instance.Faction(s)!=null);menu.ShowInvestigation(site);await Task.Delay(80);
   Check(menu.ScreenName=="외부 인재 조사","external investigation tab opens distinct dispatch workflow");
   Check(menu.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()?.text=="도보 원정대 만들기"),"walking transport can be chosen");
   Check(menu.GetComponentsInChildren<ScrollRect>().Length>=2,"dispatch columns scroll independently");
   menu.Resume();AntColony.UI.BuildScreen.Open();await Task.Delay(80);Check(Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Any(b=>b.name=="Build UndergroundScanner"),"scanner exposed in construction catalogue");while(AntColony.UI.BuildScreen.Back()){}
   Check(AntColony.Save.SaveSystem.TrySave(false,0,out var error),"UI operations keep save valid: "+error);
   var result="PASS "+checks+" F1/F2 menu interaction, disclosure and real survey approach checks";System.IO.File.WriteAllText("Temp/FIntegration1027-result.txt",result);return result;
  }
  catch(Exception e){System.IO.File.WriteAllText("Temp/FIntegration1027-result.txt",e.ToString());throw;}
  finally{Time.timeScale=0;if(go!=null)Object.Destroy(go);AntColony.UI.GameMenuController.Instance.Resume();Time.timeScale=0;UserSettings.Apply(settings,false);AntColony.Save.SaveStorage.RootOverride=root;}
 }
}
