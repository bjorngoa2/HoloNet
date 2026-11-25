namespace HoloNet.Video.Models;

/// <summary>
/// Body of <c>POST api/v1/library</c>: a link to a title's page on a streaming site (or its
/// bare id). The streaming service is recognised from the link itself.
/// </summary>
public record LibraryImportRequest(string UrlOrId);
