// Official CLI: set SessionState "AntColony.CheckFile" to an AgentScripts .cs path,
// then unity command eval_file AgentScripts/RunChecks.cs; poll "AntColony.CheckResult".
var path = SessionState.GetString("AntColony.CheckFile", "AgentScripts/CommanderChecks.cs");
if (SessionState.GetString("AntColony.CheckResult", "") == "RUNNING")
    throw new Exception("A check is already running.");
var request = new Unity.Pipeline.Compilation.CompilationRequest
{
    SourceCode = System.IO.File.ReadAllText(path),
    AssemblyName = "AntChecks_" + Guid.NewGuid().ToString("N"),
    EmitDebugInformation = true,
    DocumentPath = System.IO.Path.GetFullPath(path)
};
var compiler = typeof(Unity.Pipeline.Compilation.CompilationRequest).Assembly
    .GetType("Unity.Pipeline.Compilation.RoslynCompilationService");
var result = (Unity.Pipeline.Compilation.CompilationResult)compiler.GetMethod("Compile")
    .Invoke(null, new object[] { request });
if (!result.Success) throw new Exception(string.Join("\n", result.Diagnostics));
var entry = result.Assembly.GetTypes().Select(t => t.GetMethod("Main")).First(m => m != null);
SessionState.SetString("AntColony.CheckResult", "RUNNING");
async System.Threading.Tasks.Task Run()
{
    try
    {
        var value = entry.Invoke(null, null);
        if (value is System.Threading.Tasks.Task<string> task) value = await task;
        SessionState.SetString("AntColony.CheckResult", value?.ToString() ?? "PASS");
    }
    catch (Exception error)
    {
        SessionState.SetString("AntColony.CheckResult", "FAIL: " + error);
    }
}
_ = Run();
return SessionState.GetString("AntColony.CheckResult", "");
