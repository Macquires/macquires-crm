# تشغيل سكربتات SIM Swap على SyriaTeltest18 (من مجلد Migrations).
$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$server = 'localhost'
$db = 'SyriaTeltest18'
$scripts = @(
    'TelecomSimSwapFields_Manual.sql',
    'TelecomSimSwapApprovePermission_Manual.sql',
    'TelecomSimSwapRequestPermission_Manual.sql'
)
foreach ($s in $scripts) {
    Write-Host "Running $s ..."
    sqlcmd -S $server -d $db -E -i (Join-Path $dir $s)
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $s" }
}
Write-Host 'SIM Swap manual migrations completed.'
