namespace FarmaciaSolidariaCristiana.Maui.Views;

public partial class PdfViewerPage : ContentPage
{
    private readonly byte[] _pdfBytes;
    private readonly string _fileName;
    private string? _localFilePath;

    public PdfViewerPage(byte[] pdfBytes, string fileName)
    {
        InitializeComponent();
        _pdfBytes = pdfBytes;
        _fileName = fileName;
        Title = fileName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPdfAsync();
    }

    private async Task LoadPdfAsync()
    {
        try
        {
            LoadingView.IsVisible = true;
            LoadingLabel.Text = "Preparando PDF...";
            ErrorView.IsVisible = false;
            PdfViewer.IsVisible = false;
            ToolbarFrame.IsVisible = false;

            if (_pdfBytes == null || _pdfBytes.Length == 0)
            {
                ShowError("El PDF está vacío");
                return;
            }

            // Guardar en cache local
            _localFilePath = Path.Combine(FileSystem.CacheDirectory, _fileName);
            await File.WriteAllBytesAsync(_localFilePath, _pdfBytes);

            LoadingLabel.Text = "Cargando visor...";

            // Convertir PDF a base64
            var base64Pdf = Convert.ToBase64String(_pdfBytes);
            
            // Crear HTML con pdf.js de Mozilla (CDN)
            // pdf.js renderiza el PDF en un canvas HTML5
            var html = GetPdfJsHtml(base64Pdf);

            PdfViewer.Source = new HtmlWebViewSource { Html = html };
            
            LoadingView.IsVisible = false;
            PdfViewer.IsVisible = true;
            ToolbarFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewer] Error: {ex.Message}");
            ShowError($"Error al cargar el PDF: {ex.Message}");
        }
    }

    private string GetPdfJsHtml(string base64Pdf)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=3.0, user-scalable=yes'>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js'></script>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{ 
            width: 100%; 
            height: 100%; 
            background-color: #525659;
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
            background: white;
            border-radius: 10px;
            display: none;
        }}
        .spinner {{
            border: 4px solid #f3f3f3;
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
</head>
<body>
    <div id='loading'>
        <div class='spinner'></div>
        <div>Renderizando PDF...</div>
    </div>
    <div id='error'></div>
    <div id='pdf-container'></div>
    
    <script>
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
        
        const pdfData = atob('{base64Pdf}');
        const pdfArray = new Uint8Array(pdfData.length);
        for (let i = 0; i < pdfData.length; i++) {{
            pdfArray[i] = pdfData.charCodeAt(i);
        }}
        
        async function renderPDF() {{
            try {{
                const pdf = await pdfjsLib.getDocument({{ data: pdfArray }}).promise;
                const container = document.getElementById('pdf-container');
                const loading = document.getElementById('loading');
                
                // Calcular escala basada en el ancho de pantalla
                const containerWidth = window.innerWidth - 20;
                
                // Usar devicePixelRatio para mejorar la calidad (mínimo 3x para alta resolución)
                const pixelRatio = Math.max(window.devicePixelRatio || 1, 3);
                
                for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {{
                    const page = await pdf.getPage(pageNum);
                    const viewport = page.getViewport({{ scale: 1 }});
                    
                    // Escalar para que quepa en la pantalla
                    const scale = containerWidth / viewport.width;
                    const scaledViewport = page.getViewport({{ scale: scale * pixelRatio }});
                    
                    const canvas = document.createElement('canvas');
                    canvas.className = 'pdf-page';
                    const context = canvas.getContext('2d');
                    
                    // Canvas interno a alta resolución
                    canvas.height = scaledViewport.height;
                    canvas.width = scaledViewport.width;
                    
                    // CSS para mostrar a tamaño normal (pero con más píxeles = más nitidez)
                    canvas.style.width = (scaledViewport.width / pixelRatio) + 'px';
                    canvas.style.height = (scaledViewport.height / pixelRatio) + 'px';
                    
                    await page.render({{
                        canvasContext: context,
                        viewport: scaledViewport
                    }}).promise;
                    
                    container.appendChild(canvas);
                }}
                
                loading.style.display = 'none';
            }} catch (error) {{
                document.getElementById('loading').style.display = 'none';
                document.getElementById('error').style.display = 'block';
                document.getElementById('error').innerHTML = 'Error al cargar el PDF:<br>' + error.message;
                console.error('Error rendering PDF:', error);
            }}
        }}
        
        renderPDF();
    </script>
</body>
</html>";
    }

    private void ShowError(string message)
    {
        LoadingView.IsVisible = false;
        PdfViewer.IsVisible = false;
        ErrorView.IsVisible = true;
        ToolbarFrame.IsVisible = false;
        ErrorLabel.Text = message;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_localFilePath) || !File.Exists(_localFilePath))
            return;

        try
        {
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = $"Compartir {_fileName}",
                File = new ShareFile(_localFilePath)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewer] Error sharing: {ex.Message}");
            await DisplayAlert("Error", "No se pudo compartir el archivo", "OK");
        }
    }

    private async void OnOpenExternalClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_localFilePath) || !File.Exists(_localFilePath))
            return;

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                Title = $"Abrir {_fileName}",
                File = new ReadOnlyFile(_localFilePath)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewer] Error opening: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el archivo", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Limpiar archivo temporal después de un delay
        if (!string.IsNullOrEmpty(_localFilePath))
        {
            Task.Run(async () =>
            {
                await Task.Delay(5000); // Esperar 5 segundos
                try
                {
                    if (File.Exists(_localFilePath))
                    {
                        File.Delete(_localFilePath);
                    }
                }
                catch { /* Ignorar errores de limpieza */ }
            });
        }
    }
}
