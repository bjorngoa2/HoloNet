using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>What the home screen's hint bar can offer right now.</summary>
/// <param name="Focused">The focused tile, or <c>null</c> on an empty screen.</param>
/// <param name="CanRemove">The focused tile is a streaming library title that can be removed.</param>
/// <param name="CanMove">The focused tile is a home tile that can be picked up and moved.</param>
/// <param name="IsMoving">A home tile is being moved, so only the move's own actions apply.</param>
public sealed record HomeHintState(
    IPickerCard? Focused,
    bool CanRestart,
    bool CanGoBack,
    bool IsUpdatePending,
    bool CanRemove = false,
    bool CanMove = false,
    bool IsMoving = false);

/// <summary>
/// Builds the home screen's button hints. Like a console dashboard, the bar only lists actions
/// that do something on the current screen, in a fixed order: the primary action (named after
/// what it does, e.g. "Play"), then the other face buttons, back, and the menu button.
/// </summary>
public static class HomeHints
{
    public static IReadOnlyList<ButtonHint> For(GamepadKind kind, HomeHintState state)
    {
        if (state.IsMoving)
        {
            return
            [
                ButtonPrompts.Hint(kind, PromptButton.DPad, "Move"),
                ButtonPrompts.Hint(kind, PromptButton.Confirm, "Done"),
                ButtonPrompts.Hint(kind, PromptButton.Cancel, "Cancel")
            ];
        }

        var hints = new List<ButtonHint>();

        if (state.Focused is { } focused)
            hints.Add(ButtonPrompts.Hint(kind, PromptButton.Confirm, focused.ActionLabel));
        if (state.CanRestart)
            hints.Add(ButtonPrompts.Hint(kind, PromptButton.Secondary, "Start over"));
        if (state.CanRemove)
            hints.Add(ButtonPrompts.Hint(kind, PromptButton.Tertiary, "Remove"));
        else if (state.CanMove)
            hints.Add(ButtonPrompts.Hint(kind, PromptButton.Tertiary, "Move"));
        if (state.CanGoBack)
            hints.Add(ButtonPrompts.Hint(kind, PromptButton.Cancel, "Back"));

        hints.Add(ButtonPrompts.Hint(kind, PromptButton.Menu, state.IsUpdatePending ? "Install update" : "Refresh"));
        return hints;
    }
}
