using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.UI;
public static class BuilderPickerCheck {
public static async Task<string> Main() {
var commanders=UnityEngine.Object.FindObjectsByType<AntColony.Units.CommanderAnt>(FindObjectsSortMode.None);var enabled=commanders.Select(c=>c.enabled).ToArray();
try{
Time.timeScale=0; foreach(var c in commanders)c.enabled=false;
AntColony.UI.GameMenuController.Instance.Resume();BuildScreen.OpenDormitory();
var screen=UnityEngine.Object.FindAnyObjectByType<BuildScreen>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var entry=typeof(BuildScreen).GetField("picking",flags).GetValue(screen);
try {typeof(BuildScreen).GetMethod("RefreshPicker",flags).Invoke(screen,new[]{entry});await Task.Delay(100);return "PASS zero available builders; unassigned option only";}
catch(TargetInvocationException e){return "FAIL zero builders: "+e.InnerException.GetType().Name+" "+e.InnerException.Message;}
}finally{BuildScreen.Back();BuildScreen.Back();for(int i=0;i<commanders.Length;i++)commanders[i].enabled=enabled[i];}
}}
