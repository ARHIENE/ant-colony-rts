using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using AntColony.Core;
public static class BossFraming1115
{
 public static async Task<string> Main(){UnityEditor.EditorApplication.isPaused=false;Time.timeScale=0;var b=RocketCampaign.Instance.MothershipBoss;var c=RocketCampaign.Instance.MothershipCrew.First(x=>x&&!x.IsEmbarked);var camera=Camera.main;var control=camera.GetComponent<AntColony.Camera.IsometricCameraController>();typeof(AntColony.Camera.IsometricCameraController).GetMethod("RestoreView",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(control,new object[]{(c.Position+b.Position)*.5f+Vector3.up*3,0f,14f});await Task.Delay(300);ScreenCapture.CaptureScreenshot("work/campaign-complete-1115/03c-boss-framing.png");await Task.Delay(300);return $"focus={control.FocusPoint} boss={b.Position} crew={c.Position}";}
}
