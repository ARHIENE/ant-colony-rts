param([switch]$OfflineChecks)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($project -ne 'E:\Git\ant-wt\N1') { throw 'N1 worktree 전용' }
$unityRoot = 'E:/unity/6000.5.8f1/Editor/Data'
$bee = 'E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag'
$temp = Join-Path $project 'Temp/Review1031N1'
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
        if ($relative.StartsWith('Assets/_Project/')) { continue }
        $source = Join-Path $project $relative
        if (Test-Path -LiteralPath $source) { $lines.Add('"' + $source + '"'); $known[$relative] = $true }
        else {
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
Write-Output "게임 컴파일 통과; 원본 외부 소스는 읽기만 하고 N1 Temp로 복사: $missing"
$checks = @('Review1031N1Checks.cs', 'Review1031M1Checks.cs', 'Review1031M2Checks.cs', 'Review1031L1Checks.cs', 'Review1031L2Checks.cs', 'Review1029K1Checks.cs', 'Review1029K2Checks.cs', 'Review1029BossChecks.cs', 'Review1029FinaleChecks.cs', 'Review1029CampaignRuntimeChecks.cs') | ForEach-Object { Get-Item (Join-Path $PSScriptRoot $_) }
$editor = [Collections.Generic.List[string]]::new()
foreach ($line in (Get-Content -LiteralPath "$bee/Assembly-CSharp-Editor.rsp")) {
    if ($line -match '\.cs"?$' -and $line -notmatch '^[-/]') { continue }
    if ($line -match '^[-/](out|refout|analyzer|generatedfilesout):') { continue }
    if ($line -match '^[-/](r|reference):.*Assembly-CSharp(\.ref)?\.dll') { continue }
    $editor.Add($line.Replace('"Library/', '"E:/Git/ant/Library/'))
}
$editor.Add('-r:"' + "$temp/Assembly-CSharp.dll" + '"')
$editor.Add('-out:"' + "$temp/Review1031N1Checks.dll" + '"')
foreach ($source in $checks) { $editor.Add('"' + $source.FullName + '"') }
[IO.File]::WriteAllLines("$temp/checks.rsp", $editor, $utf8)
Compile "$temp/checks.rsp"
Write-Output 'Editor 검사 컴파일 통과 (RunScene 실행 잔여)'
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
        Console.WriteLine(await Review1029K2Checks.Main());
        Console.WriteLine(await Review1029BossChecks.Main());
        Console.WriteLine(await Review1029FinaleChecks.Main());
        Console.WriteLine(await Review1031L1Checks.Main());
        Console.WriteLine(await Review1031L2Checks.Main());
        Console.WriteLine(await Review1031M1Checks.Main());
        Console.WriteLine(await Review1031M2Checks.Main());
        Console.WriteLine(await Review1031N1Checks.Main());
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
if ($LASTEXITCODE -ne 0) { throw '오프라인 검사 실패' }
