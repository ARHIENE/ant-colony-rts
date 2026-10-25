using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Object=UnityEngine.Object;
public static class ExpeditionLayoutChecks
{
    public static async Task<string> Main()
    {
        int n=0; void Check(bool ok,string s){if(!ok)throw new Exception("FAIL "+s);n++;}
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var world=AntColony.World.WorldMapManager.Instance;
        var p=Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
        var type=p.GetType(); var refresh=type.GetMethod("RefreshExpeditions",flags);
        var ships=(List<AntColony.World.ExpeditionTransport>)world.GetType().GetField("transports",flags).GetValue(world);
        var added=new List<AntColony.World.ExpeditionTransport>();
        var open=p.IsOpen; if(!open)p.Toggle();
        try
        {
            Check(ships.Count==0,"isolated empty transport list");
            refresh.Invoke(p,new object[]{world}); Canvas.ForceUpdateCanvases();
            var area=(RectTransform)type.GetField("expeditionArea",flags).GetValue(p);
            var controls=(RectTransform)type.GetField("expeditionControls",flags).GetValue(p);
            var panel=(RectTransform)type.GetField("expeditions",flags).GetValue(p);
            Check(Mathf.Abs(area.rect.height-40)<1,"empty list compact");
            for(var i=0;i<6;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube); var ship=go.AddComponent<AntColony.World.ExpeditionTransport>();ship.Initialize(false,true);ships.Add(ship);added.Add(ship);
                type.GetField("expanded",flags).SetValue(p,ship);refresh.Invoke(p,new object[]{world}); Canvas.ForceUpdateCanvases();await Task.Yield();
                Check(area.rect.height<=240,"list capped "+i);
                Check(Mathf.Abs(controls.anchoredPosition.y+50+area.rect.height+8)<1,"controls immediately below list "+i);
                Check(Mathf.Abs(panel.rect.height-(50+area.rect.height+8+320))<1,"panel fits content "+i);
                var action=p.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Box Actions").Last();
                Check(action.rect.width<=336-16,"expanded buttons fit row "+i);
            }
            Check(area.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).vertical,"overflow scroll container");
            return "PASS "+n+" expedition layout checks (0, 1 and 6 transports)";
        }
        finally{foreach(var ship in added){ships.Remove(ship);Object.Destroy(ship.gameObject);}type.GetField("expanded",flags).SetValue(p,null);refresh.Invoke(p,new object[]{world});if(!open)p.Toggle();}
    }
}
