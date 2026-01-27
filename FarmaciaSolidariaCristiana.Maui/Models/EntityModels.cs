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
    public string? MainDiagnosis { get; set; }
    public string? KnownAllergies { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int DeliveriesCount { get; set; }
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
