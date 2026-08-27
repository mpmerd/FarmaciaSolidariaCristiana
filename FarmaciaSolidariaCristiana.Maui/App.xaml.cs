using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;
using OneSignalSDK.DotNet.Core.Debug;
using OneSignalSDK.DotNet.Core.User.Subscriptions;

namespace FarmaciaSolidariaCristiana.Maui;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly UpdateService _updateService;
    private readonly IServiceProvider _serviceProvider;
    
    // Static property to track OneSignal initialization status
    public static bool IsOneSignalInitialized { get; private set; }
    public static string? OneSignalPlayerId { get; private set; }
    public static string? OneSignalInitError { get; private set; }

    /// <summary>
    /// Ruta pendiente a la que navegar cuando la app vuelve a primer plano
    /// (ej. al tocar "Ver" en una notificación del sistema lanzada desde background).
    /// </summary>
    public static string? PendingRoute { get; set; }

    /// <summary>
    /// Proveedor de servicios raíz (para que el Foreground Service Android resuelva
    /// INotificationsHubClient sin pasar por el MauiContext que puede no estar listo tras un kill).
    /// </summary>
    public static IServiceProvider? Services { get; private set; }
    
    public App(IAuthService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _authService = authService;
        _serviceProvider = serviceProvider;
        Services = serviceProvider;
        _updateService = new UpdateService();
        
        // Initialize OneSignal
        InitializeOneSignal();
        
        // Check maintenance mode and updates after app starts
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2000); // Esperar 2 segundos después del inicio
            
            // Verificar mantenimiento primero
            await CheckMaintenanceModeAsync();
            
            // Luego verificar actualizaciones
            await _updateService.CheckForUpdatesAsync();
        });
    }

    private void InitializeOneSignal()
    {
        try
        {
#if DEBUG
            // Enable verbose OneSignal logging to debug issues
            OneSignal.Debug.LogLevel = LogLevel.VERBOSE;
#endif

            AppLog.Info($"[OneSignal] Initializing with AppId: {Constants.OneSignalAppId}");

            // OneSignal Initialization
            OneSignal.Initialize(Constants.OneSignalAppId);
            
            // Subscribe to push subscription changes
            OneSignal.User.PushSubscription.Changed += OnPushSubscriptionChanged;
            
            // Check if we already have a subscription ID
            var existingId = OneSignal.User.PushSubscription.Id;
            if (!string.IsNullOrEmpty(existingId))
            {
                OneSignalPlayerId = existingId;
                AppLog.Info($"[OneSignal] Already has PlayerId: {existingId}");
            }
            else
            {
                AppLog.Info("[OneSignal] No PlayerId yet, waiting for subscription...");
            }

            // Request notification permission (will show native prompt on Android 13+)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var granted = await OneSignal.Notifications.RequestPermissionAsync(true);
                    AppLog.Info($"[OneSignal] Permission granted: {granted}");
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[OneSignal] Permission request error: {ex.Message}");
                }
            });

            IsOneSignalInitialized = true;
            AppLog.Info("[OneSignal] Initialized successfully");
        }
        catch (Exception ex)
        {
            OneSignalInitError = ex.Message;
            AppLog.Info($"[OneSignal] Initialization error: {ex.Message}");
            AppLog.Info($"[OneSignal] Stack trace: {ex.StackTrace}");
        }
    }
    
    private void OnPushSubscriptionChanged(object? sender, PushSubscriptionChangedEventArgs e)
    {
        var newId = e.State.Current.Id;
        AppLog.Info($"[OneSignal] Push subscription changed. New ID: {newId}");

        if (!string.IsNullOrEmpty(newId))
        {
            OneSignalPlayerId = newId;
            AppLog.Info($"[OneSignal] PlayerId updated: {newId}");
        }

        // Fase 0/1: reportar disponibilidad real del canal OneSignal al servicio de salud.
        // Disponible = hay PlayerId (suscripción OK) Y permiso concedido.
        ReportOneSignalAvailabilityToHealthService(newId);
    }

    /// <summary>
    /// Reporta al PushHealthService si OneSignal es un canal instantáneo usable ahora mismo.
    /// </summary>
    private void ReportOneSignalAvailabilityToHealthService(string? playerId)
    {
        try
        {
            bool permission = false;
            try { permission = OneSignal.Notifications.Permission; }
            catch (Exception pex)
            {
                AppLog.Info($"[OneSignal] No se pudo leer permiso: {pex.Message}");
            }

            bool available = !string.IsNullOrEmpty(playerId) && permission;
            AppLog.Info(
                $"[OneSignal] Canal instantáneo available={available} (playerId={(!string.IsNullOrEmpty(playerId) ? "sí" : "no")}, permission={permission})");

            var pushHealth = _serviceProvider.GetService<IPushHealthService>();
            pushHealth?.ReportOneSignalAvailable(available);
        }
        catch (Exception ex)
        {
            AppLog.Info($"[OneSignal] Error reportando disponibilidad: {ex.Message}");
        }
    }

    private async Task CheckMaintenanceModeAsync()
    {
        try
        {
            var maintenance = await _updateService.CheckMaintenanceAsync();
            if (maintenance != null)
            {
                AppLog.Info($"[App] Maintenance mode active: {maintenance.reason}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("//MaintenancePage");

                    // Esperar un poco para que la página se cargue y luego setear la razón
                    await Task.Delay(300);
                    if (Shell.Current?.CurrentPage is Views.MaintenancePage page)
                    {
                        page.SetReason(maintenance.reason);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            AppLog.Info($"[App] Error checking maintenance: {ex.Message}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell(_authService));
        
        // Manejar cuando la app vuelve a primer plano
        window.Resumed += async (s, e) =>
        {
            AppLog.Info("[App] App resumed - checking maintenance, updates and notifications");
            
            // Verificar mantenimiento al volver a primer plano
            await CheckMaintenanceModeAsync();
            
            // Verificar actualizaciones obligatorias al volver a primer plano
            await _updateService.CheckForUpdatesAsync();
            
            try
            {
                var pollingService = _serviceProvider.GetService<IPollingNotificationService>();
                if (pollingService != null && pollingService.IsRunning)
                {
                    // Verificar notificaciones inmediatamente al volver a primer plano
                    var newCount = await pollingService.CheckNowAsync();
                    if (newCount > 0)
                    {
                        AppLog.Info($"[App] Found {newCount} new notifications on resume");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Info($"[App] Error checking notifications on resume: {ex.Message}");
            }

            // Navegar a ruta pendiente (desde acción "Ver" de una notificación del sistema).
            if (!string.IsNullOrEmpty(PendingRoute))
            {
                var route = PendingRoute;
                PendingRoute = null;
                try
                {
                    await Shell.Current.GoToAsync(route);
                    AppLog.Info($"[App] Navigated to pending route: {route}");
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[App] Error navigating to pending route {route}: {ex.Message}");
                }
            }
        };
        
        return window;
    }
}