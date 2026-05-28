[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [string]$OutputDir,

    [switch]$SelfContained,

    [switch]$NoClean
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSCommandPath
$buildScript = Join-Path $repoRoot "windows\build-windows.ps1"
$buildArgs = @(
    "-Configuration", $Configuration,
    "-Runtime", $Runtime
)

if (-not [string]::IsNullOrWhiteSpace($OutputDir)) {
    $buildArgs += @("-OutputDir", $OutputDir)
}

if ($SelfContained) {
    $buildArgs += "-SelfContained"
}

if ($NoClean) {
    $buildArgs += "-NoClean"
}

& $buildScript @buildArgs
