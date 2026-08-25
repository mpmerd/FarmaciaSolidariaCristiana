namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Servicio central que determina si existe un canal instantáneo disponible
/// (OneSignal con playerId+permiso, o SignalR conectado en Fase 2).
/// El PollingNotificationService lo consulta para decidir entre modo completo
/// o modo solo-heartbeat (estado deseado: push-first, polling solo si falla).
/// También lleva un watermark de entregas instantáneas para de-duplicar frente al polling.
/// </summary>
public interface IPushHealthService
{
    /// <summary>
    /// True si algún canal instantáneo (OneSignal o SignalR) está disponible ahora mismo.
    /// </summary>
    bool IsInstantChannelAvailable { get; }

    /// <summary>
    /// Timestamp de la última notificación entregada por un canal instantáneo.
    /// </summary>
    DateTime? LastInstantDeliveryAt { get; }

    /// <summary>
    /// Se dispara cuando la disponibilidad de un canal instantáneo cambia.
    /// </summary>
    event EventHandler? AvailabilityChanged;

    /// <summary>
    /// Reporta el estado de disponibilidad del canal OneSignal (playerId + permiso).
    /// </summary>
    void ReportOneSignalAvailable(bool available);

    /// <summary>
    /// Reporta el estado de conexión del canal SignalR (Fase 2).
    /// </summary>
    void ReportSignalRConnected(bool connected);

    /// <summary>
    /// Reporta que una notificación fue entregada por un canal instantáneo (para de-dup vs polling).
    /// </summary>
    void ReportDelivery(int notificationId, DateTime createdAt);

    /// <summary>
    /// Indica si una notificación ya fue entregada por un canal instantáneo.
    /// </summary>
    bool WasDeliveredInstantly(int notificationId);

    /// <summary>
    /// Reinicia el estado (usar en logout).
    /// </summary>
    void Reset();
}

/// <summary>
/// Implementación singleton thread-safe.
/// </summary>
public class PushHealthService : IPushHealthService
{
    private readonly object _lock = new();
    private bool _oneSignalAvailable;
    private bool _signalRConnected;
    private DateTime? _lastInstantDeliveryAt;
    private readonly HashSet<int> _deliveredIds = new();
    private readonly Queue<int> _deliveredQueue = new();
    private const int MaxDeliveredCache = 500;

    public bool IsInstantChannelAvailable
    {
        get
        {
            lock (_lock)
            {
                return _oneSignalAvailable || _signalRConnected;
            }
        }
    }

    public DateTime? LastInstantDeliveryAt
    {
        get
        {
            lock (_lock)
            {
                return _lastInstantDeliveryAt;
            }
        }
    }

    public event EventHandler? AvailabilityChanged;

    public void ReportOneSignalAvailable(bool available)
    {
        bool changed;
        lock (_lock)
        {
            if (_oneSignalAvailable == available)
                return;
            _oneSignalAvailable = available;
            changed = true;
        }

        if (changed)
        {
            System.Diagnostics.Debug.WriteLine($"[PushHealth] OneSignal available = {available}");
            RaiseAvailabilityChanged();
        }
    }

    public void ReportSignalRConnected(bool connected)
    {
        bool changed;
        lock (_lock)
        {
            if (_signalRConnected == connected)
                return;
            _signalRConnected = connected;
            changed = true;
        }

        if (changed)
        {
            System.Diagnostics.Debug.WriteLine($"[PushHealth] SignalR connected = {connected}");
            RaiseAvailabilityChanged();
        }
    }

    public void ReportDelivery(int notificationId, DateTime createdAt)
    {
        lock (_lock)
        {
            if (!_deliveredIds.Contains(notificationId))
            {
                _deliveredIds.Add(notificationId);
                _deliveredQueue.Enqueue(notificationId);

                while (_deliveredQueue.Count > MaxDeliveredCache)
                {
                    var oldest = _deliveredQueue.Dequeue();
                    _deliveredIds.Remove(oldest);
                }
            }

            if (_lastInstantDeliveryAt == null || createdAt > _lastInstantDeliveryAt)
            {
                _lastInstantDeliveryAt = createdAt;
            }
        }
    }

    public bool WasDeliveredInstantly(int notificationId)
    {
        lock (_lock)
        {
            return _deliveredIds.Contains(notificationId);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _oneSignalAvailable = false;
            _signalRConnected = false;
            _lastInstantDeliveryAt = null;
            _deliveredIds.Clear();
            _deliveredQueue.Clear();
        }
    }

    private void RaiseAvailabilityChanged()
    {
        try
        {
            AvailabilityChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PushHealth] AvailabilityChanged handler error: {ex.Message}");
        }
    }
}
