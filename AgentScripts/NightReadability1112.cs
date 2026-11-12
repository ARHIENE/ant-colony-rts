using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using UnityEngine;

// Run only after the continuous play owner returns Unity. Paused visual A/B,
// restores the live scene's clock, weather and lighting in finally.
public static class NightReadability1112
{
    const string Dir = "work/three-waves-review-1112/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<string> Compare()
    {
        if (!Application.isPlaying || !GameSession.Exists || !GameSession.Instance.GameStarted
            || Time.timeScale != 0) throw new Exception("An isolated paused game is required");
        Directory.CreateDirectory(Dir);
        var lighting = UnityEngine.Object.FindAnyObjectByType<DayNightLighting>();
        if (lighting == null || !lighting.enabled) throw new Exception("Enabled DayNightLighting required");
        var sun = (Light)typeof(DayNightLighting).GetField("sun", Private).GetValue(lighting);
        if (sun == null) throw new Exception("Directional light missing");
        var dayColor = (Color)typeof(DayNightLighting).GetField("dayColor", Private).GetValue(lighting);
        var dayPower = (float)typeof(DayNightLighting).GetField("dayIntensity", Private).GetValue(lighting);
        var dayAmbient = (float)typeof(DayNightLighting).GetField("dayAmbient", Private).GetValue(lighting);
        var savedClock = GameSession.Instance.GameSeconds;
        var savedPlay = GameSession.Instance.PlaySeconds;
        var weather = WeatherSystem.Instance.CaptureState();
        bool wasEnabled = lighting.enabled;
        var report = new System.Text.StringBuilder();
        async Task Capture(string name)
        {
            // First weather switch can compile a new fog shader variant in Editor.
            await Task.Delay(8000);
            ScreenCapture.CaptureScreenshot(Dir + name + ".png");
            await Task.Delay(400);
            report.AppendLine(name + " light=" + sun.intensity + " color=" + sun.color
                + " ambient=" + RenderSettings.ambientIntensity + " mode=" + RenderSettings.ambientMode);
        }
        try
        {
            foreach (var kind in new[] { WeatherKind.Clear, WeatherKind.Storm })
            {
                lighting.enabled = true;
                GameSession.Instance.MarkStarted(savedPlay, GameCalendar.DaySeconds + 25);
                WeatherSystem.Instance.RestoreState(new WeatherSystem.State { kind = (int)kind, remaining = 300 });
                await Capture("night-" + kind + "-current");
                lighting.enabled = false;
                // Recreate the previous formula in this same frozen scene. "current"
                // above is rendered by the actual installed production component.
                sun.color = dayColor * DayNightLighting.SeasonTint * new Color(.35f, .45f, .8f);
                sun.intensity = dayPower * .25f * DayNightLighting.SeasonIntensity;
                RenderSettings.ambientIntensity = dayAmbient * .4f * Mathf.Lerp(1, DayNightLighting.SeasonIntensity, .5f);
                await Capture("night-" + kind + "-previous-formula");
            }
            lighting.enabled = true;
            GameSession.Instance.MarkStarted(savedPlay, 30);
            WeatherSystem.Instance.RestoreState(new WeatherSystem.State { kind = 0, remaining = 300 });
            await Capture("day-current");
            File.WriteAllText(Dir + "lighting-comparison.txt", report.ToString());
            return report.ToString();
        }
        finally
        {
            GameSession.Instance.MarkStarted(savedPlay, savedClock);
            WeatherSystem.Instance.RestoreState(weather);
            lighting.enabled = wasEnabled;
            await Task.Delay(200);
            typeof(DayNightLighting).GetMethod("LateUpdate", Private).Invoke(lighting, null);
        }
    }
}
