param(
    [Parameter(Mandatory=$true)][string]$Editor,
    [string]$ProjectPath,
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$Editor = (Resolve-Path -LiteralPath $Editor).Path
# Unity.exe is a GUI process: some Windows shells return before it exits.
# Unity.com is the supplied console wrapper and preserves the real exit code.
if ([IO.Path]::GetFileName($Editor) -eq 'Unity.exe') {
    $consoleEditor = Join-Path (Split-Path -Parent $Editor) 'Unity.com'
    if (Test-Path -LiteralPath $consoleEditor) { $Editor = $consoleEditor }
}
if (-not $ProjectPath) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if ($projectPath.Length -gt 80) {
    throw 'Use a short project path on Windows (for example C:/Games/Ataka). You can pass a subst path with -ProjectPath.'
}
$logPath = Join-Path $projectPath 'Logs'
$testPath = Join-Path $projectPath 'TestResults'
New-Item -ItemType Directory -Force -Path $logPath,$testPath | Out-Null
function Run-Unity([string[]]$UnityArgs) {
    $allArgs = @('-batchmode','-projectPath',$projectPath) + $UnityArgs
    & $Editor @allArgs
    if ($LASTEXITCODE -ne 0) { throw "Unity exited with code $LASTEXITCODE; see $logPath" }
}
Run-Unity @('-quit','-executeMethod','Ataka.Editor.MilestoneBuilder.Generate','-logFile',(Join-Path $logPath 'generate.log'))
if (-not $SkipTests) {
    foreach ($mode in @('EditMode','PlayMode')) {
        Run-Unity @('-runTests','-testPlatform',$mode,'-testResults',(Join-Path $testPath "$mode.xml"),'-logFile',(Join-Path $logPath "$mode.log"))
        [xml]$result = Get-Content (Join-Path $testPath "$mode.xml")
        if ($result.'test-run'.result -ne 'Passed') { throw "$mode tests did not pass" }
    }
}
Run-Unity @('-quit','-executeMethod','Ataka.Editor.MilestoneBuilder.Build','-logFile',(Join-Path $logPath 'build.log'))
Write-Host "Built: $projectPath/Builds/M0/Ataka-M0.exe"
