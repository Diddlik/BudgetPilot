#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stoppt und entfernt den BudgetPilot-Container.

.PARAMETER Postgres
    Stoppt die PostgreSQL-Variante (docker-compose.postgres.yml).
#>
[CmdletBinding()]
param([switch]$Postgres)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$composeFile = if ($Postgres) { 'docker-compose.postgres.yml' } else { 'docker-compose.yml' }

Write-Host "Stoppe BudgetPilot ($composeFile)..." -ForegroundColor Cyan
& docker compose -f $composeFile down
Write-Host 'Gestoppt.' -ForegroundColor Green
