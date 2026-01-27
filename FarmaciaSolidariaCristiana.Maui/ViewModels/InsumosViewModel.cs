using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class InsumosViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Supply> insumos = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Supply> _allInsumos = new();

    public InsumosViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Insumos";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadInsumosAsync();
    }

    [RelayCommand]
    private async Task LoadInsumosAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetInsumosAsync();
            if (response.Success && response.Data != null)
            {
                _allInsumos = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar insumos");
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
        await LoadInsumosAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Insumos = new ObservableCollection<Supply>(_allInsumos);
        }
        else
        {
            var filtered = _allInsumos
                .Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (i.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Insumos = new ObservableCollection<Supply>(filtered);
        }
    }

    [RelayCommand]
    private async Task AddInsumoAsync()
    {
        if (!CanEdit) return;
        
        // Navigate to add insumo page
        await Shell.Current.DisplayAlert("Agregar Insumo", "Funcionalidad próximamente", "OK");
    }

    [RelayCommand]
    private async Task EditInsumoAsync(Supply insumo)
    {
        if (!CanEdit || insumo == null) return;
        
        await Shell.Current.DisplayAlert("Editar Insumo", $"Editando: {insumo.Name}", "OK");
    }

    [RelayCommand]
    private async Task DeleteInsumoAsync(Supply insumo)
    {
        if (!CanEdit || insumo == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Insumo",
            $"¿Estás seguro de eliminar '{insumo.Name}'?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeleteInsumoAsync(insumo.Id);
                
                if (response.Success)
                {
                    _allInsumos.Remove(insumo);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Insumo eliminado correctamente", "OK");
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
