# BudgetPilot Android-App — Anforderungen & Vorgehen

Status: Zwei parallele Phase-1-Ansätze · Quelle der Wahrheit für die
Android-Begleit-Apps, die mit einer selbst-gehosteten BudgetPilot-Instanz (.NET 8 /
Blazor + ASP.NET Core Identity) kommunizieren. Code/Bezeichner Englisch,
UI-Text & Specs Deutsch, Geld `decimal`, Geschäftsdaten als Datum (`dd.MM.yyyy`,
Locale `de-DE`).

---

## 1. Ziel & Umfang

Es entstehen zwei bewusst getrennte Android-Phase-1-Ansätze, damit sie parallel
bewertet werden können, ohne sich im selben Projektordner zu überschreiben:

- `android/` — Kotlin / Jetpack Compose Ansatz, aktuell von Claude Code betreut.
- `androidNet/` — .NET MAUI Blazor Hybrid Ansatz, aktuell von Codex betreut.

Beide Apps sind **Clients**. Fachliche Logik wie Projektion, Versionierung,
Validierung und Audit bleibt serverseitig in Application/Domain bzw. hinter der
bestehenden JSON-API. Die mobilen Apps rufen diese API auf und halten nur
Client-Zustand wie Instanz-URL und Token lokal.

**Nicht-Ziele für Phase 1:** keine eigene lokale Budgetlogik, kein Offline-Sync,
kein eigener Server, keine Zahlungsabwicklung/echtes Geld.

---

## 2. Technologieentscheidungen

### Kotlin-Ansatz (`android/`)

- Kotlin, Jetpack Compose, Material 3.
- Hilt, Retrofit/OkHttp, kotlinx.serialization.
- Eigenständiges Gradle-Projekt; nicht Teil von `BudgetPilot.sln`.
- In Android Studio den Ordner `android/` öffnen.

### .NET-Ansatz (`androidNet/`)

Für den .NET-Vergleich gilt:

```text
.NET MAUI Blazor Hybrid
```

Konkrete Ableitung:

- Projektordner: `androidNet/`.
- Projektdatei: `androidNet/BudgetPilot.Mobile.csproj`.
- Target: `net8.0-android`, min-SDK 26.
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

Die Web-UI nutzt weiter Cookies; Android-Clients nutzen Bearer-Token.

---

## 4. Gemeinsame Phase-1-Anforderungen

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

---

## 5. Sicherheit

- Produktiv nur HTTPS-Instanzen verwenden.
- Cleartext nur für lokale Emulator-Hosts freigeben.
- Refresh-Token nicht in Klartextdateien speichern.
- Keine sensiblen Android-Berechtigungen; Phase 1 braucht nur `INTERNET`.
- App-Lock/Biometrie ist eine spätere Härtung.

---

## 6. Build-Hinweise

Kotlin-Ansatz:

```bash
cd android
# In Android Studio öffnen oder per Gradle bauen, sobald Wrapper/SDK verfügbar sind.
```

.NET-MAUI-Ansatz:

```bash
cd androidNet
dotnet workload restore
dotnet build BudgetPilot.Mobile.csproj -f net8.0-android
```

Beide Android-Projekte bleiben außerhalb von `BudgetPilot.sln` und werden nicht
vom normalen .NET/Docker-CI-Build erzwungen.

---

## 7. Phasenplan

1. **P0 — Backend-API.** Erledigt: Token-Endpoints, Bearer-geschützte `/api/v1`,
   ProblemDetails, OpenAPI.
2. **P1 — App-Grundgerüste vergleichen.** Kotlin in `android/`, .NET MAUI in
   `androidNet/`: Setup, Login, Token-Refresh, Dashboard-Lesepfad.
3. **P2 — Lesepfade erweitern.** Im `androidNet/`-Track erledigt:
   Monats-/Jahres-/Mehrjahresübersicht, Positionen, Kategorien und
   Aktivitätsprotokoll.
4. **P3 — Schreibpfad.** Im `androidNet/`-Track erledigt:
   Positionen/Versionen anlegen und korrigieren, Status ändern/löschen sowie
   Kategorien anlegen, umbenennen und deaktivieren.
5. **P4 — Mobilreife.** Im `androidNet/`-Track erledigt: Offline-Lese-Cache,
   optionale lokale PIN-Sperre mit Android-Biometrie und PIN-Fallback,
   vollständiger Settings-Bereich, robuste Fehlerzustände, Accessibility-Basis
   und automatisierte Tests für lokale Eingaberegeln.
6. **P5 — Release.** Release-Skript für signiertes APK und AAB vorhanden.
   Extern erforderlich bleiben ein privater Keystore und gegebenenfalls der
   Play-Store-Zugang.
