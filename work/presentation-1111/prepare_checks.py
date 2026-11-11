from pathlib import Path
f=Path('AgentScripts/BuildTooltip1111Checks.cs');s=f.read_text(encoding='utf-8-sig')
needle='            log.AppendLine($"  size'
i=s.index(needle)
s=s[:i]+'''            Rect ScreenRect(RectTransform rect)
            {
                var c = new Vector3[4]; rect.GetWorldCorners(c);
                return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
            }
            var helpRect = ScreenRect(panel);
            foreach (var control in button.transform.root.GetComponentsInChildren<Button>())
            {
                if (!control.gameObject.activeInHierarchy) continue;
                Check(!helpRect.Overlaps(ScreenRect((RectTransform)control.transform)), name + " does not cover " + control.name);
            }
            Check(BuildScreen.TooltipArea((RectTransform)button.transform, out var description), name + " uses description column");
            var column = ScreenRect(description);
            Check(Mathf.Abs(column.xMin-helpRect.xMin)<1 && Mathf.Abs(column.yMax-helpRect.yMax)<1, name + " inside right column");
''' +s[i:]
f.write_text(s,encoding='utf-8')
# Reuse isolated startup and resolution restoration, with a focused gameplay capture body.
s=s.replace('BuildTooltip1111Checks','Presentation1111Checks').replace('build-tooltip-1111','presentation-1111').replace('biome = MapBiome.Forest','biome = MapBiome.None')
start=s.index('    public static async Task<string> Main()')
end=s.index('    // One run_script',start)
body='''    public static async Task<string> Main()
    {
        passed=0; var log = new StringBuilder();
        var dir="Temp/presentation-1111"; Directory.CreateDirectory(dir);
        Menu.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(1000);
        var camera=UnityEngine.Camera.main;
        var controller=camera.GetComponent<AntColony.Camera.IsometricCameraController>();
        var selection=SceneObjects.Any<SelectionManager>();
        var team=CommanderRoster.Instance.Commanders.Where(c=>c!=null).ToArray();
        var rendererFlags=team.SelectMany(c=>c.GetComponentsInChildren<Renderer>(true)).ToDictionary(r=>r,r=>r.forceRenderingOff);
        Check(GameSession.Instance.Options.biome==MapBiome.None,"legacy None world preserved");
        var terrain=AntColony.Map.MapGenerator.SelectSingleTerrain();
        var layers=(System.Collections.IEnumerable)typeof(AntColony.Map.MapGenerator).GetField("terrainLayers",Flags).GetValue(terrain);
        int textured=0; foreach(AntColony.Map.MapGenerator.Layer layer in layers) if(layer.texture) textured++;
        Check(textured>0,"None terrain has textured layers");
        log.AppendLine("Pipeline: "+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name);
        log.AppendLine("Textured layers: "+textured);
        var objective=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r=>r.name=="Objective");
        var text=objective.GetComponentInChildren<Text>();
        Check(!text.text.Contains("예상")&&!text.text.Contains("이후 ·"),"objective has no duplicate wave/reward wall");
        Check(text.preferredHeight <= text.rectTransform.rect.height+1,"objective fits");
        for(int i=0;i<team.Length;i++)
        {
            var portrait=CharacterPortraits.Get(team[i]) as RenderTexture;
            Check(portrait!=null,"portrait "+i+" exists");
            var prev=RenderTexture.active; RenderTexture.active=portrait;
            var image=new Texture2D(portrait.width,portrait.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,portrait.width,portrait.height),0,0);image.Apply();RenderTexture.active=prev;
            File.WriteAllBytes(dir+"/portrait-"+i+"-"+resolution+".png",image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
            selection.SelectOnly(team[i].GetComponent<SelectableObject>());
            await Task.Delay(350);
            ScreenCapture.CaptureScreenshot(dir+"/gameplay-"+i+"-"+resolution+".png");await Task.Delay(400);
        }
        foreach(var pair in rendererFlags) Check(pair.Key.forceRenderingOff==pair.Value,"world renderer unchanged");
        File.WriteAllText(dir+"/state-"+resolution+".txt",log.ToString());
        return "PASS "+passed+" checks; captures require visual review";
    }

'''
s=s[:start]+body+s[end:]
Path('AgentScripts/Presentation1111Checks.cs').write_text(s,encoding='utf-8')
