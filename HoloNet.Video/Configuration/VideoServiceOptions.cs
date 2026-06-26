using HoloNet.Shared.Helpers;

namespace HoloNet.Video.Configuration;

public class VideoServiceOptions
{
    public string VideoPath { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    public MediaDirectory GetVideoDirectory() => MediaDirectory.From(VideoPath);
    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);
}
