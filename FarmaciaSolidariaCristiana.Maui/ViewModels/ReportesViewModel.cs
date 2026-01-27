using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ReportesViewModel : BaseViewModel
{
    [ObservableProperty]
    private DateTime fechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime fechaFin = DateTime.Today;

    [ObservableProperty]
    private int selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    private int selectedMonth = DateTime.Today.Month;

    [ObservableProperty]
    private bool canGenerateReports;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public List<int> AvailableYears { get; } = Enumerable.Range(2020, DateTime.Today.Year - 2020 + 2).ToList();
    public List<string> AvailableMonths { get; } = new()
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    public ReportesViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Reportes";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanGenerateReports = userInfo?.Role == Constants.RoleAdmin || 
                             userInfo?.Role == Constants.RoleFarmaceutico ||
                             userInfo?.Role == Constants.RoleViewer;
    }

    [RelayCommand]
    private async Task GenerateEntregasReportAsync()
    {
        if (!CanGenerateReports) return;
        
        await ExecuteAsync(async () =>
        {
            StatusMessage = "Generando reporte de entregas...";
            
            var pdfBytes = await ApiService.DescargarReporteAsync("deliveries", FechaInicio, FechaFin);
            
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                await SaveAndOpenPdfAsync(pdfBytes, $"entregas_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}.pdf");
                StatusMessage = "Reporte generado exitosamente";
            }
            else
            {
                await ShowErrorAsync("No se pudo generar el reporte de entregas");
                StatusMessage = string.Empty;
            }
        });
    }

    [RelayCommand]
    private async Task GenerateDonacionesReportAsync()
    {
        if (!CanGenerateReports) return;
        
        await ExecuteAsync(async () =>
        {
            StatusMessage = "Generando reporte de donaciones...";
            
            var pdfBytes = await ApiService.DescargarReporteAsync("donations", FechaInicio, FechaFin);
            
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                await SaveAndOpenPdfAsync(pdfBytes, $"donaciones_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}.pdf");
                StatusMessage = "Reporte generado exitosamente";
            }
            else
            {
                await ShowErrorAsync("No se pudo generar el reporte de donaciones");
                StatusMessage = string.Empty;
            }
        });
    }

    [RelayCommand]
    private async Task GenerateMensualReportAsync()
    {
        if (!CanGenerateReports) return;
        
        await ExecuteAsync(async () =>
        {
            StatusMessage = $"Generando reporte mensual de {AvailableMonths[SelectedMonth - 1]} {SelectedYear}...";
            
            // Para el reporte mensual, usamos el mes y año seleccionados
            var startDate = new DateTime(SelectedYear, SelectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            var pdfBytes = await ApiService.DescargarReporteAsync("monthly", startDate, endDate);
            
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                await SaveAndOpenPdfAsync(pdfBytes, $"reporte_mensual_{SelectedYear}_{SelectedMonth:00}.pdf");
                StatusMessage = "Reporte generado exitosamente";
            }
            else
            {
                await ShowErrorAsync("No se pudo generar el reporte mensual");
                StatusMessage = string.Empty;
            }
        });
    }

    private async Task SaveAndOpenPdfAsync(byte[] pdfBytes, string fileName)
    {
        try
        {
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al abrir el PDF: {ex.Message}");
        }
    }
}
