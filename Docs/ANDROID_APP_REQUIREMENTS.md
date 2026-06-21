# BudgetPilot Android-App — Anforderungen & Vorgehen

Status: Entwurf · Quelle der Wahrheit für die Android-Begleit-App, die mit einer
selbst-gehosteten BudgetPilot-Instanz (.NET 8 / Blazor + ASP.NET Core Identity)
kommuniziert. Code/Bezeichner Englisch, UI-Text & Specs Deutsch, Geld `decimal`,
Geschäftsdaten als Datum (`dd.MM.yyyy`, Locale `de-DE`).

---

## 1. Ziel & Umfang

Eine moderne, native Android-App, mit der ein angemeldeter Benutzer seine
BudgetPilot-Instanz mobil bedient: Übersichten ansehen und Positionen/Versionen
pflegen — auch unterwegs, möglichst auch offline-lesend. Die App ist ein **Client**;
die fachliche Logik (Projektion, Versionierung) bleibt server­seitig.

**Nicht-Ziele (vorerst):** kein eigener Server, keine lokale Budgetlogik, kein
Mehr-Instanz-Sync, keine Zahlungsabwicklung/echtes Geld (wichtig für Google-Policy).

---

## 2. Aktueller Stand (Backend) — was schon da ist / fehlt

- ✅ ASP.NET Core Identity (`IdentityDbContext<IdentityUser>`), Cookie-Login für Web.
- ✅ `AddIdentityCore<IdentityUser>().AddApiEndpoints()` ist verdrahtet (Grundlage
  für Bearer-Token), Multi-User vorhanden, TLS via Caddy/Let's Encrypt.
- ✅ Saubere Schichten: `Application`-Services + DTOs (`IBudgetItemService`,
  `ICategoryService`, `IBudgetProjectionService`, `IAuditLog`) — die API kann dünn
  darauf aufsetzen.
- ❌ **Kein Token-Endpoint gemappt** (`MapIdentityApi` fehlt).
- ❌ **Keine REST/JSON-API** für die Daten (bisher nur Blazor-Server-UI).
- ❌ Bearer-Authentifizierungsschema nicht als Default für API-Routen aktiv.

> Konsequenz: Der größte Arbeitsblock ist **serverseitig** (API + Token), nicht in
> der App selbst. Dank vorhandener Services/DTOs ist die API aber dünn.

---

## 3. Backend-Voraussetzungen (BudgetPilot) — REQ-BE

- **REQ-BE-1 Token-Endpoints.** `MapIdentityApi<IdentityUser>()` unter
  `/api/auth` mappen (liefert `/login`, `/refresh`). Login gibt Access- **und**
  Refresh-Token zurück; öffentliche Registrierung bleibt aus.
- **REQ-BE-2 Bearer-Schema.** Data-API-Routen akzeptieren
  `IdentityConstants.BearerScheme` zusätzlich zum Cookie; Web bleibt Cookie.
- **REQ-BE-3 Versionierte JSON-API** unter `/api/v1`, geschützt mit
  `RequireAuthorization()`. Endpunkte spiegeln die bestehenden Services 1:1:
  - `GET  /api/v1/categories`
  - `GET/POST/PUT  /api/v1/budget-items` (+ `…/{id}`)
  - `POST /api/v1/budget-items/{id}/versions` (neue Version, auch rückwirkend)
  - `PUT  /api/v1/budget-items/{id}/versions/{versionId}` (Korrektur)
  - `POST /api/v1/budget-items/{id}/deactivate|reactivate`, `DELETE …/{id}`
  - `GET  /api/v1/projections/monthly?year&month&mode`
  - `GET  /api/v1/projections/yearly?year&mode`
  - `GET  /api/v1/projections/multi-year?from&to&mode`
  - `GET  /api/v1/audit?max=`
  - (optional, Admin) `…/users`
- **REQ-BE-4 Fehlerformat.** Einheitlich `ProblemDetails` (RFC 7807); fachliche
  `DomainException` → `400` mit verständlicher Meldung (de-DE).
- **REQ-BE-5 Serialisierung.** `System.Text.Json`, `decimal` als Zahl (nicht
  String), Datum als ISO `yyyy-MM-dd`. Enums als String (`"Income"`, `"Monthly"`).
- **REQ-BE-6 OpenAPI.** Swagger/OpenAPI-Dokument generieren → daraus kann der
  Android-API-Client (Retrofit-Interfaces) erzeugt/geprüft werden.
- **REQ-BE-7 Rate-Limiting & Logging** der API (Brute-Force-Schutz am
  Login-Endpoint), Audit-Einträge wie im Web (Akteur = API-User).
- **REQ-BE-8 CORS** nicht nötig für native App (kein Browser-Origin); falls später
  eine PWA/Web-Client dazukommt, separat konfigurieren.

---

## 4. Android Tech-Stack & Architektur — REQ-AND

- **REQ-AND-1 Sprache/UI.** Kotlin + Jetpack Compose (Material 3). Min-SDK 26
  (Android 8), Target-SDK aktuell (derzeit API 35).
- **REQ-AND-2 Architektur.** Clean Architecture: `data` (API, DTOs, Repos,
  Mapper), `domain` (Modelle, UseCases), `ui` (Compose + ViewModels). MVVM/MVI,
  unidirektionaler Datenfluss, `StateFlow`.
- **REQ-AND-3 DI.** Hilt.
- **REQ-AND-4 Netzwerk.** Retrofit + OkHttp + `kotlinx.serialization`. Auth-
  Interceptor hängt Bearer-Token an; Authenticator macht automatischen
  Refresh bei `401`.
- **REQ-AND-5 Async.** Coroutines + Flow.
- **REQ-AND-6 Navigation.** Navigation-Compose, typisierte Routen.
- **REQ-AND-7 Lokaler Cache.** Room für gelesene Projektionen/Positionen
  (Offline-Lesen); Einstellungen via DataStore.
- **REQ-AND-8 Lokalisierung.** de-DE: EUR-Formatierung, Datum `dd.MM.yyyy`,
  Dezimal-Komma. (Bewusst spiegelbildlich zur Web-Konvention.)
- **REQ-AND-9 Theming.** An den Web-Prototyp angelehnt (Plus Jakarta Sans,
  Akzent `#C2410C`), Light/Dark.
- **REQ-AND-10 Tests.** Unit (ViewModels/UseCases), API-Contract-Tests gegen
  OpenAPI, einige Compose-UI-Tests.

---

## 5. Authentifizierung & Sicherheit — REQ-SEC

- **REQ-SEC-1 Instanz-URL konfigurierbar.** Beim ersten Start gibt der Nutzer
  seine Basis-URL ein (z. B. `https://budget.meine-domain.de`). Validierung +
  Erreichbarkeitscheck.
- **REQ-SEC-2 Login.** E-Mail/Passwort → `/api/auth/login` → Access/Refresh-Token.
- **REQ-SEC-3 Token-Speicherung.** Refresh-Token in `EncryptedSharedPreferences`
  bzw. Android Keystore; niemals im Klartext/Log.
- **REQ-SEC-4 Auto-Refresh & Logout** bei abgelaufenem/ungültigem Token.
- **REQ-SEC-5 TLS-Pflicht.** Nur HTTPS; Cleartext per Network-Security-Config
  verboten. Let's-Encrypt-Zertifikate der Instanz sind regulär gültig → kein
  Pinning nötig (optional als Härtung möglich).
- **REQ-SEC-6 App-Lock (optional).** Biometrie/PIN beim Öffnen (Finanzdaten).
- **REQ-SEC-7 Keine Secrets im Repo/Build.** Signing-Keys & Tokens außerhalb VCS.

---

## 6. Funktionsumfang (MVP) — REQ-FEAT

Spiegelt den Web-Funktionsumfang, mobil priorisiert:

- **REQ-FEAT-1 Dashboard** (Monat/Jahr-Umschalter, KPIs, Kategorie-Balken).
- **REQ-FEAT-2 Monats-/Jahres-/Mehrjahresübersicht** (Budget- & Cashflow-Sicht).
- **REQ-FEAT-3 Positionen** anlegen, bearbeiten, deaktivieren/löschen.
- **REQ-FEAT-4 Versionen** anlegen (auch rückwirkend) und **korrigieren** —
  inkl. Monatsgrenzen-Logik serverseitig (App ruft nur die API).
- **REQ-FEAT-5 Kategorien** verwalten.
- **REQ-FEAT-6 Aktivitätsprotokoll** ansehen.
- **REQ-FEAT-7 (später) Benutzerverwaltung** (Admin), **Push-Erinnerungen**.

---

## 7. Nicht-funktionale Anforderungen — REQ-NFR

- Offline-Lesen der zuletzt geladenen Daten; klare Sync-/Fehlerzustände.
- Schnelle Startzeit, sparsamer Datenverbrauch (ETags/Caching optional).
- Barrierefreiheit (Schriftgrößen, Kontrast, TalkBack-Labels).
- Stabilität: kein Crash bei Netzwerkfehlern; ProblemDetails verständlich anzeigen.
- Versionierung der App analog Backend (SemVer), sichtbarer Build-Stand.

---

## 8. Google Play — Konto, Beantragungen, Richtlinien, Prozess

> Reihenfolge grob: Entwicklerkonto → Identitätsprüfung → App anlegen →
> Pflichtangaben (Datenschutz/Data-Safety/Content-Rating) → (für neue
> Privatkonten) geschlossener Test mit 12 Testern/14 Tage → Produktion.

- **REQ-GP-1 Play-Entwicklerkonto.** Einmalige Gebühr **25 USD**. Wahl
  **Privatperson** oder **Organisation**.
- **REQ-GP-2 Identitätsprüfung (Pflicht).** Name, Adresse, E-Mail, Telefon;
  ggf. Ausweis. **Organisation** benötigt eine **D-U-N-S-Nummer** (kostenlos
  beantragbar, Bearbeitung kann Wochen dauern → früh starten).
- **REQ-GP-3 Test-Pflicht für neue Privatkonten.** Neue Personal-Accounts müssen
  vor dem Produktions-Launch einen **geschlossenen Test mit mind. 12 Testern, die
  14 Tage durchgehend opted-in sind**, durchführen. → Tester (Familie/Freunde)
  früh einplanen; ggf. Organisation wählen, falls das vermeidbar sein soll.
- **REQ-GP-4 Datenschutzerklärung (URL Pflicht).** Besonders bei Finanz-/sensiblen
  Daten. Muss erklären, dass Daten zur **eigenen, selbst-gehosteten Instanz**
  des Nutzers gehen und die App selbst nichts sammelt.
- **REQ-GP-5 Data-Safety-Formular.** Wahrheitsgemäß deklarieren (Finanzdaten,
  Account-Daten; Übertragung verschlüsselt; keine Weitergabe an Dritte).
- **REQ-GP-6 Content-Rating** (Fragebogen) ausfüllen.
- **REQ-GP-7 Financial-Features-Deklaration.** BudgetPilot bewegt **kein echtes
  Geld** (reine Haushaltsplanung) → die strengen Vorgaben für Kredit-/Zahlungs-
  Apps greifen nicht; trotzdem als „Finanzen/Budget" einordnen.
- **REQ-GP-8 Technik-Vorgaben.** Auslieferung als **Android App Bundle (.aab)**,
  **Play App Signing** (Google verwaltet den Signing-Key, du lieferst Upload-Key),
  aktuelles **Target-API-Level** (Play erzwingt jährlich ~aktuell-1).
- **REQ-GP-9 Berechtigungen minimal.** Nur `INTERNET` (+ optional
  `USE_BIOMETRIC`). Keine sensiblen Berechtigungen.
- **REQ-GP-10 Store-Eintrag.** Icon, Feature-Grafik, Screenshots, Kurz-/
  Langbeschreibung (de + en empfehlenswert).
- **REQ-GP-11 (optional) Push.** Für Erinnerungen ein **Firebase-Projekt (FCM)**
  anlegen (separat, kostenfreier Tier). Erfordert Backend-Integration.

### Alternative ohne Google Play
Da BudgetPilot privat/Haushalt ist, ist auch **Sideload** möglich (signiertes APK
direkt verteilen) oder Distribution über **F-Droid/eigener Kanal** — dann entfällt
das gesamte Play-Prozedere (Konto, Gebühr, Test-Pflicht, Data-Safety). Nachteil:
keine automatischen Updates über den Store, manuelles Vertrauen für „unbekannte
Quellen". **Empfehlung:** Wenn nur Familie/eigene Nutzung → Sideload reicht oft;
wenn öffentliche Verfügbarkeit gewünscht → Play.

---

## 9. Vorgehen / Phasen

1. **P0 — Backend-API (Voraussetzung).** Token-Endpoints + Bearer-Schema +
   `/api/v1`-Endpunkte auf bestehende Services, OpenAPI, ProblemDetails,
   Rate-Limit. *Ohne das kann die App nichts.*
2. **P1 — App-Grundgerüst.** Projekt, Hilt, Retrofit/OkHttp, Instanz-URL-Setup,
   Login + Token-Refresh + sichere Speicherung.
3. **P2 — Lesepfad.** Dashboard + Übersichten (online), dann Room-Offline-Cache.
4. **P3 — Schreibpfad.** Positionen/Versionen anlegen/korrigieren, Kategorien.
5. **P4 — Feinschliff.** Theming, App-Lock, Aktivitätsprotokoll, i18n, Tests.
6. **P5 — Release.** Sideload: signiertes APK bauen & verteilen (sofort nutzbar).
   Parallel **früh** den Play-Vorlauf starten (Konto, Identitätsprüfung,
   ggf. D-U-N-S, geschlossener 12-Tester-Test), damit ein späterer Play-Launch
   nicht an Wartezeiten hängt.

> Schreiben ist im MVP enthalten (Entscheidung §10). Offline-**Schreiben** mit
> Sync ist eine spätere Ausbaustufe; im MVP erfolgt Schreiben online, Lesen auch
> aus dem Room-Cache.

---

## 10. Getroffene Entscheidungen

- ✅ **Distribution: Beides — Sideload jetzt, Play später.** Entwicklung & Nutzung
  starten per signiertem APK (kein Google-Overhead). Der Play-Vorlauf
  (Konto, Identitätsprüfung, ggf. D-U-N-S, 12-Tester-Phase) wird **parallel früh**
  angestoßen, da diese Schritte Wochen brauchen.
- ✅ **MVP-Umfang: Lesen + Schreiben von Anfang an.** Übersichten **und** Pflege
  von Positionen/Versionen (inkl. rückwirkender Korrektur) und Kategorien.
- ✅ **Repo-Struktur: Monorepo.** Android-Code im Ordner `android/` desselben
  Repos; gemeinsamer OpenAPI-Vertrag und Versionierung an einem Ort. Gradle-Build
  getrennt vom .NET-Build (CI-Workflows separat triggern: `.github/workflows`).

### Noch offen (später entscheidbar)
- **Konto-Typ bei Play:** Privatperson (→ 12-Tester-Pflicht) vs. Organisation
  (→ D-U-N-S). Vor der Play-Einreichung festlegen.
- **Offline-Tiefe beim Schreiben:** zunächst Schreiben nur online (Lesen aus
  Cache offline); Offline-Schreiben mit späterem Sync ist eine spätere Ausbaustufe.
- **Push-Erinnerungen (FCM):** nach dem MVP.
