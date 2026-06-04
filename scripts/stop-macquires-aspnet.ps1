# يوقف كل نسخ ASPNET / dotnet الخاصة بمشروع macquires-crm (يفك قفل الـ DLL عند Rebuild)
$ErrorActionPreference = "SilentlyContinue"

Get-Process -Name "ASPNET" | ForEach-Object {
    Write-Host "Stopping ASPNET (PID $($_.Id))"
    Stop-Process -Id $_.Id -Force
}

Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object {
    $_.CommandLine -match "macquires-crm" -or $_.CommandLine -match "Presentation\\ASPNET" -or $_.CommandLine -match "ASPNET\.csproj"
} | ForEach-Object {
    Write-Host "Stopping dotnet host (PID $($_.ProcessId))"
    Stop-Process -Id $_.ProcessId -Force
}

Start-Sleep -Seconds 1
if (-not (Get-Process -Name "ASPNET" -ErrorAction SilentlyContinue)) {
    Write-Host "Done. You can Rebuild in Visual Studio or run: dotnet run --project Presentation\ASPNET\ASPNET.csproj"
} else {
    Write-Warning "Some ASPNET processes may still be running."
}
