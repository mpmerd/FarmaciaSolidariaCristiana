using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger, IWebHostEnvironment environment)
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

        public IActionResult Index()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name");
            ViewData["SupplyId"] = new SelectList(_context.Supplies, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeliveriesPDF(int? medicineId, DateTime? startDate, DateTime? endDate)
        {
            var deliveries = _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .AsQueryable();

            if (medicineId.HasValue)
            {
                deliveries = deliveries.Where(d => d.MedicineId == medicineId.Value);
            }

            if (startDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate <= endDate.Value);
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
                table.AddHeaderCell("Nota Paciente");

                foreach (var item in data)
                {
                    var itemType = item.Medicine != null ? "Med" : "Ins";
                    var itemName = item.Medicine?.Name ?? item.Supply?.Name ?? "N/A";
                    var itemUnit = item.Medicine?.Unit ?? item.Supply?.Unit ?? "";
                    
                    table.AddCell(itemType);
                    table.AddCell(itemName);
                    table.AddCell($"{item.Quantity} {itemUnit}");
                    table.AddCell(item.DeliveryDate.ToString("dd/MM/yyyy"));
                    table.AddCell(item.PatientNote ?? "");
                }

                document.Add(table);
                document.Add(new Paragraph($"\nTotal entregas: {data.Sum(d => d.Quantity)}"));
                
                document.Close();

                _logger.LogInformation("Deliveries PDF report generated");
                return File(ms.ToArray(), "application/pdf", $"Entregas_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DonationsPDF(int? medicineId, DateTime? startDate, DateTime? endDate)
        {
            var donations = _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .AsQueryable();

            if (medicineId.HasValue)
            {
                donations = donations.Where(d => d.MedicineId == medicineId.Value);
            }

            if (startDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate <= endDate.Value);
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
                document.Add(new Paragraph($"\nTotal donaciones: {data.Sum(d => d.Quantity)}"));
                
                document.Close();

                _logger.LogInformation("Donations PDF report generated");
                return File(ms.ToArray(), "application/pdf", $"Donaciones_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MonthlyPDF(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
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

                _logger.LogInformation("Monthly PDF report generated for {Year}/{Month}", year, month);
                return File(ms.ToArray(), "application/pdf", $"Mensual_{year}_{month:D2}.pdf");
            }
        }
    }
}
