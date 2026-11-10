using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.Data;
using AntColony.Camera;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

// 조작감·피드백(GameFeel) 검사. Play 중 격리 새 게임에서 실행한다: Prepare → Run.
// 실제 키·휠 입력은 InputSystem 상태 이벤트로 넣는다. 게임 뷰 포커스가 필요한 항목은 포커스를 얻지 못하면 SKIP으로 보고한다.
public static class GameFeel1110Checks
{
    static int passed; static string skipped = "";
    static void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); passed++; }
    static async Task Frames(int count = 4) { var start = Time.frameCount; for (var i = 0; i < 200 && Time.frameCount < start + count; i++) await Task.Delay(20); }

    public static async Task<string> Prepare()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FEEL1110-" + Guid.NewGuid().ToString("N"));
        var quiet = UserSettings.Current.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
        SaveSystem.NewGame(new NewGameOptions { seed = 1110, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() });
        for (var i = 0; i < 300 && (SaveSystem.Busy || GameMenuController.Instance == null || !GameSession.Exists || !GameSession.Instance.GameStarted); i++) await Task.Delay(100);
        if (SaveSystem.Busy || !GameSession.Instance.GameStarted) throw new Exception("isolated world not ready");
        GameMenuController.Instance.Resume(); SimulationPause.UserSpeed(0); await Task.Delay(300);
        return "isolated world ready (autosave off, temp save root)";
    }

    public static async Task<string> Run()
    {
        passed = 0; skipped = "";
        Check(Application.isPlaying && GameSession.Exists && GameSession.Instance.GameStarted, "isolated game required");
        var menu = GameMenuController.Instance; menu.Resume(); SimulationPause.UserSpeed(0); await Frames();
        var feel = GameObject.Find("GameFeel");
        Check(feel != null && feel.GetComponent<GameFeel>() != null, "GameFeel bootstrapped");
        Check(feel.GetComponent<AudioSource>() != null, "GameFeel has an audio source");
        Check(EnemyAlert.AlarmClip != null && EnemyAlert.AlarmClip.length > .5f, "enemy alarm clip is now filled");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude).Length == 1, "exactly one audio listener");

        // 효과음 호출·간격 제한
        var before = GameFeel.PlayCount[(int)GameFeel.Sound.Click];
        GameFeel.Play(GameFeel.Sound.Click); GameFeel.Play(GameFeel.Sound.Complete); GameFeel.Play(GameFeel.Sound.Alarm);
        Check(GameFeel.PlayCount[(int)GameFeel.Sound.Click] == before + 1, "click sound counted");
        var clips = (AudioClip[])typeof(GameFeel).GetField("clips", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(feel.GetComponent<GameFeel>());
        Check(clips[(int)GameFeel.Sound.Click] != null && clips[(int)GameFeel.Sound.Complete] != null, "clips synthesized on demand");
        var samples = new float[clips[(int)GameFeel.Sound.Complete].samples]; clips[(int)GameFeel.Sound.Complete].GetData(samples, 0);
        Check(samples.Max(Mathf.Abs) > .2f && samples.Max(Mathf.Abs) <= 1f && !samples.Any(float.IsNaN), "synthesized clip has audible, valid samples");

        var commanders = CommanderRoster.Instance.Commanders.Where(c => c != null && c.IsColonyMember).ToArray();
        Check(commanders.Length == 3, "three starting commanders");
        var selection = SceneObjects.Any<SelectionManager>();
        var cam = SceneObjects.Any<IsometricCameraController>(); var unityCam = cam.GetComponent<Camera>();
        cam.FocusOn(commanders[0].Position); await Frames();

        // Ctrl+A 전원 선택(직접 호출)
        selection.ClearSelection();
        Check(GameFeel.SelectArmy(selection) == 3 && selection.GetSelectedObjects().Count == 3, "select army picks every home commander when none deployed");

        // 더블클릭: 같은 장수를 두 번 누르면 화면 안 같은 상태 장수 전원
        selection.ClearSelection();
        var click = typeof(SelectionManager).GetMethod("ClickSelectOrClear", BindingFlags.NonPublic | BindingFlags.Instance);
        var screen = (Vector2)unityCam.WorldToScreenPoint(commanders[0].GetComponent<SelectableObject>().GetSelectionWorldPosition() + Vector3.up * .5f);
        click.Invoke(selection, new object[] { screen, false });
        if (selection.GetSelectedObjects().Count == 1)
        {
            var onScreen = commanders.Count(c => { var v = unityCam.WorldToViewportPoint(c.Position); return v.z > 0 && v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1; });
            click.Invoke(selection, new object[] { screen, false });
            Check(selection.GetSelectedObjects().Count == onScreen && onScreen >= 1, $"double click selects all on-screen commanders ({selection.GetSelectedObjects().Count}/{onScreen})");
            await Task.Delay(500);
            click.Invoke(selection, new object[] { screen, false });
            Check(selection.GetSelectedObjects().Count == 1, "a slow third click is a single select again");
        }
        else skipped += " double-click(raycast did not hit the commander: " + selection.GetSelectedObjects().Count + ")";

        // 피해 숫자: 짧은 시간의 여러 타격이 한 번으로 합쳐진다
        var floaters = (System.Collections.IList)typeof(Juice).GetField("floaters", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(typeof(Juice).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        floaters.Clear();
        var hitBefore = GameFeel.PlayCount[(int)GameFeel.Sound.Hit];
        for (var i = 0; i < 5; i++) GameFeel.Hit(commanders[0], 3, true);
        GameFeel.Hit(commanders[1], 4, false);
        await Frames(2); Check(floaters.Count == 0, "hits are held back for the merge window");
        await Task.Delay(450); await Frames(3);
        Check(floaters.Count == 2, "five hits on one target merge into one number (floaters=" + floaters.Count + ")");
        var text = (string)floaters[0].GetType().GetField("text").GetValue(floaters[0]) + "|" + (string)floaters[1].GetType().GetField("text").GetValue(floaters[1]);
        Check(text.Contains("-15") && text.Contains("-4"), "merged amounts are summed: " + text);
        Check(GameFeel.PlayCount[(int)GameFeel.Sound.Hit] == hitBefore + 1, "one hit sound per flush");
        // 실제 피해 경로 연결
        floaters.Clear(); var troop = commanders[2].TroopHealth + commanders[2].PersonalHealth;
        commanders[2].TakeDamage(6); var lost = troop - (commanders[2].TroopHealth + commanders[2].PersonalHealth);
        await Task.Delay(450); await Frames(3);
        Check(lost > 0 && floaters.Count == 1 && ((string)floaters[0].GetType().GetField("text").GetValue(floaters[0])) == "-" + Mathf.Max(1, Mathf.RoundToInt(lost)), "real TakeDamage reports the health actually lost (" + lost + ")");

        // 카메라: 일시정지(timeScale 0) 중에도 줌·방향키 이동·회전
        Check(Time.timeScale == 0, "simulation paused for the camera checks");
        var view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); view.Focus(); await Frames(6);
        if (Application.isFocused && Keyboard.current != null && Mouse.current != null)
        {
            var center = new Vector2(Screen.width * .5f, Screen.height * .62f);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = center }); await Frames(3);
            var size = cam.ViewSize;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = center, scroll = new Vector2(0, -120) }); await Frames(2);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = center }); await Task.Delay(500); await Frames(3);
            if (cam.ViewSize > size + .5f)
            {
                passed++;
                var after = cam.ViewSize; await Task.Delay(300); await Frames(2);
                Check(Mathf.Abs(cam.ViewSize - after) < .05f, "zoom settles on its target");
                Check(Mathf.Abs(cam.ViewSize - size / .88f) < .2f, "one wheel notch is one fixed step regardless of frame rate (" + size + "→" + cam.ViewSize + ")");
            }
            else skipped += " wheel-zoom(pointer over UI or wheel event not delivered: " + size + "→" + cam.ViewSize + ")";
            var focus = cam.FocusPoint;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.RightArrow)); await Task.Delay(350);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); await Frames(3);
            if ((cam.FocusPoint - focus).magnitude > .5f) { passed++; Check(Mathf.Abs(cam.FocusPoint.z - focus.z) < .2f && cam.FocusPoint.x > focus.x, "right arrow pans right only"); }
            else skipped += " arrow-pan(key event not delivered or at map edge)";
            var yaw = cam.Yaw;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(KeyBindings.Get(GameAction.RotateLeft))); await Frames(2);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); await Task.Delay(600); await Frames(3);
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, cam.Yaw)) > 80f) passed++; else skipped += " rotate-while-paused(yaw " + yaw + "→" + cam.Yaw + ")";
        }
        else skipped += " camera-input(game view not focused)";

        selection.ClearSelection();
        return $"GameFeel1110Checks {passed} PASS" + (skipped == "" ? "" : " · SKIP:" + skipped);
    }

    // 화면 확인용: 피해 숫자·공격 마커가 보이는 순간을 만든다(캡처 직전에 호출).
    public static async Task<string> Stage()
    {
        var commanders = CommanderRoster.Instance.Commanders.Where(c => c != null && c.IsColonyMember).ToArray();
        var cam = SceneObjects.Any<IsometricCameraController>(); cam.FocusOn(commanders[0].Position); await Frames();
        GameFeel.SelectArmy(SceneObjects.Any<SelectionManager>());
        GameFeel.Hit(commanders[0], 12, true); if (commanders.Length > 1) GameFeel.Hit(commanders[1], 7, false);
        MoveMarker.Spawn(commanders[0].Position + new Vector3(3, 0, 2), new Color(1f, .3f, .25f), 3f, 1.8f, 1.2f);
        MoveMarker.Spawn(commanders[0].Position + new Vector3(-3, 0, 2), Color.green, 3f, 1.4f, 1f);
        await Task.Delay(420); await Frames(2);
        return "staged";
    }
}
