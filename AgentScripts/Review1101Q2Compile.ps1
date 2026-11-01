param([switch]$OfflineChecks)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($project -ne 'E:\Git\ant-wt\Q2') { throw 'Q2 worktree required' }
$scratch = 'C:/TESTTEMP/claude/E--Git/9cd1f401-5158-48e6-8468-2180ba139fd8/scratchpad'
$unityData = 'E:/unity/6000.5.8f1/Editor/Data'
$temp = "$project/Temp/off"
$utf8 = [Text.UTF8Encoding]::new($false)
function Compile([string]$rsp) {
    & "$unityData/DotNetSdk/dotnet.exe" exec "$unityData/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" -nologo "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw "Compile failed: $rsp" }
}
& bash "$scratch/offc.sh" '/e/Git/ant-wt/Q2'
# The shared rsp includes untracked third-party art scripts absent from this worktree.
# Add read-only original references to the LOCAL response file; no original writes.
$extra = 0
foreach ($line in Get-Content -LiteralPath "$temp/orig.rsp") {
    if ($line -notmatch '^"Assets/.*\.cs"$') { continue }
    $relative = $line.Trim('"')
    if ($relative.StartsWith('Assets/_Project/') -or (Test-Path -LiteralPath (Join-Path $project $relative))) { continue }
    $original = Join-Path 'E:/Git/ant' $relative
    if (Test-Path -LiteralPath $original) { [IO.File]::AppendAllText("$temp/a.rsp", '"' + $original + '"' + "`n", $utf8); $extra++ }
}
Compile "$temp/a.rsp"
Write-Output "Game compile: 0 errors. Read-only missing external sources: $extra"
& bash "$scratch/offcheck.sh" '/e/Git/ant-wt/Q2' "$project/AgentScripts/Review1101Q2Checks.cs"
if ((Get-Content -LiteralPath "$temp/cout.txt") -match ': error ') { throw 'Check compile failed' }
Write-Output 'Check compile: 0 errors'
if (-not $OfflineChecks) { return }
Get-ChildItem -LiteralPath "$unityData/Managed/UnityEngine" -Filter *.dll | Copy-Item -Destination $temp -Force
Get-ChildItem -LiteralPath 'E:/Git/ant/Library/ScriptAssemblies' -Filter Unity*.dll | Copy-Item -Destination $temp -Force
$rsp = [Collections.Generic.List[string]]::new()
$rsp.Add('-target:exe'); $rsp.Add('-out:"' + "$temp/OfflineQ2.dll" + '"'); $rsp.Add('-r:"' + "$temp/A.dll" + '"')
foreach ($ref in Get-ChildItem "$unityData/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0/*.dll") { $rsp.Add('-r:"' + $ref.FullName + '"') }
foreach ($name in @('UnityEngine','UnityEngine.CoreModule','UnityEngine.PhysicsModule','UnityEngine.AIModule','UnityEngine.JSONSerializeModule','UnityEngine.UI','Unity.InputSystem')) {
    $file = "$unityData/Managed/UnityEngine/$name.dll"
    if (-not (Test-Path -LiteralPath $file)) {
        $file = "E:/Git/ant/Library/ScriptAssemblies/$name.dll"
    }
    if (Test-Path -LiteralPath $file) { $rsp.Add('-r:"' + $file + '"'); Copy-Item -LiteralPath $file -Destination $temp -Force }
}
foreach ($name in @('Review1101Q2Checks','Review1031M1Checks','Review1031L1Checks','Review1029K1Checks','Review1031N1Checks')) { $rsp.Add('"' + "$project/AgentScripts/$name.cs" + '"') }
$hostSource = @'
using System;
using System.Threading.Tasks;
public static class OfflineRunner {
    public static async Task<int> Main() {
        try {
            Console.WriteLine(await Review1101Q2Checks.Main());
            Console.WriteLine(await Review1031M1Checks.Main());
            Console.WriteLine(await Review1031L1Checks.Main());
            Console.WriteLine(await Review1029K1Checks.Main());
            Console.WriteLine(await Review1031N1Checks.Main());
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
'@
[IO.File]::WriteAllText("$temp/OfflineRunner.cs", $hostSource, $utf8)
$rsp.Add('"' + "$temp/OfflineRunner.cs" + '"'); $rsp.Add('-main:OfflineRunner')
[IO.File]::WriteAllLines("$temp/offline.rsp", $rsp, $utf8)
Compile "$temp/offline.rsp"
[IO.File]::WriteAllText("$temp/OfflineQ2.runtimeconfig.json", '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}', $utf8)
& "$unityData/DotNetSdk/dotnet.exe" "$temp/OfflineQ2.dll"
if ($LASTEXITCODE -ne 0) { throw 'Offline checks failed' }
