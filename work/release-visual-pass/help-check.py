from pathlib import Path
p=Path('AgentScripts/ReleaseVisualChecks.cs');s=p.read_text(encoding='utf-8-sig')
needle='BuildScreen.Back();\n        if(resolution == "1440x900")'
s=s.replace(needle,'''await CheckBuildingHelp(report);
        BuildScreen.Back();
        if(resolution == "1440x900")''')
a=s.index('    // One run_script call')
s=s[:a]+'''    static async Task CheckBuildingHelp(StringBuilder report)
    {
        var tip=(Text)typeof(GameMenuController).GetField("tip",Flags).GetValue(Menu);
        var panel=(RectTransform)typeof(GameMenuController).GetField("tooltipPanel",Flags).GetValue(Menu);
        foreach(var (category, name, words) in Targets)
        {
            Find("Tab "+category).onClick.Invoke(); await Task.Delay(80);
            var b=Find(name); var scroll=b.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            float y=-((RectTransform)b.transform).anchoredPosition.y;
            float max=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height);
            scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(y,0,max));
            await Task.Delay(80); Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)b.transform;
            var hit=rect.TransformPoint(rect.rect.center);
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=hit},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new Exception(name+" cannot receive pointer");
            b.GetComponent<MenuTooltip>().OnPointerEnter(new PointerEventData(EventSystem.current));
            await Task.Delay(90);
            if(!panel.gameObject.activeInHierarchy || words.Any(w=>!tip.text.Contains(w)))throw new Exception(name+" help missing");
            if(tip.preferredHeight>tip.rectTransform.rect.height+1)throw new Exception(name+" help clipped: "+tip.preferredHeight+"/"+tip.rectTransform.rect.height);
            if(tip.fontSize*tip.transform.lossyScale.x <11.9f)throw new Exception("Help text too small");
            var corners=new Vector3[4];panel.GetWorldCorners(corners);
            var buttonCorners=new Vector3[4];rect.GetWorldCorners(buttonCorners);
            if(corners[0].x <buttonCorners[2].x || corners[2].x >Screen.width+1)throw new Exception("Help covers grid or screen edge");
        }
        report.AppendLine("16 building help panels: reachable pointer targets, content intact, no clipping, >=12px, right of grid");
    }

'''+s[a:]
p.write_text(s,encoding='utf-8')
