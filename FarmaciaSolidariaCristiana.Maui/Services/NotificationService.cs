using System.Net.Http.Headers;
using System.Net.Http.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del servicio de notificaciones push con OneSignal
/// </summary>
public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private string? _playerId;

    public NotificationService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public void Initialize()
    {
#if ANDROID || IOS
        try
        {
            // OneSignal SDK 5.x initialization is done via Android/iOS platform code
            // For MAUI, we need to handle it differently
            // The actual OneSignal initialization should be in MainActivity.cs for Android
            // and AppDelegate.cs for iOS
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OneSignal init error: {ex.Message}");
        }
#endif
    }

    public string? GetPlayerId()
    {
        return _playerId;
    }

    public bool IsPushEnabled()
    {
#if ANDROID || IOS
        // In a real implementation, check the notification permission status
        return true;
#else
        return false;
#endif
    }

    public async Task<bool> RegisterDeviceAsync()
    {
        try
        {
            var playerId = GetPlayerId();
            if (string.IsNullOrEmpty(playerId))
                return false;

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                oneSignalPlayerId = playerId,
                deviceType = DeviceInfo.Platform.ToString(),
                deviceName = DeviceInfo.Model,
                appVersion = AppInfo.VersionString
            };

            var response = await _httpClient.PostAsJsonAsync("/api/notifications/device", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceAsync()
    {
        try
        {
            var playerId = GetPlayerId();
            if (string.IsNullOrEmpty(playerId))
                return false;

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/notifications/device/unregister",
                new { oneSignalPlayerId = playerId });
                
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task RequestPermissionAsync()
    {
#if ANDROID || IOS
        // Request notification permissions through platform-specific code
        await Task.CompletedTask;
#else
        await Task.CompletedTask;
#endif
    }

    public Task SetUserTagsAsync(string userId, string role)
    {
        // Store tags locally, will be used when OneSignal is properly initialized
        Preferences.Set("onesignal_user_id", userId);
        Preferences.Set("onesignal_role", role);
        return Task.CompletedTask;
    }

    public async Task RegisterUserAsync(string userId, string role)
    {
        await SetUserTagsAsync(userId, role);
        await RegisterDeviceAsync();
    }

    public async Task UnregisterUserAsync()
    {
        Preferences.Remove("onesignal_user_id");
        Preferences.Remove("onesignal_role");
        await UnregisterDeviceAsync();
    }

    /// <summary>
    /// Called from platform-specific code when OneSignal provides the player ID
    /// </summary>
    public void SetPlayerId(string playerId)
    {
        _playerId = playerId;
    }
}
