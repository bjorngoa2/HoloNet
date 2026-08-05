namespace HoloNet.Shared.Helpers;

/// <summary>
/// Represents a validated absolute path to an existing directory.
/// Use this in options classes to fail fast on misconfiguration rather than
/// at the first request when <see cref="Directory.GetFiles"/> throws.
/// </summary>
public sealed class MediaDirectory
{
    /// <summary>The absolute path to the directory.</summary>
    public string Path { get; }

    private MediaDirectory(string path) => Path = path;

    /// <summary>
    /// Creates a <see cref="MediaDirectory"/> from the given path.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    public static MediaDirectory From(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Media directory path must not be empty.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Media directory not found: '{path}'.");

        return new MediaDirectory(path);
    }

    /// <summary>
    /// Checks whether <paramref name="candidateFullPath"/> resolves to a location inside this directory.
    /// Guards against path traversal when a caller-supplied (decoded) file path is used to access disk.
    /// </summary>
    public bool Contains(string? candidateFullPath)
    {
        if (string.IsNullOrWhiteSpace(candidateFullPath))
            return false;

        try
        {
            var root = System.IO.Path.GetFullPath(Path) + System.IO.Path.DirectorySeparatorChar;
            var candidate = System.IO.Path.GetFullPath(candidateFullPath);
            return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Path;
}
