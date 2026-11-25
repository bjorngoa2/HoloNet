using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="ClipboardLink"/> only prefills the add-a-title field with web links, so
/// unrelated copied text isn't offered as a title to add.
/// </summary>
public class ClipboardLinkShould
{
    [Theory]
    [InlineData("https://www.disneyplus.com/browse/entity-1", "https://www.disneyplus.com/browse/entity-1")]
    [InlineData("  http://www.amazon.com/gp/video/detail/B0ABC\r\n", "http://www.amazon.com/gp/video/detail/B0ABC")]
    public void KeepWebLinks_Trimmed(string clipboardText, string expected) =>
        Assert.Equal(expected, ClipboardLink.Parse(clipboardText));

    [Theory]
    [InlineData("")]
    [InlineData("Remember to buy milk")]
    [InlineData("file:///C:/Users/secret.txt")]
    [InlineData("B0FQM41JFJ")]
    public void IgnoreAnythingElse(string clipboardText) =>
        Assert.Equal(string.Empty, ClipboardLink.Parse(clipboardText));
}