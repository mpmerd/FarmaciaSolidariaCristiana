using UIKit;
using CoreGraphics;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación de MacCatalyst para compresión de imágenes usando UIKit
/// </summary>
public partial class ImageCompressionService
{
    public partial async Task<byte[]> CompressImageAsync(
        Stream inputStream,
        string? contentType,
        int maxWidth = 1920,
        int maxHeight = 1080,
        int quality = 80)
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

            // Decodificar la imagen
            UIImage? originalImage = null;
            try
            {
                using var data = Foundation.NSData.FromArray(originalBytes);
                originalImage = UIImage.LoadFromData(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageCompression] Error decoding image: {ex.Message}");
                return originalBytes;
            }

            if (originalImage == null)
            {
                Console.WriteLine("[ImageCompression] Could not decode image, returning original bytes");
                return originalBytes;
            }

            try
            {
                var width = (int)originalImage.Size.Width;
                var height = (int)originalImage.Size.Height;
                Console.WriteLine($"[ImageCompression] Original dimensions: {width}x{height}");

                // Calcular nuevas dimensiones manteniendo aspect ratio
                var (newWidth, newHeight) = CalculateNewDimensions(width, height, maxWidth, maxHeight);

                UIImage? resizedImage = originalImage;

                // Redimensionar solo si es necesario
                if (newWidth < width || newHeight < height)
                {
                    Console.WriteLine($"[ImageCompression] Resizing to: {newWidth}x{newHeight}");
                    var size = new CGSize(newWidth, newHeight);
                    UIGraphics.BeginImageContextWithOptions(size, false, 1.0f);
                    originalImage.Draw(new CGRect(0, 0, newWidth, newHeight));
                    resizedImage = UIGraphics.GetImageFromCurrentImageContext();
                    UIGraphics.EndImageContext();
                }

                // Comprimir a JPEG
                var qualityFloat = quality / 100.0f;
                var jpegData = resizedImage?.AsJPEG((float)qualityFloat);
                
                if (jpegData == null)
                {
                    Console.WriteLine("[ImageCompression] Could not compress to JPEG, returning original");
                    return originalBytes;
                }

                var compressedBytes = jpegData.ToArray();
                var compressionRatio = originalSize > 0
                    ? (1 - (double)compressedBytes.Length / originalSize) * 100
                    : 0;

                Console.WriteLine($"[ImageCompression] Compressed size: {FormatSize(compressedBytes.Length)}, " +
                                $"Compression: {compressionRatio:F1}%");

                // Si la compresión no fue efectiva, devolver original
                if (compressedBytes.Length >= originalSize * 0.95)
                {
                    Console.WriteLine("[ImageCompression] Compression not effective, using original");
                    return originalBytes;
                }

                return compressedBytes;
            }
            finally
            {
                originalImage?.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageCompression] Error: {ex.Message}");
            try
            {
                if (inputStream.CanSeek)
                {
                    inputStream.Position = 0;
                }
                using var fallbackStream = new MemoryStream();
                await inputStream.CopyToAsync(fallbackStream);
                return fallbackStream.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
