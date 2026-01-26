namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Modelo de Turno
/// </summary>
public class Turno
{
    public int Id { get; set; }
    public int NumeroTurno { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAsignada { get; set; }
    public DateTime? FechaPreferida { get; set; }
    public string? HoraPreferida { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public string? NotasRechazo { get; set; }
    public string? NotasFarmaceutico { get; set; }
    public bool TieneDocumentos { get; set; }
    public List<TurnoMedicamento> Medicamentos { get; set; } = new();
    public List<TurnoInsumo> Insumos { get; set; } = new();
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
}

public class TurnoMedicamento
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class TurnoInsumo
{
    public int Id { get; set; }
    public int SupplyId { get; set; }
    public string SupplyName { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class TurnoDocumento
{
    public int Id { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime FechaSubida { get; set; }
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
