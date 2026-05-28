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

function Get-FullPath {
    param([Parameter(Mandatory)][string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

function Assert-UnderDirectory {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Parent,
        [Parameter(Mandatory)][string]$Description
    )

    $fullPath = Get-FullPath $Path
    $fullParent = (Get-FullPath $Parent).TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith($fullParent, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description must stay under $fullParent. Got: $fullPath"
    }
}

function Copy-RequiredItem {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Required build input is missing: $Source"
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
}

function Remove-GeneratedPythonArtifacts {
    param([Parameter(Mandatory)][string]$Root)

    Get-ChildItem -LiteralPath $Root -Directory -Recurse -Force |
        Where-Object {
            $_.Name -in @("__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache") -or
            $_.Name -like "*.egg-info"
        } |
        Remove-Item -Recurse -Force

    Get-ChildItem -LiteralPath $Root -File -Recurse -Force -Include "*.pyc", "*.pyo" |
        Remove-Item -Force
}

$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Get-FullPath (Join-Path $scriptRoot "..")
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "windows\publish"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $artifactsRoot "windows\TwitchDropsMiner"
}

$outputRoot = Get-FullPath $OutputDir
$projectPath = Join-Path $scriptRoot "TwitchDropsMiner.Windows\TwitchDropsMiner.Windows.csproj"
$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

Assert-UnderDirectory -Path $publishRoot -Parent $artifactsRoot -Description "Publish directory"

if (-not $NoClean) {
    Assert-UnderDirectory -Path $outputRoot -Parent $artifactsRoot -Description "Cleanable output directory"
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }
}

if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$publishArgs = @(
    "publish",
    $projectPath,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--self-contained", $selfContainedValue,
    "--output", $publishRoot,
    "/p:DebugType=None",
    "/p:DebugSymbols=false"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Get-ChildItem -LiteralPath $publishRoot | Copy-Item -Destination $outputRoot -Recurse -Force

$requiredItems = @(
    "main.py",
    "pyproject.toml",
    "uv.lock",
    "README.md",
    "LICENSE",
    "src",
    "web",
    "lang",
    "icons"
)

foreach ($item in $requiredItems) {
    Copy-RequiredItem -Source (Join-Path $repoRoot $item) -Destination $outputRoot
}

Remove-GeneratedPythonArtifacts -Root $outputRoot

New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot "data") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot "logs") | Out-Null

$launcherBatch = @"
@echo off
start "" "%~dp0TwitchDropsMiner.Windows.exe"
"@
Set-Content -LiteralPath (Join-Path $outputRoot "Start-TwitchDropsMiner.bat") `
    -Value $launcherBatch `
    -Encoding ASCII

$notes = @"
Twitch Drops Miner Windows Build

Run Start-TwitchDropsMiner.bat or TwitchDropsMiner.Windows.exe.
The launcher starts the local web UI at http://127.0.0.1:8080/.

Python is resolved in this order:
1. TDM_PYTHON environment variable
2. env\Scripts\python.exe
3. .venv\Scripts\python.exe
4. uv run python
5. py -3.12
6. python
"@
Set-Content -LiteralPath (Join-Path $outputRoot "WINDOWS_BUILD_README.txt") `
    -Value $notes `
    -Encoding ASCII

Write-Host "Windows build created at: $outputRoot"
