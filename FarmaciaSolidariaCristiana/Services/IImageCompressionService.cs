namespace FarmaciaSolidariaCristiana.Services
{
    public interface IImageCompressionService
    {
        /// <summary>
        /// Compresses an image stream and returns the compressed version
        /// </summary>
        /// <param name="inputStream">Original image stream</param>
        /// <param name="contentType">MIME type of the image</param>
        /// <param name="maxWidth">Maximum width in pixels (default: 1920)</param>
        /// <param name="maxHeight">Maximum height in pixels (default: 1080)</param>
        /// <param name="quality">JPEG quality (1-100, default: 85)</param>
        /// <returns>Compressed image stream</returns>
        Task<Stream> CompressImageAsync(Stream inputStream, string contentType, int maxWidth = 1920, int maxHeight = 1080, int quality = 85);

        /// <summary>
        /// Checks if a file is an image based on its content type
        /// </summary>
        bool IsImage(string contentType);
    }
}
