from pathlib import Path
s=Path('AgentScripts/Presentation1111Checks.cs').read_text(encoding='utf-8-sig').replace('Presentation1111Checks','ReleaseVisualChecks').replace('Temp/presentation-1111','Temp/release-visual-pass').replace('MapBiome.None','MapBiome.Forest')
a=s.index('    public static async Task<string> Main()'); b=s.index('    // One run_script call',a)
s=s[:a]+'''    public static async Task<string> Main()
    {
        var dir="Temp/release-visual-pass"; Directory.CreateDirectory(dir);
        Menu.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(500);
        var camera=UnityEngine.Camera.main;
        var control=camera.GetComponent<AntColony.Camera.IsometricCameraController>();
        control.enabled=false;
        var team=CommanderRoster.Instance.Commanders.Where(c=>c!=null).ToArray();
        var selection=SceneObjects.Any<SelectionManager>();
        selection.SelectOnly(team[0].GetComponent<SelectableObject>());
        await Task.Delay(400);
        ScreenCapture.CaptureScreenshot(dir+"/gameplay-"+resolution+".png"); await Task.Delay(400);
        var decorations=UnityEngine.Object.FindObjectsByType<Transform>().Where(t=>t.name.StartsWith("Landscape ")).ToArray();
        if(decorations.Any(t=>t.GetComponentsInChildren<Collider>().Any(c=>c.enabled)))throw new Exception("Dressing collider");
        var report=new StringBuilder("Dressing objects: "+decorations.Length+"\\n");
        BuildScreen.Open(); await Task.Delay(500);
        Find("Tab 작업").onClick.Invoke();await Task.Delay(300);
        var button=Find("Build ScienceLab");
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
        await Task.Delay(500);
        ScreenCapture.CaptureScreenshot(dir+"/build-work-"+resolution+".png"); await Task.Delay(400);
        foreach(var t in button.GetComponentsInChildren<Text>())if(t.enabled)report.AppendLine("ScienceLab physical font: "+t.fontSize*t.transform.lossyScale.x+" preferred="+t.preferredHeight+" height="+t.rectTransform.rect.height);
        Find("Tab 생활").onClick.Invoke(); await Task.Delay(300);
        ScreenCapture.CaptureScreenshot(dir+"/build-life-"+resolution+".png"); await Task.Delay(400);
        BuildScreen.Back();
        File.WriteAllText(dir+"/state-"+resolution+".txt",report.ToString());
        return report.ToString();
    }

'''+s[b:]
Path('AgentScripts/ReleaseVisualChecks.cs').write_text(s,encoding='utf-8')
