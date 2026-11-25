namespace HoloNet.TvLauncher.Views;

/// <summary>
/// What the full-screen launch/status splash (see <see cref="SplashOverlay"/>) shows: the
/// tile being opened (so the transition visually continues from the focused tile, like a
/// console app launch), a title and message, whether work is in progress, and which buttons
/// are available.
/// </summary>
/// <param name="Card">The tile being opened, or <c>null</c> for app-level dialogs (e.g. updates).</param>
/// <param name="IsWorking">Shows an indeterminate progress indicator.</param>
/// <param name="Input">A text field to show below the message, or <c>null</c> for none.</param>
/// <param name="IsWarning">Shows the message in the warning colour, e.g. why an input was rejected.</param>
public sealed record SplashContent(
    string Title,
    string Message,
    IPickerCard? Card = null,
    bool IsWorking = false,
    IReadOnlyList<ButtonHint>? Hints = null,
    SplashInput? Input = null,
    bool IsWarning = false)
{
    public string AccentColor => Card?.AccentColor ?? PickerCardBase.DefaultAccentColor;
}

/// <summary>A text field on the splash (see <see cref="SplashOverlay.InputText"/>).</summary>
/// <param name="Text">The text it starts with.</param>
/// <param name="Placeholder">Shown in the empty field, as an example of what to enter.</param>
public sealed record SplashInput(string Text, string Placeholder);