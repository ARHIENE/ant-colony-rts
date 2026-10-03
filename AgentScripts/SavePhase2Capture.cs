using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// SAVE 전용. 폐기할 Play 세션에서 실제 HUD·과학·패배 화면을 준비한다.
public static class SavePhase2Capture
{
    public static async Task<string> Main(string view)
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        if (view == "hud")
        {
            var settings = UserSettings.Current.Clone();
            settings.autoSaveEnabled = false;
            UserSettings.Apply(settings, false);
            SaveSystem.NewGame(new NewGameOptions { seed = 261001, mapSize = MapSize.Small });
            var deadline = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(50);
            if (SaveSystem.Busy) throw new Exception("Scene not ready");
        }
        await Task.Delay(500);
        GameSession.Instance.MarkStarted(0, 20);
        GameMenuController.Instance.Resume();
        Time.timeScale = 0;
        foreach (var commander in CommanderRoster.Instance.Commanders)
        {
            commander.SetJobEnabled(CommanderJobs.All, false);
            commander.CommandStop();
        }
        if (view == "hud")
        {
            Object.FindAnyObjectByType<RosterBar>().Click(0);
            Object.FindAnyObjectByType<AntColony.Camera.IsometricCameraController>().FocusOn(CommanderRoster.Instance.Commanders[0].Position);
            Camera.main.orthographicSize = 18;
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == "Materials").onClick.Invoke();
        }
        else if (view == "science")
        {
            var lab = new GameObject("과학연구소 T1").AddComponent<ScienceLab>();
            lab.transform.position = new Vector3(1000, 0, 1000);
            GameMenuController.Instance.Science();
            await Task.Delay(200);
            Canvas.ForceUpdateCanvases();
            var button = GameMenuController.Instance.GetComponentsInChildren<Button>().First(b => b.name.StartsWith("T1 낚시"));
            var scroll = button.GetComponentInParent<ScrollRect>();
            scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(-button.GetComponent<RectTransform>().anchoredPosition.y - scroll.viewport.rect.height / 2, 0, scroll.content.rect.height - scroll.viewport.rect.height));
        }
        else if (view == "defeat")
        {
            foreach (var commander in CommanderRoster.Instance.Commanders) commander.PersonalState.dead = true;
        }
        else throw new ArgumentException("hud, science or defeat required");
        await Task.Delay(300);
        return "Ready: " + view + "; screen=" + GameMenuController.Instance.ScreenName;
    }
}
