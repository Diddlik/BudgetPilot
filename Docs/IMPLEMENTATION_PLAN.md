# BudgetPilot — Umsetzungsplan (Parallel-Workflow für Claude Code · Codex · Gemini CLI)

> Quelle der Wahrheit für **was** gebaut wird: `Docs/BudgetPilot – Technical Specification/requirements.md`.
> Dieses Dokument beschreibt das **wie** und **wer wann woran** — so geschnitten, dass drei KI-CLIs
> (Claude Code, Codex, Gemini CLI) gleichzeitig arbeiten können, ohne sich gegenseitig zu blockieren.
> `CLAUDE.md` im Repo-Root ist Living-Doc; architekturrelevante Änderungen dort nachziehen.

---

## 1. Grundidee der Parallelisierung: „Contract-First, dann Tracks"

Die saubere Schichtenarchitektur (`Domain → Application → Infrastructure / Web`) erlaubt echte
Parallelarbeit — **aber nur, wenn die Verträge zuerst eingefroren sind.** Daher:

1. **Wave 0 (sequentiell, 1 Agent = „Lead"):** Solution, Projektreferenzen, Package-Management,
   **Domain vollständig** und **alle Application-Verträge** (Interfaces, DTOs, Result-Typen,
   Repository-Interfaces, DI-Extension-Stubs). Build grün. → **Verträge eingefroren.**
2. **Wave 1 (parallel, 3 Tracks gleichzeitig):** Jede CLI implementiert *gegen* die eingefrorenen
   Verträge, in **disjunkten Projektordnern** → kaum Merge-Konflikte.
3. **Wave 2 (sequentiell, Lead):** Composition Root verdrahten, App + Docker starten,
   Integrations- und Verifikationsfixes, README.

**Regel:** In Wave 1 ändert **niemand** die in Wave 0 eingefrorenen Signaturen. Braucht ein Track eine
Vertragsänderung → kurzer Sync (siehe §7), Lead passt den Vertrag an, alle ziehen nach.

---

## 2. Voraussetzungen (einmalig, durch Lead)

```bash
git init                         # Repo existiert noch NICHT — Pflicht für Branch/Worktree-Parallelität
# .NET 8 SDK empfohlen (Spec = .NET 8 LTS). Installiert ist SDK 10 — baut net8.0,
# aber zur Reproduzierbarkeit global.json mit net8.0-Roll-Forward pinnen.
dotnet tool install --global dotnet-ef        # für Migrations (Track B)
```

- **TFM:** alle Projekte `net8.0`. `global.json` mit `"rollForward": "latestFeature"` auf eine 8.0.x-SDK,
  falls verfügbar; sonst SDK 10 mit explizitem `<TargetFramework>net8.0</TargetFramework>`.
- **Solution-Format:** klassisches `BudgetPilot.sln` (maximale Tool-Kompatibilität für alle drei CLIs).
- **Central Package Management:** `Directory.Packages.props` im Root (Versionen zentral), damit Tracks
  Pakete nur per `<PackageReference Include=... />` ohne Version ergänzen.

---

## 3. Ownership-Matrix (verhindert Merge-Konflikte)

Jede Datei hat **genau einen** Besitzer-Track. Geteilte Dateien gehören dem Lead.

| Bereich / Datei | Besitzer | Anmerkung |
|---|---|---|
| `BudgetPilot.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitignore` | **Lead (W0)** | In Wave 1 nicht anfassen |
| `src/BudgetPilot.Domain/**` (Entities, Enums, Exceptions, Domain-Validierung-Signaturen) | **Lead (W0)** | Eingefroren nach W0 |
| `src/BudgetPilot.Application/**` *Verträge*: Interfaces, DTOs, Requests, Result-Typen, `IRepository`-Interfaces, `AddApplication()`-Stub | **Lead (W0)** | Eingefroren nach W0 |
| `src/BudgetPilot.Application/**` *Implementierung*: `BudgetProjectionService`, `BudgetItemService`, `CategoryService`, Mapping, Validierungs-Implementierung | **Track A** | Füllt nur Stub-Bodies + neue interne Klassen |
| `src/BudgetPilot.Infrastructure/**` (DbContext, EF-Config, Migrations, Repository-Impl, Seeding, `AddInfrastructure(config)`) | **Track B** | Implementiert `Application`-Repo-Interfaces |
| `src/BudgetPilot.Web/**` (Components, Pages, Layout, `wwwroot`, UI-State, `Program.cs`) | **Track C** | `Program.cs` ruft nur `AddApplication()` + `AddInfrastructure()` |
| `tests/BudgetPilot.Domain.Tests/**`, `tests/BudgetPilot.Application.Tests/**` | **Track D** | Testet gegen Verträge + Spec §8 |
| `Dockerfile`, `docker-compose.yml`, `docker-compose.postgres.yml`, `README.md`, CI | **Lead (W2)** | |

**Konflikt-Killer = DI-Extension-Methoden:** `AddApplication(this IServiceCollection)` (Besitzer A),
`AddInfrastructure(this IServiceCollection, IConfiguration)` (Besitzer B). `Program.cs` (Besitzer C)
ruft beide auf. So fasst niemand fremde Registrierungen in `Program.cs` an.

---

## 4. Tool-Zuordnung & Begründung

| Track | Tool | Inhalt | Warum dieses Tool |
|---|---|---|---|
| **A — Application** | **Claude Code** | Projektionslogik (Budget-/Cashflow), Versionierungs-Invarianten, Validierung | Korrektheitskritischster, „denklastigster" Teil (§5/§4 der Spec) |
| **B — Infrastructure** | **Codex** | EF Core DbContext, Mapping, Migrations, Provider-Switch, Seeding | EF-/Boilerplate-stark, klare Vorlage in Spec §3.3/§10/§12 |
| **C — Web/UI** | **Gemini CLI** | Blazor-Screens nach Prototyp, Layout, Navigation, Formular | Großer, mechanischer Umfang; Prototyp als visuelle Referenz |
| **D — Tests** | **Claude Code** (mit Track A gebündelt) | xUnit-Pflichttests §8 | Tests + Projektionslogik gehören fachlich zusammen |

> Track D läuft sinnvollerweise im selben Claude-Code-Kontext wie Track A (gleiche Korrektheits­domäne),
> kann aber auch separat parallel laufen, da Tests nur gegen die eingefrorenen Verträge schreiben.

---

## 5. Wave 0 — Foundation & Contract Freeze (Lead, sequentiell)

**Ziel:** Buildbare Solution, vollständige Domain, alle Verträge als kompilierende Stubs. Danach Tag/Commit
`w0-contracts-frozen`.

Schritte:
1. `git init` + `.gitignore` (dotnet) + `global.json`.
2. Solution + 6 Projekte + Referenzen:
   ```bash
   dotnet new sln -n BudgetPilot
   dotnet new classlib   -n BudgetPilot.Domain         -o src/BudgetPilot.Domain
   dotnet new classlib   -n BudgetPilot.Application    -o src/BudgetPilot.Application
   dotnet new classlib   -n BudgetPilot.Infrastructure -o src/BudgetPilot.Infrastructure
   dotnet new blazor     -n BudgetPilot.Web            -o src/BudgetPilot.Web --interactivity Server
   dotnet new xunit      -n BudgetPilot.Domain.Tests       -o tests/BudgetPilot.Domain.Tests
   dotnet new xunit      -n BudgetPilot.Application.Tests  -o tests/BudgetPilot.Application.Tests
   # Referenzen: App→Domain; Infra→App,Domain; Web→App,Infra; Tests→jeweilige Schicht
   ```
3. `Directory.Packages.props` (EF Core 8, EF Sqlite/Npgsql, FluentAssertions, Microsoft.NET.Test.Sdk, xunit…).
4. **Domain (vollständig, eingefroren):** Entities + Enums exakt nach Spec §3.1/§3.2; `DomainException`;
   Signatur der Versionierungs-/Validierungsregeln (Implementierung darf in Track A landen, aber die
   öffentlichen Typen/Methoden hier festlegen).
5. **Application-Verträge (eingefroren):**
   - `IBudgetProjectionService`, `IBudgetItemService`, `ICategoryService` (Spec §5/§9) — Signaturen.
   - DTOs/Requests/Result-Typen: `MonthlyBudgetProjectionDto`, `YearlyBudgetProjectionDto`,
     `BudgetProjectionLine`, `CategoryProjectionSummary`, `BudgetItemDto`, `BudgetItemVersionDto`,
     `CategoryDto`, alle `Create*/Update*Request`.
   - Repository-Interfaces: `IBudgetItemRepository`, `ICategoryRepository`, `IUnitOfWork` (o. ä.).
   - `ServiceCollectionExtensions.AddApplication()` — leerer, kompilierender Stub.
   - Service-Implementierungen als Stub mit `throw new NotImplementedException()`.
6. `dotnet build` **grün** (mit NotImplemented-Stubs). Commit + Tag `w0-contracts-frozen`.
7. **Drei Branches/Worktrees anlegen** (siehe §7) und je Track den Brief aus §6 übergeben.

---

## 6. Wave 1 — Parallele Tracks (Copy-&-Paste-Briefings)

Jeder Block ist als **Prompt für die jeweilige CLI** gedacht. Jede CLI arbeitet auf ihrem eigenen
Branch/Worktree und committet nur Dateien aus ihrer Ownership-Spalte (§3).

### Track A — Application-Logik · **Claude Code** · Branch `track/application`
```
Kontext: BudgetPilot, .NET 8 Clean-Architecture. Lies Docs/.../requirements.md §4, §5, §7
und Docs/IMPLEMENTATION_PLAN.md §3. Die Application-VERTRÄGE (Interfaces, DTOs, Repo-Interfaces)
sind eingefroren — NICHT ändern, nur implementieren.

Aufgabe:
1. BudgetProjectionService: GetMonthlyProjectionAsync / GetYearlyProjectionAsync exakt nach
   Projektionstabelle §5.1 (Monthly/Quarterly/Yearly/Once × Budget/Cashflow), Aggregation §5.2,
   gültige Version je Monat nach §4.2, Fehler bei zwei gleichzeitig gültigen Versionen (§4.1.2).
   MonatsabstandSeit(ValidFrom,y,m) = (y-VF.Year)*12 + (m-VF.Month). decimal überall.
2. BudgetItemService + CategoryService gegen die Repo-Interfaces (Create/Update/AddVersion=§4.3
   Versionsflow / UpdateCurrentVersion in-place / Deactivate/Reactivate/Delete / Drilldown).
3. Domain-Validierung §7 (Amount>=0, ValidTo>=ValidFrom, PaymentDay 1..31, PaymentMonth 1..12,
   keine Überschneidung) als prüfbare Methoden; bei Verstoß DomainException.
4. AddApplication() registriert die Services.
Definition of Done: dotnet build grün; KEINE Logik außerhalb der Application-Ownership berührt;
alle Stubs ersetzt. Verifiziere gegen die Erwartungswerte in Spec §12.
```

### Track B — Infrastructure/EF Core · **Codex** · Branch `track/infrastructure`
```
Kontext: BudgetPilot, .NET 8, EF Core 8. Lies requirements.md §3.3, §10, §11, §12 und
IMPLEMENTATION_PLAN.md §3. Application-Repo-Interfaces sind eingefroren — implementieren, nicht ändern.

Aufgabe:
1. BudgetPilotDbContext mit DbSet<BudgetItem/BudgetItemVersion/Category/ActualTransaction>.
2. EF-Konfiguration §3.3: decimal(18,2); Required-Felder; Beziehungen
   (Category 1—* Item 1—* Version, Item-Delete kaskadiert auf Versionen, Category nicht löschbar
   solange Items existieren); Indizes (Version(BudgetItemId,ValidFrom), Item(CategoryId),
   ActualTransaction(Date)); DateOnly-ValueConverter für SQLite (ISO yyyy-MM-dd).
3. Repository-Implementierungen der Application-Interfaces (Items inkl. Versionen EINMAL laden, kein N+1).
4. Provider-Switch §10: Database:Provider = Sqlite|Postgres → UseSqlite/UseNpgsql.
5. Seeding der Demo-Daten §12 beim ersten Start, wenn DB leer.
6. Initiale Migration (SQLite). AddInfrastructure(IServiceCollection, IConfiguration) registriert
   DbContext + Repositories + Seeder.
Definition of Done: dotnet build grün; dotnet ef migrations add Initial erfolgreich;
nur Infrastructure-Ownership berührt.
```

### Track C — Web/UI (Blazor) · **Gemini CLI** · Branch `track/web`
```
Kontext: BudgetPilot, Blazor Web App (Interactive Server), .NET 8, de-DE/EUR/dd.MM.yyyy.
Visuelle/verhaltensbezogene Referenz: Docs/.../BudgetPilot.dc.html (NICHT 1:1 kopieren — neu in Blazor).
Lies requirements.md §6, §13 und IMPLEMENTATION_PLAN.md §3. Application-Service-Interfaces nutzen,
KEINE Berechnungslogik und KEIN DbContext in Komponenten.

Aufgabe — Screens nach §6:
1. Layout + Navigation: Desktop Seitenleiste, Mobile Bottom-Nav. Routen:
   Dashboard · Monatsübersicht · Jahresübersicht · Budgetpositionen · Kategorien.
2. Dashboard (§6.1), Monatsübersicht (§6.2, Budget/Cashflow-Umschalter, Kategorie-Gruppierung),
   Jahresübersicht (§6.3, Balken Jan–Dez + Tabelle), Budgetpositionen-Liste (§6.4),
   Kategorien + Drilldown (§6.5), Positionsdetail/Versionierung-Timeline (§6.6),
   Formular Create/Edit/Version mit 3 Modi (§6.7, Komma+Punkt bei Betrag, Save disabled bei invalide).
3. de-DE-Formatierung, tabular-nums, Touch-Targets >=44px, 0,00-€-Zeilen gedämpft.
4. Program.cs ruft AddApplication() + AddInfrastructure(builder.Configuration).
Bis Track A/B gemergt sind: UI gegen die Interfaces bauen; optional eine simple In-Memory-Fake-Impl
nur zum lokalen Rendern (wird in Wave 2 entfernt). Definition of Done: dotnet build grün; alle Screens
navigierbar; nur Web-Ownership berührt.
```

### Track D — Tests · **Claude Code** (mit A) · Branch `track/tests`
```
Kontext: xUnit (+ FluentAssertions). Lies requirements.md §8 und §12.
Aufgabe: Alle 9 Pflichtfälle aus §8 als Given/When/Then implementieren (Monthly, Yearly Budget,
Yearly Cashflow, Quarterly Budget, Quarterly Cashflow, Versionierung, Once, keine Überschneidung,
Aktiv/Inaktiv) + Test, dass zwei gleichzeitig gültige Versionen einen Fehler werfen.
Testdaten = Seed §12; Erwartungswerte aus §12 verifizieren (z. B. März Budget-Saldo ≈ 2.064,02 €).
Definition of Done: dotnet test grün, sobald Track A gemergt ist.
```

---

## 7. Koordinations- & Merge-Protokoll

- **Isolation:** Pro Track ein Git-Worktree (kein Branch-Wechsel-Stress, parallele Builds):
  ```bash
  git worktree add ../bp-application track/application
  git worktree add ../bp-infrastructure track/infrastructure
  git worktree add ../bp-web track/web
  ```
- **Merge-Reihenfolge (Wave 1 → main):** **B (Infrastructure) → A (Application) → D (Tests) → C (Web).**
  Begründung: A/B sind unabhängig, aber Tests (D) brauchen A; Web (C) zuletzt, da es beide DI-Extensions
  verdrahtet. Dank Ownership-Matrix sind Konflikte auf `Program.cs`/`.sln`/`Directory.Packages.props`
  beschränkt — die gehören dem Lead und werden in W0 fixiert.
- **Vertragsänderung nötig?** Track stoppt nicht heimlich die Signatur, sondern meldet es → Lead ändert den
  Vertrag in `Application`/`Domain`, taggt `w0-contracts-frozen-v2`, alle Tracks rebasen. Selten halten,
  weil die Verträge in §3.1/§3.2/§5/§9 der Spec bereits vollständig spezifiziert sind.
- **Build-Gate pro Track:** kein Merge ohne lokal grünes `dotnet build` (Track D zusätzlich `dotnet test`).

---

## 8. Wave 2 — Integration & Go (Lead, sequentiell)

1. Alle Tracks nach Merge-Reihenfolge in `main` integrieren; In-Memory-Fake aus Track C entfernen.
2. `Program.cs`-Endstand: `AddApplication()` + `AddInfrastructure(config)` + Blazor; DB-Migrate + Seed
   beim Start.
3. `dotnet build` + `dotnet test` grün; App lokal starten und gegen Spec §12-Erwartungswerte sicht-prüfen
   (März-Saldo, Strom-Versionierung, Kfz Budget vs. Cashflow, Waschmaschine nur Mai).
4. `Dockerfile` (multi-stage) + `docker-compose.yml` (SQLite, Port 8080, Volume `/app/data`) +
   `docker-compose.postgres.yml` vorbereiten (§11). Container-Start verifizieren.
5. PWA vorbereiten (Manifest, Icon, Installierbarkeit) — Offline später.
6. `README.md` (lokal starten, Docker starten, Tests) + `CLAUDE.md` aktualisieren, falls Architektur
   abgewichen ist.
7. **DoD-Checkliste Spec §14** abhaken.

---

## 9. „Als Agenten poolen" — was tatsächlich geht

- **Ehrlich zur Grenze:** Aus dieser Claude-Code-Session kann ich **Claude-Subagenten** poolen
  (Tool „Agent" / `fork`, optional je in eigenem Git-Worktree via `isolation: "worktree"`).
  Ich kann **Codex** und **Gemini CLI** **nicht** direkt als Subagenten starten — das sind externe Tools.
  Cross-Tool-Parallelität entsteht dadurch, dass du die Briefings aus §6 in die jeweilige CLI einfügst.
- **Praktikables Modell:**
  - **Lead = ich (diese Session):** Wave 0 + Wave 2 + Koordination/Merges.
  - **Track A + D:** kann ich als Claude-Subagent(en) im Worktree `../bp-application` poolen.
  - **Track B (Codex)** und **Track C (Gemini):** du startest sie mit den §6-Briefings in ihren Worktrees.
- **Reihenfolge:** Pooling erst **nach** Wave 0 sinnvoll — vorher gibt es keine Verträge, gegen die
  parallel gearbeitet werden kann.

---

## 10. Nächster Schritt

Vorschlag: Ich führe **Wave 0** jetzt aus (git init, Solution, Domain, eingefrorene Verträge, grüner
Build, Worktrees) und poole danach **Track A/D** als Claude-Subagenten. Track B/C startest du mit den
fertigen Briefings aus §6 in Codex bzw. Gemini CLI. Sag Bescheid, ob ich mit Wave 0 starten soll.
