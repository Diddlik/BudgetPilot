@echo off
REM ============================================================
REM  BudgetPilot - lokal starten (Entwicklungsmodus)
REM  Doppelklick oder in der Eingabeaufforderung ausfuehren.
REM ============================================================
setlocal
cd /d "%~dp0"

set "ASPNETCORE_ENVIRONMENT=Development"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [Fehler] .NET SDK wurde nicht gefunden. Bitte .NET 8 SDK installieren.
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo ============================================================
echo  BudgetPilot startet im Entwicklungsmodus...
echo.
echo  URL    : https://localhost:7130   (oder http://localhost:5070)
echo  Login  : admin@budgetpilot.local
echo  Passwort: ChangeMe!2026
echo.
echo  Zum Beenden in diesem Fenster STRG+C druecken.
echo ============================================================
echo.

dotnet run --project "src\BudgetPilot.Web" --launch-profile http

echo.
echo BudgetPilot wurde beendet.
pause
endlocal
