using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace HoloNet.TvLauncher.Services;

public interface IGameScreenshotService
{
    /// <summary>
    /// Captures the given emulator window (see <see cref="IGameLauncher.CurrentEmulatorWindowHandle"/>)
    /// and saves it as that game's "currently at" showcase image, overwriting any previous
    /// capture for the same title. Best-effort: swallows and logs any failure (e.g. the window
    /// handle is invalid/minimized) rather than disrupting the quit flow. Falls back to a
    /// full-screen capture if <paramref name="windowHandle"/> is <see cref="IntPtr.Zero"/> or
    /// invalid, so something is still captured rather than nothing.
    /// </summary>
    void Capture(string gameTitle, IntPtr windowHandle);

    /// <summary>
    /// Absolute path to the most recent showcase screenshot captured for <paramref name="gameTitle"/>,
    /// or <c>null</c> if none has been captured yet.
    /// </summary>
    string? GetScreenshotPath(string gameTitle);
}

/// <summary>
/// Captures a snapshot of the emulator window right before it's quit, so the picker can show
/// "where you currently are" in a game as a preview image — a generic, per-game-code-free
/// complement to the reverse-engineered save-stats (Bolts/playtime/location) feature. Captures
/// the on-screen region of the specific emulator window (not the whole screen), so an
/// overlapping window (e.g. this picker) can't end up in the shot instead.
/// </summary>
public class GameScreenshotService : IGameScreenshotService
{
    private static readonly string ScreenshotsDirectory =
        Path.Combine(AppContext.BaseDirectory, "Screenshots");

    // PCSX2 (and most emulators sitting behind hardware-overlay/DRM-protected present paths)
    // can't be screen-captured by any OS-level API — see CaptureViaDesktopDuplication's remarks.
    // Instead, we ask the emulator itself to take a screenshot (its own "Save Screenshot" hotkey,
    // F8 by default) since it reads its internal framebuffer directly, then pick up the resulting
    // file from its "snaps" folder.
    private const int PcsxScreenshotHotkeyVirtualKey = 0x77; // VK_F8

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const uint KeyEventFKeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public void Capture(string gameTitle, IntPtr windowHandle)
    {
        try
        {
            Directory.CreateDirectory(ScreenshotsDirectory);

            if (TryCaptureViaPcsx2Snapshot(gameTitle, windowHandle))
                return;

            LogError($"PCSX2 in-emulator screenshot unavailable/failed for \"{gameTitle}\" — falling back to screen capture (will likely be black for overlay-presented content).");

            Rectangle? cropRect = null;
            if (windowHandle != IntPtr.Zero && IsWindow(windowHandle) && GetWindowRect(windowHandle, out var rect))
                cropRect = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            Bitmap? bitmap = null;
            try
            {
                bitmap = CaptureViaDesktopDuplication(cropRect);
            }
            catch (Exception ex)
            {
                LogError($"Desktop Duplication capture failed for \"{gameTitle}\", falling back to GDI: {ex}");
            }

            bitmap ??= CaptureFullScreenGdi(cropRect);

            using var _ = bitmap;
            bitmap.Save(GetScreenshotPathForWrite(gameTitle), ImageFormat.Png);
        }
        catch (Exception ex)
        {
            LogError($"Failed to capture showcase screenshot for \"{gameTitle}\": {ex}");
        }
    }

    /// <summary>
    /// Asks PCSX2 to save its own screenshot (simulating its "Save Screenshot" hotkey, F8 by
    /// default, read from the user's PCSX2.ini in case they've rebound it) and picks up the
    /// resulting file from its "snaps" folder. This reads PCSX2's internal GS framebuffer
    /// directly rather than the composited desktop, so — unlike GDI/PrintWindow/Desktop
    /// Duplication — it isn't affected by hardware overlay planes or DXGI flip-model swapchains
    /// bypassing the compositor. Returns <c>false</c> (caller should fall back) if PCSX2's
    /// folders can't be located, the window can't be focused, or no new file shows up in time.
    /// </summary>
    private bool TryCaptureViaPcsx2Snapshot(string gameTitle, IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
            return false;

        var snapsDirectory = GetPcsx2SnapsDirectory();
        if (snapsDirectory is null || !Directory.Exists(snapsDirectory))
        {
            LogError($"Could not locate PCSX2's snaps folder (looked for {snapsDirectory ?? "<null>"}).");
            return false;
        }

        var hotkeyVk = GetPcsx2ScreenshotHotkeyVirtualKey();

        var before = new HashSet<string>(Directory.EnumerateFiles(snapsDirectory));

        if (!SetForegroundWindow(windowHandle))
            LogError("SetForegroundWindow failed before sending the PCSX2 screenshot hotkey (continuing anyway).");

        keybd_event((byte)hotkeyVk, 0, 0, UIntPtr.Zero);
        keybd_event((byte)hotkeyVk, 0, KeyEventFKeyUp, UIntPtr.Zero);

        // PCSX2 writes the file asynchronously (image encode happens on a worker thread) —
        // poll briefly rather than assuming it's instantaneous.
        string? newFile = null;
        for (var attempt = 0; attempt < 20 && newFile is null; attempt++)
        {
            Thread.Sleep(150);
            newFile = Directory.EnumerateFiles(snapsDirectory)
                .Where(f => !before.Contains(f))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        if (newFile is null)
        {
            LogError($"No new file appeared in \"{snapsDirectory}\" after sending the PCSX2 screenshot hotkey (VK=0x{hotkeyVk:X}).");
            return false;
        }

        // PCSX2 encodes/writes the screenshot on a worker thread, so the file can still be
        // growing (or exclusively locked) for a short while after it first appears — wait until
        // its size stops changing, and retry the actual read a few times, rather than assuming a
        // fixed delay is always long enough.
        if (!WaitForFileToStopGrowing(newFile))
            LogError($"\"{newFile}\" never stabilized in size — attempting to read it anyway.");

        Exception? lastError = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using var source = new Bitmap(newFile);
                source.Save(GetScreenshotPathForWrite(gameTitle), ImageFormat.Png);
                LogError($"Captured PCSX2 in-emulator screenshot for \"{gameTitle}\" from \"{newFile}\".");
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex;
                Thread.Sleep(150);
            }
        }

        {
            var ex = lastError!;
            LogError($"Failed to read/convert PCSX2 screenshot \"{newFile}\": {ex}");
            return false;
        }
    }

    /// <summary>
    /// Polls a file's size until it stops changing for two consecutive checks (indicating PCSX2's
    /// background encode/write has finished) or a timeout elapses. Returns <c>false</c> on
    /// timeout (caller falls back to retrying the read anyway).
    /// </summary>
    private static bool WaitForFileToStopGrowing(string path)
    {
        long lastSize = -1;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            Thread.Sleep(150);
            long size;
            try
            {
                size = new FileInfo(path).Length;
            }
            catch (IOException)
            {
                continue; // still exclusively locked by PCSX2 — keep waiting.
            }

            if (size > 0 && size == lastSize)
                return true;

            lastSize = size;
        }

        return false;
    }

    /// <summary>
    /// Resolves PCSX2's "snaps" folder from its PCSX2.ini (<c>[Folders] Snapshots = ...</c>,
    /// relative to the ini's own directory unless it's rooted), falling back to the default
    /// <c>Documents\PCSX2\snaps</c> if the ini can't be found/parsed.
    /// </summary>
    private static string? GetPcsx2SnapsDirectory()
    {
        var documentsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PCSX2");
        var iniPath = Path.Combine(documentsRoot, "inis", "PCSX2.ini");

        var relative = ReadIniValue(iniPath, "Folders", "Snapshots") ?? "snaps";
        return Path.IsPathRooted(relative) ? relative : Path.Combine(documentsRoot, relative);
    }

    /// <summary>
    /// Resolves PCSX2's "Save Screenshot" hotkey virtual key from PCSX2.ini
    /// (<c>[Hotkeys] Screenshot = Keyboard/F8</c>), in case the user has rebound it, falling back
    /// to F8 (PCSX2's default) if not found/parseable.
    /// </summary>
    private static int GetPcsx2ScreenshotHotkeyVirtualKey()
    {
        var documentsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PCSX2");
        var iniPath = Path.Combine(documentsRoot, "inis", "PCSX2.ini");

        var value = ReadIniValue(iniPath, "Hotkeys", "Screenshot");
        if (value is null)
            return PcsxScreenshotHotkeyVirtualKey;

        // Value looks like "Keyboard/F8" — take the part after the last '/'.
        var keyName = value[(value.LastIndexOf('/') + 1)..].Trim();
        return keyName.ToUpperInvariant() switch
        {
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            _ => PcsxScreenshotHotkeyVirtualKey,
        };
    }

    private static string? ReadIniValue(string iniPath, string section, string key)
    {
        if (!File.Exists(iniPath))
            return null;

        string? currentSection = null;
        foreach (var line in File.ReadLines(iniPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1];
                continue;
            }

            if (currentSection != section)
                continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == key)
                return parts[1].Trim();
        }

        return null;
    }

    /// <summary>
    /// Quick brightness sample (average of a handful of pixels) logged alongside each capture
    /// attempt, purely as a diagnostic — a value near 0 strongly suggests a genuinely black
    /// source frame (e.g. a hardware-overlay/DRM-protected surface neither GDI nor Desktop
    /// Duplication can see) rather than a bug in how the bitmap itself was built.
    /// </summary>
    private static double AverageBrightness(Bitmap bitmap)
    {
        var samples = 0;
        long total = 0;
        var stepX = Math.Max(1, bitmap.Width / 20);
        var stepY = Math.Max(1, bitmap.Height / 20);
        for (var y = 0; y < bitmap.Height; y += stepY)
        for (var x = 0; x < bitmap.Width; x += stepX)
        {
            var pixel = bitmap.GetPixel(x, y);
            total += pixel.R + pixel.G + pixel.B;
            samples++;
        }

        return samples == 0 ? 0 : total / (double)(samples * 3);
    }

    /// <summary>
    /// Captures the desktop via the DXGI Desktop Duplication API (the same technique OBS/Discord
    /// use for game-capture) and crops to <paramref name="cropRect"/> if given. Unlike plain GDI
    /// <c>BitBlt</c>/<c>CopyFromScreen</c>, this reads the actual composited frame at the driver
    /// level, so it isn't fooled by GPU "fullscreen optimizations" or DXGI flip-model swapchains
    /// that bypass the desktop compositor and make GDI-based capture return solid black for
    /// borderless/exclusive-fullscreen games such as PCSX2. Returns <c>null</c> (falls back to
    /// GDI) if duplication isn't available for any reason (e.g. no compatible adapter, or the
    /// desktop is in a protected/DRM state).
    /// </summary>
    private static unsafe Bitmap? CaptureViaDesktopDuplication(Rectangle? cropRect)
    {
        using var factory = new SharpDX.DXGI.Factory1();
        foreach (var adapter in factory.Adapters1)
        {
            for (var outputIndex = 0; outputIndex < adapter.GetOutputCount(); outputIndex++)
            {
                using var output = adapter.GetOutput(outputIndex);
                using var output1 = output.QueryInterface<SharpDX.DXGI.Output1>();
                var outputBounds = output.Description.DesktopBounds;

                // Only attempt duplication against the output that actually contains the
                // window/region we care about (a multi-monitor setup could otherwise duplicate
                // the wrong screen).
                if (cropRect is { } wanted &&
                    !new Rectangle(outputBounds.Left, outputBounds.Top,
                        outputBounds.Right - outputBounds.Left, outputBounds.Bottom - outputBounds.Top)
                        .IntersectsWith(wanted))
                    continue;

                LogError($"Duplicating adapter \"{adapter.Description1.Description}\" output {outputIndex} bounds=({outputBounds.Left},{outputBounds.Top},{outputBounds.Right},{outputBounds.Bottom})");

                using var device = new SharpDX.Direct3D11.Device(adapter);
                using var duplication = output1.DuplicateOutput(device);

                SharpDX.DXGI.Resource? screenResource = null;
                try
                {
                    var result = duplication.TryAcquireNextFrame(500, out _, out screenResource);
                    if (result.Failure || screenResource is null)
                        return null;

                    using var screenTexture = screenResource.QueryInterface<SharpDX.Direct3D11.Texture2D>();
                    var desc = screenTexture.Description;
                    desc.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read;
                    desc.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;
                    desc.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;
                    desc.BindFlags = SharpDX.Direct3D11.BindFlags.None;

                    using var stagingTexture = new SharpDX.Direct3D11.Texture2D(device, desc);
                    device.ImmediateContext.CopyResource(screenTexture, stagingTexture);

                    var dataBox = device.ImmediateContext.MapSubresource(
                        stagingTexture, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                    try
                    {
                        var bitmap = new Bitmap(desc.Width, desc.Height, PixelFormat.Format32bppRgb);
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, desc.Width, desc.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        try
                        {
                            var sourcePtr = dataBox.DataPointer;
                            var destPtr = bitmapData.Scan0;
                            for (var y = 0; y < desc.Height; y++)
                            {
                                // BGRA (DXGI) copies directly into Format32bppRgb (also
                                // byte-order BGRx on little-endian) row by row, since DXGI's row
                                // pitch may include padding that doesn't match the bitmap stride.
                                // Using the alpha-less Rgb format (rather than Argb) sidesteps
                                // Desktop Duplication's backbuffer alpha normally being 0 for
                                // ordinary opaque screen content, which would otherwise render as
                                // fully transparent (solid white/blank) in an Argb bitmap.
                                Buffer.MemoryCopy(
                                    (sourcePtr + y * dataBox.RowPitch).ToPointer(),
                                    (destPtr + y * bitmapData.Stride).ToPointer(),
                                    bitmapData.Stride, Math.Min(bitmapData.Stride, dataBox.RowPitch));
                            }
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }

                        if (cropRect is { } crop)
                        {
                            // The captured frame is relative to this output's own desktop
                            // origin, not the whole virtual desktop — offset the crop rect
                            // accordingly.
                            var localCrop = new Rectangle(
                                crop.X - outputBounds.Left, crop.Y - outputBounds.Top, crop.Width, crop.Height);
                            localCrop.Intersect(new Rectangle(0, 0, desc.Width, desc.Height));
                            if (localCrop.Width <= 0 || localCrop.Height <= 0)
                                return bitmap;

                            var cropped = bitmap.Clone(localCrop, bitmap.PixelFormat);
                            bitmap.Dispose();
                            return cropped;
                        }

                        return bitmap;
                    }
                    finally
                    {
                        device.ImmediateContext.UnmapSubresource(stagingTexture, 0);
                    }
                }
                finally
                {
                    screenResource?.Dispose();
                    try
                    {
                        duplication.ReleaseFrame();
                    }
                    catch (SharpDX.SharpDXException)
                    {
                        // No frame was successfully acquired above — nothing to release.
                    }
                }
            }
        }

        return null;
    }

    private static Bitmap CaptureFullScreenGdi(Rectangle? cropRect)
    {
        var bounds = cropRect ?? new Rectangle(
            (int)System.Windows.SystemParameters.VirtualScreenLeft,
            (int)System.Windows.SystemParameters.VirtualScreenTop,
            (int)System.Windows.SystemParameters.VirtualScreenWidth,
            (int)System.Windows.SystemParameters.VirtualScreenHeight);

        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
        return bitmap;
    }

    public string? GetScreenshotPath(string gameTitle)
    {
        var path = GetScreenshotPathForWrite(gameTitle);
        return File.Exists(path) ? path : null;
    }

    private static string GetScreenshotPathForWrite(string gameTitle) =>
        Path.Combine(ScreenshotsDirectory, $"{SanitizeFileName(gameTitle)}.png");

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    /// <summary>
    /// Appends a timestamped line to <c>screenshot-errors.log</c> next to the exe, since capture
    /// failures happen during the quit flow with no UI feedback otherwise — lets you check what
    /// went wrong (e.g. Desktop Duplication not available on this GPU/driver) without attaching a
    /// debugger.
    /// </summary>
    private static void LogError(string message)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "screenshot-errors.log"),
                $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging itself must never throw and break the quit flow.
        }
    }
}
