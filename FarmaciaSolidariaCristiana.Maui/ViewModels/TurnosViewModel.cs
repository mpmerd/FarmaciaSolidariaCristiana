using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para la gestión de Turnos
/// </summary>
public partial class TurnosViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Turno> _turnos = new();

    [ObservableProperty]
    private Turno? _selectedTurno;

    [ObservableProperty]
    private bool _canManageTurnos;

    [ObservableProperty]
    private bool _canCreateTurno;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "Todos";

    public List<string> FilterOptions { get; } = new()
    {
        "Todos", "Pendiente", "Aprobado", "Rechazado", "Completado", "Cancelado"
    };

    private List<Turno> _allTurnos = new();

    public TurnosViewModel(IAuthService authService, IApiService apiService) 
        : base(authService, apiService)
    {
        Title = "Turnos";
    }

    [RelayCommand]
    public async Task LoadTurnosAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Verificar permisos
            CanManageTurnos = await AuthService.IsInAnyRoleAsync(
                Constants.RoleAdmin, Constants.RoleFarmaceutico);
            
            // Solo viewerpublic puede crear turnos
            var isPatient = await AuthService.IsInRoleAsync(Constants.RoleViewerPublic);
            CanCreateTurno = isPatient;

            // Cargar turnos según rol
            var result = CanManageTurnos 
                ? await ApiService.GetTurnosAsync()
                : await ApiService.GetMisTurnosAsync();

            if (result.Success && result.Data != null)
            {
                _allTurnos = result.Data.OrderByDescending(t => t.FechaSolicitud).ToList();
                ApplyFilters();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al cargar turnos");
            }
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allTurnos.AsEnumerable();

        // Filtrar por estado
        if (SelectedFilter != "Todos")
        {
            filtered = filtered.Where(t => t.Estado == SelectedFilter);
        }

        // Filtrar por texto de búsqueda
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(t => 
                t.Id.ToString().Contains(search) ||
                (t.UserEmail?.ToLower().Contains(search) ?? false));
        }

        Turnos = new ObservableCollection<Turno>(filtered);
    }

    [RelayCommand]
    private async Task SelectTurnoAsync(Turno turno)
    {
        SelectedTurno = turno;
        await NavigateToAsync($"turnodetail?id={turno.Id}");
    }

    [RelayCommand]
    private async Task CreateTurnoAsync()
    {
        if (!CanCreateTurno)
        {
            await ShowErrorAsync("No tiene permisos para crear turnos.");
            return;
        }
        
        await NavigateToAsync("nuevaturno");
    }

    [RelayCommand]
    private async Task AprobarTurnoAsync(Turno turno)
    {
        if (!CanManageTurnos)
        {
            await ShowErrorAsync("No tiene permisos para aprobar turnos.");
            return;
        }

        var fecha = await Shell.Current.DisplayPromptAsync(
            "Aprobar Turno",
            "Ingrese la fecha asignada (dd/MM/yyyy):",
            placeholder: DateTime.Today.AddDays(1).ToString("dd/MM/yyyy"));

        if (string.IsNullOrEmpty(fecha)) return;

        if (!DateTime.TryParse(fecha, out var fechaAsignada))
        {
            await ShowErrorAsync("Fecha inválida");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.AprobarTurnoAsync(turno.Id, fechaAsignada, null);
            
            if (result.Success)
            {
                await ShowSuccessAsync("Turno aprobado exitosamente");
                await LoadTurnosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al aprobar turno");
            }
        });
    }

    [RelayCommand]
    private async Task RechazarTurnoAsync(Turno turno)
    {
        if (!CanManageTurnos)
        {
            await ShowErrorAsync("No tiene permisos para rechazar turnos.");
            return;
        }

        var motivo = await Shell.Current.DisplayPromptAsync(
            "Rechazar Turno",
            "Ingrese el motivo del rechazo:",
            placeholder: "Motivo...");

        if (string.IsNullOrEmpty(motivo)) return;

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.RechazarTurnoAsync(turno.Id, motivo);
            
            if (result.Success)
            {
                await ShowSuccessAsync("Turno rechazado");
                await LoadTurnosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al rechazar turno");
            }
        });
    }

    [RelayCommand]
    private async Task CancelarTurnoAsync(Turno turno)
    {
        var confirm = await ShowConfirmAsync(
            "Cancelar Turno",
            "¿Está seguro de que desea cancelar este turno?");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.CancelarTurnoAsync(turno.Id);
            
            if (result.Success)
            {
                await ShowSuccessAsync("Turno cancelado");
                await LoadTurnosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al cancelar turno");
            }
        });
    }

    [RelayCommand]
    private async Task DescargarPdfAsync(Turno turno)
    {
        await ExecuteAsync(async () =>
        {
            var pdfBytes = await ApiService.DescargarTurnoPdfAsync(turno.Id);
            
            if (pdfBytes != null)
            {
                // Guardar en caché y abrir
                var fileName = $"turno_{turno.Id}.pdf";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllBytesAsync(filePath, pdfBytes);
                
                // Abrir el PDF
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            else
            {
                await ShowErrorAsync("No se pudo descargar el PDF");
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTurnosAsync();
    }
}
