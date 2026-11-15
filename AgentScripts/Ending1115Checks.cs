using System;using System.IO;using System.Linq;using System.Threading.Tasks;using AntColony.Core;using AntColony.UI;using UnityEngine;using UnityEngine.UI;
public static class Ending1115Checks
{
 static void Check(bool ok,string text){File.AppendAllText("work/campaign-complete-1115/ending-ui.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new Exception(text);}
 public static async Task<string> Main(){File.WriteAllText("work/campaign-complete-1115/ending-ui.txt","");var menu=GameMenuController.Instance;Check(RocketCampaign.Instance.MothershipVictory,"Actual combat victory loaded");menu.ShowDeparture();await Task.Delay(150);Canvas.ForceUpdateCanvases();
  foreach(string name in new[]{"Save Victory","Main Menu"}){var b=menu.GetComponentsInChildren<Button>().Single(x=>x.name==name);var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);Check(b.IsInteractable()&&corners.All(p=>p.x>=0&&p.y>=0&&p.x<=Screen.width&&p.y<=Screen.height),"Visible fixed footer "+name);Check(b.GetComponentInParent<ScrollRect>()==null,"Footer independent of records "+name);}
  menu.GetComponentsInChildren<Button>().Single(x=>x.name=="Save Victory").onClick.Invoke();Check(menu.ScreenName=="Save Game","Save action opens slot chooser without overwriting");menu.ShowDeparture();await Task.Delay(80);
  menu.GetComponentsInChildren<Button>().Single(x=>x.name=="Main Menu").onClick.Invoke();Check(!GameSession.Instance.GameStarted,"Main menu action leaves completed session");return "PASS ending footer actions and visibility";
 }
}
