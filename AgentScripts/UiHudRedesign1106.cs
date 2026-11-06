using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class UiHudRedesign1106
{
    public static async Task<string> Show(int selected)
    {
        GameMenuController.Instance.Resume();
        var selection=SceneObjects.Any<SelectionManager>();selection.ClearSelection();WorkTargetPanel.Clear();
        if(selected>0)selection.SelectOnly(CommanderRoster.Instance.Commanders[0].GetComponent<SelectableObject>());
        SimulationPause.UserSpeed(0);await Task.Delay(400);Canvas.ForceUpdateCanvases();
        return $"{Screen.width}x{Screen.height}, selected={HudOverview.Selected(selection).Length}, pinned={PrototypeResourceHud.Pinned.Count}";
    }
    public static async Task<string> Check()
    {
        await Show(0);
        if(!GameObject.Find("PinnedResources"))throw new Exception("Resources not visible");
        var bar=GameObject.Find("MenuToolbar");
        var buttons=bar.GetComponentsInChildren<Button>();
        if(buttons.Length!=9)throw new Exception("Expected 9 menu routes");
        if(bar.GetComponentsInChildren<HudNavigationIcon>().Length!=9)throw new Exception("Missing navigation pictograms");
        Bounds(bar.GetComponent<RectTransform>());
        Bounds(HudConsole.Left);
        await Show(1);
        var summary=GameObject.Find("V6SelectionSummary");Bounds(summary.GetComponent<RectTransform>());
        var portrait=GameObject.Find("Selected Commander Portrait").GetComponent<RawImage>();
        if(portrait.texture==null)throw new Exception("Missing real commander portrait");
        foreach(var button in HudConsole.Right.GetComponentsInChildren<Button>())Bounds(button.GetComponent<RectTransform>());
        var fold=GameObject.Find("V6MapToggle").GetComponent<Button>();fold.onClick.Invoke();await Task.Delay(60);
        if(HudConsole.Left.Find("MinimapScreen").gameObject.activeSelf)throw new Exception("Map fold failed");
        fold.onClick.Invoke();await Task.Delay(60);
        if(!HudConsole.Left.Find("MinimapScreen").gameObject.activeSelf)throw new Exception("Map unfold failed");
        return $"PASS resource initialization, 9 routes/icons, real portrait, HUD/button bounds, map fold/unfold at {Screen.width}x{Screen.height}";
    }
    static void Bounds(RectTransform rect)
    {
        var c=new Vector3[4];rect.GetWorldCorners(c);
        if(c[0].x<0||c[0].y<0||c[2].x>Screen.width+1||c[2].y>Screen.height+1)throw new Exception("Outside screen: "+rect.name);
    }
}
