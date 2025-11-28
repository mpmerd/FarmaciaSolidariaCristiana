using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Servicio de limpieza automática de turnos no atendidos
    /// Se ejecuta periódicamente para cancelar turnos aprobados donde el usuario no asistió
    /// </summary>
    public class TurnoCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TurnoCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Ejecutar cada hora

        public TurnoCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TurnoCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TurnoCleanupService iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredTurnos();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando turnos vencidos");
                }

                // Esperar hasta la próxima ejecución
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("TurnoCleanupService detenido");
        }

        private async Task ProcessExpiredTurnos()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var now = DateTime.Now;
            
            // Solo procesar después de las 6 PM (18:00)
            if (now.Hour < 18)
            {
                _logger.LogInformation("TurnoCleanupService: Aún no son las 6 PM (hora actual: {Hora}), omitiendo verificación", now.Hour);
                return;
            }

            // Buscar turnos aprobados cuya FechaPreferida (fecha asignada del turno) ya pasó
            // Un turno se considera vencido si:
            // - Su fecha asignada (FechaPreferida) es anterior a HOY
            // - O su fecha asignada es HOY pero ya pasaron las 6 PM
            var cutoffTime = now.Date.AddHours(18); // Hoy a las 6 PM
            
            _logger.LogInformation("TurnoCleanupService: Buscando turnos vencidos. Hora actual: {Now}, Corte: {Cutoff}", 
                now.ToString("yyyy-MM-dd HH:mm:ss"), cutoffTime.ToString("yyyy-MM-dd HH:mm:ss"));
            
            var expiredTurnos = await context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Where(t => t.Estado == EstadoTurno.Aprobado &&
                           t.FechaPreferida.HasValue &&
                           t.FechaPreferida.Value.Date <= cutoffTime.Date) // Turnos de HOY o anteriores (después de las 6 PM)
                .ToListAsync();

            if (!expiredTurnos.Any())
            {
                _logger.LogInformation("TurnoCleanupService: No hay turnos vencidos para procesar");
                return;
            }

            _logger.LogInformation("TurnoCleanupService: Encontrados {Count} turnos vencidos para cancelar", expiredTurnos.Count);
            
            // Log detallado de cada turno encontrado
            foreach (var t in expiredTurnos)
            {
                _logger.LogInformation("  - Turno #{Id} | Usuario: {User} | Fecha asignada: {Fecha} | Estado: {Estado}", 
                    t.Id, t.User?.UserName ?? "N/A", t.FechaPreferida?.ToString("yyyy-MM-dd HH:mm") ?? "N/A", t.Estado);
            }

            foreach (var turno in expiredTurnos)
            {
                try
                {
                    await CancelExpiredTurno(turno, context, emailService, userManager);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelando turno vencido {TurnoId}", turno.Id);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("TurnoCleanupService: Procesamiento completado");
        }

        private async Task CancelExpiredTurno(
            Turno turno,
            ApplicationDbContext context,
            IEmailService emailService,
            UserManager<IdentityUser> userManager)
        {
            _logger.LogInformation("Cancelando turno vencido #{TurnoId} - Usuario: {UserId}, Fecha: {Fecha}",
                turno.Id, turno.UserId, turno.FechaPreferida);

            // 1. Devolver stock reservado - MEDICAMENTOS
            foreach (var tm in turno.Medicamentos)
            {
                if (tm.CantidadAprobada.HasValue && tm.Medicine != null)
                {
                    tm.Medicine.StockQuantity += tm.CantidadAprobada.Value;
                    _logger.LogInformation("Stock devuelto (turno vencido) - Medicamento: {Name}, Cantidad: {Qty}",
                        tm.Medicine.Name, tm.CantidadAprobada.Value);
                }
            }

            // 2. Devolver stock reservado - INSUMOS
            foreach (var ti in turno.Insumos)
            {
                if (ti.CantidadAprobada.HasValue && ti.Supply != null)
                {
                    ti.Supply.StockQuantity += ti.CantidadAprobada.Value;
                    _logger.LogInformation("Stock devuelto (turno vencido) - Insumo: {Name}, Cantidad: {Qty}",
                        ti.Supply.Name, ti.CantidadAprobada.Value);
                }
            }

            // 3. Marcar turno como cancelado (por no asistencia)
            turno.Estado = EstadoTurno.Cancelado;
            turno.ComentariosFarmaceutico += $"\n[CANCELADO AUTOMÁTICAMENTE - {DateTime.Now:dd/MM/yyyy HH:mm}]";
            turno.ComentariosFarmaceutico += "\nMotivo: Usuario no asistió a la farmacia en la fecha programada";

            // 4. Enviar email al usuario
            if (turno.User?.Email != null)
            {
                try
                {
                    await emailService.SendTurnoNoAsistenciaUsuarioEmailAsync(
                        turno.User.Email,
                        turno.User.UserName ?? "Usuario",
                        turno.NumeroTurno ?? 0,
                        turno.FechaPreferida ?? DateTime.Now);

                    _logger.LogInformation("Email de no asistencia enviado a usuario: {Email}", turno.User.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email al usuario del turno {TurnoId}", turno.Id);
                }
            }

            // 5. Enviar email a todos los farmacéuticos
            try
            {
                var farmaceuticos = await userManager.GetUsersInRoleAsync("Farmaceutico");
                var admins = await userManager.GetUsersInRoleAsync("Admin");
                var destinatarios = farmaceuticos.Union(admins).Where(u => u.Email != null);

                foreach (var farmaceutico in destinatarios)
                {
                    try
                    {
                        await emailService.SendTurnoNoAsistenciaFarmaceuticoEmailAsync(
                            farmaceutico.Email!,
                            farmaceutico.UserName ?? "Farmacéutico",
                            turno.NumeroTurno ?? 0,
                            turno.User?.UserName ?? "Usuario",
                            turno.FechaPreferida ?? DateTime.Now,
                            GetTurnoItemsSummary(turno));

                        _logger.LogInformation("Email de no asistencia enviado a farmacéutico: {Email}", farmaceutico.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando email a farmacéutico {Email}", farmaceutico.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando emails a farmacéuticos para turno {TurnoId}", turno.Id);
            }

            _logger.LogInformation("Turno #{TurnoId} cancelado exitosamente por no asistencia", turno.Id);
        }

        private string GetTurnoItemsSummary(Turno turno)
        {
            var items = new List<string>();

            foreach (var tm in turno.Medicamentos)
            {
                if (tm.Medicine != null && tm.CantidadAprobada.HasValue)
                {
                    items.Add($"• {tm.Medicine.Name}: {tm.CantidadAprobada.Value} {tm.Medicine.Unit}");
                }
            }

            foreach (var ti in turno.Insumos)
            {
                if (ti.Supply != null && ti.CantidadAprobada.HasValue)
                {
                    items.Add($"• {ti.Supply.Name}: {ti.CantidadAprobada.Value} {ti.Supply.Unit}");
                }
            }

            return string.Join("\n", items);
        }
    }
}
