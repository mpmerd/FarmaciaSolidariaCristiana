#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FarmaciaSolidariaCristiana.Maui.Services;
using Plugin.Maui.Audio;
using Application = Android.App.Application;

namespace FarmaciaSolidariaCristiana.Maui.Platforms.Android.Services;

/// <summary>
/// Implementación Android de ISystemNotificationService.
/// Muestra notificaciones nativas (barra de estado) con sonido notfar.mp3 (reproducido in-process,
/// el proceso está vivo gracias al Foreground Service) y acción "Ver".
/// </summary>
public class SystemNotificationService : ISystemNotificationService
{
    private const string ChannelId = "fsc_notifications";
    private const string ChannelName = "Notificaciones de Farmacia";

    private static int _nextId = 1000;

    public async Task ShowAsync(string title, string message, string notificationType, int? referenceId = null)
    {
        try
        {
            var context = Application.Context;

            EnsureChannel(context);

            // Sonido in-process (el Foreground Service mantiene el proceso vivo en background).
            await PlaySoundAsync();

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

            NotificationManagerCompat.From(context).Notify(notifId, builder.Build());

            System.Diagnostics.Debug.WriteLine($"[SysNotif] Mostrada notificación del sistema: {title}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SysNotif] Error: {ex.Message}");
        }
    }

    private static void EnsureChannel(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var mgr = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (mgr == null) return;
        if (mgr.GetNotificationChannel(ChannelId) != null) return;

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.High)
        {
            Description = "Notificaciones de turnos y avisos de la Farmacia Solidaria"
        };
        channel.EnableVibration(true);
        channel.LockscreenVisibility = NotificationVisibility.Public;
        mgr.CreateNotificationChannel(channel);
    }

    private static async Task PlaySoundAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("notfar.mp3");
            var player = AudioManager.Current.CreatePlayer(stream);
            player.Play();
            // No esperamos a que termine para no bloquear; el player se autolimpia al acabar.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SysNotif] Sonido: {ex.Message}");
        }
    }
}
#endif
