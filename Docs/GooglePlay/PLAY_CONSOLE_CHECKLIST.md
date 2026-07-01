# BudgetPilot – Google Play Console

Stand: 01.07.2026

Diese Datei enthält die konkreten Werte für den ersten Play-Store-Eintrag. Mit
`[EINTRAGEN]` markierte Angaben müssen vor der Veröffentlichung durch echte,
öffentlich erreichbare Werte ersetzt werden.

Offizielle Referenzen:

- [App erstellen und Store-Eintrag pflegen](https://support.google.com/googleplay/android-developer/answer/9859152)
- [Vorgaben für Symbol, Feature-Grafik und Screenshots](https://support.google.com/googleplay/android-developer/answer/9866151)
- [Target-API-Anforderungen](https://support.google.com/googleplay/android-developer/answer/11926878)
- [App-Inhalte und Zugriffsanweisungen](https://support.google.com/googleplay/android-developer/answer/9859455)
- [Datensicherheitsformular](https://support.google.com/googleplay/android-developer/answer/10787469)
- [Inhaltsbewertung](https://support.google.com/googleplay/android-developer/answer/9898843)
- [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)
- [Testanforderungen für neue persönliche Konten](https://support.google.com/googleplay/android-developer/answer/14151465)

## 1. App erstellen

| Feld | Eintrag |
|---|---|
| App-Name | `BudgetPilot` |
| Standardsprache | Deutsch (Deutschland) – `de-DE` |
| App oder Spiel | App |
| Kostenlos oder kostenpflichtig | Kostenlos |
| Paketname | `de.budgetpilot.mobile` |

Der Paketname und die Entscheidung „kostenlos“ sind praktisch dauerhaft. Die
App soll ohne Werbung und ohne In-App-Käufe veröffentlicht werden.

## 2. Store-Eintrag

**Kategorie:** Finanzen

**Kurzbeschreibung (70/80 Zeichen):**

> Dein selbst gehostetes Haushaltsbudget – klar planen, sicher im Blick.

**Vollständige Beschreibung:**

> BudgetPilot bringt dein selbst gehostetes Haushaltsbudget auf Android.
> Plane wiederkehrende und einmalige Einnahmen und Ausgaben, vergleiche
> Budget- und Cashflow-Sicht und behalte deine Monats-, Jahres- und
> Mehrjahresentwicklung im Blick.
>
> DEIN BUDGET AUF EINEN BLICK
>
> • Dashboard mit Einnahmen, Ausgaben und verfügbarem Budget
> • Monats-, Jahres- und Mehrjahresübersichten
> • Budget- und Cashflow-Sicht für realistische Planung
> • Kategorien und Budgetpositionen übersichtlich verwalten
> • Änderungen ab einem Stichtag versionieren, ohne historische Monate zu verändern
> • Aktivitätsprotokoll für nachvollziehbare Änderungen
>
> FÜR DEINE EIGENE INSTANZ
>
> BudgetPilot ist ein Client für eine selbst gehostete BudgetPilot-Instanz.
> Beim Anmelden wählst du HTTP oder HTTPS und gibst den Hostnamen deiner Instanz
> sowie deine dort eingerichteten Zugangsdaten ein. Für produktiv erreichbare
> Instanzen wird HTTPS empfohlen. Deine Daten werden nicht an einen zentralen
> BudgetPilot-Dienst übertragen.
>
> GESCHÜTZT AUF DEM GERÄT
>
> Zugangstoken werden im geschützten Gerätespeicher abgelegt. Optional kannst
> du die App mit einer lokalen PIN und der Biometrie deines Geräts sperren.
>
> BudgetPilot stellt keine Bankverbindung her, führt keine Zahlungen aus und
> bietet keine Finanz-, Anlage- oder Steuerberatung.

**Kontaktdaten im Store:**

| Feld | Eintrag |
|---|---|
| Support-E-Mail | `[EINTRAGEN: dauerhaft betreute E-Mail-Adresse]` |
| Website | `[EINTRAGEN: öffentliche HTTPS-Projekt- oder Supportseite]` |
| Telefon | leer lassen, sofern kein öffentlicher Support per Telefon angeboten wird |
| Datenschutzrichtlinie | `[EINTRAGEN: öffentliche HTTPS-URL zur finalisierten PRIVACY_POLICY.md]` |

## 3. Grafiken

Im Ordner `Docs/GooglePlay/assets/` liegen die vorbereiteten Quellen und
gerenderten Dateien.

| Asset | Datei | Vorgabe |
|---|---|---|
| App-Symbol | `app-icon-512.png` | 512 × 512, PNG |
| Feature-Grafik | `feature-graphic-1024x500.png` | 1024 × 500, PNG ohne Transparenz |
| Smartphone-Screenshots | noch erstellen | mindestens 2; empfohlen 6–8 |

Empfohlene Screenshot-Reihenfolge:

1. Dashboard – „Dein Budget auf einen Blick“
2. Monatsübersicht – „Budget und Cashflow vergleichen“
3. Jahresübersicht – „Das ganze Jahr im Blick“
4. Mehrjahresübersicht – „Langfristig vorausplanen“
5. Budgetpositionen – „Einnahmen und Ausgaben verwalten“
6. Einstellungen – „Mit PIN und Biometrie geschützt“

Für die Screenshots die isolierte Demo-Instanz mit Dummy-Daten verwenden:

```powershell
cd androidNet
.\start-screenshot-instance.ps1 -Reset
```

## 4. App-Inhalte

Die folgenden Antworten passen zum aktuellen Funktionsumfang. Vor Absenden
noch einmal gegen den tatsächlich hochgeladenen Build prüfen.

| Erklärung | Antwort |
|---|---|
| Werbung | Nein, die App enthält keine Werbung |
| App-Zugriff | Einige oder alle Funktionen sind eingeschränkt (Anmeldung erforderlich) |
| Zielgruppe | 18 Jahre und älter |
| Nachrichten-App | Nein |
| Behörden-App | Nein |
| Gesundheits-App | Nein |
| Finanzfunktionen | „Meine App bietet keine Finanzfunktionen“ |
| Kontenerstellung | Nutzer können in der App kein Konto erstellen |

Die Antwort zu Finanzfunktionen bedeutet: Die App plant private Budgets, bietet
aber weder Banking, Zahlungen, Kredite, Versicherungen, Handel noch
Finanzberatung. Falls Google die reine Budgetplanung künftig ausdrücklich als
deklarationspflichtige Funktion einordnet, muss diese Antwort angepasst werden.

### App-Zugriff für die Prüfung

Google benötigt eine von außen erreichbare, dauerhaft verfügbare Testinstanz.
Die lokale Screenshot-Instanz ist dafür nicht geeignet.

| Feld | Eintrag |
|---|---|
| Instanz-URL | `[EINTRAGEN: https://demo.example.org]` |
| E-Mail | `[EINTRAGEN: stabiles Review-Konto]` |
| Passwort | `[EINTRAGEN: stabiles Review-Passwort]` |
| Weitere Faktoren | Keine MFA, kein Einmalpasswort, keine Standortbeschränkung |

**Englische Prüfanweisung:**

> On the login screen, enter the instance URL, email address and password
> provided above. Tap “Sicher anmelden”. No one-time password or additional
> verification is required. The account has access to all app features and
> contains non-personal demo data.

### Inhaltsbewertung

Den IARC-Fragebogen wahrheitsgemäß ausfüllen. Für den aktuellen Stand sind
Gewalt, Sexualität, Glücksspiel, Drogen, Schimpfwörter, Käufe, Werbung,
öffentliche Nutzerkommunikation und frei zugängliche Webinhalte jeweils
„Nein“. Die endgültige Altersfreigabe vergibt IARC, sie darf nicht vorweggenommen
werden.

### Datensicherheit

BudgetPilot überträgt Daten ausschließlich an die vom Nutzer gewählte
BudgetPilot-Instanz. Auch eine Übertragung an einen selbst gehosteten Server gilt
bei Google als „Erhebung“.

| Datentyp | Erhoben | Geteilt | Zweck | Pflicht |
|---|---:|---:|---|---:|
| E-Mail-Adresse | Ja | Nein | App-Funktionalität, Anmeldung | Ja |
| Sonstige Finanzinformationen (Budgetdaten) | Ja | Nein | App-Funktionalität | Ja |

Zusätzliche Antworten:

- Keine Werbung, Analyse-, Tracking- oder Drittanbieter-SDKs.
- Daten werden nicht verkauft und nicht an Dritte weitergegeben.
- HTTPS ist vorausgewählt und wird für produktiv erreichbare Instanzen
  empfohlen. Bei bewusst gewähltem HTTP warnt die App vor der unverschlüsselten
  Verbindung.
- Zugangstoken liegen im geschützten Gerätespeicher.
- Die App bietet keine Kontenerstellung. Konten und serverseitige Daten werden
  durch den Betreiber der jeweiligen Instanz verwaltet und gelöscht.
- Die öffentliche Datenschutzrichtlinie muss Verantwortlichen, Kontakt,
  Datentypen, Zwecke, Empfänger, Schutz, Aufbewahrung und Löschung beschreiben.

## 5. Technische Veröffentlichung

- [x] Paket-ID festgelegt: `de.budgetpilot.mobile`
- [x] Anzeigename: `BudgetPilot`
- [x] Version Name: `1.0.0`
- [x] Version Code: `1`
- [x] Mindestversion: Android 8 / API 26
- [x] Target: Android API 36.1 (übertrifft die aktuelle API-35-Mindestanforderung)
- [ ] Öffentliche HTTPS-Demo-Instanz und Review-Konto bereitstellen
- [ ] Datenschutzrichtlinie finalisieren und öffentlich per HTTPS veröffentlichen
- [x] Eigene Upload-Key-Datei sicher erzeugen und außerhalb des Repositories sichern
- [ ] Play App Signing aktivieren; Google den App-Signing-Key erzeugen lassen
- [x] Signiertes Android App Bundle (`.aab`) bauen
- [ ] AAB zuerst in „Interner Test“ hochladen und auf einem echten Gerät installieren
- [ ] Store-Screenshots erstellen und hochladen
- [ ] Alle App-Inhalte- und Datensicherheits-Erklärungen absenden
- [ ] Closed Test durchführen, falls die Play Console ihn für das Konto verlangt
- [ ] Produktionszugang beantragen und Release zur Prüfung senden

Release-Build:

```powershell
cd androidNet
.\build-release.ps1
```

Keystore, Alias und Kennwörter dürfen weder committed noch in Screenshots oder
Build-Logs veröffentlicht werden. Jeder spätere Release benötigt einen höheren
`ApplicationVersion`-Wert.

## 6. Mögliche Kontosperre vor Produktion

Wenn das persönliche Entwicklerkonto nach dem 13.11.2023 erstellt wurde, kann
Google vor dem Produktionszugang einen geschlossenen Test mit mindestens zwölf
Testern verlangen, die 14 Tage ohne Unterbrechung angemeldet bleiben. Maßgeblich
ist die konkrete Anzeige im Play-Console-Dashboard.
