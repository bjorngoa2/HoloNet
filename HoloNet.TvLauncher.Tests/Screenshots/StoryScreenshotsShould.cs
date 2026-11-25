using System.IO;
using System.Runtime.CompilerServices;
using HoloNet.TvLauncher.Gallery.Stories;

namespace HoloNet.TvLauncher.Tests.Screenshots;

/// <summary>
/// Renders every gallery story (see <see cref="StoryCatalog"/>) and compares it with its approved
/// screenshot in <c>Screenshots/Approved</c>, so any visual change shows up as a failing test and,
/// once approved, as a before/after image in the pull request.
/// </summary>
/// <remarks>
/// A mismatch writes what was rendered to <c>Screenshots/Received</c>, which CI uploads. To
/// approve a change, run the "Update screenshots" workflow on the branch: it renders with
/// <c>UPDATE_SCREENSHOTS=true</c>, which writes the approved images instead of comparing, and
/// commits them. Screenshots are only comparable when rendered on the same Windows image, so
/// that workflow and CI both use the same runner.
/// </remarks>
public class StoryScreenshotsShould
{
    private static readonly string ScreenshotsDirectory = ThisDirectory();
    private static readonly string ApprovedDirectory = Path.Combine(ScreenshotsDirectory, "Approved");
    private static readonly string ReceivedDirectory = Path.Combine(ScreenshotsDirectory, "Received");

    private static bool IsUpdating => Environment.GetEnvironmentVariable("UPDATE_SCREENSHOTS") == "true";

    public static TheoryData<string> StoryIds => new(StoryCatalog.All.Select(story => story.Id));

    [Theory]
    [MemberData(nameof(StoryIds))]
    public async Task LookAsApproved(string storyId)
    {
        var story = StoryCatalog.All.Single(s => s.Id == storyId);
        var approvedPath = Path.Combine(ApprovedDirectory, $"{story.Id}.png");
        var receivedPath = Path.Combine(ReceivedDirectory, $"{story.Id}.png");

        var rendered = await StoryRenderer.RenderAsync(story);
        var approved = File.Exists(approvedPath) ? Png.Read(approvedPath) : null;
        var differentPixels = approved is null ? null : Png.CountDifferentPixels(approved, rendered);

        if (differentPixels == 0)
        {
            if (File.Exists(receivedPath))
                File.Delete(receivedPath);
            return;
        }

        if (IsUpdating)
        {
            Png.Write(rendered, approvedPath);
            return;
        }

        Png.Write(rendered, receivedPath);
        Assert.Fail(approved is null
            ? $"'{story.Id}' has no approved screenshot yet. Check {receivedPath}, then run the \"Update screenshots\" workflow to approve it."
            : differentPixels is null
                ? $"'{story.Id}' rendered at a different size than its approved screenshot. See {receivedPath}."
                : $"'{story.Id}' looks different from its approved screenshot ({differentPixels} pixels). See {receivedPath}.");
    }

    [Fact]
    public void HaveAStoryForEveryApprovedScreenshot()
    {
        var storyIds = StoryCatalog.All.Select(story => story.Id).ToHashSet();
        var orphans = Directory.Exists(ApprovedDirectory)
            ? Directory.EnumerateFiles(ApprovedDirectory, "*.png", SearchOption.AllDirectories)
                .Where(path => !storyIds.Contains(IdOf(path)))
                .ToList()
            : [];

        if (IsUpdating)
        {
            orphans.ForEach(File.Delete);
            return;
        }

        Assert.True(orphans.Count == 0, $"Approved screenshots without a story (renamed or removed?): {string.Join(", ", orphans.Select(IdOf))}");
    }

    private static string IdOf(string approvedPath) =>
        Path.ChangeExtension(Path.GetRelativePath(ApprovedDirectory, approvedPath), null).Replace('\\', '/');

    private static string ThisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
