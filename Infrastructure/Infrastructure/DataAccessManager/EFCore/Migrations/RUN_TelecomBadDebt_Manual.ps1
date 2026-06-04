# Idempotent §16 BDR — schema + optional demo patch
$root = Split-Path -Parent $PSScriptRoot
$fields = Join-Path $root "TelecomBadDebtFields_Manual.sql"
$demo = Join-Path $root "TelecomBadDebt_HeroDemoPatch_Manual.sql"
Write-Host "Run on your SQL Server (SyriaTeltest18):"
Write-Host "  1. $fields"
Write-Host "  2. $demo (optional if app seed already ran)"
