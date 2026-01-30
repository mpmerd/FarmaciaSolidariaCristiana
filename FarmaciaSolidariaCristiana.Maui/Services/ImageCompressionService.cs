namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Servicio para comprimir imágenes del lado del cliente antes de enviar al servidor.
/// Esto es especialmente importante para Cuba donde el ancho de banda es limitado.
/// </summary>
public interface IImageCompressionService
{
    /// <summary>
    /// Comprime una imagen manteniendo una calidad aceptable
    /// </summary>
    /// <param name="inputStream">Stream de la imagen original</param>
    /// <param name="contentType">Tipo MIME de la imagen</param>
    /// <param name="maxWidth">Ancho máximo (default: 1920)</param>
    /// <param name="maxHeight">Alto máximo (default: 1080)</param>
    /// <param name="quality">Calidad JPEG 0-100 (default: 80)</param>
    /// <returns>Bytes de la imagen comprimida</returns>
    Task<byte[]> CompressImageAsync(Stream inputStream, string? contentType, int maxWidth = 1920, int maxHeight = 1080, int quality = 80);

    /// <summary>
    /// Verifica si un tipo de contenido es una imagen soportada
    /// </summary>
    bool IsImage(string? contentType);
}

/// <summary>
/// Implementación de compresión de imágenes usando APIs nativas de cada plataforma
/// </summary>
public partial class ImageCompressionService : IImageCompressionService
{
    private readonly string[] _supportedImageTypes = new[]
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp",
        "image/heic",
        "image/heif"
    };

    public bool IsImage(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return _supportedImageTypes.Any(type => 
            contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calcula nuevas dimensiones manteniendo el aspect ratio
    /// </summary>
    protected static (int width, int height) CalculateNewDimensions(
        int originalWidth, 
        int originalHeight, 
        int maxWidth, 
        int maxHeight)
    {
        // Si la imagen ya es pequeña, mantener dimensiones originales
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        // Calcular aspect ratio
        var aspectRatio = (double)originalWidth / originalHeight;

        int newWidth, newHeight;

        // Determinar qué dimensión restringir
        if (originalWidth > originalHeight)
        {
            // Orientación horizontal
            newWidth = Math.Min(originalWidth, maxWidth);
            newHeight = (int)(newWidth / aspectRatio);

            // Verificar que altura no exceda máximo
            if (newHeight > maxHeight)
            {
                newHeight = maxHeight;
                newWidth = (int)(newHeight * aspectRatio);
            }
        }
        else
        {
            // Orientación vertical o cuadrada
            newHeight = Math.Min(originalHeight, maxHeight);
            newWidth = (int)(newHeight * aspectRatio);

            // Verificar que ancho no exceda máximo
            if (newWidth > maxWidth)
            {
                newWidth = maxWidth;
                newHeight = (int)(newWidth / aspectRatio);
            }
        }

        return (Math.Max(1, newWidth), Math.Max(1, newHeight));
    }

    protected static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
    
    // La implementación de CompressImageAsync es partial y está en archivos específicos de plataforma
    public partial Task<byte[]> CompressImageAsync(Stream inputStream, string? contentType, int maxWidth = 1920, int maxHeight = 1080, int quality = 80);
}

/// <summary>
/// Configuración de compresión de imágenes
/// </summary>
public class ImageCompressionOptions
{
    /// <summary>
    /// Ancho máximo de la imagen (default: 1920)
    /// </summary>
    public int MaxWidth { get; set; } = 1920;

    /// <summary>
    /// Alto máximo de la imagen (default: 1080)
    /// </summary>
    public int MaxHeight { get; set; } = 1080;

    /// <summary>
    /// Calidad JPEG 1-100 (default: 80)
    /// </summary>
    public int Quality { get; set; } = 80;

    /// <summary>
    /// Tamaño máximo de archivo en MB (default: 5)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 5;

    /// <summary>
    /// Tamaño máximo de PDF en MB (default: 3)
    /// </summary>
    public int MaxPdfSizeMB { get; set; } = 3;
}
