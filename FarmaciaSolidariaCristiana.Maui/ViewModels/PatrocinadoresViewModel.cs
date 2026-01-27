using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class PatrocinadoresViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Sponsor> patrocinadores = new();

    [ObservableProperty]
    private bool isRefreshing;

    public PatrocinadoresViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Patrocinadores";
    }

    public async Task InitializeAsync()
    {
        await LoadPatrocinadoresAsync();
    }

    [RelayCommand]
    private async Task LoadPatrocinadoresAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetPatrocinadoresAsync();
            if (response.Success && response.Data != null)
            {
                Patrocinadores = new ObservableCollection<Sponsor>(
                    response.Data.Where(p => p.IsActive));
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar patrocinadores");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPatrocinadoresAsync();
    }

    [RelayCommand]
    private async Task OpenWebsiteAsync(Sponsor sponsor)
    {
        if (sponsor == null) return;
        
        // Show sponsor details since we don't have a website field
        await Shell.Current.DisplayAlert(
            sponsor.Name, 
            sponsor.Description ?? "Sin descripción disponible", 
            "OK");
    }
}
