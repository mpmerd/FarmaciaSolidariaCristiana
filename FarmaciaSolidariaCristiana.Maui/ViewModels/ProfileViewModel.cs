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

    public string AppVersion => $"Versión {AppInfo.VersionString}";

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
                await Shell.Current.DisplayAlertAsync("Notificaciones", "Las notificaciones han sido habilitadas", "OK");
            }
            else
            {
                // Disable notifications
                await _notificationService.UnregisterUserAsync();
                await Shell.Current.DisplayAlertAsync("Notificaciones", "Las notificaciones han sido deshabilitadas", "OK");
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
        await Shell.Current.GoToAsync("ChangePasswordPage");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync(
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
}
