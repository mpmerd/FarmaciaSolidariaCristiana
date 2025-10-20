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

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize(Roles = "Farmaceutico,Viewer")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeliveriesPDF(int? medicineId, DateTime? startDate, DateTime? endDate)
        {
            var deliveries = _context.Deliveries.Include(d => d.Medicine).AsQueryable();

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
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                var title = new Paragraph("Reporte de Entregas")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);
                
                document.Add(new Paragraph("Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));

                document.Add(new Paragraph("\n"));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 4 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell("Medicamento");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Fecha");
                table.AddHeaderCell("Nota Paciente");

                foreach (var item in data)
                {
                    table.AddCell(item.Medicine?.Name ?? "");
                    table.AddCell($"{item.Quantity} {item.Medicine?.Unit ?? ""}");
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
            var donations = _context.Donations.Include(d => d.Medicine).AsQueryable();

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
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                var title = new Paragraph("Reporte de Donaciones")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);
                
                document.Add(new Paragraph("Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));

                document.Add(new Paragraph("\n"));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 2, 4 }))
                    .UseAllAvailableWidth();

                table.AddHeaderCell("Medicamento");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Fecha");
                table.AddHeaderCell("Nota Donante");

                foreach (var item in data)
                {
                    table.AddCell(item.Medicine?.Name ?? "");
                    table.AddCell($"{item.Quantity} {item.Medicine?.Unit ?? ""}");
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

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                var title = new Paragraph("Reporte Mensual")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(title);
                
                var subtitle = new Paragraph("Farmacia Solidaria Cristiana, Iglesia Metodista de Cárdenas")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetTextAlignment(TextAlignment.CENTER);
                document.Add(subtitle);

                document.Add(new Paragraph($"{startDate:MMMM yyyy}")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph($"Total Donaciones: {donations.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Total Entregas: {deliveries.Sum(d => d.Quantity)}"));
                document.Add(new Paragraph($"Stock Actual Total: {medicines.Sum(m => m.StockQuantity)}"));

                var inventoryHeader = new Paragraph("\nInventario Actual:").SetFont(boldFont);
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
                document.Close();

                _logger.LogInformation("Monthly PDF report generated for {Year}/{Month}", year, month);
                return File(ms.ToArray(), "application/pdf", $"Mensual_{year}_{month:D2}.pdf");
            }
        }
    }
}
