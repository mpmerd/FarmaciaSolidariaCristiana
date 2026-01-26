# 📲 Guía de Integración OneSignal - Notificaciones Push

## 📋 Resumen

Este documento describe la integración de OneSignal como proveedor de notificaciones push para la aplicación móvil .NET MAUI de Farmacia Solidaria Cristiana.

> **Nota**: Se eligió OneSignal porque Firebase Cloud Messaging (FCM) no está disponible en Cuba debido a restricciones geopolíticas.

---

## 🏗️ Arquitectura

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   App MAUI      │────▶│   API Backend   │────▶│   OneSignal     │
│   (Cliente)     │     │   (ASP.NET 8)   │     │   REST API      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        │ 1. Obtener PlayerId   │                       │
        │◀─────────────────────────────────────────────│
        │                       │                       │
        │ 2. Registrar token    │                       │
        │──────────────────────▶│                       │
        │                       │ 3. Guardar en BD      │
        │                       │                       │
        │                       │ 4. Enviar push        │
        │                       │──────────────────────▶│
        │                       │                       │
        │◀──────────────────────────────────────────────│
        │            5. Recibir notificación            │
```

---

## 🔧 Configuración del Backend

### 1. appsettings.json

```json
{
  "OneSignalSettings": {
    "AppId": "4d981851-f1a2-4112-8a08-08500e48f196",
    "RestApiKey": "TU_REST_API_KEY_DE_ONESIGNAL",
    "ApiUrl": "https://onesignal.com/api/v1"
  }
}
```

### 2. Obtener credenciales de OneSignal

1. Crear cuenta en [OneSignal](https://onesignal.com)
2. Crear una nueva aplicación
3. Configurar plataformas (iOS/Android)
4. Obtener:
   - **App ID**: Identificador de la aplicación
   - **REST API Key**: Clave para enviar notificaciones desde el backend

### 3. Ejecutar migración SQL

```bash
sqlcmd -S FarmaciaDb.mssql.somee.com -d FarmaciaDb -U maikelpelaez_SQLLogin_1 -P 'password' -i apply-migration-onesignal.sql
```

---

## 📡 API Endpoints

### Base URL
```
https://farmaciasolidaria.somee.com/api/notifications
```

### Autenticación
Todos los endpoints requieren JWT Bearer Token:
```
Authorization: Bearer <token>
```

---

### 1. Registrar Dispositivo

Registra el Player ID de OneSignal para recibir notificaciones.

```http
POST /api/notifications/device
Content-Type: application/json
Authorization: Bearer <token>

{
  "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "deviceToken": "token_fcm_opcional",
  "deviceType": "Android",
  "deviceName": "Samsung Galaxy S21"
}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "message": "Dispositivo registrado exitosamente",
  "data": {
    "id": 1,
    "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "deviceType": "Android",
    "deviceName": "Samsung Galaxy S21",
    "isActive": true,
    "createdAt": "2026-01-25T10:30:00Z",
    "updatedAt": "2026-01-25T10:30:00Z"
  },
  "timestamp": "2026-01-25T10:30:00Z"
}
```

---

### 2. Eliminar Dispositivo (Logout)

Desactiva el token al cerrar sesión.

```http
POST /api/notifications/device/unregister
Content-Type: application/json
Authorization: Bearer <token>

{
  "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

---

### 3. Listar Mis Dispositivos

```http
GET /api/notifications/devices
Authorization: Bearer <token>
```

**Respuesta:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "deviceType": "Android",
      "deviceName": "Samsung Galaxy S21",
      "isActive": true,
      "createdAt": "2026-01-25T10:30:00Z",
      "updatedAt": "2026-01-25T10:30:00Z"
    }
  ]
}
```

---

### 4. Verificar Estado de Push

```http
GET /api/notifications/push-status
Authorization: Bearer <token>
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "pushEnabled": true,
    "deviceCount": 2,
    "devices": [
      { "deviceType": "Android", "deviceName": "Samsung", "updatedAt": "..." },
      { "deviceType": "iOS", "deviceName": "iPhone 14", "updatedAt": "..." }
    ]
  }
}
```

---

### 5. Enviar Notificación de Prueba

```http
POST /api/notifications/test
Authorization: Bearer <token>
```

Envía una notificación de prueba al usuario autenticado.

---

### 6. Enviar Notificación (Admin/Farmacéutico)

```http
POST /api/notifications/send
Content-Type: application/json
Authorization: Bearer <token>

{
  "userId": "user-id-aqui",
  "title": "Título de la notificación",
  "message": "Cuerpo del mensaje",
  "type": "TurnoAprobado",
  "data": {
    "turnoId": "123",
    "action": "ver_turno"
  }
}
```

**Tipos de notificación disponibles:**
- `TurnoSolicitado`
- `TurnoAprobado`
- `TurnoRechazado`
- `TurnoPdfDisponible`
- `TurnoRecordatorio`
- `TurnoCancelado`
- `TurnoReprogramado`
- `General`

---

### 7. Broadcast a Todos (Solo Admin)

```http
POST /api/notifications/send/broadcast
Content-Type: application/json
Authorization: Bearer <token>

{
  "title": "Aviso importante",
  "message": "Mensaje para todos los usuarios",
  "type": "General"
}
```

---

## 📱 Integración en .NET MAUI

### 1. Instalar OneSignal SDK

```bash
dotnet add package OneSignalSDK.DotNet.iOS
dotnet add package OneSignalSDK.DotNet.Android
```

### 2. Inicializar OneSignal

```csharp
// MauiProgram.cs
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Inicializar OneSignal
        OneSignal.Default.Initialize("TU_APP_ID_DE_ONESIGNAL");
        
        // Solicitar permisos de notificación
        OneSignal.Default.PromptForPushNotificationsWithUserResponse();

        return builder.Build();
    }
}
```

### 3. Servicio de Notificaciones MAUI

```csharp
// Services/INotificationService.cs
public interface INotificationService
{
    Task<bool> RegisterDeviceAsync();
    Task<bool> UnregisterDeviceAsync();
    string? GetPlayerId();
    bool IsPushEnabled();
}
```

```csharp
// Services/NotificationService.cs
using OneSignalSDK.DotNet;
using System.Net.Http.Json;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    
    public NotificationService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }
    
    public string? GetPlayerId()
    {
        return OneSignal.Default.User.PushSubscription.Id;
    }
    
    public bool IsPushEnabled()
    {
        return OneSignal.Default.User.PushSubscription.OptedIn;
    }
    
    public async Task<bool> RegisterDeviceAsync()
    {
        var playerId = GetPlayerId();
        if (string.IsNullOrEmpty(playerId))
            return false;
            
        var request = new
        {
            oneSignalPlayerId = playerId,
            deviceType = DeviceInfo.Platform.ToString(),
            deviceName = DeviceInfo.Model
        };
        
        try
        {
            var token = await _authService.GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
                
            var response = await _httpClient.PostAsJsonAsync(
                "api/notifications/device", request);
                
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> UnregisterDeviceAsync()
    {
        var playerId = GetPlayerId();
        if (string.IsNullOrEmpty(playerId))
            return false;
            
        try
        {
            var token = await _authService.GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
                
            var response = await _httpClient.PostAsJsonAsync(
                "api/notifications/device/unregister", 
                new { oneSignalPlayerId = playerId });
                
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

### 4. ViewModel de Ejemplo (MVVM)

```csharp
// ViewModels/SettingsViewModel.cs
public partial class SettingsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    
    [ObservableProperty]
    private bool pushEnabled;
    
    [ObservableProperty]
    private int deviceCount;
    
    public SettingsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }
    
    [RelayCommand]
    private async Task LoadPushStatusAsync()
    {
        PushEnabled = _notificationService.IsPushEnabled();
        // Cargar desde API...
    }
    
    [RelayCommand]
    private async Task TogglePushAsync()
    {
        if (PushEnabled)
        {
            await _notificationService.RegisterDeviceAsync();
        }
        else
        {
            await _notificationService.UnregisterDeviceAsync();
        }
    }
}
```

### 5. Manejar Notificaciones Recibidas

```csharp
// App.xaml.cs
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // Escuchar notificaciones
        OneSignal.Default.Notifications.Clicked += OnNotificationClicked;
    }
    
    private void OnNotificationClicked(object? sender, NotificationClickedEventArgs e)
    {
        var data = e.Notification.AdditionalData;
        
        if (data != null && data.ContainsKey("action"))
        {
            var action = data["action"].ToString();
            var turnoId = data.ContainsKey("turnoId") 
                ? data["turnoId"].ToString() 
                : null;
            
            switch (action)
            {
                case "ver_turno":
                    // Navegar a detalle de turno
                    Shell.Current.GoToAsync($"//turno/{turnoId}");
                    break;
                    
                case "descargar_pdf":
                    var pdfUrl = data["pdfUrl"].ToString();
                    // Abrir PDF...
                    break;
                    
                case "ver_turnos":
                    Shell.Current.GoToAsync("//turnos");
                    break;
            }
        }
    }
}
```

---

## 🔄 Flujo de Eventos de Turno

El backend envía automáticamente notificaciones push en estos eventos:

| Evento | Método del Servicio | Cuando se dispara |
|--------|---------------------|-------------------|
| Turno solicitado | `SendTurnoSolicitadoNotificationAsync` | Usuario crea solicitud |
| Turno aprobado | `SendTurnoAprobadoNotificationAsync` | Farmacéutico aprueba |
| Turno rechazado | `SendTurnoRechazadoNotificationAsync` | Farmacéutico rechaza |
| PDF disponible | `SendTurnoPdfDisponibleNotificationAsync` | Se genera el PDF |
| Recordatorio | `SendTurnoRecordatorioNotificationAsync` | X horas antes del turno |
| Turno cancelado | `SendTurnoCanceladoNotificationAsync` | Se cancela el turno |
| Turno reprogramado | `SendTurnoReprogramadoNotificationAsync` | Cambia fecha/hora |
| Nueva solicitud | `SendNuevaSolicitudToFarmaceuticosAsync` | Notifica a farmacéuticos |

---

## 📊 Estructura de Datos de Notificación

```json
{
  "headings": { "es": "✅ Turno Aprobado", "en": "✅ Turno Aprobado" },
  "contents": { "es": "Tu turno #123 ha sido aprobado...", "en": "..." },
  "data": {
    "notificationType": "TurnoAprobado",
    "timestamp": "2026-01-25T10:30:00Z",
    "turnoId": "123",
    "numeroTurno": "123",
    "fechaTurno": "2026-01-26T09:00:00",
    "action": "ver_turno",
    "pdfUrl": "https://..."
  }
}
```

---

## 🛡️ Consideraciones de Seguridad

1. **REST API Key**: Nunca exponer en el cliente, solo usar en el backend
2. **Validación JWT**: Todos los endpoints requieren autenticación
3. **Permisos de rol**: Solo Admin/Farmacéutico pueden enviar notificaciones manuales
4. **Transferencia de dispositivos**: Si un PlayerId se registra con otro usuario, se desactiva del anterior

---

## 🧪 Pruebas

### Probar desde la API

```bash
# 1. Obtener token JWT
curl -X POST https://farmaciasolidaria.somee.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@email.com","password":"pass123"}'

# 2. Registrar dispositivo
curl -X POST https://farmaciasolidaria.somee.com/api/notifications/device \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"oneSignalPlayerId":"test-player-id","deviceType":"Android"}'

# 3. Enviar notificación de prueba
curl -X POST https://farmaciasolidaria.somee.com/api/notifications/test \
  -H "Authorization: Bearer <token>"
```

### Desde OneSignal Dashboard

1. Ir a [OneSignal Dashboard](https://app.onesignal.com)
2. Seleccionar tu aplicación
3. Messages > New Push
4. Enviar notificación de prueba

---

## 📝 Notas Adicionales

- Los tokens inactivos se conservan para auditoría (campo `IsActive = false`)
- Las notificaciones tienen TTL de 24 horas
- Se soportan múltiples dispositivos por usuario
- Los canales de Android deben configurarse en la app MAUI

---

## 📚 Referencias

- [OneSignal REST API](https://documentation.onesignal.com/reference/create-notification)
- [OneSignal .NET SDK](https://documentation.onesignal.com/docs/dotnet-sdk)
- [.NET MAUI Push Notifications](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/communication/push-notifications)
