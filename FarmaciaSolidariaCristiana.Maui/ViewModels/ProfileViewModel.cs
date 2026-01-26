using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string userEmail = string.Empty;

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private bool notificationsEnabled = true;

    public ProfileViewModel(IAuthService authService, INotificationService notificationService)
    {
        _authService = authService;
        _notificationService = notificationService;
        Title = "Mi Perfil";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await _authService.GetUserInfoAsync();
        
        if (userInfo != null)
        {
            UserName = userInfo.NombreCompleto;
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
                var userInfo = await _authService.GetUserInfoAsync();
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
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
