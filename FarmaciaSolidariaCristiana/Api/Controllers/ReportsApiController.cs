using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Api.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para generación de reportes PDF
    /// Los PDFs se devuelven como byte[] en base64
    /// </summary>
    [Route("api/reports")]
    public class ReportsApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ReportsApiController(
            ApplicationDbContext context,
            ILogger<ReportsApiController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        private void AddPdfHeader(Document document, string title)
        {
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Crear tabla para logos
            var logoTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetTextAlignment(TextAlignment.CENTER);

            // Agregar logos
            try
            {
                var logoIglesiaPath = Path.Combine(_environment.WebRootPath, "images", "logo-iglesia.png");
                var logoAdrianoPath = Path.Combine(_environment.WebRootPath, "images", "logo-adriano.png");

                if (System.IO.File.Exists(logoIglesiaPath))
                {
                    var logoIglesia = new Image(ImageDataFactory.Create(logoIglesiaPath))
                        .SetHeight(50)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                    logoTable.AddCell(new Cell().Add(logoIglesia).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }
                else
                {
                    logoTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }

                if (System.IO.File.Exists(logoAdrianoPath))
                {
                    var logoAdriano = new Image(ImageDataFactory.Create(logoAdrianoPath))
                        .SetHeight(50)
                        .SetHorizontalAlignment(HorizontalAlignment.LEFT);
                    logoTable.AddCell(new Cell().Add(logoAdriano).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }
                else
                {
                    logoTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }
            }
            catch
            {
                // Si hay error cargando logos, continuar sin ellos
                logoTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                logoTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }

            document.Add(logoTable);

            // Título
            var titleParagraph = new Paragraph(title)
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(titleParagraph);

            // Subtítulo
            document.Add(new Paragraph("Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas y Adriano Solidario")
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            // Fecha
            document.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(new Paragraph("\n"));
        }

        /// <summary>
        /// Genera reporte PDF de entregas
        /// </summary>
        [HttpPost("deliveries")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<ReportResultDto>), 200)]
        public async Task<IActionResult> DeliveriesPdf([FromBody] DeliveriesReportRequest request)
        {
            var deliveries = _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Include(d => d.Patient)
                .AsQueryable();

            // Filtrar por tipo
            if (!string.IsNullOrEmpty(request.TipoFiltro))
            {
                if (request.TipoFiltro == "Medicamentos")
                {
                    deliveries = deliveries.Where(d => d.MedicineId != null);
                }
                else if (request.TipoFiltro == "Insumos")
                {
                    deliveries = deliveries.Where(d => d.SupplyId != null);
                }
            }

            if (request.MedicineId.HasValue)
            {
                deliveries = deliveries.Where(d => d.MedicineId == request.MedicineId.Value);
            }

            if (request.SupplyId.HasValue)
            {
                deliveries = deliveries.Where(d => d.SupplyId == request.SupplyId.Value);
            }

            if (request.StartDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate <= request.EndDate.Value);
            }

            var data = await deliveries.OrderByDescending(d => d.DeliveryDate).ToListAsync();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                AddPdfHeader(document, "Reporte de Entregas");

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3, 2, 2, 4 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell("Tipo");
                table.AddHeaderCell("Item");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Fecha");
                table.AddHeaderCell("Comentarios");

                foreach (var item in data)
                {
                    var itemType = item.Medicine != null ? "Med" : "Ins";
                    var itemName = item.Medicine?.Name ?? item.Supply?.Name ?? "N/A";
                    var itemUnit = item.Medicine?.Unit ?? item.Supply?.Unit ?? "";
                    
                    table.AddCell(itemType);
                    table.AddCell(itemName);
                    table.AddCell($"{item.Quantity} {itemUnit}");
                    table.AddCell(item.DeliveryDate.ToString("dd/MM/yyyy"));
                    table.AddCell(item.Comments ?? "");
                }

                document.Add(table);
                
                var totalMedicamentos = data.Where(d => d.MedicineId != null).Sum(d => d.Quantity);
                var totalInsumos = data.Where(d => d.SupplyId != null).Sum(d => d.Quantity);
                
                document.Add(new Paragraph($"\nTotal entregas: {data.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Total Medicamentos: {totalMedicamentos}"));
                document.Add(new Paragraph($"Total Insumos: {totalInsumos}"));
                
                document.Close();

                _logger.LogInformation("Deliveries PDF report generated via API");
                
                var fileName = $"Entregas_{DateTime.Now:yyyyMMdd}.pdf";
                return ApiOk(new ReportResultDto
                {
                    FileName = fileName,
                    ContentType = "application/pdf",
                    PdfBase64 = Convert.ToBase64String(ms.ToArray()),
                    GeneratedAt = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Genera reporte PDF de donaciones
        /// </summary>
        [HttpPost("donations")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<ReportResultDto>), 200)]
        public async Task<IActionResult> DonationsPdf([FromBody] DonationsReportRequest request)
        {
            var donations = _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.TipoFiltro))
            {
                if (request.TipoFiltro == "Medicamentos")
                {
                    donations = donations.Where(d => d.MedicineId != null);
                }
                else if (request.TipoFiltro == "Insumos")
                {
                    donations = donations.Where(d => d.SupplyId != null);
                }
            }

            if (request.MedicineId.HasValue)
            {
                donations = donations.Where(d => d.MedicineId == request.MedicineId.Value);
            }

            if (request.SupplyId.HasValue)
            {
                donations = donations.Where(d => d.SupplyId == request.SupplyId.Value);
            }

            if (request.StartDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate <= request.EndDate.Value);
            }

            var data = await donations.OrderByDescending(d => d.DonationDate).ToListAsync();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                AddPdfHeader(document, "Reporte de Donaciones");

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3, 2, 2, 4 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell("Tipo");
                table.AddHeaderCell("Item");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Fecha");
                table.AddHeaderCell("Nota Donante");

                foreach (var item in data)
                {
                    var itemType = item.Medicine != null ? "Med" : "Ins";
                    var itemName = item.Medicine?.Name ?? item.Supply?.Name ?? "N/A";
                    var itemUnit = item.Medicine?.Unit ?? item.Supply?.Unit ?? "";
                    
                    table.AddCell(itemType);
                    table.AddCell(itemName);
                    table.AddCell($"{item.Quantity} {itemUnit}");
                    table.AddCell(item.DonationDate.ToString("dd/MM/yyyy"));
                    table.AddCell(item.DonorNote ?? "");
                }

                document.Add(table);
                
                var totalMedicamentos = data.Where(d => d.MedicineId != null).Sum(d => d.Quantity);
                var totalInsumos = data.Where(d => d.SupplyId != null).Sum(d => d.Quantity);
                
                document.Add(new Paragraph($"\nTotal donaciones: {data.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Total Medicamentos: {totalMedicamentos}"));
                document.Add(new Paragraph($"Total Insumos: {totalInsumos}"));
                
                document.Close();

                _logger.LogInformation("Donations PDF report generated via API");
                
                var fileName = $"Donaciones_{DateTime.Now:yyyyMMdd}.pdf";
                return ApiOk(new ReportResultDto
                {
                    FileName = fileName,
                    ContentType = "application/pdf",
                    PdfBase64 = Convert.ToBase64String(ms.ToArray()),
                    GeneratedAt = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Genera reporte mensual PDF
        /// </summary>
        [HttpPost("monthly")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<ReportResultDto>), 200)]
        public async Task<IActionResult> MonthlyPdf([FromBody] MonthlyReportRequest request)
        {
            var startDate = new DateTime(request.Year, request.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var deliveries = await _context.Deliveries
                .Include(d => d.Medicine)
                .Where(d => d.DeliveryDate >= startDate && d.DeliveryDate <= endDate)
                .ToListAsync();

            var donations = await _context.Donations
                .Include(d => d.Medicine)
                .Where(d => d.DonationDate >= startDate && d.DonationDate <= endDate)
                .ToListAsync();

            var medicines = await _context.Medicines.ToListAsync();
            var supplies = await _context.Supplies.ToListAsync();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                AddPdfHeader(document, "Reporte Mensual");

                document.Add(new Paragraph($"{startDate:MMMM yyyy}")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph($"Total Donaciones: {donations.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Total Entregas: {deliveries.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Stock Actual Medicamentos: {medicines.Sum(m => m.StockQuantity)}"));
                document.Add(new Paragraph($"Stock Actual Insumos: {supplies.Sum(s => s.StockQuantity)}"));

                var inventoryHeader = new Paragraph("\nInventario Actual de Medicamentos:").SetFont(boldFont);
                document.Add(inventoryHeader);
                
                var stockTable = new Table(UnitValue.CreatePercentArray(new float[] { 4, 2 }))
                    .UseAllAvailableWidth();

                stockTable.AddHeaderCell("Medicamento");
                stockTable.AddHeaderCell("Stock");

                foreach (var med in medicines.OrderBy(m => m.Name))
                {
                    stockTable.AddCell(med.Name);
                    stockTable.AddCell($"{med.StockQuantity} {med.Unit}");
                }

                document.Add(stockTable);

                var suppliesHeader = new Paragraph("\nInventario Actual de Insumos:").SetFont(boldFont);
                document.Add(suppliesHeader);
                
                var suppliesTable = new Table(UnitValue.CreatePercentArray(new float[] { 4, 2 }))
                    .UseAllAvailableWidth();

                suppliesTable.AddHeaderCell("Insumo");
                suppliesTable.AddHeaderCell("Stock");

                foreach (var supply in supplies.OrderBy(s => s.Name))
                {
                    suppliesTable.AddCell(supply.Name);
                    suppliesTable.AddCell($"{supply.StockQuantity} {supply.Unit}");
                }

                document.Add(suppliesTable);
                document.Close();

                _logger.LogInformation("Monthly PDF report generated via API for {Year}/{Month}", request.Year, request.Month);
                
                var fileName = $"Mensual_{request.Year}_{request.Month:D2}.pdf";
                return ApiOk(new ReportResultDto
                {
                    FileName = fileName,
                    ContentType = "application/pdf",
                    PdfBase64 = Convert.ToBase64String(ms.ToArray()),
                    GeneratedAt = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Genera reporte de inventario actual
        /// </summary>
        [HttpGet("inventory")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<ReportResultDto>), 200)]
        public async Task<IActionResult> InventoryPdf()
        {
            var medicines = await _context.Medicines.OrderBy(m => m.Name).ToListAsync();
            var supplies = await _context.Supplies.OrderBy(s => s.Name).ToListAsync();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                AddPdfHeader(document, "Inventario Actual");

                // Resumen
                document.Add(new Paragraph($"Total Medicamentos: {medicines.Count} tipos, {medicines.Sum(m => m.StockQuantity)} unidades"));
                document.Add(new Paragraph($"Total Insumos: {supplies.Count} tipos, {supplies.Sum(s => s.StockQuantity)} unidades"));
                document.Add(new Paragraph("\n"));

                // Medicamentos
                var medHeader = new Paragraph("Medicamentos:").SetFont(boldFont);
                document.Add(medHeader);
                
                var medTable = new Table(UnitValue.CreatePercentArray(new float[] { 4, 1.5f, 1, 1.5f }))
                    .UseAllAvailableWidth();

                medTable.AddHeaderCell("Nombre");
                medTable.AddHeaderCell("Stock");
                medTable.AddHeaderCell("Unidad");
                medTable.AddHeaderCell("Descripción");

                foreach (var med in medicines)
                {
                    medTable.AddCell(med.Name);
                    medTable.AddCell(med.StockQuantity.ToString());
                    medTable.AddCell(med.Unit);
                    medTable.AddCell(med.Description ?? "N/A");
                }

                document.Add(medTable);

                // Insumos
                var supHeader = new Paragraph("\nInsumos:").SetFont(boldFont);
                document.Add(supHeader);
                
                var supTable = new Table(UnitValue.CreatePercentArray(new float[] { 4, 1.5f, 1, 3 }))
                    .UseAllAvailableWidth();

                supTable.AddHeaderCell("Nombre");
                supTable.AddHeaderCell("Stock");
                supTable.AddHeaderCell("Unidad");
                supTable.AddHeaderCell("Descripción");

                foreach (var sup in supplies)
                {
                    supTable.AddCell(sup.Name);
                    supTable.AddCell(sup.StockQuantity.ToString());
                    supTable.AddCell(sup.Unit);
                    supTable.AddCell(sup.Description ?? "");
                }

                document.Add(supTable);
                document.Close();

                _logger.LogInformation("Inventory PDF report generated via API");
                
                var fileName = $"Inventario_{DateTime.Now:yyyyMMdd}.pdf";
                return ApiOk(new ReportResultDto
                {
                    FileName = fileName,
                    ContentType = "application/pdf",
                    PdfBase64 = Convert.ToBase64String(ms.ToArray()),
                    GeneratedAt = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Obtiene datos de resumen del dashboard (sin PDF)
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), 200)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var stats = new DashboardStatsDto
            {
                TotalMedicines = await _context.Medicines.CountAsync(),
                TotalSupplies = await _context.Supplies.CountAsync(),
                TotalMedicinesStock = await _context.Medicines.SumAsync(m => m.StockQuantity),
                TotalSuppliesStock = await _context.Supplies.SumAsync(s => s.StockQuantity),
                TotalPatients = await _context.Patients.CountAsync(p => p.IsActive),
                
                DeliveriesToday = await _context.Deliveries.CountAsync(d => d.DeliveryDate.Date == today),
                DeliveriesThisMonth = await _context.Deliveries.CountAsync(d => d.DeliveryDate >= startOfMonth),
                DeliveriesThisYear = await _context.Deliveries.CountAsync(d => d.DeliveryDate >= startOfYear),
                
                DonationsToday = await _context.Donations.CountAsync(d => d.DonationDate.Date == today),
                DonationsThisMonth = await _context.Donations.CountAsync(d => d.DonationDate >= startOfMonth),
                DonationsThisYear = await _context.Donations.CountAsync(d => d.DonationDate >= startOfYear),
                
                MedicinesOutOfStock = await _context.Medicines.CountAsync(m => m.StockQuantity == 0),
                SuppliesOutOfStock = await _context.Supplies.CountAsync(s => s.StockQuantity == 0)
            };

            return ApiOk(stats);
        }
    }
}
