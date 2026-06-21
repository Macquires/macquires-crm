# Sovereign Master Matrix — single-test live launcher (headed browser).
# Usage: .\run-master-matrix.ps1 Test_03_SIM_Swap_Flow
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TestName
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testProject = Join-Path $PSScriptRoot "ASPNET.E2E.Tests.csproj"

$env:E2E_HEADLESS = "false"
$env:DEMO_SLOW_MO = if ($TestName -match "ACT") { "500" } else { "800" }

Write-Host "Launching Master Matrix test: $TestName (headed, slow-mo)..." -ForegroundColor Cyan

Push-Location $repoRoot
try {
    dotnet test $testProject `
        --filter "FullyQualifiedName~$TestName" `
        --logger "console;verbosity=detailed"
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
