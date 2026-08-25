# Sistema de Notificaciones Híbrido: Push + Polling + Email

## 📋 Descripción General

El sistema de notificaciones de Farmacia Solidaria Cristiana combina tres canales de comunicación para garantizar que todos los usuarios reciban notificaciones independientemente de su ubicación geográfica o disponibilidad de Google Services:

1. **📧 Email** (SMTP) - Para usuarios inactivos o solicitudes críticas
2. **📱 Push Notifications** (OneSignal/FCM) - Para usuarios con Google Services
3. **🔄 Polling** (Consultas cada 30s) - Para usuarios en Cuba o como respaldo

## 🎯 Arquitectura del Sistema

### Flujo de Notificaciones

```
┌──────────────────────────────────────────────────────────────┐
│                    EVENTO DE TURNO                           │
│  (Solicitud, Aprobación, Rechazo, Cancelación, Expiración)  │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────┴────────────────┐
        │                                 │
        ▼                                 ▼
┌───────────────┐                 ┌───────────────┐
│ FARMACÉUTICOS │                 │   PACIENTES   │
│    + ADMINS   │                 │ (ViewerPublic)│
└───────┬───────┘                 └───────┬───────┘
        │                                 │
        ▼                                 ▼
┌─────────────────────────┐      ┌─────────────────────────┐
│ SIEMPRE envía:          │      │ Verifica actividad:     │
│ • Email a TODOS         │      │ • IsUserActiveOnMobile? │
│ • PendingNotification   │      │   (últimos 5 minutos)   │
│ • Intenta Push          │      └───────┬──────┬──────────┘
└─────────────────────────┘              │      │
                                         │      │
                                    ┌────▼──┐ ┌─▼────┐
                                    │  SÍ   │ │  NO  │
                                    └───┬───┘ └──┬───┘
                                        │        │
                                        ▼        ▼
                                   ┌─────────┐ ┌─────────┐
                                   │ Push +  │ │ Push +  │
                                   │ Polling │ │ Polling │
                                   │ (NO     │ │ + EMAIL │
                                   │ Email)  │ │         │
                                   └─────────┘ └─────────┘
```

## 🌐 Flujo en Plataforma Web (MVC)

### Cuando ViewerPublic solicita turno desde la web

**Archivo**: `TurnosController.cs` (líneas 314-349)

1. Usuario completa formulario web y envía solicitud
2. Sistema crea el turno en base de datos
3. **Email a Farmacéuticos/Admins** (línea 319):
   ```csharp
   await _emailService.SendTurnoNotificationToFarmaceuticosAsync(
       user?.UserName ?? "Usuario", 
       createdTurno.Id,
       tipoSolicitud);
   ```
   - Envía a **TODOS** los usuarios con rol Farmaceutico + Admin
   - No verifica si están en la app móvil
   - Email con enlace directo a revisar el turno

4. **Push/Polling a Farmacéuticos** (línea 333):
   ```csharp
   await _notificationService.SendNuevaSolicitudToFarmaceuticosAsync(
       createdTurno.Id,
       createdTurno.NumeroTurno ?? createdTurno.Id,
       user?.UserName ?? "Usuario");
   ```
   - Crea `PendingNotifications` para todos los farmacéuticos/admins
   - Intenta enviar Push a dispositivos con PlayerId activo
   - Si Push falla, las notificaciones quedan pendientes para Polling

## 📱 Flujo en App MAUI (API)

### Cuando ViewerPublic solicita turno desde la app

**Archivo**: `TurnosApiController.cs` (líneas 403-420)

1. Usuario envía solicitud desde la app móvil
2. Sistema crea el turno en base de datos
3. **Push/Polling a Farmacéuticos** (línea 407):
   ```csharp
   await _notificationService.SendNuevaSolicitudToFarmaceuticosAsync(
       turno.Id,
       turno.NumeroTurno ?? turno.Id,
       userName ?? "Usuario");
   ```
   - Crea `PendingNotifications` para polling
   - Intenta Push si hay dispositivos activos

4. **Email a Farmacéuticos/Admins** (línea 413):
   ```csharp
   await _emailService.SendTurnoNotificationToFarmaceuticosAsync(
       userName ?? "Usuario",
       turno.Id,
       tipoSolicitud);
   ```
   - Igual que en web, envía a TODOS los farmacéuticos/admins

**Resultado**: Ambos paths (web y MAUI) notifican igual a farmacéuticos.

## 🔔 Notificaciones a Pacientes

### Cuando Farmacéutico aprueba/rechaza turno

**Archivo**: `TurnoService.cs` (líneas 860-910)

1. **Verifica actividad en app móvil** (línea 880):
   ```csharp
   var isActiveOnMobile = await _notificationService.IsUserActiveOnMobileAsync(turno.UserId);
   ```
   - `IsUserActiveOnMobileAsync` → Verifica `LastActivityAt` en `UserDeviceTokens`
   - Si actividad < 5 minutos → Usuario está en la app

2. **Si está activo en app** (línea 882):
   ```csharp
   if (isActiveOnMobile)
   {
       _logger.LogInformation("Usuario está activo en la app móvil, no se envía email");
       turno.EmailEnviado = false;
   }
   ```
   - ✅ Crea `PendingNotification` (polling lo recogerá)
   - ✅ Intenta Push
   - ❌ **NO** envía email (para evitar spam)

3. **Si NO está activo en app** (línea 888):
   ```csharp
   else if (turno.User.Email != null)
   {
       await _emailService.SendTurnoAprobadoEmailAsync(
           turno.User.Email,
           turno.User.UserName ?? "Usuario",
           turno.NumeroTurno.Value,
           turno.FechaPreferida.Value,
           pdfPhysicalPath);
       turno.EmailEnviado = emailSent;
   }
   ```
   - ✅ Crea `PendingNotification`
   - ✅ Intenta Push
   - ✅ Envía Email con PDF adjunto

## 🔐 Sistema de Login y Persistencia de Sesión

### Auto-login tipo Facebook

**Duración del JWT**: 30 días (43,200 minutos)

**Archivo**: `appsettings.json` (línea 9)
```json
"ExpirationMinutes": 43200
```

**Al iniciar la app** (`AppShell.xaml.cs`, línea 64):
```csharp
private async void CheckAuthenticationAsync()
{
    var isAuthenticated = await _authService.IsAuthenticatedAsync();
    
    if (isAuthenticated)
    {
        await UpdateMenuForRoleAsync();
        await GoToAsync("//DashboardPage");  // ← Auto-login
    }
    else
    {
        await GoToAsync("//LoginPage");
    }
}
```

**Cómo funciona**:
1. Token guardado en `SecureStorage` (cifrado por el OS)
2. Al abrir la app, verifica si existe token
3. Si existe y no ha expirado (< 30 días) → Login automático
4. Si expiró → Muestra LoginPage

**Ventajas**:
- Usuario permanece logueado por 30 días
- Solo pide credenciales si:
  - Es instalación nueva
  - Se hizo logout manual
  - Pasaron más de 30 días sin usar la app

## 📲 Estrategia Push + Polling en MAUI

### Lógica Híbrida (LoginViewModel.cs, líneas 69-114)

**Estrategia actual**: Push-first + Polling-always

```csharp
// 1. Intentar registrar Push (con timeout)
bool pushWorking = false;
try
{
    await _notificationService.SetUserTagsAsync(user.Id, primaryRole);
    await _notificationService.RegisterDeviceAsync();
    
    var playerId = await _notificationService.GetPlayerIdAsync(maxRetries: 5, delayMs: 1000);
    
    if (!string.IsNullOrEmpty(playerId))
    {
        pushWorking = true;  // ✅ Push disponible
    }
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"[Login] ⚠️ Push falló: {ex.Message}");
}

// 2. SIEMPRE iniciar Polling (respaldo + heartbeat)
await _pollingService.StartAsync();

if (pushWorking)
{
    System.Diagnostics.Debug.WriteLine("[Login] ✅ Polling iniciado como respaldo (Push es primario)");
}
else
{
    System.Diagnostics.Debug.WriteLine("[Login] ✅ Polling iniciado como canal principal (Push no disponible)");
}
```

**Razones para siempre iniciar Polling**:
1. **Heartbeat**: Actualiza `LastActivityAt` cada 30s
   - Backend usa esto para decidir si enviar email o no
   - Mantiene al servidor informado de la actividad
2. **Respaldo**: Si Push falla (Cuba, problemas de red), garantiza notificaciones
3. **Auto-refresh**: Actualiza el CollectionView de turnos automáticamente

### PollingNotificationService.cs

**Sin push-awareness** - Siempre muestra notificaciones (línea 230):
```csharp
await ShowLocalNotificationAsync(notification);
```

- No verifica si Push ya entregó la notificación
- Asume que cada `PendingNotification` debe mostrarse
- Backend es responsable de crear PendingNotifications solo cuando necesario

## 📊 Matriz de Canales de Notificación

| Evento | Destinatario | Email | Push | Polling | Condición |
|--------|-------------|-------|------|---------|-----------|
| Nueva solicitud turno | Farmacéuticos/Admins | ✅ Siempre | ✅ Intenta | ✅ Siempre | - |
| Nueva solicitud turno | Paciente (confirmación) | ✅ Siempre | ❌ No | ❌ No | Solo confirmación |
| Turno aprobado | Paciente | ✅ Si inactivo | ✅ Intenta | ✅ Siempre | Verifica IsUserActiveOnMobile |
| Turno rechazado | Paciente | ✅ Si inactivo | ✅ Intenta | ✅ Siempre | Verifica IsUserActiveOnMobile |
| Turno cancelado por paciente | Farmacéuticos/Admins | ❌ No | ✅ Intenta | ✅ Siempre | Solo notificación in-app |
| Turno expirado (no presentación) | Paciente | ❌ No | ✅ Intenta | ✅ Siempre | Solo notificación in-app |
| Turno expirado (no presentación) | Farmacéuticos/Admins | ❌ No | ✅ Intenta | ✅ Siempre | Solo notificación in-app |

## 🔧 Archivos Clave del Sistema

### Backend (ASP.NET Core)

1. **TurnosController.cs** (Web MVC)
   - Líneas 118-370: `RequestForm` - Solicitud de turno desde web
   - Envía email + push/polling a farmacéuticos

2. **TurnosApiController.cs** (MAUI API)
   - Líneas 330-425: `CreateTurno` - Solicitud de turno desde MAUI
   - Envía push/polling + email a farmacéuticos

3. **TurnoService.cs**
   - Líneas 860-920: `ApproveTurnoAsync` - Lógica de aprobación
   - Líneas 960-1020: `RejectTurnoAsync` - Lógica de rechazo
   - Verifica `IsUserActiveOnMobileAsync` para decidir email

4. **OneSignalNotificationService.cs**
   - Líneas 534-640: `SendNuevaSolicitudToFarmaceuticosAsync`
   - Crea PendingNotifications + intenta Push
   - Línea 248: `IsUserActiveOnMobileAsync` verifica LastActivityAt

5. **EmailService.cs**
   - Líneas 359-506: `SendTurnoNotificationToFarmaceuticosAsync`
   - Envía a roles Farmaceutico + Admin
   - Incluye link directo al turno

### Frontend (MAUI)

1. **LoginViewModel.cs**
   - Líneas 46-120: Lógica de login
   - Líneas 69-114: Estrategia Push + Polling
   - Siempre inicia Polling para heartbeat

2. **PollingNotificationService.cs**
   - Línea 230: Siempre muestra notificaciones (sin push-awareness)
   - Intervalo: 30 segundos
   - Actualiza LastActivityAt en cada consulta

3. **TurnosPage.xaml.cs**
   - Líneas 32-45: Suscripción a NotificationReceived
   - Auto-refresh del CollectionView cuando llega notificación

4. **App.xaml.cs**
   - Líneas 119-135: OnResume handler
   - Verifica notificaciones pendientes al volver a primer plano

5. **AuthService.cs**
   - Líneas 48-62: Auto-refresh del JWT si expira
   - Guarda token en SecureStorage cifrado

## 🧪 Escenarios de Uso

### Escenario 1: Paciente en Cuba solicita turno desde la web

```
1. Usuario completa formulario en navegador
2. TurnosController.RequestForm procesa la solicitud
3. Farmacéuticos/Admins reciben:
   ✅ Email: "Nueva solicitud turno #123"
   ✅ PendingNotification (si usan app móvil)
   ✅ Push (si están fuera de Cuba con Google Services)
4. Paciente recibe:
   ✅ Email de confirmación: "Tu solicitud ha sido enviada"
```

### Escenario 2: Farmacéutico en España aprueba turno

```
1. Farmacéutico aprueba desde el panel web
2. Sistema verifica LastActivityAt del paciente
3. Si paciente usó la app en últimos 5 min:
   ✅ PendingNotification creada
   ✅ Push enviado (si tiene PlayerId)
   ❌ NO email (evita spam)
   → Polling de la app recogerá la notificación
4. Si paciente NO usó la app recientemente:
   ✅ PendingNotification creada
   ✅ Push enviado
   ✅ Email con PDF del turno adjunto
```

### Escenario 3: Usuario viaja de Cuba a España

```
Día 1 (Cuba):
- Login → OneSignal no obtiene PlayerId
- pushWorking = false
- Polling activo como canal principal
- Recibe notificaciones cada 30s

Día 5 (España):
- Usuario ya está logueado (JWT dura 30 días)
- No necesita re-login
- Polling sigue activo (heartbeat + respaldo)
- OneSignal ahora puede enviar Push
- Recibe notificaciones por ambos canales
- Nota: Para optimizar, podría cerrar sesión y re-loguearse
  para activar Push como primario
```

## ⚡ Ventajas del Sistema Actual

### 1. **Redundancia Triple**
- Email garantiza notificación aunque la app esté cerrada
- Push para notificaciones instantáneas
- Polling como respaldo universal

### 2. **Inteligencia en Email**
- No spam: Solo envía email a pacientes si NO están en la app
- Farmacéuticos SIEMPRE reciben email (es su trabajo)
- Heartbeat actualizado cada 30s para decisión precisa

### 3. **Paridad Web-MAUI**
- Ambos paths notifican igual a farmacéuticos
- Usuario no nota diferencia según plataforma usada

### 4. **Persistencia de Sesión**
- 30 días sin re-login mejora UX
- Token en SecureStorage cifrado (seguro)
- Compatible con estrategia de notificaciones continuas

### 5. **Auto-refresh UI**
- CollectionView se actualiza automáticamente
- OnResume verifica notificaciones pendientes
- Experiencia fluida sin intervención del usuario

## 🔍 Logs para Debugging

### Login exitoso con Push
```
[Login] ✅ Push registrado. PlayerId: abc123...
[Login] ✅ Polling iniciado como respaldo (Push es primario)
[App] Auto-login successful
```

### Login en Cuba (sin Push)
```
[Login] ⚠️ Push sin PlayerId
[Login] ✅ Polling iniciado como canal principal (Push no disponible)
[App] Auto-login successful
```

### Nueva solicitud de turno
```
[TurnosController] ✓ Notificaciones por email enviadas a farmacéuticos para turno 123
[TurnosController] ✓ Notificación push/polling enviada a 3 farmacéuticos para turno 123
```

### Aprobación de turno (paciente activo)
```
[TurnoService] Usuario abc123 está activo en la app móvil, no se envía email
[TurnoService] Notificación pendiente creada para usuario abc123
[TurnoService] Push notification enviada para aprobación de turno 123
```

### Aprobación de turno (paciente inactivo)
```
[TurnoService] Usuario abc123 no está activo en la app, enviando email a user@example.com
[EmailService] Email de turno aprobado enviado a user@example.com
```

## 📚 Configuración

### Backend (appsettings.json)

```json
{
  "JwtSettings": {
    "ExpirationMinutes": 43200  // 30 días
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    // ... otras configuraciones
  },
  "OneSignalSettings": {
    "AppId": "4d981851-f1a2-4112-8a08-08500e48f196",
    // ... otras configuraciones
  }
}
```

### App MAUI (Constants.cs)

```csharp
public const string OneSignalAppId = "4d981851-f1a2-4112-8a08-08500e48f196";
public const int PollingIntervalSeconds = 30;
public const int UserActiveTimeoutMinutes = 5;  // Para IsUserActiveOnMobileAsync
```

## ✅ Checklist de Verificación

### Después de desplegar

- [x] JWT configurado a 30 días en producción
- [x] Email llegando a farmacéuticos cuando se solicita turno (web)
- [x] Email llegando a farmacéuticos cuando se solicita turno (MAUI)
- [x] Email llegando a admins en ambos casos
- [ ] Push funcionando fuera de Cuba (OneSignal/FCM)
- [x] Polling funcionando en Cuba (y ahora con lógica push-first)
- [x] Canal SignalR sobre 443 implementado (activar con `SignalRChannelEnabled=true`)
- [x] Auto-login arranca notificaciones (fix Fase 1.4)
- [ ] Email NO enviándose a pacientes activos en app
- [ ] Email SÍ enviándose a pacientes inactivos

### Pruebas recomendadas

1. **Web → Farmacéutico**: Solicitar turno desde web, verificar email
2. **MAUI → Farmacéutico**: Solicitar turno desde app, verificar email
3. **Aprobar con paciente activo**: Verificar que NO envía email
4. **Aprobar con paciente inactivo**: Verificar que SÍ envía email
5. **Auto-login**: Cerrar app, esperar 1 min, reabrir → debe auto-loguearse
6. **Auto-login expirado**: Esperar 31 días, reabrir → debe pedir login

---

# 🚀 Plan de mejora: canal 443 + push-first + Foreground Service

> **Motivación**: el 99.9 % de los usuarios están en Cuba, donde FCM/OneSignal no entrega
> (ETECSA filtra los puertos 5228/5229/5230 que usa FCM). Solo el polling funcionaba.
> La idea del documento `notificaciones-push-cuba.md` se traduce así: **no forzar a Google a
> usar 443 (no controlamos mtalk.google.com), sino usar nuestro propio servidor extranjero
> sobre 443, que Cuba ya alcanza (lo prueba que el polling funciona)**.

## Estado deseado (confirmado)
- **Push funciona (cualquier canal instantáneo: OneSignal o SignalR)** → el polling baja a
  **modo solo-heartbeat** (cada 60 s, solo `POST /heartbeat`, sin `GET /pending`).
- **Push falla** → el polling sube a **modo completo** (30 s, `GET /pending` + mostrar + heartbeat).
- **Transición** automática vía `IPushHealthService`, con de-duplicación (watermark) para no repetir.
- El **heartbeat siempre corre** (opción a) para que `IsUserActiveOnMobileAsync` (5 min) siga
  alimentando la decisión de email (no spam a pacientes activos en la app).
- **Entrega en background (push real)** para Cuba vía **Foreground Service** Android que mantiene
  viva la conexión SignalR y lanza notificaciones del sistema (estilo Telegram).

## Fases implementadas

### ✅ Fase 0 — Diagnóstico (MAUI)
- Logging estructurado en `App.xaml.cs` (`ReportOneSignalAvailabilityToHealthService`) y
  `NotificationService.cs`: estado de suscripción, permiso, PlayerId y disponibilidad de canal.
- Backend: logs existentes en `OneSignalNotificationService` registran envíos push.
- Confirma si la causa es suscripción vs entrega (con GMS presente → apunta a puerto 5228 bloqueado).

### ✅ Fase 1 — Lógica push-first (MAUI)
- **`IPushHealthService` / `PushHealthService`** (`Services/PushHealthService.cs`):
  `IsInstantChannelAvailable` (OneSignal ∨ SignalR), `ReportDelivery`/`WasDeliveredInstantly`
  (watermark de dedup), `AvailabilityChanged` event, `Reset()` en logout.
- **Polling consciente** (`PollingNotificationService.cs`): modo solo-heartbeat cuando hay canal
  instantáneo; `CheckNowAsync` respeta el flag; de-dup frente a entregas instantáneas.
- **Fix auto-login** (`AppShell.CheckAuthenticationAsync`): arranca push+polling en el path de
  token guardado (cold-start) — antes un usuario que volvía por token **no recibía notificaciones**.
- **Fix logout** (`AppShell.OnLogoutClicked`): `StopAsync` + `UnregisterUserAsync` + `Reset()`.

### ✅ Fase 2 — Canal SignalR sobre 443 + Foreground Service (push real para Cuba)
- **Backend**: `Hubs/NotificationsHub.cs` (`[Authorize]` JWT, grupo `user:{userId}`),
  `Services/INotificationBroadcaster.cs` + `SignalRNotificationBroadcaster.cs`,
  `AddSignalR()` + `MapHub<NotificationsHub>("/hubs/notifications")` en `Program.cs`.
  Integrado en `PendingNotificationService.CreateNotificationAsync` (difunde al crear).
  Si el host no soporta WebSocket, el cliente cae automáticamente a SSE/long-polling sobre 443.
- **MAUI cliente**: NuGet `Microsoft.AspNetCore.SignalR.Client`, `Services/NotificationsHubClient.cs`
  con `WithAutomaticReconnect` (backoff 0→2→5→10→20→30→60 s), reporta estado a `PushHealthService`,
  de-dup, y dispara `NotificationReceived` + notificación del sistema.
- **Foreground Service** (`Platforms/Android/NotificationsForegroundService.cs`): aloja el hub client,
  notificación persistente, `START_STICKY`, `OnBind` null. Arrancado por
  `NotificationsForegroundStarter` desde login y auto-login; detenido en logout.
- **Notificaciones del sistema** (`Platforms/Android/Services/SystemNotificationService.cs`):
  canal `fsc_notifications`, icono `ic_notification`, sonido `notfar.mp3` (in-process), acción "Ver"
  → `App.PendingRoute` → navegación a `//TurnosPage`.
- **Ciclo de vida Android**: `MainActivity` lee `route` extra y pide exención de optimización de
  batería (una sola vez). Permisos en `AndroidManifest.xml`: `FOREGROUND_SERVICE`,
  `FOREGROUND_SERVICE_DATA_SYNC`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`.
- **Integración UI** (`TurnosPage.xaml.cs`): suscrito a `INotificationsHubClient.NotificationReceived`.

### ✅ Fase 3 — Feature flags + verificación
Flags en `Helpers/Constants.cs` (rollback seguro):
- `EnablePushAwarePolling = true` (false = polling siempre, comportamiento anterior).
- `SignalRChannelEnabled = false` ← **desactivado hasta probar en dispositivo Cuba; al pasarlo a `true`
  se activa el Foreground Service y el push real sobre 443.**
- `HeartbeatIntervalSeconds = 60`.

**Matriz de verificación** (al activar `SignalRChannelEnabled = true`):
1. Cuba, app cerrada → llega notificación del sistema (sonido + "Ver") vía SignalR 443.
2. Cuba, app abierta → refresco del CollectionView sin duplicar (dedup).
3. Push fuera de Cuba (OneSignal) → polling en solo-heartbeat, sin spam.
4. Auto-login → arrancan polling + foreground service.
5. Logout → se detiene polling + foreground service + reset de salud.
6. SignalR caído → polling escala a completo; al reconectar → vuelve a solo-heartbeat.

## 🔲 Pendientes / mejoras futuras
- **WorkManager** de respaldo para revivir el Foreground Service si OEM agresivos (Xiaomi/Huawei)
  lo matan (hoy se cubre con `START_STICKY` + foreground notification + exención de batería).
- Wiring del evento foreground de OneSignal (`OneSignal.Notifications.ReceivedInForeground`) para
  reportar entregas reales de FCM a `PushHealthService` (cuando se confirme la API del SDK 5.2.2).
- Sonido `notfar.mp3` como sonido del canal del sistema (hoy se reproduce in-process; queda como
  canal default en la barra).
- Verificar explícitamente si Somee (plan de pago) soporta WebSocket; si no, SignalR ya cae a SSE.

## 📌 Futuro: opción (b) — mover `LastActivityAt` a webhook de OneSignal
Hoy (opción a) se mantiene un loop mínimo de heartbeat (60 s) cuando el push funciona, para
alimentar `LastActivityAt` y la lógica de "no email a pacientes activos". Evolución limpia:
- Configurar **OneSignal delivery webhook** → endpoint en el backend que, al confirmar entrega push
  a un dispositivo, actualice `UserDeviceToken.LastActivityAt`.
- Así se puede **eliminar el heartbeat** y el loop de polling queda 100 % apagado cuando hay canal
  instantáneo (estado deseado puro, sin tráfico residual).
- Requiere añadir endpoint `POST /api/notifications/onesignal-webhook` y registrarlo en OneSignal.

---

**Última actualización**: 25 de agosto de 2026  
**Versión del sistema**: Post-despliegue + Fase 0/1 + Fase 2 (SignalR gated, `SignalRChannelEnabled=false`)
