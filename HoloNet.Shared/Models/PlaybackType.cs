namespace HoloNet.Shared.Models;

/// <summary>
/// Where actual playback of a <see cref="MediaItem"/> happens. Only providers that expose a
/// legitimate, authorized playback mechanism should ever report <see cref="Native"/> or
/// <see cref="Embedded"/> — everything else must fall back to <see cref="External"/> (the user
/// is sent to the provider's own app/site to watch). See HoloNet.Video's provider notes for why
/// this distinction matters: it keeps HoloNet from ever needing to touch a provider's DRM/license
/// flow for providers that don't officially support third-party playback.
/// </summary>
public enum PlaybackType
{
    /// <summary>Plays inside HoloNet's own player.</summary>
    Native,

    /// <summary>The provider's own player is embedded inside HoloNet's UI.</summary>
    Embedded,

    /// <summary>HoloNet only links out — playback happens in the provider's app/site.</summary>
    External,
}
