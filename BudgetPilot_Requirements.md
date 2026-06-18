# BudgetPilot – Requirements & Technical Specification

## 1. Ziel des Projekts

**BudgetPilot** ist ein webbasiertes Haushaltsbudget-Tool zur Planung, Pflege und Analyse von privaten Einnahmen und Ausgaben.

Das Tool soll es ermöglichen, regelmäßige und einmalige Kosten strukturiert zu erfassen, Änderungen ab einem bestimmten Zeitpunkt sauber zu versionieren und daraus Monats-, Quartals- und Jahresübersichten zu berechnen.

Das Ziel ist zunächst eine **responsive Web-App / PWA**, die sowohl am Desktop als auch auf dem Smartphone nutzbar ist. Eine native Android-App kann später ergänzt werden, soll aber nicht Bestandteil des ersten MVPs sein.

---

## 2. Produktname

Arbeitstitel:

```text
BudgetPilot
```

Alternative Namen, falls später gewünscht:

- BudgetKompass
- BudgetFlow
- MeinBudget
- Kassenblick
- FinanzPlaner

Für die initiale Umsetzung wird der Name **BudgetPilot** verwendet.

---

## 3. Zielplattform

### 3.1 Primäre Zielplattform

Die Anwendung soll zunächst als **responsive Web-App** umgesetzt werden.

Anforderungen:

- Nutzbar auf Desktop-Browsern.
- Nutzbar auf Android-Smartphones.
- Mobile Darstellung soll von Anfang an berücksichtigt werden.
- Die Anwendung soll später als PWA installierbar sein.
- Eine native Android-App ist vorerst nicht Bestandteil des MVPs.

### 3.2 Technologievorschlag

Empfohlener Stack:

```text
Frontend:        Blazor Web App / Razor Components
Backend:         ASP.NET Core
Datenbank MVP:   SQLite
Datenbank später: PostgreSQL
ORM:             Entity Framework Core
Auth später:     ASP.NET Core Identity
Deployment:      Docker / Docker Compose
Tests:           xUnit
```

Begründung:

- Der Entwickler arbeitet bereits mit .NET/C#.
- Backend, UI-Logik und Domain-Logik können in einer Sprache umgesetzt werden.
- Blazor eignet sich gut für interne Tools und responsive Web-Anwendungen.
- SQLite reicht für den MVP aus.
- PostgreSQL kann später für produktives Hosting genutzt werden.
- Docker ermöglicht einfaches Self-Hosting.

---

## 4. MVP-Scope

Der MVP soll sich auf die Planungslogik konzentrieren.

### 4.1 Im MVP enthalten

Der MVP muss folgende Funktionen enthalten:

- Einnahmen erfassen.
- Ausgaben erfassen.
- Einmalige Kosten erfassen.
- Monatliche Kosten erfassen.
- Quartalsweise Kosten erfassen.
- Jährliche Kosten erfassen.
- Kostenpositionen ab einem bestimmten Datum ändern.
- Änderungen dürfen nur für Folgemonate gelten.
- Historische Monate dürfen durch spätere Änderungen nicht verändert werden.
- Monatsübersicht anzeigen.
- Jahresübersicht anzeigen.
- Budget-Sicht mit anteilig verteilten jährlichen/quartalsweisen Kosten anzeigen.
- Cashflow-Sicht mit tatsächlichem Zahlungsmonat anzeigen.
- Kategorien verwalten.
- Aktive und inaktive Budgetpositionen unterscheiden.

### 4.2 Nicht im MVP enthalten

Folgende Funktionen sind explizit nicht Teil des MVPs:

- Bankimport.
- Automatische Kategorisierung.
- OCR für Rechnungen.
- Mehrbenutzerfähigkeit.
- Familienfreigabe.
- Push-Benachrichtigungen.
- Native Android-App.
- Synchronisierung mit Bankkonten.
- KI-basierte Analyse.
- Komplexe Forecasts.
- Steuerliche Auswertung.

Diese Funktionen können später ergänzt werden.

---

## 5. Grundlegende Fachlogik

Das Tool arbeitet nicht nur mit einzelnen Buchungen, sondern mit Budgetregeln.

Eine Budgetregel beschreibt eine Einnahme oder Ausgabe, die in einem bestimmten Rhythmus auftritt.

Beispiele:

| Name | Typ | Frequenz | Betrag |
|---|---|---|---:|
| Gehalt | Einnahme | Monatlich | 3500,00 |
| Miete | Ausgabe | Monatlich | 1200,00 |
| Strom | Ausgabe | Monatlich | 145,00 |
| Kfz-Versicherung | Ausgabe | Jährlich | 720,00 |
| Amazon Prime | Ausgabe | Jährlich | 89,90 |
| Urlaub | Ausgabe | Einmalig | 1500,00 |

Die Anwendung muss aus diesen Regeln periodische Übersichten erzeugen.

---

## 6. Zentrale Fachanforderung: Versionierung von Kostenpositionen

Eine der wichtigsten Anforderungen ist die **zeitliche Versionierung** von Budgetpositionen.

Wenn eine bestehende Kostenposition geändert wird, darf die Änderung nicht rückwirkend auf alte Monate wirken.

### 6.1 Beispiel

Eine monatliche Stromzahlung beträgt ab Januar 2026 zunächst 120,00 EUR.

Ab März 2026 steigt der Betrag auf 145,00 EUR.

Die Anwendung muss dies so abbilden:

```text
Strom
01.01.2026 - 28.02.2026: 120,00 EUR monatlich
ab 01.03.2026:             145,00 EUR monatlich
```

Die Monate Januar und Februar 2026 müssen weiterhin mit 120,00 EUR berechnet werden.

Ab März 2026 muss mit 145,00 EUR gerechnet werden.

### 6.2 Akzeptanzkriterien

- Wenn ein Betrag ab einem bestimmten Monat geändert wird, wird eine neue Version erstellt.
- Die alte Version erhält ein `ValidTo`-Datum.
- Die neue Version erhält ein `ValidFrom`-Datum.
- Historische Berechnungen bleiben stabil.
- Bei Monatsauswertungen wird die zum jeweiligen Monat gültige Version verwendet.
- Es darf keine Überschneidungen zwischen Versionen derselben Budgetposition geben.
- Es darf keine Lücke geben, sofern eine Kostenposition durchgehend aktiv sein soll.

---

## 7. Begriffe

### 7.1 BudgetItem

Ein BudgetItem ist die fachliche Hauptposition.

Beispiele:

- Miete
- Strom
- Netflix
- Gehalt
- Kfz-Versicherung

Das BudgetItem enthält Informationen, die langfristig zur Position gehören.

### 7.2 BudgetItemVersion

Eine BudgetItemVersion beschreibt den Betrag, die Frequenz und den Gültigkeitszeitraum eines BudgetItems.

Ein BudgetItem kann mehrere Versionen haben.

### 7.3 ActualTransaction

Eine ActualTransaction beschreibt eine echte Buchung oder tatsächliche Ausgabe.

Diese ist für spätere Plan/Ist-Vergleiche vorgesehen.

Für den MVP kann die Tabelle bereits vorbereitet werden, die UI dafür ist aber optional.

### 7.4 Budget-Sicht

In der Budget-Sicht werden jährliche oder quartalsweise Kosten anteilig auf Monate verteilt.

Beispiel:

```text
Kfz-Versicherung: 720,00 EUR jährlich
Budget-Sicht:     60,00 EUR pro Monat
```

### 7.5 Cashflow-Sicht

In der Cashflow-Sicht wird eine Ausgabe in dem Monat angezeigt, in dem sie tatsächlich fällig ist.

Beispiel:

```text
Kfz-Versicherung: 720,00 EUR jährlich, Zahlung im März
Cashflow-Sicht März: 720,00 EUR
Andere Monate:       0,00 EUR
```

---

## 8. Funktionale Anforderungen

## 8.1 Budgetpositionen verwalten

Die Anwendung muss Budgetpositionen anlegen, anzeigen, bearbeiten und deaktivieren können.

### Anforderungen

- Neue Budgetposition anlegen.
- Bestehende Budgetposition anzeigen.
- Budgetposition deaktivieren.
- Budgetposition reaktivieren.
- Budgetposition einer Kategorie zuordnen.
- Budgetposition als Einnahme oder Ausgabe markieren.
- Budgetposition mit Beschreibung versehen.

### Felder

```text
Name
Beschreibung
Typ: Einnahme / Ausgabe
Kategorie
Aktiv/Inaktiv
```

### Akzeptanzkriterien

- Eine Budgetposition muss mindestens einen Namen haben.
- Eine Budgetposition muss einen Typ haben.
- Eine Budgetposition muss einer Kategorie zugeordnet werden können.
- Deaktivierte Budgetpositionen erscheinen nicht mehr in zukünftigen Planungen, historische Daten bleiben aber erhalten.

---

## 8.2 Versionen von Budgetpositionen verwalten

Jede Budgetposition muss mindestens eine Version haben.

### Anforderungen

Eine Version enthält:

```text
Betrag
Frequenz
Gültig ab
Gültig bis optional
Zahlungstag optional
Monat der Zahlung optional
Notiz optional
```

### Unterstützte Frequenzen

```text
Monthly
Quarterly
Yearly
Once
```

### Akzeptanzkriterien

- Beim Anlegen einer neuen Budgetposition wird automatisch eine erste Version erstellt.
- Wird eine bestehende Position ab einem Datum geändert, wird eine neue Version erstellt.
- Die alte Version wird zeitlich beendet.
- Versionen dürfen sich nicht überschneiden.
- Einmalige Kosten dürfen nur einmal in der Planung erscheinen.
- Monatliche Kosten erscheinen in jedem gültigen Monat.
- Quartalsweise Kosten erscheinen je nach Cashflow-/Budget-Sicht korrekt.
- Jährliche Kosten erscheinen je nach Cashflow-/Budget-Sicht korrekt.

---

## 8.3 Kosten ab Zeitpunkt ändern

Die Anwendung muss es ermöglichen, eine laufende Position ab einem bestimmten Monat anzupassen.

### Beispiel

```text
Miete aktuell: 1200,00 EUR monatlich
Neue Miete ab 01.07.2026: 1270,00 EUR monatlich
```

### Erwartetes Verhalten

- Bis einschließlich Juni 2026 wird 1200,00 EUR verwendet.
- Ab Juli 2026 wird 1270,00 EUR verwendet.
- Alte Auswertungen vor Juli 2026 ändern sich nicht.

### UI-Anforderung

Beim Bearbeiten einer bestehenden Position soll der Benutzer auswählen können:

```text
Änderung rückwirkend für aktuelle Version übernehmen
oder
Neue Version ab Datum erstellen
```

Für den MVP soll standardmäßig die sichere Variante verwendet werden:

```text
Neue Version ab Datum erstellen
```

Rückwirkende Änderungen können später ergänzt werden.

---

## 8.4 Kategorien verwalten

Die Anwendung muss Kategorien unterstützen.

Beispiele:

- Wohnen
- Energie
- Versicherungen
- Mobilität
- Lebensmittel
- Abos
- Freizeit
- Kinder
- Gesundheit
- Sonstiges
- Einkommen

### Anforderungen

- Kategorien anlegen.
- Kategorien umbenennen.
- Kategorien deaktivieren.
- Budgetpositionen Kategorien zuordnen.
- Auswertungen nach Kategorie gruppieren.

### Akzeptanzkriterien

- Jede Budgetposition kann genau einer Kategorie zugeordnet werden.
- Eine Kategorie kann mehrere Budgetpositionen enthalten.
- Deaktivierte Kategorien bleiben für historische Auswertungen sichtbar.

---

## 8.5 Monatsübersicht

Die Anwendung muss eine Monatsübersicht anzeigen.

### Inhalte

Für einen ausgewählten Monat sollen angezeigt werden:

```text
Gesamteinnahmen
Gesamtausgaben
Fixkosten
Einmalige Kosten
Saldo
Restbudget
Kosten nach Kategorie
Liste der einzelnen Budgetpositionen
```

### Beispiel

```text
Monat: März 2026

Einnahmen:
+ Gehalt: 3.500,00 EUR

Ausgaben:
- Miete: 1.200,00 EUR
- Strom: 145,00 EUR
- Netflix: 15,99 EUR
- Kfz-Versicherung anteilig: 60,00 EUR

Saldo:
3.500,00 - 1.420,99 = 2.079,01 EUR
```

### Akzeptanzkriterien

- Der Benutzer kann den Monat auswählen.
- Die Anwendung berechnet alle gültigen Budgetpositionen für diesen Monat.
- Es wird die korrekte Version je Budgetposition verwendet.
- Einnahmen und Ausgaben werden getrennt dargestellt.
- Kategorien werden zusammengefasst.

---

## 8.6 Jahresübersicht

Die Anwendung muss eine Jahresübersicht anzeigen.

### Inhalte

Für ein ausgewähltes Jahr sollen angezeigt werden:

```text
Monatliche Einnahmen
Monatliche Ausgaben
Monatlicher Saldo
Jahressumme Einnahmen
Jahressumme Ausgaben
Jahressaldo
Kategorieauswertung
```

### Akzeptanzkriterien

- Der Benutzer kann ein Jahr auswählen.
- Alle Monate Januar bis Dezember werden dargestellt.
- Pro Monat wird Einnahme, Ausgabe und Saldo berechnet.
- Die Jahresgesamtwerte werden berechnet.
- Budget-Sicht und Cashflow-Sicht können verglichen werden.

---

## 8.7 Budget-Sicht und Cashflow-Sicht

Die Anwendung muss zwei Berechnungsarten unterstützen.

### 8.7.1 Budget-Sicht

In der Budget-Sicht werden größere regelmäßige Zahlungen auf Monate verteilt.

Beispiele:

```text
Jährlich 1200,00 EUR => 100,00 EUR pro Monat
Quartalsweise 300,00 EUR => 100,00 EUR pro Monat
Monatlich 50,00 EUR => 50,00 EUR pro Monat
Einmalig 500,00 EUR => 500,00 EUR im Ereignismonat
```

### 8.7.2 Cashflow-Sicht

In der Cashflow-Sicht wird die Zahlung im tatsächlichen Zahlungsmonat angezeigt.

Beispiele:

```text
Jährlich 1200,00 EUR im März => März: 1200,00 EUR
Quartalsweise 300,00 EUR ab Januar => Januar, April, Juli, Oktober: 300,00 EUR
Monatlich 50,00 EUR => jeden Monat 50,00 EUR
Einmalig 500,00 EUR => im Ereignismonat 500,00 EUR
```

### Akzeptanzkriterien

- Der Benutzer kann zwischen Budget-Sicht und Cashflow-Sicht wechseln.
- Beide Sichten verwenden dieselben Budgetpositionen und Versionen.
- Die Ergebnisse können unterschiedlich sein.
- Die Berechnung muss testbar und deterministisch sein.

---

## 9. Datenmodell

## 9.1 Entity: BudgetItem

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
```

## 9.2 Entity: BudgetItemVersion

```csharp
public class BudgetItemVersion
{
    public Guid Id { get; set; }
    public Guid BudgetItemId { get; set; }
    public BudgetItem BudgetItem { get; set; } = null!;

    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; }

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public int? PaymentDay { get; set; }
    public int? PaymentMonth { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 9.3 Entity: Category

```csharp
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BudgetItem> BudgetItems { get; set; } = new();
}
```

## 9.4 Entity: ActualTransaction

```csharp
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

## 9.5 Enums

```csharp
public enum BudgetItemType
{
    Income = 1,
    Expense = 2
}

public enum BudgetFrequency
{
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
    Once = 4
}

public enum BudgetViewMode
{
    Budget = 1,
    Cashflow = 2
}
```

---

## 10. Berechnungslogik

Die Berechnungslogik soll in einer eigenen Domain-/Application-Komponente liegen und nicht direkt in der UI implementiert werden.

Vorgeschlagener Service:

```csharp
public interface IBudgetProjectionService
{
    MonthlyBudgetProjection GetMonthlyProjection(int year, int month, BudgetViewMode viewMode);
    YearlyBudgetProjection GetYearlyProjection(int year, BudgetViewMode viewMode);
}
```

## 10.1 MonthlyBudgetProjection

```csharp
public class MonthlyBudgetProjection
{
    public int Year { get; set; }
    public int Month { get; set; }
    public BudgetViewMode ViewMode { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }

    public List<BudgetProjectionLine> Lines { get; set; } = new();
    public List<CategoryProjectionSummary> Categories { get; set; } = new();
}
```

## 10.2 BudgetProjectionLine

```csharp
public class BudgetProjectionLine
{
    public Guid BudgetItemId { get; set; }
    public string BudgetItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public BudgetItemType Type { get; set; }
    public BudgetFrequency Frequency { get; set; }
    public decimal Amount { get; set; }
    public decimal ProjectedMonthlyAmount { get; set; }
    public string? Note { get; set; }
}
```

## 10.3 YearlyBudgetProjection

```csharp
public class YearlyBudgetProjection
{
    public int Year { get; set; }
    public BudgetViewMode ViewMode { get; set; }
    public List<MonthlyBudgetProjection> Months { get; set; } = new();

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}
```

---

## 11. Regeln für die Monatsberechnung

Für einen Monat `M` muss die Anwendung pro BudgetItem die gültige Version ermitteln.

Eine Version ist gültig, wenn:

```text
ValidFrom <= Ende des Monats
und
ValidTo ist null oder ValidTo >= Anfang des Monats
```

Falls mehrere Versionen gültig wären, ist das ein Datenfehler.

## 11.1 Monthly

### Budget-Sicht

```text
ProjectedMonthlyAmount = Amount
```

### Cashflow-Sicht

```text
ProjectedMonthlyAmount = Amount
```

## 11.2 Quarterly

### Budget-Sicht

```text
ProjectedMonthlyAmount = Amount / 3
```

### Cashflow-Sicht

Die Zahlung erscheint nur in den fälligen Monaten.

Beispiel bei Start im Januar:

```text
Januar, April, Juli, Oktober
```

Regel:

```text
Monat ist zahlungsrelevant, wenn die Anzahl Monate seit ValidFrom durch 3 teilbar ist.
```

## 11.3 Yearly

### Budget-Sicht

```text
ProjectedMonthlyAmount = Amount / 12
```

### Cashflow-Sicht

Die Zahlung erscheint nur im Zahlungsmonat.

Der Zahlungsmonat wird über `PaymentMonth` bestimmt.

Wenn `PaymentMonth` leer ist, wird der Monat aus `ValidFrom` verwendet.

## 11.4 Once

Einmalige Kosten erscheinen nur im Monat von `ValidFrom`.

### Budget-Sicht

```text
ProjectedMonthlyAmount = Amount im Ereignismonat, sonst 0
```

### Cashflow-Sicht

```text
ProjectedMonthlyAmount = Amount im Ereignismonat, sonst 0
```

---

## 12. Validierungsregeln

## 12.1 BudgetItem

- Name darf nicht leer sein.
- Typ muss gesetzt sein.
- Kategorie muss gesetzt sein.

## 12.2 BudgetItemVersion

- Betrag muss größer oder gleich 0 sein.
- Frequenz muss gesetzt sein.
- ValidFrom muss gesetzt sein.
- ValidTo darf nicht vor ValidFrom liegen.
- PaymentDay darf nur zwischen 1 und 31 liegen.
- PaymentMonth darf nur zwischen 1 und 12 liegen.
- Versionen desselben BudgetItems dürfen sich nicht überschneiden.

## 12.3 Frequency-spezifisch

### Monthly

- PaymentMonth wird ignoriert.
- PaymentDay ist optional.

### Quarterly

- PaymentMonth ist optional.
- Wenn PaymentMonth gesetzt ist, kann daraus das Quartalsraster abgeleitet werden.

### Yearly

- PaymentMonth ist optional, aber empfohlen.
- Wenn PaymentMonth nicht gesetzt ist, wird der Monat aus ValidFrom genutzt.

### Once

- ValidFrom bestimmt den Ereignismonat.
- ValidTo sollte null sein.

---

## 13. UI-Anforderungen

## 13.1 Allgemein

Die UI muss responsive sein.

Desktop:

- Tabellenansichten.
- Filter.
- Seitennavigation.

Mobile:

- Kartenansicht statt breiter Tabellen.
- Große Buttons.
- Schnelle Eingabe.
- Gute Bedienbarkeit mit einer Hand.

## 13.2 Navigation

Vorgeschlagene Hauptnavigation:

```text
Dashboard
Monatsübersicht
Jahresübersicht
Budgetpositionen
Kategorien
Einstellungen
```

## 13.3 Dashboard

Das Dashboard zeigt:

```text
Aktueller Monat
Einnahmen
Ausgaben
Saldo
Größte Ausgabenkategorien
Nächste große Zahlungen
```

## 13.4 Budgetpositionen-Seite

Funktionen:

- Liste aller Budgetpositionen.
- Filter nach Kategorie.
- Filter nach Einnahme/Ausgabe.
- Filter nach aktiv/inaktiv.
- Neue Position anlegen.
- Position bearbeiten.
- Neue Version ab Datum erstellen.
- Historie der Versionen anzeigen.

## 13.5 Position bearbeiten

Beim Bearbeiten einer Position sollen folgende Daten gepflegt werden können:

```text
Name
Beschreibung
Kategorie
Typ
Aktiv/Inaktiv
```

Beim Bearbeiten der finanziellen Daten:

```text
Betrag
Frequenz
Gültig ab
Zahlungstag
Zahlungsmonat
Notiz
```

Standardverhalten:

```text
Änderung erzeugt neue Version ab angegebenem Datum.
```

## 13.6 Monatsübersicht

Die Monatsübersicht muss enthalten:

- Monatsauswahl.
- Umschalter Budget-Sicht / Cashflow-Sicht.
- Einnahmen gesamt.
- Ausgaben gesamt.
- Saldo.
- Liste der Budgetzeilen.
- Gruppierung nach Kategorien.

## 13.7 Jahresübersicht

Die Jahresübersicht muss enthalten:

- Jahresauswahl.
- Umschalter Budget-Sicht / Cashflow-Sicht.
- Tabelle Januar bis Dezember.
- Einnahmen je Monat.
- Ausgaben je Monat.
- Saldo je Monat.
- Jahresgesamtwerte.

---

## 14. Projektstruktur

Die Solution soll sauber nach Schichten aufgebaut werden.

Vorschlag:

```text
BudgetPilot/
 ├─ src/
 │   ├─ BudgetPilot.Web/
 │   ├─ BudgetPilot.Application/
 │   ├─ BudgetPilot.Domain/
 │   ├─ BudgetPilot.Infrastructure/
 │   └─ BudgetPilot.Shared/
 ├─ tests/
 │   ├─ BudgetPilot.Domain.Tests/
 │   └─ BudgetPilot.Application.Tests/
 ├─ docker-compose.yml
 ├─ README.md
 └─ docs/
     └─ requirements.md
```

## 14.1 BudgetPilot.Domain

Enthält:

- Entities.
- Enums.
- Domain-Regeln.
- Validierungslogik, soweit domänennah.

## 14.2 BudgetPilot.Application

Enthält:

- Services.
- Use Cases.
- DTOs.
- Projection-Logik.
- Interfaces für Repositories.

## 14.3 BudgetPilot.Infrastructure

Enthält:

- EF Core DbContext.
- Repository-Implementierungen.
- Datenbankmigrationen.
- SQLite/PostgreSQL-Anbindung.

## 14.4 BudgetPilot.Web

Enthält:

- Blazor-Komponenten.
- Seiten.
- Layout.
- UI-Services.

## 14.5 BudgetPilot.Tests

Enthält:

- Unit Tests für Berechnungslogik.
- Unit Tests für Versionierung.
- Unit Tests für Validierung.

---

## 15. Datenbank

## 15.1 MVP

Im MVP soll SQLite verwendet werden.

Vorteile:

- Einfacher Start.
- Keine zusätzliche Datenbankinstallation notwendig.
- Leicht in Docker nutzbar.

## 15.2 Später

Später soll PostgreSQL unterstützt werden.

Daher soll die Infrastruktur so aufgebaut werden, dass der Datenbankanbieter konfigurierbar ist.

Beispiel:

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=budgetpilot.db"
  }
}
```

Später:

```json
{
  "Database": {
    "Provider": "Postgres",
    "ConnectionString": "Host=db;Database=budgetpilot;Username=budgetpilot;Password=..."
  }
}
```

---

## 16. API / Services

Auch wenn die erste Version als Blazor Web App gebaut wird, soll die Application-Logik so strukturiert sein, dass später eine API oder native App darauf zugreifen kann.

Vorgeschlagene Services:

```csharp
public interface IBudgetItemService
{
    Task<BudgetItemDto> CreateAsync(CreateBudgetItemRequest request);
    Task<BudgetItemDto> UpdateMetadataAsync(Guid id, UpdateBudgetItemMetadataRequest request);
    Task<BudgetItemVersionDto> AddVersionAsync(Guid budgetItemId, CreateBudgetItemVersionRequest request);
    Task<IReadOnlyList<BudgetItemDto>> GetAllAsync();
    Task<BudgetItemDto?> GetByIdAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task ReactivateAsync(Guid id);
}
```

```csharp
public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task RenameAsync(Guid id, string newName);
    Task DeactivateAsync(Guid id);
}
```

```csharp
public interface IBudgetProjectionService
{
    Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(int year, int month, BudgetViewMode viewMode);
    Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(int year, BudgetViewMode viewMode);
}
```

---

## 17. Testanforderungen

Die Berechnungslogik muss mit Unit Tests abgesichert werden.

## 17.1 Pflichttests

### Monatliche Kosten

```text
Gegeben: Miete 1200 EUR monatlich ab Januar 2026
Wenn: Monatsübersicht März 2026 berechnet wird
Dann: Miete erscheint mit 1200 EUR
```

### Jährliche Kosten in Budget-Sicht

```text
Gegeben: Kfz-Versicherung 720 EUR jährlich ab Januar 2026
Wenn: Budget-Sicht März 2026 berechnet wird
Dann: Es werden 60 EUR angezeigt
```

### Jährliche Kosten in Cashflow-Sicht

```text
Gegeben: Kfz-Versicherung 720 EUR jährlich, PaymentMonth März
Wenn: Cashflow-Sicht März 2026 berechnet wird
Dann: Es werden 720 EUR angezeigt

Wenn: Cashflow-Sicht April 2026 berechnet wird
Dann: Es werden 0 EUR angezeigt
```

### Quartalsweise Kosten in Budget-Sicht

```text
Gegeben: Versicherung 300 EUR quartalsweise
Wenn: Budget-Sicht Februar 2026 berechnet wird
Dann: Es werden 100 EUR angezeigt
```

### Quartalsweise Kosten in Cashflow-Sicht

```text
Gegeben: Versicherung 300 EUR quartalsweise ab Januar 2026
Wenn: Cashflow-Sicht Januar 2026 berechnet wird
Dann: Es werden 300 EUR angezeigt

Wenn: Cashflow-Sicht Februar 2026 berechnet wird
Dann: Es werden 0 EUR angezeigt

Wenn: Cashflow-Sicht April 2026 berechnet wird
Dann: Es werden 300 EUR angezeigt
```

### Versionierung

```text
Gegeben: Strom 120 EUR monatlich ab Januar 2026
Und: Strom wird ab März 2026 auf 145 EUR geändert
Wenn: Februar 2026 berechnet wird
Dann: Strom = 120 EUR

Wenn: März 2026 berechnet wird
Dann: Strom = 145 EUR
```

### Einmalige Kosten

```text
Gegeben: Waschmaschine 600 EUR einmalig am 15.05.2026
Wenn: Mai 2026 berechnet wird
Dann: Waschmaschine = 600 EUR

Wenn: Juni 2026 berechnet wird
Dann: Waschmaschine = 0 EUR
```

### Keine überschneidenden Versionen

```text
Gegeben: Eine Budgetposition hat eine Version ab Januar bis März
Wenn: Eine neue Version ab Februar angelegt wird
Dann: Die bestehende Version muss korrekt beendet werden oder die Operation muss abgelehnt werden
```

---

## 18. Qualitätsanforderungen

## 18.1 Codequalität

- Klare Schichtenarchitektur.
- Keine Berechnungslogik in UI-Komponenten.
- Keine direkten DbContext-Zugriffe aus UI-Komponenten.
- Domain-Logik testbar halten.
- Services über Interfaces abstrahieren.
- DTOs für UI/API-Kommunikation verwenden.
- Decimal statt double für Geldbeträge verwenden.

## 18.2 Fehlerbehandlung

- Validierungsfehler müssen verständlich angezeigt werden.
- Datenfehler wie überschneidende Versionen müssen erkannt werden.
- Ungültige Frequenzen oder Beträge dürfen nicht gespeichert werden.

## 18.3 Lokalisierung

MVP-Sprache:

```text
Deutsch
```

Formatierung:

```text
Währung: EUR
Datumsformat: dd.MM.yyyy
Zahlenformat: de-DE
```

## 18.4 Performance

Da es sich um ein Haushaltsbudget handelt, sind die Datenmengen klein.

Trotzdem soll die Berechnung sauber implementiert werden:

- Jahresübersicht darf nicht spürbar langsam sein.
- Projektionen sollen nur relevante aktive und historisch relevante Versionen laden.
- Keine unnötigen Datenbankabfragen pro Tabellenzeile.

---

## 19. Docker / Deployment

Für den MVP soll ein Dockerfile vorbereitet werden.

Optional zusätzlich:

```yaml
services:
  budgetpilot:
    image: budgetpilot:latest
    container_name: budgetpilot
    ports:
      - "8080:8080"
    volumes:
      - ./data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__Provider=Sqlite
      - Database__ConnectionString=Data Source=/app/data/budgetpilot.db
```

Später kann PostgreSQL ergänzt werden:

```yaml
services:
  db:
    image: postgres:latest
    environment:
      POSTGRES_DB: budgetpilot
      POSTGRES_USER: budgetpilot
      POSTGRES_PASSWORD: budgetpilot
    volumes:
      - ./postgres:/var/lib/postgresql/data

  budgetpilot:
    image: budgetpilot:latest
    ports:
      - "8080:8080"
    depends_on:
      - db
```

---

## 20. PWA-Anforderungen

Die Anwendung soll später als PWA nutzbar sein.

Für den MVP muss die UI bereits mobil optimiert sein.

PWA-Funktionen können in einer späteren Iteration ergänzt werden:

- Manifest.
- App-Icon.
- Installierbarkeit auf Android.
- Offline-Fallback.
- Optional Offline-Erfassung.

Nicht zwingend im ersten MVP:

- Vollständiger Offline-Modus.
- Hintergrundsynchronisierung.
- Push-Benachrichtigungen.

---

## 21. Mögliche spätere Erweiterungen

## 21.1 Plan/Ist-Vergleich

Später sollen echte Buchungen erfasst werden können.

Dann kann verglichen werden:

```text
Geplante Ausgaben
Tatsächliche Ausgaben
Differenz
```

## 21.2 CSV-Import

Bankdaten könnten später aus CSV-Dateien importiert werden.

Mögliche Funktionen:

- CSV-Datei hochladen.
- Spalten zuordnen.
- Buchungen importieren.
- Doppelte Buchungen erkennen.

## 21.3 Regeln für automatische Kategorien

Beispiel:

```text
Wenn Beschreibung enthält "ALDI", dann Kategorie "Lebensmittel"
Wenn Beschreibung enthält "NETFLIX", dann Kategorie "Abos"
```

## 21.4 Wiederkehrende Einnahmen

Neben Ausgaben müssen auch Einnahmen vollständig unterstützt werden.

Beispiele:

- Gehalt
- Kindergeld
- Bonus
- Rückerstattungen

## 21.5 Szenarien

Später könnten Szenarien unterstützt werden:

```text
Aktuelles Budget
Budget nach Gehaltserhöhung
Budget nach Umzug
Budget mit neuem Auto
```

---

## 22. Initiale Codex-Aufgabe

Codex soll zuerst das Grundprojekt erzeugen.

### Aufgabe 1: Solution-Struktur erstellen

Erstelle eine .NET Solution mit folgender Struktur:

```text
BudgetPilot/
 ├─ src/
 │   ├─ BudgetPilot.Web/
 │   ├─ BudgetPilot.Application/
 │   ├─ BudgetPilot.Domain/
 │   ├─ BudgetPilot.Infrastructure/
 │   └─ BudgetPilot.Shared/
 └─ tests/
     ├─ BudgetPilot.Domain.Tests/
     └─ BudgetPilot.Application.Tests/
```

### Aufgabe 2: Domain-Modell implementieren

Implementiere:

```text
BudgetItem
BudgetItemVersion
Category
ActualTransaction
BudgetItemType
BudgetFrequency
BudgetViewMode
```

### Aufgabe 3: EF Core DbContext implementieren

Implementiere:

```text
BudgetPilotDbContext
DbSet<BudgetItem>
DbSet<BudgetItemVersion>
DbSet<Category>
DbSet<ActualTransaction>
```

Konfiguriere:

- Required-Felder.
- Decimal Precision.
- Beziehungen.
- Indizes.
- SQLite als MVP-Datenbank.

### Aufgabe 4: Projection-Service implementieren

Implementiere:

```text
IBudgetProjectionService
BudgetProjectionService
MonthlyBudgetProjection
YearlyBudgetProjection
BudgetProjectionLine
CategoryProjectionSummary
```

Der Service muss Budget-Sicht und Cashflow-Sicht unterstützen.

### Aufgabe 5: Unit Tests implementieren

Implementiere Unit Tests für:

- Monthly.
- Quarterly Budget-Sicht.
- Quarterly Cashflow-Sicht.
- Yearly Budget-Sicht.
- Yearly Cashflow-Sicht.
- Once.
- Versionierung ab Datum.
- Keine überschneidenden Versionen.

### Aufgabe 6: Erste Blazor-Seiten implementieren

Implementiere einfache Seiten:

```text
Dashboard
Budgetpositionen
Budgetposition anlegen
Monatsübersicht
Jahresübersicht
Kategorien
```

UI muss noch nicht perfekt sein, aber responsive Grundlagen sollen berücksichtigt werden.

---

## 23. Akzeptanzkriterien für den MVP

Der MVP gilt als erfüllt, wenn folgende Punkte funktionieren:

- Eine Kategorie kann angelegt werden.
- Eine monatliche Ausgabe kann angelegt werden.
- Eine jährliche Ausgabe kann angelegt werden.
- Eine quartalsweise Ausgabe kann angelegt werden.
- Eine einmalige Ausgabe kann angelegt werden.
- Eine Einnahme kann angelegt werden.
- Eine Ausgabe kann ab einem Monat geändert werden.
- Die Änderung wirkt nur ab diesem Monat.
- Alte Monate bleiben unverändert.
- Monatsübersicht zeigt korrekte Werte.
- Jahresübersicht zeigt korrekte Werte.
- Budget-Sicht verteilt jährliche und quartalsweise Kosten anteilig.
- Cashflow-Sicht zeigt jährliche und quartalsweise Kosten im Zahlungsmonat.
- Unit Tests für die zentrale Berechnungslogik sind vorhanden und grün.
- Die Anwendung kann lokal gestartet werden.
- Die Anwendung kann in Docker gestartet werden.
- Die Oberfläche ist auf Smartphone-Breite grundsätzlich nutzbar.

---

## 24. Wichtige technische Leitlinien für Codex

Codex soll folgende Leitlinien beachten:

```text
- Verwende C# und .NET.
- Verwende decimal für Geldbeträge.
- Verwende DateOnly für fachliche Datumswerte.
- Halte Berechnungslogik aus der UI heraus.
- Schreibe Unit Tests für jede wichtige Budgetregel.
- Implementiere Versionierung sauber und testbar.
- Baue zuerst einfache Funktionalität, keine überladene UI.
- Verwende klare Namen und kleine Services.
- Erzeuge keine native Android-App im MVP.
- Baue die Web-App responsive.
- Bereite PWA vor, aber implementiere Offline-Funktionen erst später.
```

---

## 25. Beispiel-Daten für Tests und Demo

Für Demo und Tests können folgende Daten verwendet werden:

```text
Kategorie Einkommen
- Gehalt: 3500 EUR monatlich ab 01.01.2026

Kategorie Wohnen
- Miete: 1200 EUR monatlich ab 01.01.2026

Kategorie Energie
- Strom: 120 EUR monatlich ab 01.01.2026
- Strom Änderung: 145 EUR monatlich ab 01.03.2026

Kategorie Abos
- Netflix: 15,99 EUR monatlich ab 01.01.2026
- Amazon Prime: 89,90 EUR jährlich ab 01.02.2026

Kategorie Versicherungen
- Kfz-Versicherung: 720 EUR jährlich, Zahlung im März
- Haftpflicht: 90 EUR jährlich, Zahlung im Januar

Kategorie Haushalt
- Waschmaschine: 600 EUR einmalig am 15.05.2026
```

Erwartung:

- Januar 2026 Strom: 120 EUR.
- Februar 2026 Strom: 120 EUR.
- März 2026 Strom: 145 EUR.
- Kfz-Versicherung Budget-Sicht: 60 EUR pro Monat.
- Kfz-Versicherung Cashflow-Sicht: 720 EUR im März.
- Waschmaschine nur im Mai 2026.

---

## 26. Offene Entscheidungen

Folgende Punkte müssen später final entschieden werden:

- Soll es mehrere Haushalte/Budgets geben?
- Soll ein Benutzer mehrere Szenarien pflegen können?
- Soll Authentifizierung direkt im MVP enthalten sein?
- Soll SQLite dauerhaft reichen oder früh auf PostgreSQL gewechselt werden?
- Soll es eine Importfunktion für CSV geben?
- Soll es echte Ist-Buchungen im MVP geben oder erst danach?
- Soll es eine Exportfunktion nach Excel/CSV geben?

Für den ersten MVP gelten diese Entscheidungen:

```text
Ein Benutzer
Ein Budget
Keine Authentifizierung erforderlich, wenn lokal betrieben
SQLite
Keine CSV-Importe
Keine Ist-Buchungen notwendig
Keine native Android-App
```

---

## 27. Zusammenfassung

BudgetPilot soll ein kleines, sauberes Haushaltsbudget-Tool werden.

Der wichtigste Kern ist die korrekte Behandlung von:

- wiederkehrenden Kosten,
- einmaligen Kosten,
- jährlichen Kosten,
- quartalsweisen Kosten,
- Versionierung ab Datum,
- Budget-Sicht,
- Cashflow-Sicht.

Die technische Umsetzung soll zuerst als .NET/Blazor-Web-App erfolgen.

Die Anwendung soll mobil nutzbar sein und später als PWA erweitert werden können.

Die erste Implementierung soll bewusst klein bleiben, aber die Architektur muss so aufgebaut werden, dass spätere Funktionen wie Bankimport, Plan/Ist-Vergleich, Kategorienregeln und Android-App möglich sind.
