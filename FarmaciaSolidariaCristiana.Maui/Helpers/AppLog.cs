namespace FarmaciaSolidariaCristiana.Maui.Helpers;

/// <summary>
/// Logger unificado que SÍ emite a logcat en builds de release, a diferencia de
/// <c>System.Diagnostics.Debug.WriteLine</c> (no-op en release). En Android usa
/// <see cref="Android.Util.Log"/>; en otras plataformas cae a Debug.WriteLine.
/// <para>Filtrar en logcat con: <c>adb logcat -s FSC</c></para>
/// <para>Los mensajes conservan su prefijo de módulo ([FgService], [HubClient],
/// [PollingService], [AppShell], etc.) para poder filtrar por módulo.</para>
/// </summary>
public static class AppLog
{
    public const string Tag = "FSC";

    public static void Info(string? message)
    {
#if ANDROID
        Android.Util.Log.Info(Tag, message ?? string.Empty);
#else
        System.Diagnostics.Debug.WriteLine(message);
#endif
    }

    public static void Warn(string? message)
    {
#if ANDROID
        Android.Util.Log.Warn(Tag, message ?? string.Empty);
#else
        System.Diagnostics.Debug.WriteLine(message);
#endif
    }

    public static void Error(string? message)
    {
#if ANDROID
        Android.Util.Log.Error(Tag, message ?? string.Empty);
#else
        System.Diagnostics.Debug.WriteLine(message);
#endif
    }
}
