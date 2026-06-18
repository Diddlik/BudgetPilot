# BudgetPilot — Detaillierte Anforderungen für die Umsetzung

> Implementierungs-Spezifikation für Claude Code. Sie führt die fachliche Ausgangs-Spec mit den
> konkreten Entscheidungen aus dem geprüften UI-Prototyp (`BudgetPilot.dc.html` / `BudgetApp.dc.html`)
> zusammen. Wo der Prototyp ein Verhalten festlegt, ist es hier als verbindliche Anforderung formuliert.

---

## 0. Auftrag in einem Satz

Baue **BudgetPilot**, ein lokal betreibbares Haushaltsbudget-Tool (responsive Web-App / PWA-fähig) mit
**.NET / Blazor**, das Einnahmen und Ausgaben als **zeitlich versionierte Budgetregeln** verwaltet und
daraus **Monats- und Jahresübersichten** in zwei Berechnungsarten (**Budget-Sicht** und **Cashflow-Sicht**)
erzeugt. Historische Monate dürfen sich durch spätere Änderungen nie verändern.

---

## 1. Technischer Stack (verbindlich)

```text
Frontend:          Blazor Web App (Interactive Server ODER WebAssembly — siehe 1.1)
Backend/Hosting:   ASP.NET Core (.NET 8 LTS)
Sprache:           C#
ORM:               Entity Framework Core 8
Datenbank MVP:     SQLite
Datenbank später:  PostgreSQL (Provider konfigurierbar, siehe §10)
Tests:             xUnit (+ FluentAssertions empfohlen)
Deployment:        Dockerfile + docker-compose
Lokalisierung:     de-DE, Währung EUR, Datumsformat dd.MM.yyyy
```

### 1.1 Render-Modus
- MVP: **Blazor Web App, Interactive Server** (einfachstes Hosting, kein separates API-Projekt nötig).
- Architektur trotzdem so schneiden, dass die Application-Schicht später über eine HTTP-API
  oder von einer nativen App genutzt werden kann (siehe §9). **Keine** Domain-/Berechnungslogik
  in Razor-Komponenten.

### 1.2 Geldbeträge & Datum
- **Immer `decimal`** für Geld (nie `double`/`float`).
- **`DateOnly`** für fachliche Datumswerte (`ValidFrom`, `ValidTo`, `ActualTransaction.Date`).
- `DateTime` nur für technische Zeitstempel (`CreatedAt`, `UpdatedAt`).

---

## 2. Projektstruktur (Clean-ish, schichtenbasiert)

```text
BudgetPilot/
 ├─ src/
 │   ├─ BudgetPilot.Domain/          # Entities, Enums, Domain-Regeln, Domain-Exceptions
 │   ├─ BudgetPilot.Application/     # Services, DTOs, Use Cases, Interfaces, Projektionslogik
 │   ├─ BudgetPilot.Infrastructure/  # EF Core DbContext, Repositories, Migrations, Seeding
 │   └─ BudgetPilot.Web/             # Blazor-Komponenten, Seiten, Layout, UI-State
 ├─ tests/
 │   ├─ BudgetPilot.Domain.Tests/
 │   └─ BudgetPilot.Application.Tests/
 ├─ docker-compose.yml
 ├─ Dockerfile
 ├─ README.md
 └─ docs/requirements.md
```

Abhängigkeitsrichtung: `Web → Application → Domain`; `Infrastructure → Application/Domain`.
`Web` kennt `Infrastructure` nur über DI-Registrierung (Composition Root in `Program.cs`).

---

## 3. Domänenmodell

### 3.1 Enums

```csharp
public enum BudgetItemType { Income = 1, Expense = 2 }
public enum BudgetFrequency { Monthly = 1, Quarterly = 2, Yearly = 3, Once = 4 }
public enum BudgetViewMode { Budget = 1, Cashflow = 2 }
```

### 3.2 Entities

```csharp
public class BudgetItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BudgetItemType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BudgetItemVersion> Versions { get; set; } = new();
}

public class BudgetItemVersion
{
    public Guid Id { get; set; }
    public Guid BudgetItemId { get; set; }
    public BudgetItem BudgetItem { get; set; } = null!;
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }      // null = offen / unbegrenzt
    public int? PaymentDay { get; set; }         // 1..31, optional
    public int? PaymentMonth { get; set; }       // 1..12, optional (Yearly/Quarterly)
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BudgetItem> BudgetItems { get; set; } = new();
}

// Für späteren Plan/Ist-Vergleich. Tabelle anlegen, UI im MVP NICHT nötig.
public class ActualTransaction
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public BudgetItemType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? BudgetItemId { get; set; }
    public BudgetItem? BudgetItem { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3.3 EF Core Konfiguration
- `decimal` → Precision `(18,2)` für alle Geldfelder.
- Required: `BudgetItem.Name`, `BudgetItem.Type`, `BudgetItem.CategoryId`, `Category.Name`,
  `BudgetItemVersion.Amount`, `Frequency`, `ValidFrom`.
- Beziehungen: `Category 1—* BudgetItem 1—* BudgetItemVersion`; `BudgetItem`-Löschung kaskadiert
  auf seine Versionen. `Category` darf nicht gelöscht werden, solange Items existieren (siehe §6.4).
- Indizes: `BudgetItemVersion(BudgetItemId, ValidFrom)`, `BudgetItem(CategoryId)`,
  `ActualTransaction(Date)`.
- `DateOnly`-Mapping: bei SQLite ggf. `ValueConverter` auf `string` (ISO `yyyy-MM-dd`) konfigurieren,
  damit Vergleiche/Sortierung korrekt sind.

---

## 4. Kernfachlichkeit: Versionierung

Eine **Budgetposition** (`BudgetItem`) hat **≥ 1 Version** (`BudgetItemVersion`). Eine Version beschreibt
Betrag + Frequenz + Gültigkeitszeitraum. Änderungen am Betrag/Rhythmus ab einem Stichtag erzeugen eine
**neue Version**; die alte wird beendet. So bleiben historische Auswertungen stabil.

### 4.1 Invarianten (müssen immer gelten)
1. Versionen desselben `BudgetItem` **überschneiden sich nicht** (Zeitintervalle disjunkt).
2. Für einen Monat M existiert **höchstens eine** gültige Version je Item (mehr = Datenfehler → Exception).
3. Soll eine Position durchgehend aktiv sein, dürfen **keine Lücken** zwischen Versionen entstehen
   (beim „Neue Version ab Datum"-Flow automatisch sicherstellen, siehe 4.3).

### 4.2 Gültigkeitsregel für Monat M (Jahr `y`, Monat `m`)
Eine Version ist in M gültig, wenn:
```text
ValidFrom <= letzter Tag von M     UND     (ValidTo == null ODER ValidTo >= erster Tag von M)
```

### 4.3 „Neue Version ab Datum" (Standard-Änderungsflow)
Beim Ändern von Betrag/Frequenz ab Stichtag `D`:
1. Neue Version mit `ValidFrom = D`, `ValidTo = null`.
2. Bisher offene (aktuelle) Version: `ValidTo = D - 1 Tag`.
3. Reihenfolge bleibt lückenlos und überschneidungsfrei.

> Der UI-Prototyp setzt **standardmäßig** auf diesen sicheren Flow. Eine „rückwirkend ändern"-Option
> ist im UI vorgesehen (Radio-Auswahl), aber im MVP optional / nachrangig. Wenn implementiert: ändert
> die aktuelle Version **in-place** ohne neue Version.

### 4.4 Bearbeiten vs. Neue Version (aus dem Prototyp)
- **Bearbeiten (in-place):** ändert Stammdaten (Name/Typ/Kategorie/Notiz) und überschreibt die
  **aktuelle** (= jüngste, offene) Version. Für reine Tippfehler/Korrekturen.
- **Neue Version anlegen:** der Versionierungsflow aus 4.3 — für „ab Datum X kostet es anders".
- **Löschen:** entfernt das Item inkl. **aller** Versionen (Hard-Delete; mit Bestätigungsdialog).
- **Deaktivieren/Reaktivieren:** `IsActive` umschalten. Inaktive Items verschwinden aus zukünftigen
  Planungen, bleiben aber für historische Auswertungen und in der Positionsliste (ausgegraut) sichtbar.

---

## 5. Berechnungslogik (Projektion)

Liegt in `BudgetPilot.Application`, **nicht** in der UI. Muss deterministisch und unit-getestet sein.

```csharp
public interface IBudgetProjectionService
{
    Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(int year, int month, BudgetViewMode viewMode);
    Task<YearlyBudgetProjectionDto>  GetYearlyProjectionAsync(int year, BudgetViewMode viewMode);
}
```

Result-DTOs (analog zur Spec): `MonthlyBudgetProjection` (Year, Month, ViewMode, TotalIncome,
TotalExpense, Balance, `List<BudgetProjectionLine>`, `List<CategoryProjectionSummary>`),
`BudgetProjectionLine` (BudgetItemId, Name, CategoryName, Type, Frequency, Amount,
ProjectedMonthlyAmount, Note), `YearlyBudgetProjection` (Year, ViewMode, `List<MonthlyBudgetProjection>`,
Jahressummen).

### 5.1 Pro Monat M je aktivem Item: gültige Version ermitteln (§4.2), dann `ProjectedMonthlyAmount`:

| Frequenz | Budget-Sicht | Cashflow-Sicht |
|---|---|---|
| **Monthly** | `Amount` | `Amount` |
| **Quarterly** | `Amount / 3` | `Amount`, wenn `MonatsabstandSeit(ValidFrom) % 3 == 0`, sonst `0` |
| **Yearly** | `Amount / 12` | `Amount` im Zahlungsmonat, sonst `0`. Zahlungsmonat = `PaymentMonth`, fällt zurück auf Monat aus `ValidFrom` |
| **Once** | `Amount` im Monat von `ValidFrom`, sonst `0` | identisch zu Budget-Sicht |

`MonatsabstandSeit(ValidFrom, y, m) = (y - ValidFrom.Year) * 12 + (m - ValidFrom.Month)`.

### 5.2 Aggregation
- `TotalIncome` = Summe `ProjectedMonthlyAmount` aller Income-Items; `TotalExpense` analog für Expense.
- `Balance = TotalIncome - TotalExpense`.
- `CategoryProjectionSummary`: Ausgaben je Kategorie summieren (in der jeweiligen Sicht).
- Jahresübersicht = 12 Monatsprojektionen + Jahressummen; Bar-Chart Einnahmen vs. Ausgaben je Monat.

### 5.3 Performance
Datenmengen sind klein. Trotzdem: Items inkl. Versionen **einmal** laden (kein N+1 / keine DB-Abfrage
pro Tabellenzeile). Jahresübersicht ruft die Monatsberechnung 12× auf derselben in-memory Datenbasis auf.

---

## 6. Funktionale Anforderungen (Screens & Verhalten)

Die Navigation entspricht dem Prototyp: **Dashboard · Monatsübersicht · Jahresübersicht ·
Budgetpositionen · Kategorien**. Desktop: Seitenleiste. Mobile: Bottom-Navigation.

### 6.1 Dashboard
- KPI-Karten: Einnahmen, Ausgaben, Saldo für den aktuellen Monat (Budget-Sicht).
- „Ausgaben nach Kategorie" als horizontale Balken (Budget-Sicht).
- „Nächste große Zahlungen": kommende einmalige/quartals-/jährliche Cashflow-Fälligkeiten der nächsten
  12 Monate, aufsteigend, gefiltert auf größere Beträge.

### 6.2 Monatsübersicht
- Monatsnavigation (vor/zurück, über Jahresgrenze).
- Umschalter **Budget-Sicht / Cashflow-Sicht** + erklärender Hinweistext.
- KPI-Karten (Einnahmen/Ausgaben/Saldo) in der gewählten Sicht.
- Einnahmen-Gruppe + Ausgaben **gruppiert nach Kategorie** mit Zwischensumme je Kategorie.
- Pro Zeile: Name, Frequenz, Betrag; bei lumpy Kosten ein Tag „anteilig" (Budget) bzw.
  „fällig" / „nicht fällig" (Cashflow). Nicht fällige Beträge (0,00 €) gedämpft darstellen.

### 6.3 Jahresübersicht
- Jahresnavigation + Sicht-Umschalter.
- Balkendiagramm Jan–Dez (Einnahmen vs. Ausgaben), aktueller Monat hervorgehoben.
- KPI-Karten Jahressummen + Jahressaldo.
- Tabelle Jan–Dez: Einnahmen / Ausgaben / Saldo je Monat (Saldo grün/rot).

### 6.4 Budgetpositionen
- Liste aller Positionen (aktiv + inaktiv). Inaktive ausgegraut.
- Pro Zeile: Name, Status-Pill (Aktiv/Inaktiv), Kategorie, Frequenz, Anzahl Versionen, Typ-Pill, Betrag.
- Filter (mind. vorbereiten): nach Kategorie, nach Typ (Einnahme/Ausgabe), nach Aktiv/Inaktiv.
- **„+ Neue Ausgabe"** → Formular (§6.7).
- Klick auf Zeile → **Detail/Versionierung** (§6.6).

### 6.5 Kategorien (mit Drilldown)
- Kartenraster aller Kategorien: Name, Anzahl Positionen, monatliches Budget der Kategorie
  (Einkommens-Kategorie zeigt „—").
- **Kategorie ist klickbar → Kategorie-Detailansicht:**
  - Kopf: Kategoriename, Anzahl Positionen, Monatsbudget, Button „+ Ausgabe hinzufügen"
    (Kategorie ist vorausgewählt).
  - Liste **aller Positionen dieser Kategorie** (z. B. *Versicherungen → Kfz-Versicherung, Haftpflicht*).
  - Klick auf eine Position → deren Detail/Versionierung.
  - Leerer Zustand, wenn keine Positionen vorhanden.
- „+ Neue Kategorie" anlegen; Kategorien umbenennen; deaktivieren. Deaktivierte Kategorien bleiben
  für historische Auswertungen sichtbar. Eine Kategorie mit zugeordneten Items darf nicht hart
  gelöscht werden (nur deaktivieren).

### 6.6 Positionsdetail / Versionierung
- Kopf: Name, Typ-Pill, Status-Pill + Aktionen **Bearbeiten**, **Deaktivieren/Reaktivieren**, **Löschen**.
- „Betrag ab einem Datum ändern": Auswahl **Neue Version ab Datum** (Default, empfohlen) vs.
  **rückwirkend** (optional) + Button **„Neue Version anlegen"** → öffnet Formular im Versionsmodus.
- **Versionshistorie** als Timeline (neueste oben): je Version Betrag, Frequenz, Badge Aktuell/Historisch,
  Gültig ab, Gültig bis (offen), Zahlung (Monat / „jeden Monat" / „aus Gültig ab"), Notiz.
- **Löschen** öffnet Bestätigungsdialog („… wird mit allen Versionen entfernt"). Nach Löschen zurück
  zur Positionsliste.

### 6.7 Formular „Position anlegen / bearbeiten / neue Version"
Ein Formular (Modal/Sheet) mit drei Modi, gespeist aus demselben Speicher-Pfad:

| Feld | Typ | Pflicht | Hinweise |
|---|---|---|---|
| Name | Text | ja | nicht leer |
| Typ | Select | ja | Ausgabe / Einnahme |
| Kategorie | Select | ja | aus aktiven Kategorien; vorbelegt bei Drilldown |
| Betrag (EUR) | Text/decimal | ja | `>= 0`; akzeptiert Komma **und** Punkt |
| Frequenz | Select | ja | Monatlich / Quartalsweise / Jährlich / Einmalig |
| Gültig ab / Datum | Date | ja | bei „Einmalig" = Ereignisdatum; bei Versionsmodus = Stichtag |
| Zahlungsmonat | Select | optional | **nur** bei Jährlich/Quartalsweise sichtbar; leer = „aus Gültig ab" |
| Notiz | Text | nein | |

Verhalten:
- **Create:** neues Item + erste Version. Nach Speichern → Detailseite des neuen Items.
- **Edit:** Stammdaten + aktuelle Version in-place aktualisieren.
- **Version:** Versionsflow §4.3 (alte Version beenden, neue anhängen).
- Speichern-Button **disabled**, solange Name leer oder Betrag ungültig.
- Abbrechen/Schließen verwirft Eingaben.

---

## 7. Validierung

### 7.1 BudgetItem
- `Name` nicht leer; `Type` gesetzt; `CategoryId` gesetzt und existierende, **aktive** Kategorie.

### 7.2 BudgetItemVersion
- `Amount >= 0`; `Frequency` gesetzt; `ValidFrom` gesetzt.
- `ValidTo` (falls gesetzt) **nicht vor** `ValidFrom`.
- `PaymentDay` nur 1..31; `PaymentMonth` nur 1..12.
- Keine Überschneidung mit anderen Versionen desselben Items → sonst `DomainException` /
  Validierungsfehler, **nicht** speichern.

### 7.3 Frequenz-spezifisch
- **Monthly:** `PaymentMonth` ignoriert; `PaymentDay` optional.
- **Quarterly:** `PaymentMonth` optional (definiert Quartalsraster, sonst aus `ValidFrom`).
- **Yearly:** `PaymentMonth` optional, empfohlen; fehlt er → Monat aus `ValidFrom`.
- **Once:** `ValidFrom` = Ereignismonat; `ValidTo` sollte `null` sein.

### 7.4 Fehlerdarstellung
Validierungs- und Datenfehler (z. B. überschneidende Versionen) müssen verständlich in der UI
angezeigt werden; ungültige Beträge/Frequenzen werden nicht gespeichert.

---

## 8. Tests (xUnit) — Pflicht

Berechnungslogik + Versionierung + Validierung sind testpflichtig. Mindestens diese Fälle (Given/When/Then):

1. **Monthly:** Miete 1200 €/Monat ab 01.01.2026 → März 2026 = 1200 €.
2. **Yearly Budget-Sicht:** Kfz 720 €/Jahr ab 01.01.2026 → März Budget = 60 €.
3. **Yearly Cashflow:** Kfz 720 €, `PaymentMonth`=März → März = 720 €, April = 0 €.
4. **Quarterly Budget-Sicht:** 300 €/Quartal → Februar Budget = 100 €.
5. **Quarterly Cashflow (Start Januar):** Januar = 300 €, Februar = 0 €, April = 300 €.
6. **Versionierung:** Strom 120 €/Monat ab 01.01.2026; Änderung auf 145 € ab 01.03.2026 →
   Februar = 120 €, März = 145 €.
7. **Once:** Waschmaschine 600 € am 15.05.2026 → Mai = 600 €, Juni = 0 €.
8. **Keine Überschneidung:** neue Version ab Februar bei bestehender Jan–März-Version → bestehende
   wird korrekt beendet **oder** Operation wird abgelehnt (kein überlappender Zustand).
9. **Aktiv/Inaktiv:** inaktives Item taucht in zukünftiger Projektion nicht auf.

Zusätzlich empfohlen: Test, dass `GetMonthlyProjection` bei zwei gleichzeitig gültigen Versionen
einen Fehler wirft (Invariante §4.1.2).

---

## 9. Application-Services (für UI + spätere API)

```csharp
public interface IBudgetItemService
{
    Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request);          // Item + erste Version
    Task<BudgetItemDto> UpdateMetadataAsync(Guid id, UpdateBudgetItemMetadataRequest request);
    Task<BudgetItemVersionDto> AddVersionAsync(Guid budgetItemId, CreateBudgetItemVersionRequest request); // §4.3
    Task UpdateCurrentVersionAsync(Guid budgetItemId, UpdateVersionRequest request); // in-place Edit
    Task<IReadOnlyList<BudgetItemDto>> GetAllAsync();
    Task<BudgetItemDto?> GetByIdAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task ReactivateAsync(Guid id);
    Task DeleteAsync(Guid id);                                                 // Hard-Delete inkl. Versionen
}

public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<IReadOnlyList<BudgetItemDto>> GetItemsByCategoryAsync(Guid categoryId); // für Drilldown §6.5
    Task RenameAsync(Guid id, string newName);
    Task DeactivateAsync(Guid id);
}

public interface IBudgetProjectionService { /* siehe §5 */ }
```

- DTOs für UI/API-Kommunikation; Entities nicht direkt an die UI durchreichen.
- Repositories als Interfaces in `Application`, Implementierung in `Infrastructure`.
- `AddVersionAsync` kapselt die Versionierungs-Invarianten (§4.1/§4.3) serverseitig — die UI verlässt
  sich darauf, nicht auf Client-Logik.

---

## 10. Datenbank & Konfiguration

Provider per Konfiguration umschaltbar:

```json
{
  "Database": { "Provider": "Sqlite", "ConnectionString": "Data Source=/app/data/budgetpilot.db" }
}
```
Später:
```json
{
  "Database": { "Provider": "Postgres",
    "ConnectionString": "Host=db;Database=budgetpilot;Username=budgetpilot;Password=..." }
}
```
- In `Program.cs` Provider lesen und `UseSqlite` / `UseNpgsql` entsprechend wählen.
- Migrations für beide Provider lauffähig halten (oder getrennte Migrations-Assemblies).
- **Seeding** der Demo-Daten (§12) beim ersten Start, wenn DB leer.

---

## 11. Deployment

`Dockerfile` (multi-stage build → runtime), plus:

```yaml
services:
  budgetpilot:
    image: budgetpilot:latest
    container_name: budgetpilot
    ports: ["8080:8080"]
    volumes: ["./data:/app/data"]
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__Provider=Sqlite
      - Database__ConnectionString=Data Source=/app/data/budgetpilot.db
```
PostgreSQL-Variante (mit `db`-Service) als auskommentierte/zweite Compose-Datei vorbereiten.

---

## 12. Demo-/Seed-Daten (für Entwicklung & Tests)

```text
Kategorie Einkommen
  - Gehalt: 3500 € monatlich ab 01.01.2026

Kategorie Wohnen
  - Miete: 1200 € monatlich ab 01.01.2026

Kategorie Energie
  - Strom: 120 € monatlich ab 01.01.2026 ; Änderung auf 145 € ab 01.03.2026   (→ zwei Versionen)

Kategorie Abos
  - Netflix: 15,99 € monatlich ab 01.01.2026
  - Amazon Prime: 89,90 € jährlich ab 01.02.2026 (PaymentMonth = Februar)

Kategorie Versicherungen
  - Kfz-Versicherung: 720 € jährlich, PaymentMonth = März
  - Haftpflicht: 90 € jährlich, PaymentMonth = Januar

Kategorie Haushalt
  - Waschmaschine: 600 € einmalig am 15.05.2026
```

Erwartete Ergebnisse (zur Verifikation):
- Strom: Jan 120 €, Feb 120 €, **ab März 145 €**.
- Kfz Budget-Sicht: 60 €/Monat; Cashflow: 720 € **nur** im März.
- Waschmaschine: nur Mai 2026.
- März 2026 (Budget-Sicht): Einnahmen 3.500 €, Ausgaben ≈ 1.435,98 € (Miete 1200 + Strom 145 +
  Netflix 15,99 + Kfz 60 + Haftpflicht 7,50 + Prime 7,49), Saldo ≈ 2.064,02 €.

---

## 13. UI-/Qualitäts-Leitlinien

- **Responsive zuerst:** Desktop = Tabellen/Seitenleiste; Mobile = Karten, Bottom-Nav, Touch-Targets
  ≥ 44 px, einhändig bedienbar.
- Lokalisierung de-DE: EUR (`1.234,56 €`), Datum `dd.MM.yyyy`, `tabular-nums` für Beträge.
- **Keine** Berechnungslogik und **keine** direkten `DbContext`-Zugriffe in Razor-Komponenten.
- Services über Interfaces; kleine, klar benannte Methoden.
- PWA **vorbereiten** (Manifest, App-Icon, Installierbarkeit), Offline-Funktionen erst später.

---

## 14. MVP-Definition of Done

- [ ] Kategorie anlegen / umbenennen / deaktivieren; Drilldown Kategorie → Positionsliste.
- [ ] Position (Einnahme/Ausgabe) in allen vier Frequenzen anlegen, bearbeiten, löschen,
      deaktivieren/reaktivieren.
- [ ] Betrag ab einem Monat ändern → neue Version; alte Monate unverändert.
- [ ] Monatsübersicht korrekt in Budget- **und** Cashflow-Sicht.
- [ ] Jahresübersicht korrekt inkl. Sichtenvergleich.
- [ ] Unit-Tests aus §8 vorhanden und **grün**.
- [ ] App lokal **und** in Docker (SQLite) startbar; Seed-Daten vorhanden.
- [ ] Oberfläche auf Smartphone-Breite nutzbar.

---

## 15. Bewusst NICHT im MVP

Bankimport, automatische Kategorisierung, OCR, Mehrbenutzer/Familienfreigabe, Push, native Android-App,
Kontosync, KI-Analyse, komplexe Forecasts, Steuer-Auswertung, vollständiger Offline-Modus, CSV-Import,
echte Ist-Buchungen (Tabelle `ActualTransaction` nur vorbereiten).

## 16. Spätere Erweiterungen (Architektur offenhalten)

Plan/Ist-Vergleich (`ActualTransaction`), CSV-Import mit Spaltenmapping & Dublettenerkennung,
Auto-Kategorien per Regeln, Szenarien (z. B. „nach Gehaltserhöhung"), Mehrere Haushalte/Budgets,
Authentifizierung (ASP.NET Core Identity), Export nach Excel/CSV.

---

### Hinweis zum Prototyp
Der HTML-Prototyp (`BudgetPilot.dc.html`) dient als **visuelle und verhaltensbezogene Referenz** für
Layout, Screens, Flows (CRUD, Versionierung, Kategorie-Drilldown) und Beispieldaten. Er ist **keine**
Implementierungsvorlage in Code — die produktive Umsetzung erfolgt nach diesem Dokument in .NET/Blazor.
