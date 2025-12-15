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
        private readonly IImageCompressionService _imageCompressionService;
        private readonly ILogger<TurnoService> _logger;

        public TurnoService(
            ApplicationDbContext context, 
            IEmailService emailService,
            IWebHostEnvironment environment,
            IImageCompressionService imageCompressionService,
            ILogger<TurnoService> logger)
        {
            _context = context;
            _emailService = emailService;
            _environment = environment;
            _imageCompressionService = imageCompressionService;
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
        /// Obtiene el próximo slot disponible para turnos
        /// Martes/Jueves, 1-4 PM, slots cada 6 minutos (30 turnos/día)
        /// </summary>
        public async Task<DateTime> GetNextAvailableSlotAsync()
        {
            var now = DateTime.Now;
            var startDate = now.Date.AddDays(1); // Empezar desde mañana
            
            // Slots por día: De 1 PM (13:00) a 4 PM (16:00) = 3 horas = 180 minutos
            // 180 minutos / 6 minutos por slot = 30 slots por día
            const int SLOT_DURATION_MINUTES = 6;
            const int START_HOUR = 13; // 1 PM
            const int MAX_SLOTS_PER_DAY = 30;

            // Buscar hasta 8 semanas en el futuro
            for (int week = 0; week < 8; week++)
            {
                var checkDate = startDate.AddDays(week * 7);
                
                // Obtener los MARTES y JUEVES de esta semana
                var daysToCheck = new List<DateTime>();
                
                // Martes (2)
                var tuesday = checkDate.AddDays((DayOfWeek.Tuesday - checkDate.DayOfWeek + 7) % 7);
                if (tuesday >= startDate)
                {
                    daysToCheck.Add(tuesday);
                }
                
                // Jueves (4)
                var thursday = checkDate.AddDays((DayOfWeek.Thursday - checkDate.DayOfWeek + 7) % 7);
                if (thursday >= startDate)
                {
                    daysToCheck.Add(thursday);
                }

                daysToCheck = daysToCheck.OrderBy(d => d).ToList();

                foreach (var day in daysToCheck)
                {
                    // Verificar si la fecha está bloqueada
                    var fechaBloqueada = await _context.FechasBloqueadas
                        .AnyAsync(f => f.Fecha.Date == day.Date);
                    
                    if (fechaBloqueada)
                    {
                        _logger.LogInformation("Fecha bloqueada saltada: {Date}", day.Date.ToString("dd/MM/yyyy"));
                        continue; // Saltar esta fecha
                    }

                    // Obtener todos los turnos aprobados/completados para este día
                    var turnosDelDia = await _context.Turnos
                        .Where(t => t.FechaPreferida.HasValue &&
                                   t.FechaPreferida.Value.Date == day.Date &&
                                   (t.Estado == EstadoTurno.Aprobado || t.Estado == EstadoTurno.Completado))
                        .OrderBy(t => t.FechaPreferida)
                        .Select(t => t.FechaPreferida!.Value)
                        .ToListAsync();

                    // Si hay menos de 30 turnos, buscar el primer slot disponible
                    if (turnosDelDia.Count < MAX_SLOTS_PER_DAY)
                    {
                        // Generar todos los slots posibles del día
                        var allSlots = new List<DateTime>();
                        for (int i = 0; i < MAX_SLOTS_PER_DAY; i++)
                        {
                            var slotTime = day.Date
                                .AddHours(START_HOUR)
                                .AddMinutes(i * SLOT_DURATION_MINUTES);
                            allSlots.Add(slotTime);
                        }

                        // Encontrar el primer slot que no esté ocupado
                        foreach (var slot in allSlots)
                        {
                            if (!turnosDelDia.Contains(slot))
                            {
                                _logger.LogInformation("Slot disponible encontrado: {Slot} ({Day})", 
                                    slot.ToString("dd/MM/yyyy HH:mm"), 
                                    slot.DayOfWeek == DayOfWeek.Tuesday ? "Martes" : "Jueves");
                                return slot;
                            }
                        }
                    }
                }
            }

            // Si no encontramos slot en 8 semanas, devolver el primer martes en 8 semanas
            var fallbackDate = startDate.AddDays(8 * 7);
            var fallbackTuesday = fallbackDate.AddDays((DayOfWeek.Tuesday - fallbackDate.DayOfWeek + 7) % 7);
            var fallbackSlot = fallbackTuesday.Date.AddHours(START_HOUR);
            
            _logger.LogWarning("No se encontró slot disponible en 8 semanas, usando fallback: {Slot}", 
                fallbackSlot.ToString("dd/MM/yyyy HH:mm"));
            
            return fallbackSlot;
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
            List<(int SupplyId, int Quantity)> insumos,
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
                
                try
                {
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                        _logger.LogInformation("Directorio creado: {Path}", uploadsFolder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando directorio: {Path}", uploadsFolder);
                    throw new InvalidOperationException($"No se pudo crear el directorio de uploads: {ex.Message}");
                }

                // Guardar receta médica
                if (receta != null && receta.Length > 0)
                {
                    try
                    {
                        var recetaFileName = $"{Guid.NewGuid()}_{Path.GetFileName(receta.FileName)}";
                        var recetaPath = Path.Combine(uploadsFolder, recetaFileName);
                        
                        long fileSize = receta.Length;
                        var originalSize = receta.Length;
                        
                        _logger.LogInformation("Guardando receta en: {Path}, tamaño original: {Size} bytes", recetaPath, originalSize);
                        
                        // Comprimir imagen si es una imagen
                        if (_imageCompressionService.IsImage(receta.ContentType))
                        {
                            _logger.LogInformation("Comprimiendo imagen de receta: {FileName}", receta.FileName);
                            
                            using (var inputStream = receta.OpenReadStream())
                            {
                                using (var compressedStream = await _imageCompressionService.CompressImageAsync(
                                    inputStream,
                                    receta.ContentType,
                                    maxWidth: 1920,
                                    maxHeight: 1920,
                                    quality: 85))
                                {
                                    using (var fileStream = new FileStream(recetaPath, FileMode.Create))
                                    {
                                        await compressedStream.CopyToAsync(fileStream);
                                        fileSize = fileStream.Length;
                                    }
                                }
                            }
                            
                            var compressionRatio = originalSize > 0 ? (1 - (double)fileSize / originalSize) * 100 : 0;
                            _logger.LogInformation("Receta comprimida: {Size} bytes, compresión: {Ratio:F2}%", fileSize, compressionRatio);
                        }
                        else
                        {
                            // No es imagen, guardar como PDF sin compresión
                            using (var stream = new FileStream(recetaPath, FileMode.Create))
                            {
                                await receta.CopyToAsync(stream);
                            }
                            _logger.LogInformation("Receta PDF guardada: {Size} bytes", fileSize);
                        }
                        
                        turno.RecetaMedicaPath = $"/uploads/turnos/{recetaFileName}";
                        _logger.LogInformation("Receta guardada exitosamente: {Path}", turno.RecetaMedicaPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error guardando receta médica");
                        throw new InvalidOperationException($"Error al guardar la receta: {ex.Message}");
                    }
                }

                // Guardar tarjetón
                if (tarjeton != null && tarjeton.Length > 0)
                {
                    try
                    {
                        var tarjetonFileName = $"{Guid.NewGuid()}_{Path.GetFileName(tarjeton.FileName)}";
                        var tarjetonPath = Path.Combine(uploadsFolder, tarjetonFileName);
                        
                        long fileSize = tarjeton.Length;
                        var originalSize = tarjeton.Length;
                        
                        _logger.LogInformation("Guardando tarjetón en: {Path}, tamaño original: {Size} bytes", tarjetonPath, originalSize);
                        
                        // Comprimir imagen si es una imagen
                        if (_imageCompressionService.IsImage(tarjeton.ContentType))
                        {
                            _logger.LogInformation("Comprimiendo imagen de tarjetón: {FileName}", tarjeton.FileName);
                            
                            using (var inputStream = tarjeton.OpenReadStream())
                            {
                                using (var compressedStream = await _imageCompressionService.CompressImageAsync(
                                    inputStream,
                                    tarjeton.ContentType,
                                    maxWidth: 1920,
                                    maxHeight: 1920,
                                    quality: 85))
                                {
                                    using (var fileStream = new FileStream(tarjetonPath, FileMode.Create))
                                    {
                                        await compressedStream.CopyToAsync(fileStream);
                                        fileSize = fileStream.Length;
                                    }
                                }
                            }
                            
                            var compressionRatio = originalSize > 0 ? (1 - (double)fileSize / originalSize) * 100 : 0;
                            _logger.LogInformation("Tarjetón comprimido: {Size} bytes, compresión: {Ratio:F2}%", fileSize, compressionRatio);
                        }
                        else
                        {
                            // No es imagen, guardar como PDF sin compresión
                            using (var stream = new FileStream(tarjetonPath, FileMode.Create))
                            {
                                await tarjeton.CopyToAsync(stream);
                            }
                            _logger.LogInformation("Tarjetón PDF guardado: {Size} bytes", fileSize);
                        }
                        
                        turno.TarjetonPath = $"/uploads/turnos/{tarjetonFileName}";
                        _logger.LogInformation("Tarjetón guardado exitosamente: {Path}", turno.TarjetonPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error guardando tarjetón");
                        throw new InvalidOperationException($"Error al guardar el tarjetón: {ex.Message}");
                    }
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

                // Agregar insumos solicitados
                foreach (var (supplyId, quantity) in insumos)
                {
                    var supply = await _context.Supplies.FindAsync(supplyId);
                    
                    var turnoIns = new TurnoInsumo
                    {
                        TurnoId = turno.Id,
                        SupplyId = supplyId,
                        CantidadSolicitada = quantity,
                        DisponibleAlSolicitar = supply?.StockQuantity >= quantity
                    };
                    
                    _context.TurnoInsumos.Add(turnoIns);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Turno #{Id} creado para usuario {UserId} con {MedCount} medicamentos y {InsCount} insumos", 
                    turno.Id, turno.UserId, medicamentos.Count, insumos.Count);

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
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
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
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
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
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
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
                    
                    // ✅ CORREGIDO: Actualizar cantidades aprobadas para INSUMOS también
                    foreach (var ti in turno.Insumos)
                    {
                        if (cantidadesAprobadas.ContainsKey(ti.SupplyId))
                        {
                            ti.CantidadAprobada = cantidadesAprobadas[ti.SupplyId];
                        }
                        else
                        {
                            ti.CantidadAprobada = ti.CantidadSolicitada;
                        }
                    }
                }
                else
                {
                    // Aprobar todas las cantidades solicitadas para medicamentos
                    foreach (var tm in turno.Medicamentos)
                    {
                        tm.CantidadAprobada = tm.CantidadSolicitada;
                    }
                    
                    // ✅ CORREGIDO: Aprobar todas las cantidades solicitadas para INSUMOS también
                    foreach (var ti in turno.Insumos)
                    {
                        ti.CantidadAprobada = ti.CantidadSolicitada;
                    }
                }

                // Asignar fecha y hora automáticamente
                turno.FechaPreferida = await GetNextAvailableSlotAsync();
                _logger.LogInformation("Fecha asignada automáticamente: {Fecha}", turno.FechaPreferida.Value.ToString("dd/MM/yyyy HH:mm"));

                // Generar número de turno
                turno.NumeroTurno = await GenerateNumeroTurnoAsync(turno.FechaPreferida.Value);

                // ✅ NUEVO: Reservar stock (descontar temporalmente del stock disponible)
                // ⚠️ IMPORTANTE: Usar FromSqlRaw con UPDLOCK+ROWLOCK (SQL Server) para bloquear filas y evitar race conditions
                foreach (var tm in turno.Medicamentos)
                {
                    if (!tm.CantidadAprobada.HasValue) continue;

                    // Bloquear fila del medicamento para lectura exclusiva durante la transacción (SQL Server)
                    var medicine = await _context.Medicines
                        .FromSqlRaw("SELECT * FROM Medicines WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", tm.MedicineId)
                        .FirstOrDefaultAsync();

                    if (medicine == null)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Medicamento con ID {tm.MedicineId} no encontrado", null);
                    }

                    if (medicine.StockQuantity < tm.CantidadAprobada.Value)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Stock insuficiente para {medicine.Name}. Disponible: {medicine.StockQuantity}, Solicitado: {tm.CantidadAprobada.Value}", null);
                    }

                    medicine.StockQuantity -= tm.CantidadAprobada.Value;
                    _logger.LogInformation("Stock reservado para medicamento {Name}: {Cantidad} unidades (Stock restante: {Stock})", 
                        medicine.Name, tm.CantidadAprobada.Value, medicine.StockQuantity);
                }

                foreach (var ti in turno.Insumos)
                {
                    if (!ti.CantidadAprobada.HasValue) continue;

                    // Bloquear fila del insumo para lectura exclusiva durante la transacción (SQL Server)
                    var supply = await _context.Supplies
                        .FromSqlRaw("SELECT * FROM Supplies WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", ti.SupplyId)
                        .FirstOrDefaultAsync();

                    if (supply == null)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Insumo con ID {ti.SupplyId} no encontrado", null);
                    }

                    if (supply.StockQuantity < ti.CantidadAprobada.Value)
                    {
                        await transaction.RollbackAsync();
                        return (false, $"Stock insuficiente para {supply.Name}. Disponible: {supply.StockQuantity}, Solicitado: {ti.CantidadAprobada.Value}", null);
                    }

                    supply.StockQuantity -= ti.CantidadAprobada.Value;
                    _logger.LogInformation("Stock reservado para insumo {Name}: {Cantidad} unidades (Stock restante: {Stock})", 
                        supply.Name, ti.CantidadAprobada.Value, supply.StockQuantity);
                }

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

                // Enviar email de aprobación con PDF adjunto
                if (turno.User?.Email != null)
                {
                    // Convertir ruta relativa a ruta física completa para el adjunto
                    var pdfPhysicalPath = Path.Combine(_environment.WebRootPath, pdfPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    
                    _logger.LogInformation("Enviando email con PDF: {PhysicalPath}", pdfPhysicalPath);
                    
                    var emailSent = await _emailService.SendTurnoAprobadoEmailAsync(
                        turno.User.Email,
                        turno.User.UserName ?? "Usuario",
                        turno.NumeroTurno.Value,
                        turno.FechaPreferida.Value,
                        pdfPhysicalPath
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

                // Eliminar archivos físicos asociados al turno
                DeleteTurnoFiles(turno);

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
        /// Busca turno por documento hasheado (prioriza Aprobado, luego Pendiente)
        /// </summary>
        public async Task<Turno?> FindTurnoByDocumentHashAsync(string documentHash)
        {
            // Buscar primero turnos aprobados
            var turnoAprobado = await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
                .Include(t => t.User)
                .Include(t => t.RevisadoPor)
                .Where(t => t.DocumentoIdentidadHash == documentHash && t.Estado == EstadoTurno.Aprobado)
                .OrderByDescending(t => t.FechaPreferida)
                .FirstOrDefaultAsync();
            
            if (turnoAprobado != null)
            {
                return turnoAprobado;
            }
            
            // Si no hay aprobado, buscar pendiente
            return await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
                .Include(t => t.User)
                .Include(t => t.RevisadoPor)
                .Where(t => t.DocumentoIdentidadHash == documentHash && t.Estado == EstadoTurno.Pendiente)
                .OrderByDescending(t => t.FechaSolicitud)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Busca TODOS los turnos activos por documento hasheado
        /// </summary>
        public async Task<List<Turno>> FindAllTurnosByDocumentHashAsync(string documentHash)
        {
            return await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
                .Include(t => t.User)
                .Include(t => t.RevisadoPor)
                .Where(t => t.DocumentoIdentidadHash == documentHash && 
                           (t.Estado == EstadoTurno.Aprobado || t.Estado == EstadoTurno.Pendiente))
                .OrderByDescending(t => t.Estado == EstadoTurno.Aprobado ? 1 : 0) // Aprobados primero
                .ThenByDescending(t => t.FechaPreferida ?? t.FechaSolicitud)
                .ToListAsync();
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
                    AddInfoRow(infoTable, "Fecha del Turno:", turno.FechaPreferida?.ToString("dddd, dd 'de' MMMM 'de' yyyy") ?? "N/A", boldFont, normalFont);
                    AddInfoRow(infoTable, "Hora del Turno:", turno.FechaPreferida?.ToString("HH:mm") ?? "N/A", boldFont, normalFont);
                    AddInfoRow(infoTable, "Fecha de Aprobación:", turno.FechaRevision?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", boldFont, normalFont);

                    document.Add(infoTable);

                    // === MEDICAMENTOS APROBADOS ===
                    if (turno.Medicamentos != null && turno.Medicamentos.Any())
                    {
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
                    }

                    // === INSUMOS APROBADOS ===
                    if (turno.Insumos != null && turno.Insumos.Any())
                    {
                        document.Add(new Paragraph("\nInsumos Médicos Aprobados:")
                            .SetFont(boldFont)
                            .SetFontSize(14)
                            .SetMarginTop(10));

                        var insumoTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 20, 20 }))
                            .UseAllAvailableWidth()
                            .SetMarginTop(10);

                        // Encabezados
                        insumoTable.AddHeaderCell(new Cell().Add(new Paragraph("Insumo").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                        insumoTable.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER));
                        insumoTable.AddHeaderCell(new Cell().Add(new Paragraph("Unidad").SetFont(boldFont)).SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetTextAlignment(TextAlignment.CENTER));

                        // Datos
                        foreach (var ti in turno.Insumos)
                        {
                            insumoTable.AddCell(new Cell().Add(new Paragraph(ti.Supply?.Name ?? "N/A").SetFont(normalFont)));
                            insumoTable.AddCell(new Cell().Add(new Paragraph((ti.CantidadAprobada ?? ti.CantidadSolicitada).ToString()).SetFont(normalFont)).SetTextAlignment(TextAlignment.CENTER));
                            insumoTable.AddCell(new Cell().Add(new Paragraph(ti.Supply?.Unit ?? "").SetFont(normalFont)).SetTextAlignment(TextAlignment.CENTER));
                        }

                        document.Add(insumoTable);
                    }

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
                        .Add(new ListItem("Debe traer los comprobantes de recetas médicas o documentos médicos necesarios."))
                        .Add(new ListItem("Llegue 10 minutos antes de la hora indicada."))
                        .Add(new ListItem("Si no puede asistir el día de su turno lo perderá. Tendrá que solicitar uno nuevo."))
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

        /// <summary>
        /// Valida si un usuario puede cancelar su turno aprobado
        /// Solo permitido si faltan más de 7 días para la fecha del turno
        /// </summary>
        public bool CanUserCancelTurno(Turno turno)
        {
            // Solo turnos Aprobados pueden ser cancelados por usuario
            if (turno.Estado != EstadoTurno.Aprobado)
                return false;
            
            // Debe tener fecha asignada
            if (!turno.FechaPreferida.HasValue)
                return false;
            
            // Calcular días restantes
            var diasRestantes = (turno.FechaPreferida.Value.Date - DateTime.Now.Date).Days;
            
            // Permitir cancelar solo si faltan más de 7 días
            return diasRestantes > 7;
        }

        /// <summary>
        /// Obtiene mensaje explicando por qué no se puede cancelar
        /// </summary>
        public string GetCancelReasonMessage(Turno turno)
        {
            if (turno.Estado != EstadoTurno.Aprobado)
                return "Solo se pueden cancelar turnos aprobados.";
            
            if (!turno.FechaPreferida.HasValue)
                return "El turno no tiene fecha asignada.";
            
            var diasRestantes = (turno.FechaPreferida.Value.Date - DateTime.Now.Date).Days;
            
            if (diasRestantes <= 7)
                return $"No se puede cancelar. Faltan solo {diasRestantes} día(s). Debe cancelar con al menos 7 días de anticipación.";
            
            return string.Empty;
        }

        /// <summary>
        /// Cancela un turno por solicitud del usuario
        /// </summary>
        public async Task<bool> CancelTurnoByUserAsync(int turnoId, string userId, string motivoCancelacion)
        {
            var turno = await _context.Turnos
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == turnoId && t.UserId == userId);
            
            if (turno == null)
                return false;
            
            // Validar que se puede cancelar
            if (!CanUserCancelTurno(turno))
                return false;
            
            // ✅ NUEVO: Devolver stock reservado al cancelar turno aprobado
            if (turno.Estado == EstadoTurno.Aprobado)
            {
                foreach (var tm in turno.Medicamentos)
                {
                    if (tm.CantidadAprobada.HasValue && tm.Medicine != null)
                    {
                        tm.Medicine.StockQuantity += tm.CantidadAprobada.Value;
                        _logger.LogInformation("Stock devuelto para medicamento {Name}: {Cantidad} unidades", 
                            tm.Medicine.Name, tm.CantidadAprobada.Value);
                    }
                }

                foreach (var ti in turno.Insumos)
                {
                    if (ti.CantidadAprobada.HasValue && ti.Supply != null)
                    {
                        ti.Supply.StockQuantity += ti.CantidadAprobada.Value;
                        _logger.LogInformation("Stock devuelto para insumo {Name}: {Cantidad} unidades", 
                            ti.Supply.Name, ti.CantidadAprobada.Value);
                    }
                }
            }
            
            // Eliminar archivos físicos asociados al turno
            DeleteTurnoFiles(turno);
            
            // Cambiar estado a Rechazado (usamos este estado para cancelaciones)
            turno.Estado = EstadoTurno.Rechazado;
            turno.FechaRevision = DateTime.Now;
            turno.ComentariosFarmaceutico += $"\n[CANCELADO POR USUARIO - {DateTime.Now:dd/MM/yyyy HH:mm}]";
            turno.ComentariosFarmaceutico += $"\nMotivo: {motivoCancelacion}";
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Turno {TurnoId} cancelado por usuario {UserId}", turnoId, userId);
            
            return true;
        }

        /// <summary>
        /// Elimina archivos físicos asociados a un turno (receta, tarjetón, PDF)
        /// </summary>
        private void DeleteTurnoFiles(Turno turno)
        {
            try
            {
                // Eliminar receta médica si existe
                if (!string.IsNullOrEmpty(turno.RecetaMedicaPath))
                {
                    var recetaPath = Path.Combine(_environment.WebRootPath, turno.RecetaMedicaPath.TrimStart('/'));
                    if (File.Exists(recetaPath))
                    {
                        File.Delete(recetaPath);
                        _logger.LogInformation("Archivo de receta eliminado: {FilePath}", recetaPath);
                    }
                }

                // Eliminar tarjetón si existe
                if (!string.IsNullOrEmpty(turno.TarjetonPath))
                {
                    var tarjetonPath = Path.Combine(_environment.WebRootPath, turno.TarjetonPath.TrimStart('/'));
                    if (File.Exists(tarjetonPath))
                    {
                        File.Delete(tarjetonPath);
                        _logger.LogInformation("Archivo de tarjetón eliminado: {FilePath}", tarjetonPath);
                    }
                }

                // Eliminar PDF del turno si existe
                if (!string.IsNullOrEmpty(turno.TurnoPdfPath))
                {
                    var pdfPath = Path.Combine(_environment.WebRootPath, turno.TurnoPdfPath.TrimStart('/'));
                    if (File.Exists(pdfPath))
                    {
                        File.Delete(pdfPath);
                        _logger.LogInformation("PDF del turno eliminado: {FilePath}", pdfPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando archivos del turno {TurnoId}", turno.Id);
            }
        }
    }
}

