# Sistema de Notificaciones Híbrido: SignalR + OneSignal + Polling + Email

## 📋 Descripción General

El sistema de notificaciones de Farmacia Solidaria Cristiana combina **cuatro** canales para garantizar que todos los usuarios reciban notificaciones independientemente de su ubicación geográfica:

1. **📧 Email** (SMTP) - Para usuarios inactivos o solicitudes críticas
2. **📱 OneSignal (FCM)** - Push nativo para usuarios **fuera de Cuba** (OneSignal bloquea Cuba por IP)
3. **🔌 SignalR sobre 443** - Push real para **Cuba y fuera** (canal propio al servidor extranjero)
4. **🔄 Polling** (Consultas cada 30s) - Respaldo universal cuando ningún canal instantáneo está disponible

> **Hecho clave (confirmado en pruebas agosto 2026)**: OneSignal **bloquea Cuba por IP**
> (`Access denied... from a country we do not support`). No es ETECSA filtrando puertos —
> es OneSignal bloqueando el registro/distribución desde IPs cubanas.
> Por eso se construyó el canal SignalR sobre 443: es el "push real" que funciona en Cuba
> (donde está el 99.9 % de los usuarios) y también fuera de Cuba (como redundancia).

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
3. **Email a Farmacéuticos/Admins — ELIMINADO**:
   > Antes se enviaba email a todos los farmacéuticos+admins por cada nueva solicitud.
   > **Eliminado**: ahora SignalR (push real 443) + OneSignal (fuera de Cuba) + polling
   > entregan la notificación en tiempo real. El email era redundante.

4. **Push/SignalR/Polling a Farmacéuticos** (línea 336):
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
3. **Push/SignalR/Polling a Farmacéuticos** (línea 465):
   ```csharp
   await _notificationService.SendNuevaSolicitudToFarmaceuticosAsync(
       turno.Id,
       turno.NumeroTurno ?? turno.Id,
       userName ?? "Usuario");
   ```
   - Crea `PendingNotifications` para polling
   - Difunde por SignalR (entrega instantánea) + intenta OneSignal push

4. **Email a Farmacéuticos/Admins — ELIMINADO**:
   > Antes se enviaba email a farmacéuticos inactivos. **Eliminado**: redundante con SignalR/push.
   > (El email a PACIENTES por aprobación/rechazo sí se mantiene — es un evento distinto.)

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

## 📲 Estrategia Push-first + Polling (estado actual)

### Lógica push-first (implementada)

**Estado deseado**: si hay un canal instantáneo disponible (OneSignal **o** SignalR), el polling baja a **modo solo-heartbeat** (cada 60s, solo `POST /heartbeat`, sin `GET /pending`). Si no hay canal instantáneo, el polling sube a **modo completo** (30s, `GET /pending` + mostrar + heartbeat).

El `IPushHealthService` (`Services/PushHealthService.cs`) centraliza la señal de salud:
- `IsInstantChannelAvailable` = OneSignal disponible (playerId + permiso) **∨** SignalR conectado.
- `ReportDelivery(notificationId, createdAt)` / `WasDeliveredInstantly(notificationId)` → watermark de de-duplicación entre canales.
- `AvailabilityChanged` event → el polling reacciona dinámicamente (cambia de modo sin reiniciar).
- `Reset()` en logout.

### Login (LoginViewModel.cs, líneas 69-120)

```csharp
// 1. Intentar registrar OneSignal Push
bool pushWorking = false;
await _notificationService.SetUserTagsAsync(user.Id, primaryRole);
await _notificationService.RegisterDeviceAsync();
var playerId = await _notificationService.GetPlayerIdAsync(maxRetries: 5, delayMs: 1000);
if (!string.IsNullOrEmpty(playerId)) pushWorking = true;

// 2. SIEMPRE iniciar Polling (heartbeat + respaldo)
await _pollingService.StartAsync();  // se autoajusta: solo-heartbeat si hay canal instantáneo

// 3. Arrancar Foreground Service (SignalR en background) si SignalRChannelEnabled
if (Constants.SignalRChannelEnabled)
    Platforms.Android.NotificationsForegroundStarter.Start();
```

### PollingNotificationService.cs (push-aware)

Ya **no es** "sin push-awareness". El loop ahora:
```csharp
var instantAvailable = Constants.EnablePushAwarePolling && _pushHealth.IsInstantChannelAvailable;
if (!instantAvailable)
    await PollForNotificationsAsync();   // solo si NO hay canal instantáneo
await SendHeartbeatAsync();              // SIEMPRE (alimenta LastActivityAt)
var delay = instantAvailable ? Constants.HeartbeatIntervalSeconds : PollingIntervalSeconds;
await Task.Delay(delay, token);
```
- `CheckNowAsync` respeta el flag (skip poll si canal instantáneo disponible).
- De-dup: salta notificaciones ya entregadas por SignalR/OneSignal (`WasDeliveredInstantly`).

### Heartbeat (opción a) — por qué siempre corre
El loop mínimo de heartbeat (60s) alimenta `LastActivityAt` en el backend, que `IsUserActiveOnMobileAsync` (5 min) usa para decidir **no enviar email** a pacientes activos en la app. Si se apagara el loop por completo cuando hay push, el backend creería que el paciente está inactivo → le llovería email spam aunque reciba push. (Ver "opción b" futura más abajo para eliminar el heartbeat.)

## 📊 Matriz de Canales de Notificación

| Evento | Destinatario | Email | Push | Polling | Condición |
|--------|-------------|-------|------|---------|-----------|
| Nueva solicitud turno | Farmacéuticos/Admins | ❌ Eliminado | ✅ SignalR+OneSignal | ✅ Siempre (fallback) | El email era redundante con SignalR/push |
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
   - Líneas 46-130: Lógica de login + estrategia push-first + arranque foreground service
   - Siempre inicia Polling (se autoajusta a solo-heartbeat si hay canal instantáneo)

2. **PollingNotificationService.cs** (push-aware)
   - Loop push-aware: solo-heartbeat (60s) si canal instantáneo disponible; completo (30s) si no
   - `CheckNowAsync` respeta el flag; de-dup vs SignalR/OneSignal
   - Intervalo: 30s (completo) / 60s (heartbeat-only)

3. **NotificationsHubClient.cs** (SignalR client)
   - `WithAutomaticReconnect()` infinito, `ServerTimeout=3min`
   - Recibe `ReceiveNotification`, reporta a `PushHealthService`, muestra notificación del sistema
   - De-dup: skip si `WasDeliveredInstantly`

4. **PushHealthService.cs**
   - `IsInstantChannelAvailable` (OneSignal ∨ SignalR), dedup watermark, `AvailabilityChanged`

5. **TurnosPage.xaml.cs**
   - Suscrito a `IPollingNotificationService.NotificationReceived` Y `INotificationsHubClient.NotificationReceived`
   - Auto-refresh del CollectionView cuando llega notificación

6. **App.xaml.cs**
   - `App.Services` (IServiceProvider estático para el Foreground Service)
   - `App.PendingRoute` (navegación desde notificación del sistema)
   - `ReportOneSignalAvailabilityToHealthService` en cambio de suscripción
   - OnResume: verifica notificaciones + navega a PendingRoute

7. **AppShell.xaml.cs**
   - `CheckAuthenticationAsync`: auto-login arranca push+polling+foreground (Fase 1.4)
   - `OnLogoutClicked`: detiene polling+foreground+reset (Fase 1.5)

8. **AuthService.cs**
   - Auto-refresh del JWT si expira; token en SecureStorage cifrado

### Platform (Android)

1. **NotificationsForegroundService.cs** — FG service (TypeDataSync, START_STICKY, aloja hub client)
2. **NotificationsForegroundStarter.cs** — Start/Stop estático del FG service
3. **SystemNotificationService.cs** — Notificaciones del sistema (canal, sonido, acción "Ver")
4. **MainActivity.cs** — Lee `route` extra; pide exención de batería (una vez)
5. **AndroidManifest.xml** — Permisos FG service + batería

## 🧪 Escenarios de Uso

### Escenario 1: Paciente en Cuba solicita turno desde la web

```
1. Usuario completa formulario en navegador
2. TurnosController.RequestForm procesa la solicitud
3. Farmacéuticos/Admins reciben:
   ✅ SignalR (push real 443, si están en la app — background incluido)
   ✅ OneSignal push (si están fuera de Cuba)
   ✅ PendingNotification (polling lo recoge como fallback)
   ❌ Email a farmacéuticos ELIMINADO (redundante con SignalR/push)
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

### Escenario 3: Usuario viaja de Cuba a España (o red Starlink fuera de Cuba)

```
En Cuba (IP cubana):
- OneSignal: BLOQUEADO por IP (Access denied) → no registra playerId
- SignalR: conecta sobre 443 (push real en background via Foreground Service)
- Polling: en modo solo-heartbeat (SignalR disponible)
- Recibe notificaciones instantáneas por SignalR + email si está inactivo

Fuera de Cuba (IP no cubana, ej. Starlink USA):
- OneSignal: FUNCIONA (API 200, suscripción con token, push entrega real) ✅
- SignalR: también conecta (redundancia)
- Polling: en modo solo-heartbeat
- Recibe por OneSignal (push nativo) + SignalR (redundante)
```

> ⚠️ **Cuidado con suscripciones OneSignal corruptas**: si un dispositivo estuvo en
> Cuba y OneSignal no pudo registrar, la suscripción puede quedar `notification_types: -8`
> (opt-out, sin token). Al salir de Cuba, OneSignal **no se autorregenera** — hay que
> **borrar datos de la app** para forzar una suscripción fresca. Si el JWT también
> expiró, borrar datos arregla ambos (login limpio + suscripción nueva).

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
- [x] OneSignal funcionando FUERA de Cuba (confirmado: push directo al S25 entregado)
- [x] OneSignal BLOQUEADO en Cuba por IP (confirmado: `Access denied... country we do not support`)
- [x] Polling funcionando en Cuba (y con lógica push-first: solo-heartbeat si hay canal instantáneo)
- [x] Canal SignalR sobre 443 implementado y VALIDADO en Cuba (background) y fuera de Cuba
- [x] Auto-login arranca notificaciones (fix Fase 1.4)
- [x] Push real en background para Cuba (Foreground Service + SignalR)
- [ ] Email NO enviándose a pacientes activos en app (verificar en producción)
- [ ] Email SÍ enviándose a pacientes inactivos (verificar en producción)

### Pruebas recomendadas

1. **Web → Farmacéutico**: Solicitar turno desde web, verificar email
2. **MAUI → Farmacéutico**: Solicitar turno desde app, verificar email
3. **Aprobar con paciente activo**: Verificar que NO envía email
4. **Aprobar con paciente inactivo**: Verificar que SÍ envía email
5. **Auto-login**: Cerrar app, esperar 1 min, reabrir → debe auto-loguearse
6. **Auto-login expirado**: Esperar 31 días, reabrir → debe pedir login

---

# 🚀 Plan de mejora: canal 443 + push-first + Foreground Service

> **Motivación**: el 99.9 % de los usuarios están en Cuba, donde OneSignal/FCM no entrega.
> **Causa raíz confirmada en pruebas (25 ago 2026)**: OneSignal **bloquea Cuba por IP** —
> el log muestra `Access denied... from a country we do support` con la IP cubana del cliente.
> No es ETECSA filtrando el puerto 5228: es OneSignal bloqueando el registro/distribución
> desde IPs cubanas. Por eso OneSignal **nunca** funcionará en Cuba.
> La idea del documento `notificaciones-push-cuba.md` se traduce así: **no depender de
> Google/OneSignal, sino usar nuestro propio servidor extranjero sobre 443, que Cuba ya
> alcanza (lo prueba que el polling funciona)**. Ese canal es SignalR.

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
- **Causa raíz confirmada en pruebas**: OneSignal bloquea Cuba por IP (ver "Estado de pruebas").

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
- **Catch-up server-side**: `OnConnectedAsync` envía al cliente las pendientes no leídas al
  conectar/reconectar → red de seguridad para notificaciones perdidas en huecos de desconexión.
  (El cliente las de-dup con `WasDeliveredInstantly` para no repetir.)
- **MAUI cliente**: NuGet `Microsoft.AspNetCore.SignalR.Client`, `Services/NotificationsHubClient.cs`
  con `WithAutomaticReconnect()` (retry infinito con backoff 0→2→10→30s), `ServerTimeout=3min`
  (Somee no envía keep-alives; el default 30s provocaba desconexiones cada 30s), reporta estado
  a `PushHealthService`, de-dup, y dispara `NotificationReceived` + notificación del sistema.
- **Foreground Service** (`Platforms/Android/NotificationsForegroundService.cs`): aloja el hub client,
  notificación persistente, `START_STICKY`, `OnBind` null. Arrancado por
  `NotificationsForegroundStarter` desde login y auto-login; detenido en logout.
  **Tipo de servicio**: `ForegroundServiceType.TypeDataSync` (valor enum `TypeDataSync=1`).
  ⚠️ No usar el literal `4` — en el enum de .NET Android `4` = `TypePhoneCall` (requería permiso
  `FOREGROUND_SERVICE_PHONE_CALL` y fallaba silenciosamente el `startForeground`).
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
- `SignalRChannelEnabled = true` ← **activado tras validar en dispositivo/emulador Cuba**.
- `HeartbeatIntervalSeconds = 60`.

## ✅ Estado de pruebas (25-26 ago 2026) — push real VALIDADO en Cuba y fuera

### Pruebas en Cuba (emulador + S25, IP cubana)
- **Causa raíz confirmada**: OneSignal bloquea Cuba por IP. Log: `Access denied... from a
  country we do not support` con IP cubana del cliente. OneSignal **nunca** funcionará en Cuba.
- **Bug crítico encontrado y arreglado**: el tipo de Foreground Service usaba el literal `4` que
  en el enum .NET es `TypePhoneCall` → `startForeground` fallaba silenciosamente → el servicio
  no era foreground de verdad → en background Android mataba el proceso y SignalR se perdía.
  Fix: `ForegroundServiceType.TypeDataSync` (valor `1`). Tras el fix, `isForeground=true`.
- **Entrega en background VALIDADA en Cuba**: paciente real solicitó turno, el farmacéutico con
  la app en background (emulador saliendo por IP cubana) recibió la notificación del sistema con
  sonido `notfar.mp3`. Log: `[HubClient] Recibida notificación #21974` vía SignalR.
- **Timeout de Somee**: Somee no envía keep-alives de SignalR → el cliente (default 30s)
  desconectaba cada 30s. Fix: `ServerTimeout=3min` + `WithAutomaticReconnect()` infinito →
  huecos de desconexión mucho menores; el catch-up cubre los restantes.

### Pruebas fuera de Cuba (emulador + S25, red Starlink USA)
- **OneSignal fuera de Cuba: FUNCIONA**. API REST 200, suscripción `enabled=true` con token.
  Enviado push directo via API REST al S25 → **llegó** (push nativo OneSignal entregado a
  dispositivo físico real). Cuenta OneSignal **activa y funcional fuera de Cuba** ✅.
- **SignalR fuera de Cuba: también conecta** (redundancia). Entrega validada via `[HubClient]`.
- **S25 no recibió push antes de borrar datos**: la suscripción OneSignal estaba
  `notification_types: -8` (opt-out, sin token) por las sesiones en Cuba + JWT probablemente
  expirado. Al borrar datos → suscripción fresca + login limpio → todo funcionó.
- **Emulador NO sirve para probar entrega OneSignal**: los tokens FCM del emulador son
  efímeros (cada sesión genera tokens que mueren); OneSignal los guarda como "enabled" pero
  el token está muerto → la entrega FCM falla silenciosamente. Para OneSignal, **siempre
  probar en dispositivo físico real**.

## 🧭 Guía para futuras sesiones

### Cómo levantar el entorno de pruebas
- **Emulador**: `~/Library/Android/sdk/emulator/emulator -avd Pixel_5_API_34` (arm64, API 34).
  Útil para probar SignalR/foreground service (ver logs via `adb logcat`).
  **No sirve para OneSignal** (tokens FCM efímeros).
- **Para apuntar el emulador a producción** (temporal): cambiar `Constants.cs` DEBUG
  `ApiBaseUrl` a `https://farmaciasolidaria.somee.com`, compilar, instalar. **REVERTIR** al
  terminar (volver a `http://192.168.2.104:5003`).
- **Logs en release no visibles**: compilar Debug para el emulador para ver `Debug.WriteLine`
  (salen en logcat con tag `DOTNET` o `com.fsolidaria.app`).
- **logcat útil**: `adb logcat -v time | grep -iE "HubClient|PushHealth|FgService|PollingService|SysNotif|OneSignal"`

### Estado de despliegue
- **App MAUI**: `SignalRChannelEnabled=true` en `Constants.cs` (activado). Compilar release
  para distribuir a dispositivos físicos.
- **Backend Somee**: tiene el hub SignalR + broadcaster + `PendingNotificationService` con
  broadcast. **El catch-up de `OnConnectedAsync` está commiteado — verificar que esté
  desplegado** (si no, las notificaciones perdidas en huecos de desconexión no se recuperan).

### Bugs conocidos y gotchas
1. **`ForegroundServiceType` enum**: `4` = `TypePhoneCall` (NO DataSync). Usar
   `ForegroundServiceType.TypeDataSync` (valor `1`). El literal incorrecto falla silenciosamente.
2. **Somee no envía keep-alives SignalR**: el `ServerTimeout` default (30s) provoca
   desconexiones cada 30s. Ya fijado a 3min + reconnect infinito.
3. **OneSignal + Cuba**: bloqueo por IP, no por puerto. La suscripción puede quedar corrupta
   (`-8`) — borrar datos del dispositivo para regenerar.
4. **Auto-login no arrancaba notificaciones** (ya arreglado Fase 1.4): `AppShell.CheckAuthenticationAsync`
   ahora arranca push+polling+foreground en el path de token guardado.
5. **Logout no detenía polling** (ya arreglado Fase 1.5): ahora `StopAsync` + `UnregisterUserAsync` + `Reset()`.

### Archivos clave (mapa rápido)
| Archivo | Rol |
|---------|-----|
| `Maui/Services/PushHealthService.cs` | Salud de canal instantáneo + dedup watermark |
| `Maui/Services/NotificationsHubClient.cs` | Cliente SignalR (reconnect infinito, ServerTimeout 3min) |
| `Maui/Services/PollingNotificationService.cs` | Polling push-aware (solo-heartbeat vs completo) |
| `Maui/Platforms/Android/NotificationsForegroundService.cs` | FG service (TypeDataSync, START_STICKY) |
| `Maui/Platforms/Android/Services/SystemNotificationService.cs` | Notificaciones del sistema (canal + sonido) |
| `Maui/Platforms/Android/NotificationsForegroundStarter.cs` | Arrancar/detener FG service |
| `Maui/App.xaml.cs` | `App.Services` (IServiceProvider), `PendingRoute`, reporte OneSignal |
| `Maui/AppShell.xaml.cs` | Auto-login arranca notificaciones; logout las detiene |
| `Maui/Helpers/Constants.cs` | Feature flags (`SignalRChannelEnabled`, `EnablePushAwarePolling`) |
| `Backend/Hubs/NotificationsHub.cs` | Hub SignalR + catch-up al conectar |
| `Backend/Services/SignalRNotificationBroadcaster.cs` | Difunde a grupo del usuario |
| `Backend/Services/PendingNotificationService.cs` | Crea pendientes + difunde SignalR |
| `Backend/Program.cs` | `AddSignalR()` + `MapHub<NotificationsHub>("/hubs/notifications")` |

## 🔲 Pendientes / mejoras futuras
- **⚠️ Verificar que el catch-up de `OnConnectedAsync` esté desplegado en Somee** (commiteado en
  `2a74744`, pero si no se redeployó, las notificaciones perdidas en huecos de desconexión no se
  recuperan al reconectar). Si hay dudas, redeployar el backend.
- **WorkManager** de respaldo para revivir el Foreground Service si OEM agresivos (Xiaomi/Huawei)
  lo matan (hoy se cubre con `START_STICKY` + foreground notification + exención de batería).
- Wiring del evento foreground de OneSignal (`OneSignal.Notifications.ReceivedInForeground`) para
  reportar entregas reales de FCM a `PushHealthService` (cuando se confirme la API del SDK 5.2.2).
  Nota: dado que OneSignal bloquea Cuba por IP, esto solo aplicaría a usuarios fuera de Cuba.
- Sonido `notfar.mp3` como sonido del canal del sistema (hoy se reproduce in-process; queda como
  canal default en la barra).
- Exención de batería en el S25 (release) no apareció: investigar si fue Auto-Backup restaurando
  el flag `battery_exemption_requested` o si Samsung exime por defecto. No bloquea el push real.
- Prueba de estrés pendiente: varias solicitudes seguidas, alguna con background prolongado.
- **Doble notificación fuera de Cuba**: si tanto OneSignal como SignalR entregan la misma
  notificación, el usuario puede ver duplicados. Investigar de-dup entre canales a nivel de
  OneSignal (reportar entrega OneSignal a PushHealth para que SignalR/sonido no duplique).

## 📌 Futuro: opción (b) — mover `LastActivityAt` a webhook de OneSignal
Hoy (opción a) se mantiene un loop mínimo de heartbeat (60 s) cuando el push funciona, para
alimentar `LastActivityAt` y la lógica de "no email a pacientes activos". Evolución limpia:
- Configurar **OneSignal delivery webhook** → endpoint en el backend que, al confirmar entrega push
  a un dispositivo, actualice `UserDeviceToken.LastActivityAt`.
- Así se puede **eliminar el heartbeat** y el loop de polling queda 100 % apagado cuando hay canal
  instantáneo (estado deseado puro, sin tráfico residual).
- Requiere añadir endpoint `POST /api/notifications/onesignal-webhook` y registrarlo en OneSignal.

---

**Última actualización**: 26 de agosto de 2026  
**Versión del sistema**: SignalR push-first + Foreground Service + email a farmacéuticos por solicitud ELIMINADO — `SignalRChannelEnabled=true` validado en Cuba y fuera de Cuba  
**Commits clave**: `0067a7a` (plan), `2a74744` (fix push real bg), `7cd65d8` (docs), `93c622f` (docs exhaustivo), este commit (email eliminado)
