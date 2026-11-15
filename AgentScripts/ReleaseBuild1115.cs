using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Build.Reporting;
public static class ReleaseBuild1115
{
 public static string Development()=>Queue(true);
 public static string Release()=>Queue(false);
 public static string Both()=>Queue(true)+"\n"+Queue(false);
 static string Queue(bool development)
 {
  foreach(var callback in EditorApplication.delayCall?.GetInvocationList()??Array.Empty<Delegate>())
   if(callback.Method.Module.Assembly.GetName().Name.StartsWith("PipelineRunScript_ReleaseBuild1115")) EditorApplication.delayCall-=(EditorApplication.CallbackFunction)callback;
  string dir=development?"Builds/Windows1115-Validation":"Builds/Windows1115";Directory.CreateDirectory(dir);File.WriteAllText(dir+"/build-result.txt","BUILDING");return Build(development);
 }
 static string Build(bool development)
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play before build");
  string dir=development?"Builds/Windows1115-Validation":"Builds/Windows1115";Directory.CreateDirectory(dir);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=dir+"/AntColony.exe",target=BuildTarget.StandaloneWindows64,options=development?BuildOptions.Development:BuildOptions.None});
  string result=$"{report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} bytes={report.summary.totalSize} elapsed={report.summary.totalTime}";
  File.WriteAllText(dir+"/build-result.txt",result+"\n"+string.Join("\n",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==UnityEngine.LogType.Error||m.type==UnityEngine.LogType.Exception).Select(m=>m.content)));
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception(result);return result;
 }
}
