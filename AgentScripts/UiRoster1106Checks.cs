using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Buildings;
using System.Reflection;
using AntColony.Units;
using AntColony.UI;
using UnityEngine;
using UnityEngine.UI;

public static class UiRoster1106Checks
{
    public static async Task<string> Populate()
    {
        var roster = CommanderRoster.Instance;
        if (!Application.isPlaying || roster == null || roster.Count < 1) throw new Exception("Isolated test world required");
        string[] names = { "서리", "보리", "연지", "여울", "달래", "솔", "하늘", "나루", "단비", "산들", "해솔", "이슬", "마루", "가람", "별", "노을", "초롱", "다온", "한결", "은솔", "새봄" };
        var origin = roster.Commanders[0].transform.position;
        for (int i = roster.Count; i < 24; i++)
        {
            var c = roster.Create(names[(i - 3) % names.Length], CommanderRank.Sergeant, new[] { UnitRole.Worker, UnitRole.Melee },
                UnitRole.Worker, new CommanderTraits(), origin + new Vector3(i % 6 * 2, 0, i / 6 * 2));
            if (c == null) throw new Exception("Cannot place commander " + i);
            c.SetJobEnabled(CommanderJobs.All, false);
        }
        var selection = SceneObjects.Any<SelectionManager>(); selection.ClearSelection();
        foreach (var c in roster.Commanders.Take(8)) selection.Add(c.GetComponent<SelectableObject>());
        GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0);
        await Task.Delay(500);
        if (roster.Count != 24 || HudOverview.Selected(selection).Length != 8) throw new Exception("24 / 8 setup failed");
        return $"24 real commanders / 8 selected; frame {Time.frameCount}; {Screen.width}x{Screen.height}";
    }
    public static async Task<string> CheckRoster()
    {
        GameMenuController.Instance.Roster(); await Task.Delay(250);
        var rows = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b => b.name.StartsWith("Commander ")).ToArray();
        if (rows.Length != 24) throw new Exception("24 roster rows missing: " + rows.Length);
        var root = GameObject.Find("PrototypeWindow").GetComponent<RectTransform>();
        if (Math.Abs(root.lossyScale.x - root.GetComponentInParent<Canvas>().scaleFactor) > .01) throw new Exception("Roster text shrunk");
        var corners = new Vector3[4]; root.GetWorldCorners(corners);
        if (corners[0].x < -1 || corners[0].y < -1 || corners[2].x > Screen.width + 1 || corners[2].y > Screen.height + 1) throw new Exception("Roster outside screen");
        rows.First(b => b.name == "Commander " + GameMenuController.SortedCommanders().Last().CommanderName).onClick.Invoke(); await Task.Delay(200);
        if (!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Any(b => b.name == "Roster Equipment")) throw new Exception("Last commander detail unavailable");
        return $"PASS 24 rows, last commander detail, no text shrink, window bounds at {Screen.width}x{Screen.height}; frame {Time.frameCount}";
    }
    public static async Task<string> Fixtures()
    {
        int i=0;
        foreach(var kind in new[]{BuildingKind.Storage,BuildingKind.Workshop,BuildingKind.Kitchen,BuildingKind.Pen,BuildingKind.Infirmary})
        {
            var prefab=(GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{kind,UnitRole.Worker});
            if(prefab==null)throw new Exception("Missing fixture "+kind);
            var go=UnityEngine.Object.Instantiate(prefab,CommanderRoster.Instance.Commanders[0].transform.position+new Vector3(12+i++*5,0,10),Quaternion.identity);
            go.name="UI Test "+kind;go.SetActive(true);
        }
        await Task.Delay(400);
        return "Operational fixtures ready; isolated world only";
    }
    public static string Show(string page)
    {
        var menu=GameMenuController.Instance;
        switch(page)
        {
            case "storage":menu.ShowStorage(ResourceManager.Instance.Bins.First());break;
            case "production":menu.ShowWorkshop(SceneObjects.All<Workshop>().First());break;
            case "medical":menu.ShowHealth(GameMenuController.SortedCommanders().First());break;
            case "equipment":menu.ShowEquipment(GameMenuController.SortedCommanders().First());break;
            case "ranch":menu.ShowRanch(SceneObjects.All<Ranch>().First());break;
            case "housing":menu.Population();break;
            case "work":menu.WorkSchedule();break;
            case "repair":menu.RepairOverview();break;
            case "resources":menu.PrototypeResources();break;
            case "research":menu.Science();break;
            case "roster":typeof(GameMenuController).GetField("rosterDetail",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,false);menu.Roster();break;
            default:menu.Resume();break;
        }
        return page;
    }
    public static async Task<string> Operations()
    {
        Show("storage");await Task.Delay(250);
        var bin=ResourceManager.Instance.Bins.First();bool allowed=bin.Allows(ResourceType.Wood);
        Button Find(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(b=>b.name==name);
        var tab=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b=>b.name=="Section 1");
        if(tab!=null){tab.onClick.Invoke();await Task.Delay(100);}
        Find("Allow Wood").onClick.Invoke();await Task.Delay(150);
        if(bin.Allows(ResourceType.Wood)==allowed)throw new Exception("Warehouse allow action failed");
        tab=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b=>b.name=="Section 1");
        if(tab!=null){tab.onClick.Invoke();await Task.Delay(100);}
        Find("Allow Wood").onClick.Invoke();await Task.Delay(100);
        if(bin.Allows(ResourceType.Wood)!=allowed)throw new Exception("Warehouse restore failed");
        Show("work");await Task.Delay(150);
        var c=GameMenuController.SortedCommanders().First();
        var before=c.JobPriority(CommanderJobs.Repair);
        var row=GameObject.Find("Duty "+c.CommanderName);
        row.transform.Find("Priority Repair").GetComponent<Button>().onClick.Invoke();
        if(c.JobPriority(CommanderJobs.Repair)==before)throw new Exception("Priority action failed");
        Show("research");await Task.Delay(150);
        if(!GameObject.Find("ResearchGraph"))throw new Exception("Research graph missing");
        Find("Research Detail Tab").onClick.Invoke();await Task.Delay(150);
        Show("housing");await Task.Delay(200);
        if(GameObject.Find("Prototype Settings"))throw new Exception("Gear overlaps modal");
        return $"PASS warehouse toggles, job priority changes, research detail tab, housing/gear at {Screen.width}x{Screen.height}";
    }
}

