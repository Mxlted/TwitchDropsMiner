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
$buildParams = @{
    Configuration = $Configuration
    Runtime = $Runtime
}

if (-not [string]::IsNullOrWhiteSpace($OutputDir)) {
    $buildParams.OutputDir = $OutputDir
}

if ($SelfContained) {
    $buildParams.SelfContained = $true
}

if ($NoClean) {
    $buildParams.NoClean = $true
}

& $buildScript @buildParams
