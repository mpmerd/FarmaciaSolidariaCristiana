using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO para resultado de reporte PDF
    /// </summary>
    public class ReportResultDto
    {
        /// <summary>
        /// Nombre sugerido del archivo
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Tipo MIME del archivo (application/pdf)
        /// </summary>
        public string ContentType { get; set; } = "application/pdf";
        
        /// <summary>
        /// Contenido del PDF codificado en Base64
        /// </summary>
        public string PdfBase64 { get; set; } = string.Empty;
        
        /// <summary>
        /// Fecha y hora de generación
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Request para reporte de entregas
    /// </summary>
    public class DeliveriesReportRequest
    {
        /// <summary>
        /// ID del medicamento a filtrar (opcional)
        /// </summary>
        public int? MedicineId { get; set; }
        
        /// <summary>
        /// ID del insumo a filtrar (opcional)
        /// </summary>
        public int? SupplyId { get; set; }
        
        /// <summary>
        /// Fecha inicial del rango (opcional)
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Fecha final del rango (opcional)
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Tipo de filtro: "Medicamentos", "Insumos", o null para todos
        /// </summary>
        public string? TipoFiltro { get; set; }
    }

    /// <summary>
    /// Request para reporte de donaciones
    /// </summary>
    public class DonationsReportRequest
    {
        public int? MedicineId { get; set; }
        public int? SupplyId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TipoFiltro { get; set; }
    }

    /// <summary>
    /// Request para reporte mensual
    /// </summary>
    public class MonthlyReportRequest
    {
        [Required]
        [Range(2020, 2100)]
        public int Year { get; set; }
        
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }
    }

    /// <summary>
    /// DTO con estadísticas para dashboard
    /// </summary>
    public class DashboardStatsDto
    {
        // Inventario
        public int TotalMedicines { get; set; }
        public int TotalSupplies { get; set; }
        public int TotalMedicinesStock { get; set; }
        public int TotalSuppliesStock { get; set; }
        public int TotalPatients { get; set; }
        
        // Entregas
        public int DeliveriesToday { get; set; }
        public int DeliveriesThisMonth { get; set; }
        public int DeliveriesThisYear { get; set; }
        
        // Donaciones
        public int DonationsToday { get; set; }
        public int DonationsThisMonth { get; set; }
        public int DonationsThisYear { get; set; }
        
        // Alertas
        public int MedicinesOutOfStock { get; set; }
        public int SuppliesOutOfStock { get; set; }
    }
}
