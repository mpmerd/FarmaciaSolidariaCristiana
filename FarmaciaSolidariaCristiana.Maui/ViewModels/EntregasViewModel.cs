using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class EntregasViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Delivery> entregas = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Delivery> _allEntregas = new();

    public EntregasViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Entregas";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadEntregasAsync();
    }

    [RelayCommand]
    private async Task LoadEntregasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetEntregasAsync();
            if (response.Success && response.Data != null)
            {
                _allEntregas = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar entregas");
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
        await LoadEntregasAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Entregas = new ObservableCollection<Delivery>(_allEntregas);
        }
        else
        {
            var filtered = _allEntregas
                .Where(e => (e.PatientName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (e.Comments?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Entregas = new ObservableCollection<Delivery>(filtered);
        }
    }

    [RelayCommand]
    private async Task AddEntregaAsync()
    {
        if (!CanEdit) return;
        await Shell.Current.DisplayAlert("Nueva Entrega", "Funcionalidad próximamente", "OK");
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(Delivery entrega)
    {
        if (entrega == null) return;
        
        var details = $"Paciente: {entrega.PatientName}\n" +
                     $"Artículo: {entrega.ItemName}\n" +
                     $"Cantidad: {entrega.Quantity}\n" +
                     $"Fecha: {entrega.DeliveryDate:dd/MM/yyyy}\n" +
                     $"Entregado por: {entrega.DeliveredBy ?? "N/A"}\n" +
                     $"Observaciones: {entrega.Comments ?? "N/A"}";
                     
        await Shell.Current.DisplayAlert("Detalle de Entrega", details, "OK");
    }

    [RelayCommand]
    private async Task DeleteEntregaAsync(Delivery entrega)
    {
        if (!CanEdit || entrega == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Entrega",
            $"¿Estás seguro de eliminar esta entrega?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeleteEntregaAsync(entrega.Id);
                
                if (response.Success)
                {
                    _allEntregas.Remove(entrega);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Entrega eliminada correctamente", "OK");
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
