using System.Runtime.InteropServices;
using System.Windows;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Reads a web link from the clipboard, so a link copied on the PC is already filled in when
/// adding a title - leaving just one button press to confirm it from the couch.
/// </summary>
public static class ClipboardLink
{
    /// <summary>The clipboard's text when it's an http(s) link, otherwise an empty string.</summary>
    public static string Read()
    {
        try
        {
            return Clipboard.ContainsText() ? Parse(Clipboard.GetText()) : string.Empty;
        }
        catch (ExternalException)
        {
            // Another app has the clipboard open.
            return string.Empty;
        }
    }

    /// <summary><paramref name="text"/>, trimmed, when it's an http(s) link, otherwise an empty string.</summary>
    public static string Parse(string text)
    {
        var trimmed = text.Trim();
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? trimmed
            : string.Empty;
    }
}