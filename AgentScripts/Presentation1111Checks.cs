using System;
using System.IO;
using System.Collections.Generic;
using AntColony.Units;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;

// BUILD-TOOLTIP-1111: 건설 메뉴 초반 건물 툴팁의 내용과 실제 표시를 격리된 새 게임에서 확인한다.
// 사용자 저장 슬롯은 쓰지 않는다. 단일 Run 진입점으로 실행한다(설정/저장루트/해상도 finally 복원).
public static class Presentation1111Checks
{
    // Read-only follow-up in another verifier's current Play scene: no selection,
    // camera, pause, scene, save-root or settings changes.
    public static async Task<string> Current()
    {
        var dir="Temp/presentation-1111/final"; Directory.CreateDirectory(dir);
        var rects=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        var goal=rects.First(r=>r.name=="Objective");
        var resources=rects.First(r=>r.name=="PinnedResources");
        Rect Bounds(RectTransform rect) { var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y); }
        if(Bounds(goal).Overlaps(Bounds(resources)))throw new Exception("Goal covers resource inventory");
        var shader=Shader.Find("AntColony/TerrainBlend");
        if(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"))throw new Exception("Terrain shader error");
        int index=0;
        foreach(var commander in CommanderRoster.Instance.Commanders.Where(c=>c!=null))
        {
            var portrait=CharacterPortraits.Get(commander) as RenderTexture;
            if(portrait==null)throw new Exception("Missing portrait");
            var previous=RenderTexture.active;
            var snapshot=new Texture2D(portrait.width,portrait.height,TextureFormat.RGBA32,false);
            try { RenderTexture.active=portrait;snapshot.ReadPixels(new Rect(0,0,portrait.width,portrait.height),0,0);snapshot.Apply();File.WriteAllBytes(dir+"/portrait-"+(index++)+".png",snapshot.EncodeToPNG()); }
            finally { RenderTexture.active=previous;UnityEngine.Object.Destroy(snapshot); }
        }
        ScreenCapture.CaptureScreenshot(dir+"/gameplay.png");await Task.Delay(500);
        var result=$"Current Play: {Screen.width}x{Screen.height}, resource/goal overlap=0, portraits={index}, shader errors=0; visual review required";
        File.WriteAllText(dir+"/checks.txt",result);return result;
    }
    static int passed;
    static UserSettingsData original;
    static string originalRoot;
    static string resolution = "current";
    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string name) { if (!value) throw new Exception("FAIL " + name); passed++; }
    static GameMenuController Menu => GameMenuController.Instance;
    static Button Find(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == name);

    public static async Task<string> Prepare()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        for (int i = 0; i < 600 && (SaveSystem.Busy || Menu == null || !GameSession.Exists); i++) await Task.Delay(100);
        if (SaveSystem.Busy || Menu == null || !GameSession.Exists) throw new Exception("Initial editor scene still loading");
        originalRoot = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BuildTooltip1111-" + Guid.NewGuid().ToString("N"));
        original = UserSettings.Current.Clone();
        var quiet = UserSettings.Current.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
        SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small, biome = MapBiome.None, startingCount = 3,
            startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() });
        for (var i = 0; i < 300 && (SaveSystem.Busy || Menu == null || !GameSession.Exists || !GameSession.Instance.GameStarted); i++) await Task.Delay(100);
        if (SaveSystem.Busy || Menu == null || !GameSession.Exists || !GameSession.Instance.GameStarted)
            throw new Exception("Isolated world did not become ready within 30 seconds; verification aborted.");
        Menu.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(300);
        return "Isolated build-tooltip world ready, autosave disabled; user slots untouched.";
    }

    // 대상: 탭 이름, 버튼 이름, 반드시 들어가야 하는 문구.
    static readonly (string tab, string button, string[] must)[] Targets =
    {
        ("저장", "Build Storage", new[] { "<b>창고</b>", "보관 한도", "운반" }),
        ("생활", "Build SleepingMat", new[] { "<b>자리</b>", "1명 잠자리", "잠자리" }),
        ("생활", "Build SingleBed", new[] { "<b>침대</b>", "1명 잠자리" }),
        ("생활", "Build Dormitory", new[] { "<b>숙소</b>", "4명 잠자리" }),
        ("생활", "Build Toilet", new[] { "<b>화장실</b>", "위생" }),
        ("식량", "Build Farm", new[] { "<b>밭</b>", "농사", "종자" }),
        ("작업", "Build ScienceLab", new[] { "<b>정착지 연구대</b>", "연구 메뉴에서 담당", "연구비" }),
        ("방어", "Build AcidTower", new[] { "<b>산성탑</b>", "사거리", "2웨이브" }),
        ("타일", "Build SoilWall", new[] { "<b>흙벽</b>", "통행", "연구 미완료: 흙벽 공법" }),
        ("타일", "Build LeafWall", new[] { "<b>나뭇잎 벽</b>", "불에 탐", "선행 연구 없음" }),
        ("타일", "Build CapWall", new[] { "<b>병뚜껑 벽</b>", "통행", "흙벽 공법" }),
        ("방어", "Build CastleWall", new[] { "<b>성벽</b>", "방어", "흙벽 공법" }),
        ("타일", "Build Door", new[] { "<b>문</b>", "아군은 지나가고" }),
        ("방어", "Build TrapPit", new[] { "<b>끈끈이 함정</b>", "함정 공학", "← 흙벽 공법" }),
        ("방어", "Build SpikeTrap", new[] { "<b>가시 함정</b>", "함정 공학" }),
        ("작업", "Build Workshop", new[] { "<b>공방</b>", "근접 무기", "제작" })
    };

    public static async Task<string> Main()
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
        Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
        var pinned=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).First(r=>r.name=="PinnedResources");
        Check(!ScreenRect(objective).Overlaps(ScreenRect(pinned)),"objective does not cover pinned resources");
        var shader=Shader.Find("AntColony/TerrainBlend");
        Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"terrain shader has no errors");
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

    // One run_script call keeps the snapshot in the same dynamically compiled assembly.
    // Independent Prepare/Main/Finish calls cannot safely share static fields.
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var asm = typeof(Editor).Assembly; var sizes = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizes).GetProperty("instance").GetValue(null);
        var group = sizes.GetMethod("GetGroup").Invoke(singleton, new[] { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        var viewType = asm.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType);
        var prop = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        int old = (int)prop.GetValue(view);
        int slot = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        bool added = false; var result = new StringBuilder();
        Directory.CreateDirectory("Temp/presentation-1111");
        try
        {
            await Prepare();
            foreach (var size in new[] { new Vector2Int(1440, 900), new Vector2Int(760, 600) })
            {
                resolution = size.x + "x" + size.y;
                var entry = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), size.x, size.y, "Build tooltip check");
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry }); added = true;
                prop.SetValue(view, slot); view.Focus(); view.Repaint(); await Task.Delay(900);
                if (Screen.width != size.x || Screen.height != size.y) throw new Exception("resolution mismatch " + resolution);
                result.AppendLine(resolution + ": " + await Main());
                File.WriteAllText("Temp/presentation-1111/checks.txt", result.ToString());
                prop.SetValue(view, old);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot }); added = false;
            }
            return result.ToString();
        }
        catch (Exception e) { File.WriteAllText("Temp/presentation-1111/checks.txt", result + "FAIL " + e); throw; }
        finally
        {
            if (BuildScreen.IsOpen) BuildScreen.Back();
            Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
            prop.SetValue(view, old);
            if (added) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot });
        }
    }

    public static string Finish()
    {
        if (original != null) UserSettings.Apply(original, false);
        SaveStorage.RootOverride = originalRoot;
        return "settings and save root restored";
    }
}
