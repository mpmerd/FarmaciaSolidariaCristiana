using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para la página de Login
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public LoginViewModel(
        IAuthService authService, 
        IApiService apiService,
        INotificationService notificationService) 
        : base(authService, apiService)
    {
        _notificationService = notificationService;
        Title = "Iniciar Sesión";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowErrorAsync("Por favor, ingrese su correo y contraseña.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await AuthService.LoginAsync(Email, Password);
            
            if (result.Success && result.Data != null)
            {
                // Registrar dispositivo para notificaciones push
                await _notificationService.RegisterDeviceAsync();
                
                // Configurar tags de usuario en OneSignal
                var user = result.Data.User;
                var primaryRole = user.Roles.FirstOrDefault() ?? "user";
                await _notificationService.SetUserTagsAsync(user.Id, primaryRole);
                
                // Navegar al Shell principal
                var appShell = App.Current?.Handler?.MauiContext?.Services.GetService<AppShell>();
                if (appShell != null)
                {
                    Application.Current!.MainPage = appShell;
                }
                else
                {
                    // Fallback: crear AppShell con el servicio de autenticación
                    Application.Current!.MainPage = new AppShell(AuthService);
                }
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al iniciar sesión");
            }
        });
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await NavigateToAsync("//register");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        await ShowErrorAsync("Para recuperar su contraseña, contacte al administrador.");
    }
}
