# Generates large CSV samples for Bulk Import Monitor (3 job types).
# Usage:
#   Copy config.sample.json to config.local.json and fill IDs from your DB.
#   .\generate-samples.ps1
#   .\generate-samples.ps1 -RowCount 10000

param(
    [int] $RowCount = 3000,
    [string] $ConfigPath = "$PSScriptRoot\config.local.json",
    [string] $OutDir = $PSScriptRoot
)

function Get-LuhnCheckDigit {
    param([string]$DigitsWithoutCheck)
    $sum = 0
    $alt = $true
    for ($i = $DigitsWithoutCheck.Length - 1; $i -ge 0; $i--) {
        $n = [int][string]$DigitsWithoutCheck[$i]
        if ($alt) {
            $n *= 2
            if ($n -gt 9) { $n -= 9 }
        }
        $sum += $n
        $alt = -not $alt
    }
    return ((10 - ($sum % 10)) % 10).ToString()
}

function New-ValidIccid {
    param([int]$Serial)
    # 89 + issuer 103 + 12-digit serial => 19 digits + Luhn
    $base = "891030{0:D12}" -f ($Serial % 1000000000000)
    return $base + (Get-LuhnCheckDigit $base)
}

function New-SyrianMsisdn {
    param([int]$Index)
    # 09 + 8 digits (demo range 93 1xx xxxx)
    $suffix = "{0:D8}" -f ($Index % 100000000)
    return "09$suffix"
}

$config = @{
    CustomerGroupId = "REPLACE_GROUP"
    CustomerCategoryId = "REPLACE_CATEGORY"
    ProductId = "REPLACE_PRODUCT"
    ProductOfferingId = ""
    InjectErrorPercent = 2
    MsisdnStartIndex = 1000000
}

if (Test-Path $ConfigPath) {
    $loaded = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    if ($loaded.CustomerGroupId) { $config.CustomerGroupId = $loaded.CustomerGroupId }
    if ($loaded.CustomerCategoryId) { $config.CustomerCategoryId = $loaded.CustomerCategoryId }
    if ($loaded.ProductId) { $config.ProductId = $loaded.ProductId }
    if ($null -ne $loaded.ProductOfferingId) { $config.ProductOfferingId = $loaded.ProductOfferingId }
    if ($loaded.RowCount) { $RowCount = [int]$loaded.RowCount }
    if ($loaded.MsisdnStartIndex) { $config.MsisdnStartIndex = [int]$loaded.MsisdnStartIndex }
    if ($null -ne $loaded.InjectErrorPercent) { $config.InjectErrorPercent = [int]$loaded.InjectErrorPercent }
}

$errPct = [Math]::Max(0, [Math]::Min(20, $config.InjectErrorPercent))
$start = $config.MsisdnStartIndex

Write-Host "Generating $RowCount rows per file -> $OutDir"
Write-Host "CustomerGroupId=$($config.CustomerGroupId)"
Write-Host "CustomerCategoryId=$($config.CustomerCategoryId)"
Write-Host "ProductId=$($config.ProductId)"

# --- 1) MsisdnAsset ---
$msisdnPath = Join-Path $OutDir "sample-msisdn-assets.csv"
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("MSISDN,IMSI,ICCID,Pin1,Puk1")
for ($i = 0; $i -lt $RowCount; $i++) {
    $idx = $start + $i
    $injectErr = ($i % 100) -lt $errPct
    if ($injectErr -and ($i % 2 -eq 0)) {
        [void]$sb.AppendLine(",,INVALID_ICCID,,")
        continue
    }
    $msisdn = New-SyrianMsisdn $idx
    $iccid = New-ValidIccid $idx
    $imsi = "41701{0:D10}" -f ($idx % 10000000000)
    [void]$sb.AppendLine("$msisdn,$imsi,$iccid,1234,5678")
}
[System.IO.File]::WriteAllText($msisdnPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($true))
Write-Host "Wrote $msisdnPath"

# --- 2) CustomerProfiles ---
$custPath = Join-Path $OutDir "sample-customer-profiles.csv"
$sb.Clear() | Out-Null
[void]$sb.AppendLine("CustomerCode,FullNameAr,FullNameEn,NationalId,CustomerType")
for ($i = 0; $i -lt $RowCount; $i++) {
    $idx = $start + $i
    $injectErr = ($i % 100) -lt $errPct
    if ($injectErr -and ($i % 2 -eq 1)) {
        [void]$sb.AppendLine(",,,,")
        continue
    }
    $code = "CST{0:D8}" -f $idx
    $nat = "{0:D10}" -f (1000000000 + ($idx % 900000000))
    $nameAr = "Bulk Customer AR $idx"
    [void]$sb.AppendLine("$code,$nameAr,Bulk Customer $idx,$nat,Individual")
}
[System.IO.File]::WriteAllText($custPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($true))
Write-Host "Wrote $custPath"

# --- 3) PackageMigration (same MSISDN range as file 1 — import MSISDN file first, then create subscriptions, or expect row errors) ---
$pkgPath = Join-Path $OutDir "sample-package-migration.csv"
$sb.Clear() | Out-Null
[void]$sb.AppendLine("MSISDN,CurrentOfferCode,NewOfferCode")
for ($i = 0; $i -lt $RowCount; $i++) {
    $idx = $start + $i
    $injectErr = ($i % 100) -lt $errPct
    if ($injectErr -and ($i % 3 -eq 0)) {
        [void]$sb.AppendLine(",MIX_500,")
        continue
    }
    $msisdn = New-SyrianMsisdn $idx
    $newCode = if ($config.ProductOfferingId) { "OFFER_$idx" } else { "MIX_500" }
    [void]$sb.AppendLine("$msisdn,MIX_100,$newCode")
}
[System.IO.File]::WriteAllText($pkgPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($true))
Write-Host "Wrote $pkgPath"
Write-Host "Done."
