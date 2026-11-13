using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
public static class Motion1113View
{
 public static async Task<string> Capture(string label="bed")
 {
  AntColony.UI.SimulationPause.UserSpeed(1);await Task.Delay(600);AntColony.UI.SimulationPause.UserSpeed(0);
  Directory.CreateDirectory("work/motion-actions-1113/screens");
  ScreenCapture.CaptureScreenshot("work/motion-actions-1113/screens/"+label+".png");await Task.Delay(300);
  return string.Join("\n",CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).Select(c=>c.name+" asleep="+c.IsAsleep+" pose="+c.TrySleepingPose(out _,out _)+" source="+c.GetComponent<MeshyNativeMotionPlayer>().PlayingMotion));
 }
}
