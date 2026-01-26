using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class DonacionesViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<Donation> donaciones = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Donation> _allDonaciones = new();

    public DonacionesViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Donaciones";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await _authService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadDonacionesAsync();
    }

    [RelayCommand]
    private async Task LoadDonacionesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await _apiService.GetDonacionesAsync();
            if (response.Success && response.Data != null)
            {
                _allDonaciones = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar donaciones");
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
        await LoadDonacionesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Donaciones = new ObservableCollection<Donation>(_allDonaciones);
        }
        else
        {
            var filtered = _allDonaciones
                .Where(d => (d.Donante?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (d.Descripcion?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Donaciones = new ObservableCollection<Donation>(filtered);
        }
    }

    [RelayCommand]
    private async Task AddDonacionAsync()
    {
        if (!CanEdit) return;
        await Shell.Current.DisplayAlert("Agregar Donación", "Funcionalidad próximamente", "OK");
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(Donation donacion)
    {
        if (donacion == null) return;
        
        var details = $"Donante: {donacion.Donante}\n" +
                     $"Fecha: {donacion.FechaDonacion:dd/MM/yyyy}\n" +
                     $"Descripción: {donacion.Descripcion ?? "N/A"}";
                     
        await Shell.Current.DisplayAlert("Detalle de Donación", details, "OK");
    }

    [RelayCommand]
    private async Task DeleteDonacionAsync(Donation donacion)
    {
        if (!CanEdit || donacion == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Donación",
            "¿Estás seguro de eliminar esta donación?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await _apiService.DeleteDonacionAsync(donacion.Id);
                
                if (response.Success)
                {
                    _allDonaciones.Remove(donacion);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Donación eliminada correctamente", "OK");
                }
                else
                {
                    await ShowErrorAsync(response.Message ?? "Error al eliminar");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
