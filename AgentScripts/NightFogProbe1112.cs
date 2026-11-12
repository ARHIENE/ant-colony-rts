using System.IO;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using UnityEngine;
public static class NightFogProbe1112
{
    public static async Task<string> Run()
    {
        float clock = GameSession.Instance.GameSeconds, play = GameSession.Instance.PlaySeconds;
        var weather = WeatherSystem.Instance.CaptureState(); var report = new System.Text.StringBuilder();
        try
        {
            GameSession.Instance.MarkStarted(play, GameCalendar.DaySeconds + 25);
            WeatherSystem.Instance.RestoreState(new WeatherSystem.State { kind = 2, remaining = 300 });
            foreach (var end in new[] { 180f, 500f, 5000f })
            {
                RenderSettings.fogEndDistance = end;
                await Task.Delay(3000);
                report.AppendLine("end=" + end + " fog=" + Shader.GetGlobalVector("unity_FogParams") + " color=" + Shader.GetGlobalVector("unity_FogColor"));
                ScreenCapture.CaptureScreenshot("work/three-waves-review-1112/night-fog-" + end + ".png"); await Task.Delay(400);
            }
            return report.ToString();
        }
        finally { GameSession.Instance.MarkStarted(play, clock); WeatherSystem.Instance.RestoreState(weather); File.WriteAllText("work/three-waves-review-1112/night-fog.txt", report.ToString()); }
    }
}
