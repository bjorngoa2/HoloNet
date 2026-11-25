using System.Text.Json.Serialization;
using HoloNet.Shared.Models;

namespace HoloNet.Video.Models;

/// <summary>
/// A title in the user's streaming library (see <see cref="Services.ILibraryService"/>), as
/// returned by <c>api/v1/library</c>. <see cref="Provider"/> is the same name used by the
/// <c>api/v1/catalog/{provider}/{id}</c> route, so a client can look the title up directly.
/// </summary>
public record LibraryTitleDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter<MediaProvider>))] MediaProvider Provider,
    string ProviderMediaId,
    string Title,
    [property: JsonConverter(typeof(JsonStringEnumConverter<MediaEntityType>))] MediaEntityType EntityType,
    string? PosterUrl,
    DateTimeOffset AddedUtc);
