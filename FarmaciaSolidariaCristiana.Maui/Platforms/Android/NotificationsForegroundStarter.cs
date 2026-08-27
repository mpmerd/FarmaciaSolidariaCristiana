#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using Application = Android.App.Application;

namespace FarmaciaSolidariaCristiana.Maui.Platforms.Android;

/// <summary>
/// Arranca/detiene el NotificationsForegroundService desde código compartido (#if ANDROID).
/// </summary>
public static class NotificationsForegroundStarter
{
    /// <summary>
    /// Inicia el Foreground Service (y con él la conexión SignalR en background).
    /// </summary>
    public static void Start()
    {
        try
        {
            var context = Application.Context;
            var intent = new Intent(context, typeof(NotificationsForegroundService));
            ContextCompat.StartForegroundService(context, intent);
            AppLog.Info("[FgStarter] Foreground service start solicitado");
        }
        catch (Exception ex)
        {
            AppLog.Info($"[FgStarter] Error al iniciar: {ex.Message}");
        }
    }

    /// <summary>
    /// Detiene el Foreground Service (y la conexión SignalR).
    /// </summary>
    public static void Stop()
    {
        try
        {
            var context = Application.Context;
            context.StopService(new Intent(context, typeof(NotificationsForegroundService)));
            AppLog.Info("[FgStarter] Foreground service stop solicitado");
        }
        catch (Exception ex)
        {
            AppLog.Info($"[FgStarter] Error al detener: {ex.Message}");
        }
    }
}
#endif
