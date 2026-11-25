namespace HoloNet.TvLauncher.Views;

/// <summary>
/// One option of a setting in the Settings folder (see <see cref="SettingsMenu"/>), e.g. the
/// "Ambient clock" screensaver. Its icon turns into a check mark while it's the option in use, so
/// the current choice stands out among its siblings without opening anything.
/// </summary>
/// <param name="settingName">The setting this is an option of, shown as the subtitle.</param>
/// <param name="isChosen">Whether this option is the one currently in use.</param>
/// <param name="choose">Makes this option the one in use (Command pattern).</param>
public sealed class SettingChoiceCardViewModel(
    string title,
    string settingName,
    string description,
    string iconGlyph,
    Func<bool> isChosen,
    Func<Task> choose) : PickerCardBase
{
    public override string Title { get; } = title;

    public override string Subtitle => IsChosen ? $"{settingName} · In use" : settingName;

    public override string Description { get; } = description;

    public override string IconGlyph => IsChosen ? TileIcons.Check : iconGlyph;

    public override string AccentColor => SettingsMenu.AccentColor;

    public override string ActionLabel => "Use";

    public bool IsChosen => isChosen();

    public Task ChooseAsync() => choose();

    /// <summary>Re-reads whether this option is in use, after any option of its setting is chosen.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(IconGlyph));
    }

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitSettingChoice(this);
}
