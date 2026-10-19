using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Services;

/// <summary>
/// Procesa la cola persistente de emails de notificación masiva (BroadcastEmailQueueItems).
///
/// - Ritmo: 1 email cada AppSettings:BroadcastEmailIntervalSeconds (default 60s).
/// - Tope diario: AppSettings:BroadcastEmailDailyMax (default 350) emails de broadcast
///   por día (UTC). El límite duro de Gmail gratuito es 500/día compartidos con los
///   emails transaccionales (turnos, códigos de registro): el tope deja ~150/día de margen.
/// - Reintentos: 3 intentos por ítem con backoff de 30 min entre fallos (ej. cuota
///   temporal de Gmail agotada por emails transaccionales); tras 3 fallos queda Failed.
/// - La cola vive en la BD: sobrevive reciclados/reinicios del hosting (Somee).
/// </summary>
public class BroadcastEmailProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BroadcastEmailProcessor> _logger;

    /// <summary>
    /// Espera cuando no hay trabajo: cola vacía o tope diario alcanzado.
    /// El tope diario se reevalúa en cada ciclo (cambio de día UTC).
    /// </summary>
    private readonly TimeSpan _idleDelay = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Backoff tras un fallo de envío (posible cuota de Gmail agotada temporalmente).
    /// </summary>
    private readonly TimeSpan _failureDelay = TimeSpan.FromMinutes(30);

    private const int MaxAttempts = 3;

    public BroadcastEmailProcessor(
        IServiceProvider serviceProvider,
        ILogger<BroadcastEmailProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastEmailProcessor iniciado (cola persistente de emails de broadcast)");

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                delay = await ProcessOneAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BroadcastEmail] Error inesperado procesando la cola");
                delay = _idleDelay;
            }

            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("BroadcastEmailProcessor detenido");
    }

    /// <summary>
    /// Procesa un ítem de la cola. Devuelve la espera antes del siguiente ciclo:
    /// ritmo normal (60s) tras envío, backoff (30 min) tras fallo, o idle (15 min)
    /// si la cola está vacía o se alcanzó el tope diario.
    /// </summary>
    private async Task<TimeSpan> ProcessOneAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var dailyMax = config.GetValue<int?>("AppSettings:BroadcastEmailDailyMax") ?? 350;
        var intervalSeconds = config.GetValue<int?>("AppSettings:BroadcastEmailIntervalSeconds") ?? 60;

        // Tope diario (UTC): emails de broadcast ya enviados hoy
        var todayUtc = DateTime.UtcNow.Date;
        var sentToday = await context.BroadcastEmailQueueItems
            .CountAsync(i => i.Status == BroadcastEmailStatus.Sent && i.SentAtUtc >= todayUtc, stoppingToken);
        if (sentToday >= dailyMax)
        {
            _logger.LogInformation(
                "[BroadcastEmail] Tope diario alcanzado: {SentToday}/{DailyMax}. Se reanuda al cambiar el día UTC",
                sentToday, dailyMax);
            return _idleDelay;
        }

        // Ítem pendiente más antiguo (TOP 1). Los Failed no se reintentan.
        var item = await context.BroadcastEmailQueueItems
            .Where(i => i.Status == BroadcastEmailStatus.Pending)
            .OrderBy(i => i.Id)
            .FirstOrDefaultAsync(stoppingToken);
        if (item == null)
            return _idleDelay;

        try
        {
            var body = BuildEmailBody(item.Title, item.Message);
            await emailService.SendEmailAsync(item.Email, $"📢 {item.Title}", body);

            item.Status = BroadcastEmailStatus.Sent;
            item.SentAtUtc = DateTime.UtcNow;
            item.Attempts++;
            item.LastError = null;
            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "[BroadcastEmail] Enviado a {Email} ({SentToday}/{DailyMax} hoy, quedan {Pending} en cola)",
                item.Email, sentToday + 1, dailyMax,
                await context.BroadcastEmailQueueItems.CountAsync(i => i.Status == BroadcastEmailStatus.Pending, stoppingToken));

            return TimeSpan.FromSeconds(intervalSeconds);
        }
        catch (Exception ex)
        {
            item.Attempts++;
            item.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            if (item.Attempts >= MaxAttempts)
            {
                item.Status = BroadcastEmailStatus.Failed;
                _logger.LogError(
                    "[BroadcastEmail] Email a {Email} marcado Failed tras {Attempts} intentos: {Error}",
                    item.Email, item.Attempts, item.LastError);
            }
            else
            {
                item.Status = BroadcastEmailStatus.Pending; // reintento tras el backoff
                _logger.LogWarning(
                    "[BroadcastEmail] Error enviando a {Email} (intento {Attempts}/{Max}): reintento en {Minutes} min",
                    item.Email, item.Attempts, MaxAttempts, _failureDelay.TotalMinutes);
            }
            await context.SaveChangesAsync(stoppingToken);

            return _failureDelay;
        }
    }

    /// <summary>
    /// Cuerpo HTML del email de broadcast (mismo diseño que los emails transaccionales).
    /// </summary>
    private static string BuildEmailBody(string title, string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 20px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #0d6efd, #0a58ca); padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 22px;'>📢 {System.Net.WebUtility.HtmlEncode(title)}</h1>
        </div>
        <div style='padding: 30px;'>
            <p style='color: #333; font-size: 16px; line-height: 1.6; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
        </div>
        <div style='background: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef;'>
            <p style='color: #6c757d; font-size: 13px; margin: 0;'>Farmacia Solidaria Cristiana</p>
            <p style='color: #adb5bd; font-size: 11px; margin: 5px 0 0;'>Este mensaje fue enviado a todos los usuarios registrados.</p>
        </div>
    </div>
</body>
</html>";
    }
}
