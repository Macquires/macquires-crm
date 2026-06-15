# إصلاح شامل: NULL على كل أعمدة bit القابلة للـ NULL (SqlNullValueException).
$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$server = 'localhost'
$db = 'SyriaTeltest18'
$sql = 'Database_AllNullableBitColumnsFix_Manual.sql'
Write-Host "Running $sql on $db ..."
sqlcmd -S $server -d $db -E -i (Join-Path $dir $sql)
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $sql" }
Write-Host 'All nullable bit columns fix completed.'
