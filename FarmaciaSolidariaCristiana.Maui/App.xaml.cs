using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core;
using OneSignalSDK.DotNet.Core.Debug;
using OneSignalSDK.DotNet.Core.Notifications;

namespace FarmaciaSolidariaCristiana.Maui;

public partial class App : Application
{
    private readonly IAuthService _authService;
    
    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        // Initialize OneSignal
        InitializeOneSignal();
    }

    private void InitializeOneSignal()
    {
        try
        {
#if DEBUG
            // Enable verbose OneSignal logging to debug issues if needed.
            OneSignal.Debug.LogLevel = LogLevel.VERBOSE;
#endif

            // OneSignal Initialization
            OneSignal.Initialize(Constants.OneSignalAppId);

            // Request notification permission (will show native prompt)
            OneSignal.Notifications.RequestPermissionAsync(true);

            System.Diagnostics.Debug.WriteLine("OneSignal initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OneSignal initialization error: {ex.Message}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_authService));
    }
}