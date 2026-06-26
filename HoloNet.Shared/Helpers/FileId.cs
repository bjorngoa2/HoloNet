using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace HoloNet.Shared.Helpers;

/// <summary>
/// Helpers for encoding and decoding file-path-based IDs.
/// IDs are the absolute file path Base64Url-encoded using <see cref="WebEncoders"/>.
/// They are not stable across machines or if files move — never cache or persist them.
/// </summary>
public static class FileId
{
    /// <summary>
    /// Encodes an absolute file path into a URL-safe Base64 ID.
    /// </summary>
    public static string Encode(string absolutePath)
        => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(absolutePath));

    /// <summary>
    /// Decodes a Base64Url ID back to an absolute file path.
    /// Returns <c>null</c> if the input is malformed rather than throwing.
    /// </summary>
    public static string? TryDecode(string id)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(id));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
