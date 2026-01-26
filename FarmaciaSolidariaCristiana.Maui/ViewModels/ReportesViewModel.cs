using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ReportesViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private DateTime fechaInicio = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime fechaFin = DateTime.Today;

    [ObservableProperty]
    private bool canGenerateReports;

    public ReportesViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Reportes";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await _authService.GetUserInfoAsync();
        CanGenerateReports = userInfo?.Role == Constants.RoleAdmin || 
                             userInfo?.Role == Constants.RoleFarmaceutico ||
                             userInfo?.Role == Constants.RoleViewer;
    }

    [RelayCommand]
    private async Task GenerateTurnosReportAsync()
    {
        if (!CanGenerateReports) return;
        
        try
        {
            IsBusy = true;
            
            // For now, show a message. In the future, this could generate a PDF or navigate to a report view
            await Shell.Current.DisplayAlert(
                "Reporte de Turnos",
                $"Generando reporte del {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}\n\nFuncionalidad en desarrollo.",
                "OK");
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

    [RelayCommand]
    private async Task GenerateMedicamentosReportAsync()
    {
        if (!CanGenerateReports) return;
        
        try
        {
            IsBusy = true;
            
            await Shell.Current.DisplayAlert(
                "Reporte de Medicamentos",
                "Generando reporte de inventario de medicamentos\n\nFuncionalidad en desarrollo.",
                "OK");
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

    [RelayCommand]
    private async Task GenerateInsumosReportAsync()
    {
        if (!CanGenerateReports) return;
        
        try
        {
            IsBusy = true;
            
            await Shell.Current.DisplayAlert(
                "Reporte de Insumos",
                "Generando reporte de inventario de insumos\n\nFuncionalidad en desarrollo.",
                "OK");
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

    [RelayCommand]
    private async Task GenerateDonacionesReportAsync()
    {
        if (!CanGenerateReports) return;
        
        try
        {
            IsBusy = true;
            
            await Shell.Current.DisplayAlert(
                "Reporte de Donaciones",
                $"Generando reporte del {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}\n\nFuncionalidad en desarrollo.",
                "OK");
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

    [RelayCommand]
    private async Task GenerateEntregasReportAsync()
    {
        if (!CanGenerateReports) return;
        
        try
        {
            IsBusy = true;
            
            await Shell.Current.DisplayAlert(
                "Reporte de Entregas",
                $"Generando reporte del {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}\n\nFuncionalidad en desarrollo.",
                "OK");
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
