using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.ViewModels;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class TurnosPage : ContentPage
{
    private readonly TurnosViewModel _viewModel;
    private readonly IPollingNotificationService _pollingService;

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
        await _viewModel.LoadTurnosCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollingService.NotificationReceived -= OnNotificationReceived;
    }

    private async void OnNotificationReceived(object? sender, NotificationReceivedEventArgs e)
    {
        // Refrescar la lista cuando llega una notificación relacionada con turnos
        if (e.NotificationType?.Contains("Turno") == true)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    await _viewModel.LoadTurnosCommand.ExecuteAsync(null);
                    System.Diagnostics.Debug.WriteLine($"[TurnosPage] Auto-refreshed after notification: {e.NotificationType}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TurnosPage] Error auto-refreshing: {ex.Message}");
                }
            });
        }
    }
}
