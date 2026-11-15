using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
public static class DepartureNotice1115Checks
{
 const string Log="work/colony-polish-1115/departure-notice.txt";
 static void Check(bool value,string text){File.AppendAllText(Log,(value?"PASS ":"FAIL ")+text+"\n");if(!value)throw new Exception(text);}
 public static async Task<string> Main(){
  if(Time.timeScale!=0||string.IsNullOrEmpty(SaveStorage.RootOverride))throw new Exception("Paused isolated run required");
  if(!SaveSystem.TrySave(false,0,out var error))throw new Exception(error);
  var path=SaveSlots.PathFor(false,0);File.WriteAllText(Log,"Paused fixture only; no departure commands or gameplay advances. Full checkpoint restored afterward.\n");
  try{
   var c=SceneObjects.All<CommanderAnt>().First(c=>c.IsColonyMember&&!c.IsDead);
   var party=WorldMapManager.Instance.Transports.First(t=>t.Walking&&t.State==ExpeditionState.Home);
   var crew=(List<CommanderAnt>)typeof(ExpeditionTransport).GetField("crew",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(party);
   var site=WorldMapManager.Instance.Sites.First(WorldMapManager.SiteRevealed);
   crew.Add(c);c.PersonalState.social.pendingDeparture=false;
   var warning=party.DepartureWarning(site);var key=party.DepartureWarningKey(site);var block=party.DepartureBlock(site);
   Check(!warning.Contains("이탈 예정"),"No pending departure means no extra warning");
   c.PersonalState.social.pendingDeparture=true;
   Check(party.DepartureWarning(site).Contains("이탈 예정 장수: "+c.CommanderName)&&party.DepartureWarning(site).Contains("하선 시"),"Warns about named pending departure before embarkation");
   Check(party.DepartureWarning(site).StartsWith(warning),"Existing supply/home warnings retained");
   Check(party.DepartureBlock(site)==block,"Warning does not change departure eligibility");
   Check(party.DepartureWarningKey(site)!=key,"Changed risk invalidates old warning acknowledgement");
   c.PersonalState.social.pendingDeparture=false;crew.Remove(c);
   var med="med:"+c.PersonalState.id;var wait="medwait:"+c.PersonalState.id;
   ToastManager.SetCrisis(med,"fixture medical crisis");ToastManager.SetPinned(wait,"fixture medical wait");
   ToastManager.SetPinned("fixture-other-patient","other patient retained");
   Check(ToastManager.PinnedKeys.Contains(wait),"Medical wait fixture visible before disable");
   c.gameObject.SetActive(false);
   Check(!ToastManager.PinnedKeys.Contains(wait)&&!ToastManager.VisibleLines.Any(s=>s.Contains("fixture medical crisis")),"Disabling patient clears both medical notices");
   Check(ToastManager.PinnedKeys.Contains("fixture-other-patient"),"Other patient's notice retained");
  }finally{
   if(!SaveSystem.TryLoad(path,out error))throw new Exception(error);
   var until=DateTime.UtcNow.AddSeconds(100);while((SaveSystem.Busy||!GameSession.Exists||!GameSession.Instance.GameStarted)&&DateTime.UtcNow<until)await Task.Delay(100);
   SimulationPause.UserSpeed(0);
  }
  return File.ReadAllText(Log);
 }
}
