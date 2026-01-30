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
    private readonly IPollingNotificationService _pollingService;

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
        INotificationService notificationService,
        IPollingNotificationService pollingService) 
        : base(authService, apiService)
    {
        _notificationService = notificationService;
        _pollingService = pollingService;
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
                // Intentar registrar dispositivo para notificaciones push (puede fallar en Cuba)
                try
                {
                    await _notificationService.RegisterDeviceAsync();
                    
                    // Configurar tags de usuario en OneSignal (si funciona)
                    var user = result.Data.User;
                    var primaryRole = user.Roles.FirstOrDefault() ?? "user";
                    await _notificationService.SetUserTagsAsync(user.Id, primaryRole);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] Push registration failed (expected in Cuba): {ex.Message}");
                }
                
                // Iniciar servicio de polling para notificaciones (funciona siempre)
                try
                {
                    await _pollingService.StartAsync();
                    System.Diagnostics.Debug.WriteLine("[Login] Polling service started successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] Failed to start polling: {ex.Message}");
                }
                
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
        await Shell.Current.GoToAsync(nameof(Views.RegisterPage));
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        // Solicitar el email/usuario
        var emailOrUserName = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Recuperar Contraseña",
            "Ingrese su correo electrónico o nombre de usuario:",
            "Enviar",
            "Cancelar",
            placeholder: "correo@ejemplo.com",
            keyboard: Keyboard.Email);

        if (string.IsNullOrWhiteSpace(emailOrUserName))
            return;

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.ForgotPasswordAsync(emailOrUserName);
            
            if (result.Success)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Correo Enviado",
                    result.Message ?? "Si el usuario existe, recibirá un correo con instrucciones para restablecer su contraseña.",
                    "OK");
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al procesar la solicitud");
            }
        });
    }
}
