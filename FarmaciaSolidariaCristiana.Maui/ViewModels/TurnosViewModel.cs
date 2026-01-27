using System.Collections.ObjectModel;
using System.Text;
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
    private bool _isViewerPublic;
    
    [ObservableProperty]
    private bool _isAdmin;

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
            
            // Solo Admin puede reprogramar turnos
            IsAdmin = await AuthService.IsInRoleAsync(Constants.RoleAdmin);
            
            // Solo viewerpublic puede crear turnos
            IsViewerPublic = await AuthService.IsInRoleAsync(Constants.RoleViewerPublic);
            CanCreateTurno = IsViewerPublic;

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
        if (turno == null) return;
        SelectedTurno = turno;
        
        // Si es admin/farmacéutico y el turno está pendiente, mostrar opciones
        if (CanManageTurnos && turno.Estado == "Pendiente")
        {
            var action = await Shell.Current.DisplayActionSheet(
                $"Turno #{turno.Id}", "Cancelar", null,
                "👁️ Ver detalles", "✅ Aprobar", "❌ Rechazar");
            switch (action)
            {
                case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                case "✅ Aprobar": await AprobarTurnoAsync(turno); break;
                case "❌ Rechazar": await RechazarTurnoAsync(turno); break;
            }
        }
        // Si es admin y el turno está aprobado, mostrar opciones incluyendo reprogramar
        else if (IsAdmin && turno.Estado == "Aprobado")
        {
            var action = await Shell.Current.DisplayActionSheet(
                $"Turno #{turno.Id}", "Cancelar", null,
                "👁️ Ver detalles", "📅 Reprogramar", "📄 Descargar PDF");
            switch (action)
            {
                case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                case "📅 Reprogramar": await ReprogramarTurnoAsync(turno); break;
                case "📄 Descargar PDF": await DescargarPdfAsync(turno); break;
            }
        }
        // Si es farmacéutico y el turno está aprobado (sin reprogramar)
        else if (CanManageTurnos && turno.Estado == "Aprobado")
        {
            var action = await Shell.Current.DisplayActionSheet(
                $"Turno #{turno.Id}", "Cancelar", null,
                "👁️ Ver detalles", "📄 Descargar PDF");
            switch (action)
            {
                case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                case "📄 Descargar PDF": await DescargarPdfAsync(turno); break;
            }
        }
        // Si es viewerpublic y el turno está aprobado, mostrar opción de PDF
        else if (IsViewerPublic && turno.Estado == "Aprobado")
        {
            var action = await Shell.Current.DisplayActionSheet(
                $"Turno #{turno.Id}", "Cancelar", null,
                "👁️ Ver detalles", "📄 Descargar PDF");
            switch (action)
            {
                case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                case "📄 Descargar PDF": await DescargarPdfAsync(turno); break;
            }
        }
        else
        {
            // Para otros casos solo mostrar detalles
            await VerDetallesTurnoAsync(turno);
        }
    }
    
    [RelayCommand]
    private async Task VerDetallesTurnoAsync(Turno turno)
    {
        if (turno == null) return;
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📧 Usuario: {turno.UserEmail}");
            sb.AppendLine($"📅 Solicitado: {turno.FechaSolicitud:dd/MM/yyyy HH:mm}");
            if (turno.FechaPreferida.HasValue)
                sb.AppendLine($"📆 Fecha preferida: {turno.FechaPreferida:dd/MM/yyyy}");
            sb.AppendLine($"📊 Estado: {turno.Estado}");
            if (turno.FechaRevision.HasValue)
                sb.AppendLine($"✅ Revisado: {turno.FechaRevision:dd/MM/yyyy HH:mm}");
            if (!string.IsNullOrEmpty(turno.NotasSolicitante))
                sb.AppendLine($"\n📝 Notas:\n{turno.NotasSolicitante}");
            if (!string.IsNullOrEmpty(turno.ComentariosFarmaceutico))
                sb.AppendLine($"\n💊 Comentarios:\n{turno.ComentariosFarmaceutico}");
            if (turno.Medicamentos?.Any() == true)
            {
                sb.AppendLine("\n💊 Medicamentos:");
                foreach (var med in turno.Medicamentos)
                {
                    var aprobado = med.CantidadAprobada.HasValue ? $" (Aprob: {med.CantidadAprobada})" : "";
                    sb.AppendLine($"  • {med.MedicineName}: {med.CantidadSolicitada}{aprobado}");
                }
            }
            if (turno.Insumos?.Any() == true)
            {
                sb.AppendLine("\n🏥 Insumos:");
                foreach (var ins in turno.Insumos)
                {
                    var aprobado = ins.CantidadAprobada.HasValue ? $" (Aprob: {ins.CantidadAprobada})" : "";
                    sb.AppendLine($"  • {ins.SupplyName}: {ins.CantidadSolicitada}{aprobado}");
                }
            }
            if (turno.DocumentosCount > 0)
                sb.AppendLine($"\n📎 Documentos adjuntos: {turno.DocumentosCount}");

            await Shell.Current.DisplayAlert($"Turno #{turno.Id}", sb.ToString(), "Cerrar");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateTurnoAsync()
    {
        if (!CanCreateTurno)
        {
            await ShowErrorAsync("No tiene permisos para crear turnos.");
            return;
        }
        
        // Por ahora mostrar mensaje informativo
        await Shell.Current.DisplayAlert(
            "Solicitar Turno", 
            "La funcionalidad de solicitar turno desde la app móvil estará disponible próximamente.\n\nPor ahora, puede solicitar turnos desde la aplicación web.", 
            "Entendido");
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
    private async Task ReprogramarTurnoAsync(Turno turno)
    {
        if (!IsAdmin)
        {
            await ShowErrorAsync("Solo el administrador puede reprogramar turnos.");
            return;
        }

        // Mostrar fecha actual
        var fechaActual = turno.FechaPreferida?.ToString("dd/MM/yyyy") ?? "Sin fecha";
        
        // Pedir nueva fecha (en formato simple por limitaciones de MAUI)
        var nuevaFechaStr = await Shell.Current.DisplayPromptAsync(
            "Reprogramar Turno",
            $"Fecha actual: {fechaActual}\n\nIngrese la nueva fecha (dd/MM/yyyy):",
            placeholder: DateTime.Today.AddDays(1).ToString("dd/MM/yyyy"));

        if (string.IsNullOrEmpty(nuevaFechaStr)) return;

        // Parsear la fecha
        if (!DateTime.TryParseExact(nuevaFechaStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var nuevaFecha))
        {
            await ShowErrorAsync("Formato de fecha inválido. Use dd/MM/yyyy (ej: 25/12/2025)");
            return;
        }

        if (nuevaFecha.Date <= DateTime.Today)
        {
            await ShowErrorAsync("La fecha debe ser posterior a hoy");
            return;
        }

        // Pedir motivo opcional
        var motivo = await Shell.Current.DisplayPromptAsync(
            "Reprogramar Turno",
            "Motivo de la reprogramación (opcional):",
            placeholder: "Motivo...");

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.ReprogramarTurnoAsync(turno.Id, nuevaFecha, motivo);
            
            if (result.Success)
            {
                await ShowSuccessAsync($"Turno reprogramado para el {nuevaFecha:dd/MM/yyyy}");
                await LoadTurnosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al reprogramar turno");
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
        // Solo permitir descargar PDF para turnos aprobados
        if (turno.Estado != "Aprobado")
        {
            await ShowErrorAsync("El PDF solo está disponible para turnos aprobados.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var pdfBytes = await ApiService.DescargarTurnoPdfAsync(turno.Id);
            
            if (pdfBytes != null && pdfBytes.Length > 0)
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
                await ShowErrorAsync("No se pudo descargar el PDF del turno.");
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadTurnosAsync();
    }
}
