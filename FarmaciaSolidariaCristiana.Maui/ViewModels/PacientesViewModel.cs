using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class PacientesViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<Patient> pacientes = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Patient> _allPacientes = new();

    public PacientesViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Pacientes";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await _authService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadPacientesAsync();
    }

    [RelayCommand]
    private async Task LoadPacientesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await _apiService.GetPacientesAsync();
            if (response.Success && response.Data != null)
            {
                _allPacientes = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar pacientes");
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
        await LoadPacientesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Pacientes = new ObservableCollection<Patient>(_allPacientes);
        }
        else
        {
            var filtered = _allPacientes
                .Where(p => p.NombreCompleto.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (p.Cedula?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (p.Telefono?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Pacientes = new ObservableCollection<Patient>(filtered);
        }
    }

    [RelayCommand]
    private async Task AddPacienteAsync()
    {
        if (!CanEdit) return;
        await Shell.Current.DisplayAlert("Agregar Paciente", "Funcionalidad próximamente", "OK");
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(Patient paciente)
    {
        if (paciente == null) return;
        
        var details = $"Nombre: {paciente.NombreCompleto}\n" +
                     $"Cédula: {paciente.Cedula ?? "N/A"}\n" +
                     $"Teléfono: {paciente.Telefono ?? "N/A"}\n" +
                     $"Email: {paciente.Email ?? "N/A"}\n" +
                     $"Dirección: {paciente.Direccion ?? "N/A"}\n" +
                     $"Activo: {(paciente.Activo ? "Sí" : "No")}";
                     
        await Shell.Current.DisplayAlert("Detalle de Paciente", details, "OK");
    }

    [RelayCommand]
    private async Task EditPacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null) return;
        await Shell.Current.DisplayAlert("Editar Paciente", $"Editando: {paciente.NombreCompleto}", "OK");
    }

    [RelayCommand]
    private async Task DeletePacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Paciente",
            $"¿Estás seguro de eliminar a '{paciente.NombreCompleto}'?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await _apiService.DeletePacienteAsync(paciente.Id);
                
                if (response.Success)
                {
                    _allPacientes.Remove(paciente);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Paciente eliminado correctamente", "OK");
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
