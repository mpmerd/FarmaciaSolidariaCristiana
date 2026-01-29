using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class DocumentoViewerPage : ContentPage
{
    private readonly TurnoDocumento _documento;
    private readonly HttpClient _httpClient;
    private string? _localFilePath;

    public DocumentoViewerPage(TurnoDocumento documento)
    {
        InitializeComponent();
        _documento = documento;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        Title = documento.DocumentType;
        DocTypeLabel.Text = documento.DocumentType;
        FileNameLabel.Text = documento.FileName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDocumentAsync();
    }

    private async Task LoadDocumentAsync()
    {
        try
        {
            LoadingView.IsVisible = true;
            LoadingLabel.Text = "Verificando documento...";
            ErrorView.IsVisible = false;
            ImageScrollView.IsVisible = false;
            PdfViewer.IsVisible = false;

            var url = _documento.FullUrl;
            
            if (string.IsNullOrEmpty(url))
            {
                ShowError("URL del documento no disponible");
                return;
            }

            // Verificar si el documento existe
            var exists = await CheckDocumentExistsAsync(url);
            
            if (!exists)
            {
                ShowError("Documento inexistente");
                return;
            }

            // Determinar tipo de documento
            var isImage = _documento.ContentType?.StartsWith("image/") == true ||
                         _documento.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

            var isPdf = _documento.ContentType == "application/pdf" ||
                       _documento.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (isImage)
            {
                await LoadImageAsync(url);
            }
            else if (isPdf)
            {
                await LoadPdfAsync(url);
            }
            else
            {
                // Tipo desconocido, intentar como imagen primero
                await LoadImageAsync(url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error: {ex.Message}");
            ShowError("Error al cargar el documento");
        }
    }

    private async Task LoadImageAsync(string url)
    {
        LoadingLabel.Text = "Cargando imagen...";
        
        try
        {
            ImageViewer.Source = ImageSource.FromUri(new Uri(url));
            
            LoadingView.IsVisible = false;
            ImageScrollView.IsVisible = true;
            InfoFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error loading image: {ex.Message}");
            ShowError("Error al cargar la imagen");
        }
    }

    private async Task LoadPdfAsync(string url)
    {
        LoadingLabel.Text = "Descargando PDF...";
        
        try
        {
            // Descargar el PDF a un archivo temporal
            var pdfBytes = await _httpClient.GetByteArrayAsync(url);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                ShowError("No se pudo descargar el PDF");
                return;
            }

            LoadingLabel.Text = "Preparando visor...";

            // Guardar en cache
            var fileName = $"temp_{Guid.NewGuid()}.pdf";
            _localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(_localFilePath, pdfBytes);

            // Usar Google Docs Viewer embebido en WebView
            // Este enfoque funciona mejor para PDFs en WebView de Android
            var encodedUrl = Uri.EscapeDataString(url);
            var viewerUrl = $"https://docs.google.com/gview?embedded=true&url={encodedUrl}";
            
            PdfViewer.Source = viewerUrl;
            
            LoadingView.IsVisible = false;
            PdfViewer.IsVisible = true;
            InfoFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error loading PDF: {ex.Message}");
            
            // Si falla Google Docs, intentar abrir localmente
            if (!string.IsNullOrEmpty(_localFilePath) && File.Exists(_localFilePath))
            {
                var openExternal = await DisplayAlert(
                    "Visor PDF",
                    "No se pudo mostrar el PDF en la app. ¿Desea abrirlo con otra aplicación?",
                    "Sí", "No");
                    
                if (openExternal)
                {
                    await OpenPdfExternallyAsync();
                    return;
                }
            }
            
            ShowError("Error al cargar el PDF");
        }
    }

    private async Task OpenPdfExternallyAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_localFilePath) && File.Exists(_localFilePath))
            {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_localFilePath)
                });
            }
            else
            {
                // Abrir URL directamente
                await Browser.Default.OpenAsync(_documento.FullUrl, BrowserLaunchMode.SystemPreferred);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error opening externally: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el documento", "OK");
        }
    }

    private async Task<bool> CheckDocumentExistsAsync(string url)
    {
        try
        {
            // Hacer un HEAD request para verificar si existe
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // Si HEAD no funciona, intentar GET con rango pequeño
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.PartialContent;
            }

            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            // Timeout
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ShowError(string message)
    {
        LoadingView.IsVisible = false;
        ImageScrollView.IsVisible = false;
        PdfViewer.IsVisible = false;
        InfoFrame.IsVisible = false;
        ErrorView.IsVisible = true;
        ErrorLabel.Text = message;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        await OpenPdfExternallyAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Limpiar archivo temporal
        try
        {
            if (!string.IsNullOrEmpty(_localFilePath) && File.Exists(_localFilePath))
            {
                File.Delete(_localFilePath);
            }
        }
        catch { }
    }
}
