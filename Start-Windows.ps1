[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSCommandPath
$projectPath = Join-Path $repoRoot "windows\TwitchDropsMiner.Windows\TwitchDropsMiner.Windows.csproj"

Push-Location $repoRoot
try {
    dotnet run --project $projectPath
}
finally {
    Pop-Location
}
