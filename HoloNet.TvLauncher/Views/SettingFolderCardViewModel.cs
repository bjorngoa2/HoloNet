namespace HoloNet.TvLauncher.Views;

/// <summary>
/// A folder for one setting in the Settings folder (see <see cref="SettingsMenu"/>). Its subtitle
/// shows the option in use, read fresh each time, so it's right after going back from choosing a
/// new option.
/// </summary>
public sealed class SettingFolderCardViewModel(
    string title,
    Func<string> currentOptionName,
    Func<List<IPickerCard>> childrenFactory,
    CardArt art) : FolderCardViewModel(title, string.Empty, childrenFactory, art: art)
{
    public override string Subtitle => currentOptionName();
}
