# Guía de Despliegue a Producción - App MAUI

## ✅ CONFIGURACIÓN YA LISTA

La app ya tiene configuración dual automática mediante compilación condicional.

### 🔧 Archivos Configurados:

**Backend (ASP.NET):**
- ✅ `appsettings.json` → **PRODUCCIÓN** (Somee.com)
- ✅ `appsettings.Development.json` → **DESARROLLO** (BD local 192.168.2.105)

**MAUI:**
- ✅ `Helpers/Constants.cs` → Usa `#if DEBUG` / `#else`
  - **DEBUG**: `http://192.168.2.104:5003`
  - **RELEASE**: `https://farmaciasolidaria.somee.com`

---

## 📦 PASOS PARA DESPLEGAR MAÑANA

### 1. Mergear developerConApi → developer
```bash
git checkout developer
git merge developerConApi
git push origin developer
```

### 2. Compilar APK de Producción (Release)
```bash
cd FarmaciaSolidariaCristiana.Maui
dotnet build -f net9.0-android -c Release
```

El APK estará en:
```
bin/Release/net9.0-android/com.fsolidaria.app-Signed.apk
```

### 3. Instalar en dispositivo
```bash
adb install -r bin/Release/net9.0-android/com.fsolidaria.app-Signed.apk
```

---

## 🔍 VERIFICACIÓN

### Debug Mode (Desarrollo):
- Conecta a: `http://192.168.2.104:5003`
- BD: `192.168.2.105`
- Build: `dotnet build -c Debug`

### Release Mode (Producción):
- Conecta a: `https://farmaciasolidaria.somee.com`
- BD: `FarmaciaDb.mssql.somee.com`
- Build: `dotnet build -c Release`

---

## ⚠️ IMPORTANTE

**NO NECESITAS CAMBIAR NINGÚN ARCHIVO**

La app automáticamente usa:
- **Debug** cuando desarrollas localmente
- **Release** cuando compilas para producción

---

## 🚀 Compilación Rápida de Producción

```bash
# Desde la raíz del proyecto
dotnet build FarmaciaSolidariaCristiana.Maui/FarmaciaSolidariaCristiana.Maui.csproj \
  -f net9.0-android \
  -c Release \
  -t:SignAndroidPackage
```

APK final: `FarmaciaSolidariaCristiana.Maui/bin/Release/net9.0-android/com.fsolidaria.app-Signed.apk`
