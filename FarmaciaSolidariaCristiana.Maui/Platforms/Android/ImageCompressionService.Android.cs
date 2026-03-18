using Android.Graphics;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación de Android para compresión de imágenes usando Android.Graphics.Bitmap
/// </summary>
public partial class ImageCompressionService
{
    public partial async Task<byte[]> CompressImageAsync(
        Stream inputStream,
        string? contentType,
        int maxWidth,
        int maxHeight,
        int quality)
    {
        try
        {
            // Asegurarse de que el stream está al inicio
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            // Leer el stream completo a memoria
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            var originalBytes = memoryStream.ToArray();
            var originalSize = originalBytes.Length;

            Console.WriteLine($"[ImageCompression] Original size: {FormatSize(originalSize)}");

            // Decodificar el bitmap
            Bitmap? originalBitmap = null;
            try
            {
                originalBitmap = await BitmapFactory.DecodeByteArrayAsync(originalBytes, 0, originalBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageCompression] Error decoding image: {ex.Message}");
                return originalBytes;
            }

            if (originalBitmap == null)
            {
                Console.WriteLine("[ImageCompression] Could not decode image, returning original bytes");
                return originalBytes;
            }

            try
            {
                Console.WriteLine($"[ImageCompression] Original dimensions: {originalBitmap.Width}x{originalBitmap.Height}");

                // Calcular nuevas dimensiones manteniendo aspect ratio
                var (newWidth, newHeight) = CalculateNewDimensions(
                    originalBitmap.Width,
                    originalBitmap.Height,
                    maxWidth,
                    maxHeight);

                Bitmap? resizedBitmap = originalBitmap;

                // Redimensionar solo si es necesario
                if (newWidth < originalBitmap.Width || newHeight < originalBitmap.Height)
                {
                    Console.WriteLine($"[ImageCompression] Resizing to: {newWidth}x{newHeight}");
                    resizedBitmap = Bitmap.CreateScaledBitmap(originalBitmap, newWidth, newHeight, true);
                }

                // Comprimir a JPEG
                using var outputStream = new MemoryStream();
                
                // Usar Compress para guardar como JPEG con la calidad especificada
                await Task.Run(() =>
                {
                    resizedBitmap!.Compress(Bitmap.CompressFormat.Jpeg!, quality, outputStream);
                });

                var compressedBytes = outputStream.ToArray();
                var compressionRatio = originalSize > 0
                    ? (1 - (double)compressedBytes.Length / originalSize) * 100
                    : 0;

                Console.WriteLine($"[ImageCompression] Compressed size: {FormatSize(compressedBytes.Length)}, " +
                                $"Compression: {compressionRatio:F1}%");

                // Liberar el bitmap redimensionado si es diferente del original
                if (resizedBitmap != originalBitmap)
                {
                    resizedBitmap?.Recycle();
                    resizedBitmap?.Dispose();
                }

                // Si la compresión no fue efectiva (menos del 5%), devolver original
                if (compressedBytes.Length >= originalSize * 0.95)
                {
                    Console.WriteLine("[ImageCompression] Compression not effective, using original");
                    return originalBytes;
                }

                return compressedBytes;
            }
            finally
            {
                // Siempre liberar el bitmap original
                originalBitmap.Recycle();
                originalBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageCompression] Error: {ex.Message}");
            // En caso de error, intentar devolver los bytes originales
            try
            {
                if (inputStream.CanSeek)
                {
                    inputStream.Position = 0;
                    using var fallbackStream = new MemoryStream();
                    await inputStream.CopyToAsync(fallbackStream);
                    var fallbackBytes = fallbackStream.ToArray();
                    if (fallbackBytes.Length > 0)
                    {
                        Console.WriteLine($"[ImageCompression] Returning original bytes from stream: {fallbackBytes.Length}");
                        return fallbackBytes;
                    }
                }
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"[ImageCompression] Fallback also failed: {innerEx.Message}");
            }
            
            // Si todo falla, lanzar excepción para que el llamador maneje el error
            throw new InvalidOperationException($"No se pudo procesar la imagen: {ex.Message}", ex);
        }
    }
}
