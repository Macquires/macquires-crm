# §9 RCN — runs against SyriaTeltest18 (adjust server as needed).
param(
    [string]$Server = "localhost",
    [string]$Database = "SyriaTeltest18"
)

$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

$scripts = @(
    "TelecomReconnectFields_Manual.sql",
    "TelecomReconnectPermissions_Manual.sql"
)

foreach ($name in $scripts) {
    $path = Join-Path $dir $name
    Write-Host "Running $name ..."
    sqlcmd -S $Server -d $Database -i $path -b
}

Write-Host "Done RCN. Log out and log in to refresh permissions."
