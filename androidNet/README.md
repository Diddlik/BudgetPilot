# BudgetPilot — Android .NET App

Installierbare Android-Begleit-App als **.NET MAUI Blazor Hybrid App**. Dieser
Ansatz lebt bewusst in `androidNet/`, damit er nicht mit dem parallelen
Kotlin-/Jetpack-Compose-Ansatz in `../android/` kollidiert.

Die App ist ein Client für eine selbst-gehostete BudgetPilot-Instanz und spricht
mit der bestehenden JSON-API (`/api/auth` + `/api/v1`). Fachliche Logik wie
Projektion, Versionierung und Validierung bleibt im Backend; die App
wiederverwendet vorhandene Application-DTOs und Domain-Enums per ProjectReference.

## Stack

- .NET 8, .NET MAUI, Blazor Hybrid (`BlazorWebView`)
- Android Target `net8.0-android`, min-SDK 26
- Bearer-Token-Auth gegen ASP.NET Core Identity (`/api/auth/login`, `/refresh`)
- Access-/Refresh-Token in `SecureStorage`, Instanz-URL in `Preferences`
- UI-Texte Deutsch, EUR-/Datumsformat `de-DE`

## Was es schon kann

1. **Setup** - Instanz-URL eingeben, HTTPS erzwingen; HTTP nur für lokale
   Emulator-Tests (`10.0.2.2`/`localhost`).
2. **Login** - E-Mail/Passwort gegen `/api/auth/login`; Token werden sicher
   gespeichert.
3. **Dashboard** - lädt die Monatsprojektion aus `/api/v1/projections/monthly`,
   zeigt KPIs, Kategorie-Balken und Positionen, inklusive Budget-/Cashflow-Modus
   und Monatsnavigation.
4. **Refresh** - bei `401` wird `/api/auth/refresh` versucht; bei Fehlschlag geht
   die App zurück zum Login.
5. **P2-Lesepfade** - eine mobile Hauptnavigation öffnet Listen für
   Budgetpositionen und Kategorien. Positionen zeigen aktuelle Version, Betrag,
   Frequenz, Gültigkeitsbeginn und Aktivstatus; Kategorien zeigen Zuordnungszahl
   und Aktivstatus.
6. **Jahresplanung** - Jahres-KPIs, Monatsverlauf, Monatssalden und
   Fünfjahresvergleich, jeweils in Budget- oder Cashflow-Sicht.
7. **Aktivität** - die letzten Änderungen der Instanz als mobil lesbare
   Ereigniskarten inklusive Zeitpunkt, Benutzer und Details.
8. **Schreiben** - Kategorien pflegen sowie Positionen und deren zeitliche
   Versionen anlegen, korrigieren, deaktivieren, reaktivieren oder löschen.
9. **Mobilreife** - Offline-Lese-Cache mit sichtbarem Datenstand, optionale
   lokale App-PIN, zugängliche Formulare und robuste Netzwerkfehlerzustände.
10. **Biometrie & Einstellungen** - AndroidX `BiometricPrompt` entsperrt die App
    per Fingerabdruck oder Gesicht, immer mit App-PIN als Fallback. Im
    „Mehr“-Bereich lassen sich Konto, Sicherheit, Startansicht,
    Budget-/Cashflow-Standard, Instanz-URL und Offline-Daten verwalten.

## Öffnen & Bauen

```bash
cd androidNet
dotnet workload restore
dotnet build BudgetPilot.Mobile.csproj -f net8.0-android
```

Das Projekt ist bewusst **nicht** Teil von `../BudgetPilot.sln`, damit der normale
Backend-/Docker-Build keine Android-/MAUI-Workload voraussetzt.

## Gegen die lokale Dev-Instanz testen (Emulator)

1. Backend lokal starten: `dotnet run --project ../src/BudgetPilot.Web --launch-profile http`.
2. Im Emulator als Instanz-URL `http://10.0.2.2:5070` verwenden (Debug-Builds schlagen diesen Wert bereits vor).
3. Login (Development): `admin@budgetpilot.local` / `ChangeMe!2026`.

Für echte Geräte und produktive Nutzung HTTPS verwenden.

## APK auf dem Ziel-Emulator installieren

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb -s emulator-5556 install -r "F:\Coding\BudgetPilot\androidNet\bin\Debug\net8.0-android\de.budgetpilot.mobile-Signed.apk"
& $adb -s emulator-5556 shell am start -S -n de.budgetpilot.mobile/crc645ee8833f816a263c.MainActivity
```

Falls `adb install` ohne hilfreiche Fehlermeldung abbricht, funktioniert meist der
Push-Install-Pfad:

```powershell
$adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
& $adb -s emulator-5556 push "F:\Coding\BudgetPilot\androidNet\bin\Debug\net8.0-android\de.budgetpilot.mobile-Signed.apk" /data/local/tmp/de.budgetpilot.mobile-Signed.apk
& $adb -s emulator-5556 shell pm install -r -d -g /data/local/tmp/de.budgetpilot.mobile-Signed.apk
& $adb -s emulator-5556 shell rm /data/local/tmp/de.budgetpilot.mobile-Signed.apk
```

## Signiertes Release (APK + AAB)

Der private Keystore bleibt außerhalb des Repositories. Vor dem Release-Build
werden vier Umgebungsvariablen gesetzt:

```powershell
$env:BUDGETPILOT_KEYSTORE = "C:\Secrets\budgetpilot-release.jks"
$env:BUDGETPILOT_KEY_ALIAS = "budgetpilot"
$env:BUDGETPILOT_KEYSTORE_PASSWORD = "<secret>"
$env:BUDGETPILOT_KEY_PASSWORD = "<secret>"
.\build-release.ps1
```

Das Skript erzeugt ein signiertes APK für Sideloading und ein AAB für Google
Play unter `androidNet/artifacts/`. Der Keystore und Passwörter werden weder
kopiert noch protokolliert.

## Tests

```powershell
dotnet test ..\androidNet.Tests\BudgetPilot.Mobile.Tests.csproj -m:1
```
