# BudgetPilot Android-App — Anforderungen & Vorgehen

Status: `androidNet/` ist der einzige fortgeführte Android-Client · Quelle der Wahrheit für die
Android-Begleit-App, die mit einer selbst-gehosteten BudgetPilot-Instanz (.NET 8 /
Blazor + ASP.NET Core Identity) kommunizieren. Code/Bezeichner Englisch,
UI-Text & Specs Deutsch, Geld `decimal`, Geschäftsdaten als Datum (`dd.MM.yyyy`,
Locale `de-DE`).

---

## 1. Ziel & Umfang

Die anfänglich getrennten Ansätze wurden verglichen. Die Produktentwicklung wird
ausschließlich mit .NET MAUI fortgeführt:

- `androidNet/` — aktiver .NET MAUI Blazor Hybrid Client.
- `android/` — eingefrorener Kotlin/Jetpack-Compose-Prototyp; keine neuen Features,
  API-Paritätsänderungen, Builds oder Releases mehr.

Die aktive App ist ein **Client**. Fachliche Logik wie Projektion, Versionierung,
Validierung und Audit bleibt serverseitig in Application/Domain bzw. hinter der
bestehenden JSON-API. Die mobile App ruft diese API auf und hält nur
Client-Zustand wie Instanz-URL und Token lokal.

**Nicht-Ziele für Phase 1:** keine eigene lokale Budgetlogik, kein Offline-Sync,
kein eigener Server, keine Zahlungsabwicklung/echtes Geld.

---

## 2. Technologieentscheidungen

### Historischer Kotlin-Prototyp (`android/`)

- Bleibt als Referenz im Repository, wird aber nicht weiterentwickelt.
- Neue Anforderungen und API-Verträge gelten nur für `androidNet/`.

### Aktiver .NET-Client (`androidNet/`)

Festgelegt ist:

```text
.NET MAUI Blazor Hybrid
```

Konkrete Ableitung:

- Projektordner: `androidNet/`.
- Projektdatei: `androidNet/BudgetPilot.Mobile.csproj`.
- Target: `net10.0-android36.1`, min-SDK 26.
- UI: Blazor Hybrid im `BlazorWebView`.
- Wiederverwendung: ProjectReference auf `BudgetPilot.Application` für DTOs und
  transitiv `BudgetPilot.Domain` für Enums.
- Keine Aufnahme in `BudgetPilot.sln`, damit `dotnet build` der Web-/Backend-
  Lösung keine MAUI-/Android-Workload benötigt.

---

## 3. Backend-Voraussetzungen

Der benötigte Backend-Stand ist vorhanden:

- `/api/auth/login` und `/api/auth/refresh` liefern ASP.NET-Identity-Bearer-Token.
- `/api/v1/...` ist versioniert, per Bearer geschützt und nutzt die vorhandenen
  Application-Services/DTOs.
- Enums werden als Strings serialisiert.
- Fachliche `DomainException` wird als RFC-7807 `ProblemDetails` mit Status 400
  ausgegeben.
- Swagger/OpenAPI ist in Development unter `/swagger` verfügbar.

Die Web-UI nutzt weiter Cookies; `androidNet` nutzt Bearer-Token.

---

## 4. Anforderungen an `androidNet`

- **REQ-P1-1 Instanz-Setup.** Beim ersten Start gibt der Nutzer die Basis-URL ein.
  HTTPS ist Pflicht; HTTP ist nur für lokale Emulator-Tests gegen `10.0.2.2` oder
  `localhost` erlaubt.
- **REQ-P1-2 Login.** E-Mail/Passwort gegen `/api/auth/login`; keine Registrierung
  in der App.
- **REQ-P1-3 Token-Speicherung.** Access-/Refresh-Token sicher speichern; keine
  Tokens im Log.
- **REQ-P1-4 Refresh.** Bei `401` `/api/auth/refresh` versuchen und den Request
  wiederholen; bei Fehlschlag ausloggen.
- **REQ-P1-5 Dashboard.** Monatsprojektion laden und anzeigen: Einnahmen,
  Ausgaben, Saldo, Kategorie-Balken, Positionen.
- **REQ-P1-6 Ansichtsmodus.** Budget-/Cashflow-Modus für die Monatsprojektion
  umschaltbar.
- **REQ-P1-7 Lokalisierung.** Deutsche UI-Texte, EUR-Formatierung und `de-DE`.
- **REQ-P1-8 Theming.** An den Web-Prototyp angelehnt: Akzent `#C2410C`, ruhige
  BudgetPilot-Farbwelt.
- **REQ-P1-9 Haushaltszeit.** Aktueller Tag und Monat kommen aus
  `/api/v1/settings/household`, nicht aus der Gerätezeitzone. `androidNet` kann
  die gemeinsame IANA-Zeitzone ändern.
- **REQ-P1-9a Zahlungsverfolgung.** Ein haushaltsweiter Starttag begrenzt offene
  Zahlungsaufgaben unabhängig vom historischen Vertragsbeginn. `androidNet`
  kann den Schnitt ändern oder auf den ersten Tag des aktuellen Monats setzen;
  frühere Plandaten und bereits aufgelöste Zahlungen bleiben erhalten.
- **REQ-P1-10 Einfache Positionserfassung.** Die sechs Kernangaben sind direkt
  sichtbar, optionale Angaben bleiben eingeklappt. Vor dem Speichern zeigt die
  App die einmalige beziehungsweise die nächsten drei geplanten Zahlungen.

---

## 5. Sicherheit

- Produktiv nur HTTPS-Instanzen verwenden.
- Cleartext nur für lokale Emulator-Hosts freigeben.
- Refresh-Token nicht in Klartextdateien speichern.
- Keine sensiblen Android-Berechtigungen; Phase 1 braucht nur `INTERNET`.
- App-Lock/Biometrie ist eine spätere Härtung.

---

## 6. Build-Hinweise

.NET-MAUI-Ansatz:

```bash
cd androidNet
dotnet workload restore
dotnet build BudgetPilot.Mobile.csproj -f net10.0-android36.1
```

Der aktive Android-Client bleibt außerhalb von `BudgetPilot.sln` und wird nicht
vom normalen .NET/Docker-CI-Build erzwungen. Der Kotlin-Prototyp besitzt kein
Build- oder Paritäts-Gate mehr.

---

## 7. Phasenplan

1. **P0 — Backend-API.** Erledigt: Token-Endpoints, Bearer-geschützte `/api/v1`,
   ProblemDetails, OpenAPI.
2. **P1 — App-Grundgerüste vergleichen.** Abgeschlossen; `androidNet/` wurde als
   einziger fortgeführter Client ausgewählt. `android/` bleibt als eingefrorener Prototyp.
3. **P2 — Lesepfade erweitern.** Im `androidNet/`-Track erledigt:
   Monats-/Jahres-/Mehrjahresübersicht, Positionen, Kategorien und
   Aktivitätsprotokoll. Der Client zeigt außerdem die serverseitig
   berechneten anstehenden Zahlungen ab dem Haushaltstag. Fällige Positionen
   werden automatisch als erfolgt berücksichtigt und erzeugen keinen Rückstau.
   Der Dashboard-Lesepfad zeigt den berücksichtigten Haushaltsbuch-Stand aus den geschützten
   `/api/v1/comparisons/monthly`- und `/api/v1/comparisons/yearly`-Verträgen.
4. **P3 — Schreibpfad.** Im `androidNet/`-Track erledigt:
   Positionen/Versionen anlegen und korrigieren, Status ändern/löschen sowie
   Kategorien anlegen, umbenennen und deaktivieren. Web und `androidNet`
   können für anstehende Fälligkeiten Abweichungen bei Betrag/Datum erfassen
   sowie sie nach Rückfrage als entfallen markieren. Bearbeitete
   Fälligkeiten bleiben sichtbar, können korrigiert und sicher wieder geöffnet werden.
   Beide aktiven Oberflächen bieten außerdem eine prominente Schnellausgabe: Betrag und
   Kategorie genügen, Budgetposition und Notiz bleiben optional. Eine beim Öffnen
   erzeugte Request-ID wird bei Netzwerkfehlern wiederverwendet und verhindert
   Doppelbuchungen durch Doppeltipp oder erneutes Senden.
5. **P4 — Mobilreife.** Im `androidNet/`-Track erledigt: Offline-Lese-Cache,
   optionale lokale PIN-Sperre mit Android-Biometrie und PIN-Fallback,
   vollständiger Settings-Bereich, robuste Fehlerzustände, Accessibility-Basis
   und automatisierte Tests für lokale Eingaberegeln.
6. **P5 — Release.** Release-Skript für signiertes APK und AAB vorhanden.
   Extern erforderlich bleiben ein privater Keystore und gegebenenfalls der
   Play-Store-Zugang.
