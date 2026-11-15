using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using AntColony.Buildings;
using AntColony.UI;
using UnityEngine;
public static class StarvationWarning1116Checks
{
    const string Dir="work/starvation-warning-1116/";
    static void Check(bool ok,string message) { File.AppendAllText(Dir+"checks.txt",(ok?"PASS ":"FAIL ")+message+"\n"); if(!ok)throw new Exception(message); }
    public static async Task<string> Main()
    {
        if(!Application.isPlaying || Time.timeScale!=0)throw new Exception("Paused isolated Play required");
        Directory.CreateDirectory(Dir); File.WriteAllText(Dir+"checks.txt","Temporary food availability fixtures; natural state restored afterward.\n");
        var c=CommanderRoster.Instance.Commanders.First(x=>x.IsColonyMember&&!x.IsDead);
        var meal=c.PersonalState.meal; var rations=c.PersonalState.carriedRations;
        var pause=SimulationPause.Capture(); var settings=UserSettings.Current; var auto=settings.autoPauseThreat;
        var sources=SceneObjects.All<ResourceNode>().Where(n=>CookedFoods.MealTypes.Contains(n.ResourceType)).Select(n=>n.gameObject)
            .Concat(SceneObjects.All<Storage>().Select(b=>b.gameObject)).Concat(SceneObjects.All<Kitchen>().Select(k=>k.gameObject)).Where(g=>g.activeSelf).Distinct().ToArray();
        GameObject food=null;
        try
        {
            foreach(var g in sources)g.SetActive(false);
            c.PersonalState.meal=new CommanderMealState{satiety=26,diet=CommanderAnt.AllDiet}; c.PersonalState.carriedRations=new FoodLots();
            Check(UpkeepManager.HasMealShortage(c)&&!UpkeepManager.NeedsMealWarning(c),"Unavailable food above warning threshold does not alert");
            c.PersonalState.meal.satiety=25;
            Check(UpkeepManager.NeedsMealWarning(c),"Early warning occurs before zero satiety");
            settings.autoPauseThreat=true; ThreatIncidents.Scan(true);
            var first=SimulationPause.Capture();
            Check(first.active.Contains("hunger:"+c.PersonalState.id)&&first.reasons.Any(r=>r.StartsWith("incident:hunger:")),"Existing threat auto-pause preference pauses on new food emergency");
            ThreatIncidents.Scan(true);
            Check(SimulationPause.Capture().incidents.Count==first.incidents.Count,"Repeated scan does not duplicate the incident");
            c.PersonalState.meal.served=true;
            Check(!UpkeepManager.NeedsMealWarning(c),"Already served meal suppresses warning");
            c.PersonalState.meal.served=false;
            food=new GameObject("MealWarningFixture"); food.SetActive(false); food.transform.position=c.Position+Vector3.right;
            food.AddComponent<ResourceNode>().ConfigureLoot(ResourceType.Food,40); food.SetActive(true);
            Check(!UpkeepManager.NeedsMealWarning(c),"Accessible floor food suppresses warning using actual meal rules");
            food.SetActive(false); settings.autoPauseThreat=false;
            SimulationPause.Restore(new SimulationPause.State()); SimulationPause.UserSpeed(0); ThreatIncidents.Scan(true);
            Check(!SimulationPause.Reasons.Any(r=>r.StartsWith("incident:hunger:")),"Disabled auto-pause keeps warning without pausing");
            GameMenuController.Instance.Details(c); await Task.Delay(250); Canvas.ForceUpdateCanvases();
            var text=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().First(t=>t.text.Contains("<b>굶주림 이탈</b>"));
            Check(text.preferredHeight<=text.rectTransform.rect.height+1,"Both departure explanations fit their text area");
            var scroll=text.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            var cell=(RectTransform)text.transform.parent;
            scroll.content.anchoredPosition=new Vector2(scroll.content.anchoredPosition.x,Mathf.Max(0,-cell.anchoredPosition.y-80));
            Canvas.ForceUpdateCanvases(); await Task.Delay(250);
            ScreenCapture.CaptureScreenshot(Dir+"departure-detail.png"); await Task.Delay(350);
            Check(c.PersonalState.social.departure==DepartureState.None,"Read-only warning does not trigger departure");
        }
        finally
        {
            if(food!=null)UnityEngine.Object.DestroyImmediate(food);
            foreach(var g in sources)if(g!=null)g.SetActive(true);
            c.PersonalState.meal=meal;c.PersonalState.carriedRations=rations; settings.autoPauseThreat=auto;
            GameMenuController.Instance.Resume();SimulationPause.Restore(pause);SimulationPause.UserSpeed(0);
        }
        return File.ReadAllText(Dir+"checks.txt");
    }
}
