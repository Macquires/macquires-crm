# Runs Termination SQL scripts against SyriaTeltest18 (adjust server/instance as needed).
param(
    [string]$Server = "localhost",
    [string]$Database = "SyriaTeltest18"
)

$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

$scripts = @(
    "TelecomTerminationFields_Manual.sql",
    "TelecomTerminationPermissions_Manual.sql"
)

foreach ($name in $scripts) {
    $path = Join-Path $dir $name
    Write-Host "Running $name ..."
    sqlcmd -S $Server -d $Database -i $path -b
}

Write-Host "Done. Log out and log in to refresh permissions."
