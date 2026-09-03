#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using Application = Android.App.Application;

namespace FarmaciaSolidariaCristiana.Maui.Platforms.Android;

/// <summary>
/// Foreground Service que mantiene viva la conexión SignalR en background.
/// Es lo que habilita el "push real" para usuarios en Cuba (donde FCM no entrega):
/// el proceso permanece activo y recibe notificaciones por el canal 443 aunque la UI esté cerrada.
/// Mantiene una notificación persistente (estilo Telegram).
/// </summary>
[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
public class NotificationsForegroundService : Service
{
    private const int ForegroundNotificationId = 7770;
    private const string ChannelId = "fsc_foreground";
    private const string ChannelName = "Servicio de notificaciones";

    private INotificationsHubClient? _hubClient;
    private System.Threading.Timer? _watchdogTimer;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        try
        {
            StartForegroundCompat();
            AppLog.Info("[FgService] Foreground service iniciado");
        }
        catch (Exception ex)
        {
            AppLog.Info($"[FgService] Error StartForeground: {ex.Message}");
        }

        // Arrancar el hub client (push real sobre 443).
        _ = StartHubAsync();

        // Watchdog: refuerzo ante OEM agresivos — si el hub quedó muerto
        // (o el loop de supervisión del propio hub fallara), se revive aquí.
        StartWatchdog();

        // START_STICKY: si Android mata el servicio, lo recrea y llama OnStartCommand de nuevo.
        return StartCommandResult.Sticky;
    }

    // Servicio iniciado (no enlazado): no exponemos un binder.
    public override IBinder? OnBind(Intent? intent) => null;

    private async Task StartHubAsync()
    {
        try
        {
            var services = App.Services;
            if (services == null)
            {
                AppLog.Info("[FgService] App.Services aún no disponible");
                return;
            }

            _hubClient = services.GetService<INotificationsHubClient>();
            if (_hubClient == null)
            {
                AppLog.Info("[FgService] INotificationsHubClient no registrado");
                return;
            }

            if (!_hubClient.IsConnected)
            {
                await _hubClient.StartAsync();
            }
        }
        catch (Exception ex)
        {
            AppLog.Info($"[FgService] Error iniciando hub: {ex.Message}");
        }
    }

    private async Task StopHubAsync()
    {
        try
        {
            if (_hubClient != null)
            {
                await _hubClient.StopAsync();
            }
        }
        catch (Exception ex)
        {
            AppLog.Info($"[FgService] Error deteniendo hub: {ex.Message}");
        }
    }

    /// <summary>
    /// Watchdog: cada Constants.SignalRWatchdogIntervalSeconds segundos verifica que el hub
    /// esté conectado; si no, lo revive (StartAsync es ensure-connected: no-op si ya vive).
    /// Es el refuerzo externo al loop de supervisión interno del hub client.
    /// </summary>
    private void StartWatchdog()
    {
        StopWatchdog();
        _watchdogTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                if (_hubClient == null)
                {
                    _hubClient = App.Services?.GetService<INotificationsHubClient>();
                }

                if (_hubClient != null && !_hubClient.IsConnected)
                {
                    AppLog.Info("[FgService] Watchdog: SignalR no conectado, reintentando");
                    await _hubClient.StartAsync();
                }
            }
            catch (Exception ex)
            {
                AppLog.Info($"[FgService] Watchdog error: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(Constants.SignalRWatchdogIntervalSeconds),
           TimeSpan.FromSeconds(Constants.SignalRWatchdogIntervalSeconds));
        AppLog.Info("[FgService] Watchdog iniciado");
    }

    private void StopWatchdog()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = null;
    }

    public override async void OnDestroy()
    {
        AppLog.Info("[FgService] OnDestroy");
        StopWatchdog();
        await StopHubAsync();
        base.OnDestroy();
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // Algunos OEM matan el servicio al cerrar la tarea. Mantener vivo (START_STICKY lo recrea).
        AppLog.Info("[FgService] OnTaskRemoved - manteniendo servicio");
        base.OnTaskRemoved(rootIntent);
    }

    private void StartForegroundCompat()
    {
        var context = Application.Context;
        EnsureForegroundChannel(context);

        var intent = new Intent(context, typeof(MainActivity));
        const PendingIntentFlags flags = PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent;
        var pendingIntent = PendingIntent.GetActivity(context, 0, intent, flags);

        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetContentTitle("Farmacia Solidaria")
            .SetContentText("Notificaciones activas")
            .SetOngoing(true)
            .SetContentIntent(pendingIntent);

        // ServiceCompat.StartForeground gestiona las diferencias de versión y el tipo de servicio.
        // TypeDataSync = 1 (Android.Content.PM.ForegroundService.TypeDataSync).
        var notif = builder.Build();
        ServiceCompat.StartForeground(this, ForegroundNotificationId, notif,
            (int)global::Android.Content.PM.ForegroundService.TypeDataSync);
    }

    private static void EnsureForegroundChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var mgr = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (mgr == null) return;
        if (mgr.GetNotificationChannel(ChannelId) != null) return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
        {
            Description = "Mantiene activas las notificaciones en segundo plano"
        };
        channel.EnableVibration(false);
        channel.LockscreenVisibility = NotificationVisibility.Private;
        mgr.CreateNotificationChannel(channel);
    }
}
#endif
