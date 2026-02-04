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
    
    // Static property to track OneSignal initialization status
    public static bool IsOneSignalInitialized { get; private set; }
    public static string? OneSignalPlayerId { get; private set; }
    public static string? OneSignalInitError { get; private set; }
    
    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        _updateService = new UpdateService();
        
        // Initialize OneSignal
        InitializeOneSignal();
        
        // Check for updates after app starts
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2000); // Esperar 2 segundos después del inicio
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

            System.Diagnostics.Debug.WriteLine($"[OneSignal] Initializing with AppId: {Constants.OneSignalAppId}");

            // OneSignal Initialization
            OneSignal.Initialize(Constants.OneSignalAppId);
            
            // Subscribe to push subscription changes
            OneSignal.User.PushSubscription.Changed += OnPushSubscriptionChanged;
            
            // Check if we already have a subscription ID
            var existingId = OneSignal.User.PushSubscription.Id;
            if (!string.IsNullOrEmpty(existingId))
            {
                OneSignalPlayerId = existingId;
                System.Diagnostics.Debug.WriteLine($"[OneSignal] Already has PlayerId: {existingId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[OneSignal] No PlayerId yet, waiting for subscription...");
            }

            // Request notification permission (will show native prompt on Android 13+)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var granted = await OneSignal.Notifications.RequestPermissionAsync(true);
                    System.Diagnostics.Debug.WriteLine($"[OneSignal] Permission granted: {granted}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OneSignal] Permission request error: {ex.Message}");
                }
            });

            IsOneSignalInitialized = true;
            System.Diagnostics.Debug.WriteLine("[OneSignal] Initialized successfully");
        }
        catch (Exception ex)
        {
            OneSignalInitError = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[OneSignal] Initialization error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OneSignal] Stack trace: {ex.StackTrace}");
        }
    }
    
    private void OnPushSubscriptionChanged(object? sender, PushSubscriptionChangedEventArgs e)
    {
        var newId = e.State.Current.Id;
        System.Diagnostics.Debug.WriteLine($"[OneSignal] Push subscription changed. New ID: {newId}");
        
        if (!string.IsNullOrEmpty(newId))
        {
            OneSignalPlayerId = newId;
            System.Diagnostics.Debug.WriteLine($"[OneSignal] PlayerId updated: {newId}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_authService));
    }
}