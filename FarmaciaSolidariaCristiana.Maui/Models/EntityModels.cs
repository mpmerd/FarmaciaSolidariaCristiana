namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Modelo de Medicamento
/// </summary>
public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ActiveIngredient { get; set; }
    public string? Laboratory { get; set; }
    public string? Presentation { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? CimaCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Propiedades calculadas
    public bool LowStock => Stock <= MinimumStock && Stock > 0;
    public bool OutOfStock => Stock == 0;
    public string StockStatus => OutOfStock ? "Sin stock" : (LowStock ? "Stock bajo" : "Disponible");
    public Color StockColor => OutOfStock ? Colors.Red : (LowStock ? Colors.Orange : Colors.Green);
}

/// <summary>
/// Modelo de Insumo
/// </summary>
public class Supply
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public string? Unit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public bool LowStock => Stock <= MinimumStock && Stock > 0;
    public bool OutOfStock => Stock == 0;
    public string StockStatus => OutOfStock ? "Sin stock" : (LowStock ? "Stock bajo" : "Disponible");
    public Color StockColor => OutOfStock ? Colors.Red : (LowStock ? Colors.Orange : Colors.Green);
}

/// <summary>
/// Modelo de Donación
/// </summary>
public class Donation
{
    public int Id { get; set; }
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? SupplyId { get; set; }
    public string? SupplyName { get; set; }
    public int Quantity { get; set; }
    public int? SponsorId { get; set; }
    public string? SponsorName { get; set; }
    public DateTime DonationDate { get; set; }
    public string? Notes { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public string ItemName => MedicineName ?? SupplyName ?? "Sin especificar";
    public string ItemType => MedicineId.HasValue ? "Medicamento" : "Insumo";
}

/// <summary>
/// Modelo de Entrega
/// </summary>
public class Delivery
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientIdentification { get; set; }
    public int? MedicineId { get; set; }
    public string? MedicineName { get; set; }
    public int? SupplyId { get; set; }
    public string? SupplyName { get; set; }
    public int Quantity { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public string DeliveredBy { get; set; } = string.Empty;
    public int? TurnoId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string ItemName => MedicineName ?? SupplyName ?? "Sin especificar";
    public string ItemType => MedicineId.HasValue ? "Medicamento" : "Insumo";
}

/// <summary>
/// Modelo de Paciente
/// </summary>
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? MedicalNotes { get; set; }
    public bool IsActive { get; set; }
    public bool IdentificationRequired { get; set; }
    public string? IdentificationDocumentPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Modelo de Patrocinador
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
}
