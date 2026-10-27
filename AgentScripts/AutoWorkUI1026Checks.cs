using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.UI;
using AntColony.World;
using AntColony.Data;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class AutoWorkUI1026Checks
{
 static int checks;static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+checks+" "+label);checks++;}
 static Button Button(string name)=>GameMenuController.Instance.GetComponentsInChildren<Button>().First(b=>b.name==name);
 static bool Inside(RectTransform parent,RectTransform child){var corners=new Vector3[4];child.GetWorldCorners(corners);return corners.All(p=>parent.rect.Contains(parent.InverseTransformPoint(p)));}
 public static async Task<string> Main()
 {
  checks=0;var menu=GameMenuController.Instance;var old=Time.timeScale;
  try
  {
   menu.WorkDesignations();await Task.Delay(100);Canvas.ForceUpdateCanvases();
   var panel=menu.GetComponentsInChildren<RectTransform>().First(t=>t.name=="WorkDesignations");
   foreach(var name in new[]{"Designate Logging","Designate Mining","Designate Harvesting","Cancel Natural Work","Forbid Natural Work","Allow Natural Work","Work Orders","Work Priorities","Close Work Designations"})
   {var b=Button(name);Check(b.interactable&&Inside(panel,(RectTransform)b.transform),"visible button within panel "+name);}
   Button("Designate Logging").onClick.Invoke();await Task.Delay(50);Check(GatherDesignation.IsActive&&GatherDesignation.ConsumesPointerInput&&!GameMenuController.BlocksInput,"logging input mode closes menu and owns pointer");GatherDesignation.End();
   var n=ResourceNode.Available.First(n=>n.NeedsDesignation&&!n.IsDepleted&&!n.GatheringForbidden&&n.IsUnlocked);n.RequestWork();
   menu.ShowTasks(GameMenuController.TaskCandidates().ToList());await Task.Delay(100);Canvas.ForceUpdateCanvases();
   var text=menu.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();Check(text.Any(t=>t.Contains(n.WorkName)),"designated natural work listed");Check(text.Any(t=>t.Contains("종류:"))&&text.Any(t=>t.Contains("진행·대기")),"kind and waiting filters available");
   Check(menu.GetComponentsInChildren<ScrollRect>().Length>=2,"work sections scroll independently");
   var pause=menu.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="선택 전체 일시정지");pause.onClick.Invoke();Check(n.WorkPaused,"bulk pause reaches natural request");
   var resume=menu.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="선택 전체 재개");resume.onClick.Invoke();Check(!n.WorkPaused,"bulk resume reaches natural request");
   var jump=menu.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="위치 → "+n.ResourceType.DisplayName()+" "+n.WorkName);jump.onClick.Invoke();await Task.Delay(50);Check(!GameMenuController.BlocksInput,"target jump closes work list");n.CancelWork();
   var result="PASS "+checks+" work designation/list interaction and bounds checks";System.IO.File.WriteAllText("Temp/autowork-ui1026-result.txt",result);return result;
  }catch(Exception e){System.IO.File.WriteAllText("Temp/autowork-ui1026-result.txt",e.ToString());throw;}
  finally{GatherDesignation.End();menu.Resume();Time.timeScale=old;}
 }
}
