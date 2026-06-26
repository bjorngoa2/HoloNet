namespace HoloNet.Shared.Helpers;

/// <summary>
/// Represents a validated absolute HTTP/HTTPS base URL for a service.
/// Ensures the URL is well-formed, uses http or https, and has no trailing slash,
/// so that resource URLs constructed from it are always valid.
/// </summary>
public sealed class ServiceBaseUrl
{
    /// <summary>The base URL string with no trailing slash.</summary>
    public string Value { get; }

    private ServiceBaseUrl(string value) => Value = value;

    /// <summary>
    /// Creates a <see cref="ServiceBaseUrl"/> from the given string.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="url"/> is null, empty, or not an http/https URI.
    /// </exception>
    /// <exception cref="UriFormatException">
    /// Thrown if <paramref name="url"/> is not a valid absolute URI.
    /// </exception>
    public static ServiceBaseUrl From(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Service base URL must not be empty.", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new UriFormatException($"Service base URL is not a valid absolute URI: '{url}'.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException(
                $"Service base URL must use http or https. Got: '{uri.Scheme}'.", nameof(url));

        return new ServiceBaseUrl(url.TrimEnd('/'));
    }

    public override string ToString() => Value;
}
