using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ChangePasswordViewModel : BaseViewModel
{
    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isCurrentPasswordHidden = true;

    [ObservableProperty]
    private bool isNewPasswordHidden = true;

    [ObservableProperty]
    private bool isConfirmPasswordHidden = true;

    public string CurrentPasswordIcon => IsCurrentPasswordHidden ? "👁️" : "👁️‍🗨️";
    public string NewPasswordIcon => IsNewPasswordHidden ? "👁️" : "👁️‍🗨️";
    public string ConfirmPasswordIcon => IsConfirmPasswordHidden ? "👁️" : "👁️‍🗨️";

    public ChangePasswordViewModel(IAuthService authService, IApiService apiService)
        : base(authService, apiService)
    {
        Title = "Cambiar Contraseña";
    }

    [RelayCommand]
    private void ToggleCurrentPasswordVisibility()
    {
        IsCurrentPasswordHidden = !IsCurrentPasswordHidden;
        OnPropertyChanged(nameof(CurrentPasswordIcon));
    }

    [RelayCommand]
    private void ToggleNewPasswordVisibility()
    {
        IsNewPasswordHidden = !IsNewPasswordHidden;
        OnPropertyChanged(nameof(NewPasswordIcon));
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;
        OnPropertyChanged(nameof(ConfirmPasswordIcon));
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            await ShowErrorAsync("Por favor, ingresa tu contraseña actual.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            await ShowErrorAsync("Por favor, ingresa tu nueva contraseña.");
            return;
        }

        if (NewPassword.Length < 6)
        {
            await ShowErrorAsync("La nueva contraseña debe tener al menos 6 caracteres.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            await ShowErrorAsync("Por favor, confirma tu nueva contraseña.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            await ShowErrorAsync("Las contraseñas no coinciden.");
            return;
        }

        if (CurrentPassword == NewPassword)
        {
            await ShowErrorAsync("La nueva contraseña debe ser diferente a la actual.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword
            };

            var result = await ApiService.ChangePasswordAsync(request);

            if (result.Success)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                    "¡Éxito!",
                    result.Message ?? "Tu contraseña ha sido actualizada correctamente.",
                    "OK");

                // Limpiar campos
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                // Volver a la página anterior
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al cambiar la contraseña");
            }
        });
    }
}
