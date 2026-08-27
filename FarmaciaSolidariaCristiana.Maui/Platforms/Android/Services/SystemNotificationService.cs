#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using Application = Android.App.Application;

namespace FarmaciaSolidariaCristiana.Maui.Platforms.Android.Services;

/// <summary>
/// Implementación Android de ISystemNotificationService.
/// Muestra notificaciones nativas (barra de estado) con sonido notfar.mp3 como sonido del
/// canal (lo reproduce el sistema, no el proceso) y acción "Ver".
/// </summary>
public class SystemNotificationService : ISystemNotificationService
{
    private const string ChannelId = "fsc_notifications_v2";
    private const string LegacyChannelId = "fsc_notifications";
    private const string ChannelName = "Notificaciones de Farmacia";

    private static int _nextId = 1000;

    public async Task ShowAsync(string title, string message, string notificationType, int? referenceId = null)
    {
        try
        {
            var context = Application.Context;

            EnsureChannel(context);

            // Intent: abrir MainActivity; si es notificación de turno, navegar a Mis Turnos.
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            if (!string.IsNullOrEmpty(notificationType) && notificationType.Contains("Turno"))
            {
                intent.PutExtra("route", "//TurnosPage");
            }

            const PendingIntentFlags flags = PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent;
            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, flags);

            var notifId = referenceId ?? System.Threading.Interlocked.Increment(ref _nextId);

            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
                .SetPriority((int)NotificationPriority.High)
                .SetCategory(NotificationCompat.CategoryCall)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            // API < 26: no existen canales; el sonido se define en el builder.
            if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                builder.SetSound(NotificationSoundUri(context));
            }

            NotificationManagerCompat.From(context).Notify(notifId, builder.Build());

            AppLog.Info($"[SysNotif] Mostrada notificación del sistema: {title}");
        }
        catch (Exception ex)
        {
            AppLog.Info($"[SysNotif] Error: {ex.Message}");
        }
    }

    private static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var mgr = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (mgr == null) return;

        // El canal no es modificable una vez creado. La v1 quedó con el sonido por defecto;
        // creamos la v2 con notfar.mp3 como sonido del canal y eliminamos la v1 (Android la
        // recrea con la nueva configuración en el siguiente arranque).
        mgr.DeleteNotificationChannel(LegacyChannelId);

        if (mgr.GetNotificationChannel(ChannelId) != null) return;

        var attrs = new global::Android.Media.AudioAttributes.Builder()
            .SetUsage(global::Android.Media.AudioUsageKind.Notification)
            .SetContentType(global::Android.Media.AudioContentType.Sonification)
            .Build();

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
        {
            Description = "Notificaciones de turnos y avisos de la Farmacia Solidaria"
        };
        channel.EnableVibration(true);
        channel.LockscreenVisibility = NotificationVisibility.Public;
        channel.SetSound(NotificationSoundUri(context), attrs);
        mgr.CreateNotificationChannel(channel);
    }

    private static global::Android.Net.Uri NotificationSoundUri(Context context)
        => global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/raw/notfar");
}
#endif
