using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class TurnosPage : ContentPage
{
    private readonly TurnosViewModel _viewModel;
    private readonly IPollingNotificationService _pollingService;
    private bool _initialized;

    public TurnosPage(TurnosViewModel viewModel, IPollingNotificationService pollingService)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _pollingService = pollingService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _pollingService.NotificationReceived += OnNotificationReceived;
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
                    System.Diagnostics.Debug.WriteLine($"[TurnosPage] Force-refreshed after notification: {e.NotificationType}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TurnosPage] Error auto-refreshing: {ex.Message}");
                }
            });
        }
    }
}
