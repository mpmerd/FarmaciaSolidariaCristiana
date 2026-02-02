namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Resultado paginado genérico (coincide con API)
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Modelo de Medicamento (coincide con MedicineDto del API)
/// </summary>
public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "comprimidos";
    public string? NationalCode { get; set; }
    
    // Propiedades calculadas para UI
    public bool LowStock => StockQuantity <= 5 && StockQuantity > 0;
    public bool OutOfStock => StockQuantity == 0;
    public string StockStatus => OutOfStock ? "Sin stock" : (LowStock ? "Stock bajo" : "Disponible");
    public Color StockColor => OutOfStock ? Colors.Red : (LowStock ? Colors.Orange : Colors.Green);
}

/// <summary>
/// Modelo de Insumo (coincide con SupplyDto del API)
/// </summary>
public class Supply
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "Unidades";
    
    // Propiedades calculadas para UI
    public bool LowStock => StockQuantity <= 5 && StockQuantity > 0;
    public bool OutOfStock => StockQuantity == 0;
    public string StockStatus => OutOfStock ? "Sin stock" : (LowStock ? "Stock bajo" : "Disponible");
    public Color StockColor => OutOfStock ? Colors.Red : (LowStock ? Colors.Orange : Colors.Green);
}

/// <summary>
/// Modelo de Donación (coincide con DonationDto del API)
/// </summary>
public class Donation
{
    public int Id { get; set; }
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? SupplyId { get; set; }
    public string? SupplyName { get; set; }
    public int Quantity { get; set; }
    public DateTime DonationDate { get; set; }
    public string? DonorNote { get; set; }
    public string? Comments { get; set; }
    
    // Propiedades calculadas para UI
    public string ItemName => MedicineName ?? SupplyName ?? "Sin especificar";
    public string ItemType => MedicineId.HasValue ? "Medicamento" : "Insumo";
}

/// <summary>
/// Modelo de Entrega (coincide con DeliveryDto del API)
/// </summary>
public class Delivery
{
    public int Id { get; set; }
    public string PatientIdentification { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? SupplyId { get; set; }
    public string? SupplyName { get; set; }
    public int? TurnoId { get; set; }
    public int Quantity { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Dosage { get; set; }
    public string? TreatmentDuration { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DeliveredBy { get; set; }
    public string? Comments { get; set; }
    
    // Propiedades calculadas para UI
    public string ItemName => MedicineName ?? SupplyName ?? "Sin especificar";
    public string ItemType => MedicineId.HasValue ? "Medicamento" : "Insumo";
}

/// <summary>
/// Modelo de Documento de Paciente
/// </summary>
public class PatientDocument
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? Notes { get; set; }
    public DateTime UploadedAt { get; set; }
    
    /// <summary>
    /// URL completa del documento para visualización
    /// </summary>
    public string? FullUrl => string.IsNullOrEmpty(FilePath) 
        ? null 
        : (FilePath.StartsWith("http") 
            ? FilePath 
            : $"{Helpers.Constants.ApiBaseUrl}/{FilePath.TrimStart('/')}");
}

/// <summary>
/// Modelo de Paciente (coincide con PatientDto del API)
/// </summary>
public class Patient
{
    public int Id { get; set; }
    public string IdentificationDocument { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Municipality { get; set; }
    public string? Province { get; set; }
    
    // Datos clínicos
    public string? MainDiagnosis { get; set; }
    public string? AssociatedPathologies { get; set; }
    public string? KnownAllergies { get; set; }
    public string? CurrentTreatments { get; set; }
    
    // Datos vitales
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    
    // Observaciones
    public string? Observations { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int DeliveriesCount { get; set; }
    
    // Propiedad calculada para mostrar presión arterial
    public string BloodPressureDisplay => 
        BloodPressureSystolic.HasValue && BloodPressureDiastolic.HasValue 
            ? $"{BloodPressureSystolic}/{BloodPressureDiastolic} mmHg" 
            : "N/A";
}

/// <summary>
/// Modelo de Patrocinador (coincide con SponsorDto del API)
/// </summary>
public class Sponsor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedDate { get; set; }
    
    // Propiedades de conveniencia para la vista
    public string Nombre => Name;
    public string? Descripcion => Description;
    public string? SitioWeb => null; // El API no tiene este campo
    
    /// <summary>
    /// URL completa del logo
    /// </summary>
    public string? LogoUrl => string.IsNullOrEmpty(LogoPath) 
        ? null 
        : (LogoPath.StartsWith("http") 
            ? LogoPath 
            : $"{Helpers.Constants.ApiBaseUrl}/{LogoPath.TrimStart('/')}");
}

/// <summary>
/// Resultado de búsqueda de documentos de turnos
/// </summary>
public class TurnoDocumentsSearchResult
{
    public bool Found { get; set; }
    public int Count { get; set; }
    public List<TurnoDocumentItem> Documents { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Documento de turno para importar
/// </summary>
public class TurnoDocumentItem
{
    public int Id { get; set; }
    public int TurnoId { get; set; }
    public int? NumeroTurno { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// URL completa del documento
    /// </summary>
    public string? FullUrl => string.IsNullOrEmpty(FilePath) 
        ? null 
        : $"{Helpers.Constants.ApiBaseUrl}/{FilePath.TrimStart('/')}";
    
    /// <summary>
    /// Descripción para mostrar en la UI
    /// </summary>
    public string DisplayDescription => 
        $"Turno #{NumeroTurno ?? 0} ({FechaSolicitud:dd/MM/yyyy})";
}

/// <summary>
/// Item para importar documento de turno
/// </summary>
public class TurnoDocumentImportItem
{
    public int TurnoId { get; set; }
    public int? NumeroTurno { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
}

/// <summary>
/// Resultado de importación de documentos
/// </summary>
public class ImportDocumentsResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public List<PatientDocument> ImportedDocuments { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
}
