using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="HomeHints"/> only offers actions that work on the current screen, in
/// the controller's own button glyphs.
/// </summary>
public class HomeHintsShould
{
    private static readonly IPickerCard App = new ShortcutCardViewModel(new ShortcutMapping { Title = "YouTube", Url = "https://youtube.com" });

    [Fact]
    public void NameThePrimaryActionAfterTheFocusedTile()
    {
        var hints = HomeHints.For(GamepadKind.Xbox, new HomeHintState(App, CanRestart: false, CanGoBack: false, IsUpdatePending: false));

        Assert.Equal(["Open", "Refresh"], hints.Select(h => h.Label));
        Assert.Equal("A", hints[0].Buttons.Single().Symbol);
    }

    [Fact]
    public void OfferEveryAvailableAction_InAFixedOrder()
    {
        var hints = HomeHints.For(GamepadKind.PlayStation, new HomeHintState(App, CanRestart: true, CanGoBack: true, IsUpdatePending: true, CanRemove: true));

        Assert.Equal(["Open", "Start over", "Remove", "Back", "Install update"], hints.Select(h => h.Label));
        Assert.Equal(["✕", "△", "◻", "○", "Options"],  hints.Select(h => h.Buttons.Single().Symbol));
    }

    [Fact]
    public void OmitThePrimaryAction_OnAnEmptyScreen()
    {
        var hints = HomeHints.For(GamepadKind.Xbox, new HomeHintState(Focused: null, CanRestart: false, CanGoBack: true, IsUpdatePending: false));

        Assert.Equal(["Back", "Refresh"], hints.Select(h => h.Label));
    }

    [Fact]
    public void OfferMove_ForAMovableHomeTile()
    {
        var hints = HomeHints.For(GamepadKind.Xbox, new HomeHintState(App, CanRestart: false, CanGoBack: false, IsUpdatePending: false, CanMove: true));

        Assert.Equal(["Open", "Move", "Refresh"], hints.Select(h => h.Label));
        Assert.Equal("X", hints[1].Buttons.Single().Symbol);
    }

    [Fact]
    public void OnlyOfferTheMovesOwnActions_WhileMovingATile()
    {
        var hints = HomeHints.For(GamepadKind.PlayStation, new HomeHintState(App, CanRestart: false, CanGoBack: false, IsUpdatePending: true, IsMoving: true));

        Assert.Equal(["Move", "Done", "Cancel"], hints.Select(h => h.Label));
        Assert.Equal(["◀ ▲ ▼ ▶", "✕", "○"], hints.Select(h => h.Buttons.Single().Symbol));
    }
}
