using System.Net.Http.Headers;
using System.Net.Http.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using OneSignalSDK.DotNet;

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
            // OneSignal SDK 5.x initialization is done in platform-specific code
            // MainActivity.cs for Android and AppDelegate.cs for iOS
            // Here we just try to get the subscription ID if available
            UpdatePlayerIdFromOneSignal();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OneSignal init error: {ex.Message}");
        }
#endif
    }

    /// <summary>
    /// Updates the PlayerId from OneSignal SDK
    /// </summary>
    private void UpdatePlayerIdFromOneSignal()
    {
#if ANDROID || IOS
        try
        {
            // First check if App has the PlayerId (from subscription change event)
            if (!string.IsNullOrEmpty(App.OneSignalPlayerId))
            {
                _playerId = App.OneSignalPlayerId;
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Got PlayerId from App: {_playerId}");
                return;
            }
            
            // Try to get directly from OneSignal SDK
            var subscriptionId = OneSignal.User.PushSubscription.Id;
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                _playerId = subscriptionId;
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Got PlayerId from SDK: {_playerId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[NotificationService] PlayerId is null or empty from SDK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] Error getting subscription ID: {ex.Message}");
        }
#endif
    }

    public string? GetPlayerId()
    {
        // Try to update from OneSignal if we don't have it yet
        if (string.IsNullOrEmpty(_playerId))
        {
            UpdatePlayerIdFromOneSignal();
        }
        
        // Return App's PlayerId as fallback
        return _playerId ?? App.OneSignalPlayerId;
    }

    public bool IsPushEnabled()
    {
#if ANDROID || IOS
        try
        {
            return OneSignal.Notifications.Permission;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    public async Task<bool> RegisterDeviceAsync()
    {
        try
        {
            // Retry up to 5 times with delay if playerId is not available yet
            string? playerId = null;
            for (int i = 0; i < 5; i++)
            {
                playerId = GetPlayerId();
                if (!string.IsNullOrEmpty(playerId))
                    break;
                    
                System.Diagnostics.Debug.WriteLine($"[NotificationService] PlayerId not available yet, retry {i + 1}/5...");
                await Task.Delay(1000); // Wait 1 second
            }
            
            if (string.IsNullOrEmpty(playerId))
            {
                System.Diagnostics.Debug.WriteLine("[NotificationService] Failed to get OneSignal PlayerId after 5 retries");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[NotificationService] PlayerId obtained: {playerId}");

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[NotificationService] No auth token available");
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                oneSignalPlayerId = playerId,
                deviceType = DeviceInfo.Platform.ToString(),
                deviceName = DeviceInfo.Model,
                appVersion = AppInfo.VersionString
            };

            System.Diagnostics.Debug.WriteLine($"[NotificationService] Registering device with API: {playerId}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/notifications/device", request);
            
            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("[NotificationService] Device registered successfully!");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Device registration failed: {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationService] RegisterDeviceAsync error: {ex.Message}");
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
        try
        {
            // Request notification permissions through OneSignal SDK 5.x
            await OneSignal.Notifications.RequestPermissionAsync(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting permission: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    public Task SetUserTagsAsync(string userId, string role)
    {
#if ANDROID || IOS
        try
        {
            // Set user tags in OneSignal SDK 5.x
            OneSignal.User.AddTag("user_id", userId);
            OneSignal.User.AddTag("role", role);
            
            // Also login the user in OneSignal for better tracking
            OneSignal.Login(userId);
            
            System.Diagnostics.Debug.WriteLine($"OneSignal tags set for user: {userId}, role: {role}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting OneSignal tags: {ex.Message}");
        }
#endif
        
        // Also store locally as backup
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
#if ANDROID || IOS
        try
        {
            // Logout from OneSignal
            OneSignal.Logout();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error logging out from OneSignal: {ex.Message}");
        }
#endif
        
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
