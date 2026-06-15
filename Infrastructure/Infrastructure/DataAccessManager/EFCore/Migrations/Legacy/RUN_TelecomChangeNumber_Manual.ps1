# Runs Change Number SQL scripts against SyriaTeltest18 (adjust server/instance as needed).
param(
    [string]$Server = "localhost",
    [string]$Database = "SyriaTeltest18"
)

$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

$scripts = @(
    "TelecomChangeNumberFields_Manual.sql",
    "TelecomChangeNumberRequestPermission_Manual.sql",
    "TelecomChangeNumberApprovePermission_Manual.sql",
    "TelecomChangeNumberPremiumPool_Manual.sql"
)

foreach ($name in $scripts) {
    $path = Join-Path $dir $name
    Write-Host "Running $name ..."
    sqlcmd -S $Server -d $Database -i $path -b
}

Write-Host "Done. Log out and log in to refresh permissions."
