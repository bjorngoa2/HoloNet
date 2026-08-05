using HoloNet.Shared.Helpers;

namespace HoloNet.Games.Configuration;

public class GameServiceOptions
{
    public string GamePath { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// UNC root of an SMB/NFS share that mirrors <see cref="GamePath"/>, e.g. <c>\\holonet-server\games</c>.
    /// When set, <see cref="GameDto.NetworkPath"/> is populated so emulators (e.g. PCSX2) can open game
    /// files directly over the network share instead of downloading them through the API — this avoids
    /// slow, full-file HTTP transfers on every launch.
    /// </summary>
    public string? NetworkShareRoot { get; set; }

    public MediaDirectory GetGameDirectory() => MediaDirectory.From(GamePath);
    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);

    /// <summary>
    /// Maps an absolute game file path under <see cref="GamePath"/> to a UNC path under
    /// <see cref="NetworkShareRoot"/>. Returns <c>null</c> if no share root is configured.
    /// </summary>
    public string? GetNetworkPath(string absoluteFilePath)
    {
        if (string.IsNullOrWhiteSpace(NetworkShareRoot))
            return null;

        var relativePath = Path.GetRelativePath(GetGameDirectory().Path, absoluteFilePath);
        var uncRelativePath = relativePath.Replace(Path.DirectorySeparatorChar, '\\')
            .Replace(Path.AltDirectorySeparatorChar, '\\');

        return $"{NetworkShareRoot.TrimEnd('\\')}\\{uncRelativePath}";
    }
}
