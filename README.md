# BudgetPilot

Webbasiertes Haushaltsbudget-Tool (.NET 8 / Blazor) zur Planung von Einnahmen und
Ausgaben als **zeitlich versionierte Budgetregeln**. Daraus werden **Monats- und
Jahresübersichten** in zwei Berechnungsarten erzeugt: **Budget-Sicht** (lumpy Kosten
anteilig verteilt) und **Cashflow-Sicht** (Beträge im tatsächlichen Zahlungsmonat).
Historische Monate ändern sich durch spätere Anpassungen nie.

## Stack

- **Blazor Web App** (Interactive Server), ASP.NET Core, .NET 8 (LTS)
- **Entity Framework Core 8** — SQLite (MVP), PostgreSQL (umschaltbar)
- **xUnit** + FluentAssertions
- Lokalisierung **de-DE** (EUR, `dd.MM.yyyy`)

## Projektstruktur

```
src/BudgetPilot.Domain          Entities, Enums, Domain-Regeln (Projektion, Validierung)
src/BudgetPilot.Application      Service-Interfaces + -Implementierungen, DTOs, Repository-Interfaces
src/BudgetPilot.Infrastructure   EF Core DbContext, Repositories, Migrations, Seeding
src/BudgetPilot.Web              Blazor-UI, Composition Root (Program.cs)
tests/BudgetPilot.Domain.Tests
tests/BudgetPilot.Application.Tests
```

Abhängigkeitsrichtung: `Web → Application → Domain`; `Infrastructure → Application + Domain`.
Berechnungslogik liegt ausschließlich in Domain/Application — nie in der UI.
Architektur- und Beitragshinweise: siehe [`CLAUDE.md`](CLAUDE.md) und
[`Docs/IMPLEMENTATION_PLAN.md`](Docs/IMPLEMENTATION_PLAN.md).

## Lokal starten

Voraussetzung: .NET SDK 8 (per `global.json` auf 8.0.4xx gepinnt).

```bash
dotnet run --project src/BudgetPilot.Web
```

Beim ersten Start werden die Migration angewendet und Demo-Daten (Spec §12) in eine
SQLite-Datei unter `src/BudgetPilot.Web/data/budgetpilot.db` geseedet. Die App ist dann
unter der in der Konsole angezeigten URL erreichbar.

## Tests

```bash
dotnet test                                            # alle Tests
dotnet test tests/BudgetPilot.Domain.Tests             # ein Projekt
dotnet test --filter "FullyQualifiedName~Quarterly"    # einzelne Gruppe
```

## Docker

**Einfachster Weg (Windows/PowerShell):** Das Startskript prüft den Docker-Daemon
(startet Docker Desktop bei Bedarf), baut das Image, fährt den Container hoch,
wartet auf die App und öffnet sie im Browser.

```powershell
./scripts/start.ps1                 # SQLite
./scripts/start.ps1 -Postgres       # PostgreSQL-Variante
./scripts/start.ps1 -Foreground     # mit Live-Logs (Strg+C beendet)
./scripts/stop.ps1                  # Container stoppen
```

**Manuell:**

```bash
docker compose up --build                              # SQLite
docker compose -f docker-compose.postgres.yml up --build   # PostgreSQL
```

Erreichbar unter <http://localhost:8080>. Die SQLite-Datenbank liegt im gemounteten Volume `./data`.

## Konfiguration

Datenbankprovider ist umschaltbar (`appsettings.json` oder Umgebungsvariablen):

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=data/budgetpilot.db"
  }
}
```

Für PostgreSQL: `Provider = "Postgres"` und ein Npgsql-Connection-String
(`Host=...;Database=...;Username=...;Password=...`). Per Umgebungsvariable:
`Database__Provider` / `Database__ConnectionString`.

## PWA

Die App bringt ein Web-App-Manifest und App-Icons mit und ist auf mobilen Browsern
installierbar. Offline-Betrieb (Service Worker) ist bewusst einer späteren Iteration
vorbehalten.

## Fachliche Kernregeln

| Frequenz | Budget-Sicht | Cashflow-Sicht |
|---|---|---|
| Monthly | `Betrag` | `Betrag` |
| Quarterly | `Betrag / 3` | voller Betrag, wenn Monatsabstand seit `ValidFrom` durch 3 teilbar |
| Yearly | `Betrag / 12` | voller Betrag im Zahlungsmonat (`PaymentMonth`, sonst Monat aus `ValidFrom`) |
| Once | `Betrag` im `ValidFrom`-Monat, sonst 0 | identisch zur Budget-Sicht |

Änderungen an Betrag/Frequenz ab einem Stichtag erzeugen eine **neue Version**; die alte
Version wird auf `ValidTo = Stichtag − 1 Tag` beendet. Versionen einer Position überschneiden
sich nie. Vollständige Spezifikation: `Docs/BudgetPilot – Technical Specification/requirements.md`.
