param([switch]$NoBrowser)
$ErrorActionPreference = 'Stop'
$gameDirectory = $PSScriptRoot
$gameUrl = 'http://127.0.0.1:4177/'
function Test-GameServer {
    try {
        $response = Invoke-WebRequest -Uri $gameUrl -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200 -and $response.Content -match 'main.js' -and $response.Content -match 'handheld'
    } catch { return $false }
}
if (-not (Test-GameServer)) {
    $nodeCommand = Get-Command node -ErrorAction SilentlyContinue
    $bundledNode = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
    if ($nodeCommand) { $nodePath = $nodeCommand.Source }
    elseif (Test-Path -LiteralPath $bundledNode) { $nodePath = $bundledNode }
    else { throw 'Node.js is required. Install the LTS version from nodejs.org, then run START.cmd again.' }
    $logDirectory = Join-Path $gameDirectory 'run'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    $serverPath = Join-Path $gameDirectory 'server.mjs'
    $process = Start-Process -FilePath $nodePath -ArgumentList ('"' + $serverPath + '"') -WorkingDirectory $gameDirectory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDirectory 'server.log') -RedirectStandardError (Join-Path $logDirectory 'server-error.log') -PassThru
    $process.Id | Set-Content -LiteralPath (Join-Path $logDirectory 'server.pid')
    for ($attempt=0; $attempt -lt 20; $attempt++) {
        if (Test-GameServer) { break }
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-GameServer)) { throw 'The local game server did not start. See run/server-error.log.' }
}
if (-not $NoBrowser) { Start-Process $gameUrl }
Write-Output "Game ready: $gameUrl"
