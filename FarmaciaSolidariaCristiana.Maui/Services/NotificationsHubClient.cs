using System.Threading;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del cliente SignalR sobre 443.
/// Si el host no soporta WebSocket, el cliente cae automáticamente a SSE/long-polling (mismo puerto 443).
/// <para>
/// Auto-sanación (Fase 5): la política de reconexión es infinita (nunca pasa a Closed por agotamiento),
/// un loop de supervisión revive la conexión si queda muerta (start inicial fallido, Closed, etc.),
/// y el evento <see cref="Connectivity.ConnectivityChanged"/> fuerza una reconexión inmediata
/// cuando la red vuelve (ej. una noche sin internet en Cuba → SignalR regresa solo al amanecer).
/// </para>
/// </summary>
public class NotificationsHubClient : INotificationsHubClient, IDisposable
{
    private readonly IAuthService _authService;
    private readonly IPushHealthService _pushHealth;
    private readonly ISystemNotificationService _systemNotification;
    private readonly ILogger<NotificationsHubClient>? _logger;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private CancellationTokenSource? _supervisionCts;
    private Task? _supervisionTask;
    private volatile bool _running;
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

        // Al volver la red (ej. madrugada sin internet → amanecer con internet):
        // reconexión inmediata, sin esperar el próximo tick del loop de supervisión.
        try
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }
        catch (Exception ex)
        {
            AppLog.Info($"[HubClient] No se pudo suscribir a ConnectivityChanged: {ex.Message}");
        }
    }

    public async Task StartAsync()
    {
        if (!Constants.SignalRChannelEnabled)
        {
            AppLog.Info("[HubClient] SignalR deshabilitado por feature flag (Constants.SignalRChannelEnabled=false)");
            return;
        }

        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            AppLog.Info("[HubClient] No hay JWT, no se inicia SignalR");
            return;
        }

        _running = true;

        // Ensure-connected: si ya está conectada no-op; si existe pero está muerta, se recrea.
        await EnsureConnectedAsync(CancellationToken.None, force: false);

        StartSupervisionLoop();
    }

    public async Task StopAsync()
    {
        _running = false;

        // Cancelar el loop de supervisión y cualquier start en vuelo (token enlazado).
        _supervisionCts?.Cancel();
        if (_supervisionTask != null)
        {
            try { await Task.WhenAny(_supervisionTask, Task.Delay(TimeSpan.FromSeconds(25))); }
            catch { /* esperado si ya terminó */ }
        }

        await _startLock.WaitAsync();
        try
        {
            await DisposeConnectionAsync();
            _pushHealth.ReportSignalRConnected(false);
            AppLog.Info("[HubClient] Detenido");
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Garantiza que exista una conexión viva. Si la conexión actual está muerta
    /// (Disconnected o null) se descarta y se crea una nueva. Idempotente.
    /// </summary>
    /// <param name="ct">Cancelación (desde StopAsync vía el token de supervisión).</param>
    /// <param name="force">true para descartar incluso una conexión en Reconnecting
    /// (usado al volver la red: reconexión inmediata en vez de esperar el backoff).</param>
    private async Task EnsureConnectedAsync(CancellationToken ct, bool force)
    {
        await _startLock.WaitAsync(ct);
        try
        {
            if (!ct.IsCancellationRequested && !_running)
                return;

            var state = _connection?.State;

            if (state == HubConnectionState.Connected)
                return; // ya viva

            if (!force && (state == HubConnectionState.Reconnecting || state == HubConnectionState.Connecting))
            {
                // La política infinita de auto-reconnect (o un start en curso) ya está trabajando.
                AppLog.Info($"[HubClient] Estado {state}, auto-reconnect en curso (retry infinito)");
                return;
            }

            // Disconnected, null (o force): recrear desde cero.
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                AppLog.Info("[HubClient] Sin JWT, no se puede (re)conectar");
                return;
            }

            await DisposeConnectionAsync();
            _connection = BuildConnection();
            var conn = _connection;

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
                await conn.StartAsync(timeoutCts.Token);
                _pushHealth.ReportSignalRConnected(true);
                AppLog.Info("[HubClient] Conectado (push real sobre 443 activo)");
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                AppLog.Info("[HubClient] Start timeout (20s), el loop de supervisión reintentará");
                _pushHealth.ReportSignalRConnected(false);
            }
            catch (Exception ex)
            {
                AppLog.Info($"[HubClient] Start falló: {ex.Message} (el loop de supervisión reintentará)");
                _pushHealth.ReportSignalRConnected(false);
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    private HubConnection BuildConnection()
    {
        var hubUrl = $"{Constants.ApiBaseUrl.TrimEnd('/')}/hubs/notifications";
        AppLog.Info($"[HubClient] Construyendo conexión a {hubUrl}");

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    // Se reevalúa el token en cada reconnect (refresca si expira)
                    var t = await _authService.GetTokenAsync();
                    return string.IsNullOrEmpty(t) ? null : t;
                };
            })
            // Retry INFINITO real (0→2→10s y luego 30s para siempre). La política default
            // de WithAutomaticReconnect() se rinde tras 4 intentos (~42s) y la conexión
            // pasa a Closed para siempre — era la causa de "SignalR muerto tras una noche sin internet".
            .WithAutomaticReconnect(new InfiniteRetryPolicy())
            .Build();

        // Somee (IIS compartido) no envía los keep-alive de SignalR con fiabilidad,
        // lo que provoca timeouts cada 30s. Ampliamos el timeout del servidor para
        // reducir los huecos de desconexión (menos ventanas donde se pierde una entrega).
        connection.ServerTimeout = TimeSpan.FromMinutes(3);
        connection.KeepAliveInterval = TimeSpan.FromSeconds(15);

        connection.Reconnecting += ex =>
        {
            AppLog.Info($"[HubClient] Reconectando: {ex?.Message}");
            _pushHealth.ReportSignalRConnected(false);
            return Task.CompletedTask;
        };

        connection.Reconnected += connectionId =>
        {
            AppLog.Info($"[HubClient] Reconectado: {connectionId}");
            _pushHealth.ReportSignalRConnected(true);
            return Task.CompletedTask;
        };

        connection.Closed += ex =>
        {
            AppLog.Info($"[HubClient] Conexión cerrada: {ex?.Message} (la supervisión la revivirá)");
            _pushHealth.ReportSignalRConnected(false);
            return Task.CompletedTask;
        };

        connection.On<HubNotificationPayload>("ReceiveNotification", async payload =>
        {
            await OnReceiveNotificationAsync(payload);
        });

        return connection;
    }

    /// <summary>
    /// Loop de supervisión: revisa periódicamente que la conexión esté viva y,
    /// si está muerta (null/Disconnected), la recrea. Cubre el fallo del start
    /// inicial, el estado Closed y cualquier caso residual del ciclo de vida.
    /// </summary>
    private void StartSupervisionLoop()
    {
        if (_supervisionTask != null && !_supervisionTask.IsCompleted)
            return; // ya corre

        _supervisionCts = new CancellationTokenSource();
        var ct = _supervisionCts.Token;

        _supervisionTask = Task.Run(async () =>
        {
            AppLog.Info($"[HubClient] Loop de supervisión iniciado (cada {Constants.SignalRSupervisionIntervalSeconds}s)");
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Constants.SignalRSupervisionIntervalSeconds), ct);
                    if (!_running)
                        continue;

                    var state = _connection?.State;
                    if (state == HubConnectionState.Connected)
                        continue;
                    if (state == HubConnectionState.Reconnecting || state == HubConnectionState.Connecting)
                        continue; // auto-reconnect infinito en curso

                    AppLog.Info($"[HubClient] Supervisión: conexión en estado {state ?? null}, reviviendo...");
                    await EnsureConnectedAsync(ct, force: false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[HubClient] Error en supervisión: {ex.Message}");
                }
            }
            AppLog.Info("[HubClient] Loop de supervisión detenido");
        }, ct);
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (!_running)
            return;
        if (e.NetworkAccess != NetworkAccess.Internet)
            return;
        if (_connection?.State == HubConnectionState.Connected)
            return;

        AppLog.Info("[HubClient] Red disponible de nuevo — reconexión inmediata de SignalR");
        _ = EnsureConnectedAsync(CancellationToken.None, force: true);
    }

    private async Task DisposeConnectionAsync()
    {
        var conn = _connection;
        _connection = null;
        if (conn != null)
        {
            try { await conn.StopAsync(); } catch { }
            try { await conn.DisposeAsync(); } catch { }
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
        _running = false;
        try { Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged; } catch { }
        try { _supervisionCts?.Cancel(); _supervisionCts?.Dispose(); } catch { }
        try { _connection?.DisposeAsync().AsTask().Wait(1000); } catch { }
        _startLock.Dispose();
    }

    /// <summary>
    /// Política de reconexión infinita: 0s → 2s → 10s y luego 30s para siempre.
    /// Nunca devuelve null (null = rendirse → Closed permanente), que era el bug
    /// de la política default de WithAutomaticReconnect().
    /// </summary>
    private sealed class InfiniteRetryPolicy : IRetryPolicy
    {
        private int _attempt;
        private static readonly TimeSpan[] InitialDelays =
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10)
        };

        public TimeSpan? NextRetryDelay(RetryContext context)
        {
            _attempt++;
            if (_attempt <= InitialDelays.Length)
                return InitialDelays[_attempt - 1];

            return TimeSpan.FromSeconds(30); // cap para siempre
        }
    }
}
