using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class DocumentoViewerPage : ContentPage
{
    private readonly TurnoDocumento _documento;
    private readonly HttpClient _httpClient;
    private string? _localFilePath;
    private readonly byte[]? _preloadedBytes;
    private readonly bool _hasPreloadedBytes;
    private static string? _pdfJsScript;
    private static string? _pdfWorkerScript;

    public DocumentoViewerPage(TurnoDocumento documento)
    {
        InitializeComponent();
        _documento = documento;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _hasPreloadedBytes = false;
        
        Title = documento.DocumentType;
        DocTypeLabel.Text = documento.DocumentType;
        FileNameLabel.Text = documento.FileName;
    }

    /// <summary>
    /// Constructor con bytes pre-descargados (para documentos que requieren autenticación)
    /// </summary>
    public DocumentoViewerPage(TurnoDocumento documento, byte[] fileBytes) : this(documento)
    {
        _preloadedBytes = fileBytes;
        _hasPreloadedBytes = true;
    }

    /// <summary>
    /// Constructor simplificado con bytes y nombre de archivo
    /// </summary>
    public DocumentoViewerPage(byte[] fileBytes, string fileName, string documentType = "Documento")
    {
        InitializeComponent();
        _documento = new TurnoDocumento
        {
            FileName = fileName,
            DocumentType = documentType,
            ContentType = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) 
                ? "application/pdf" 
                : "image/jpeg"
        };
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _preloadedBytes = fileBytes;
        _hasPreloadedBytes = true;
        
        Title = documentType;
        DocTypeLabel.Text = documentType;
        FileNameLabel.Text = fileName;
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

            // Determinar tipo de documento
            var isImage = _documento.ContentType?.StartsWith("image/") == true ||
                         _documento.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                         _documento.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);

            var isPdf = _documento.ContentType == "application/pdf" ||
                       _documento.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            // Si tenemos bytes pre-descargados, usarlos directamente
            if (_hasPreloadedBytes && _preloadedBytes != null && _preloadedBytes.Length > 0)
            {
                if (isImage)
                {
                    await LoadImageFromBytesAsync(_preloadedBytes);
                }
                else if (isPdf)
                {
                    await LoadPdfFromBytesAsync(_preloadedBytes);
                }
                else
                {
                    // Tipo desconocido, intentar como imagen
                    await LoadImageFromBytesAsync(_preloadedBytes);
                }
                return;
            }

            // Cargar desde URL
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

    private async Task LoadImageFromBytesAsync(byte[] imageBytes)
    {
        LoadingLabel.Text = "Cargando imagen...";
        
        try
        {
            // Crear ImageSource desde bytes
            ImageViewer.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            
            LoadingView.IsVisible = false;
            ImageScrollView.IsVisible = true;
            InfoFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error loading image from bytes: {ex.Message}");
            ShowError("Error al cargar la imagen");
        }
    }

    private async Task LoadPdfFromBytesAsync(byte[] pdfBytes)
    {
        LoadingLabel.Text = "Preparando visor PDF...";
        
        try
        {
            // Guardar PDF en cache
            var fileName = $"temp_{Guid.NewGuid()}.pdf";
            _localFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(_localFilePath, pdfBytes);

            Console.WriteLine($"[DocumentoViewer] PDF guardado: {pdfBytes.Length} bytes");

            // Cargar pdf.js desde recursos locales (solo una vez)
            if (_pdfJsScript == null)
            {
                using var pdfStream = await FileSystem.OpenAppPackageFileAsync("pdf.min.js");
                using var pdfReader = new StreamReader(pdfStream);
                _pdfJsScript = await pdfReader.ReadToEndAsync();
            }

            if (_pdfWorkerScript == null)
            {
                using var workerStream = await FileSystem.OpenAppPackageFileAsync("pdf.worker.min.js");
                using var workerReader = new StreamReader(workerStream);
                _pdfWorkerScript = await workerReader.ReadToEndAsync();
            }

            LoadingLabel.Text = "Renderizando PDF...";

            // Usar pdf.js para renderizar el PDF en WebView
            var base64Pdf = Convert.ToBase64String(pdfBytes);
            var html = GetPdfJsHtml(base64Pdf, _pdfJsScript, _pdfWorkerScript);
            
            PdfViewer.Source = new HtmlWebViewSource { Html = html };
            
            LoadingView.IsVisible = false;
            PdfViewer.IsVisible = true;
            InfoFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error loading PDF from bytes: {ex.Message}");
            ShowError("Error al cargar el PDF");
        }
    }

    private string GetPdfJsHtml(string base64Pdf, string pdfJsScript, string pdfWorkerScript)
    {
        // Convertir el worker a base64 para crear un Blob URL
        var workerBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pdfWorkerScript));
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=3.0, user-scalable=yes'>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ 
            width: 100%; 
            height: 100%; 
            background-color: #1a1a1a;
            overflow-x: hidden;
            overflow-y: auto;
        }}
        #pdf-container {{
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 10px;
            gap: 10px;
        }}
        .pdf-page {{
            background: white;
            box-shadow: 0 2px 10px rgba(0,0,0,0.3);
            margin-bottom: 10px;
        }}
        #loading {{
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white;
            font-family: sans-serif;
            font-size: 18px;
            text-align: center;
        }}
        #error {{
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: #ff6b6b;
            font-family: sans-serif;
            font-size: 16px;
            text-align: center;
            padding: 20px;
            background: #333;
            border-radius: 10px;
            max-width: 90%;
            display: none;
        }}
        .spinner {{
            border: 4px solid #333;
            border-top: 4px solid #4CAF50;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto 15px;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
    </style>
    <!-- pdf.js embebido (sin necesidad de internet) -->
    <script>{pdfJsScript}</script>
</head>
<body>
    <div id='loading'>
        <div class='spinner'></div>
        <div id='loadingText'>Renderizando PDF...</div>
    </div>
    <div id='error'></div>
    <div id='pdf-container'></div>
    
    <script>
        function showError(msg) {{
            document.getElementById('loading').style.display = 'none';
            var errorDiv = document.getElementById('error');
            errorDiv.style.display = 'block';
            errorDiv.innerHTML = 'Error:<br>' + msg;
        }}
        
        // Configurar worker desde base64 (sin necesidad de internet)
        try {{
            var workerBlob = new Blob([atob('{workerBase64}')], {{ type: 'application/javascript' }});
            pdfjsLib.GlobalWorkerOptions.workerSrc = URL.createObjectURL(workerBlob);
        }} catch (e) {{
            pdfjsLib.GlobalWorkerOptions.workerSrc = '';
        }}
        
        // Iniciar renderizado
        try {{
            var pdfData = atob('{base64Pdf}');
            var pdfArray = new Uint8Array(pdfData.length);
            for (var i = 0; i < pdfData.length; i++) {{
                pdfArray[i] = pdfData.charCodeAt(i);
            }}
            
            renderPDF(pdfArray);
        }} catch (e) {{
            showError('Error al decodificar el PDF: ' + e.message);
        }}
        
        async function renderPDF(pdfArray) {{
            try {{
                var pdf = await pdfjsLib.getDocument({{ data: pdfArray }}).promise;
                var container = document.getElementById('pdf-container');
                var containerWidth = window.innerWidth - 20;
                var pixelRatio = Math.max(window.devicePixelRatio || 1, 3);
                
                for (var pageNum = 1; pageNum <= pdf.numPages; pageNum++) {{
                    document.getElementById('loadingText').textContent = 'Página ' + pageNum + '/' + pdf.numPages;
                    
                    var page = await pdf.getPage(pageNum);
                    var viewport = page.getViewport({{ scale: 1 }});
                    var scale = containerWidth / viewport.width;
                    var scaledViewport = page.getViewport({{ scale: scale * pixelRatio }});
                    
                    var canvas = document.createElement('canvas');
                    canvas.className = 'pdf-page';
                    var context = canvas.getContext('2d');
                    canvas.height = scaledViewport.height;
                    canvas.width = scaledViewport.width;
                    canvas.style.width = (scaledViewport.width / pixelRatio) + 'px';
                    canvas.style.height = (scaledViewport.height / pixelRatio) + 'px';
                    
                    await page.render({{
                        canvasContext: context,
                        viewport: scaledViewport
                    }}).promise;
                    
                    container.appendChild(canvas);
                }}
                
                document.getElementById('loading').style.display = 'none';
            }} catch (error) {{
                showError('Error al renderizar: ' + error.message);
            }}
        }}
    </script>
</body>
</html>";
    }

    private async Task<bool> TryOpenPdfWithSystemViewerAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_localFilePath) && File.Exists(_localFilePath))
            {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_localFilePath)
                });
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error opening PDF with system viewer: {ex.Message}");
            return false;
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
            // Descargar el PDF
            var pdfBytes = await _httpClient.GetByteArrayAsync(url);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                ShowError("No se pudo descargar el PDF");
                return;
            }

            // Usar el mismo método que LoadPdfFromBytesAsync para renderizar con pdf.js embebido
            await LoadPdfFromBytesAsync(pdfBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DocumentoViewer] Error loading PDF: {ex.Message}");
            ShowError($"Error al cargar el PDF: {ex.Message}");
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
