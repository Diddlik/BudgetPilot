#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Startet BudgetPilot in Docker und öffnet die App im Browser.

.DESCRIPTION
    Prüft, ob der Docker-Daemon läuft (startet Docker Desktop bei Bedarf), baut das
    Image und fährt den Container per docker compose hoch. Wartet, bis die App auf
    Port 8080 antwortet, und öffnet sie im Standardbrowser.

.PARAMETER Postgres
    Nutzt die PostgreSQL-Variante (docker-compose.postgres.yml) statt SQLite.

.PARAMETER NoBuild
    Überspringt den Image-Build (nutzt das vorhandene Image).

.PARAMETER Foreground
    Läuft im Vordergrund mit Live-Logs (Strg+C beendet den Container).

.EXAMPLE
    ./scripts/start.ps1
.EXAMPLE
    ./scripts/start.ps1 -Postgres
.EXAMPLE
    ./scripts/start.ps1 -Foreground
#>
[CmdletBinding()]
param(
    [switch]$Postgres,
    [switch]$NoBuild,
    [switch]$Foreground
)

$ErrorActionPreference = 'Stop'

# Immer aus dem Repo-Root arbeiten (Skript liegt in scripts/).
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$composeFile = if ($Postgres) { 'docker-compose.postgres.yml' } else { 'docker-compose.yml' }
$url = 'http://localhost:8080'

function Test-DockerRunning {
    try { docker info *> $null; return $LASTEXITCODE -eq 0 } catch { return $false }
}

# ── 1. Docker-Daemon sicherstellen ──────────────────────────────────────────
if (-not (Test-DockerRunning)) {
    Write-Host 'Docker-Daemon läuft nicht – versuche Docker Desktop zu starten...' -ForegroundColor Yellow
    $desktop = @(
        "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe",
        "$env:LOCALAPPDATA\Docker\Docker Desktop.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $desktop) {
        Write-Error 'Docker Desktop wurde nicht gefunden. Bitte Docker starten und erneut versuchen.'
    }

    Start-Process $desktop
    Write-Host 'Warte auf den Docker-Daemon (bis zu 120s)...' -NoNewline
    for ($i = 0; $i -lt 60; $i++) {
        if (Test-DockerRunning) { break }
        Start-Sleep -Seconds 2
        Write-Host '.' -NoNewline
    }
    Write-Host ''
    if (-not (Test-DockerRunning)) {
        Write-Error 'Docker-Daemon ist nicht rechtzeitig gestartet. Bitte Docker Desktop manuell starten.'
    }
}
Write-Host 'Docker läuft.' -ForegroundColor Green

# ── 2. Compose-Argumente zusammenbauen ──────────────────────────────────────
$composeArgs = @('compose', '-f', $composeFile, 'up')
if (-not $NoBuild)   { $composeArgs += '--build' }
if (-not $Foreground) { $composeArgs += '-d' }

Write-Host "Starte BudgetPilot ($composeFile)..." -ForegroundColor Cyan

if ($Foreground) {
    # Vordergrund: Logs live, Strg+C beendet.
    & docker @composeArgs
    exit $LASTEXITCODE
}

& docker @composeArgs
if ($LASTEXITCODE -ne 0) { Write-Error 'docker compose up ist fehlgeschlagen.' }

# ── 3. Auf Erreichbarkeit warten ────────────────────────────────────────────
Write-Host "Warte auf $url ..." -NoNewline
$up = $false
for ($i = 0; $i -lt 40; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $up = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
    Write-Host '.' -NoNewline
}
Write-Host ''

if ($up) {
    Write-Host "BudgetPilot läuft unter $url" -ForegroundColor Green
    Start-Process $url
} else {
    Write-Host "App antwortet noch nicht. Logs ansehen mit:  docker compose -f $composeFile logs -f" -ForegroundColor Yellow
}

Write-Host ''
Write-Host 'Logs:    ' -NoNewline; Write-Host "docker compose -f $composeFile logs -f" -ForegroundColor Gray
Write-Host 'Stoppen: ' -NoNewline; Write-Host './scripts/stop.ps1' -ForegroundColor Gray
