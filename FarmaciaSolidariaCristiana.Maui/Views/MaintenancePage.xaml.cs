using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class MaintenancePage : ContentPage
{
    private readonly UpdateService _updateService = new();

    public MaintenancePage()
    {
        InitializeComponent();
    }

    public void SetReason(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            LblReason.Text = reason;
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        BtnRetry.IsEnabled = false;
        BtnRetry.Text = "Verificando...";

        try
        {
            var maintenance = await _updateService.CheckMaintenanceAsync();

            if (maintenance == null)
            {
                // Ya no está en mantenimiento, volver a la app
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                SetReason(maintenance.reason);
                await DisplayAlert("Mantenimiento", 
                    "El sistema sigue en mantenimiento. Intente más tarde.", "OK");
            }
        }
        catch
        {
            await DisplayAlert("Error", 
                "No se pudo verificar el estado. Intente más tarde.", "OK");
        }
        finally
        {
            BtnRetry.IsEnabled = true;
            BtnRetry.Text = "Reintentar";
        }
    }
}
