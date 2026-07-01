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

dotnet publish (Join-Path $PSScriptRoot "BudgetPilot.Mobile.csproj") `
    -f net8.0-android `
    -c Release `
    -m:1 `
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
