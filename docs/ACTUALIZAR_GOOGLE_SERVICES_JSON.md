# Cómo Actualizar google-services.json con la Clave de Firebase

## 📋 Situación Actual

Has eliminado correctamente la API key expuesta. Ahora necesitas actualizar el archivo `google-services.json` con la clave que Firebase creó automáticamente.

## ✅ OPCIÓN 1: Volver a descargar el archivo (MÁS FÁCIL)

1. Ve a: https://console.firebase.google.com/project/farmaciasolidaria-11ee6/settings/general
2. Baja a **"Tus apps"**
3. En la app `com.fsolidaria.app`, haz clic en **"google-services.json"**
4. Descarga el archivo NUEVO (ahora tendrá la clave correcta)
5. Copia el archivo:
   ```bash
   cp ~/Downloads/google-services.json FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json
   ```

## ✅ OPCIÓN 2: Copiar la clave manualmente

### Paso 1: Obtener la API Key de Firebase

1. Ve a: https://console.cloud.google.com/apis/credentials?project=farmaciasolidaria-11ee6

2. En la lista de **"Claves de API"**, busca la que dice:
   ```
   Android key (auto created by Firebase)
   ```
   o similar.

3. Haz clic en el nombre de esa clave para ver sus detalles

4. **Copia el valor de la clave** (algo como: `AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`)

### Paso 2: Pegar en google-services.json

Edita el archivo:
```bash
code FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json
```

Busca la línea:
```json
"current_key": "AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8"
```

Reemplázala con:
```json
"current_key": "LA_CLAVE_QUE_COPIASTE_DE_FIREBASE"
```

### Paso 3: Verificar

```bash
cat FarmaciaSolidariaCristiana.Maui/Platforms/Android/google-services.json | grep "current_key"
```

Debe mostrar la nueva clave, NO la antigua (`AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`).

---

## 🔐 Sobre la Cuenta de Servicio (Service Account)

La cuenta que ves:
```
firebase-adminsdk-fbsvc@farmaciasolidaria-11ee6.iam.gserviceaccount.com
```

Es **diferente** y se usa para:
- **Backend/Servidor**: Para que tu API en ASP.NET se comunique con Firebase (si lo usas)
- **Enviar notificaciones desde el servidor**
- **Acceso administrativo a Firebase**

**NO la uses en la app móvil**. Las cuentas de servicio son para servidores, no para apps cliente.

### ¿Necesitas configurar Firebase Admin en el backend?

Si quieres enviar notificaciones desde tu backend ASP.NET a Firebase, necesitarías:

1. Generar una clave de cuenta de servicio (Service Account Key)
2. Descargar el archivo JSON con las credenciales
3. Usarlo en tu backend

Pero para la **app móvil** (MAUI), solo necesitas el `google-services.json` con la API key normal.

---

## 📊 Resumen de Claves en tu Proyecto

### Para la App MAUI Android:
- ✅ **Archivo**: `google-services.json`
- ✅ **Clave**: La "Android key (auto created by Firebase)"
- ✅ **Ubicación**: `FarmaciaSolidariaCristiana.Maui/Platforms/Android/`
- ❌ **NO usar**: Cuentas de servicio (service accounts)

### Para el Backend ASP.NET (si usas Firebase Admin):
- 📄 **Archivo**: JSON de cuenta de servicio (otro archivo diferente)
- 🔐 **Cuenta**: `firebase-adminsdk-fbsvc@farmaciasolidaria-11ee6.iam.gserviceaccount.com`
- 📍 **Ubicación**: En el servidor, nunca en GitHub
- ⚠️ **Nota**: Solo si necesitas acceso admin desde el servidor

---

## ✅ Checklist - ¿Qué hacer ahora?

- [ ] Volver a descargar `google-services.json` desde Firebase Console
  - O copiar manualmente la "Android key (auto created by Firebase)"
- [ ] Verificar que el archivo tenga la nueva clave
- [ ] La clave antigua (`AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`) NO debe aparecer
- [ ] Compilar nuevo APK con la clave actualizada
- [ ] Probar que OneSignal funcione correctamente

---

## ❓ Preguntas Frecuentes

**P: ¿Es segura la clave "auto created by Firebase"?**  
R: Sí, siempre que tenga restricciones configuradas (solo apps con tu package name y SHA-1).

**P: ¿Debería crear una clave nueva o usar la de Firebase?**  
R: **Usa la de Firebase** (auto created). Ya tiene las restricciones correctas.

**P: ¿La cuenta de servicio es peligrosa si se expone?**  
R: Sí, MUY peligrosa. Da acceso administrativo completo. Nunca la subas a GitHub ni la incluyas en la app móvil.

**P: ¿Uso OneSignal o Firebase para notificaciones?**  
R: OneSignal usa Firebase/FCM internamente en Android. El `google-services.json` es necesario para que OneSignal funcione.
