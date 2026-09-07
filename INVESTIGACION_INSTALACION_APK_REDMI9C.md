# Investigación CONFIRMADA: "La aplicación no se instaló" — Redmi 9C (M2006C3LC)

Fecha: 2026-09-07 (2.ª fase: análisis del APK real)
Proyecto: FarmaciaSolidariaCristiana · APK analizado: `farmaciasolidaria.apk` (38.308.113 bytes, 2026-08-27)
Dispositivo: Xiaomi Redmi 9C M2006C3LC (angelica) · Android 10 · MIUI Global 12.0.18 · Helio G35/G25

---

## ✅ VEREDICTO: INSTALL_FAILED_NO_MATCHING_ABIS

**El APK no trae librerías nativas para ARM de 32 bits (`armeabi-v7a`) y el Redmi 9C corre Android de 32 bits → Android rechaza la instalación con el error genérico "La aplicación no se instaló".**

## Evidencia (cadena completa)

### 1. Qué contiene el APK (verificado con aapt/apksigner/unzip)
```
package: com.fsolidaria.app · versionCode 8 · versionName 1.0.8
sdkVersion: 24 (minSdk Android 7.0)   → el Android 10 del teléfono cumple
targetSdkVersion: 36                  → NO bloquea instalación en Android 10
native-code: 'arm64-v8a' 'x86_64'     ← ⚠️ SOLO 64 bits
lib/: arm64-v8a y x86_64 (NO existe lib/armeabi-v7a)
Integridad zip: OK · sha256 baeb7f852e5f28c06d1531df3f89e5ca6c561d58b732582998788fdd1e7faf65
```

### 2. Qué teléfono es (fuentes: XDA, postmarketOS, foros AIDA64, Reddit)
El Redmi 9C (angelica) tiene SoC MediaTek Helio **de 64 bits**, pero Xiaomi lo envió con **Android 10/MIUI de userspace 32 bits** (kernel arm64 + sistema 32-bit; variante "a64 ab"). La ABI primaria del dispositivo es `armeabi-v7a`; **no puede ejecutar ni instalar apps solo-arm64**.

### 3. Por qué el sistema responde "La aplicación no se instaló"
Al instalar, Android compara la ABI del APK con `ro.product.cpu.abilist` del teléfono:
`armeabi-v7a,armeabi` → no hay coincidencia con `arm64-v8a,x86_64` → `INSTALL_FAILED_NO_MATCHING_ABIS`.
El PackageInstaller de MIUI muestra ese fallo como el mensaje **genérico**, sin código visible.

### 4. Por qué encaja con todo lo observado
- **Es la primera instalación**: no hay conflicto de versión/firma previa (se descarta la hipótesis H1 de la fase 1).
- **"Siempre son chinos"**: los teléfonos económicos chinos (Redmi 9A/9C, POCO C3 y clones MTK) con ROM de 32 bits son justo los que fallan; los teléfonos más modernos (Android 11+ o ROM arm64) instalan sin problema → el fallo parece "aislado" pero es un patrón de ABI.
- **600 usuarios por web**: la web no tiene nada que ver con ABIs → todos acceden bien.

## 🔎 Hallazgo secundario (no causa de ESTE fallo, pero importante)

El APK está firmado con **clave de DEBUG**: `Signer #1 DN: CN=Android Debug, O=Android, C=US` (SHA-1 `d28badde…`, el certificado de debug estándar). Es decir: **este binario NO está firmado con el keystore de producción** que se cree estar usando — a menos que en el Mac el "release" esté firmando con `~/.android/debug.keystore` por defecto.
Consecuencias:
- No rompe la instalación limpia, pero **cualquier teléfono que ya tenga una versión firmada con otra clave rechazará la actualización** (`INSTALL_FAILED_UPDATE_INCOMPATIBLE`).
- Verificar en el Mac qué keystore firma realmente (csproj local, `-p:AndroidSigningKeyStore`, o UI de Visual Studio/VS Code). El csproj del repo no define keystore → el build firma con debug a menos que se pase por línea de comandos.

## 🛠️ Solución (lado build, en el Mac)

Generar el APK incluyendo ARM de 32 bits (`android-arm` = `armeabi-v7a`). .NET Android todavía soporta ese RID (pack `Microsoft.Android.Runtime.Mono.36.android-arm`).

Opción A — APK universal (recomendada para distribución directa a teléfonos variados):
```xml
<!-- FarmaciaSolidariaCristiana.Maui.csproj -->
<RuntimeIdentifiers>android-arm;android-arm64</RuntimeIdentifiers>
```
(o en el comando: `-p:RuntimeIdentifiers=android-arm;android-arm64`)

Opción B — un APK por ABI:
```bash
dotnet publish -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm   # para los 9C/32-bit
dotnet publish -f net10.0-android -c Release -p:RuntimeIdentifier=android-arm64 # para el resto
```

Verificación antes de distribuir:
```bash
$HOME/Android/build-tools/36.0.0/aapt dump badging app.apk | grep native-code
# debe aparecer: native-code: 'armeabi-v7a' 'arm64-v8a'
```

Notas:
- El APK crecerá algo (~+15 MB por incluir arm 32-bit); aceptable para distribución manual.
- Mantener SIEMPRE la misma clave de firma (la de producción real) en todas las versiones para que las actualizaciones funcionen en los teléfonos que ya tienen la app.
- Confirmación opcional en el propio teléfono: `adb install` imprime `INSTALL_FAILED_NO_MATCHING_ABIS`, y `adb logcat -d | grep -i installd` tras el intento lo registra; AIDA64 muestra "Android: 32-bit".

## Descartado con evidencia
- minSdk (24 ≤ Android 10) ✓
- targetSdk 36 bloqueando sideload en Android 10 (no aplica) ✓
- Conflicto de firma por actualización (primera instalación) ✓
- APK corrupto (zip OK, integridad verificada) ✓
- Almacenamiento (18 GB libres) ✓
- MIUI "fuentes desconocidas"/Play Protect (el error real es de ABI, previo a cualquier política) ✓
