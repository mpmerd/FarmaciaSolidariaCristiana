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

    [ObservableProperty]
    private bool _isRefreshing;

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
        if (IsRefreshing) return;
        
        IsRefreshing = true;
        try
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Turnos] Error: {ex.Message}");
            await ShowErrorAsync("Error al cargar turnos");
        }
        finally
        {
            IsRefreshing = false;
        }
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
        // Si es viewerpublic y el turno está aprobado, mostrar opciones incluyendo cancelar
        else if (IsViewerPublic && turno.Estado == "Aprobado")
        {
            // Verificar si puede cancelar este turno
            var canCancelResult = await ApiService.PuedeCancelarTurnoAsync(turno.Id);
            
            if (canCancelResult.Success && canCancelResult.Data?.CanCancel == true)
            {
                var action = await Shell.Current.DisplayActionSheet(
                    $"Turno #{turno.Id}", "Cerrar", null,
                    "👁️ Ver detalles", "📄 Ver/Descargar PDF", "🚫 Cancelar turno");
                switch (action)
                {
                    case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                    case "📄 Ver/Descargar PDF": await DescargarPdfAsync(turno); break;
                    case "🚫 Cancelar turno": await CancelarTurnoAsync(turno); break;
                }
            }
            else
            {
                var action = await Shell.Current.DisplayActionSheet(
                    $"Turno #{turno.Id}", "Cerrar", null,
                    "👁️ Ver detalles", "📄 Ver/Descargar PDF");
                switch (action)
                {
                    case "👁️ Ver detalles": await VerDetallesTurnoAsync(turno); break;
                    case "📄 Ver/Descargar PDF": await DescargarPdfAsync(turno); break;
                }
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
            
            // Mostrar documentos adjuntos
            if (turno.Documentos?.Any() == true)
            {
                sb.AppendLine($"\n📎 Documentos adjuntos ({turno.Documentos.Count}):");
                foreach (var doc in turno.Documentos)
                {
                    sb.AppendLine($"  {doc.IconDisplay} {doc.DocumentType}: {doc.FileName}");
                }
            }
            else if (turno.DocumentosCount > 0)
            {
                sb.AppendLine($"\n📎 Documentos adjuntos: {turno.DocumentosCount}");
            }

            // Mostrar alerta con opción de ver documentos si existen
            if (turno.Documentos?.Any() == true)
            {
                var verDocs = await Shell.Current.DisplayAlert(
                    $"Turno #{turno.Id}", 
                    sb.ToString(), 
                    "Ver Documentos", 
                    "Cerrar");
                    
                if (verDocs)
                {
                    await VerDocumentosTurnoAsync(turno);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert($"Turno #{turno.Id}", sb.ToString(), "Cerrar");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
    }

    private async Task VerDocumentosTurnoAsync(Turno turno)
    {
        if (turno.Documentos == null || !turno.Documentos.Any())
        {
            await Shell.Current.DisplayAlert("Información", "Este turno no tiene documentos adjuntos", "OK");
            return;
        }

        // Crear lista de opciones con los documentos
        var options = turno.Documentos
            .Select(d => $"{d.IconDisplay} {d.DocumentType}: {d.FileName}")
            .ToArray();

        var selected = await Shell.Current.DisplayActionSheet(
            "Seleccione un documento para ver",
            "Cancelar",
            null,
            options);

        if (string.IsNullOrEmpty(selected) || selected == "Cancelar")
            return;

        // Encontrar el documento seleccionado
        var index = Array.IndexOf(options, selected);
        if (index >= 0 && index < turno.Documentos.Count)
        {
            var doc = turno.Documentos[index];
            await AbrirDocumentoAsync(doc);
        }
    }

    private async Task AbrirDocumentoAsync(Models.TurnoDocumento doc)
    {
        try
        {
            if (string.IsNullOrEmpty(doc.FullUrl))
            {
                await ShowErrorAsync("No se puede acceder al documento");
                return;
            }

            // Navegar a la página de visualización de documento
            var viewerPage = new Views.DocumentoViewerPage(doc);
            await Shell.Current.Navigation.PushAsync(viewerPage);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al abrir documento: {ex.Message}");
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
        
        // Navegar a la página de solicitar turno
        await Shell.Current.GoToAsync("SolicitarTurnoPage");
    }

    [RelayCommand]
    private async Task AprobarTurnoAsync(Turno turno)
    {
        if (!CanManageTurnos)
        {
            await ShowErrorAsync("No tiene permisos para aprobar turnos.");
            return;
        }

        // Confirmar aprobación
        var confirm = await ShowConfirmAsync(
            "Aprobar Turno",
            $"¿Desea aprobar el turno #{turno.Id}?\n\nLa fecha y hora se asignarán automáticamente al próximo slot disponible.");

        if (!confirm) return;

        // Pedir comentarios opcionales
        var comentarios = await Shell.Current.DisplayPromptAsync(
            "Comentarios (opcional)",
            "Puede agregar comentarios para el paciente:",
            placeholder: "Ej: Traer carnet de identidad...",
            maxLength: 500);

        // El usuario puede cancelar el prompt pero aún así aprobar
        // Si presiona Cancel en comentarios, comentarios será null (lo cual está bien)

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.AprobarTurnoAsync(turno.Id, comentarios);
            
            if (result.Success)
            {
                var fechaAsignada = result.Data?.FechaPreferida?.ToString("dd/MM/yyyy HH:mm") ?? "próximo disponible";
                await ShowSuccessAsync($"Turno aprobado exitosamente.\n\nFecha asignada: {fechaAsignada}");
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
        // Verificar primero si puede cancelar y mostrar info
        var canCancelResult = await ApiService.PuedeCancelarTurnoAsync(turno.Id);
        
        if (!canCancelResult.Success || canCancelResult.Data?.CanCancel != true)
        {
            var reason = canCancelResult.Data?.Reason ?? "No se puede cancelar este turno.";
            await ShowErrorAsync(reason);
            return;
        }

        var diasRestantes = canCancelResult.Data.DiasRestantes;
        var mensaje = diasRestantes > 7 
            ? $"Faltan {diasRestantes} días para tu turno.\n¿Estás seguro de que deseas cancelarlo?"
            : "¿Estás seguro de que deseas cancelar este turno?";

        var confirm = await ShowConfirmAsync("Cancelar Turno", mensaje);

        if (!confirm) return;

        // Pedir motivo de cancelación
        var motivo = await Shell.Current.DisplayPromptAsync(
            "Motivo de Cancelación",
            "Por favor, ingrese el motivo de la cancelación:",
            placeholder: "Ej: No podré asistir...",
            maxLength: 500);

        if (string.IsNullOrWhiteSpace(motivo))
        {
            await ShowErrorAsync("Debe proporcionar un motivo para cancelar el turno.");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await ApiService.CancelarTurnoAsync(turno.Id, motivo);
            
            if (result.Success)
            {
                // Mostrar notificación local de confirmación (no push)
                await ShowSuccessAsync("Tu turno ha sido cancelado exitosamente.\n\nLos farmacéuticos han sido notificados.");
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
                // Abrir en el visor de PDF integrado de la app
                var fileName = $"Turno_{turno.NumeroTurno ?? turno.Id}.pdf";
                var pdfViewerPage = new Views.PdfViewerPage(pdfBytes, fileName);
                await Shell.Current.Navigation.PushAsync(pdfViewerPage);
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
