# §14 Device Sales — apply TelecomDeviceSales_Manual.sql to target database.
param(
    [string]$Server = "localhost",
    [string]$Database = "SyriaTeltest18"
)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "TelecomDeviceSales_Manual.sql"
sqlcmd -S $Server -d $Database -i $sqlFile -b
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "TelecomDeviceSales_Manual.sql applied to $Database."
