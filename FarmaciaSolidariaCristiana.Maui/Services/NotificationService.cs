using System.Net.Http.Headers;
using System.Net.Http.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del servicio de notificaciones push con OneSignal
/// </summary>
public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public NotificationService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public void Initialize()
    {
        OneSignal.Default.Initialize(Constants.OneSignalAppId);
        OneSignal.Default.Notifications.RequestPermissionAsync(true);
    }

    public string? GetPlayerId()
    {
        try
        {
            return OneSignal.Default.User.PushSubscription.Id;
        }
        catch
        {
            return null;
        }
    }

    public bool IsPushEnabled()
    {
        try
        {
            return OneSignal.Default.User.PushSubscription.OptedIn;
        }
        catch
        {
            return false;
        }
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
        await OneSignal.Default.Notifications.RequestPermissionAsync(true);
    }

    public async Task SetUserTagsAsync(string userId, string role)
    {
        try
        {
            OneSignal.Default.User.AddTag("user_id", userId);
            OneSignal.Default.User.AddTag("role", role);
            await Task.CompletedTask;
        }
        catch
        {
            // Ignorar errores de tags
        }
    }
}
