namespace HoloNet.Shared.Helpers;

/// <summary>
/// Small helper for wiring validating factory methods (e.g. <see cref="MediaDirectory.From"/>,
/// <see cref="ServiceBaseUrl.From"/>) into <c>OptionsBuilder&lt;T&gt;.Validate(...)</c>. Pairs
/// with <c>.ValidateOnStart()</c> so a misconfigured setting (a missing media directory, a
/// malformed base URL) fails application startup with a clear message, instead of surfacing only
/// on the first request that happens to touch it.
/// </summary>
public static class OptionsValidation
{
    /// <summary>Runs <paramref name="probe"/> and reports whether it completed without throwing.</summary>
    public static bool IsValid(Action probe)
    {
        try
        {
            probe();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
