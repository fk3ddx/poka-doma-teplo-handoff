param(
    [Parameter(Mandatory=$true)][string]$Editor,
    [string]$ProjectPath,
    [switch]$SkipTests
)
# The default build now follows the current milestone. M0 remains in Git history.
& "$PSScriptRoot/build-m1.ps1" @PSBoundParameters
