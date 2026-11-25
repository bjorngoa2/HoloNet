using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Converters backing the picker card's progress-bar overlay (see <c>Themes/Theme.xaml</c>'s
/// shared card <c>DataTemplate</c>), driven purely by <see cref="IPickerCard.ProgressPercentage"/>
/// (0-100, or <c>null</c> for "no progress data").
/// </summary>
public sealed class ProgressToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double percentage && percentage > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a 0-100 percentage to a star-sized <see cref="GridLength"/> for a two-column progress
/// track (filled portion in column 0, remainder in column 1) - pass <c>ConverterParameter="Remainder"</c>
/// for column 1's binding so the two columns always sum to a consistent total regardless of the
/// track's actual pixel width.
/// </summary>
public sealed class ProgressToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percentage = value is double p ? Math.Clamp(p, 0, 100) : 0;
        var isRemainder = string.Equals(parameter as string, "Remainder", StringComparison.OrdinalIgnoreCase);
        var weight = isRemainder ? 100 - percentage : percentage;

        // A zero-weight star column collapses to nothing, which is fine for either extreme
        // (0% watched, or fully watched) - avoid GridLength(0, Star) edge cases by flooring at
        // a tiny nonzero weight instead.
        return new GridLength(Math.Max(weight, 0.01), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
