using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel base con funcionalidades comunes
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;

    protected readonly IAuthService AuthService;
    protected readonly IApiService ApiService;

    public BaseViewModel(IAuthService authService, IApiService apiService)
    {
        AuthService = authService;
        ApiService = apiService;
    }

    protected async Task ExecuteAsync(Func<Task> operation, string? loadingMessage = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;
            
            await operation();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
    {
        if (IsBusy) return default;

        try
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;
            
            return await operation();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            await ShowErrorAsync(ex.Message);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static async Task ShowErrorAsync(string message)
    {
        await Shell.Current.DisplayAlert("Error", message, "OK");
    }

    protected static async Task ShowSuccessAsync(string message)
    {
        await Shell.Current.DisplayAlert("Éxito", message, "OK");
    }

    protected static async Task<bool> ShowConfirmAsync(string title, string message)
    {
        return await Shell.Current.DisplayAlert(title, message, "Sí", "No");
    }

    protected static async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    protected static async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
