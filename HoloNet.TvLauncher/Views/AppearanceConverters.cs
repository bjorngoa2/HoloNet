using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Parses the hex accent colours carried by cards (see <see cref="IPickerCard.AccentColor"/>).
/// Shortcut colours come from user config, so a typo falls back to HoloNet blue rather than
/// throwing during a render pass.
/// </summary>
public static class AccentColors
{
    public static Color Parse(string? hex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex) && ColorConverter.ConvertFromString(hex) is Color color)
                return color;
        }
        catch (FormatException)
        {
            // Invalid config value - fall through to the default.
        }

        return (Color)ColorConverter.ConvertFromString(PickerCardBase.DefaultAccentColor);
    }

    /// <summary>Darkens <paramref name="color"/> towards black by <paramref name="amount"/> (0-1).</summary>
    public static Color Darken(Color color, double amount) => Color.FromRgb(
        (byte)(color.R * (1 - amount)),
        (byte)(color.G * (1 - amount)),
        (byte)(color.B * (1 - amount)));
}

/// <summary>
/// Turns a card's hex <see cref="IPickerCard.AccentColor"/> into the brand-tile background: a
/// diagonal gradient from the accent to a deeper shade of it, which reads as "designed" rather
/// than a flat swatch (the treatment Google TV and tvOS use for app tiles without banner art).
/// </summary>
public sealed class AccentToTileBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var accent = AccentColors.Parse(value as string);
        var brush = new LinearGradientBrush(accent, AccentColors.Darken(accent, 0.55), new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts a hex colour string to a <see cref="SolidColorBrush"/> (button prompt chips).</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var brush = new SolidColorBrush(AccentColors.Parse(value as string));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
