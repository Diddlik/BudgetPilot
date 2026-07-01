param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "artifacts")
)

$ErrorActionPreference = "Stop"

$required = @(
    "BUDGETPILOT_KEYSTORE",
    "BUDGETPILOT_KEY_ALIAS",
    "BUDGETPILOT_KEYSTORE_PASSWORD",
    "BUDGETPILOT_KEY_PASSWORD"
)

$missing = $required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) }
if ($missing.Count -gt 0) {
    throw "Fehlende Umgebungsvariablen: $($missing -join ', ')"
}

$keystore = [Environment]::GetEnvironmentVariable("BUDGETPILOT_KEYSTORE")
if (-not (Test-Path -LiteralPath $keystore -PathType Leaf)) {
    throw "Keystore nicht gefunden: $keystore"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$isShallow = (& git -C $repositoryRoot rev-parse --is-shallow-repository).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Git-Repository konnte für die automatische App-Version nicht gelesen werden."
}
if ($isShallow -eq "true") {
    throw "Automatische App-Version benötigt die vollständige Git-Historie. Führe zuerst 'git fetch --unshallow' aus."
}

$commitCountText = (& git -C $repositoryRoot rev-list --count HEAD).Trim()
$versionCode = 0
if ($LASTEXITCODE -ne 0 -or
    -not [int]::TryParse($commitCountText, [ref]$versionCode) -or
    $versionCode -le 0) {
    throw "Git-Commit-Anzahl konnte nicht als Android-Version ermittelt werden."
}
$displayVersion = "1.0.$versionCode"

Write-Host "Android-Version: $displayVersion (versionCode $versionCode)" -ForegroundColor Cyan

dotnet publish (Join-Path $PSScriptRoot "BudgetPilot.Mobile.csproj") `
    -f net10.0-android36.1 `
    -c Release `
    -m:1 `
    "-p:ApplicationVersion=$versionCode" `
    "-p:ApplicationDisplayVersion=$displayVersion" `
    -p:AndroidKeyStore=true `
    "-p:AndroidSigningKeyStore=$keystore" `
    "-p:AndroidSigningKeyAlias=$([Environment]::GetEnvironmentVariable('BUDGETPILOT_KEY_ALIAS'))" `
    -p:AndroidSigningStorePass=env:BUDGETPILOT_KEYSTORE_PASSWORD `
    -p:AndroidSigningKeyPass=env:BUDGETPILOT_KEY_PASSWORD `
    -p:AndroidPackageFormats=apk%3Baab `
    "-p:PublishDir=$resolvedOutput"

if ($LASTEXITCODE -ne 0) {
    throw "Release-Build fehlgeschlagen (Exitcode $LASTEXITCODE)."
}

Get-ChildItem -LiteralPath $resolvedOutput -File |
    Where-Object Extension -In ".apk", ".aab" |
    Select-Object FullName, Length, LastWriteTime
