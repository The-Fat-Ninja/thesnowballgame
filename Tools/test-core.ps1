param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $projectRoot "Tests\SnowballGame.Core.Tests\SnowballGame.Core.Tests.csproj"

dotnet test $testProject --configuration $Configuration
