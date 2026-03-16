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
    private string _verificationCode = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isConfirmPasswordVisible;

    [ObservableProperty]
    private bool _registrationEnabled = true;

    [ObservableProperty]
    private string _registrationMessage = string.Empty;

    [ObservableProperty]
    private bool _isCodeSent;

    [ObservableProperty]
    private bool _isSendingCode;

    [ObservableProperty]
    private string _codeStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isCodeStatusError;

    [ObservableProperty]
    private int _cooldownSeconds;

    [ObservableProperty]
    private bool _isCooldownActive;

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
    private async Task SendVerificationCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            await ShowErrorAsync("Ingrese su correo electrónico primero.");
            return;
        }

        if (!Email.Contains("@") || !Email.Contains("."))
        {
            await ShowErrorAsync("Ingrese un correo electrónico válido.");
            return;
        }

        IsSendingCode = true;
        CodeStatusMessage = string.Empty;

        try
        {
            var result = await ApiService.SendVerificationCodeAsync(Email.Trim());

            if (result.Success)
            {
                IsCodeSent = true;
                IsCodeStatusError = false;
                CodeStatusMessage = result.Message ?? "Código enviado a tu correo.";
                await StartCooldownAsync();
            }
            else
            {
                IsCodeStatusError = true;
                CodeStatusMessage = result.Message ?? "No se pudo enviar el código.";
            }
        }
        catch (Exception ex)
        {
            IsCodeStatusError = true;
            CodeStatusMessage = $"Error de conexión: {ex.Message}";
        }
        finally
        {
            IsSendingCode = false;
        }
    }

    private async Task StartCooldownAsync()
    {
        IsCooldownActive = true;
        CooldownSeconds = 60;
        while (CooldownSeconds > 0)
        {
            await Task.Delay(1000);
            CooldownSeconds--;
        }
        IsCooldownActive = false;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
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

        if (string.IsNullOrWhiteSpace(VerificationCode) || VerificationCode.Trim().Length != 6)
        {
            await ShowErrorAsync("Ingrese el código de verificación de 6 dígitos enviado a su correo.");
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
                ConfirmPassword = ConfirmPassword,
                VerificationCode = VerificationCode.Trim()
            };

            var result = await ApiService.RegisterAsync(request);

            if (result.Success)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "¡Registro Exitoso!",
                    result.Message ?? "Su cuenta ha sido creada. Ya puede iniciar sesión.",
                    "OK");

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
