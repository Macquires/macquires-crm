param(
    [string]$S = "localhost",
    [string]$d = "SyriaTeltest18"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$sqlFiles = @(
    "TelecomRefundFields_Manual.sql",
    "TelecomRefundPermissions_Manual.sql"
)

foreach ($file in $sqlFiles) {
    $path = Join-Path $root $file
    if (-not (Test-Path $path)) {
        throw "Missing SQL file: $path"
    }
    Write-Host "Running $file on $S / $d ..."
    sqlcmd -S $S -d $d -i $path -b
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed for $file (exit $LASTEXITCODE)"
    }
}

Write-Host "Refund migration completed successfully."
