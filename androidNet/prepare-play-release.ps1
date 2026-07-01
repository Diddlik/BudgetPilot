param(
    [string]$KeystorePath = (Join-Path $env:USERPROFILE ".android-keys\BudgetPilot\budgetpilot-upload.jks"),
    [string]$Alias = "budgetpilot-upload",
    [string]$OutputPath = (Join-Path $PSScriptRoot "artifacts")
)

$ErrorActionPreference = "Stop"

function ConvertTo-PlainText([Security.SecureString]$SecureValue) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

$keytoolCandidates = @(
    "C:\Program Files\Java\jdk-17\bin\keytool.exe",
    "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe"
)

if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
    $keytoolCandidates = @((Join-Path $env:JAVA_HOME "bin\keytool.exe")) + $keytoolCandidates
}

$keytool = $keytoolCandidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

if (-not $keytool) {
    throw "keytool.exe wurde nicht gefunden. Installiere ein JDK oder Android Studio."
}

$isNewKeystore = -not (Test-Path -LiteralPath $KeystorePath -PathType Leaf)
$securePassword = Read-Host "Passwort für den BudgetPilot-Upload-Key" -AsSecureString
$password = ConvertTo-PlainText $securePassword

if ([string]::IsNullOrWhiteSpace($password) -or $password.Length -lt 12) {
    throw "Das Passwort muss mindestens 12 Zeichen lang sein."
}

if ($isNewKeystore) {
    $secureConfirmation = Read-Host "Passwort wiederholen" -AsSecureString
    $confirmation = ConvertTo-PlainText $secureConfirmation
    if ($password -cne $confirmation) {
        throw "Die Passwörter stimmen nicht überein."
    }
}

try {
    $env:BUDGETPILOT_KEYSTORE = [IO.Path]::GetFullPath($KeystorePath)
    $env:BUDGETPILOT_KEY_ALIAS = $Alias
    $env:BUDGETPILOT_KEYSTORE_PASSWORD = $password
    $env:BUDGETPILOT_KEY_PASSWORD = $password

    if ($isNewKeystore) {
        $directory = Split-Path -Parent $env:BUDGETPILOT_KEYSTORE
        New-Item -ItemType Directory -Path $directory -Force | Out-Null

        & $keytool -genkeypair -v `
            -keystore $env:BUDGETPILOT_KEYSTORE `
            -storetype PKCS12 `
            -storepass:env BUDGETPILOT_KEYSTORE_PASSWORD `
            -keypass:env BUDGETPILOT_KEY_PASSWORD `
            -alias $Alias `
            -keyalg RSA `
            -keysize 4096 `
            -validity 10000 `
            -dname "CN=BudgetPilot Upload, OU=Mobile, O=BudgetPilot, C=DE"

        if ($LASTEXITCODE -ne 0) {
            throw "Upload-Key konnte nicht erzeugt werden (Exitcode $LASTEXITCODE)."
        }

        Write-Host "Upload-Key erstellt: $($env:BUDGETPILOT_KEYSTORE)" -ForegroundColor Green
        Write-Warning "Sichere die Keystore-Datei und das Passwort getrennt an einem geschützten Ort."
    }

    & $keytool -list `
        -keystore $env:BUDGETPILOT_KEYSTORE `
        -storepass:env BUDGETPILOT_KEYSTORE_PASSWORD `
        -alias $Alias | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Keystore, Alias oder Passwort ist ungültig."
    }

    & (Join-Path $PSScriptRoot "build-release.ps1") -OutputPath $OutputPath
}
finally {
    $password = $null
    $confirmation = $null
    Remove-Item Env:BUDGETPILOT_KEYSTORE_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:BUDGETPILOT_KEY_PASSWORD -ErrorAction SilentlyContinue
}
