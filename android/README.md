# BudgetPilot — Android-App

> **Archivierter Prototyp:** Dieser Kotlin/Jetpack-Compose-Client wird nicht mehr
> weiterentwickelt. Neue Android-Features, API-Parität, Builds und Releases erfolgen
> ausschließlich in [`androidNet/`](../androidNet/).

Native Begleit-App (Kotlin / Jetpack Compose), die mit einer selbst-gehosteten
BudgetPilot-Instanz über die JSON-API (`/api/auth` + `/api/v1`) kommuniziert.
Dies ist der frühere **P1-Prototyp** des Kotlin-/Jetpack-Compose-Ansatzes.

## Stack
- Kotlin, Jetpack Compose (Material 3), Navigation-Compose
- Hilt (DI), Retrofit + OkHttp + kotlinx.serialization (KSP)
- Bearer-Token-Auth mit automatischem Refresh (OkHttp `Authenticator`)
- Tokens in `EncryptedSharedPreferences` (Keystore), Instanz-URL in SharedPreferences
- min-SDK 26, target/compile-SDK 35

## Was es schon kann
1. **Setup** – Instanz-URL eingeben (z. B. `https://budget.deine-domain.de`).
2. **Login** – E-Mail/Passwort → Bearer-Token (sicher gespeichert, Auto-Refresh).
3. **Dashboard** – lädt die Monatsprojektion (Einnahmen/Ausgaben/Saldo) und die
   Kategorien über die API.

## Voraussetzungen
- Android Studio (aktuelle stabile Version), JDK 17, Android SDK 35.

## Öffnen & Bauen
1. In Android Studio **nur den Ordner `android/`** öffnen (nicht das Repo-Root).
2. Gradle-Sync ausführen. Android Studio lädt den passenden Gradle-Wrapper.
   - Falls die CLI genutzt wird und der Wrapper-Jar fehlt: einmal
     `gradle wrapper` ausführen (lokales Gradle ≥ 8.9 nötig).
3. Versionen sind im Version-Catalog `gradle/libs.versions.toml` zentral — bei
   Sync-Fehlern dort die kompatiblen Versionen anpassen.

## Gegen die lokale Dev-Instanz testen (Emulator)
1. Backend lokal starten: `dotnet run --project ../src/BudgetPilot.Web` (Port 5070).
2. In der App als Instanz-URL `http://10.0.2.2:5070` eingeben
   (`10.0.2.2` = Host-localhost aus dem Android-Emulator; Cleartext ist nur dafür
   freigegeben, siehe `res/xml/network_security_config.xml`).
3. Login (Development): `admin@budgetpilot.local` / `ChangeMe!2026`.

## Historische Hinweise
- Geldbeträge werden im Gerüst als `Double` angezeigt; für korrekte Rundung
  später auf `BigDecimal`/String-Serialisierung umstellen.
- Offene P2/P3-Punkte werden hier bewusst nicht mehr umgesetzt.
