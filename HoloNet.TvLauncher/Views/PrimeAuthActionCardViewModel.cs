namespace HoloNet.TvLauncher.Views;

/// <summary>
/// A fixed "Refresh Prime Login" tile shown at the top of the Prime Video folder (see
/// <see cref="FolderNavigator"/>), alongside the resolved <see cref="MediaCardViewModel"/> title
/// cards. Selecting it launches the standalone <c>HoloNet.PrimeAuth</c> browser-login tool (see
/// <see cref="Services.IPrimeAuthLauncher"/>) so you can refresh an expired Prime Video session
/// without leaving the picker - always present (regardless of whether the current cookie is
/// still valid, or whether any titles currently resolve) so there's always a way back in.
/// </summary>
public class PrimeAuthActionCardViewModel : PickerCardBase
{
    public override string Title => "Refresh Prime Login";

    public override string Subtitle => "Prime Video account";

    public override string Description =>
        "Sign in to Prime Video again if your titles stop loading. A browser window opens for the login.";

    public override string IconGlyph => TileIcons.Key;

    public override string AccentColor => StreamingService.PrimeVideo.AccentColor;

    public override string ActionLabel => "Sign in";

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitPrimeAuthAction(this);
}
