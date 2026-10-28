using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using UnityEngine;
using UnityEngine.UI;

public static class Settings1028Checks
{
    public static async Task<string> UI()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        var saved = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Settings1028-" + Guid.NewGuid().ToString("N"));
        var count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Settings UI FAIL " + text); count++; }
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var menu = AntColony.UI.GameMenuController.Instance;
        var tab = menu.GetType().GetField("settingsTab", flags);
        var show = menu.GetType().GetMethod("Settings", flags);
        void Open(int n) { tab.SetValue(menu, n); show.Invoke(menu, null); Canvas.ForceUpdateCanvases(); }
        void Click(string name) => menu.GetComponentsInChildren<Button>(true).First(b => b.name == name && b.gameObject.activeInHierarchy).onClick.Invoke();
        try
        {
            var quiet = saved.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            for (var i = 0; i < 5; i++) { Open(i); Check(menu.ScreenName == "Settings", "tab " + i); await Task.Yield(); }
            var custom = UserSettings.Current.Clone(); custom.resolutionWidth = 1280; custom.resolutionHeight = 720;
            custom.masterVolume = .3f; custom.edgeScrollThickness = 0; UserSettings.Apply(custom, false);
            Open(0); Click("Native Resolution");
            Check(UserSettings.Current.resolutionWidth == 0 && UserSettings.Current.resolutionHeight == 0, "native resolution button");
            Check(UserSettings.Current.masterVolume == .3f && UserSettings.Current.edgeScrollThickness == 0, "native leaves other settings intact");
            Open(2); var camera = UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>();
            var speed = (float)camera.GetType().GetField("panSpeed", flags).GetValue(camera);
            Click("카메라 이동 속도 Next");
            Check((float)camera.GetType().GetField("panSpeed", flags).GetValue(camera) == UserSettings.Current.cameraPanSpeed, "camera setting applied");
            Click("화면 가장자리 스크롤 0");
            Check((float)camera.GetType().GetField("edgeScrollThickness", flags).GetValue(camera) == 18, "edge scrolling applied");
            var keys = UserSettings.Current.Clone(); var oldRoster = KeyBindings.Get(keys, GameAction.Roster); var oldBuild = KeyBindings.Get(keys, GameAction.Build);
            KeyBindings.Bind(keys, GameAction.Roster, oldBuild);
            Check(KeyBindings.Get(keys, GameAction.Roster) == oldBuild && KeyBindings.Get(keys, GameAction.Build) == oldRoster, "duplicate key swaps");
            var prior = keys.Clone(); KeyBindings.Bind(keys, GameAction.Roster, UnityEngine.InputSystem.Key.Space);
            Check(KeyBindings.Get(keys, GameAction.Roster) == KeyBindings.Get(prior, GameAction.Roster), "fixed keys protected");
            UserSettings.Apply(keys); UserSettings.ResetCacheForReload(); UserSettings.Load();
            Check(JsonUtility.ToJson(UserSettings.Current) == JsonUtility.ToJson(keys), "settings file roundtrip");
            Open(4); Click("Reset Settings");
            Check(UserSettings.Current.cameraPanSpeed == 25 && UserSettings.Current.edgeScrollThickness == 18, "settings reset");
            Check((float)camera.GetType().GetField("panSpeed", flags).GetValue(camera) == 25, "reset camera applied");
            Check(QualitySettings.vSyncCount == 1 && AudioListener.volume == 1, "reset rendering and volume applied");
            var result = "PASS " + count + " settings tabs/buttons/camera/keys/reset/persistence checks";
            File.WriteAllText("Temp/settings1028-result.txt", result); return result;
        }
        finally
        {
            menu.Resume(); SaveStorage.RootOverride = root; UserSettings.Apply(saved, false);
            UnityEngine.Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>()?.ApplySettings(saved.cameraPanSpeed, saved.edgeScrollThickness);
            Time.timeScale = 0;
        }
    }
    public static string Runtime()
    {
        var level = QualitySettings.GetQualityLevel();
        var sync = QualitySettings.vSyncCount; var fps = Application.targetFrameRate; var volume = AudioListener.volume;
        var count = 0;
        try
        {
            foreach (var vsync in new[] { false, true })
                for (var q = 0; q < QualitySettings.names.Length; q++)
                {
                    // Force an actual preset change so its vSync override is exercised.
                    QualitySettings.SetQualityLevel((q + 1) % QualitySettings.names.Length, true);
                    var data = new UserSettingsData { quality = q, vSync = vsync, frameLimit = 30, masterVolume = .4f };
                    data.ApplyRuntime();
                    if (QualitySettings.GetQualityLevel() != q || QualitySettings.vSyncCount != (vsync ? 1 : 0)
                        || Application.targetFrameRate != (vsync ? -1 : 30) || Mathf.Abs(AudioListener.volume - .4f) > .001f)
                        throw new Exception("Settings FAIL preset=" + q + " selectedVSync=" + vsync + " actualVSync=" + QualitySettings.vSyncCount);
                    count++;
                }
            return "PASS " + count + " quality/vSync/frame limit/volume combinations";
        }
        finally
        {
            QualitySettings.SetQualityLevel(level, true); QualitySettings.vSyncCount = sync;
            Application.targetFrameRate = fps; AudioListener.volume = volume;
        }
    }
}
