using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para el Dashboard
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    [ObservableProperty]
    private string _userRole = string.Empty;

    [ObservableProperty]
    private int _turnosPendientes;

    [ObservableProperty]
    private int _medicamentosDisponibles;

    [ObservableProperty]
    private int _insumosDisponibles;

    [ObservableProperty]
    private int _entregasHoy;

    [ObservableProperty]
    private bool _canManageTurnos;

    [ObservableProperty]
    private bool _canViewReports;

    public DashboardViewModel(IAuthService authService, IApiService apiService) 
        : base(authService, apiService)
    {
        Title = "Inicio";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Cargar información del usuario
            var user = await AuthService.GetCurrentUserAsync();
            if (user != null)
            {
                WelcomeMessage = $"¡Hola, {user.UserName}!";
                UserRole = GetRoleDisplayName(user.Roles.FirstOrDefault() ?? "");
                
                // Permisos basados en rol
                CanManageTurnos = await AuthService.IsInAnyRoleAsync(
                    Constants.RoleAdmin, Constants.RoleFarmaceutico);
                CanViewReports = await AuthService.IsInAnyRoleAsync(
                    Constants.RoleAdmin, Constants.RoleViewer);
            }

            // Cargar estadísticas
            await LoadStatisticsAsync();
        });
    }

    private async Task LoadStatisticsAsync()
    {
        // Obtener turnos pendientes
        var turnosResult = CanManageTurnos 
            ? await ApiService.GetTurnosAsync()
            : await ApiService.GetMisTurnosAsync();
            
        if (turnosResult.Success && turnosResult.Data != null)
        {
            TurnosPendientes = turnosResult.Data.Count(t => t.Estado == "Pendiente");
        }

        // Obtener medicamentos disponibles
        var medsResult = await ApiService.GetMedicamentosAsync();
        if (medsResult.Success && medsResult.Data != null)
        {
            MedicamentosDisponibles = medsResult.Data.Count(m => m.Stock > 0);
        }

        // Obtener insumos disponibles
        var suppliesResult = await ApiService.GetInsumosAsync();
        if (suppliesResult.Success && suppliesResult.Data != null)
        {
            InsumosDisponibles = suppliesResult.Data.Count(s => s.Stock > 0);
        }

        // Entregas de hoy (solo para roles con acceso)
        if (await AuthService.IsInAnyRoleAsync(Constants.RoleAdmin, Constants.RoleFarmaceutico))
        {
            var deliveriesResult = await ApiService.GetEntregasAsync();
            if (deliveriesResult.Success && deliveriesResult.Data != null)
            {
                EntregasHoy = deliveriesResult.Data.Count(d => 
                    d.DeliveryDate.Date == DateTime.Today);
            }
        }
    }

    private static string GetRoleDisplayName(string role)
    {
        return role.ToLower() switch
        {
            "admin" => "Administrador",
            "farmaceutico" => "Farmacéutico",
            "viewer" => "Visualizador",
            "viewerpublic" => "Paciente",
            _ => "Usuario"
        };
    }

    [RelayCommand]
    private async Task NavigateToTurnosAsync()
    {
        await NavigateToAsync("//turnos");
    }

    [RelayCommand]
    private async Task NavigateToMedicamentosAsync()
    {
        await NavigateToAsync("//medicamentos");
    }

    [RelayCommand]
    private async Task NavigateToInsumosAsync()
    {
        await NavigateToAsync("//insumos");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }
}
