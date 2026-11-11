from pathlib import Path
p=Path('AgentScripts/ReleaseVisualChecks.cs');s=p.read_text(encoding='utf-8-sig');needle='Find("Tab 생활").onClick.Invoke(); await Task.Delay(300);';assert needle in s
s=s.replace(needle,'''var scroll = button.GetComponentInParent<ScrollRect>();
        var beforeScroll = scroll.content.anchoredPosition.y;
        ExecuteEvents.Execute(scroll.gameObject, new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -30) }, ExecuteEvents.scrollHandler);
        await Task.Delay(400);
        if (scroll.content.anchoredPosition.y <= beforeScroll) throw new Exception("Wheel did not scroll");
        var last = Find("Build ChemistryBench");
        var corners = new Vector3[4]; ((RectTransform)last.transform).GetWorldCorners(corners);
        var viewportCorners = new Vector3[4]; scroll.viewport.GetWorldCorners(viewportCorners);
        if(corners[0].y < viewportCorners[0].y - 2 || corners[2].y > viewportCorners[2].y + 2) throw new Exception("Last building not reachable");
        ExecuteEvents.Execute(last.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
        await Task.Delay(250);
        ScreenCapture.CaptureScreenshot(dir+"/build-bottom-"+resolution+".png"); await Task.Delay(300);
        report.AppendLine("Wheel scroll reached final building; viewport contains last card");
        '''+needle)
s=s.replace('BuildScreen.Back();\n        File.WriteAllText', '''BuildScreen.Back();
        if(resolution == "1440x900")
        {
            SimulationPause.UserSpeed(1); await Task.Delay(12000); SimulationPause.UserSpeed(0);
            ScreenCapture.CaptureScreenshot(dir+"/gameplay-running-"+resolution+".png"); await Task.Delay(400);
        }
        File.WriteAllText''')
p.write_text(s,encoding='utf-8')
