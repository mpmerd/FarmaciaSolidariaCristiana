# Sistema Inteligente de Notificaciones: Push vs Polling

## 🎯 Problema Resuelto

Antes, la app iniciaba **ambos sistemas de notificaciones concurrentemente**:
- ✅ OneSignal/FCM Push (funciona fuera de Cuba)
- ✅ Sistema de Polling (alternativa para Cuba)

**Resultado**: Usuarios fuera de Cuba recibían **notificaciones duplicadas** 📱📱

## ✅ Solución Implementada

### Lógica Inteligente (Auto-detección)

La app ahora detecta automáticamente si OneSignal/FCM funciona y decide qué sistema usar:

```
┌─────────────────────────────────────────┐
│ Usuario inicia sesión                    │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│ Intentar registrar con OneSignal/FCM    │
└────────────────┬────────────────────────┘
                 │
                 ▼
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
┌──────────────┐   ┌──────────────┐
│ ✅ Funciona  │   │ ❌ Falla     │
│ (Fuera Cuba) │   │ (Cuba)       │
└──────┬───────┘   └──────┬───────┘
       │                  │
       ▼                  ▼
┌──────────────┐   ┌──────────────┐
│ Usar PUSH    │   │ Usar POLLING │
│ No Polling   │   │ Cada 30s     │
└──────────────┘   └──────────────┘
```

## 📝 Cambios Realizados

### 1. Nueva interfaz en INotificationService

```csharp
/// Obtiene el Player ID de OneSignal de manera asíncrona con reintentos
Task<string?> GetPlayerIdAsync(int maxRetries = 3, int delayMs = 500);
```

### 2. Implementación en NotificationService

```csharp
public async Task<string?> GetPlayerIdAsync(int maxRetries = 3, int delayMs = 500)
{
    // Intenta obtener el PlayerId con reintentos
    for (int i = 0; i < maxRetries; i++)
    {
        var playerId = GetPlayerId();
        
        if (!string.IsNullOrEmpty(playerId))
        {
            return playerId; // ✅ OneSignal funciona
        }
        
        await Task.Delay(delayMs); // Esperar antes de reintentar
    }
    
    return null; // ❌ OneSignal no disponible
}
```

### 3. Lógica condicional en LoginViewModel

```csharp
// Intentar registrar con Push/OneSignal
bool pushWorking = false;
try
{
    await _notificationService.RegisterDeviceAsync();
    
    // Verificar que tengamos un PlayerId válido
    var playerId = await _notificationService.GetPlayerIdAsync();
    if (!string.IsNullOrEmpty(playerId))
    {
        pushWorking = true; // ✅ Push funciona
    }
}
catch (Exception ex)
{
    // Push no disponible (Cuba u otro problema)
}

// Iniciar Polling SOLO si Push NO funciona
if (!pushWorking)
{
    await _pollingService.StartAsync();
    System.Diagnostics.Debug.WriteLine("Push not available - Using Polling");
}
else
{
    System.Diagnostics.Debug.WriteLine("Push/OneSignal working - Polling NOT started");
}
```

## 🔍 Detección de Funcionalidad

La app detecta si OneSignal/FCM funciona verificando:

1. **Inicialización de OneSignal** → ¿Se inicializó correctamente?
2. **Registro del dispositivo** → ¿Obtuvo un Player ID?
3. **PlayerId válido** → ¿Hay un ID de suscripción push?

Si **todos estos pasos** funcionan → Use Push ✅  
Si **alguno falla** → Use Polling 🔄

## 📊 Escenarios

### Escenario 1: Usuario en España (Google Services disponible)

```
1. Inicia sesión
2. OneSignal se inicializa ✅
3. Obtiene PlayerId: "abc123..." ✅
4. pushWorking = true
5. ✅ USA PUSH (OneSignal/FCM)
6. ❌ NO inicia Polling
7. Recibe notificaciones instantáneas vía Push
```

### Escenario 2: Usuario en Cuba (Google Services bloqueado)

```
1. Inicia sesión
2. OneSignal se inicializa pero no puede conectar a FCM ❌
3. PlayerId = null después de 3 reintentos ❌
4. pushWorking = false
5. ❌ Push no disponible
6. ✅ INICIA Polling (cada 30 segundos)
7. Recibe notificaciones vía consultas al servidor
```

### Escenario 3: Usuario viaja de Cuba a España

```
Día 1 (Cuba):
- Login → Detecta Push no funciona
- Usa Polling ✅

Día 5 (España):
- Cierra sesión
- Login nuevamente
- Detecta Push funciona ✅
- Cambia automáticamente a Push
- Deja de usar Polling
```

## ⚡ Ventajas de la Solución

### 1. **Sin Duplicados**
- Usuarios fuera de Cuba: Solo reciben notificaciones Push (1 vez)
- No hay consumo innecesario de batería por polling

### 2. **Adaptable**
- Se ajusta automáticamente según la disponibilidad de Google Services
- No requiere configuración manual

### 3. **Fallback Robusto**
- Si Push falla, automáticamente usa Polling
- Garantiza que todos los usuarios reciban notificaciones

### 4. **Eficiente**
- Polling solo cuando es necesario
- Ahorro de batería y ancho de banda en países con Google Services

### 5. **Transparente**
- El usuario no nota la diferencia
- Logs detallados para debugging

## 🧪 Pruebas Recomendadas

### Prueba 1: Con Google Services (Fuera de Cuba)

1. Desinstalar la app
2. Instalar APK nuevo
3. Iniciar sesión
4. Verificar logs:
   ```
   [Login] Push/OneSignal working correctly. PlayerId: XXXXX
   [Login] Push/OneSignal is working - Polling service NOT started
   ```
5. Enviar notificación desde OneSignal Dashboard
6. Debe recibir la notificación instantáneamente
7. NO debe recibir duplicados

### Prueba 2: Simulando Cuba (Sin Google Services)

Opción A - Deshabilitar Google Services en el dispositivo:
1. Settings → Apps → Google Play Services → Disable
2. Reiniciar app
3. Iniciar sesión
4. Verificar logs:
   ```
   [NotificationService] Failed to get PlayerId after 3 attempts
   [Login] Push not available - Polling service started as fallback
   ```
5. Crear un turno desde el panel de admin
6. La app debe mostrar notificación en ~30 segundos (via polling)

Opción B - Simular con APK sin Google Services:
1. Usar un emulador sin Google Play
2. Instalar APK
3. Iniciar sesión
4. Debe automáticamente activar Polling

### Prueba 3: Logout

1. Con sesión activa (usando Push o Polling)
2. Cerrar sesión
3. Verificar logs:
   ```
   [Profile] Polling service stopped
   ```
4. El service debe detenerse correctamente

## 📋 Archivos Modificados

1. **INotificationService.cs**
   - ✅ Agregado: `GetPlayerIdAsync()` con reintentos

2. **NotificationService.cs**
   - ✅ Implementado: `GetPlayerIdAsync()` con lógica de reintentos
   - ✅ Logs detallados de debugging

3. **LoginViewModel.cs**
   - ✅ Lógica condicional: Push vs Polling
   - ✅ Detección automática de disponibilidad
   - ✅ Logs informativos

## 🔧 Configuración

### Parámetros ajustables

En `LoginViewModel.cs`, el método `GetPlayerIdAsync()` acepta:

```csharp
await _notificationService.GetPlayerIdAsync(
    maxRetries: 3,    // Número de reintentos
    delayMs: 500      // Delay entre reintentos (ms)
);
```

**Por defecto**: 3 reintentos con 500ms entre cada uno = 1.5 segundos máximo

Si OneSignal es lento en inicializarse, puedes aumentar:
```csharp
await _notificationService.GetPlayerIdAsync(maxRetries: 5, delayMs: 1000);
```

### Intervalo de Polling

Si el Polling está activo, el intervalo se configura en `PollingNotificationService`:

```csharp
public int PollingIntervalSeconds { get; set; } = 30; // 30 segundos
```

## 🐛 Troubleshooting

### Problema: Sigue recibiendo duplicados

**Diagnóstico**: Verificar en logs qué sistema está activo

**Solución**:
1. Revisar logs después del login
2. Si dice "Polling service NOT started" pero igual hace polling → revisar que se detuvo correctamente

### Problema: No recibe notificaciones en Cuba

**Diagnóstico**: Verificar que Polling se inició

**Solución**:
1. Buscar en logs: `[Login] Push not available - Polling service started`
2. Si no aparece → revisar errores de autenticación
3. Verificar conectividad al servidor

### Problema: OneSignal no funciona fuera de Cuba

**Diagnóstico**: API key incorrecta o restricciones mal configuradas

**Solución**:
1. Verificar que `google-services.json` tiene la nueva API key
2. Revisar restricciones en Google Cloud Console
3. Verificar en logs: `[OneSignal] Permission granted: true`

## 📚 Recursos

- **Logs de OneSignal**: Buscar `[OneSignal]` en logcat
- **Logs de Polling**: Buscar `[PollingService]` en logcat
- **Logs de Login**: Buscar `[Login]` en logcat
- **OneSignal Dashboard**: https://app.onesignal.com/
- **Firebase Console**: https://console.firebase.google.com/

## ✅ Checklist de Despliegue

Antes de distribuir la nueva versión:

- [ ] Compilar APK con la nueva API key
- [ ] Probar en dispositivo con Google Services
- [ ] Probar en dispositivo sin Google Services (o emulador)
- [ ] Verificar que no hay duplicados fuera de Cuba
- [ ] Verificar que Polling funciona en Cuba
- [ ] Revisar logs de ambos escenarios
- [ ] Incrementar versión de la app
- [ ] Actualizar CHANGELOG
