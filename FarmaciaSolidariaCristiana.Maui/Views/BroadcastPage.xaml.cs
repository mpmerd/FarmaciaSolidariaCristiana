using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class BroadcastPage : ContentPage
{
    private readonly IApiService _apiService;

    public BroadcastPage(IApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        EditorMessage.TextChanged += (s, e) =>
        {
            LblCharCount.Text = $"{EditorMessage.Text?.Length ?? 0}/2000";
        };
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var title = EntryTitle.Text?.Trim();
        var message = EditorMessage.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Error", "El título y el mensaje son requeridos.", "OK");
            return;
        }

        if (!SwitchEmail.IsToggled && !SwitchNotification.IsToggled)
        {
            await DisplayAlert("Error", "Debe seleccionar al menos un canal de envío.", "OK");
            return;
        }

        var channels = new List<string>();
        if (SwitchEmail.IsToggled) channels.Add("email");
        if (SwitchNotification.IsToggled) channels.Add("notificación en app");

        var confirm = await DisplayAlert(
            "Confirmar envío",
            $"¿Enviar esta notificación a TODOS los usuarios por {string.Join(" y ", channels)}?\n\nEsta acción no se puede deshacer.",
            "Enviar", "Cancelar");

        if (!confirm) return;

        BtnSend.IsEnabled = false;
        BtnSend.Text = "Enviando...";
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var result = await _apiService.SendBroadcastAsync(
                title, message, SwitchEmail.IsToggled, SwitchNotification.IsToggled);

            if (result.Success && result.Data != null)
            {
                var summary = new List<string>();
                if (SwitchEmail.IsToggled)
                {
                    var emailMsg = $"{result.Data.EmailsSent} emails enviados";
                    if (result.Data.EmailsFailed > 0)
                        emailMsg += $" ({result.Data.EmailsFailed} fallidos)";
                    summary.Add(emailMsg);
                }
                if (SwitchNotification.IsToggled)
                    summary.Add($"{result.Data.NotificationsCreated} notificaciones creadas");

                await DisplayAlert("Éxito",
                    $"Notificación masiva enviada:\n{string.Join("\n", summary)}",
                    "OK");

                // Limpiar formulario
                EntryTitle.Text = string.Empty;
                EditorMessage.Text = string.Empty;
            }
            else
            {
                await DisplayAlert("Error", result.Message ?? "Error al enviar la notificación.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
        }
        finally
        {
            BtnSend.IsEnabled = true;
            BtnSend.Text = "📢 Enviar Notificación Masiva";
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}
