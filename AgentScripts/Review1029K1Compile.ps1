param([switch]$OfflineChecks)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($project -ne 'E:\Git\ant-wt\K1') { throw 'K1 worktree only' }
$unityRoot = 'E:/unity/6000.5.8f1/Editor/Data'
$bee = 'E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag'
$temp = Join-Path $project 'Temp/Review1029K1'
New-Item -ItemType Directory -Path $temp -Force | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Compile([string]$rsp) {
    & "$unityRoot/DotNetSdk/dotnet.exe" exec "$unityRoot/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" -nologo "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw "Compile failed: $rsp" }
}
$lines = [Collections.Generic.List[string]]::new()
$known = @{}; $missing = 0
foreach ($line in (Get-Content -LiteralPath "$bee/Assembly-CSharp.rsp")) {
    if ($line -match '\.cs"?$' -and $line -notmatch '^[-/]') {
        $relative = $line.Trim('"').Replace('\', '/')
        $source = Join-Path $project $relative
        if (Test-Path -LiteralPath $source) { $lines.Add('"' + $source + '"'); $known[$relative] = $true }
        else {
            if ($relative.StartsWith('Assets/_Project/')) { continue } # 원본의 후속 개발 소스를 K1 기준선에 섞지 않는다.
            $missing++
            $original = Join-Path 'E:/Git/ant' $relative
            if (Test-Path -LiteralPath $original) {
                $copy = Join-Path "$temp/ReferenceSources" $relative
                New-Item -ItemType Directory -Path (Split-Path $copy) -Force | Out-Null
                Copy-Item -LiteralPath $original -Destination $copy -Force
                $lines.Add('"' + $copy + '"')
            }
        }
    } elseif ($line -notmatch '^[-/](out|refout|analyzer|generatedfilesout):') { $lines.Add($line.Replace('"Library/', '"E:/Git/ant/Library/')) }
}
foreach ($source in (Get-ChildItem -LiteralPath "$project/Assets/_Project/Scripts" -Recurse -Filter *.cs)) {
    $relative = $source.FullName.Substring($project.Length + 1).Replace('\', '/')
    if (-not $known.ContainsKey($relative)) { $lines.Add('"' + $source.FullName + '"') }
}
$lines.Add('-out:"' + "$temp/Assembly-CSharp.dll" + '"')
[IO.File]::WriteAllLines("$temp/game.rsp", $lines, $utf8)
Compile "$temp/game.rsp"
Write-Output "Game compile PASS; missing external sources copied read-only from original into K1 Temp: $missing"
$checks = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter Review1029K1Checks.cs)
$editor = [Collections.Generic.List[string]]::new()
foreach ($line in (Get-Content -LiteralPath "$bee/Assembly-CSharp-Editor.rsp")) {
    if ($line -match '\.cs"?$' -and $line -notmatch '^[-/]') { continue }
    if ($line -match '^[-/](out|refout|analyzer|generatedfilesout):') { continue }
    if ($line -match '^[-/](r|reference):.*Assembly-CSharp(\.ref)?\.dll') { continue }
    $editor.Add($line.Replace('"Library/', '"E:/Git/ant/Library/'))
}
$editor.Add('-r:"' + "$temp/Assembly-CSharp.dll" + '"')
$editor.Add('-out:"' + "$temp/Review1029K1Checks.dll" + '"')
foreach ($source in $checks) { $editor.Add('"' + $source.FullName + '"') }
[IO.File]::WriteAllLines("$temp/checks.rsp", $editor, $utf8)
Compile "$temp/checks.rsp"
Write-Output 'Editor checks compile PASS (execution requires isolated game scene)'
if (-not $OfflineChecks) { return }
$offline = [Collections.Generic.List[string]]::new()
$offline.Add('-target:exe'); $offline.Add('-out:"' + "$temp/OfflineChecks.dll" + '"')
foreach ($ref in (Get-ChildItem "$unityRoot/DotNetSdk/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0/*.dll")) { $offline.Add('-r:"' + $ref.FullName + '"') }
foreach ($name in @('UnityEngine', 'UnityEngine.CoreModule', 'UnityEngine.PhysicsModule', 'UnityEngine.AIModule', 'UnityEngine.JSONSerializeModule')) {
    $path = "$unityRoot/Managed/UnityEngine/$name.dll"
    $offline.Add('-r:"' + $path + '"'); Copy-Item -LiteralPath $path -Destination $temp -Force
}
$offline.Add('-r:"' + "$temp/Assembly-CSharp.dll" + '"')
foreach ($source in $checks | Where-Object { $_.Name -notmatch 'Runtime' }) { $offline.Add('"' + $source.FullName + '"') }
$hostSource = @'
using System;
using System.Threading.Tasks;
public static class OfflineRunner {
    public static async Task<int> Main() {
        Console.WriteLine(await Review1029K1Checks.Main());
        return 0;
    }
}
'@
[IO.File]::WriteAllText("$temp/OfflineRunner.cs", $hostSource, $utf8)
$offline.Add('"' + "$temp/OfflineRunner.cs" + '"'); $offline.Add('-main:OfflineRunner')
[IO.File]::WriteAllLines("$temp/offline.rsp", $offline, $utf8)
Compile "$temp/offline.rsp"
[IO.File]::WriteAllText("$temp/OfflineChecks.runtimeconfig.json", '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.21"}}}', $utf8)
& "$unityRoot/DotNetSdk/dotnet.exe" "$temp/OfflineChecks.dll"
if ($LASTEXITCODE -ne 0) { throw 'Offline checks failed' }
