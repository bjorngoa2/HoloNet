using HoloNet.Shared.Helpers;

namespace HoloNet.Photos.Configuration;

public class PhotoServiceOptions
{
    public string PhotoPath { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    public MediaDirectory GetPhotoDirectory() => MediaDirectory.From(PhotoPath);
    public ServiceBaseUrl GetBaseUrl() => ServiceBaseUrl.From(BaseUrl);
}
