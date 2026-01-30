using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;
    private readonly IPollingNotificationService _pollingService;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private bool notificationsEnabled = true;

    public ProfileViewModel(
        IAuthService authService, 
        IApiService apiService, 
        INotificationService notificationService,
        IPollingNotificationService pollingService)
        : base(authService, apiService)
    {
        _notificationService = notificationService;
        _pollingService = pollingService;
        Title = "Mi Perfil";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        
        if (userInfo != null)
        {
            UserName = userInfo.UserName;
            UserEmail = userInfo.Email;
            UserRole = GetRoleDisplayName(userInfo.Role);
        }
    }

    private string GetRoleDisplayName(string role)
    {
        return role switch
        {
            "Admin" => "Administrador",
            "Farmaceutico" => "Farmacéutico",
            "Viewer" => "Visualizador",
            "ViewerPublic" => "Paciente",
            _ => role
        };
    }

    [RelayCommand]
    private async Task ToggleNotificationsAsync()
    {
        try
        {
            if (NotificationsEnabled)
            {
                // Enable notifications
                var userInfo = await AuthService.GetUserInfoAsync();
                if (userInfo != null)
                {
                    await _notificationService.RegisterUserAsync(userInfo.Id, userInfo.Role);
                }
                await Shell.Current.DisplayAlert("Notificaciones", "Las notificaciones han sido habilitadas", "OK");
            }
            else
            {
                // Disable notifications
                await _notificationService.UnregisterUserAsync();
                await Shell.Current.DisplayAlert("Notificaciones", "Las notificaciones han sido deshabilitadas", "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al cambiar configuración de notificaciones: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        await Shell.Current.DisplayAlert("Cambiar Contraseña", "Funcionalidad próximamente disponible", "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Cerrar Sesión",
            "¿Estás seguro que deseas cerrar sesión?",
            "Sí",
            "No");

        if (confirm)
        {
            // Detener el servicio de polling
            try
            {
                await _pollingService.StopAsync();
                System.Diagnostics.Debug.WriteLine("[Profile] Polling service stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile] Error stopping polling: {ex.Message}");
            }
            
            await AuthService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    [RelayCommand]
    private async Task ShowNotificationDebugAsync()
    {
        try
        {
            var playerId = _notificationService.GetPlayerId();
            var isPushEnabled = _notificationService.IsPushEnabled();
            
            // Get additional debug info from App
            var appPlayerId = App.OneSignalPlayerId;
            var isInitialized = App.IsOneSignalInitialized;
            var initError = App.OneSignalInitError;
            
            string message = $"🔔 Estado de Notificaciones Push\n\n" +
                           $"• OneSignal Init: {(isInitialized ? "✅ Sí" : "❌ No")}\n" +
                           $"• PlayerId (Service): {(string.IsNullOrEmpty(playerId) ? "❌ No disponible" : $"✅ {playerId[..Math.Min(15, playerId.Length)]}...")}\n" +
                           $"• PlayerId (App): {(string.IsNullOrEmpty(appPlayerId) ? "❌ No disponible" : $"✅ {appPlayerId[..Math.Min(15, appPlayerId.Length)]}...")}\n" +
                           $"• Permisos: {(isPushEnabled ? "✅ Habilitados" : "❌ Deshabilitados")}\n";
            
            if (!string.IsNullOrEmpty(initError))
            {
                message += $"• Error Init: {initError}\n";
            }
            
            message += "\n";
            
            if (string.IsNullOrEmpty(playerId) && string.IsNullOrEmpty(appPlayerId))
            {
                message += "⚠️ El PlayerId no está disponible.\n\n" +
                          "Posibles causas:\n" +
                          "• FCM no pudo conectarse (¿VPN?)\n" +
                          "• Google Play Services no disponible\n" +
                          "• Firebase no configurado correctamente\n" +
                          "• Dispositivo sin conexión a internet\n\n" +
                          "💡 Intenta:\n" +
                          "1. Verifica que tienes conexión a internet\n" +
                          "2. Cierra y abre la app\n" +
                          "3. Revisa si Google Play Services está actualizado";
            }
            else
            {
                message += "✅ OneSignal está funcionando.\n\n" +
                          "💡 Para probar:\n" +
                          "1. Toca 'Forzar Registro' abajo\n" +
                          "2. Luego aprueba un turno desde MVC\n" +
                          "3. Deberías recibir una notificación push";
            }
            
            await Shell.Current.DisplayAlert("Debug Notificaciones", message, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ForceRegisterDeviceAsync()
    {
        try
        {
            IsBusy = true;
            
            var result = await _notificationService.RegisterDeviceAsync();
            
            if (result)
            {
                await Shell.Current.DisplayAlert("Éxito", "✅ Dispositivo registrado correctamente en el servidor", "OK");
            }
            else
            {
                var playerId = _notificationService.GetPlayerId();
                string errorMsg = string.IsNullOrEmpty(playerId) 
                    ? "❌ No hay PlayerId de OneSignal disponible" 
                    : "❌ Error al registrar en el servidor";
                await Shell.Current.DisplayAlert("Error", errorMsg, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
