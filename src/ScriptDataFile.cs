using System.Text;

namespace SFD.ImageGenerator;

/// <summary>
/// Result of saving one image entry to imgdump.txt.
/// </summary>
public sealed record SaveResult(
    string Path,
    bool Created,
    int Index,
    int TotalImages,
    bool UsedFallback);

/// <summary>
/// Writes encoded image payloads as imgdump.txt for the game's
/// Shared script storage. Primary location is the game's Shared folder;
/// falls back to the application directory when it doesn't exist.
/// File content (UTF-8, no BOM, no trailing newline):
///   v.1.0.0.0|UTF8| foo
///   string|Image.0|&lt;base64 frames&gt;
///   string|Image.1|&lt;base64 frames&gt;
///   ...
/// Each OK press appends the current image as the next free index.
/// </summary>
public static class ScriptDataFile
{
    public const string FileName = "imgdump.txt";

    private const string HeaderLine = "v.1.0.0.0|UTF8| foo";
    private const string EntryPrefix = "string|Image.";

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

    public static string BuildContent(IReadOnlyList<string> payloads)
    {
        var lines = new string[payloads.Count + 1];
        lines[0] = HeaderLine;
        for (var i = 0; i < payloads.Count; i++)
            lines[i + 1] = EntryPrefix + i + "|" + payloads[i];
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Appends one image payload as the next free index and returns what happened.
    /// Creates the file (with <paramref name="framePayload"/> as Image.0) when missing.
    /// Throws <see cref="InvalidDataException"/> when an existing file doesn't match
    /// the expected format; the existing file is left untouched in that case.
    /// </summary>
    public static SaveResult Append(string framePayload)
    {
        if (string.IsNullOrEmpty(framePayload))
            throw new ArgumentException("Image payload must not be empty.", nameof(framePayload));

        var shared = SharedDirectory;
        var usedFallback = shared is null;
        var path = Path.Combine(shared ?? AppContext.BaseDirectory, FileName);

        List<string> payloads;
        bool created;
        if (!File.Exists(path))
        {
            payloads = [];
            created = true;
        }
        else
        {
            payloads = ParsePayloads(File.ReadAllText(path));
            created = false;
        }

        payloads.Add(framePayload);
        File.WriteAllText(path, BuildContent(payloads), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new SaveResult(path, created, payloads.Count - 1, payloads.Count, usedFallback);
    }

    /// <summary>
    /// Reads the stored payloads in index order. Only indexed entries
    /// ("string|Image.N|...") are accepted.
    /// </summary>
    internal static List<string> ParsePayloads(string content)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !string.Equals(lines[0], HeaderLine, StringComparison.Ordinal))
            throw new InvalidDataException("Existing imgdump.txt has an unexpected header.");

        // Keep entries sorted by index so a hand-edited file still appends sanely.
        var indexed = new SortedDictionary<int, string>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.StartsWith(EntryPrefix, StringComparison.Ordinal))
                throw new InvalidDataException($"Existing imgdump.txt has a malformed entry on line {i + 1}.");

            var rest = line[EntryPrefix.Length..];
            var sep = rest.IndexOf('|');
            if (sep < 1 || !int.TryParse(rest[..sep], out var index) || index < 0)
                throw new InvalidDataException($"Existing imgdump.txt has a malformed entry on line {i + 1}.");
            var payload = rest[(sep + 1)..];
            if (payload.Length == 0)
                throw new InvalidDataException($"Existing imgdump.txt has an empty entry on line {i + 1}.");
            indexed[index] = payload;
        }

        return [.. indexed.Values];
    }
}
