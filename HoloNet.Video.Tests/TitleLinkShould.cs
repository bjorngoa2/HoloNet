using HoloNet.Video.Providers.DisneyPlus;
using HoloNet.Video.Providers.HboMax;
using HoloNet.Video.Providers.PrimeVideo;

namespace HoloNet.Video.Tests;

public class TitleLinkShould
{
    [Theory]
    [InlineData("https://www.primevideo.com/-/nb/detail/0K16R3PLUFGC2JUE457C26O4OD/ref=atv_sr_fle_c_Tn74RA_1_1_1", "0K16R3PLUFGC2JUE457C26O4OD")]
    [InlineData("https://www.primevideo.com/region/eu/detail/0SHGKA0J8D4G01ZGD647627NEJ", "0SHGKA0J8D4G01ZGD647627NEJ")]
    [InlineData("www.primevideo.com/detail/Reacher/0K16R3PLUFGC2JUE457C26O4OD/", "0K16R3PLUFGC2JUE457C26O4OD")]
    [InlineData("https://www.amazon.co.uk/gp/video/detail/B0D8KQFHMD/ref=atv_dp", "B0D8KQFHMD")]
    [InlineData(" 0GZ76HXP3TQS4E42G5JI9KQR6Q ", "0GZ76HXP3TQS4E42G5JI9KQR6Q")]
    public void RecognisePrimeVideoTitles(string link, string expectedId) =>
        Assert.Equal(expectedId, PrimeVideoTitleLink.ParseTitleId(link));

    [Theory]
    [InlineData("https://www.primevideo.com/-/nb/storefront")]
    [InlineData("https://www.disneyplus.com/en-gb/browse/entity-314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    [InlineData("https://evil.example/primevideo.com/detail/0K16R3PLUFGC2JUE457C26O4OD")]
    [InlineData("reacher")]
    public void RejectNonPrimeVideoTitles(string link) =>
        Assert.Null(PrimeVideoTitleLink.ParseTitleId(link));

    [Theory]
    [InlineData("https://www.disneyplus.com/en-gb/browse/entity-314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    [InlineData("https://www.disneyplus.com/nb-no/browse/entity-314F14B4-B70A-4EC6-B634-2559F0B1F77E?season=abc")]
    [InlineData("disneyplus.com/browse/entity-314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    [InlineData("314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    public void RecogniseDisneyPlusSeries(string link) =>
        Assert.Equal("314f14b4-b70a-4ec6-b634-2559f0b1f77e", DisneyPlusTitleLink.ParseTitleId(link));

    [Theory]
    [InlineData("https://www.disneyplus.com/en-gb/play/2b2b0e4f-6d7e-4a43-9a0a-3c2a2e0d4a11")]
    [InlineData("https://www.primevideo.com/detail/0K16R3PLUFGC2JUE457C26O4OD")]
    [InlineData("https://notdisneyplus.com/browse/entity-314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    public void RejectNonDisneyPlusSeries(string link) =>
        Assert.Null(DisneyPlusTitleLink.ParseTitleId(link));

    [Theory]
    [InlineData("https://www.hbomax.com/no/en/show/c8ea8e19-cae7-4683-9b62-cdbbed744784")]
    [InlineData("https://www.hbomax.com/no/en/show/C8EA8E19-CAE7-4683-9B62-CDBBED744784/s3")]
    [InlineData("https://play.hbomax.com/show/c8ea8e19-cae7-4683-9b62-cdbbed744784")]
    [InlineData("www.hbomax.com/no/en/movie/c8ea8e19-cae7-4683-9b62-cdbbed744784")]
    public void RecogniseHboMaxTitles(string link) =>
        Assert.Equal("c8ea8e19-cae7-4683-9b62-cdbbed744784", HboMaxTitleLink.ParseTitleId(link));

    [Theory]
    [InlineData("https://play.hbomax.com/video/watch/da61aef2-e4b6-441a-b2aa-91855ca05dad")]
    [InlineData("https://www.disneyplus.com/en-gb/browse/entity-314f14b4-b70a-4ec6-b634-2559f0b1f77e")]
    [InlineData("https://nothbomax.com/no/en/show/c8ea8e19-cae7-4683-9b62-cdbbed744784")]
    [InlineData("c8ea8e19-cae7-4683-9b62-cdbbed744784")]
    public void RejectNonHboMaxTitles(string link) =>
        Assert.Null(HboMaxTitleLink.ParseTitleId(link));
}
