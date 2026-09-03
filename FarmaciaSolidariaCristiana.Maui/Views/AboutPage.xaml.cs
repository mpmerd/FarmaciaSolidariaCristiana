namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnWhatsAppTapped(object sender, EventArgs e)
    {
        try
        {
            // Abrir WhatsApp con el número de contacto
            var uri = new Uri("https://wa.me/5358140494");
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo abrir WhatsApp: {ex.Message}", "OK");
        }
    }
}
