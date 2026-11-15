using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using AntColony.UI;
public static class BuildMaterial1115Preview
{
 public static async Task<string> Main(){
  if(Time.timeScale!=0)throw new Exception("Pause first");
  BuildScreen.Open();await Task.Delay(200);
  Button Find(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b=>b.name==name);
  Find("Tab 식량").onClick.Invoke();Find("Build Hearth").onClick.Invoke();
  await Task.Delay(400);Canvas.ForceUpdateCanvases();
  ScreenCapture.CaptureScreenshot("work/colony-polish-1115/build-default-wood.png");await Task.Delay(400);
  return string.Join("\n",UnityEngine.Object.FindObjectsByType<Text>().Where(t=>t.isActiveAndEnabled&&t.text.Contains("주재료 선택")).Select(t=>t.text));
 }
 public static string Close(){while(BuildScreen.IsOpen)BuildScreen.Back();return "Closed";}
}
