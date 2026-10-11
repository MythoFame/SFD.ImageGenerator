using Eto.Drawing;
using SkiaSharp;

namespace SFD.ImageGenerator;

/// <summary>
/// One decoded animation frame. Single (non-animated) images yield exactly
/// one frame with <see cref="Delay"/> zero.
/// </summary>
public sealed record ImageFrame(Bitmap Image, TimeSpan Delay);

/// <summary>
/// Loads images as frame lists. GIFs (sniffed by magic bytes, not extension)
/// are decoded with all animation frames composited to full RGBA canvases;
/// anything else loads as a single frame.
/// </summary>
public static class ImageLoader
{
    private static readonly byte[] Gif87A = "GIF87a"u8.ToArray();
    private static readonly byte[] Gif89A = "GIF89a"u8.ToArray();

    public static IReadOnlyList<ImageFrame> Load(string path)
    {
        if (IsGif(path))
            return LoadGif(path);
        return [new ImageFrame(new Bitmap(path), TimeSpan.Zero)];
    }

    private static bool IsGif(string path)
    {
        try
        {
            Span<byte> magic = stackalloc byte[6];
            using var stream = File.OpenRead(path);
            return stream.Read(magic) == 6 &&
                (magic.SequenceEqual(Gif87A) || magic.SequenceEqual(Gif89A));
        }
        catch
        {
            // Unreadable here; the Bitmap load below throws the real error.
            return false;
        }
    }

    private static IReadOnlyList<ImageFrame> LoadGif(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var codec = SKCodec.Create(stream)
                ?? throw new InvalidDataException("Not a decodable GIF file.");
            if (codec.FrameCount == 0)
                throw new InvalidDataException("GIF contains no frames.");

            // Straight (non-premultiplied) alpha keeps the stored RGBA bytes
            // faithful to the source, including fully transparent pixels.
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height,
                SKColorType.Rgba8888, SKAlphaType.Unpremul);

            var frames = new List<ImageFrame>(codec.FrameCount);
            for (var i = 0; i < codec.FrameCount; i++)
            {
                using var bitmap = new SKBitmap(info);
                var result = codec.GetPixels(info, bitmap.GetPixels(), new SKCodecOptions(i));
                if (result != SKCodecResult.Success)
                    throw new InvalidDataException($"Could not decode GIF frame {i}: {result}.");
                frames.Add(new ImageFrame(ToEtoBitmap(bitmap, info.Width, info.Height),
                    GetDelay(codec.FrameInfo[i].Duration)));
            }
            return frames;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Could not decode GIF: {ex.Message}", ex);
        }
    }

    private static TimeSpan GetDelay(int milliseconds)
    {
        // Zero delays would spin the preview; treat them as ~100ms like browsers do.
        return milliseconds <= 0
            ? TimeSpan.FromMilliseconds(100)
            : TimeSpan.FromMilliseconds(milliseconds);
    }

    private static Bitmap ToEtoBitmap(SKBitmap bitmap, int width, int height)
    {
        var rgba = bitmap.GetPixelSpan().ToArray();
        if (rgba.Length != width * height * 4)
            throw new InvalidDataException("Unexpected GIF frame stride.");

        var colors = new Eto.Drawing.Color[rgba.Length / 4];
        for (var i = 0; i < colors.Length; i++)
            colors[i] = new Eto.Drawing.Color(
                rgba[i * 4] / 255f, rgba[i * 4 + 1] / 255f,
                rgba[i * 4 + 2] / 255f, rgba[i * 4 + 3] / 255f);

        return new Bitmap(width, height, PixelFormat.Format32bppRgba, colors);
    }
}
