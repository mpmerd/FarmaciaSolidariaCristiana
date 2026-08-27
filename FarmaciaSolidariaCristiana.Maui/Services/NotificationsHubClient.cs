using System.Threading;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del cliente SignalR sobre 443.
/// Si el host no soporta WebSocket, el cliente cae automáticamente a SSE/long-polling (mismo puerto 443).
/// </summary>
public class NotificationsHubClient : INotificationsHubClient, IDisposable
{
    private readonly IAuthService _authService;
    private readonly IPushHealthService _pushHealth;
    private readonly ISystemNotificationService _systemNotification;
    private readonly ILogger<NotificationsHubClient>? _logger;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _disposed;

    public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public NotificationsHubClient(
        IAuthService authService,
        IPushHealthService pushHealth,
        ISystemNotificationService systemNotification,
        ILogger<NotificationsHubClient>? logger = null)
    {
        _authService = authService;
        _pushHealth = pushHealth;
        _systemNotification = systemNotification;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        await _startLock.WaitAsync();
        try
        {
            if (!Constants.SignalRChannelEnabled)
            {
                AppLog.Info("[HubClient] SignalR deshabilitado por feature flag (Constants.SignalRChannelEnabled=false)");
                return;
            }

            if (_connection != null)
            {
                AppLog.Info("[HubClient] Ya existe conexión, skip start");
                return;
            }

            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                AppLog.Info("[HubClient] No hay JWT, no se inicia SignalR");
                return;
            }

            var hubUrl = $"{Constants.ApiBaseUrl.TrimEnd('/')}/hubs/notifications";
            AppLog.Info($"[HubClient] Construyendo conexión a {hubUrl}");

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        // Se reevalúa el token en cada reconnect (refresca si expira)
                        var t = await _authService.GetTokenAsync();
                        return string.IsNullOrEmpty(t) ? null : t;
                    };
                })
                .WithAutomaticReconnect() // retry infinito con backoff (0,2,10,30s...) - no se rinde
                .Build();

            // Somee (IIS compartido) no envía los keep-alive de SignalR con fiabilidad,
            // lo que provoca timeouts cada 30s. Ampliamos el timeout del servidor para
            // reducir los huecos de desconexión (menos ventanas donde se pierde una entrega).
            _connection.ServerTimeout = TimeSpan.FromMinutes(3);
            _connection.KeepAliveInterval = TimeSpan.FromSeconds(15);

            _connection.Reconnecting += ex =>
            {
                AppLog.Info($"[HubClient] Reconectando: {ex?.Message}");
                _pushHealth.ReportSignalRConnected(false);
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                AppLog.Info($"[HubClient] Reconectado: {connectionId}");
                _pushHealth.ReportSignalRConnected(true);
                return Task.CompletedTask;
            };

            _connection.Closed += ex =>
            {
                AppLog.Info($"[HubClient] Conexión cerrada: {ex?.Message}");
                _pushHealth.ReportSignalRConnected(false);
                return Task.CompletedTask;
            };

            _connection.On<HubNotificationPayload>("ReceiveNotification", async payload =>
            {
                await OnReceiveNotificationAsync(payload);
            });

            try
            {
                await _connection.StartAsync();
                _pushHealth.ReportSignalRConnected(true);
                AppLog.Info("[HubClient] Conectado (push real sobre 443 activo)");
            }
            catch (Exception ex)
            {
                AppLog.Info($"[HubClient] Start falló: {ex.Message} (auto-reconnect reintentará)");
                _pushHealth.ReportSignalRConnected(false);
                // WithAutomaticReconnect no cubre el fallo del Start inicial;
                // dejamos la conexión viva para que reintente, o se relanzará desde el foreground service.
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _startLock.WaitAsync();
        try
        {
            var conn = _connection;
            _connection = null;
            if (conn != null)
            {
                try
                {
                    await conn.StopAsync();
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[HubClient] Error en StopAsync: {ex.Message}");
                }
                try { await conn.DisposeAsync(); } catch { }
            }
            _pushHealth.ReportSignalRConnected(false);
            AppLog.Info("[HubClient] Detenido");
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task OnReceiveNotificationAsync(HubNotificationPayload payload)
    {
        try
        {
            // De-dup: si ya fue entregada antes (catch-up en un reconectar anterior),
            // no la volvemos a mostrar/sonar.
            if (_pushHealth.WasDeliveredInstantly(payload.Id))
            {
                AppLog.Info($"[HubClient] Notificación #{payload.Id} ya entregada, skip (dedup)");
                return;
            }

            AppLog.Info($"[HubClient] Recibida notificación #{payload.Id}: {payload.Title}");

            // De-dup: reportar entrega al PushHealthService para que el polling no la repita.
            _pushHealth.ReportDelivery(payload.Id, payload.CreatedAt);

            // Refresco de UI (mismo contrato que el polling).
            var args = new NotificationReceivedEventArgs
            {
                NotificationId = payload.Id,
                Title = payload.Title,
                Message = payload.Message,
                NotificationType = payload.NotificationType,
                ReferenceId = payload.ReferenceId,
                ReferenceType = payload.ReferenceType,
                CreatedAt = payload.CreatedAt
            };

            try { NotificationReceived?.Invoke(this, args); }
            catch (Exception ex) { AppLog.Info($"[HubClient] Error en handler de NotificationReceived: {ex.Message}"); }

            // Notificación del sistema (visible en background y foreground).
            try { await _systemNotification.ShowAsync(payload.Title, payload.Message, payload.NotificationType, payload.ReferenceId); }
            catch (Exception ex) { AppLog.Info($"[HubClient] Error mostrando notificación del sistema: {ex.Message}"); }
        }
        catch (Exception ex)
        {
            AppLog.Info($"[HubClient] Error procesando notificación recibida: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _connection?.DisposeAsync().AsTask().Wait(1000); } catch { }
        _startLock.Dispose();
    }
}
