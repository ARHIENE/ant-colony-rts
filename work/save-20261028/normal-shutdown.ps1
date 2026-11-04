Start-Sleep -Seconds 20
$shutdownRequest = Get-Content -LiteralPath 'E:\Git\ant\work\save-20261028\shutdown-request.json' -Raw | ConvertFrom-Json
if ($shutdownRequest.cancelled -eq $true) { exit }
if ([DateTimeOffset]::Now -ge [DateTimeOffset]::Parse($shutdownRequest.expiresAt)) { exit }
& "$env:SystemRoot\System32\shutdown.exe" /s /t 0
