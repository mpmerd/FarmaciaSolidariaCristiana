using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para la página de Registro
/// </summary>
public partial class RegisterViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    [ObservableProperty]
    private bool _registrationEnabled = true;

    [ObservableProperty]
    private string _registrationMessage = string.Empty;

    public RegisterViewModel(IAuthService authService, IApiService apiService) 
        : base(authService, apiService)
    {
        Title = "Registro";
    }

    /// <summary>
    /// Verifica el estado del registro público al cargar la página
    /// </summary>
    [RelayCommand]
    private async Task CheckRegistrationStatusAsync()
    {
        var result = await ApiService.GetRegistrationStatusAsync();
        if (result.Success && result.Data != null)
        {
            RegistrationEnabled = result.Data.IsEnabled;
            RegistrationMessage = result.Data.Message ?? string.Empty;
        }
        else
        {
            // Si no se puede verificar, asumimos habilitado
            RegistrationEnabled = true;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(UserName))
        {
            await ShowErrorAsync("Por favor, ingrese un nombre de usuario.");
            return;
        }

        if (UserName.Length < 3)
        {
            await ShowErrorAsync("El nombre de usuario debe tener al menos 3 caracteres.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            await ShowErrorAsync("Por favor, ingrese su correo electrónico.");
            return;
        }

        if (!Email.Contains("@") || !Email.Contains("."))
        {
            await ShowErrorAsync("Por favor, ingrese un correo electrónico válido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            await ShowErrorAsync("Por favor, ingrese una contraseña.");
            return;
        }

        if (Password.Length < 6)
        {
            await ShowErrorAsync("La contraseña debe tener al menos 6 caracteres.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await ShowErrorAsync("Las contraseñas no coinciden.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new RegisterRequest
            {
                UserName = UserName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                ConfirmPassword = ConfirmPassword
            };

            var result = await ApiService.RegisterAsync(request);

            if (result.Success)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "¡Registro Exitoso!",
                    result.Message ?? "Su cuenta ha sido creada. Ya puede iniciar sesión.",
                    "OK");

                // Navegar de regreso al login
                await GoToLoginAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al crear la cuenta");
            }
        });
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
