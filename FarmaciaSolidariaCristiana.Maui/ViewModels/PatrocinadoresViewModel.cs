using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class PatrocinadoresViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Sponsor> patrocinadores = new();

    [ObservableProperty]
    private bool isRefreshing;

    public PatrocinadoresViewModel(IApiService apiService)
    {
        _apiService = apiService;
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

            var response = await _apiService.GetPatrocinadoresAsync();
            if (response.Success && response.Data != null)
            {
                Patrocinadores = new ObservableCollection<Sponsor>(
                    response.Data.Where(p => p.Activo));
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
        if (sponsor == null || string.IsNullOrEmpty(sponsor.SitioWeb)) return;

        try
        {
            var uri = new Uri(sponsor.SitioWeb);
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"No se pudo abrir el sitio web: {ex.Message}");
        }
    }
}
