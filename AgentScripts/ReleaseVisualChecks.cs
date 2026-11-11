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
public static class ReleaseVisualChecks
{
    // Read-only follow-up in another verifier's current Play scene: no selection,
    // camera, pause, scene, save-root or settings changes.
    public static async Task<string> Current()
    {
        var dir="Temp/release-visual-pass/final"; Directory.CreateDirectory(dir);
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
        SaveSystem.NewGame(new NewGameOptions { seed = 261111, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 3,
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
        var report=new StringBuilder("Dressing objects: "+decorations.Length+"\n");
        var terrain=AntColony.Map.MapGenerator.Active;
        var center=(Vector3)typeof(AntColony.Map.MapGenerator).GetField("flatCenter",Flags).GetValue(terrain);
        foreach(var decoration in decorations)
        {
            var delta=decoration.GetComponentsInChildren<Renderer>()[0].bounds.center-center;
            if(new Vector2(delta.x,delta.z).magnitude < 12f)throw new Exception("Dressing inside build core");
            if(decoration.GetComponentsInChildren<AntColony.World.ResourceNode>().Length>0)throw new Exception("Dressing changes stock");
        }
        report.AppendLine("Dressing: no enabled colliders, no resource components, central core clear");

        BuildScreen.Open(); await Task.Delay(500);
        Find("Tab 작업").onClick.Invoke();await Task.Delay(300);
        var button=Find("Build ScienceLab");
        ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
        await Task.Delay(500);
        ScreenCapture.CaptureScreenshot(dir+"/build-work-"+resolution+".png"); await Task.Delay(400);
        foreach(var t in button.GetComponentsInChildren<Text>())if(t.enabled)report.AppendLine("ScienceLab physical font: "+t.fontSize*t.transform.lossyScale.x+" preferred="+t.preferredHeight+" height="+t.rectTransform.rect.height);
        var scroll = button.GetComponentInParent<ScrollRect>();
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
        Find("Tab 생활").onClick.Invoke(); await Task.Delay(300);
        ScreenCapture.CaptureScreenshot(dir+"/build-life-"+resolution+".png"); await Task.Delay(400);
        await CheckBuildingHelp(report);
        BuildScreen.Back();
        if(resolution == "1440x900")
        {
            SimulationPause.UserSpeed(1); await Task.Delay(12000); SimulationPause.UserSpeed(0);
            ScreenCapture.CaptureScreenshot(dir+"/gameplay-running-"+resolution+".png"); await Task.Delay(400);
        }
        File.WriteAllText(dir+"/state-"+resolution+".txt",report.ToString());
        return report.ToString();
    }

    static async Task CheckBuildingHelp(StringBuilder report)
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
        Directory.CreateDirectory("Temp/release-visual-pass");
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
                File.WriteAllText("Temp/release-visual-pass/checks.txt", result.ToString());
                prop.SetValue(view, old);
                group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot }); added = false;
            }
            return result.ToString();
        }
        catch (Exception e) { File.WriteAllText("Temp/release-visual-pass/checks.txt", result + "FAIL " + e); throw; }
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
