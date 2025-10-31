using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de turnos
    /// Incluye lógica anti-abuso y validaciones de negocio
    /// </summary>
    public class TurnoService : ITurnoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TurnoService> _logger;

        public TurnoService(
            ApplicationDbContext context, 
            IEmailService emailService,
            IWebHostEnvironment environment,
            ILogger<TurnoService> logger)
        {
            _context = context;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Valida si el usuario puede solicitar un turno (límite: 2 por mes)
        /// </summary>
        public async Task<(bool CanRequest, string? Reason)> CanUserRequestTurnoAsync(string userId)
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var turnosEsteMes = await _context.Turnos
                .Where(t => t.UserId == userId && 
                           t.FechaSolicitud >= startOfMonth && 
                           t.FechaSolicitud <= endOfMonth &&
                           (t.Estado == EstadoTurno.Pendiente || 
                            t.Estado == EstadoTurno.Aprobado ||
                            t.Estado == EstadoTurno.Completado))
                .CountAsync();

            if (turnosEsteMes >= 2)
            {
                return (false, "Ya has alcanzado el límite de turnos este mes. Límite: 2 turnos por mes.");
            }

            return (true, null);
        }

        /// <summary>
        /// Valida si hay disponibilidad para un día específico (límite: 30 turnos por día)
        /// </summary>
        public async Task<(bool HasCapacity, int CurrentCount, string? Reason)> CheckDailyCapacityAsync(DateTime fecha)
        {
            // Obtener solo la fecha sin hora
            var fechaSolo = fecha.Date;
            var siguienteDia = fechaSolo.AddDays(1);

            var turnosDelDia = await _context.Turnos
                .Where(t => t.FechaPreferida >= fechaSolo && 
                           t.FechaPreferida < siguienteDia &&
                           (t.Estado == EstadoTurno.Pendiente || 
                            t.Estado == EstadoTurno.Aprobado ||
                            t.Estado == EstadoTurno.Completado))
                .CountAsync();

            const int LIMITE_DIARIO = 30;

            if (turnosDelDia >= LIMITE_DIARIO)
            {
                return (false, turnosDelDia, $"No hay disponibilidad para ese día. Límite alcanzado: {LIMITE_DIARIO} turnos por día.");
            }

            return (true, turnosDelDia, null);
        }

        /// <summary>
        /// Verifica stock disponible para lista de medicamentos
        /// </summary>
        public async Task<Dictionary<int, (bool Available, int Stock)>> CheckMedicinesStockAsync(List<int> medicineIds)
        {
            var result = new Dictionary<int, (bool Available, int Stock)>();

            var medicines = await _context.Medicines
                .Where(m => medicineIds.Contains(m.Id))
                .Select(m => new { m.Id, m.StockQuantity })
                .ToListAsync();

            foreach (var med in medicines)
            {
                result[med.Id] = (med.StockQuantity > 0, med.StockQuantity);
            }

            return result;
        }

        /// <summary>
        /// Crea una nueva solicitud de turno con uploads
        /// </summary>
        public async Task<Turno> CreateTurnoAsync(
            Turno turno, 
            List<(int MedicineId, int Quantity)> medicamentos,
            IFormFile? receta, 
            IFormFile? tarjeton)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Validar límite mensual
                var (canRequest, reason) = await CanUserRequestTurnoAsync(turno.UserId);
                if (!canRequest)
                {
                    throw new InvalidOperationException(reason);
                }

                // Crear directorio para archivos del turno
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "turnos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Guardar receta médica
                if (receta != null && receta.Length > 0)
                {
                    var recetaFileName = $"{Guid.NewGuid()}_{Path.GetFileName(receta.FileName)}";
                    var recetaPath = Path.Combine(uploadsFolder, recetaFileName);
                    
                    using (var stream = new FileStream(recetaPath, FileMode.Create))
                    {
                        await receta.CopyToAsync(stream);
                    }
                    
                    turno.RecetaMedicaPath = $"/uploads/turnos/{recetaFileName}";
                }

                // Guardar tarjetón
                if (tarjeton != null && tarjeton.Length > 0)
                {
                    var tarjetonFileName = $"{Guid.NewGuid()}_{Path.GetFileName(tarjeton.FileName)}";
                    var tarjetonPath = Path.Combine(uploadsFolder, tarjetonFileName);
                    
                    using (var stream = new FileStream(tarjetonPath, FileMode.Create))
                    {
                        await tarjeton.CopyToAsync(stream);
                    }
                    
                    turno.TarjetonPath = $"/uploads/turnos/{tarjetonFileName}";
                }

                // Guardar turno
                turno.FechaSolicitud = DateTime.Now;
                turno.Estado = EstadoTurno.Pendiente;
                
                _context.Turnos.Add(turno);
                await _context.SaveChangesAsync();

                // Agregar medicamentos solicitados
                foreach (var (medicineId, quantity) in medicamentos)
                {
                    var medicine = await _context.Medicines.FindAsync(medicineId);
                    
                    var turnoMed = new TurnoMedicamento
                    {
                        TurnoId = turno.Id,
                        MedicineId = medicineId,
                        CantidadSolicitada = quantity,
                        DisponibleAlSolicitar = medicine?.StockQuantity >= quantity
                    };
                    
                    _context.TurnoMedicamentos.Add(turnoMed);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Turno #{Id} creado para usuario {UserId}", turno.Id, turno.UserId);

                return turno;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creando turno para usuario {UserId}", turno.UserId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene turnos pendientes ordenados por fecha
        /// </summary>
        public async Task<List<Turno>> GetPendingTurnosAsync()
        {
            return await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Where(t => t.Estado == EstadoTurno.Pendiente)
                .OrderBy(t => t.FechaSolicitud)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene turnos con filtros opcionales
        /// </summary>
        public async Task<List<Turno>> GetTurnosAsync(string? estado = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var query = _context.Turnos
                .Include(t => t.User)
                .Include(t => t.RevisadoPor)
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(t => t.Estado == estado);
            }

            if (desde.HasValue)
            {
                query = query.Where(t => t.FechaSolicitud >= desde.Value);
            }

            if (hasta.HasValue)
            {
                query = query.Where(t => t.FechaSolicitud <= hasta.Value);
            }

            return await query.OrderByDescending(t => t.FechaSolicitud).ToListAsync();
        }

        /// <summary>
        /// Obtiene turno por ID con todas sus relaciones
        /// </summary>
        public async Task<Turno?> GetTurnoByIdAsync(int id)
        {
            return await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.RevisadoPor)
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Aprueba turno, genera PDF y envía email
        /// </summary>
        public async Task<(bool Success, string? Message, string? PdfPath)> ApproveTurnoAsync(
            int turnoId, 
            string farmaceuticoId,
            Dictionary<int, int>? cantidadesAprobadas = null,
            string? comentarios = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var turno = await GetTurnoByIdAsync(turnoId);
                if (turno == null)
                {
                    return (false, "Turno no encontrado", null);
                }

                if (turno.Estado != EstadoTurno.Pendiente)
                {
                    return (false, $"Turno ya fue {turno.Estado.ToLower()}", null);
                }

                // Actualizar cantidades aprobadas si se especificaron
                if (cantidadesAprobadas != null)
                {
                    foreach (var tm in turno.Medicamentos)
                    {
                        if (cantidadesAprobadas.ContainsKey(tm.MedicineId))
                        {
                            tm.CantidadAprobada = cantidadesAprobadas[tm.MedicineId];
                        }
                        else
                        {
                            tm.CantidadAprobada = tm.CantidadSolicitada;
                        }
                    }
                }
                else
                {
                    // Aprobar todas las cantidades solicitadas
                    foreach (var tm in turno.Medicamentos)
                    {
                        tm.CantidadAprobada = tm.CantidadSolicitada;
                    }
                }

                // Generar número de turno
                turno.NumeroTurno = await GenerateNumeroTurnoAsync(turno.FechaPreferida);

                // Actualizar estado del turno
                turno.Estado = EstadoTurno.Aprobado;
                turno.RevisadoPorId = farmaceuticoId;
                turno.FechaRevision = DateTime.Now;
                turno.ComentariosFarmaceutico = comentarios;

                await _context.SaveChangesAsync();

                // Generar PDF con toda la información del turno
                var pdfPath = await GenerateTurnoPdfAsync(turno);
                turno.TurnoPdfPath = pdfPath;

                await _context.SaveChangesAsync();

                // Enviar email de aprobación
                if (turno.User?.Email != null)
                {
                    var emailSent = await _emailService.SendTurnoAprobadoEmailAsync(
                        turno.User.Email,
                        turno.User.UserName ?? "Usuario",
                        turno.NumeroTurno.Value,
                        turno.FechaPreferida,
                        pdfPath
                    );

                    turno.EmailEnviado = emailSent;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Turno #{Id} aprobado por farmacéutico {FarmaceuticoId}", turnoId, farmaceuticoId);

                return (true, "Turno aprobado exitosamente", pdfPath);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error aprobando turno {TurnoId}", turnoId);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Rechaza turno con motivo
        /// </summary>
        public async Task<(bool Success, string? Message)> RejectTurnoAsync(
            int turnoId, 
            string farmaceuticoId, 
            string motivo)
        {
            try
            {
                var turno = await GetTurnoByIdAsync(turnoId);
                if (turno == null)
                {
                    return (false, "Turno no encontrado");
                }

                if (turno.Estado != EstadoTurno.Pendiente)
                {
                    return (false, $"Turno ya fue {turno.Estado.ToLower()}");
                }

                turno.Estado = EstadoTurno.Rechazado;
                turno.RevisadoPorId = farmaceuticoId;
                turno.FechaRevision = DateTime.Now;
                turno.ComentariosFarmaceutico = motivo;

                await _context.SaveChangesAsync();

                // Enviar email de rechazo
                if (turno.User?.Email != null)
                {
                    var emailSent = await _emailService.SendTurnoRechazadoEmailAsync(
                        turno.User.Email,
                        turno.User.UserName ?? "Usuario",
                        motivo
                    );

                    turno.EmailEnviado = emailSent;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Turno #{Id} rechazado por farmacéutico {FarmaceuticoId}", turnoId, farmaceuticoId);

                return (true, "Turno rechazado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechazando turno {TurnoId}", turnoId);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Marca turno como completado
        /// </summary>
        public async Task<bool> CompleteTurnoAsync(int turnoId)
        {
            try
            {
                var turno = await _context.Turnos.FindAsync(turnoId);
                if (turno == null || turno.Estado != EstadoTurno.Aprobado)
                {
                    return false;
                }

                turno.Estado = EstadoTurno.Completado;
                turno.FechaEntrega = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Turno #{Id} marcado como completado", turnoId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completando turno {TurnoId}", turnoId);
                return false;
            }
        }

        /// <summary>
        /// Busca turno por documento hasheado
        /// </summary>
        public async Task<Turno?> FindTurnoByDocumentHashAsync(string documentHash)
        {
            return await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .FirstOrDefaultAsync(t => t.DocumentoIdentidadHash == documentHash &&
                                         (t.Estado == EstadoTurno.Aprobado || t.Estado == EstadoTurno.Pendiente));
        }

        /// <summary>
        /// Obtiene historial de turnos de un usuario
        /// </summary>
        public async Task<List<Turno>> GetUserTurnosAsync(string userId)
        {
            return await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Include(t => t.RevisadoPor)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.FechaSolicitud)
                .ToListAsync();
        }

        /// <summary>
        /// Hashea documento usando SHA256
        /// </summary>
        public string HashDocument(string document)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(document.Trim().ToUpper());
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Genera número de turno secuencial para el día
        /// </summary>
        public async Task<int> GenerateNumeroTurnoAsync(DateTime fecha)
        {
            var startOfDay = fecha.Date;
            var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

            var maxNumero = await _context.Turnos
                .Where(t => t.FechaPreferida >= startOfDay && 
                           t.FechaPreferida <= endOfDay &&
                           t.NumeroTurno.HasValue)
                .MaxAsync(t => (int?)t.NumeroTurno) ?? 0;

            return maxNumero + 1;
        }

        /// <summary>
        /// Genera PDF del turno con logos y detalles
        /// </summary>
        private Task<string> GenerateTurnoPdfAsync(Turno turno)
        {
            try
            {
                // Crear directorio de PDFs si no existe
                var pdfsDir = Path.Combine(_environment.WebRootPath, "pdfs", "turnos");
                Directory.CreateDirectory(pdfsDir);

                // Nombre del archivo PDF
                var fileName = $"turno_{turno.Id}_{turno.NumeroTurno}.pdf";
                var filePath = Path.Combine(pdfsDir, fileName);
                var relativeP = $"pdfs/turnos/{fileName}";

                // Crear PDF
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                {
                    var document = new Document(pdf);
                    var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // === ENCABEZADO CON LOGOS ===
                    var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 }))
                        .UseAllAvailableWidth();

                    // Logo Iglesia (izquierda)
                    var logoIglesiaPath = Path.Combine(_environment.WebRootPath, "images", "logo-iglesia.png");
                    if (File.Exists(logoIglesiaPath))
                    {
                        var logoIglesia = new Image(ImageDataFactory.Create(logoIglesiaPath))
                            .SetWidth(60)
                            .SetHeight(60);
                        headerTable.AddCell(new Cell().Add(logoIglesia).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    }
                    else
                    {
                        headerTable.AddCell(new Cell().Add(new Paragraph("")).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    }

                    // Título central
                    var titleCell = new Cell().Add(new Paragraph("Farmacia Solidaria Cristiana")
                        .SetFont(boldFont)
                        .SetFontSize(18)
                        .SetTextAlignment(TextAlignment.CENTER))
                        .Add(new Paragraph("Iglesia Metodista de Cárdenas")
                            .SetFont(normalFont)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                    headerTable.AddCell(titleCell);

                    // Logo Adriano (derecha)
                    var logoAdrianoPath = Path.Combine(_environment.WebRootPath, "images", "logo-adriano.png");
                    if (File.Exists(logoAdrianoPath))
                    {
                        var logoAdriano = new Image(ImageDataFactory.Create(logoAdrianoPath))
                            .SetWidth(60)
                            .SetHeight(60);
                        headerTable.AddCell(new Cell().Add(logoAdriano).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    }
                    else
                    {
                        headerTable.AddCell(new Cell().Add(new Paragraph("")).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    }

                    document.Add(headerTable);
                    document.Add(new Paragraph("\n"));

                    // === NÚMERO DE TURNO DESTACADO ===
                    var turnoNumero = new Paragraph($"TURNO #{turno.NumeroTurno:000}")
                        .SetFont(boldFont)
                        .SetFontSize(36)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.BLUE);
                    document.Add(turnoNumero);

                    // === INFORMACIÓN DEL TURNO ===
                    document.Add(new Paragraph("\n"));
                    var infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 }))
                        .UseAllAvailableWidth()
                        .SetMarginBottom(10);

                    AddInfoRow(infoTable, "Usuario:", turno.User?.UserName ?? "N/A", boldFont, normalFont);
                    AddInfoRow(infoTable, "Email:", turno.User?.Email ?? "N/A", boldFont, normalFont);
                    AddInfoRow(infoTable, "Fecha del Turno:", turno.FechaPreferida.ToString("dddd, dd 'de' MMMM 'de' yyyy"), boldFont, normalFont);
                    AddInfoRow(infoTable, "Hora del Turno:", turno.FechaPreferida.ToString("HH:mm"), boldFont, normalFont);
                    AddInfoRow(infoTable, "Fecha de Aprobación:", turno.FechaRevision?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", boldFont, normalFont);

                    document.Add(infoTable);

                    // === MEDICAMENTOS APROBADOS ===
                    document.Add(new Paragraph("\nMedicamentos Aprobados:")
                        .SetFont(boldFont)
                        .SetFontSize(14)
                        .SetMarginTop(10));

                    var medicTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 20, 20 }))
                        .UseAllAvailableWidth()
                        .SetMarginTop(10);

                    // Encabezados
                    medicTable.AddHeaderCell(new Cell().Add(new Paragraph("Medicamento").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    medicTable.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER));
                    medicTable.AddHeaderCell(new Cell().Add(new Paragraph("Unidad").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER));

                    // Datos
                    foreach (var tm in turno.Medicamentos)
                    {
                        medicTable.AddCell(new Cell().Add(new Paragraph(tm.Medicine?.Name ?? "N/A").SetFont(normalFont)));
                        medicTable.AddCell(new Cell().Add(new Paragraph((tm.CantidadAprobada ?? tm.CantidadSolicitada).ToString()).SetFont(normalFont)).SetTextAlignment(TextAlignment.CENTER));
                        medicTable.AddCell(new Cell().Add(new Paragraph(tm.Medicine?.Unit ?? "").SetFont(normalFont)).SetTextAlignment(TextAlignment.CENTER));
                    }

                    document.Add(medicTable);

                    // === COMENTARIOS ===
                    if (!string.IsNullOrEmpty(turno.ComentariosFarmaceutico))
                    {
                        document.Add(new Paragraph("\nComentarios del Farmacéutico:")
                            .SetFont(boldFont)
                            .SetFontSize(12)
                            .SetMarginTop(15));
                        document.Add(new Paragraph(turno.ComentariosFarmaceutico)
                            .SetFont(normalFont)
                            .SetFontSize(10)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetPadding(10));
                    }

                    // === INSTRUCCIONES ===
                    document.Add(new Paragraph("\nInstrucciones Importantes:")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetMarginTop(15));

                    var instrucciones = new List()
                        .Add(new ListItem("Presente este documento junto con su documento de identidad original."))
                        .Add(new ListItem("Llegue 10 minutos antes de la hora indicada."))
                        .Add(new ListItem("Si no puede asistir, contacte a la farmacia con anticipación."))
                        .Add(new ListItem("Los medicamentos no recogidos en 7 días serán liberados."))
                        .SetFont(normalFont)
                        .SetFontSize(10);

                    document.Add(instrucciones);

                    // === PIE DE PÁGINA ===
                    document.Add(new Paragraph("\n"));
                    var footer = new Paragraph("Generado el: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                        .SetFont(normalFont)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.GRAY);
                    document.Add(footer);

                    document.Close();
                }

                _logger.LogInformation("PDF generado exitosamente para turno #{TurnoId}: {FilePath}", turno.Id, relativeP);
                return Task.FromResult(relativeP);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF para turno #{TurnoId}", turno.Id);
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Helper para agregar filas a tabla de información
        /// </summary>
        private void AddInfoRow(Table table, string label, string value, PdfFont boldFont, PdfFont normalFont)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFont(normalFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        }
    }
}

