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
            var notificationService = scope.ServiceProvider.GetRequiredService<IOneSignalNotificationService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var now = DateTime.Now;
            var today = now.Date;
            
            _logger.LogInformation("TurnoCleanupService: Ejecutando verificación. Hora actual: {Hora}", now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // ✅ FIX: No cancelar turnos que ya tienen entregas completadas
            // Obtener IDs de turnos que ya tienen entregas (medicamentos o insumos entregados)
            var turnosConEntregas = await context.Deliveries
                .Where(d => d.TurnoId.HasValue)
                .Select(d => d.TurnoId!.Value)
                .Distinct()
                .ToListAsync();
            
            _logger.LogInformation("TurnoCleanupService: Encontrados {Count} turnos con entregas registradas (no se cancelarán)", turnosConEntregas.Count);
            
            // Buscar turnos aprobados vencidos:
            // 1. Turnos de días ANTERIORES a hoy (siempre se cancelan)
            // 2. Turnos de HOY solo si ya pasaron las 6 PM
            var expiredTurnos = await context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Where(t => t.Estado == EstadoTurno.Aprobado &&
                           t.FechaPreferida.HasValue &&
                           !turnosConEntregas.Contains(t.Id) && // Excluir turnos que ya tienen entregas
                           (
                               // Turnos de días anteriores: siempre vencidos
                               t.FechaPreferida.Value.Date < today ||
                               // Turnos de hoy: solo vencidos si ya pasaron las 6 PM
                               (t.FechaPreferida.Value.Date == today && now.Hour >= 18)
                           ))
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
                    await CancelExpiredTurno(turno, context, emailService, notificationService, userManager);
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
            IOneSignalNotificationService notificationService,
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
            turno.CanceladoPorNoPresentacion = true; // ✅ Marca para penalización - cuenta contra límite mensual
            turno.ComentariosFarmaceutico += $"\n[CANCELADO AUTOMÁTICAMENTE - {DateTime.Now:dd/MM/yyyy HH:mm}]";
            turno.ComentariosFarmaceutico += "\nMotivo: Usuario no asistió a la farmacia en la fecha programada";

            var nombrePaciente = turno.User?.UserName ?? "Paciente";
            var fechaTurno = turno.FechaPreferida ?? DateTime.Now;
            var numeroTurno = turno.NumeroTurno ?? 0;

            // 4. Verificar si el paciente tiene la app para enviar push o email
            var pacienteTienePush = await notificationService.UserHasPushEnabledAsync(turno.UserId);

            if (pacienteTienePush)
            {
                // Enviar notificación push al paciente
                try
                {
                    await notificationService.SendTurnoCanceladoNoPresentacionAsync(
                        turno.UserId,
                        turno.Id,
                        numeroTurno,
                        fechaTurno);

                    _logger.LogInformation("Push de no asistencia enviado al paciente: {UserId}", turno.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando push al paciente del turno {TurnoId}", turno.Id);
                }
            }
            else if (turno.User?.Email != null)
            {
                // Enviar email al usuario si no tiene la app
                try
                {
                    await emailService.SendTurnoNoAsistenciaUsuarioEmailAsync(
                        turno.User.Email,
                        nombrePaciente,
                        numeroTurno,
                        fechaTurno);

                    _logger.LogInformation("Email de no asistencia enviado a usuario: {Email}", turno.User.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email al usuario del turno {TurnoId}", turno.Id);
                }
            }

            // 5. Enviar notificación push a farmacéuticos y admin
            try
            {
                await notificationService.SendTurnoCanceladoNoPresentacionToFarmaceuticosAsync(
                    turno.Id,
                    numeroTurno,
                    nombrePaciente,
                    fechaTurno);

                _logger.LogInformation("Push de no asistencia enviado a farmacéuticos para turno #{TurnoId}", turno.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando push a farmacéuticos para turno {TurnoId}", turno.Id);
            }

            // 6. También enviar email a farmacéuticos (además del push, por si no todos tienen la app)
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
                            numeroTurno,
                            nombrePaciente,
                            fechaTurno,
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
