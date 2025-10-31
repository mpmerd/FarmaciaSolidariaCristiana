using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

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
        /// Valida si el usuario puede solicitar un turno (límite: 1 por mes)
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

            if (turnosEsteMes >= 1)
            {
                return (false, "Ya has solicitado un turno este mes. Límite: 1 turno por mes.");
            }

            return (true, null);
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

                // TODO: Generar PDF con iText7 (implementar en próximo paso)
                var pdfPath = $"/pdfs/turnos/turno_{turno.Id}_{turno.NumeroTurno}.pdf";
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
    }
}
