using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace NoitaMap.Graphics;

public static class ImageUtility
{
    public static Image<Rgba32> LoadImage(string path)
    {
        Configuration configuration = Configuration.Default;

        configuration.PreferContiguousImageBuffers = true;

        return Image.Load<Rgba32>(new DecoderOptions() { Configuration = configuration }, path);
    }
}