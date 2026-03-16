using System.Net.Http.Headers;
using System.Net.Http.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using CommunityToolkit.Maui.Alerts;
using Plugin.Maui.Audio;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del servicio de polling para notificaciones.
/// Consulta periódicamente al servidor para verificar nuevas notificaciones.
/// Esta es la solución para Cuba donde FCM/Push no funciona.
/// </summary>
public class PollingNotificationService : IPollingNotificationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly IAudioManager _audioManager;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;
    private readonly HashSet<int> _shownNotificationIds = new();
    private bool _disposed;

    public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
    
    public bool IsRunning { get; private set; }
    public int PollingIntervalSeconds { get; set; } = 30; // Por defecto cada 30 segundos

    public PollingNotificationService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _audioManager = AudioManager.Current;
    }

    public async Task StartAsync()
    {
        if (IsRunning)
        {
            System.Diagnostics.Debug.WriteLine("[PollingService] Already running, skipping start");
            return;
        }

        // Verificar que el usuario esté autenticado
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            System.Diagnostics.Debug.WriteLine("[PollingService] No auth token, cannot start polling");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        IsRunning = true;

        System.Diagnostics.Debug.WriteLine($"[PollingService] Starting with interval of {PollingIntervalSeconds} seconds");

        _pollingTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await PollForNotificationsAsync();
                    await SendHeartbeatAsync();
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("[PollingService] Polling cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PollingService] Error during polling: {ex.Message}");
                    // Esperar más tiempo antes de reintentar en caso de error
                    await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds * 2), _cancellationTokenSource.Token);
                }
            }
        }, _cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        System.Diagnostics.Debug.WriteLine("[PollingService] Stopping...");

        _cancellationTokenSource?.Cancel();
        
        if (_pollingTask != null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Esperado
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _pollingTask = null;
        IsRunning = false;
        _shownNotificationIds.Clear();

        System.Diagnostics.Debug.WriteLine("[PollingService] Stopped");
    }

    public async Task<int> CheckNowAsync()
    {
        return await PollForNotificationsAsync();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var response = await _httpClient.GetAsync("/api/notifications/pending/count");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<UnreadCountResponse>>();
                return result?.Data?.UnreadCount ?? 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error getting unread count: {ex.Message}");
        }

        return 0;
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var response = await _httpClient.PostAsync($"/api/notifications/pending/{notificationId}/read", null);
            if (response.IsSuccessStatusCode)
            {
                _shownNotificationIds.Add(notificationId);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error marking as read: {ex.Message}");
        }

        return false;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var response = await _httpClient.PostAsync("/api/notifications/pending/read-all", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<MarkedCountResponse>>();
                _shownNotificationIds.Clear();
                return result?.Data?.MarkedCount ?? 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error marking all as read: {ex.Message}");
        }

        return 0;
    }

    private async Task<int> PollForNotificationsAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var response = await _httpClient.GetAsync("/api/notifications/pending");
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[PollingService] Failed to get notifications: {response.StatusCode}");
                return 0;
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<PendingNotificationsResponse>>();
            if (result?.Data == null || result.Data.Count == 0)
            {
                return 0;
            }

            // Recopilar notificaciones nuevas primero
            var newNotifications = new List<PendingNotificationItem>();
            foreach (var notification in result.Data.Notifications)
            {
                if (!_shownNotificationIds.Contains(notification.Id))
                {
                    _shownNotificationIds.Add(notification.Id);
                    newNotifications.Add(notification);
                }
            }

            int newCount = newNotifications.Count;

            // Mostrar cada notificación secuencialmente con espera entre ellas
            foreach (var notification in newNotifications)
            {
                // Disparar evento
                var args = new NotificationReceivedEventArgs
                {
                    NotificationId = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    NotificationType = notification.NotificationType,
                    ReferenceId = notification.ReferenceId,
                    ReferenceType = notification.ReferenceType,
                    CreatedAt = notification.CreatedAt
                };
                NotificationReceived?.Invoke(this, args);

                // Mostrar notificación local y esperar a que sea visible el tiempo completo
                await ShowLocalNotificationAsync(notification);
                System.Diagnostics.Debug.WriteLine($"[PollingService] Showing local notification: {notification.Title}");

                // Marcar como leída en el servidor
                await MarkAsReadAsync(notification.Id);

                // Si hay más notificaciones en cola, esperar antes de mostrar la siguiente
                // para que el usuario tenga tiempo de leer cada una
                if (newNotifications.Count > 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(6));
                }
            }

            if (newCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PollingService] {newCount} new notifications received");
            }

            return newCount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error polling: {ex.Message}");
            return 0;
        }
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var heartbeat = new
            {
                deviceType = DeviceInfo.Platform.ToString(),
                deviceName = DeviceInfo.Model
            };

            await _httpClient.PostAsJsonAsync("/api/notifications/heartbeat", heartbeat);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error sending heartbeat: {ex.Message}");
        }
    }

    private async Task ShowLocalNotificationAsync(PendingNotificationItem notification)
    {
        // Reproducir sonido de notificación
        await PlayNotificationSoundAsync();
        
        // Mostrar como toast/snackbar en la app
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Usar CommunityToolkit.Maui para mostrar un snackbar
                var snackbar = Snackbar.Make(
                    message: $"🔔 {notification.Title}: {notification.Message}",
                    actionButtonText: "Ver",
                    duration: TimeSpan.FromSeconds(10),
                    action: async () =>
                    {
                        // Marcar como leída cuando el usuario toca "Ver"
                        await MarkAsReadAsync(notification.Id);
                        
                        // Navegar según el tipo de notificación
                        if (notification.NotificationType?.Contains("Turno") == true)
                        {
                            await Shell.Current.GoToAsync("//TurnosPage");
                        }
                    });

                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PollingService] Error showing snackbar: {ex.Message}");
                
                // Fallback: mostrar como DisplayAlert
                try
                {
                    if (Shell.Current?.CurrentPage != null)
                    {
                        await Shell.Current.DisplayAlert(
                            notification.Title,
                            notification.Message,
                            "OK");
                    }
                }
                catch { /* Ignorar errores de UI */ }
            }
        });
    }

    private async Task PlayNotificationSoundAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[PollingService] Intentando reproducir sonido...");
            
            // Abrir el archivo de sonido desde Resources/Raw
            using var stream = await FileSystem.OpenAppPackageFileAsync("notfar.mp3");
            var player = _audioManager.CreatePlayer(stream);
            
            player.Play();
            
            // Esperar a que termine de reproducir (máximo 5 segundos)
            var timeout = DateTime.Now.AddSeconds(5);
            while (player.IsPlaying && DateTime.Now < timeout)
            {
                await Task.Delay(100);
            }
            
            // Liberar el player
            player.Dispose();
            
            System.Diagnostics.Debug.WriteLine("[PollingService] Sonido de notificación reproducido correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PollingService] Error reproduciendo sonido: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PollingService] Stack trace: {ex.StackTrace}");
        }
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}

// DTOs auxiliares para deserialización
internal class ApiResponseWrapper<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

internal class UnreadCountResponse
{
    public int UnreadCount { get; set; }
}

internal class MarkedCountResponse
{
    public int MarkedCount { get; set; }
}
