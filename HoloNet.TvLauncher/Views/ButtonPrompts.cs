using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>The controller buttons the UI prompts for, independent of controller family.</summary>
public enum PromptButton
{
    Confirm,
    Cancel,
    Secondary,
    Tertiary,

    /// <summary>Options (PlayStation) / Start (Xbox).</summary>
    Menu,

    /// <summary>Share (PlayStation) / Back (Xbox).</summary>
    View,

    /// <summary>D-pad left and right, e.g. to skip back and forward.</summary>
    LeftRight,

    /// <summary>The whole D-pad, e.g. to move a tile in any direction.</summary>
    DPad
}

/// <summary>
/// One on-screen controller button "chip". Face buttons render as a round chip with the
/// family's own symbol and colour (✕○△□ / A B Y X), system buttons as a labelled pill - the
/// convention consoles and Steam Big Picture use so prompts read at a glance from the couch.
/// </summary>
public sealed record ButtonPrompt(string Symbol, string Color, bool IsRound);

/// <summary>A hint like "[A] Play" or "Hold [Share] [Options] Exit app".</summary>
public sealed record ButtonHint(IReadOnlyList<ButtonPrompt> Buttons, string Label, bool IsHold = false);

/// <summary>
/// Maps <see cref="PromptButton"/>s to the glyphs of the controller currently in use (see
/// <see cref="IGamepadService.CurrentControllerKind"/>), so prompts always match the pad in
/// the player's hands.
/// </summary>
public static class ButtonPrompts
{
    private const string SystemButtonColor = "#E6E6EB";

    public static ButtonPrompt For(GamepadKind kind, PromptButton button) => (kind, button) switch
    {
        (_, PromptButton.LeftRight) => new("◀ ▶", SystemButtonColor, IsRound: false),
        (_, PromptButton.DPad) => new("◀ ▲ ▼ ▶", SystemButtonColor, IsRound: false),
        (GamepadKind.Xbox, PromptButton.Confirm) => new("A", "#6CC24A", IsRound: true),
        (GamepadKind.Xbox, PromptButton.Cancel) => new("B", "#F0484F", IsRound: true),
        (GamepadKind.Xbox, PromptButton.Secondary) => new("Y", "#F5C518", IsRound: true),
        (GamepadKind.Xbox, PromptButton.Tertiary) => new("X", "#3FA2F7", IsRound: true),
        (GamepadKind.Xbox, PromptButton.Menu) => new("Start", SystemButtonColor, IsRound: false),
        (GamepadKind.Xbox, PromptButton.View) => new("Back", SystemButtonColor, IsRound: false),
        (_, PromptButton.Confirm) => new("✕", "#8AB4FF", IsRound: true),
        (_, PromptButton.Cancel) => new("○", "#FF7A85", IsRound: true),
        (_, PromptButton.Secondary) => new("△", "#4FD6B0", IsRound: true),
        (_, PromptButton.Tertiary) => new("◻", "#F08BD8", IsRound: true),
        (_, PromptButton.Menu) => new("Options", SystemButtonColor, IsRound: false),
        _ => new("Share", SystemButtonColor, IsRound: false)
    };

    public static ButtonHint Hint(GamepadKind kind, PromptButton button, string label) =>
        new([For(kind, button)], label);

    /// <summary>The hold-to-quit combo that returns from any game or app (see <see cref="GamepadButton.Quit"/>).</summary>
    public static ButtonHint QuitHint(GamepadKind kind, string label) =>
        new([For(kind, PromptButton.View), For(kind, PromptButton.Menu)], label, IsHold: true);
}
