namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Modelo de Turno (coincide con TurnoDto del API)
/// </summary>
public class Turno
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public DateTime? FechaPreferida { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? NotasSolicitante { get; set; }
    public string? ComentariosFarmaceutico { get; set; }
    public DateTime? FechaRevision { get; set; }
    public List<TurnoMedicamento> Medicamentos { get; set; } = new();
    public List<TurnoInsumo> Insumos { get; set; } = new();
    public int DocumentosCount { get; set; }
    
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
/// Request para aprobar/rechazar turno
/// </summary>
public class GestionarTurnoRequest
{
    public int TurnoId { get; set; }
    public string Accion { get; set; } = string.Empty; // "aprobar" o "rechazar"
    public DateTime? FechaAsignada { get; set; }
    public string? Notas { get; set; }
}
