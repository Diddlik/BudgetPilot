param(
    [int]$Port = 8089,
    [switch]$Reset
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$instanceDirectory = Join-Path $repoRoot ".tmp-build\screenshot-instance"
$databasePath = Join-Path $instanceDirectory "budgetpilot-screenshots.db"

if ($Reset -and (Test-Path -LiteralPath $databasePath)) {
    Remove-Item -LiteralPath $databasePath -Force
}

New-Item -ItemType Directory -Path $instanceDirectory -Force | Out-Null

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Database__Provider = "Sqlite"
$env:Database__ConnectionString = "Data Source=$databasePath"
$env:Auth__Email = "screenshots@budgetpilot.local"
$env:Auth__Password = "Screenshots!2026"

Write-Host ""
Write-Host "BudgetPilot Screenshot-Instanz" -ForegroundColor DarkYellow
Write-Host "Web:      http://localhost:$Port"
Write-Host "Emulator: http://10.0.2.2:$Port"
Write-Host "E-Mail:   screenshots@budgetpilot.local"
Write-Host "Passwort: Screenshots!2026"
Write-Host "Datenbank: $databasePath"
Write-Host ""

dotnet run `
    --project (Join-Path $repoRoot "src\BudgetPilot.Web") `
    --no-launch-profile `
    --urls "http://0.0.0.0:$Port"
