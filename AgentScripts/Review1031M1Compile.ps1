param([switch]$OfflineChecks)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($project -ne 'E:\Git\ant-wt\M1') { throw 'M1 worktree에서만 실행할 수 있습니다' }
$unityRoot = 'E:/unity/6000.5.8f1/Editor/Data'
$bee = 'E:/Git/ant/Library/Bee/artifacts/1900b0aE.dag'
$temp = Join-Path $project 'Temp/Review1031M1'
New-Item -ItemType Directory -Path $temp -Force | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Compile([string]$rsp) {
    & "$unityRoot/DotNetSdk/dotnet.exe" exec "$unityRoot/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" -nologo "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw "컴파일 실패: $rsp" }
}
$lines = [Collections.Generic.List[string]]::new()
$known = @{}; $missing = 0
foreach ($line in (Get-Content -LiteralPath "$bee/Assembly-CSharp.rsp")) {
    if ($line -match '\.cs"?$' -and $line -notmatch '^[-/]') {
        $relative = $line.Trim('"').Replace('\', '/')
        $source = Join-Path $project $relative
        if (Test-Path -LiteralPath $source) { $lines.Add('"' + $source + '"'); $known[$relative] = $true }
        else {
            if ($relative.StartsWith('Assets/_Project/')) { continue } # 원본의 후속 개발 소스를 M1 기준선에 섞지 않는다.
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
Write-Output "게임 컴파일 통과; 원본 외부 소스는 읽기만 하고 M1 Temp로 복사: $missing 개"
$checks = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter Review1031M1Checks.cs)
$editor = [Collections.Generic.List[string]]::new()
foreach ($line in (Get-Content -LiteralPath "$bee/Assembly-CSharp-Editor.rsp")) {
    if ($line -match '\.cs"?$' -and $line -notmatch '^[-/]') { continue }
    if ($line -match '^[-/](out|refout|analyzer|generatedfilesout):') { continue }
    if ($line -match '^[-/](r|reference):.*Assembly-CSharp(\.ref)?\.dll') { continue }
    $editor.Add($line.Replace('"Library/', '"E:/Git/ant/Library/'))
}
$editor.Add('-r:"' + "$temp/Assembly-CSharp.dll" + '"')
$editor.Add('-out:"' + "$temp/Review1031M1Checks.dll" + '"')
foreach ($source in $checks) { $editor.Add('"' + $source.FullName + '"') }
[IO.File]::WriteAllLines("$temp/checks.rsp", $editor, $utf8)
Compile "$temp/checks.rsp"
Write-Output '검사 코드 컴파일 통과 (RunScene 실행은 격리 장면 필요·실행 잔여)'
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
        try { Console.WriteLine(await Review1031M1Checks.Main()); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
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
