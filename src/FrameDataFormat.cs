using Eto.Drawing;

namespace SFD.ImageGenerator;

/// <summary>
/// Encoder for the frame data format v1 (as implemented by Commands++):
/// one Base64 frame per animation frame, frames joined by "_".
/// Each frame decodes to width u32 LE + height u32 LE + w*h*3 RGB bytes,
/// row-major, top-left origin. Alpha is not stored (text objects
/// don't support transparency); source alpha is ignored.
/// </summary>
public static class FrameDataFormat
{
    public static string EncodeFrame(Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var width = bitmap.Size.Width;
        var height = bitmap.Size.Height;
        if (width < 1 || height < 1)
            throw new InvalidOperationException($"Image must be at least 1x1 pixels (got {width}x{height}).");

        var blob = new byte[8 + ((long)width * height * 3)];

        WriteUInt32LE(blob, 0, unchecked((uint)width));
        WriteUInt32LE(blob, 4, unchecked((uint)height));

        var offset = 8;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                blob[offset++] = ToChannel(color.R);
                blob[offset++] = ToChannel(color.G);
                blob[offset++] = ToChannel(color.B);
            }
        }

        return Convert.ToBase64String(blob);
    }

    public static string EncodeFrames(IEnumerable<Bitmap> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        return string.Join("_", frames.Select(EncodeFrame));
    }

    private static void WriteUInt32LE(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }

    private static byte ToChannel(float value) =>
        (byte)Math.Round(Math.Clamp(value, 0f, 1f) * 255f, MidpointRounding.AwayFromZero);
}
