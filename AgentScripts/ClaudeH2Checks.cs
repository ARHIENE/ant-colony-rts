using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;

// Claude H2 패치(c10 드래그 선택·설정창) 검사. Play 모드에서 RecoveryUnitChecks와 같은 방식으로 실행.
public static class ClaudeH2Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = System.IO.Path.Combine(Application.temporaryCachePath, "ClaudeH2-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            var wait = Time.realtimeSinceStartup + 50;
            while (SaveSystem.Busy && Time.realtimeSinceStartup < wait) await Task.Delay(50);
            StartingCandidate[] team;
            do { team = Enumerable.Range(0, 2).Select(StartingCandidate.Roll).ToArray(); } while (StartingCandidate.Missing(team).Length > 0);
            SaveSystem.NewGame(new NewGameOptions { seed = 261025, mapSize = MapSize.Small, startingCount = 2, startingTeam = team });
            wait = Time.realtimeSinceStartup + 50;
            while (SaveSystem.Busy && Time.realtimeSinceStartup < wait) await Task.Delay(50);
            Check(!SaveSystem.Busy, "new game ready");
            var menu = AntColony.UI.GameMenuController.Instance;
            menu.Resume(); Time.timeScale = 0;
            await Task.Yield(); await Task.Yield();

            // 선택 대상은 기존대로 유지하고, 선택만으로 수면이 해제되지 않는지 확인한다.
            var selection = UnityEngine.Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
            var box = typeof(AntColony.Units.SelectionManager).GetMethod("SelectObjectsInRect", Private);
            var cs = CommanderRoster.Instance.Commanders.Where(c => c.IsColonyMember).ToArray();
            Check(cs.Length == 2, "two survivors");
            var cam = UnityEngine.Camera.main;
            foreach (var c in cs) c.transform.position = cs[0].transform.position + (c == cs[0] ? Vector3.zero : Vector3.right); // 둘 다 화면 안
            var whole = new Rect(-10000, -10000, 20000, 20000);
            cs[0].PersonalState.sleep.asleep = true; cs[1].PersonalState.sleep.asleep = false;
            box.Invoke(selection, new object[] { whole, false });
            var sel = selection.GetSelectedObjects();
            Check(sel.Contains(cs[0].GetComponent<AntColony.Units.SelectableObject>()), "box includes sleeper without waking");
            Check(sel.Contains(cs[1].GetComponent<AntColony.Units.SelectableObject>()), "box selects awake survivor");
            Check(cs[0].IsAsleep, "sleeper stays asleep");
            cs[1].PersonalState.sleep.asleep = true;
            box.Invoke(selection, new object[] { whole, false });
            Check(selection.GetSelectedObjects().Count == 2, "box of only sleepers selects them");
            foreach (var c in cs) c.PersonalState.sleep.asleep = false;
            selection.ClearSelection();

            // c10: 드래그 시작점을 땅에 고정 → 카메라가 움직이면 화면 위치도 따라간다.
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var args = new object[] { center, null };
            Check((bool)typeof(AntColony.Units.SelectionManager).GetMethod("TryGroundPoint", Private).Invoke(selection, args), "ground point under screen center");
            var anchor = (Vector3)args[1];
            var before = (Vector2)cam.WorldToScreenPoint(anchor);
            Check((before - center).magnitude < 2f, "anchor reprojects to start " + before);
            cam.transform.position += cam.transform.right * 3f;
            var after = (Vector2)cam.WorldToScreenPoint(anchor);
            Check((after - before).magnitude > 5f, "anchored start follows camera pan");
            cam.transform.position -= cam.transform.right * 3f;

            // 설정: 화면을 열고 품질 '기본' 복귀·초기화 시 카메라 반영·목록 위치 유지.
            var mc = typeof(AntColony.UI.GameMenuController);
            var defaultQuality = (int)typeof(UserSettingsData).GetField("defaultQuality", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Check(defaultQuality >= 0, "default quality remembered");
            var q = UserSettings.Current.Clone(); q.quality = QualitySettings.names.Length - 1 == defaultQuality ? 0 : QualitySettings.names.Length - 1;
            UserSettings.Apply(q, false); Check(QualitySettings.GetQualityLevel() == q.quality, "quality applied");
            q.quality = -1; UserSettings.Apply(q, false); Check(QualitySettings.GetQualityLevel() == defaultQuality, "'기본' restores project quality");

            var camCtrl = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
            var pan = typeof(AntColony.Camera.IsometricCameraController).GetField("panSpeed", Private);
            var slow = UserSettings.Current.Clone(); slow.cameraPanSpeed = 10; UserSettings.Apply(slow, false); camCtrl.ApplySettings(10, slow.edgeScrollThickness);
            mc.GetField("settingsTab", Private).SetValue(menu, 4);
            mc.GetMethod("Settings", Private).Invoke(menu, null);
            await Task.Yield();
            menu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Reset Settings").onClick.Invoke();
            Check(Mathf.Approximately((float)pan.GetValue(camCtrl), new UserSettingsData().cameraPanSpeed), "reset settings reaches camera");

            mc.GetField("settingsTab", Private).SetValue(menu, 2);
            mc.GetField("settingsScroll", Private).SetValue(menu, .4f);
            mc.GetMethod("Settings", Private).Invoke(menu, null);
            await Task.Yield();
            var scroll = menu.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().First(s => s.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name.StartsWith("Key ")));
            Check(Mathf.Abs(scroll.verticalNormalizedPosition - .4f) < .05f, "settings list keeps scroll " + scroll.verticalNormalizedPosition);
            menu.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.name == "화면 가장자리 스크롤 1").onClick.Invoke(); // 끄기
            await Task.Yield();
            Check(UserSettings.Current.edgeScrollThickness == 0, "edge scroll off saved");
            scroll = menu.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().First(s => s.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name.StartsWith("Key ")));
            Check(Mathf.Abs(scroll.verticalNormalizedPosition - .4f) < .05f, "scroll kept after option click");
            menu.Resume();
            return $"ClaudeH2Checks PASS {checks}";
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>()?.ApplySettings(settings.cameraPanSpeed, settings.edgeScrollThickness); Time.timeScale = 0; }
    }
}
