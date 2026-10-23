<#
.SYNOPSIS
    Verifies the solution locally: formatting, Release build and tests.

.DESCRIPTION
    Run this before pushing. It performs the same checks a CI server would,
    on your own machine:

      1. dotnet format --verify-no-changes
      2. Release build with warnings treated as errors
      3. Unit tests
      4. Integration tests against SQL Server (skipped when Docker is not running)

.PARAMETER SkipIntegration
    Skips the integration tests even if Docker is available.

.EXAMPLE
    ./build.ps1
    ./build.ps1 -SkipIntegration
#>
[CmdletBinding()]
param(
    [switch] $SkipIntegration
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_NOLOGO = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'

$solution = Join-Path $PSScriptRoot 'Supermarket.slnx'

function Invoke-Step([string] $Name, [scriptblock] $Command) {
    Write-Host "`n==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $Name" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

function Test-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    docker info --format '{{.ServerVersion}}' *> $null
    return $LASTEXITCODE -eq 0
}

Invoke-Step 'Restore' { dotnet restore $solution }
Invoke-Step 'Check formatting' { dotnet format $solution --verify-no-changes --no-restore }
Invoke-Step 'Build (Release)' { dotnet build $solution --configuration Release --no-restore }
Invoke-Step 'Unit tests' {
    dotnet test (Join-Path $PSScriptRoot 'tests/Supermarket.Core.Tests') --configuration Release --no-build
}

if ($SkipIntegration) {
    Write-Host "`nIntegration tests skipped (-SkipIntegration)." -ForegroundColor Yellow
}
elseif (-not (Test-Docker)) {
    Write-Host "`nIntegration tests skipped: Docker is not running." -ForegroundColor Yellow
}
else {
    Invoke-Step 'Integration tests (SQL Server via Testcontainers)' {
        dotnet test (Join-Path $PSScriptRoot 'tests/Supermarket.Data.IntegrationTests') --configuration Release --no-build
    }
}

Write-Host "`nAll checks passed." -ForegroundColor Green
