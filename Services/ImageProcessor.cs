using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ParrotsAPI2.Services
{
    public static class ImageProcessor
    {
        private const int MaxFullSize = 1920;
        private const int MaxThumbSize = 400;
        private const int WebpQuality = 85;

        /// <summary>
        /// Processes an image stream into two WebP versions: full (max 1920px) and thumbnail (max 400px).
        /// </summary>
        public static async Task<(MemoryStream Full, MemoryStream Thumbnail)> ProcessAsync(Stream inputStream)
        {
            using var image = await Image.LoadAsync(inputStream);

            if (image.Width > MaxFullSize || image.Height > MaxFullSize)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxFullSize, MaxFullSize),
                    Mode = ResizeMode.Max
                }));

            using var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxThumbSize, MaxThumbSize),
                Mode = ResizeMode.Max
            }));

            var encoder = new WebpEncoder { Quality = WebpQuality };

            var fullStream = new MemoryStream();
            await image.SaveAsWebpAsync(fullStream, encoder);
            fullStream.Position = 0;

            var thumbStream = new MemoryStream();
            await thumbnail.SaveAsWebpAsync(thumbStream, encoder);
            thumbStream.Position = 0;

            return (fullStream, thumbStream);
        }
    }
}
