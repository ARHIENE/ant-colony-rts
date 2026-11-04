"""Unity를 실행하지 않는 J1 Roslyn 컴파일·순수 계산 검사. Python 표준 라이브러리만 사용."""
import json
from pathlib import Path
import shutil
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")

ROOT = Path(__file__).resolve().parents[1]
ORIGINAL = Path("E:/Git/ant")
SDK = Path("E:/unity/6000.5.8f1/Editor/Data/DotNetSdk")
DOTNET = SDK / "dotnet.exe"
CSC = SDK / "sdk/8.0.318/Roslyn/bincore/csc.dll"
BEE = ORIGINAL / "Library/Bee/artifacts/1900b0aE.dag"
OUT = ROOT / "Temp/Review1029"
OUT.mkdir(parents=True, exist_ok=True)


def compile_project(editor=False):
    name = "Assembly-CSharp-Editor" if editor else "Assembly-CSharp"
    output = "J1.Checks" if editor else "J1.Game"
    lines = (BEE / (name + ".rsp")).read_text(encoding="utf-8-sig").splitlines()
    sources, options = [], []
    for line in lines:
        if line.strip().strip('"').endswith(".cs"):
            source = line.strip().strip('"')
            if editor or not source.startswith("Assets/_Project/"):
                local = ROOT / source
                # 원본의 무시된 서드파티 소스는 읽기만 한다. 프로젝트 코드는 반드시 worktree 것.
                sources.append(local if local.exists() else ORIGINAL / source)
        elif not line.startswith(("-out:", "-refout:", "-analyzer:", "-generatedfilesout:")):
            if not (editor and line.startswith("-r:") and "/Assembly-CSharp." in line):
                options.append(line.replace('"Library/', '"E:/Git/ant/Library/'))
    if editor:
        sources += sorted((ROOT / "AgentScripts").glob("Review1029*Checks.cs"))
        options.append(f'-r:"{OUT / "J1.Game.dll"}"')
    else:
        sources += sorted((ROOT / "Assets/_Project/Scripts").rglob("*.cs"))
    options.append(f'-out:"{OUT / (output + ".dll")}"')
    options += [f'"{p}"' for p in dict.fromkeys(sources)]
    response = OUT / (output + ".rsp")
    response.write_text("\n".join(options), encoding="utf-8")
    result = subprocess.run([str(DOTNET), "exec", str(CSC), "-nologo", "@" + str(response)], cwd=ROOT, capture_output=True, encoding="utf-8", errors="replace")
    (OUT / (output + "-compile.txt")).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(f"{output}: exit={result.returncode}, sources={len(sources)}, warnings={result.stdout.count('warning CS')}")
    if result.returncode:
        print(result.stdout + result.stderr)
        raise SystemExit(result.returncode)
    return options


options = compile_project()
compile_project(True)
references = {}
for line in options:
    if line.startswith('-r:"'):
        path = Path(line[4:-1])
        references[path.stem] = str(path)
(OUT / "references.json").write_text(json.dumps(references), encoding="utf-8")
runner = OUT / "Runner.cs"
runner.write_text('''using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
class Runner {
 static async Task Main() {
  var dir = AppContext.BaseDirectory;
  var refs = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(dir,"references.json")));
  AssemblyLoadContext.Default.Resolving += (ctx,name) => refs.TryGetValue(name.Name,out var path) && File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
  var checks = Assembly.LoadFrom(Path.Combine(dir,"J1.Checks.dll"));
  foreach (var name in new[]{"Review1029EconomyChecks","Review1029FoodChecks","Review1029IndustryTradeChecks"}) {
   var task = (Task<string>)checks.GetType(name).GetMethod("Main").Invoke(null,null);
   Console.WriteLine(await task);
  }
  Console.WriteLine("Unity scene/save/UI checks were NOT executed.");
 }
}
''', encoding="utf-8")
refs = SDK / "packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0"
response = OUT / "Runner.rsp"
response.write_text("\n".join(["-target:exe", f'-out:"{OUT / "Runner.dll"}"', f'"{runner}"']
                              + [f'-r:"{p}"' for p in sorted(refs.glob("*.dll"))]), encoding="utf-8")
subprocess.run([str(DOTNET), "exec", str(CSC), "-nologo", "@" + str(response)], cwd=ROOT, check=True)
(OUT / "Runner.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": "8.0.21"}}}), encoding="utf-8")
shutil.copy2("E:/unity/6000.5.8f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll", OUT)
result = subprocess.run([str(DOTNET), str(OUT / "Runner.dll")], cwd=ROOT, capture_output=True, encoding="utf-8", errors="replace")
(OUT / "offline-results.txt").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr)
raise SystemExit(result.returncode)
