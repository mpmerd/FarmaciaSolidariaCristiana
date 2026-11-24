# Guía Paso a Paso: Regenerar API Key de Firebase/Google Cloud

## 📋 Información de tu Proyecto

- **Proyecto ID**: `farmaciasolidaria-11ee6`
- **API Key antigua (A REVOCAR)**: `AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`
- **Package Name Android**: `com.fsolidaria.app`

---

## 🔄 MÉTODO 1: Regenerar desde Firebase Console (MÁS FÁCIL)

### Paso 1: Acceder a Firebase Console

1. Ve a: **https://console.firebase.google.com/**
2. Inicia sesión con tu cuenta de Google
3. Busca y selecciona el proyecto: **farmaciasolidaria-11ee6**

### Paso 2: Descargar nuevo google-services.json

1. En el panel izquierdo, haz clic en el **ícono de engranaje ⚙️** (junto a "Descripción general del proyecto")
2. Selecciona **"Configuración del proyecto"**
3. Ve a la pestaña **"General"**
4. Desplázate hacia abajo hasta la sección **"Tus apps"**
5. Busca tu app Android: `com.fsolidaria.app`
6. Haz clic en el botón **"google-services.json"** para descargarlo

   ```
   📱 com.fsolidaria.app (Android)
   
   [📥 google-services.json]
   ```

7. El archivo se descargará automáticamente con una **nueva API key**

### Paso 3: Reemplazar el archivo en tu proyecto

```bash
# Navegar a la carpeta del proyecto
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana

# Copiar el nuevo archivo descargado
cp ~/Downloads/google-services.json FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json

# Verificar que se copió correctamente
cat FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json | grep "current_key"
```

### Paso 4: Verificar el contenido

Abre el archivo y verifica que tenga una estructura similar a:

```json
{
  "project_info": {
    "project_number": "382870038786",
    "project_id": "farmaciasolidaria-11ee6",
    "storage_bucket": "farmaciasolidaria-11ee6.firebasestorage.app"
  },
  "client": [
    {
      "client_info": {
        "mobilesdk_app_id": "1:382870038786:android:...",
        "android_client_info": {
          "package_name": "com.fsolidaria.app"
        }
      },
      "api_key": [
        {
          "current_key": "AIzaSy...NUEVA_CLAVE_AQUI..."
        }
      ]
    }
  ]
}
```

---

## 🔧 MÉTODO 2: Desde Google Cloud Console (MÁS CONTROL)

### Paso 1: Acceder a Google Cloud Console

1. Ve a: **https://console.cloud.google.com/**
2. Inicia sesión con tu cuenta
3. En la parte superior, asegúrate de que esté seleccionado el proyecto: **farmaciasolidaria-11ee6**

   ```
   [🔽 farmaciasolidaria-11ee6]  ← Verifica que esté seleccionado aquí
   ```

### Paso 2: Ir a Credenciales

1. En el menú de la izquierda (☰), navega a:
   ```
   APIs y servicios → Credenciales
   ```
   
   O usa este enlace directo:
   **https://console.cloud.google.com/apis/credentials?project=farmaciasolidaria-11ee6**

### Paso 3: Localizar la API Key actual

En la sección **"Claves de API"**, verás algo como:

```
📋 Claves de API

Nombre                          Clave                                    Creada
──────────────────────────────────────────────────────────────────────────────
Android key (auto created...)   AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2N... 30/01/2026
```

### Paso 4A: OPCIÓN A - Revocar y crear nueva

#### 1. Revocar la clave antigua:
   - Haz clic en la API key antigua
   - En el panel que se abre, busca el botón **"Eliminar clave"** o **"Delete key"**
   - Confirma la eliminación
   - ⚠️ **IMPORTANTE**: Guarda la clave antigua antes de borrarla (ya la tienes en este documento)

#### 2. Crear nueva clave:
   - Haz clic en el botón **"+ CREAR CREDENCIALES"** en la parte superior
   - Selecciona **"Clave de API"**
   - Se generará automáticamente una nueva clave
   - **Aparecerá un popup con la nueva clave - CÓPIALA**

#### 3. Configurar restricciones (RECOMENDADO):
   - Haz clic en **"Editar clave de API"** o en el nombre de la clave recién creada
   - En **"Restricciones de la aplicación"**, selecciona:
     - **"Aplicaciones de Android"**
     - Agrega el nombre del paquete: `com.fsolidaria.app`
     - Agrega la huella digital SHA-1 de tu certificado (ver abajo cómo obtenerla)
   
   - En **"Restricciones de API"**, selecciona:
     - **"Restringir clave"**
     - Marca solo las APIs que necesitas:
       - ✅ Firebase Cloud Messaging API
       - ✅ Firebase Installations API
       - ✅ Token Service API
   
   - Haz clic en **"Guardar"**

### Paso 4B: OPCIÓN B - Regenerar la clave existente

Google Cloud no permite regenerar una clave directamente, pero puedes:
1. Crear una nueva clave (pasos arriba)
2. Configurar restricciones
3. Probar que funciona
4. Luego eliminar la antigua

---

## 🔐 Obtener Huella Digital SHA-1 (para restricciones)

### Para Keystore de Debug:

```bash
# Navegar al directorio del proyecto MAUI
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana.Maui

# Obtener SHA-1 del keystore de debug
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android | grep SHA1
```

### Para Keystore de Release (Producción):

```bash
# Si tienes un keystore personalizado para release
keytool -list -v -keystore /ruta/a/tu/keystore.jks -alias tu_alias

# Te pedirá la contraseña del keystore
# Busca la línea que dice SHA1: XX:XX:XX:...
```

---

## 🔄 Actualizar google-services.json manualmente

Si prefieres editar el archivo manualmente:

1. Abre: `FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json`

2. Encuentra la línea con `"current_key"`:
   ```json
   "api_key": [
     {
       "current_key": "ANTIGUA_CLAVE_AQUI"
     }
   ]
   ```

3. Reemplázala con la nueva clave:
   ```json
   "api_key": [
     {
       "current_key": "TU_NUEVA_CLAVE_AQUI"
     }
   ]
   ```

4. Guarda el archivo

---

## ✅ Verificar que todo funciona

### 1. Verificar el archivo

```bash
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana

# Ver la nueva API key
cat FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json | grep -A 2 "api_key"

# Debe mostrar algo como:
# "api_key": [
#   {
#     "current_key": "AIzaSy...LA_NUEVA_CLAVE..."
```

### 2. Limpiar builds anteriores

```bash
cd FarmaciaSolidariaCristiana.Maui

# Limpiar compilaciones anteriores
dotnet clean

# Eliminar carpetas de build
rm -rf bin/
rm -rf obj/
```

### 3. Compilar nuevo APK

```bash
# Compilar en modo Release
dotnet build -c Release -f net9.0-android

# Si necesitas generar el APK firmado
dotnet publish -c Release -f net9.0-android -p:AndroidPackageFormat=apk
```

### 4. Probar la app

1. Instala el nuevo APK en un dispositivo Android
2. Verifica en los logs que OneSignal se inicialice correctamente
3. Intenta registrarte para notificaciones
4. Envía una notificación de prueba desde OneSignal Dashboard

---

## 🚨 Solución de Problemas

### Problema: "No encuentro mi proyecto en Firebase Console"

**Solución:**
1. Verifica que estés usando la cuenta de Google correcta
2. Si el proyecto fue creado por otra cuenta, necesitas que te agreguen como colaborador
3. Ve a: https://console.firebase.google.com/ y verifica todos tus proyectos

### Problema: "No veo la app Android en Firebase"

**Solución:**
1. Ve a **Configuración del proyecto** → **General**
2. Desplázate hasta **Tus apps**
3. Si no está, haz clic en **"Agregar app"** → **Android**
4. Ingresa el package name: `com.fsolidaria.app`
5. Descarga el `google-services.json` que se generará

### Problema: "La nueva API key da error 'API key not valid'"

**Solución:**
1. Verifica que hayas habilitado las APIs necesarias:
   - Ve a **APIs y servicios** → **Biblioteca**
   - Busca: "Firebase Cloud Messaging API"
   - Haz clic en **Habilitar** si no está habilitada
2. Espera 5-10 minutos (puede tardar en propagarse)
3. Verifica las restricciones de la API key (paso 4A.3)

### Problema: "Build error: google-services.json is invalid"

**Solución:**
1. Verifica que el archivo tenga formato JSON válido
2. Usa un validador: https://jsonlint.com/
3. Compara con el template: `google-services.json.template`
4. Asegúrate que el package name coincida: `com.fsolidaria.app`

---

## 📝 Checklist Final

Después de regenerar la API key:

- [ ] ✅ Descargado nuevo `google-services.json` desde Firebase Console
- [ ] ✅ Reemplazado el archivo en `FarmaciaSolidariaCristiana.Maui/Platforms/Android/`
- [ ] ✅ Verificado que la nueva API key esté en el archivo
- [ ] ✅ Limpiado builds anteriores (`dotnet clean`)
- [ ] ✅ Compilado nuevo APK en modo Release
- [ ] ✅ Probado la app y verificado que OneSignal funcione
- [ ] ✅ (Opcional) Configurado restricciones en Google Cloud Console
- [ ] ✅ Archivo `google-services.json` está en `.gitignore`
- [ ] ✅ NO subir el nuevo archivo a GitHub

---

## 🔗 Enlaces Útiles

- **Firebase Console**: https://console.firebase.google.com/
- **Google Cloud Console**: https://console.cloud.google.com/
- **Tu Proyecto**: https://console.firebase.google.com/project/farmaciasolidaria-11ee6
- **Credenciales**: https://console.cloud.google.com/apis/credentials?project=farmaciasolidaria-11ee6
- **OneSignal Dashboard**: https://app.onesignal.com/

---

## 💡 Notas Adicionales

1. **No necesitas crear un nuevo proyecto en Firebase** - Solo descarga el archivo nuevo
2. **La API key se regenera automáticamente** cada vez que descargas `google-services.json`
3. **Puedes tener múltiples API keys activas** mientras pruebas la nueva antes de eliminar la antigua
4. **Las restricciones de API son opcionales** pero muy recomendadas para producción

¿Necesitas ayuda con algún paso específico? ¡Déjame saber en cuál te atascaste!
