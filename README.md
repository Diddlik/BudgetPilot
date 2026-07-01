# BudgetPilot

BudgetPilot ist eine selbst gehostete Haushaltsbudget-App für wiederkehrende und
einmalige Einnahmen und Ausgaben. Budgetregeln sind zeitlich versioniert:
Änderungen gelten ab einem Stichtag, ohne historische Monate nachträglich zu
verändern.

Die App bietet zwei Sichtweisen:

- **Budget:** unregelmäßige Kosten werden gleichmäßig auf Monate verteilt.
- **Cashflow:** Beträge erscheinen im tatsächlichen Zahlungsmonat.

Die Benutzeroberfläche ist deutschsprachig und verwendet EUR sowie deutsche
Datums- und Zahlenformate.

## Screenshots

### Web-App

<img width="1752" height="971" alt="BudgetPilot Monatsübersicht" src="https://github.com/user-attachments/assets/ae10caae-07b7-4f15-b568-0eede34bd127" />
<img width="1733" height="883" alt="BudgetPilot Jahresübersicht" src="https://github.com/user-attachments/assets/e4570dc3-3049-482e-9ecb-4ce01167fb8b" />
<img width="1610" height="922" alt="BudgetPilot Budgetpositionen" src="https://github.com/user-attachments/assets/2a3f1951-53f8-4cde-b90b-6672427534d2" />

### Android-App

<p>
  <img width="276" alt="BudgetPilot Android Login" src="https://github.com/user-attachments/assets/1a18b6bf-0d18-4324-a697-9b17f175c4d1" />
  <img width="275" alt="BudgetPilot Android Dashboard" src="https://github.com/user-attachments/assets/4148137a-efae-49f4-b5e6-9b27bbc88af3" />
</p>

## Funktionen

- Monats-, Jahres- und Mehrjahresplanung
- Budget- und Cashflow-Sicht
- Monatliche, quartalsweise, jährliche und einmalige Budgetpositionen
- Historisch sichere Versionierung ab einem frei wählbaren Stichtag
- Kategorien und Änderungsprotokoll
- Private Anmeldung über ASP.NET Core Identity
- Versionierte REST-API mit Bearer-Token-Authentifizierung
- Installierbare Web-App (PWA-Grundgerüst)
- Android-Clients für dieselbe selbst gehostete Instanz

## Schnellstart

### Lokal mit .NET

Voraussetzung ist das in `global.json` festgelegte .NET-8-SDK.

```bash
dotnet run --project src/BudgetPilot.Web
```

Beim ersten Start werden Datenbankmigrationen und Demo-Daten automatisch
angelegt. Die URL steht anschließend in der Konsolenausgabe.

In der Entwicklungsumgebung wird dieses Konto erzeugt:

```text
E-Mail:   admin@budgetpilot.local
Passwort: ChangeMe!2026
```

### Lokal mit Docker

```powershell
Copy-Item .env.example .env
./scripts/start.ps1
```

Danach ist BudgetPilot unter <http://localhost:8080> erreichbar. Zugangsdaten
werden in `.env` über `BP_AUTH_EMAIL` und `BP_AUTH_PASSWORD` konfiguriert.

Alternativ lässt sich Docker Compose direkt verwenden:

```bash
docker compose up --build
```

Die SQLite-Datenbank liegt im eingebundenen Verzeichnis `./data`.

## Android

Das Repository enthält zwei bewusst getrennte Client-Ansätze:

- [`androidNet/`](androidNet/) — .NET MAUI Blazor Hybrid, der umfassendere
  Mobile-Client mit Lese-/Schreibpfaden, Offline-Cache, PIN/Biometrie und
  Release-Skripten.
- [`android/`](android/) — Kotlin und Jetpack Compose als separater nativer
  Client-Prototyp.

Der MAUI-Client verwendet ein eigenes .NET-10-SDK und Android API 36.1:

```bash
cd androidNet
dotnet workload restore
dotnet build BudgetPilot.Mobile.csproj -f net10.0-android36.1
```

Beim Login werden HTTP oder HTTPS und anschließend nur Hostname und optional
Port gewählt. Für produktiv erreichbare Instanzen sollte immer HTTPS verwendet
werden.

Weitere Details stehen in den READMEs der jeweiligen Android-Projekte und in
[`Docs/ANDROID_APP_REQUIREMENTS.md`](Docs/ANDROID_APP_REQUIREMENTS.md).

Datenschutzrichtlinie:
<https://diddlik.github.io/BudgetPilot/privacy/>

## Produktion und automatische Updates

Die empfohlene Server-Variante zieht das auf GitHub Actions gebaute Image aus
GHCR. Watchtower prüft alle fünf Minuten auf Updates, sichert vor dem Austausch
die SQLite-Datenbank und startet den Container mit dem neuen Image neu.

```bash
cp .env.example .env
# BP_AUTH_EMAIL und BP_AUTH_PASSWORD in .env setzen
docker compose -f docker-compose.deploy.yml up -d
```

Standardimage: `ghcr.io/diddlik/budgetpilot:latest`

Das Datenverzeichnis `./data` bleibt bei Container-Updates erhalten. Über
`BP_IMAGE=ghcr.io/diddlik/budgetpilot:sha-<commit>` kann gezielt auf einen
bestimmten Stand zurückgegangen werden.

Für öffentlich erreichbare Installationen muss ein TLS-Reverse-Proxy
vorgeschaltet werden. Eine Caddy-Konfiguration mit automatischen
Let's-Encrypt-Zertifikaten ist enthalten:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Port 8080 darf nicht ungeschützt ins Internet veröffentlicht werden.

## Architektur

```text
src/BudgetPilot.Domain          Entitäten, Enums und Domainregeln
src/BudgetPilot.Application     Services, DTOs und Repository-Schnittstellen
src/BudgetPilot.Infrastructure  EF Core, Repositories, Migrationen und Seeding
src/BudgetPilot.Web             Blazor-UI, REST-API und Composition Root
tests/                          Domain-, Application- und Integrationstests
```

Abhängigkeitsrichtung:

```text
Web → Application → Domain
Infrastructure → Application + Domain
```

Berechnungslogik bleibt in Domain und Application. UI und API arbeiten mit DTOs
und greifen nicht direkt auf den EF-Core-Kontext zu.

Technische Planung und Mobile-Anforderungen:

- [`Docs/IMPLEMENTATION_PLAN.md`](Docs/IMPLEMENTATION_PLAN.md)
- [`Docs/ANDROID_APP_REQUIREMENTS.md`](Docs/ANDROID_APP_REQUIREMENTS.md)
- [`AGENTS.md`](AGENTS.md)

## Tests

```bash
dotnet build
dotnet test
dotnet test tests/BudgetPilot.Domain.Tests
dotnet test --filter "FullyQualifiedName~Quarterly"
dotnet test androidNet.Tests/BudgetPilot.Mobile.Tests.csproj -m:1
```

Der MAUI-Android-Build wird separat aus `androidNet/` ausgeführt und ist bewusst
nicht Teil der normalen .NET-/Docker-CI.

## Fachliche Kernregeln

| Frequenz | Budget-Sicht | Cashflow-Sicht |
|---|---|---|
| Monatlich | voller Betrag | voller Betrag |
| Quartalsweise | Betrag / 3 pro Monat | voller Betrag alle drei Monate |
| Jährlich | Betrag / 12 pro Monat | voller Betrag im Zahlungsmonat |
| Einmalig | Betrag im Startmonat | Betrag im Startmonat |

Änderungen an Betrag oder Frequenz erzeugen eine neue Version. Die vorherige
Version endet am Tag vor dem neuen Gültigkeitsbeginn. Versionen derselben
Budgetposition dürfen sich nie überschneiden.
