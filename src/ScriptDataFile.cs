using System.Text;

namespace SFD.ImageGenerator;

/// <summary>
/// Writes the encoded image payload as imgdump.txt for the game's
/// Shared script storage. Primary location is the game's Shared folder;
/// falls back to the application directory when it doesn't exist.
/// File content (two lines, no BOM, no trailing newline):
///   v.1.0.0.0|UTF8| foo
///   string|Image|&lt;base64 frame&gt;
/// </summary>
public static class ScriptDataFile
{
    public const string FileName = "imgdump.txt";

    private const string HeaderLine = "v.1.0.0.0|UTF8| foo";
    private const string EntryPrefix = "string|Image|";

    /// <summary>
    /// The game's Shared script-data directory, or null when it doesn't exist.
    /// </summary>
    public static string? SharedDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Superfighters Deluxe", "Cache", "ScriptData", "Shared");
            return Directory.Exists(dir) ? dir : null;
        }
    }

    public static string BuildContent(string framePayload) =>
        HeaderLine + Environment.NewLine + EntryPrefix + framePayload;

    /// <summary>
    /// Writes imgdump.txt and returns the full path written.
    /// <paramref name="usedFallback"/> is true when the Shared folder was
    /// missing and the application directory was used instead.
    /// </summary>
    public static string Write(string framePayload, out bool usedFallback)
    {
        var shared = SharedDirectory;
        usedFallback = shared is null;
        var path = Path.Combine(shared ?? AppContext.BaseDirectory, FileName);
        File.WriteAllText(path, BuildContent(framePayload), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
