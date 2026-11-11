using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using AntColony.Units;
using AntColony.Buildings;

public static class PlayabilityHud1111Checks
{
    static int checks;
    static string log;
    static void Check(bool ok, string label)
    {
        log += (ok ? "PASS " : "FAIL ") + label + "\n";
        File.WriteAllText("Temp/playability-1111/hud-checks.txt", log);
        if (!ok) throw new Exception(label);
        checks++;
    }
    static async Task Frames() { await Task.Delay(800); Canvas.ForceUpdateCanvases(); }
    static async Task Shot(string name)
    {
        await Frames();
        var path = Path.GetFullPath("Temp/playability-1111/" + name + ".png");
        var before = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        ScreenCapture.CaptureScreenshot(path);
        for (int i = 0; i < 100; i++) { await Task.Delay(50); if (File.Exists(path) && File.GetLastWriteTimeUtc(path) > before) return; }
        throw new Exception("capture timeout");
    }
    static void CheckPanel()
    {
        var panel = GameObject.Find("Objective");
        Check(panel != null && panel.activeInHierarchy, "objective stays visible after HUD LateUpdate");
        var text = panel.GetComponentInChildren<Text>();
        Check(text.text.Contains("예상") && text.text.Contains("창고"), "rendered objective includes wave and next action");
        var corners = new Vector3[4]; ((RectTransform)panel.transform).GetWorldCorners(corners);
        Check(corners[0].x >= 0 && corners[0].y >= 0 && corners[2].x <= Screen.width && corners[2].y <= Screen.height, "objective inside viewport");
        Check(text.preferredHeight <= text.rectTransform.rect.height + 1, "objective text fits panel height");
        var wave = GameObject.Find("HudWave");
        Check(wave.GetComponentsInChildren<Text>().Any(t => t.text == "앞당기기"), "advance action label visible");
        Check(wave.GetComponentsInChildren<Text>().Any(t => t.text.Contains("예상")), "wave count visible without tooltip");
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        checks = 0; log = ""; Directory.CreateDirectory("Temp/playability-1111");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var asm = typeof(Editor).Assembly; var sizes = asm.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizes).GetProperty("instance").GetValue(null);
        var group = sizes.GetMethod("GetGroup").Invoke(singleton, new[] { Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        var viewType = asm.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType);
        var prop = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        int old = (int)prop.GetValue(view);
        int slot = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null) + (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null);
        bool added = false;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "PlayabilityHud-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseDowned = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            SaveSystem.NewGame(new NewGameOptions { seed = 261106, mapSize = MapSize.Small, biome = MapBiome.Forest, startingCount = 3 });
            var end = DateTime.UtcNow.AddSeconds(60);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(100);
            Check(!SaveSystem.Busy, "isolated new game loaded");
            GameMenuController.Instance.Resume(); Time.timeScale = 0; await Frames();
            GoalChain.Refresh(Time.unscaledTime);
            Check(GoalChain.CurrentText.Contains("웨이브 1까지") && GoalChain.CurrentText.Contains("예상 " + WaveDefense.Count(1)), "preview from first peace, actual wave count");
            Check(CampaignNextSteps.EarlyGoal(0).Contains("저장 → 창고"), "no storage points to actual build tab");
            Check(!GoalChain.CurrentText.Contains("과학연구소"), "obsolete lab name absent");
            var action = typeof(GoalChain).GetMethod("Action", BindingFlags.Static | BindingFlags.NonPublic);
            Check(((string)action.Invoke(null, new object[] { 2 })).Contains("작업 → 정착지 연구대"), "research action matches build menu");
            foreach (var size in new[] { new Vector2Int(1440, 900), new Vector2Int(760, 600) })
            {
                var entry = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), size.x, size.y, "Playability HUD check");
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry }); added = true;
                prop.SetValue(view, slot); view.Focus(); view.Repaint(); await Frames();
                Check(Screen.width == size.x && Screen.height == size.y, "resolution applied " + size);
                CheckPanel(); await Shot("hud-peace-" + size.x);
                prop.SetValue(view, old); group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot }); added = false;
            }
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.Storage, UnitRole.Worker });
            var building = UnityEngine.Object.Instantiate(template); building.SetActive(false);
            var planned = new GameObject("HUD planned storage fixture").AddComponent<BuildingConstructionSite>(); planned.Initialize(building, 3);
            Check(!CampaignNextSteps.EarlyGoal(0).Contains("입고할 곳 마련"), "planned storage prevents duplicate build advice");
            planned.Complete(null); await Frames();
            Check(CampaignNextSteps.EarlyGoal(1).Contains("작업 지정"), "no natural designations gives designation action");
            var commander = CommanderRoster.Instance.Commanders.First(c => !c.IsDead);
            commander.Health.wounds.Add(new Wound { part = AntColony.Units.BodyPart.ArmL, kind = WoundKind.Bruise, damage = 1 });
            building.GetComponent<BuildingBase>().TakeDamage(10);
            Check(CampaignNextSteps.RecoverySummary(false).Contains("부상 1명 · 파손 1곳"), "recovery counts current injuries and damage");
            GoalChain.Restore(new GoalChain.State { initialized = true, completed = 15 });
            var state = WaveDefense.Instance.Capture(); state.wave = 1; state.inWave = false; state.peaceLeft = WaveDefense.Peace;
            WaveDefense.Instance.Restore(state);
            typeof(WaveDefense).GetMethod("End", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(WaveDefense.Instance, null);
            GoalChain.Refresh(Time.unscaledTime); await Frames();
            var toast = ToastManager.VisibleLines.FirstOrDefault(t => t.Contains("웨이브 1 방어 성공"));
            Check(toast != null && !toast.Contains("원정") && toast.Contains("부상 1명"), "victory notification reports recovery without paused expedition");
            Check(GoalChain.CurrentText.Contains("웨이브 2까지") && GoalChain.CurrentText.Contains("흙벽 공법·함정 공학"), "second peace shows next defense research");
            await Shot("hud-recovery");
            return "PASS " + checks + "\n" + log;
        }
        finally
        {
            prop.SetValue(view, old);
            if (added) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { slot });
            Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
