[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSCommandPath
$testProject = Join-Path $repoRoot "windows\TwitchDropsMiner.Windows.Tests\TwitchDropsMiner.Windows.Tests.csproj"

Push-Location $repoRoot
try {
    dotnet run --project $testProject
}
finally {
    Pop-Location
}
