using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class TurnosPage : ContentPage
{
    private readonly TurnosViewModel _viewModel;
    private readonly IPollingNotificationService _pollingService;
    private readonly INotificationsHubClient _hubClient;
    private bool _initialized;

    public TurnosPage(TurnosViewModel viewModel, IPollingNotificationService pollingService, INotificationsHubClient hubClient)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _pollingService = pollingService;
        _hubClient = hubClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _pollingService.NotificationReceived += OnNotificationReceived;
        // Fase 2.8: también escuchar el canal SignalR (push real sobre 443).
        _hubClient.NotificationReceived += OnNotificationReceived;
        if (!_initialized)
        {
            _initialized = true;
            await _viewModel.LoadTurnosCommand.ExecuteAsync(null);
        }
        // Reloads posteriores usan caché automáticamente; los cambios de estado
        // (Aprobar, Rechazar, etc.) ya llaman LoadTurnosAsync() tras invalidar el caché.
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollingService.NotificationReceived -= OnNotificationReceived;
        _hubClient.NotificationReceived -= OnNotificationReceived;
    }

    private async void OnNotificationReceived(object? sender, NotificationReceivedEventArgs e)
    {
        // Cuando llega una notificación de turno, forzar refresco invalidando caché
        if (e.NotificationType?.Contains("Turno") == true)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await _viewModel.ForceRefreshAsync();
                    AppLog.Info($"[TurnosPage] Force-refreshed after notification: {e.NotificationType}");
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[TurnosPage] Error auto-refreshing: {ex.Message}");
                }
            });
        }
    }
}
