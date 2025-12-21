using System.IO;
using System.Windows.Media.Imaging;

namespace ClipMate.Infrastructure;

public class BitmapCodec
{
    public static byte[] EncodePngBytes(BitmapSource image)
    {
        byte[] imageBytes;

        using (var memoryStream = new MemoryStream())
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(memoryStream);
            imageBytes = memoryStream.ToArray();
        }

        return imageBytes;
    }

    public static BitmapImage DecodeBitmapImage(byte[] content, int? decodePixelHeight = null)
    {
        using var stream = new MemoryStream(content);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;

        if (decodePixelHeight.HasValue && decodePixelHeight.Value > 0)
        {
            image.DecodePixelHeight = decodePixelHeight.Value;
        }

        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        image.EndInit();
        image.Freeze();

        return image;
    }
}
