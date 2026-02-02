using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace FarmaciaSolidariaCristiana.Maui.Platforms.Android;

/// <summary>
/// Handler personalizado para WebView en Android que habilita JavaScript,
/// contenido mixto (HTTPS cargando recursos HTTP) y otras configuraciones
/// necesarias para el visor de PDF con pdf.js.
/// </summary>
public static class CustomWebViewHandler
{
    public static void Configure()
    {
        WebViewHandler.Mapper.AppendToMapping("CustomWebView", (handler, view) =>
        {
            var webView = handler.PlatformView;
            if (webView == null) return;

            var settings = webView.Settings;
            if (settings == null) return;

            // Habilitar JavaScript (requerido para pdf.js)
            settings.JavaScriptEnabled = true;

            // Habilitar DOM Storage (usado por algunas bibliotecas JS)
            settings.DomStorageEnabled = true;

            // Permitir contenido mixto (HTTP desde HTTPS)
            // Esto es necesario cuando se carga pdf.js desde CDN
            settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

            // Permitir acceso a archivos locales
            settings.AllowFileAccess = true;
            settings.AllowFileAccessFromFileURLs = true;
            settings.AllowUniversalAccessFromFileURLs = true;

            // Habilitar zoom (útil para PDFs)
            settings.BuiltInZoomControls = true;
            settings.DisplayZoomControls = false; // Ocultar controles de zoom (usamos pinch-to-zoom)
            settings.SetSupportZoom(true);

            // Configurar User-Agent para evitar problemas con CDNs
            settings.UserAgentString = settings.UserAgentString + " PdfViewer/1.0";

            // Configurar cache
            settings.CacheMode = CacheModes.Default;

            System.Diagnostics.Debug.WriteLine("[CustomWebViewHandler] WebView configurado correctamente");
        });
    }
}
