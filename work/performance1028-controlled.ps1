$ErrorActionPreference = 'Stop'
$taskRoot = 'E:\Git\ant\Temp\performance1028'
$taskPlayer = Join-Path $taskRoot 'controlled\AntPerf.exe'
$taskResults = @()
foreach ($taskCase in @(@{mode='baseline';run=1},@{mode='optimized';run=1},@{mode='baseline';run=2},@{mode='optimized';run=2})) {
    if (Test-Path -LiteralPath (Join-Path $taskRoot 'cancel-controlled')) { throw 'Controlled benchmark cancelled' }
    $taskLabel = "controlled-$($taskCase.mode)$($taskCase.run)"
    $taskOutput = (Join-Path $taskRoot "$taskLabel.json").Replace('\','/')
    $taskLog = (Join-Path $taskRoot "$taskLabel.log").Replace('\','/')
    $taskFlag = if ($taskCase.mode -eq 'baseline') { '-ant-perf-baseline' } else { '' }
    $taskChild = Start-Process -FilePath $taskPlayer -ArgumentList "-ant-perf $taskOutput $taskFlag -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile $taskLog" -WorkingDirectory 'E:\Git\ant' -WindowStyle Hidden -PassThru
    @{pid=$taskChild.Id;label=$taskLabel} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'controlled-active.json')
    if (-not $taskChild.WaitForExit(180000)) { $taskChild.CloseMainWindow() | Out-Null; throw "Player measurement timed out: $taskLabel" }
    if (-not (Test-Path -LiteralPath $taskOutput)) { throw "Report missing: $taskLabel" }
    $taskReport = Get-Content -LiteralPath $taskOutput -Raw | ConvertFrom-Json
    if ($taskReport.baseline -ne ($taskCase.mode -eq 'baseline')) { throw "Mode mismatch: $taskLabel" }
    $taskRow = [pscustomobject]@{mode=$taskCase.mode;run=$taskCase.run;width=$taskReport.width;height=$taskReport.height;frames=$taskReport.frames;meanMs=$taskReport.meanMs;p95Ms=$taskReport.p95Ms;p99Ms=$taskReport.p99Ms;gcBytes=($taskReport.counters | Where-Object name -eq 'GC Allocated In Frame').mean;updateNs=($taskReport.counters | Where-Object name -eq 'BehaviourUpdate').mean;boundsNs=($taskReport.counters | Where-Object name -eq 'UpdateRendererBoundingVolumes').mean}
    $taskResults += $taskRow
    $taskResults | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskRoot 'controlled-summary.json')
    $taskRow | ConvertTo-Json -Compress
}
