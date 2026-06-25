# 📲 Sistema de Notificaciones en Farmacia Solidaria Cristiana

## 1. Introducción / Descripción General

El sistema de notificaciones de la aplicación móvil MAUI implementa un enfoque híbrido que combina notificaciones push (OneSignal) con polling como respaldo, especialmente diseñado para el contexto específico de Cuba.

### 🎯 Contexto Específico: Restricciones en Cuba

- **Bloqueos frecuentes a Google Services**: El acceso a FCM/Google Services está restringido
- **Necesidad de alternativas confiables**: Se requiere un sistema que funcione incluso con restricciones geográficas
- **Solución elegida**: OneSignal + polling agresivo (cada 30 segundos)

### 🔄 Enfoque Híbrido

El sistema utiliza dos canales principales:
1. **Push Notifications** (OneSignal) - Para usuarios con conectividad a Google Services
2. **Polling** - Como respaldo y mecanismo de heartbeat para todos los usuarios

## 2. Arquitectura General del Sistema

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   App MAUI      │────▶│   API Backend   │────▶│   OneSignal     │
│   (Cliente)     │     │   (ASP.NET 10)  │     │   REST API      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        │ 1. Registro PlayerId  │                       │
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

### 📡 Diferencias por Rol

- **Farmacéuticos/Admins**: Reciben notificaciones push y polling con alta prioridad
- **Pacientes (ViewerPublic)**: Reciben notificaciones básicas, con verificación de actividad

## 3. Estrategia Híbrida Push + Polling

### 📥 Algoritmo del Push

#### Registro en Login:
```csharp
// En LoginViewModel.cs
var player = await OneSignalService.RegisterForPushNotifications();
if (player != null)
{
    // Registrar PlayerId con el backend
    await NotificationService.RegisterPlayerId(player);
    
    // Establecer tags por rol
    if (user.Role == "Farmaceutico")
        await OneSignalService.SetTag("role", "farmaceutico");
}
```

#### Manejo de Notificaciones:
```csharp
// En App.xaml.cs
protected override void OnNewIntent(Intent intent)
{
    base.OnNewIntent(intent);
    
    // Procesar notificación push recibida
    var notification = OneSignalService.GetNotificationFromIntent(intent);
    if (notification != null)
        HandlePushNotification(notification);
}
```

### 🔄 Algoritmo del Polling

#### Intervalo de Consulta:
```csharp
// En PollingNotificationService.cs
private const int POLLING_INTERVAL_SECONDS = 30;
private Timer _pollingTimer;

public void StartPolling()
{
    _pollingTimer = new Timer(async _ => 
    {
        await CheckForPendingNotifications();
        await UpdateLastActivity();
    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(POLLING_INTERVAL_SECONDS));
}
```

#### Lógica de Polling:
```csharp
private async Task CheckForPendingNotifications()
{
    var pending = await NotificationService.GetPendingNotifications();
    
    foreach (var notification in pending)
    {
        // Verificar si ya fue mostrada
        if (!AlreadyDisplayed(notification.Id))
        {
            ShowNotification(notification);
            MarkAsDisplayed(notification.Id);
        }
    }
}
```

### 📋 Lógica en LoginViewModel

```csharp
public async Task LoginAsync()
{
    // Intentar registro push
    var player = await OneSignalService.RegisterForPushNotifications();
    
    if (player != null)
    {
        // Registrar PlayerId con el backend
        await NotificationService.RegisterPlayerId(player);
        
        // Establecer tags por rol
        SetUserTags();
    }
    
    // Iniciar siempre polling
    PollingNotificationService.StartPolling();
}
```

## 4. Manejo de Casos Especiales y Restricciones en Cuba

### ⚠️ Comportamiento sin PlayerId

Cuando el registro push falla por restricciones geográficas:
- El sistema continúa operativo con polling como único mecanismo
- Se mantiene el heartbeat para verificar actividad del usuario
- No se pierden notificaciones críticas gracias al polling agresivo

### 🔄 Por qué Polling Siempre Activo

1. **Respaldo confiable**: Garantiza que las notificaciones lleguen incluso con bloqueos de Google Services
2. **Heartbeat continuo**: Mantiene actualizado el estado de actividad del usuario
3. **Simplicidad técnica**: Evita complejidades en la gestión de conexiones persistentes

### 📱 Comparación con Apps Populares en Cuba

| App | Tecnología | Ventajas |
|-----|------------|----------|
| WhatsApp | WebSockets + CDN | Conexión persistente, alta disponibilidad |
| Facebook | Proxy distribuidos | Redundancia geográfica |
| Google Services | FCM/Google Play | Integración nativa |

> **Nuestra solución**: OneSignal + polling HTTP es más simple y mantenible para un proyecto de este tamaño, aunque menos eficiente que las grandes plataformas.

## 5. Archivos Clave

### 📁 Proyecto MAUI

| Ruta | Descripción |
|------|-------------|
| `Services/OneSignalService.cs` | Implementación del registro y manejo de OneSignal |
| `Services/PollingNotificationService.cs` | Servicio de polling para notificaciones pendientes |
| `ViewModels/LoginViewModel.cs` | Lógica de inicio de sesión con registro push |
| `App.xaml.cs` | Manejo de notificaciones recibidas |
| `Models/NotificationModel.cs` | Estructura de datos de notificaciones |

### 📁 Backend ASP.NET 8

| Ruta | Descripción |
|------|-------------|
| `Services/OneSignalNotificationService.cs` | Servicio para enviar notificaciones OneSignal |
| `Controllers/NotificationsController.cs` | Endpoint para manejo de notificaciones |
| `Models/PendingNotification.cs` | Modelo de notificaciones pendientes |

## 6. Mejoras Futuras / Consideraciones

### 🔧 Optimizaciones Propuestas

1. **WebSockets**: Implementar conexión persistente para reducir latencia
2. **Ajuste dinámico de intervalos**: 
   - Reducción a 15 segundos en redes rápidas
   - Aumento a 60 segundos en redes lentas
3. **Alternativa Firebase**: Si se desbloquea Google Services, migrar a FCM

### 📈 Consideraciones Técnicas

- **Uso de CPU**: El polling agresivo puede impactar la batería (se debe optimizar)
- **Latencia**: Se prioriza confiabilidad sobre velocidad
- **Compatibilidad**: Mantener soporte para versiones antiguas de Android/iOS

### 🧪 Pruebas y Monitoreo

```csharp
// Ejemplo de monitoreo de notificaciones
public class NotificationMonitoringService
{
    public void LogNotificationDelivery(string notificationId, bool delivered)
    {
        // Registrar en logs o sistema de monitoreo
        Console.WriteLine($"Notificación {notificationId} - {(delivered ? "Entregada" : "Fallida")}");
    }
}
```