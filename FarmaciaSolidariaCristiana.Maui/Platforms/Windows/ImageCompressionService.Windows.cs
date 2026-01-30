using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación de Windows para compresión de imágenes usando Windows.Graphics.Imaging
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

            // Convertir a IRandomAccessStream
            using var randomAccessStream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(originalBytes);
                await writer.StoreAsync();
            }
            randomAccessStream.Seek(0);

            // Decodificar la imagen
            BitmapDecoder decoder;
            try
            {
                decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageCompression] Error decoding image: {ex.Message}");
                return originalBytes;
            }

            var width = (int)decoder.PixelWidth;
            var height = (int)decoder.PixelHeight;
            Console.WriteLine($"[ImageCompression] Original dimensions: {width}x{height}");

            // Calcular nuevas dimensiones manteniendo aspect ratio
            var (newWidth, newHeight) = CalculateNewDimensions(width, height, maxWidth, maxHeight);

            // Crear el encoder de salida
            using var outputStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);

            // Obtener los píxeles
            var pixelData = await decoder.GetPixelDataAsync();
            var pixels = pixelData.DetachPixelData();

            // Configurar el encoder
            encoder.SetPixelData(
                decoder.BitmapPixelFormat,
                decoder.BitmapAlphaMode,
                decoder.PixelWidth,
                decoder.PixelHeight,
                decoder.DpiX,
                decoder.DpiY,
                pixels);

            // Redimensionar si es necesario
            if (newWidth < width || newHeight < height)
            {
                Console.WriteLine($"[ImageCompression] Resizing to: {newWidth}x{newHeight}");
                encoder.BitmapTransform.ScaledWidth = (uint)newWidth;
                encoder.BitmapTransform.ScaledHeight = (uint)newHeight;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            }

            // Configurar calidad JPEG
            var qualityProperty = new BitmapPropertySet();
            qualityProperty.Add("ImageQuality", new BitmapTypedValue((float)(quality / 100.0), Windows.Foundation.PropertyType.Single));
            await encoder.BitmapProperties.SetPropertiesAsync(qualityProperty);

            // Guardar
            await encoder.FlushAsync();

            // Leer los bytes comprimidos
            var reader = new DataReader(outputStream.GetInputStreamAt(0));
            var compressedBytes = new byte[outputStream.Size];
            await reader.LoadAsync((uint)outputStream.Size);
            reader.ReadBytes(compressedBytes);

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
