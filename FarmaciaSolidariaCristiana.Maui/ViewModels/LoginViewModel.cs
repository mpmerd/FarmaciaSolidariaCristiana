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

        // Verificar conexión a internet antes de intentar login
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await ShowErrorAsync("No hay conexión a internet. Por favor, verifique su conexión e intente nuevamente.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await AuthService.LoginAsync(Email, Password);
            
            if (result.Success && result.Data != null)
            {
                // Estrategia híbrida: Intentar Push primero, Polling siempre como respaldo
                // El Polling también envía heartbeat para que el servidor sepa que estamos activos
                
                var userData = result.Data;
                var user = userData.User;
                var primaryRole = user.Roles.FirstOrDefault() ?? "user";
                
                bool pushWorking = false;
                
                // 1. Intentar registrar Push (con timeout)
                try
                {
                    await _notificationService.SetUserTagsAsync(user.Id, primaryRole);
                    await _notificationService.RegisterDeviceAsync();
                    
                    // Esperar un poco para que OneSignal obtenga el PlayerId
                    var playerId = await _notificationService.GetPlayerIdAsync(maxRetries: 5, delayMs: 1000);
                    
                    if (!string.IsNullOrEmpty(playerId))
                    {
                        pushWorking = true;
                        System.Diagnostics.Debug.WriteLine($"[Login] ✅ Push registrado. PlayerId: {playerId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Login] ⚠️ Push sin PlayerId");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] ⚠️ Push falló: {ex.Message}");
                }
                
                // 2. SIEMPRE iniciar Polling (para heartbeat y notificaciones de respaldo)
                // Si Push funciona, Polling sirve como backup y para mantener heartbeat
                // Si Push no funciona, Polling es el canal principal
                try
                {
                    await _pollingService.StartAsync();
                    if (pushWorking)
                    {
                        System.Diagnostics.Debug.WriteLine("[Login] ✅ Polling iniciado como respaldo (Push es primario)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Login] ✅ Polling iniciado como canal principal (Push no disponible)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] ❌ Error iniciando Polling: {ex.Message}");
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
