namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Double-dispatch counterpart to <see cref="IPickerCard.Accept{TResult}"/>. Lets callers (e.g.
/// <see cref="MainWindow"/>'s selection handling) branch on which concrete card was selected
/// without downcasting/type-checking against <see cref="FolderCardViewModel"/>,
/// <see cref="ShortcutCardViewModel"/>, <see cref="GameCardViewModel"/>,
/// <see cref="MediaCardViewModel"/>, <see cref="PrimeAuthActionCardViewModel"/>,
/// <see cref="AddTitleActionCardViewModel"/>, and <see cref="SettingChoiceCardViewModel"/> — the
/// compiler enforces that every card kind is handled, and adding a new kind is a compile error
/// here rather than a silently-missed branch elsewhere.
/// </summary>
public interface IPickerCardVisitor<out TResult>
{
    TResult VisitFolder(FolderCardViewModel folder);

    TResult VisitShortcut(ShortcutCardViewModel shortcut);

    TResult VisitGame(GameCardViewModel game);

    TResult VisitMedia(MediaCardViewModel media);

    TResult VisitPrimeAuthAction(PrimeAuthActionCardViewModel primeAuthAction);

    TResult VisitAddTitleAction(AddTitleActionCardViewModel addTitleAction);

    TResult VisitSettingChoice(SettingChoiceCardViewModel settingChoice);
}
