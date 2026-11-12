using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Map;
using UnityEngine;
public static class FogProbe1112
{
    public static async Task<string> Run()
    {
        var saved = WeatherSystem.Instance.CaptureState(); var report = new System.Text.StringBuilder();
        var start = RenderSettings.fogStartDistance; var end = RenderSettings.fogEndDistance;
        try
        {
            WeatherSystem.Instance.RestoreState(new WeatherSystem.State { kind = (int)WeatherKind.Storm, remaining = 300 });
            await Task.Delay(400);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                report.AppendLine(c.name + " pos=" + c.transform.position + " ortho=" + c.orthographic + " size=" + c.orthographicSize + " clip=" + c.nearClipPlane + "/" + c.farClipPlane);
            report.AppendLine("fog=" + RenderSettings.fogMode + " density=" + RenderSettings.fogDensity + " range=" + RenderSettings.fogStartDistance + "/" + RenderSettings.fogEndDistance + " params=" + Shader.GetGlobalVector("unity_FogParams"));
            foreach (var mode in new[] { FogMode.Exponential, FogMode.Linear })
            {
                RenderSettings.fogMode = mode; RenderSettings.fogStartDistance = 15; RenderSettings.fogEndDistance = 180;
                await Task.Delay(8000); ScreenCapture.CaptureScreenshot("work/three-waves-review-1112/fog-probe-" + mode + ".png"); await Task.Delay(300);
            }
            RenderSettings.fog = false; await Task.Delay(400); ScreenCapture.CaptureScreenshot("work/three-waves-review-1112/fog-probe-Off.png"); await Task.Delay(300);
            return report.ToString();
        }
        finally { WeatherSystem.Instance.RestoreState(saved); RenderSettings.fogStartDistance = start; RenderSettings.fogEndDistance = end; File.WriteAllText("work/three-waves-review-1112/fog-probe.txt", report.ToString()); }
    }
}
