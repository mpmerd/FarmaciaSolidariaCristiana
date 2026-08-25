#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Application = Android.App.Application;

namespace FarmaciaSolidariaCristiana.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Ruta desde una notificación del sistema (acción "Ver").
        HandleRouteIntent(Intent);

        // Exención de optimización de batería (una sola vez) para que el Foreground Service
        // sobreviva a Doze/OEM agresivos (Xiaomi/Huawei, comunes en Cuba).
        TryRequestBatteryExemptionOnce();
    }

    protected override void OnNewIntent(Intent? newIntent)
    {
        base.OnNewIntent(newIntent);
        HandleRouteIntent(newIntent);
    }

    private static void HandleRouteIntent(Intent? intent)
    {
        try
        {
            if (intent == null) return;
            var route = intent.GetStringExtra("route");
            if (!string.IsNullOrEmpty(route))
            {
                App.PendingRoute = route;
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Route extra: {route}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] HandleRouteIntent: {ex.Message}");
        }
    }

    private static void TryRequestBatteryExemptionOnce()
    {
        try
        {
            const string key = "battery_exemption_requested";
            if (Preferences.Get(key, false)) return;

            var pm = Application.Context.GetSystemService(Context.PowerService) as PowerManager;
            if (pm == null) return;

            // Solo pedimos si NO está ya exento.
            if (pm.IsIgnoringBatteryOptimizations(Application.Context.PackageName)) return;

            Preferences.Set(key, true);
            var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + Application.Context.PackageName));
            intent.AddFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
            System.Diagnostics.Debug.WriteLine("[MainActivity] Solicitando exención de batería (primera vez)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Battery exemption: {ex.Message}");
        }
    }
}
#endif
