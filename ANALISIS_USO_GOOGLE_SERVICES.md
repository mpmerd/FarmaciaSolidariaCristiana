# Análisis: Uso de google-services.json en Producción

## 📊 Resumen Ejecutivo

**Conclusión**: La API key de `google-services.json` **SÍ se está usando en producción** en la app MAUI de Android porque OneSignal la requiere para funcionar.

## 🔍 Detalles Técnicos

### 1. Configuración actual

#### OneSignal (Push Notifications)
- ✅ **Configurado y activo**: OneSignal se inicializa al arrancar la app
- ✅ **Ubicación**: [App.xaml.cs](FarmaciaSolidariaCristiana.Maui/App.xaml.cs#L38-L86)
- ✅ **OneSignal App ID**: `4d981851-f1a2-4112-8a08-08500e48f196`

```csharp
// En App.xaml.cs línea 48
OneSignal.Initialize(Constants.OneSignalAppId);
```

#### Firebase Cloud Messaging (FCM)
- ✅ **Configurado**: `google-services.json` está incluido en el proyecto
- ✅ **Ubicación**: [FarmaciaSolidariaCristiana.Maui.csproj](FarmaciaSolidariaCristiana.Maui/FarmaciaSolidariaCristiana.Maui.csproj#L64)
- ⚠️ **API Key expuesta**: `AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`

```xml
<!-- En el .csproj -->
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
    <GoogleServicesJson Include="Platforms\Android\google-services.json" />
</ItemGroup>
```

#### Sistema de Polling (Alternativa)
- ✅ **Configurado y activo**: Se inicia después del login
- ✅ **Ubicación**: [PollingNotificationService.cs](FarmaciaSolidariaCristiana.Maui/Services/PollingNotificationService.cs)
- ✅ **Intervalo**: Cada 30 segundos
- ✅ **Inicio**: [LoginViewModel.cs línea 78](FarmaciaSolidariaCristiana.Maui/ViewModels/LoginViewModel.cs#L78)

```csharp
// Después de login exitoso
await _pollingService.StartAsync();
```

### 2. ¿Por qué OneSignal necesita Firebase/FCM?

**En Android, OneSignal requiere Firebase Cloud Messaging (FCM) para las notificaciones push.**

OneSignal es una **capa de abstracción** sobre los servicios de notificaciones nativas:
- **Android**: Usa FCM (Firebase Cloud Messaging) → Requiere `google-services.json`
- **iOS**: Usa APNs (Apple Push Notification service) → Requiere certificados de Apple

Sin `google-services.json`, OneSignal **no puede funcionar** en Android porque no puede:
1. Registrar el dispositivo con FCM
2. Obtener un token de push
3. Recibir notificaciones push

### 3. Estrategia Dual (Push + Polling)

Tu app implementa **dos sistemas en paralelo**:

#### Sistema Push (OneSignal + FCM)
- ✅ **Funciona en**: Países con acceso a Google Services
- ❌ **Bloqueado en**: Cuba (Google Services no accesible)
- 📱 **Ventaja**: Notificaciones instantáneas con menor consumo de batería

#### Sistema Polling
- ✅ **Funciona en**: Todos los países (incluida Cuba)
- ⚠️ **Desventaja**: Mayor consumo de batería y ancho de banda
- 📱 **Intervalo**: Consulta al servidor cada 30 segundos

### 4. Funcionamiento en Cuba

Aunque OneSignal esté configurado:
- ❌ Firebase/FCM está **bloqueado** en Cuba
- ❌ OneSignal **no puede registrar el dispositivo** con FCM
- ✅ El **sistema de polling funciona** como respaldo
- ✅ Los usuarios en Cuba reciben notificaciones vía polling

## ⚠️ IMPLICACIONES DE SEGURIDAD

### La API Key expuesta SÍ es un problema porque:

1. **Se usa en producción**: Aunque FCM/OneSignal esté bloqueado en Cuba, la app intenta usarlo
2. **Usuarios fuera de Cuba**: Si alguien usa la app fuera de Cuba, OneSignal + FCM funcionan
3. **Compilación de APK**: La API key se embebe en cada APK compilado
4. **Potencial abuso**: Alguien con la API key podría:
   - Enviar notificaciones falsas a los usuarios
   - Abusar de la cuota de Firebase
   - Acceder a estadísticas del proyecto

## ✅ RECOMENDACIONES

### 1. URGENTE: Revocar y Regenerar API Key
- [ ] Ir a [Google Cloud Console](https://console.cloud.google.com/)
- [ ] Revocar: `AIzaSyBffyQ_SVKVvwtyoijBw7DNU1eSU2NYyO8`
- [ ] Generar nueva API key
- [ ] Descargar nuevo `google-services.json` desde Firebase Console
- [ ] Actualizar archivo local

### 2. Compilar nuevo APK
Después de actualizar el `google-services.json`:
```bash
# Limpiar builds anteriores
cd FarmaciaSolidariaCristiana.Maui
dotnet clean

# Compilar nuevo APK con nueva API key
dotnet build -c Release -f net9.0-android

# Publicar/distribuir el nuevo APK
```

### 3. Considerar arquitectura alternativa (Futuro)

Para Cuba y otros países con restricciones:

#### Opción A: APK dual
- **APK Internacional**: Con OneSignal + FCM
- **APK Cuba**: Solo con Polling (sin Google Services)

#### Opción B: Detección automática
```csharp
// Detectar si Google Services está disponible
var isGoogleServicesAvailable = await CheckGoogleServicesAsync();

if (isGoogleServicesAvailable)
{
    // Usar OneSignal + FCM
    OneSignal.Initialize(Constants.OneSignalAppId);
}
else
{
    // Solo usar Polling
    await _pollingService.StartAsync();
}
```

#### Opción C: Migrar a alternativa sin Google
- Usar servicios de notificaciones que no dependan de FCM
- Mantener solo el sistema de polling universal

## 📝 Conclusión

**SÍ necesitas revocar y regenerar la API key** porque:
1. ✅ Está siendo usada en producción (aunque sea limitadamente)
2. ✅ Está embebida en todos los APKs distribuidos
3. ✅ Representa un riesgo de seguridad real
4. ✅ Es una buena práctica de seguridad

El sistema de polling es **complementario**, no reemplaza completamente a OneSignal/FCM para usuarios fuera de Cuba.
