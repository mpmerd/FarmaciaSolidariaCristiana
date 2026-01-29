namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Modelo de Turno (coincide con TurnoDto del API)
/// </summary>
public class Turno
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public int? NumeroTurno { get; set; }
    public DateTime? FechaPreferida { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? NotasSolicitante { get; set; }
    public string? ComentariosFarmaceutico { get; set; }
    public DateTime? FechaRevision { get; set; }
    public string? TurnoPdfPath { get; set; }
    public List<TurnoMedicamento> Medicamentos { get; set; } = new();
    public List<TurnoInsumo> Insumos { get; set; } = new();
    public int DocumentosCount { get; set; }
    public List<TurnoDocumento> Documentos { get; set; } = new();
    
    // Propiedades calculadas para UI
    public string EstadoDisplay => Estado switch
    {
        "Pendiente" => "⏳ Pendiente",
        "Aprobado" => "✅ Aprobado",
        "Rechazado" => "❌ Rechazado",
        "Completado" => "✔️ Completado",
        "Cancelado" => "🚫 Cancelado",
        _ => Estado
    };
    
    public Color EstadoColor => Estado switch
    {
        "Pendiente" => Colors.Orange,
        "Aprobado" => Colors.Green,
        "Rechazado" => Colors.Red,
        "Completado" => Colors.Blue,
        "Cancelado" => Colors.Gray,
        _ => Colors.Black
    };

    public bool HasPdf => !string.IsNullOrEmpty(TurnoPdfPath) && Estado == "Aprobado";
    
    public string PdfUrl => string.IsNullOrEmpty(TurnoPdfPath) 
        ? string.Empty 
        : $"{Helpers.Constants.ApiBaseUrl}/{TurnoPdfPath.TrimStart('/')}";
}

/// <summary>
/// Medicamento en turno (coincide con TurnoMedicamentoDto)
/// </summary>
public class TurnoMedicamento
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int CantidadSolicitada { get; set; }
    public int? CantidadAprobada { get; set; }
    public bool DisponibleAlSolicitar { get; set; }
}

/// <summary>
/// Insumo en turno (coincide con TurnoInsumoDto)
/// </summary>
public class TurnoInsumo
{
    public int SupplyId { get; set; }
    public string SupplyName { get; set; } = string.Empty;
    public int CantidadSolicitada { get; set; }
    public int? CantidadAprobada { get; set; }
    public bool DisponibleAlSolicitar { get; set; }
}

/// <summary>
/// Request para crear turno
/// </summary>
public class CrearTurnoRequest
{
    public DateTime? FechaPreferida { get; set; }
    public string? HoraPreferida { get; set; }
    public string? Notas { get; set; }
    public List<TurnoItemRequest> Medicamentos { get; set; } = new();
    public List<TurnoItemRequest> Insumos { get; set; } = new();
}

public class TurnoItemRequest
{
    public int Id { get; set; }
    public int Cantidad { get; set; }
}

/// <summary>
/// Response de documento de turno subido
/// </summary>
public class TurnoDocumentoResponse
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public DateTime UploadDate { get; set; }
}

/// <summary>
/// Documento adjunto a un turno
/// </summary>
public class TurnoDocumento
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public DateTime UploadDate { get; set; }
    
    // Propiedades calculadas para UI
    public string FileSizeDisplay
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F1} MB";
        }
    }
    
    public string IconDisplay => ContentType?.StartsWith("image/") == true ? "🖼️" : "📄";
    
    public string FullUrl => string.IsNullOrEmpty(FilePath) 
        ? string.Empty 
        : $"{Helpers.Constants.ApiBaseUrl}/{FilePath.TrimStart('/')}";
}

/// <summary>
/// Request para aprobar/rechazar turno
/// </summary>
public class GestionarTurnoRequest
{
    public int TurnoId { get; set; }
    public string Accion { get; set; } = string.Empty; // "aprobar" o "rechazar"
    public DateTime? FechaAsignada { get; set; }
    public string? Notas { get; set; }
}

/// <summary>
/// Respuesta de si se puede cancelar un turno
/// </summary>
public class CanCancelTurnoResponse
{
    public bool CanCancel { get; set; }
    public string? Reason { get; set; }
    public int DiasRestantes { get; set; }
}
