using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace FarmaciaSolidariaCristiana.Services
{
    public class ImageCompressionService : IImageCompressionService
    {
        private readonly ILogger<ImageCompressionService> _logger;

        public ImageCompressionService(ILogger<ImageCompressionService> logger)
        {
            _logger = logger;
        }

        public async Task<Stream> CompressImageAsync(
            Stream inputStream, 
            string contentType, 
            int maxWidth = 1920, 
            int maxHeight = 1080, 
            int quality = 85)
        {
            try
            {
                // Load the image
                using var image = await Image.LoadAsync(inputStream);
                
                var originalSize = inputStream.Length;
                _logger.LogInformation("Compressing image: {Width}x{Height}, Original size: {Size} bytes", 
                    image.Width, image.Height, originalSize);

                // Calculate new dimensions while maintaining aspect ratio
                var (newWidth, newHeight) = CalculateNewDimensions(image.Width, image.Height, maxWidth, maxHeight);

                // Resize if necessary
                if (newWidth < image.Width || newHeight < image.Height)
                {
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    _logger.LogInformation("Image resized to: {Width}x{Height}", newWidth, newHeight);
                }

                // Save to memory stream with compression
                var outputStream = new MemoryStream();
                
                if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
                {
                    // PNG compression
                    var encoder = new PngEncoder
                    {
                        CompressionLevel = PngCompressionLevel.BestCompression
                    };
                    await image.SaveAsync(outputStream, encoder);
                }
                else
                {
                    // JPEG compression (default for all other image types)
                    var encoder = new JpegEncoder
                    {
                        Quality = quality
                    };
                    await image.SaveAsync(outputStream, encoder);
                }

                outputStream.Position = 0;
                
                var compressionRatio = originalSize > 0 ? (1 - (double)outputStream.Length / originalSize) * 100 : 0;
                _logger.LogInformation("Compression complete. New size: {Size} bytes, Compression: {Ratio:F2}%", 
                    outputStream.Length, compressionRatio);

                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing image");
                // Return original stream on error
                inputStream.Position = 0;
                return inputStream;
            }
        }

        public bool IsImage(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            var imageTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/bmp",
                "image/webp",
                "image/tiff",
                "image/heic",
                "image/heif"
            };

            return imageTypes.Any(type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
        }

        private (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // If image is already smaller, keep original dimensions
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            {
                return (originalWidth, originalHeight);
            }

            // Calculate aspect ratio
            var aspectRatio = (double)originalWidth / originalHeight;

            int newWidth, newHeight;

            // Determine which dimension to constrain
            if (originalWidth > originalHeight)
            {
                // Landscape orientation
                newWidth = Math.Min(originalWidth, maxWidth);
                newHeight = (int)(newWidth / aspectRatio);

                // Check if height still exceeds max
                if (newHeight > maxHeight)
                {
                    newHeight = maxHeight;
                    newWidth = (int)(newHeight * aspectRatio);
                }
            }
            else
            {
                // Portrait orientation
                newHeight = Math.Min(originalHeight, maxHeight);
                newWidth = (int)(newHeight * aspectRatio);

                // Check if width still exceeds max
                if (newWidth > maxWidth)
                {
                    newWidth = maxWidth;
                    newHeight = (int)(newWidth / aspectRatio);
                }
            }

            return (newWidth, newHeight);
        }
    }
}
